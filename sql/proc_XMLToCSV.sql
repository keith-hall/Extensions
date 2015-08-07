CREATE PROCEDURE proc_XMLToCSV
	@XML NVARCHAR(MAX),
	@Separator NVARCHAR(100) = ';',
	@CSV NVARCHAR(MAX) OUTPUT
AS

SET NOCOUNT ON
DECLARE @Done BIT = 0
DECLARE @HeaderRow TABLE (ColumnNumber INT IDENTITY(1, 1), Header SYSNAME)
DECLARE @RootElement SYSNAME
DECLARE @Pos INT = 0
DECLARE @ContentStartPos INT
DECLARE @State BIT = 0
DECLARE @ColumnNumber SMALLINT = 0

WHILE @Done = 0
BEGIN
	SET @Pos = CHARINDEX('<', @XML, @Pos)
	IF @Pos = 0
		SET @Done = 1
	ELSE
	BEGIN
		DECLARE @EndPos INT = CHARINDEX('>', @XML, @Pos)
		DECLARE @Header SYSNAME = SUBSTRING(@XML, @Pos + 1, @EndPos - @Pos - 1)
		
		SET @State = CASE WHEN SUBSTRING(@Header, 1, 1) = '/' THEN 0 ELSE 1 END
		IF @State = 1
		BEGIN
			IF @Pos = 1
				SET @RootElement = @Header
			ELSE IF @Header = @RootElement
			BEGIN
				SET @CSV = @CSV + CHAR(13) + CHAR(10)
				SET @ColumnNumber = 0
			END
			ELSE
			BEGIN
				SET @ColumnNumber = @ColumnNumber + 1
				IF NOT EXISTS (SELECT TOP 1 * FROM @HeaderRow WHERE Header = @Header)
					INSERT INTO @HeaderRow (Header) VALUES (@Header)
			END
			SET @ContentStartPos = @EndPos
		END
		ELSE
		BEGIN
			IF @Header != '/' + @RootElement
			BEGIN
				-- TODO: if value contains separator, escape it
				SET @CSV = @CSV + CASE WHEN @ColumnNumber = 1 THEN '' ELSE @Separator END + SUBSTRING(@XML, @ContentStartPos + 1, @Pos - @ContentStartPos - 1)
				SET @ContentStartPos = @EndPos
			END
		END
		SET @Pos = @EndPos
	END
END
SET @CSV = (
	SELECT CASE WHEN ColumnNumber = 1 THEN '' ELSE @Separator END + Header AS [text()]
	FROM @HeaderRow AS hr
	FOR XML PATH ('')
) + CHAR(13) + CHAR(10) + @CSV
