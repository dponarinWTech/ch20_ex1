USE [Kofax_FormInfo]
GO

-- ========================================================================================================================
-- Description: Create dummy script for initial set up
--				It will create dummy scripts for Stored procedures 
--				Later actual scripts will replace this dummy procedures with correct procedures 
--				Benefit - Developers can create scripts as "ALTER" instead of "CREATE". 
--							AS "ALTER" set up does NOT deletes existing procedures, this arrangement will help 
--							DBAs to maintain granted permissions to all database stored procedures
-- -------------------------------------------------------------------------------------------------
-- Special Instructions: Run ONLY ONCE in each environment during initial set up in target database	      
-- ========================================================================================================================

-- keep this SP in KofaxIndexRecon_OnBase installation package until Tower is not decommissioned
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[KfxIndxRcon_SelectRecFromKofaxInfo]') AND type in (N'P', N'PC'))
    EXEC('CREATE PROCEDURE dbo.KfxIndxRcon_SelectRecFromKofaxInfo AS RETURN')
GO


IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[KfxIndxRconOB_SelectRecFromKofaxInfo]') AND type in (N'P', N'PC'))
    EXEC('CREATE PROCEDURE dbo.KfxIndxRconOB_SelectRecFromKofaxInfo AS RETURN')
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[KfxIndxRconOB_TruncateTables]') AND type in (N'P', N'PC'))
    EXEC('CREATE PROCEDURE dbo.KfxIndxRconOB_TruncateTables AS RETURN')
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[KfxIndxRcon_UpdtRecNotScanned]') AND type in (N'P', N'PC'))
    EXEC('CREATE PROCEDURE dbo.KfxIndxRcon_UpdtRecNotScanned AS RETURN')
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[KfxIndxRconOB_UpdtRecScannedButMissingInOnBase]') AND type in (N'P', N'PC'))
    EXEC('CREATE PROCEDURE dbo.KfxIndxRconOB_UpdtRecScannedButMissingInOnBase AS RETURN')
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[KfxIndxRconOB_UpdateFormInfoTable]') AND type in (N'P', N'PC'))
    EXEC('CREATE PROCEDURE dbo.KfxIndxRconOB_UpdateFormInfoTable AS RETURN')
GO

