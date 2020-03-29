USE [OnBaseHelper]
GO

IF TYPE_ID(N'[dbo].[list_varchar]') IS NULL
    EXEC('CREATE TYPE dbo.list_varchar AS TABLE (id varchar(20) NOT NULL PRIMARY KEY);')
GO


IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[KfxIndxRconOB_SelectRecFromOnBase]') AND type in (N'P', N'PC'))
    EXEC('CREATE PROCEDURE dbo.KfxIndxRconOB_SelectRecFromOnBase AS RETURN')
GO

