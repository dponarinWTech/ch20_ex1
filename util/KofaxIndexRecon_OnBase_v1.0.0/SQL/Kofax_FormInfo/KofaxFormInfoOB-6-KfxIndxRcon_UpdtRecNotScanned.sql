USE [Kofax_FormInfo]
GO
/****** Object:  StoredProcedure [dbo].[KfxIndxRcon_UpdtRecNotScanned]    Script Date: 07/17/2018 11:44:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ===============================================================================================
-- Author:      Yogesh Sanghvi
-- Create date: 08/01/2018
-- Description:	Update records in "FormInfo" table that are not scanned as of today
--					Status = "MISSING";
--					Reason = "ScanDate Empty"
-- Execution:	Will be (called by) C# application during nighly process
-- ===============================================================================================
ALTER PROCEDURE [dbo].[KfxIndxRcon_UpdtRecNotScanned]
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
	SET @ProcessName 			= 'KfxIndxRcon_UpdtRecNotScanned';
	SET @SysError				= 0;
	SET @SysRowCount			= 0;
-- -----------------------------------------------------------------------------
	SET @LogDetails = 'Starting to update not scanned records in "FormInfo" table';
	SET @ExecutionParameters = NULL;
	SET @ErrorStatus = 0;
	
	INSERT INTO ExecutionHistory (ProcessName,ExecutionParameters,ExecutionDateTime,LogDetails,ErrorStatus)
	VALUES (@ProcessName,@ExecutionParameters,GETDATE(),@LogDetails,@ErrorStatus)
	-- ------------
	
	BEGIN TRANSACTION UpdateDesiredRecords
		-- Start "UPDATE" process
		UPDATE FormInfo
		SET UpdateDate = GETDATE(), 
			UpdateBy = 'SYSTEM',
			Status = 'MISSING', 
			Reason = 'ScanDate Empty'
		WHERE ScanDate IS NULL AND  Status IS NULL
		-- ------------

		-- Capture values as it will be reset to zero in next step
		SELECT 	@SysError 	 = @@ERROR,
				@SysRowCount = @@ROWCOUNT;
		
		IF @SysError <> 0
			BEGIN
				ROLLBACK TRANSACTION UpdateDesiredRecords
				
				SET @ReturnRowCounts 	= 0		-- Retrun zero row counts
				SET @ReturnResult 		= 0		-- "BAD" result	
				
				RETURN							-- End further processing
			END
		ELSE
			BEGIN
				COMMIT TRANSACTION UpdateDesiredRecords
				
				SET @ExecutionParameters = NULL;
				SET @LogDetails = 'Successfully updated ' + CONVERT(VARCHAR(9), @SysRowCount) + ' records in "FormInfo" table with "Status = MISSING" and "Reason = ScanDate Empty"';
				SET @ErrorStatus = 0;

				INSERT INTO ExecutionHistory (ProcessName,ExecutionParameters,ExecutionDateTime,LogDetails,ErrorStatus)
				VALUES (@ProcessName,@ExecutionParameters,GETDATE(),@LogDetails,@ErrorStatus)

				SET @ReturnRowCounts 	= @SysRowCount		-- Retrun selected row counts
				SET @ReturnResult 		= 1					-- "GOOD" result						
			END
	;		--	END TRANSACTION UpdateDesiredRecords
-- -------------------------------------------------------------------------------
END;  -- End stored procedure "KfxIndxRcon_UpdtRecNotScanned"