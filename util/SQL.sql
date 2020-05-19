USE [MEMBERID]
GO
/* Custom_Synch_HR_MemID_Update_MemberInfo_EmpNos */

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'Custom_Synch_HR_MemID_Update_MemberInfo_EmpNos')
DROP PROCEDURE Custom_Synch_HR_MemID_Update_MemberInfo_EmpNos
GO

CREATE PROCEDURE [dbo].Custom_Synch_HR_MemID_Update_MemberInfo_EmpNos

AS
BEGIN
	SET ANSI_WARNINGS OFF 

	BEGIN TRANSACTION

	DECLARE @Err		Int

	-- Update Incorrect EmpNos on MemberInfo Table
	UPDATE MemberInformation
	SET EMPLOYEENUMBER = e.EMPNO,
	EMPLOYEENETWORKID=SUSER_NAME()
	FROM MemberInformation m
	JOIN Update_EmpNo e on m.SSN=e.SSN
	WHERE (m.EMPLOYEENUMBER <> e.EMPNO 
	or m.EMPLOYEENUMBER is null)
	-- Backout everything if error
	SELECT @err = @@error IF @err <> 0 BEGIN ROLLBACK TRANSACTION RETURN @err END

	-- Update EmpNos that should be null on MemberInfo Table
	UPDATE MemberInformation
	SET EMPLOYEENUMBER = NULL,EMPLOYEENETWORKID=SUSER_NAME()
	FROM MemberInformation m
	LEFT OUTER JOIN Update_EmpNo e on m.SSN=e.SSN
	WHERE e.SSN is null and m.EMPLOYEENUMBER is not null and m.EMPLOYEENUMBER < 30000

	-- Backout everything if error
	SELECT @err = @@error IF @err <> 0 BEGIN ROLLBACK TRANSACTION RETURN @err END

	-- Backout everything if duplicate Employee Numbers found
	IF EXISTS(SELECT EMPLOYEENUMBER, Count(EMPLOYEENUMBER)  from MemberInformation
					 GROUP BY EMPLOYEENUMBER
					 HAVING Count(EMPLOYEENUMBER) > 1 and EMPLOYEENUMBER < 30000)
	BEGIN
		RAISERROR('ERROR: Duplicate Employee Numbers Found on MemberInformation', 16, 1)
		ROLLBACK TRANSACTION
		RETURN @err
	END

	COMMIT TRANSACTION

	--At the end of the process, truncate the Update_EmpNo table and raise any errors 
	TRUNCATE TABLE Update_EmpNo
	if(@@ERROR <> 0)
	BEGIN
		RAISERROR('ERROR: Application Failed to Truncate Update_EmpNo Table ', 16,1)
		return
	END 
END
GO

/* ENDCustom_Synch_HR_MemID_Update_MemberInfo_EmpNos */

/* EmpData View */
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.views WHERE name = 'EmpData')
DROP VIEW EmpData
GO

CREATE VIEW EmpData
AS

Select EMPLOYEENUMBER, FIRSTNAME, LASTNAME, PHOTO 
from MemberInformation
where EMPLOYEENUMBER IS NOT NULL
GO
