USE [Kofax_FormInfo]
GO

-- NOTE: Update "Domain"  AND  "UserID" for target environment
-- These changes are valid for following applications - 
--	1 - Kofax Index Recon - Nightly and weekly process
--  2 - Kofax Margo Branch Scan Report
-- 	3 - Kofax Index Recon User Interface (UI)
--  4 - Kofax UID Batch class

DECLARE @DKFX_Login varchar(40);

-- NOTE: Update @KFX_Login for target environment
SET @DKFX_Login = 'DEVNCSECU\svc-dkfx-process';

DECLARE @sqlstmt varchar(200);

IF  NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = @DKFX_Login)
BEGIN
    SET @sqlstmt = 'CREATE LOGIN [' + @DKFX_Login + '] FROM WINDOWS WITH DEFAULT_DATABASE=[Kofax_FormInfo], DEFAULT_LANGUAGE=[us_english]';
	PRINT @sqlstmt;
	EXEC(@sqlstmt);
END

GO
