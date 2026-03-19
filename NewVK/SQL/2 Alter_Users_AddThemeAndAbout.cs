/*USE [NewVKDb];
GO

IF COL_LENGTH('dbo.Users', 'AboutMe') IS NULL
BEGIN
    ALTER TABLE dbo.Users
    ADD AboutMe NVARCHAR(1000) NULL;
END
GO

IF COL_LENGTH('dbo.Users', 'ThemeKey') IS NULL
BEGIN
    ALTER TABLE dbo.Users
    ADD ThemeKey NVARCHAR(50) NOT NULL
        CONSTRAINT DF_Users_ThemeKey DEFAULT N'earthy';
END
GO

UPDATE dbo.Users
SET ThemeKey = N'earthy'
WHERE ThemeKey IS NULL;
GO*/