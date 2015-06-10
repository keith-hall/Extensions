IF OBJECT_ID('proc_ImportCSVFile', N'P') IS NOT NULL
	EXEC ('DROP PROCEDURE proc_ImportCSVFile')
GO
CREATE PROCEDURE proc_ImportCSVFile
	@PathToFile NVARCHAR(1000),
	@Separator CHAR(1) = ';',
	@Header NVARCHAR(600) = NULL,
	@IgnoreFileHeader BIT = 0,
	@TableName SYSNAME = NULL, -- if provided, stores the imported data in a table with this name
	@RowSample INT = 0, -- specify number of rows to load - used to check the data to see if the columns have been imported correctly and the data has the correct characters (i.e. identify charset / character encoding problems...)
	@ExtraColumns VARCHAR(1000) = NULL, -- useful for when executing SQL when the table already exists, i.e. no late-binding, to avoid errors
	@DebugMessages BIT = 0
AS

DECLARE @FileExists INT
SET NOCOUNT ON
EXEC master.dbo.xp_fileexist @PathToFile, @FileExists OUTPUT
SET NOCOUNT OFF
IF @FileExists = 0
BEGIN
	RAISERROR ('File "%s" does not exist.', 16, -1, @PathToFile)
	RETURN -1
END

DECLARE @FileContainsHeader BIT = CASE WHEN @Header IS NULL THEN 1 ELSE @IgnoreFileHeader END

IF @Header IS NULL AND @IgnoreFileHeader = 1
BEGIN
	RAISERROR ('No header provided.', 16, -1, @PathToFile)
	RETURN -1
END

DECLARE @existingTranCount INT = @@TRANCOUNT
IF @existingTranCount = 0
	BEGIN TRANSACTION
ELSE
	SAVE TRANSACTION before_import

DECLARE @SQL VARCHAR(MAX)
IF @TableName IS NULL
	SET @TableName = '##ImportedData'
ELSE IF SUBSTRING(@TableName, 1, 1) = '#' AND NOT SUBSTRING(@TableName, 2, 1) = '#' -- temporary table needs to be declared with two hashes otherwise drops out of scope immediately after dynamic sql statement creates it
BEGIN
	RAISERROR ('Table name must start with two hashes if it is to be temporary, to ensure that it does not drop out of scope immediately.', 16, -1)
	RETURN -1
END
DECLARE @Database SYSNAME = CASE WHEN SUBSTRING(@TableName, 1, 1) = '#' THEN 'TempDB' ELSE DB_NAME() END
DECLARE @Schema SYSNAME = 'dbo'
DECLARE @FullTableName SYSNAME = @Database + '.' + @Schema + '.' + @TableName
DECLARE @UseTableName SYSNAME = CASE WHEN @Database = 'TempDB' THEN @TableName ELSE @FullTableName END
DECLARE @FieldType VARCHAR(20) = 'NVARCHAR(450)'
DECLARE @Message NVARCHAR(2000)

SET NOCOUNT ON

-- if the table exists, drop it
IF OBJECT_ID(@FullTableName) IS NOT NULL
BEGIN
	SET @Message = 'Table ' + @FullTableName + ' exists, dropping...'
	IF @DebugMessages = 1
		RAISERROR (@Message, 0, 1) WITH NOWAIT
	
	SET @SQL = 'DROP TABLE ' + @UseTableName
	EXEC (@SQL)
	
	SET @Message = 'Table ' + @FullTableName + ' dropped.'
	IF @DebugMessages = 1
		RAISERROR (@Message, 0, 1) WITH NOWAIT
END

SET @Message = 'Reading first line of file to get ' + CASE WHEN @FileContainsHeader = 1 AND @IgnoreFileHeader = 0 THEN 'header' ELSE 'number of fields' END + '...'
IF @DebugMessages = 1
	RAISERROR (@Message, 0, 1) WITH NOWAIT

-- import the first line from the file
CREATE TABLE #Header (Line VARCHAR(MAX))

SET @SQL = '
BULK INSERT
	#Header
FROM
	''' + REPLACE(@PathToFile, '''', '''''') + '''
WITH (
	FIRSTROW        = 1,
	LASTROW         = 1,
	FIELDTERMINATOR = ''\n'',
	ROWTERMINATOR   = ''\n'',
	CODEPAGE        = ''1257'',
	TABLOCK
)'
EXEC (@SQL)

IF @FileContainsHeader = 1 AND @IgnoreFileHeader = 0
	SELECT TOP 1 @Header = Line
	FROM #Header
