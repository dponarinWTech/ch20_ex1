USE [Kofax_FormInfo]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ===============================================================================================
-- Author:      Moses Mwangi
-- Create date: 08/01/2018
-- Description:	Update "FormIDs_To_Process" table 
--					Status = "MISSING";
--					Reason = "Kofax Index Database has the document but it is Missing in OnBase"
-- Execution:	Will be (called by) C# application during nightly process
-- ===============================================================================================
ALTER PROCEDURE [dbo].[KfxIndxRconOB_UpdtRecScannedButMissingInOnBase]
(	
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
	SET @ProcessName 			= 'KfxIndxRconOB_UpdtRecScannedButMissingInOnBase';
	SET @SysError				= 0;
	SET @SysRowCount			= 0;
-- -----------------------------------------------------------------------------
	SET @LogDetails = 'Starting to update records in "FormIDs_To_Process" table for records with UniqueID is missing in OnBase';
	SET @ExecutionParameters = NULL;
	SET @ErrorStatus = 0;
	
	INSERT INTO ExecutionHistory (ProcessName,ExecutionParameters,ExecutionDateTime,LogDetails,ErrorStatus)
	VALUES (@ProcessName,@ExecutionParameters,GETDATE(),@LogDetails,@ErrorStatus)
	-- ------------------
	
	BEGIN TRANSACTION UpdtRecScannedButMissingInOnBase
	
		-- Start "UPDATE" process		
    UPDATE FormIDs_To_Process 
    SET UpdateDate = GETDATE(),
        UpdateBy = 'SYSTEM',
        [Status] = 'MISSING-PROCESSED-T', 
        Reason = 'Kofax Index Database has the document but it is Missing in OnBase'
    WHERE NOT EXISTS ( SELECT *
                       FROM [dbo].[OnBase_MemdocRecords] 
                       WHERE  UIDNumber = spstr3)
          AND ScanDate IS NOT NULL
          AND [Status] IS NULL
		-- ------------------
		
		-- Capture values as it will be reset to zero in next step
		SELECT 	@SysError 	 = @@ERROR,
				@SysRowCount = @@ROWCOUNT; -- Storing updated rows count 
			
		IF @SysError <> 0
			BEGIN
				ROLLBACK TRANSACTION UpdtRecScannedButMissingInOnBase
				
				SET @ReturnRowCounts 	= 0		-- Return zero row counts
				SET @ReturnResult 		= 0		-- "BAD" result						
				
				RETURN							-- End further processing
			END
			
		ELSE							
			BEGIN
				COMMIT TRANSACTION UpdtRecScannedButMissingInOnBase
				
				SET @ExecutionParameters = NULL;
				SET @LogDetails = 'Successfully updated ' + CONVERT(VARCHAR(9), @SysRowCount) + ' records in "FormIDs_To_Process" table with "Status = MISSING" and "Reason = Kofax Index Database has Scan Date but Data is Missing in OnBase"';
				SET @ErrorStatus = 0;

				INSERT INTO ExecutionHistory (ProcessName,ExecutionParameters,ExecutionDateTime,LogDetails,ErrorStatus)
				VALUES (@ProcessName,@ExecutionParameters,GETDATE(),@LogDetails,@ErrorStatus)

				SET @ReturnRowCounts 	= @SysRowCount		-- Retrun selected row counts
				SET @ReturnResult 		= 1					-- "GOOD" result
			END
	;		--	END TRANSACTION UpdtRecScannedButMissingInOnBase
-- -------------------------------------------------------------------------------
END;  -- End stored procedure "KfxIndxRconOB_UpdtRecScannedButMissingInOnBase"