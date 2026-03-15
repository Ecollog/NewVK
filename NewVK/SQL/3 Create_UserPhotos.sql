USE [NewVKDb];
GO

IF OBJECT_ID(N'dbo.UserPhotos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserPhotos
    (
        Id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserPhotos PRIMARY KEY,
        UserId           INT               NOT NULL,
        FileName         NVARCHAR(260)     NOT NULL,
        OriginalFileName NVARCHAR(260)     NOT NULL,
        RelativeUrl      NVARCHAR(500)     NOT NULL,
        ContentType      NVARCHAR(100)     NOT NULL,
        SizeBytes        BIGINT            NOT NULL,
        UploadedAtUtc    DATETIME2         NOT NULL CONSTRAINT DF_UserPhotos_UploadedAtUtc DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_UserPhotos_Users
            FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
            ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_UserPhotos_UserId_UploadedAtUtc'
      AND object_id = OBJECT_ID(N'dbo.UserPhotos')
)
BEGIN
    CREATE INDEX IX_UserPhotos_UserId_UploadedAtUtc
        ON dbo.UserPhotos(UserId, UploadedAtUtc DESC);
END
GO