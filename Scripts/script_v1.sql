-- script_v1.sql
-- ExSignAnalytics initial schema

IF DB_ID(N'ExSignAnalytics') IS NULL
BEGIN
    CREATE DATABASE ExSignAnalytics;
END
GO

USE ExSignAnalytics;
GO

IF OBJECT_ID(N'dbo.TrackingEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TrackingEvents
    (
        Id                UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TrackingEvents PRIMARY KEY,
        EmailId           NVARCHAR(200)    NOT NULL,
        EventType         NVARCHAR(50)     NOT NULL,
        LinkType          NVARCHAR(100)    NULL,
        [Timestamp]       DATETIME2(3)     NOT NULL,
        UserAgent         NVARCHAR(1000)   NOT NULL CONSTRAINT DF_TrackingEvents_UserAgent DEFAULT (N''),
        IpAddress         NVARCHAR(64)     NOT NULL CONSTRAINT DF_TrackingEvents_IpAddress DEFAULT (N''),
        EmailClient       NVARCHAR(200)    NULL,
        SourceEmailClient NVARCHAR(200)    NULL,
        DeviceType        NVARCHAR(100)    NULL,
        OperatingSystem   NVARCHAR(100)    NULL,
        Country           NVARCHAR(100)    NULL,
        City              NVARCHAR(100)    NULL,
        SenderEmail       NVARCHAR(320)    NOT NULL CONSTRAINT DF_TrackingEvents_SenderEmail DEFAULT (N''),
        RecipientEmail    NVARCHAR(320)    NULL
    );

    CREATE INDEX IX_TrackingEvents_EmailId
        ON dbo.TrackingEvents (EmailId);

    CREATE INDEX IX_TrackingEvents_Timestamp
        ON dbo.TrackingEvents ([Timestamp] DESC);

    CREATE INDEX IX_TrackingEvents_EventType
        ON dbo.TrackingEvents (EventType);
END
GO
