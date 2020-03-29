USE [OnBaseHelper]
GO

DECLARE @KFX_Login varchar(40);
DECLARE @KFX_ServiceAccount varchar(40);
DECLARE @sqlstmt varchar(200);

-- NOTE: Update @KFX_Login  AND  @KFX_ServiceAccount for target environment
SET @KFX_Login = 'DEVNCSECU\svc-dkfx-process';
SET @KFX_ServiceAccount = 'DEVNCSECU\svc-dkfx-process';

IF  NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = @KFX_Login)
BEGIN
    SET @sqlstmt = 'CREATE LOGIN [' + @KFX_Login + '] FROM WINDOWS WITH DEFAULT_DATABASE=[OnBaseHelper], DEFAULT_LANGUAGE=[us_english]';
	PRINT @sqlstmt;
	EXEC(@sqlstmt);
END

IF NOT EXISTS(SELECT * FROM sys.database_principals WHERE NAME = @KFX_ServiceAccount)
BEGIN
	SET @sqlstmt = 'CREATE USER [' + @KFX_ServiceAccount + ']  FOR LOGIN [' + @KFX_Login + '] WITH DEFAULT_SCHEMA=[dbo]';
	PRINT @sqlstmt;
	EXEC(@sqlstmt);
END
GO
