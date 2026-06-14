IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RevokedTokens]') AND type in (N'U'))
BEGIN
    CREATE TABLE RevokedTokens (
        Token VARCHAR(900) PRIMARY KEY,
        ExpiryDate DATETIME2 NOT NULL
    );
END;
