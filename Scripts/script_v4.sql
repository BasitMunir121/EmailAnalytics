-- script_v4.sql
-- Email signatures management

USE ExSignAnalytics;
GO

IF OBJECT_ID(N'dbo.EmailSignatures', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmailSignatures
    (
        Id              INT             NOT NULL IDENTITY(1,1) CONSTRAINT PK_EmailSignatures PRIMARY KEY,
        Name            NVARCHAR(200)   NOT NULL,
        TrackingKey     NVARCHAR(200)   NOT NULL,
        HtmlBody        NVARCHAR(MAX)   NOT NULL CONSTRAINT DF_EmailSignatures_HtmlBody DEFAULT (N''),
        EnableTracking  BIT             NOT NULL CONSTRAINT DF_EmailSignatures_EnableTracking DEFAULT (1),
        IsEnabled       BIT             NOT NULL CONSTRAINT DF_EmailSignatures_IsEnabled DEFAULT (1),
        CreatedAt       DATETIME2(3)    NOT NULL CONSTRAINT DF_EmailSignatures_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       DATETIME2(3)    NOT NULL CONSTRAINT DF_EmailSignatures_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_EmailSignatures_TrackingKey UNIQUE (TrackingKey)
    );

    CREATE INDEX IX_EmailSignatures_Name ON dbo.EmailSignatures (Name);
    CREATE INDEX IX_EmailSignatures_CreatedAt ON dbo.EmailSignatures (CreatedAt DESC);
END
GO
