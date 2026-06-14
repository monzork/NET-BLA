using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Infrastructure.Data;

public class DbSeeder
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public DbSeeder(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task SeedAsync()
    {
        // 1. Connect to 'master' to ensure database 'TaskDb' exists
        using (var tempConn = _connectionFactory.CreateConnection())
        {
            var builder = new SqlConnectionStringBuilder(tempConn.ConnectionString)
            {
                InitialCatalog = "master"
            };

            using var masterConnection = new SqlConnection(builder.ConnectionString);
            await masterConnection.OpenAsync();

            using var createDbCmd = masterConnection.CreateCommand();
            createDbCmd.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'TaskDb')
                BEGIN
                    CREATE DATABASE TaskDb;
                END;";
            await createDbCmd.ExecuteNonQueryAsync();
        }

        // 2. Connect to 'TaskDb' (using the default connection factory) to setup schema & seed data
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        // Ensure Tables Exist
        using var createTablesCmd = connection.CreateCommand();
        createTablesCmd.CommandText = @"
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
            BEGIN
                CREATE TABLE Users (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    Username NVARCHAR(100) NOT NULL,
                    Email NVARCHAR(256) NOT NULL UNIQUE,
                    PasswordHash NVARCHAR(500) NOT NULL,
                    CreatedAt DATETIME2 NOT NULL
                );
            END;

            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TaskItems]') AND type in (N'U'))
            BEGIN
                CREATE TABLE TaskItems (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    Title NVARCHAR(200) NOT NULL,
                    Description NVARCHAR(MAX) NULL,
                    Status INT NOT NULL,
                    DueDate DATETIME2 NULL,
                    UserId UNIQUEIDENTIFIER NOT NULL,
                    CreatedAt DATETIME2 NOT NULL,
                    CONSTRAINT FK_TaskItems_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
                );
            END;";
        
        await createTablesCmd.ExecuteNonQueryAsync();

        // Check if users table is empty
        using var checkCountCmd = connection.CreateCommand();
        checkCountCmd.CommandText = "SELECT COUNT(1) FROM Users";
        var userCount = (int)(await checkCountCmd.ExecuteScalarAsync() ?? 0);

        if (userCount == 0)
            {
            // Seed a default admin user
            var adminUserId = Guid.Parse("a5c8e517-768d-4c9c-868c-d8d5d2e14631");
            var adminEmail = "admin@example.com";
            var adminUsername = "admin";
            var adminPasswordHash = HashPassword("Password123!");

            using var insertUserCmd = connection.CreateCommand();
            insertUserCmd.CommandText = @"
                INSERT INTO Users (Id, Username, Email, PasswordHash, CreatedAt)
                VALUES (@Id, @Username, @Email, @PasswordHash, @CreatedAt)";
            
            insertUserCmd.Parameters.AddWithValue("@Id", adminUserId);
            insertUserCmd.Parameters.AddWithValue("@Username", adminUsername);
            insertUserCmd.Parameters.AddWithValue("@Email", adminEmail);
            insertUserCmd.Parameters.AddWithValue("@PasswordHash", adminPasswordHash);
            insertUserCmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

            await insertUserCmd.ExecuteNonQueryAsync();

            // Seed some sample tasks for this user
            using var insertTasksCmd = connection.CreateCommand();
            insertTasksCmd.CommandText = @"
                INSERT INTO TaskItems (Id, Title, Description, Status, DueDate, UserId, CreatedAt)
                VALUES 
                (@T1_Id, @T1_Title, @T1_Desc, @T1_Status, @T1_DueDate, @UserId, @CreatedAt),
                (@T2_Id, @T2_Title, @T2_Desc, @T2_Status, @T2_DueDate, @UserId, @CreatedAt),
                (@T3_Id, @T3_Title, @T3_Desc, @T3_Status, @T3_DueDate, @UserId, @CreatedAt)";

            insertTasksCmd.Parameters.AddWithValue("@UserId", adminUserId);
            insertTasksCmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

            // Task 1: Completed
            insertTasksCmd.Parameters.AddWithValue("@T1_Id", Guid.NewGuid());
            insertTasksCmd.Parameters.AddWithValue("@T1_Title", "Configure local development environment");
            insertTasksCmd.Parameters.AddWithValue("@T1_Desc", "Set up Docker containers, database connections, and environment settings.");
            insertTasksCmd.Parameters.AddWithValue("@T1_Status", 2); // Completed
            insertTasksCmd.Parameters.AddWithValue("@T1_DueDate", DateTime.UtcNow.AddDays(-1));

            // Task 2: InProgress
            insertTasksCmd.Parameters.AddWithValue("@T2_Id", Guid.NewGuid());
            insertTasksCmd.Parameters.AddWithValue("@T2_Title", "Implement Clean Architecture backend");
            insertTasksCmd.Parameters.AddWithValue("@T2_Desc", "Create Domain, Application, Infrastructure, and API layers with ADO.NET and JWT.");
            insertTasksCmd.Parameters.AddWithValue("@T2_Status", 1); // InProgress
            insertTasksCmd.Parameters.AddWithValue("@T2_DueDate", DateTime.UtcNow.AddDays(2));

            // Task 3: Pending
            insertTasksCmd.Parameters.AddWithValue("@T3_Id", Guid.NewGuid());
            insertTasksCmd.Parameters.AddWithValue("@T3_Title", "Build Angular 20 Standalone frontend");
            insertTasksCmd.Parameters.AddWithValue("@T3_Desc", "Create modern presentation component dashboard and forms with Angular Material.");
            insertTasksCmd.Parameters.AddWithValue("@T3_Status", 0); // Pending
            insertTasksCmd.Parameters.AddWithValue("@T3_DueDate", DateTime.UtcNow.AddDays(7));

            await insertTasksCmd.ExecuteNonQueryAsync();
        }
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}
