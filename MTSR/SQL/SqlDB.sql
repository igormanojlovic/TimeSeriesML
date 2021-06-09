IF (OBJECT_ID('CreateDatabase') IS NOT NULL)
BEGIN
    DROP PROCEDURE [CreateDatabase];
END;
GO

CREATE PROCEDURE [CreateDatabase] @name VARCHAR(128), @recreate BIT
AS
BEGIN
    DECLARE @cmd NVARCHAR(MAX);

    IF (@recreate = 1 AND EXISTS (SELECT TOP 1 1 FROM SYSDATABASES WHERE NAME = @name))
    BEGIN
        SET @cmd = N'ALTER DATABASE [' + @name + '] SET SINGLE_USER WITH ROLLBACK IMMEDIATE DROP DATABASE [' + @name + ']';
        EXEC(@cmd);
    END;

    IF NOT EXISTS (SELECT TOP 1 1 FROM SYSDATABASES WHERE NAME = @name)
    BEGIN
        DECLARE @folder VARCHAR(MAX) = (SELECT CONVERT(NVARCHAR(MAX), SERVERPROPERTY('InstanceDefaultLogPath')));

        SET @cmd = N'
        CREATE DATABASE [' + @name + ']
            ON PRIMARY (NAME = N''' + @name + ''', FILENAME = N''' + @folder + @name + '.mdf'', SIZE = 1GB, MAXSIZE = UNLIMITED, FILEGROWTH = 1GB)
             , FILEGROUP [' + @name + '_FG] CONTAINS MEMORY_OPTIMIZED_DATA
               DEFAULT (NAME = N''' + @name + '_InMemory'', FILENAME = N''' + @folder + @name + '_InMemory'', MAXSIZE = UNLIMITED)
           LOG ON (NAME = N''' + @name + '_log'', FILENAME = N''' + @folder + @name + '_log.ldf'', SIZE = 1GB, MAXSIZE = 10GB, FILEGROWTH = 1GB)
        ';

        EXEC(@cmd);
    END;

    IF (OBJECT_ID('tempdb..#CMD') IS NOT NULL)
    BEGIN
        DROP TABLE #CMD;
    END;

    CREATE TABLE #CMD ([Value] VARCHAR(MAX));

    SET @cmd = N'INSERT #CMD
    SELECT ''EXEC [' + @name + '].[sys].[sp_executesql] N''''DROP PROCEDURE ['' + [name] + '']''''''
      FROM [' + @name + '].[dbo].[sysobjects]
     WHERE [type] = ''P''
     UNION ALL
    SELECT ''EXEC [' + @name + '].[sys].[sp_executesql] N''''DROP FUNCTION ['' + [name] + '']''''''
      FROM [' + @name + '].[dbo].[sysobjects]
     WHERE [type] IN (''FN'', ''IF'', ''TF'')
     UNION ALL
    SELECT ''EXEC [' + @name + '].[sys].[sp_executesql] N''''DROP VIEW ['' + [name] + '']''''''
      FROM [' + @name + '].[dbo].[sysobjects]
     WHERE [type] = ''V''
     UNION ALL
    SELECT ''EXEC [' + @name + '].[sys].[sp_executesql] N''''DROP TYPE ['' + [name] + '']''''''
      FROM [' + @name + '].[sys].[table_types]';
    EXEC(@cmd);

    DECLARE CMD CURSOR LOCAL FAST_FORWARD FOR
     SELECT * FROM #CMD;

    OPEN CMD;
    FETCH NEXT FROM CMD INTO @cmd;
    WHILE (@@FETCH_STATUS = 0)
    BEGIN
        EXEC(@cmd);
        FETCH NEXT FROM CMD INTO @cmd;
    END;
    CLOSE CMD;
    DEALLOCATE CMD;
END;
GO
