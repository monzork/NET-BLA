IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RefreshTokens]') AND type in (N'U'))
BEGIN
    CREATE TABLE RefreshTokens (
        Token VARCHAR(500) PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        ExpiryDate DATETIME2 NOT NULL,
        CreatedAt DATETIME2 NOT NULL,
        RevokedAt DATETIME2 NULL,
        CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
    );
END;
