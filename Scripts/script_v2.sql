-- script_v2.sql
-- Normalize tracking schema; INT IDENTITY primary keys

USE ExSignAnalytics;
GO

IF OBJECT_ID(N'dbo.TrackingEvents', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.TrackingEvents;
END
GO

IF OBJECT_ID(N'dbo.Clicks', N'U') IS NOT NULL
    DROP TABLE dbo.Clicks;
GO

IF OBJECT_ID(N'dbo.Opens', N'U') IS NOT NULL
    DROP TABLE dbo.Opens;
GO

IF OBJECT_ID(N'dbo.Trackings', N'U') IS NOT NULL
    DROP TABLE dbo.Trackings;
GO

CREATE TABLE dbo.Trackings
(
    Id              INT            NOT NULL IDENTITY(1,1) CONSTRAINT PK_Trackings PRIMARY KEY,
    TrackingKey     NVARCHAR(200)  NOT NULL,
    SenderEmail     NVARCHAR(320)  NOT NULL CONSTRAINT DF_Trackings_SenderEmail DEFAULT (N''),
    RecipientEmail  NVARCHAR(320)  NULL,
    CreatedAt       DATETIME2(3)   NOT NULL CONSTRAINT DF_Trackings_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT UQ_Trackings_TrackingKey UNIQUE (TrackingKey)
);
GO

CREATE TABLE dbo.Opens
(
    Id               INT            NOT NULL IDENTITY(1,1) CONSTRAINT PK_Opens PRIMARY KEY,
    TrackingId       INT            NOT NULL,
    [Timestamp]      DATETIME2(3)   NOT NULL,
    UserAgent        NVARCHAR(1000) NOT NULL CONSTRAINT DF_Opens_UserAgent DEFAULT (N''),
    IpAddress        NVARCHAR(64)   NOT NULL CONSTRAINT DF_Opens_IpAddress DEFAULT (N''),
    EmailClient      NVARCHAR(200)  NULL,
    DeviceType       NVARCHAR(100)  NULL,
    OperatingSystem  NVARCHAR(100)  NULL,
    Country          NVARCHAR(100)  NULL,
    City             NVARCHAR(100)  NULL,
    CONSTRAINT FK_Opens_Trackings FOREIGN KEY (TrackingId)
        REFERENCES dbo.Trackings (Id) ON DELETE CASCADE
);
GO

CREATE INDEX IX_Opens_TrackingId ON dbo.Opens (TrackingId);
CREATE INDEX IX_Opens_Timestamp ON dbo.Opens ([Timestamp] DESC);
GO

CREATE TABLE dbo.Clicks
(
    Id                INT            NOT NULL IDENTITY(1,1) CONSTRAINT PK_Clicks PRIMARY KEY,
    TrackingId        INT            NOT NULL,
    LinkType          NVARCHAR(100)  NOT NULL,
    [Timestamp]       DATETIME2(3)   NOT NULL,
    UserAgent         NVARCHAR(1000) NOT NULL CONSTRAINT DF_Clicks_UserAgent DEFAULT (N''),
    IpAddress         NVARCHAR(64)   NOT NULL CONSTRAINT DF_Clicks_IpAddress DEFAULT (N''),
    Browser           NVARCHAR(200)  NULL,
    SourceEmailClient NVARCHAR(200)  NULL,
    DeviceType        NVARCHAR(100)  NULL,
    OperatingSystem   NVARCHAR(100)  NULL,
    Country           NVARCHAR(100)  NULL,
    City              NVARCHAR(100)  NULL,
    CONSTRAINT FK_Clicks_Trackings FOREIGN KEY (TrackingId)
        REFERENCES dbo.Trackings (Id) ON DELETE CASCADE
);
GO

CREATE INDEX IX_Clicks_TrackingId ON dbo.Clicks (TrackingId);
CREATE INDEX IX_Clicks_Timestamp ON dbo.Clicks ([Timestamp] DESC);
CREATE INDEX IX_Clicks_LinkType ON dbo.Clicks (LinkType);
GO
