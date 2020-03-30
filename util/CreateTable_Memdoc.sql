USE [OnBaseHelper]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[MemdocRecords](
	[SpStr3] [varchar](20) NOT NULL,
	[DocDate] [datetime] NULL,
	[Account] [varchar](20) NULL,
	[SSN] [varchar](11) NULL
) ON [PRIMARY]
GO