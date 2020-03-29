USE [Kofax_FormInfo]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ===============================================================================================
-- Author:      Yogesh Sanghvi
-- Create date: 08/01/2018
-- Description:	Update table "FormInfo" with information from table "FormIDs_To_Process"
--				
-- Execution:	Will be (called by) C# application during nightly process
-- ===============================================================================================
ALTER PROCEDURE [dbo].[KfxIndxRconOB_UpdateFormInfoTable]
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
	SET @ProcessName 			= 'KfxIndxRconOB_UpdateFormInfoTable';
	SET @SysError				= 0;
	SET @SysRowCount			= 0;
-- -----------------------------------------------------------------------------
	SET @LogDetails = 'Starting to update desired records in "FormInfo" table with info from "FormIDs_To_Process"';
	SET @ExecutionParameters = NULL;
	SET @ErrorStatus = 0;
	
	INSERT INTO ExecutionHistory (ProcessName,ExecutionParameters,ExecutionDateTime,LogDetails,ErrorStatus)
	VALUES (@ProcessName,@ExecutionParameters,GETDATE(),@LogDetails,@ErrorStatus)
	-- ------------
	
	BEGIN TRANSACTION Kofax_FormInfo
		UPDATE FormInfo
		SET UpdateDate = FormIDs_To_Process.UpdateDate, 
			UpdateBy = 'ONBASE',
			[Status] = FormIDs_To_Process.[Status], 
			Reason = FormIDs_To_Process.Reason
		FROM FormInfo, FormIDs_To_Process
		WHERE FormInfo.id = FormIDs_To_Process.[UID]
 		-- ------------
 
		-- Capture values as it will be reset to zero in next step
		SELECT 	@SysError 	 = @@ERROR,
				@SysRowCount = @@ROWCOUNT;
		
		IF @SysError <> 0
			BEGIN
				ROLLBACK TRANSACTION UpdateFormInfo
				
				SET @ReturnRowCounts 	= 0		-- Return zero row counts
				SET @ReturnResult 		= 0		-- "BAD" result		
				
				RETURN							-- End further processing
			END
		ELSE
			BEGIN
				COMMIT TRANSACTION UpdateFormInfo
				
				SET @ExecutionParameters = NULL;
				SET @LogDetails = 'Successfully updated ' + CONVERT(VARCHAR(9), @SysRowCount) + ' records in "FormInfo" table from "FormIDs_To_Process" table';
				SET @ErrorStatus = 0;

				INSERT INTO ExecutionHistory (ProcessName,ExecutionParameters,ExecutionDateTime,LogDetails,ErrorStatus)
				VALUES (@ProcessName,@ExecutionParameters,GETDATE(),@LogDetails,@ErrorStatus)

				SET @ReturnRowCounts 	= @SysRowCount		-- Retrun selected row counts
				SET @ReturnResult 		= 1					-- "GOOD" result						
			END	
	;		--	END TRANSACTION UpdateFormInfo
-- -------------------------------------------------------------------------------
END;  -- End stored procedure "KfxIndxRconOB_UpdateFormInfoTable"