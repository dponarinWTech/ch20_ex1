USE [Kofax_FormInfo]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

-- ===============================================================================================
-- Author:      Yogesh Sanghvi
-- Create date: 08/01/2018
-- Description:	Table to hold execution activities
-- Execution:	Will be (called by) all other SQL scripts for "MARGO Form Recon" application
-- ===============================================================================================

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ExecutionHistory]') AND type in (N'U'))
DROP TABLE [dbo].[ExecutionHistory]
GO
-- --------------------------------------------------------------------------

CREATE TABLE [dbo].[ExecutionHistory](
	[ProcessName] 			[varchar](50) NOT NULL,
	[ExecutionParameters] 	[varchar](100) NULL,
	[ExecutionDateTime] 	[datetime] NOT NULL,
	[LogDetails] 			[varchar](250) NOT NULL,
	[ErrorStatus] 			[bit] NOT NULL,
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

ALTER TABLE [dbo].[ExecutionHistory] ADD  DEFAULT ((0)) FOR [ErrorStatus]
GO
-- --------------------------------------------------------------------------
			
-- ===============================================================================================
-- Author:      Yogesh Sanghvi
-- Create date: 08/01/2018
-- Description:	Table to hold desired records to process retrieved from "Kofax_FormInfo" database
-- Execution:	Will be (called by) all other SQL scripts for "MARGO Form Recon" application
-- ===============================================================================================

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FormIDs_To_Process]') AND type in (N'U'))
DROP TABLE [dbo].[FormIDs_To_Process]
GO
-- --------------------------------------------------------------------------
-- We have to have Primary Key in this table in order to allow DataAdapter to generate UpdateCommand

CREATE TABLE [dbo].[FormIDs_To_Process](
    [Id] int IDENTITY(1,1),
	[UIDNumber] 	[varchar](12) NOT NULL,
	[UID] 			INT NOT NULL,
	[SSN] 			[varchar](11) NULL,
	[Account] 		[varchar](20) NULL,
	[CreateDate] 	[datetime] NULL,
	[UpdateDate] 	[datetime] NULL,
	[UpdateBy] 		[varchar](10) NULL,
	[ScanDate] 		[datetime] NULL, 
	[Status] 		[varchar](30) NULL,
	[Reason] 		[varchar](500) NULL,
CONSTRAINT [PK_FormIDs_To_Process] PRIMARY KEY CLUSTERED
([Id] ASC)
WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO   

SET ANSI_PADDING OFF
GO
-- -----------------------------------------------------------------------------------------------------



-- ===============================================================================================
-- Author:      Yogesh Sanghvi
-- Create date: 08/01/2018
-- Description:	Table to hold data retrieved from OnBase
-- Execution:	Will be (called by) all other SQL scripts for "MARGO Form Recon" application
-- ===============================================================================================

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OnBase_MemdocRecords]') AND type in (N'U'))
DROP TABLE [dbo].[OnBase_MemdocRecords]
GO
-- --------------------------------------------------------------------------

CREATE TABLE [dbo].[OnBase_MemdocRecords](
	[SpStr3] 	[varchar](20) NOT NULL,
	[DocDate] 	[datetime] NULL,
	[Account] 	[varchar](20) NULL,
	[SSN] 		[varchar](11) NULL
) ON [PRIMARY]
GO   

SET ANSI_PADDING OFF
GO
