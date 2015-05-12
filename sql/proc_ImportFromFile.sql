USE [darbine]
GO
IF OBJECT_ID('proc_ImportCardHashData', N'P') IS NOT NULL
	EXEC ('DROP PROCEDURE proc_ImportCardHashData')
GO
CREATE PROCEDURE proc_ImportCardHashData
	@FromFile NVARCHAR(1000),
	@Separator CHAR(1) = ';',
	@Header NVARCHAR(600) = NULL,
	@IgnoreFileHeader BIT = 0,
	@CardHashField SYSNAME = 1, -- name of field as per header, or ordinal position
	@TableName SYSNAME = NULL, -- if provided, stores the imported data in a table with this name
	@RowSample INT = 0, -- check the data to see if the columns have been imported correctly and the data has the correct characters (i.e. identify charset / character encoding problems...)
	@ExtraColumns VARCHAR(1000) = NULL,
	@DebugMessages BIT = 0
AS

DECLARE @FileExists INT
SET NOCOUNT ON
EXEC master.dbo.xp_fileexist @FromFile, @FileExists OUTPUT
SET NOCOUNT OFF
IF @FileExists = 0
BEGIN
	RAISERROR ('File "%s" does not exist', 16, -1, @FromFile)
	RETURN -1
END

DECLARE @FileContainsHeader BIT = CASE WHEN @Header IS NULL THEN 1 ELSE @IgnoreFileHeader END

IF @Header IS NULL AND @IgnoreFileHeader = 1
BEGIN
	RAISERROR ('No header provided.', 16, -1, @FromFile)
	RETURN -1
END

DECLARE @existingTranCount INT = @@TRANCOUNT
IF @existingTranCount = 0
	BEGIN TRANSACTION
ELSE
	SAVE TRANSACTION before_import

DECLARE @SQL VARCHAR(MAX)
--DECLARE @DropTable BIT = CASE WHEN @TableName IS NULL THEN 1 ELSE 0 END
IF @TableName IS NULL
	SET @TableName = '##ImportedData'
ELSE IF SUBSTRING(@TableName, 1, 1) = '#' AND NOT SUBSTRING(@TableName, 2, 1) = '#' -- temporary table needs to be declared with two hashes otherwise drops out of scope immediately after dynamic sql statement creates it
	SET @TableName = '#' + @TableName
DECLARE @Database SYSNAME = CASE WHEN SUBSTRING(@TableName, 1, 1) = '#' THEN 'TempDB' ELSE DB_NAME() END
DECLARE @Schema SYSNAME = 'dbo'
DECLARE @FullTableName SYSNAME = @Database + '.' + @Schema + '.' + @TableName
DECLARE @UseTableName SYSNAME = CASE WHEN @Database = 'TempDB' THEN @TableName ELSE @FullTableName END
DECLARE @FieldType VARCHAR(20) = 'NVARCHAR(450)'

SET NOCOUNT ON

-- if the table exists, drop it
IF OBJECT_ID(@FullTableName) IS NOT NULL
BEGIN
	IF @DebugMessages = 1
		PRINT 'Table ' + @FullTableName + ' exists, dropping...'
	SET @SQL = 'DROP TABLE ' + @UseTableName
	EXEC (@SQL)
	IF @DebugMessages = 1
		PRINT 'Table ' + @FullTableName + ' dropped.'
END

IF @DebugMessages = 1
	PRINT 'Reading first line of file to get ' + CASE WHEN @FileContainsHeader = 1 AND @IgnoreFileHeader = 0 THEN 'header' ELSE 'number of fields' END + '...'
		
-- import the first line from the file
CREATE TABLE #Header (Line VARCHAR(MAX))

SET @SQL = '
BULK INSERT
	#Header
FROM
	''' + REPLACE(@FromFile, '''', '''''') + '''
