USE [Kofax_FormInfo]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ===============================================================================================
-- Author:      Yogesh Sanghvi
-- Create date: 08/01/2018
-- Description:	Truncate tables to be used for current day process
-- Execution:	Will be (called by) C# application during nightly process
-- ===============================================================================================
ALTER PROCEDURE [dbo].[KfxIndxRconOB_TruncateTables]
(
	@ReturnResult	BIT OUT	
) 
AS
BEGIN	
	DECLARE @ErrorStatus			BIT;
	DECLARE @ExecutionParameters	VARCHAR(100);
	DECLARE @LogDetails 			VARCHAR(250);
	DECLARE @ProcessName			VARCHAR(50);
	DECLARE @SysError				INT;
	
	SET @ErrorStatus			= 0;
	SET @ExecutionParameters 	= NULL;
	SET @LogDetails 			= NULL;
	SET @ProcessName 			= 'KfxIndxRconOB_TruncateTables';
	SET @SysError				= 0;
-- -----------------------------------------------------------------------------
	SET @LogDetails = 'Starting to truncate "FormIDs_To_Process" table"';  
	SET @ExecutionParameters = NULL;
	SET @ErrorStatus = 0;
	
	INSERT INTO ExecutionHistory (ProcessName,ExecutionParameters,ExecutionDateTime,LogDetails,ErrorStatus)
	VALUES (@ProcessName,@ExecutionParameters,GETDATE(),@LogDetails,@ErrorStatus)
	-- ------------
	
	BEGIN TRANSACTION TruncateTables
		
		-- Delete existing records, if any
		TRUNCATE TABLE FormIDs_To_Process
		-- ------------
		
		-- Capture values as it will be reset to zero in next step
		SELECT 	@SysError = @@ERROR
			
		IF @SysError <> 0
			BEGIN
				ROLLBACK TRANSACTION TruncateTables
				
				SET @ReturnResult = 0		-- "BAD" result		
				
				RETURN						-- End further processing
			END
		ELSE
			BEGIN			
				SET @ExecutionParameters = NULL;
				SET @LogDetails = 'Successfully truncated "FormIDs_To_Process" table';
				SET @ErrorStatus = 0;
				
				INSERT INTO ExecutionHistory (ProcessName,ExecutionParameters,ExecutionDateTime,LogDetails,ErrorStatus)
				VALUES (@ProcessName,@ExecutionParameters,GETDATE(),@LogDetails,@ErrorStatus)	
				-- ------------

				WAITFOR DELAY '00:00:00.010';
				
				SET @LogDetails = 'Starting to truncate "OnBase_MemdocRecords" table"';  
				SET @ExecutionParameters = NULL;
				SET @ErrorStatus = 0;
				
				INSERT INTO ExecutionHistory (ProcessName,ExecutionParameters,ExecutionDateTime,LogDetails,ErrorStatus)
				VALUES (@ProcessName,@ExecutionParameters,GETDATE(),@LogDetails,@ErrorStatus)
				-- ------------
				
				-- Delete existing records, if any
				TRUNCATE TABLE OnBase_MemdocRecords
				-- ------------
				
				-- Capture values as it will be reset to zero in next step
				SELECT 	@SysError = @@ERROR
					
				IF @SysError <> 0
					BEGIN
						ROLLBACK TRANSACTION TruncateTables
						
						SET @ReturnResult = 0		-- "BAD" result		
						
						RETURN						-- End further processing
					END
				ELSE
					BEGIN
						COMMIT TRANSACTION TruncateTables
						
						SET @ExecutionParameters = NULL;
						SET @LogDetails = 'Successfully truncated "OnBase_MemdocRecords" table';
						SET @ErrorStatus = 0;

						INSERT INTO ExecutionHistory (ProcessName,ExecutionParameters,ExecutionDateTime,LogDetails,ErrorStatus)
						VALUES (@ProcessName,@ExecutionParameters,GETDATE(),@LogDetails,@ErrorStatus)
						
						SET @ReturnResult = 1		-- "GOOD" result					
					END
			END
	;		--	END TRANSACTION TruncateTables
-- -------------------------------------------------------------------------------
END;  -- End stored procedure "KfxIndxRconOB_TruncateTables"