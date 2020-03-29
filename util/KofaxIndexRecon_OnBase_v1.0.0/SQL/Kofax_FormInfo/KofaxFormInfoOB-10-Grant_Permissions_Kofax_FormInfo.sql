USE [Kofax_FormInfo]
GO

-- NOTE: Update "Domain"  AND  "UserID" for target environment
-- These changes are valid for following applications - 
--	1 - Kofax Index Recon - Nightly and weekly process
--  2 - Kofax Index Recon OnBase [future]
--  2 - Kofax Margo Branch Scan Report [future]
-- 	3 - Kofax Index Recon User Interface (UI)
--  4 - Kofax UID Batch class

DECLARE @KFX_Login varchar(40);
DECLARE @KFX_ServiceAccount varchar(40);

-- NOTE: Update @KFX_Login  AND  @KFX_ServiceAccount for target environment
SET @KFX_Login = 'DEVNCSECU\svc-dkfx-process';
SET @KFX_ServiceAccount = 'DEVNCSECU\svc-dkfx-process';

DECLARE @sqlstmt varchar(200);


IF NOT EXISTS(SELECT * FROM sys.database_principals WHERE NAME = @KFX_ServiceAccount)
BEGIN
	SET @sqlstmt = 'CREATE USER [' + @KFX_ServiceAccount + ']  FOR LOGIN [' + @KFX_Login + '] WITH DEFAULT_SCHEMA=[dbo]';
	PRINT @sqlstmt;
	EXEC(@sqlstmt);
END
GO

-- Permissions for database Tables
SET @sqlstmt =
    ' GRANT SELECT, UPDATE, DELETE, INSERT, ALTER ON dbo.FormInfo TO [' + @KFX_ServiceAccount + '];' +
    ' GRANT SELECT, UPDATE, DELETE, INSERT, ALTER ON dbo.ExecutionHistory TO [' + @KFX_ServiceAccount + '];' +
    ' GRANT SELECT, UPDATE, DELETE, INSERT, ALTER ON dbo.FormIDs_To_Process TO [' + @KFX_ServiceAccount + '];' +
    ' GRANT SELECT, UPDATE, DELETE, INSERT, ALTER ON dbo.Tower_MemdocRecords TO [' + @KFX_ServiceAccount + '];' ;
PRINT @sqlstmt;
EXEC(@sqlstmt);

-- Permissions for database stored procedures
SET @sqlstmt =
    ' GRANT EXECUTE ON GetFormInfo TO [' + @KFX_ServiceAccount + '];' +
    ' GRANT EXECUTE ON InsertXmlIntoFormInfo TO [' + @KFX_ServiceAccount + '];' +
    ' GRANT EXECUTE ON KfxIndxRconOB_SelectRecFromKofaxInfo TO [' + @KFX_ServiceAccount + '];' +
    ' GRANT EXECUTE ON KfxIndxRconOB_UpdtRecScannedButMissingInOnBase TO [' + @KFX_ServiceAccount + '];' +
    ' GRANT EXECUTE ON KfxIndxRconOB_UpdateFormInfoTable TO [' + @KFX_ServiceAccount + '];' +
    ' GRANT EXECUTE ON KfxIndxRcon_UpdtRecNotScanned TO [' + @KFX_ServiceAccount + '];' +
    ' GRANT EXECUTE ON KfxIndxRconOB_TruncateTables TO [' + @KFX_ServiceAccount + '];' ;    
PRINT @sqlstmt;
EXEC(@sqlstmt);

GO