WITH (
	FIRSTROW        = 1,
	LASTROW         = 1,
	FIELDTERMINATOR = ''\n'',
	ROWTERMINATOR   = ''\n'',
	CODEPAGE        = ''1257''
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
		RAISERROR ('File contains a different number of fields than the header supplied', 16, -1)
		RETURN -1
	END
END

DROP TABLE #Header

IF @DebugMessages = 1
BEGIN
	IF @IgnoreFileHeader = 1
		PRINT 'Header skipped from file, using as provided: ' + @Header
	ELSE IF @FileContainsHeader = 1
		PRINT 'Header retrieved from file: ' + @Header
	
	IF @FileContainsHeader = 0 OR @IgnoreFileHeader = 1
		PRINT 'Field counts in first row of file match header.'
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
	RAISERROR ('A field name specified in the header is empty', 16, -1)
	RETURN -1
END

SET @SQL = 'CREATE TABLE ' + @UseTableName + ' (' + @SQL + ')'
IF @DebugMessages = 1
	PRINT 'Creating table ' + @FullTableName + '...
	' + @SQL

EXEC (@SQL)

IF @DebugMessages = 1
	PRINT 'Table ' + @FullTableName + ' created successfully'

-- get the CardHash field from it's position
IF ISNUMERIC(@CardHashField) = 1
	SELECT TOP 1 @CardHashField = ColumnName
	FROM @Columns
	WHERE ColumnPosition = @CardHashField
	OPTION (MAXDOP 1)

IF @DebugMessages = 1
BEGIN
	PRINT 'The field containing the card hash is called ' + @CardHashField
	--PRINT 'Creating index on this field...'
END

-- create index on CardHash field
--SET @SQL = 'CREATE INDEX [IX_' + @TableName + '_' + @CardHashField + '] ON ' + @UseTableName + ' ([' + @CardHashField + '])'
--EXEC (@SQL)

IF @DebugMessages = 1
BEGIN
	--PRINT @CardHashField + ' index created.'
	PRINT 'Importing data from file...'
END

SET NOCOUNT OFF

-- import the data from the file
SET @SQL = '
BULK INSERT
	' + @UseTableName + '
FROM
	''' + REPLACE(@FromFile, '''', '''''') + '''
WITH (
	FIRSTROW        = ' + CAST(1 + @FileContainsHeader AS VARCHAR) + ',
	FIELDTERMINATOR = ''' + @Separator + ''',
	ROWTERMINATOR   = ''\n'',
	CODEPAGE        = ''1257''' + CASE WHEN @RowSample > 0 THEN ',
	LASTROW         = ' + CAST(@RowSample + @FileContainsHeader AS VARCHAR) ELSE '' END + ',
	TABLOCK
)'
EXEC (@SQL)

IF @DebugMessages = 1
	PRINT 'Data imported from file. Adding CardID column and index...'

-- create CardID column in temporary table
SET @SQL = 'ALTER TABLE ' + @UseTableName + ' ADD /*LineNumber INT IDENTITY(1, 1),*/ CardID int null' + CASE WHEN @ExtraColumns IS NULL THEN '' ELSE ', ' + @ExtraColumns END
EXEC (@SQL)
SET @SQL = 'CREATE INDEX IX_' + @TableName + '_CardID ON ' + @UseTableName + ' (CardID)'
EXEC (@SQL)

IF @DebugMessages = 1
	PRINT 'Table updated.  Populating CardID field...'

-- splitting these into 2 separate queries drastically improves execution time!! (i.e. no LEFT JOIN to ECL)
/*-- lookup the cards by the CardHash field
SET @SQL = '
UPDATE D
SET CardID = Ca.CardID
FROM ' + @UseTableName + '                  (NOLOCK) D
LEFT OUTER JOIN PayLo.dbo.ExternalCardLoads (NOLOCK) ECL ON ECL.PanHash = D.[' + @CardHashField + '] COLLATE DATABASE_DEFAULT
INNER JOIN      PayLo.dbo.Cards             (NOLOCK) Ca  ON (ECL.CardID IS NOT NULL AND Ca.CardID = ECL.CardID) OR (ECL.CardID IS NULL AND Ca.CardNumberHash = D.[' + @CardHashField + '] COLLATE DATABASE_DEFAULT)  
OPTION (MAXDOP 1)'
EXEC (@SQL)*/
IF @DebugMessages = 1
	PRINT '...from External Card Loads...'
SET @SQL = '
UPDATE D
SET CardID = ECL.CardID
FROM ' + @UseTableName + '             (NOLOCK) D
INNER JOIN PayLo.dbo.ExternalCardLoads (NOLOCK) ECL ON ECL.PanHash = D.[' + @CardHashField + '] COLLATE DATABASE_DEFAULT
OPTION (MAXDOP 1)'
EXEC (@SQL)
IF @DebugMessages = 1
	PRINT '...from Cards...'
SET @SQL = '
UPDATE D
SET CardID = Ca.CardID
FROM ' + @UseTableName + '  (NOLOCK) D
INNER JOIN PayLo.dbo.Cards  (NOLOCK) Ca ON Ca.CardNumberHash = D.[' + @CardHashField + '] COLLATE DATABASE_DEFAULT
OPTION (MAXDOP 1)'
EXEC (@SQL)

IF @DebugMessages = 1
	PRINT 'CardID field updated.'

/*
-- output the mapped data if the table we created is temporary
IF @DropTable = 1
BEGIN
	IF @DebugMessages = 1
		PRINT 'Outputting contents of table ' + @FullTableName + ' and dropping it...'

	IF @DebugMessages = 0
		SET NOCOUNT ON

	SET @SQL = 'SELECT *
	FROM ' + @FullTableName + ' (NOLOCK)
	OPTION (MAXDOP 1)
	
	DROP TABLE ' + @FullTableName
	EXEC (@SQL)
END
*/


SET NOCOUNT OFF
IF @existingTranCount = 0
	COMMIT TRANSACTION

IF @DebugMessages = 1
	PRINT 'End of stored procedure sp_ImportCardHashData.'
RETURN 0
