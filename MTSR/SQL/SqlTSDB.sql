CREATE PROCEDURE [Flush] @table VARCHAR(128), @representation [dbo].[Representation] READONLY
AS
BEGIN
    DECLARE @cmd NVARCHAR(MAX);

    IF (OBJECT_ID(@table) IS NULL)
    BEGIN
        SET @cmd = N'SELECT * INTO [' + @table + '] FROM @representation';
    END
    ELSE
    BEGIN
        SET @cmd = N'INSERT [' + @table + '] SELECT * FROM @representation';
    END;

    EXEC sp_executesql @cmd, N'@representation [dbo].[Representation] READONLY', @representation = @representation;
END;
GO
