-- script_v3.sql
-- Survey support: Surveys, SurveyResponses, Trackings flags

USE ExSignAnalytics;
GO

IF OBJECT_ID(N'dbo.Surveys', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Surveys
    (
        Id          INT            NOT NULL IDENTITY(1,1) CONSTRAINT PK_Surveys PRIMARY KEY,
        Name        NVARCHAR(100)  NOT NULL,
        SurveyType  NVARCHAR(50)   NOT NULL, -- stars | scale | emoji
        MinValue    INT            NOT NULL CONSTRAINT DF_Surveys_MinValue DEFAULT (1),
        MaxValue    INT            NOT NULL CONSTRAINT DF_Surveys_MaxValue DEFAULT (5),
        IsActive    BIT            NOT NULL CONSTRAINT DF_Surveys_IsActive DEFAULT (1),
        CreatedAt   DATETIME2(3)   NOT NULL CONSTRAINT DF_Surveys_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_Surveys_SurveyType UNIQUE (SurveyType)
    );

    INSERT INTO dbo.Surveys (Name, SurveyType, MinValue, MaxValue)
    VALUES
        (N'Star Rating', N'stars', 1, 5),
        (N'Number Scale', N'scale', 1, 10),
        (N'Emoji Feedback', N'emoji', 1, 5);
END
GO

IF COL_LENGTH(N'dbo.Trackings', N'HasSignature') IS NULL
BEGIN
    ALTER TABLE dbo.Trackings ADD HasSignature BIT NOT NULL
        CONSTRAINT DF_Trackings_HasSignature DEFAULT (1);
END
GO

IF COL_LENGTH(N'dbo.Trackings', N'HasSurvey') IS NULL
BEGIN
    ALTER TABLE dbo.Trackings ADD HasSurvey BIT NOT NULL
        CONSTRAINT DF_Trackings_HasSurvey DEFAULT (0);
END
GO

IF COL_LENGTH(N'dbo.Trackings', N'SurveyId') IS NULL
BEGIN
    ALTER TABLE dbo.Trackings ADD SurveyId INT NULL;
    ALTER TABLE dbo.Trackings ADD CONSTRAINT FK_Trackings_Surveys
        FOREIGN KEY (SurveyId) REFERENCES dbo.Surveys (Id);
END
GO

IF OBJECT_ID(N'dbo.SurveyResponses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SurveyResponses
    (
        Id              INT              NOT NULL IDENTITY(1,1) CONSTRAINT PK_SurveyResponses PRIMARY KEY,
        TrackingId      INT              NOT NULL,
        SurveyType      NVARCHAR(50)     NOT NULL,
        Score           INT              NULL,
        ChoiceKey       NVARCHAR(50)     NULL,
        ResponseToken   UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_SurveyResponses_Token DEFAULT (NEWID()),
        Comment         NVARCHAR(2000)   NULL,
        CommentedAt     DATETIME2(3)     NULL,
        [Timestamp]     DATETIME2(3)     NOT NULL,
        UserAgent       NVARCHAR(1000)   NOT NULL CONSTRAINT DF_SurveyResponses_UserAgent DEFAULT (N''),
        IpAddress       NVARCHAR(64)     NOT NULL CONSTRAINT DF_SurveyResponses_IpAddress DEFAULT (N''),
        Browser         NVARCHAR(200)    NULL,
        DeviceType      NVARCHAR(100)    NULL,
        OperatingSystem NVARCHAR(100)    NULL,
        SenderEmail     NVARCHAR(320)    NOT NULL CONSTRAINT DF_SurveyResponses_SenderEmail DEFAULT (N''),
        RecipientEmail  NVARCHAR(320)    NULL,
        CONSTRAINT FK_SurveyResponses_Trackings FOREIGN KEY (TrackingId)
            REFERENCES dbo.Trackings (Id) ON DELETE CASCADE,
        CONSTRAINT UQ_SurveyResponses_ResponseToken UNIQUE (ResponseToken)
    );

    CREATE INDEX IX_SurveyResponses_TrackingId ON dbo.SurveyResponses (TrackingId);
    CREATE INDEX IX_SurveyResponses_Timestamp ON dbo.SurveyResponses ([Timestamp] DESC);
    CREATE INDEX IX_SurveyResponses_SurveyType ON dbo.SurveyResponses (SurveyType);
END
GO
