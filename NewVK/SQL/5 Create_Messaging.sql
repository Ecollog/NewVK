USE [NewVKDb];
GO

IF OBJECT_ID(N'dbo.Conversations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Conversations
    (
        Id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Conversations PRIMARY KEY,
        ConversationType NVARCHAR(20)      NOT NULL CONSTRAINT DF_Conversations_Type DEFAULT N'Direct',
        Title            NVARCHAR(200)     NULL,
        CreatedByUserId  INT               NULL,
        CreatedAtUtc     DATETIME2         NOT NULL CONSTRAINT DF_Conversations_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        LastMessageAtUtc DATETIME2         NULL,

        CONSTRAINT CK_Conversations_Type
            CHECK (ConversationType IN (N'Direct', N'Group')),

        CONSTRAINT FK_Conversations_Users
            FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(Id)
            ON DELETE SET NULL
    );
END
GO

IF OBJECT_ID(N'dbo.ConversationParticipants', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConversationParticipants
    (
        ConversationId   INT       NOT NULL,
        UserId           INT       NOT NULL,
        JoinedAtUtc      DATETIME2 NOT NULL CONSTRAINT DF_ConversationParticipants_JoinedAtUtc DEFAULT SYSUTCDATETIME(),
        LastReadAtUtc    DATETIME2 NULL,

        CONSTRAINT PK_ConversationParticipants PRIMARY KEY (ConversationId, UserId),

        CONSTRAINT FK_ConversationParticipants_Conversations
            FOREIGN KEY (ConversationId) REFERENCES dbo.Conversations(Id)
            ON DELETE CASCADE,

        CONSTRAINT FK_ConversationParticipants_Users
            FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
            ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ConversationParticipants_UserId'
      AND object_id = OBJECT_ID(N'dbo.ConversationParticipants')
)
BEGIN
    CREATE INDEX IX_ConversationParticipants_UserId
        ON dbo.ConversationParticipants(UserId);
END
GO

IF OBJECT_ID(N'dbo.Messages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Messages
    (
        Id             INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Messages PRIMARY KEY,
        ConversationId INT               NOT NULL,
        SenderUserId   INT               NOT NULL,
        Body           NVARCHAR(4000)    NOT NULL,
        SentAtUtc      DATETIME2         NOT NULL CONSTRAINT DF_Messages_SentAtUtc DEFAULT SYSUTCDATETIME(),
        EditedAtUtc    DATETIME2         NULL,
        IsDeleted      BIT               NOT NULL CONSTRAINT DF_Messages_IsDeleted DEFAULT (0),

        CONSTRAINT FK_Messages_Conversations
            FOREIGN KEY (ConversationId) REFERENCES dbo.Conversations(Id)
            ON DELETE CASCADE,

        CONSTRAINT FK_Messages_Users
            FOREIGN KEY (SenderUserId) REFERENCES dbo.Users(Id)
            ON DELETE NO ACTION
    );
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Messages_ConversationId_SentAtUtc'
      AND object_id = OBJECT_ID(N'dbo.Messages')
)
BEGIN
    CREATE INDEX IX_Messages_ConversationId_SentAtUtc
        ON dbo.Messages(ConversationId, SentAtUtc DESC);
END
GO
