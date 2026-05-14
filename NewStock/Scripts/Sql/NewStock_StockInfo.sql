-- NewStock: dbo.StockInfo (FinMind TaiwanStockInfo-aligned; no stored API date).
-- StockId: SMALLINT PK = numeric ticker (e.g. stock_id '2330' -> 2330; '0050' -> 50).
-- All columns NOT NULL (use empty string N'' where FinMind omits optional text).
-- If an old StockInfo exists, backup then DROP TABLE before re-running.

SET NOCOUNT ON;

IF DB_ID(N'NewStock') IS NULL
BEGIN
    DECLARE @sql nvarchar(max) =
        N'CREATE DATABASE [' + REPLACE(N'NewStock', N']', N']]') + N']';
    EXEC (@sql);
END;
GO

USE [NewStock];
GO

IF OBJECT_ID(N'dbo.StockInfo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockInfo
    (
        StockId           SMALLINT        NOT NULL,
        StockName         NVARCHAR(128)   NOT NULL,
        IndustryCategory  NVARCHAR(256)   NOT NULL,
        MarketType        NVARCHAR(32)    NOT NULL,
        CONSTRAINT PK_StockInfo PRIMARY KEY CLUSTERED (StockId)
    );
END;
GO