ELSE
BEGIN
	-- compare number of fields in file with number of fields in header supplied
	DECLARE @CurrentPosHeader INT = 1, @CurrentPosFile INT = 1
	WHILE @CurrentPosHeader > 0 AND @CurrentPosFile > 0
	BEGIN
		SET @CurrentPosHeader = CHARINDEX(@Separator, @Header                         , @CurrentPosHeader + LEN(@Separator))
		SET @CurrentPosFile   = CHARINDEX(@Separator, (SELECT TOP 1 Line FROM #Header), @CurrentPosFile   + LEN(@Separator))
	END
	IF NOT (@CurrentPosHeader = 0 AND @CurrentPosFile = 0)
	BEGIN
		IF @existingTranCount = 0
			ROLLBACK TRANSACTION
		ELSE
			ROLLBACK TRANSACTION before_import

		--RAISERROR (120, -1, -1)
		RAISERROR ('File contains a different number of fields than the header supplied.', 16, -1)
		RETURN -1
	END
END

DROP TABLE #Header

IF @DebugMessages = 1
BEGIN
	IF @IgnoreFileHeader = 1
	BEGIN
		SET @Message = 'Header skipped from file, using as provided: ' + @Header
		RAISERROR (@Message, 0, 1) WITH NOWAIT
	END
	ELSE IF @FileContainsHeader = 1
	BEGIN
		SET @Message = 'Header retrieved from file: ' + @Header
		RAISERROR (@Message, 0, 1) WITH NOWAIT
	END
	
	IF @FileContainsHeader = 0 OR @IgnoreFileHeader = 1
	BEGIN
		SET @Message =  'Field counts in first row of file match header.'
		RAISERROR (@Message, 0, 1) WITH NOWAIT
	END
END

-- read the columns from the header and add these to the table
DECLARE @Columns TABLE (ColumnPosition INT IDENTITY(1,1), ColumnName SYSNAME)
SET @SQL = ''
DECLARE @Field SYSNAME
WHILE CHARINDEX(@Separator, @Header) > 0
BEGIN
	SET @Field = SUBSTRING(@Header, 1, CHARINDEX(@Separator, @Header) - 1)
	SET @Header = SUBSTRING(@Header, LEN(@Field + @Separator) + 1, LEN(@Header))

	INSERT INTO @Columns (ColumnName) VALUES (@Field)

	SET @SQL = @SQL + '[' + @Field + '] ' + @FieldType + ', '
END
INSERT INTO @Columns (ColumnName) VALUES (@Header)
SET @SQL = @SQL + '[' + @Header + '] ' + @FieldType

IF EXISTS (SELECT TOP 1 * FROM @Columns WHERE ColumnName = '')
BEGIN
	IF @existingTranCount = 0
		ROLLBACK TRANSACTION
	ELSE
		ROLLBACK TRANSACTION before_import
	--RAISERROR (1038, 16, -1)
	RAISERROR ('A field name specified in the header is empty.', 16, -1)
	RETURN -1
END

SET @SQL = 'CREATE TABLE ' + @UseTableName + ' (' + @SQL + ')'
IF @DebugMessages = 1
BEGIN
	SET @Message = 'Creating table ' + @FullTableName + '...
	' + @SQL
	RAISERROR (@Message, 0, 1) WITH NOWAIT
END
EXEC (@SQL)

SET @Message = 'Table ' + @FullTableName + ' created successfully.'
IF @DebugMessages = 1
	RAISERROR (@Message, 0, 1) WITH NOWAIT

SET @Message = 'Importing data from file...'
IF @DebugMessages = 1
	RAISERROR (@Message, 0, 1) WITH NOWAIT

SET NOCOUNT OFF

-- import the data from the file
SET @SQL = '
BULK INSERT
	' + @UseTableName + '
FROM
	''' + REPLACE(@PathToFile, '''', '''''') + '''
WITH (
	FIRSTROW        = ' + CAST(1 + @FileContainsHeader AS VARCHAR) + ',
	FIELDTERMINATOR = ''' + @Separator + ''',
	ROWTERMINATOR   = ''\n'',
	CODEPAGE        = ''1257''' + CASE WHEN @RowSample > 0 THEN ',
	LASTROW         = ' + CAST(@RowSample + @FileContainsHeader AS VARCHAR) ELSE '' END + ',
	TABLOCK
)'
EXEC (@SQL)

SET @Message = 'Data imported from file.'
IF @DebugMessages = 1
	RAISERROR (@Message, 0, 1) WITH NOWAIT

IF ISNULL(@ExtraColumns, '') != ''
BEGIN
	SET @Message = 'Adding extra columns specified...'
	IF @DebugMessages = 1
		RAISERROR (@Message, 0, 1) WITH NOWAIT

	SET @SQL = 'ALTER TABLE ' + @UseTableName + ' ADD /*LineNumber INT IDENTITY(1, 1),*/ ' + @ExtraColumns -- commented out adding line number because doesn't seem to work as expected to use original order from file
	EXEC (@SQL)
	
	SET @Message = 'Table altered to include extra fields.'
	IF @DebugMessages = 1
		RAISERROR (@Message, 0, 1) WITH NOWAIT
END

SET NOCOUNT OFF
IF @existingTranCount = 0
	COMMIT TRANSACTION

SET @Message = 'End of stored procedure proc_ImportCSVFile.'
IF @DebugMessages = 1
	RAISERROR (@Message, 0, 1) WITH NOWAIT

RETURN 0
