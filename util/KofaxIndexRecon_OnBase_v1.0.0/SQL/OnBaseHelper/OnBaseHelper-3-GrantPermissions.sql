USE [OnBaseHelper]
GO

DECLARE @KFX_Login varchar(40);
DECLARE @KFX_ServiceAccount varchar(40);
DECLARE @sqlstmt varchar(200);

-- NOTE: Update @KFX_Login  AND  @KFX_ServiceAccount for target environment
SET @KFX_Login = 'DEVNCSECU\svc-dkfx-process';
SET @KFX_ServiceAccount = 'DEVNCSECU\svc-dkfx-process';


SET @sqlstmt =
    ' GRANT EXECUTE ON TYPE::dbo.list_varchar TO [' + @KFX_ServiceAccount + ']; ' + 
	' GRANT EXECUTE ON KfxIndxRconOB_SelectRecFromOnBase TO [' + @KFX_ServiceAccount + ']; ' 
PRINT @sqlstmt;
EXEC(@sqlstmt);
	
GO