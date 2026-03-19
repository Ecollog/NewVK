USE [NewVKDb];
GO

IF OBJECT_ID(N'dbo.NewsItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NewsItems
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NewsItems PRIMARY KEY,
        Category        NVARCHAR(100)     NOT NULL,
        Title           NVARCHAR(200)     NOT NULL,
        Summary         NVARCHAR(2000)    NOT NULL,
        Accent          NVARCHAR(50)      NOT NULL CONSTRAINT DF_NewsItems_Accent DEFAULT N'accent-primary',
        IsPublished     BIT               NOT NULL CONSTRAINT DF_NewsItems_IsPublished DEFAULT (1),
        PublishedAtUtc  DATETIME2         NOT NULL CONSTRAINT DF_NewsItems_PublishedAtUtc DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT               NULL,
        CreatedAtUtc    DATETIME2         NOT NULL CONSTRAINT DF_NewsItems_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_NewsItems_Users
            FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(Id)
            ON DELETE SET NULL
    );
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_NewsItems_IsPublished_PublishedAtUtc'
      AND object_id = OBJECT_ID(N'dbo.NewsItems')
)
BEGIN
    CREATE INDEX IX_NewsItems_IsPublished_PublishedAtUtc
        ON dbo.NewsItems(IsPublished, PublishedAtUtc DESC);
END
GO
