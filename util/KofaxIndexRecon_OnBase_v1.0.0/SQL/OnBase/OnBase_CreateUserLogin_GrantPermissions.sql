USE [master]

DECLARE @KFX_Login varchar(40);
DECLARE @KFX_ServiceAccount varchar(40);
DECLARE @sqlstmt varchar(200);

--------------------------------------------
-- Update @KFX_Login and  @KFX_ServiceAccount
--          for target environment:
---> for SIT  = SKFX 
---> for UAT  = UKFX 
---> for PROD = PKFX
--------------------------------------------
SET @KFX_Login          = 'NCSECU\svc-SKFX-process'
SET @KFX_ServiceAccount = 'NCSECU\svc-SKFX-process'

IF  NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = @KFX_Login)
BEGIN
    SET @sqlstmt = 'CREATE LOGIN [' + @KFX_Login + '] FROM WINDOWS;';
	PRINT @sqlstmt;
	EXEC(@sqlstmt);
END


Use [OnBase]

IF NOT EXISTS(SELECT * FROM sys.database_principals WHERE NAME = @KFX_ServiceAccount)
BEGIN
	SET @sqlstmt = 'CREATE USER [' + @KFX_ServiceAccount + ']  FOR LOGIN [' + @KFX_Login + '] WITH DEFAULT_SCHEMA=[hsi]';
	PRINT @sqlstmt;
	EXEC(@sqlstmt);
END

SET @sqlstmt = 'EXEC sp_addrolemember ''db_datareader'', [' + @KFX_ServiceAccount + '] ;';
PRINT @sqlstmt;
EXEC(@sqlstmt);
GO
