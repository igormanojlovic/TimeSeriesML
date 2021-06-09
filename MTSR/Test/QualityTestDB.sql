CREATE TYPE [Dates] AS TABLE ([Date] DATE)
GO

CREATE PROCEDURE [SetTSDates] @id INT, @dates [Dates] READONLY
AS
BEGIN
    IF (OBJECT_ID('TSDates') IS NULL)
    BEGIN
        CREATE TABLE [TSDates] ([Id] INT NOT NULL, [Date] DATE NOT NULL);
    END;

    DELETE FROM [TSDates] WHERE [Id] = @id; 
    INSERT [TSDates] SELECT @id, [Date] FROM @dates;
END;
GO

CREATE PROCEDURE [dbo].[GetANDLPs] @table VARCHAR(128), @resolution INT, @holidays [Dates] READONLY AS
BEGIN
    SET NOCOUNT ON;
    SET ANSI_NULLS OFF;
    SET ANSI_WARNINGS OFF;
    IF (OBJECT_ID(@table) IS NULL)
    BEGIN
        RETURN;
    END;
    IF (OBJECT_ID('tempdb..#DLP') IS NOT NULL)
    BEGIN
        DROP TABLE #DLP;
    END;
    IF (OBJECT_ID('tempdb..#ADLP') IS NOT NULL)
    BEGIN
        DROP TABLE #ADLP;
    END;
    IF (OBJECT_ID('tempdb..#ANDLP') IS NOT NULL)
    BEGIN
        DROP TABLE #ANDLP;
    END;

    CREATE TABLE #DLP
    (
        [Id]                   INT NOT NULL,
        [CharacteristicPeriod] INT NOT NULL,
        [CharacteristicDay]    INT NOT NULL,
        [TimeOfDayIndex]       INT NOT NULL,
        [Average]            FLOAT     NULL,
        [StandardDeviation]  FLOAT     NULL
    );

    CREATE NONCLUSTERED INDEX NCI_DLP ON #DLP ([Id], [CharacteristicPeriod], [CharacteristicDay], [TimeOfDayIndex]);

    DECLARE @cmd NVARCHAR(MAX) = N'
     INSERT #DLP
     SELECT s.[Id]
          , [CharacteristicPeriod] = DATEPART(MONTH, [Timestamp])
          , [CharacteristicDay]    = CASE WHEN h.[Date] IS NOT NULL
                                         THEN 2 -- Holiday
                                         WHEN DATEPART(WEEKDAY, [Timestamp]) IN (1, 7)
                                         THEN 1 -- Weekend
                                         ELSE 0 -- Workday
                                    END
          , [TimeOfDayIndex]      = (DATEPART(HOUR, [Timestamp]) * 60 + DATEPART(MINUTE, [Timestamp]))/@resolution
          , s.[Average]
          , s.[StandardDeviation]
       FROM [' + @table + '] s
      INNER JOIN [TSDates] d
         ON s.[Id] = d.[Id]
        AND CAST(s.[Timestamp] AS DATE) = d.[Date]
       LEFT OUTER JOIN @holidays h
         ON CAST(s.[Timestamp] AS DATE) = h.[Date]';
       EXEC [sp_executesql] @cmd, N'@resolution INT, @holidays [Dates] READONLY', @resolution = @resolution, @holidays = @holidays;

    WITH AverageDLP1
      AS (SELECT [Id]
               , [CharacteristicPeriod]
               , [CharacteristicDay]
               , [TimeOfDayIndex]
               , [Average] = AVG([Average])
            FROM #DLP
           GROUP BY [Id]
                  , [CharacteristicPeriod]
                  , [CharacteristicDay]
                  , [TimeOfDayIndex])
       , AverageDLP2
      AS (SELECT adlp.[Id]
               , adlp.[CharacteristicPeriod]
               , adlp.[CharacteristicDay]
               , adlp.[TimeOfDayIndex]
               , adlp.[Average]
               , [StandardDeviation] = SQRT(  SUM(POWER(source.[Average] - adlp.[Average], 2))  +  SUM(POWER(source.[StandardDeviation], 2))  ) / COUNT(*)
            FROM #DLP source
           INNER JOIN AverageDLP1 adlp
              ON source.[Id]                   = adlp.[Id]
             AND source.[CharacteristicPeriod] = adlp.[CharacteristicPeriod]
             AND source.[CharacteristicDay]    = adlp.[CharacteristicDay]
             AND source.[TimeOfDayIndex]       = adlp.[TimeOfDayIndex]
           GROUP BY adlp.[Id]
                  , adlp.[CharacteristicPeriod]
                  , adlp.[CharacteristicDay]
                  , adlp.[TimeOfDayIndex]
                  , adlp.[Average])
    SELECT [Id]
         , [CharacteristicPeriod]
         , [CharacteristicDay]
         , [TimeOfDayIndex]
         , [Average]
         , [Minimum] = [Average] - [StandardDeviation]
         , [Maximum] = [Average] + [StandardDeviation]
      INTO #ADLP
      FROM AverageDLP2;

    DECLARE @tsCount INT = (SELECT COUNT(DISTINCT [Id]) FROM #ADLP);

    SELECT [CharacteristicPeriod]
         , [CharacteristicDay]
         , [TimeOfDayIndex]
         , [Count] = COUNT([Id])
      INTO #Unexpected
      FROM #ADLP
     GROUP BY [CharacteristicPeriod]
            , [CharacteristicDay]
            , [TimeOfDayIndex]
    HAVING COUNT([Id]) < @tsCount;

    DELETE a
      FROM #ADLP a
     INNER JOIN #Unexpected u
        ON a.[CharacteristicPeriod] = u.[CharacteristicPeriod]
       AND a.[CharacteristicDay]    = u.[CharacteristicDay]
       AND a.[TimeOfDayIndex]       = u.[TimeOfDayIndex];

    SET NOCOUNT OFF;
    WITH ScalingFactors
      AS (SELECT [Id]
               , [TotalAVG] = AVG([Average])
            FROM #ADLP
           GROUP BY [Id])
    SELECT p.[Id]
         , p.[CharacteristicPeriod]
         , p.[CharacteristicDay]
         , p.[TimeOfDayIndex]
         , [Average] = CASE WHEN f.[TotalAVG] = 0 THEN p.[Average] ELSE p.[Average] / f.[TotalAVG] END
         , [Minimum] = CASE WHEN f.[TotalAVG] = 0 THEN p.[Minimum] ELSE p.[Minimum] / f.[TotalAVG] END
         , [Maximum] = CASE WHEN f.[TotalAVG] = 0 THEN p.[Maximum] ELSE p.[Maximum] / f.[TotalAVG] END
      FROM #ADLP p
     INNER JOIN ScalingFactors f
        ON p.[Id] = f.[Id]
     ORDER BY (1), (2), (3), (4);
END;
GO
