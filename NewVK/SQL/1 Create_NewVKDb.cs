/*IF DB_ID(N'NewVKDb') IS NULL
BEGIN
    CREATE DATABASE [NewVKDb];
END
GO

USE [NewVKDb];
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        Login             NVARCHAR(50)      NOT NULL,
        NormalizedLogin   NVARCHAR(50)      NOT NULL,
        FirstName         NVARCHAR(100)     NOT NULL,
        LastName          NVARCHAR(100)     NOT NULL,
        Email             NVARCHAR(255)     NOT NULL,
        NormalizedEmail   NVARCHAR(255)     NOT NULL,
        Phone             NVARCHAR(30)      NULL,
        PasswordHash      NVARCHAR(256)     NOT NULL,
        PasswordSalt      NVARCHAR(256)     NOT NULL,
        AboutMe           NVARCHAR(1000)    NULL,
        CreatedAtUtc      DATETIME2         NOT NULL CONSTRAINT DF_Users_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        LastLoginAtUtc    DATETIME2         NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Users_NormalizedLogin' AND object_id = OBJECT_ID(N'dbo.Users'))
BEGIN
    CREATE UNIQUE INDEX UX_Users_NormalizedLogin
        ON dbo.Users(NormalizedLogin);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Users_NormalizedEmail' AND object_id = OBJECT_ID(N'dbo.Users'))
BEGIN
    CREATE UNIQUE INDEX UX_Users_NormalizedEmail
        ON dbo.Users(NormalizedEmail);
END
GO*/