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

            // Seed some sample tasks representing the actual steps to build this project
            var projectTasks = new[]
            {
                new { Title = "Setup Solution and Projects (.NET Core)", Description = "Initialize solution and structure Domain, Application, Infrastructure, API, and Unit Test projects.", Status = 2, DueDays = -5 },
                new { Title = "TDD Phase 1: Create Domain Entities/Enums and Application Interfaces/DTOs", Description = "Define core domain models, DTOs, and interface abstractions for task management.", Status = 2, DueDays = -4 },
                new { Title = "TDD Phase 2: Write xUnit tests for application service validations", Description = "Write unit tests for business rules (e.g. required title, future due date, valid status).", Status = 2, DueDays = -3 },
                new { Title = "TDD Phase 3: Implement TaskService and UserService to pass tests", Description = "Implement password hashing, validation logic, and CRUD services to pass unit tests.", Status = 2, DueDays = -3 },
                new { Title = "Implement Infrastructure Layer (ADO.NET, connection factory, JWT generator, and Seeder)", Description = "Build connection factory, JWT token providers, database seeders, and repositories using parameterized SQL.", Status = 2, DueDays = -2 },
                new { Title = "Implement API Layer (Controllers, Middleware, Program.cs, Dockerfile)", Description = "Build REST controllers, global exception middleware, CORS configuration, and backend containerization.", Status = 2, DueDays = -2 },
                new { Title = "Create database script and docker-compose.yml", Description = "Create database initialization scripts and coordinate containers with docker-compose.", Status = 2, DueDays = -1 },
                new { Title = "Run tests and verify backend compilation and execution", Description = "Compile the backend, run the test suites, and launch backend containers to verify setup.", Status = 2, DueDays = -1 },
                new { Title = "Initialize Angular Frontend Application", Description = "Initialize Angular client with routing, theme settings, and global CSS structures.", Status = 2, DueDays = 0 },
                new { Title = "Implement Angular Smart/Dumb Components, Services, and Interceptors", Description = "Design login/registration forms and dashboard container using Material Design and reactive state.", Status = 1, DueDays = 1 },
                new { Title = "Perform integration testing and create walkthrough", Description = "Verify user flows in browser and document findings in walkthrough.md.", Status = 0, DueDays = 2 },
                new { Title = "Configure single shared Git monorepo and write documentation", Description = "Unify project workspaces, stage and commit repository files, and write README.md setup guides.", Status = 0, DueDays = 3 }
            };

            foreach (var task in projectTasks)
            {
                using var insertTaskCmd = connection.CreateCommand();
                insertTaskCmd.CommandText = @"
                    INSERT INTO TaskItems (Id, Title, Description, Status, DueDate, UserId, CreatedAt)
                    VALUES (@Id, @Title, @Description, @Status, @DueDate, @UserId, @CreatedAt)";

                insertTaskCmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
                insertTaskCmd.Parameters.AddWithValue("@Title", task.Title);
                insertTaskCmd.Parameters.AddWithValue("@Description", task.Description);
                insertTaskCmd.Parameters.AddWithValue("@Status", task.Status);
                insertTaskCmd.Parameters.AddWithValue("@DueDate", DateTime.UtcNow.AddDays(task.DueDays));
                insertTaskCmd.Parameters.AddWithValue("@UserId", adminUserId);
                insertTaskCmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                await insertTaskCmd.ExecuteNonQueryAsync();
            }
        }
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}
