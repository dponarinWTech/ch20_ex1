USE [Kofax_FormInfo]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ===============================================================================================
-- Author:      Yogesh Sanghvi
-- Create date: 08/01/2018
-- Description:	Select records to be processed from "Kofax_FormInfo" table
-- Execution:	Will be (called by) C# application during nightly process
-- ===============================================================================================
ALTER PROCEDURE [dbo].[KfxIndxRconOB_SelectRecFromKofaxInfo]
(		
	@DayDifference		INT,
	@ReturnRowCounts	INT OUT,
	@ReturnResult		BIT OUT
) 
AS
BEGIN	
	DECLARE @ErrorStatus			BIT;
	DECLARE @ExecutionParameters	VARCHAR(100);
	DECLARE @LogDetails 			VARCHAR(250);
	DECLARE @ProcessName			VARCHAR(50);
	DECLARE @SysError				INT;
	DECLARE @SysRowCount			INT;
	
	SET @ErrorStatus			= 0;
	SET @ExecutionParameters 	= NULL;
	SET @LogDetails 			= NULL;
	SET @ProcessName 			= 'KfxIndxRconOB_SelectRecFromKofaxInfo';
	SET @SysError				= 0;
	SET @SysRowCount			= 0;
-- -----------------------------------------------------------------------------
	SET @LogDetails = 'Starting to select desired records to process from table "FormInfo" into table "FormIDs_To_Process"';
	SET @ExecutionParameters = 'Number of previous days to serach = ' + CONVERT(VARCHAR(9), @DayDifference);
	SET @ErrorStatus = 0;
	
	INSERT INTO ExecutionHistory (ProcessName,ExecutionParameters,ExecutionDateTime,LogDetails,ErrorStatus)
	VALUES (@ProcessName,@ExecutionParameters,GETDATE(),@LogDetails,@ErrorStatus)
		
	BEGIN TRANSACTION SelectDesiredRecords
		-- NOTE: The "Outer" option will parse SSN and Account from XML creating seprate row for each SSN/Account pair
		INSERT INTO FormIDs_To_Process (UIDNumber,UID,CreateDate,UpdateDate,UpdateBy,ScanDate,Status,Reason,SSN,Account)
		SELECT f.FormType + RIGHT(REPLICATE('0', 8) + CONVERT(varChar(8), f.Id), 8) AS UniqueID,  
				f.Id, f.CreateDate, f.UpdateDate, f.UpdateBy, f.Scandate, f.Status, f.Reason,
				x.value('.','varchar(9)') AS SSN, 
				y.value('.','varchar(20)') AS Account
		FROM [FormInfo] AS f
			OUTER APPLY f.InfoXml.nodes('/form/ssnlist/ssn') a(x)
			OUTER APPLY f.InfoXml.nodes('/form/accountlist/account') b(y)
		WHERE 	(ScanDate IS NOT NULL) 
			AND (
					(Status='MISSING'  OR  Status='Not Found') OR
					(Status='PARTIAL' AND UpdateBy='ONBASE') OR
					(Status IS NULL  AND  DATEDIFF(DAY, ScanDate, GETDATE()) > @DayDifference)
				)
		ORDER BY Id, SSN, Account			
		-- ---------------
		
		-- Capture values as it will be reset to zero in next step
		SELECT 	@SysError 	 = @@ERROR,
				@SysRowCount = @@ROWCOUNT;
		
		IF @SysError <> 0
			BEGIN
				ROLLBACK TRANSACTION SelectDesiredRecords
				
				SET @ReturnRowCounts 	= 0		-- Return zero row counts
				SET @ReturnResult 		= 0		-- "BAD" result			
				
				RETURN							-- End further processing
			END
		ELSE
			BEGIN
				COMMIT TRANSACTION SelectDesiredRecords
				
				SET @ExecutionParameters = NULL;
				SET @ReturnRowCounts 	= @SysRowCount		-- Retrun selected row counts
				SET @LogDetails = 'Successfully selected ' + CONVERT(VARCHAR(9), @ReturnRowCounts) + 'records to process';
				SET @ErrorStatus = 0;

				INSERT INTO ExecutionHistory (ProcessName,ExecutionParameters,ExecutionDateTime,LogDetails,ErrorStatus)
				VALUES (@ProcessName,@ExecutionParameters,GETDATE(),@LogDetails,@ErrorStatus)
				
				SET @ReturnResult = 1		-- "GOOD" result	
			-- END "SELECT" process						
			END
;		--	END TRANSACTION SelectDesiredRecords
-- -------------------------------------------------------------------------------
END;  -- End stored procedure "KfxIndxRconOB_SelectRecFromKofaxInfo"