# Task Management System (NET-BLA)

A premium, modern full-stack Task Management application featuring a .NET 10.0 Clean Architecture Web API and an Angular 21 standalone frontend styled with a stunning dark-glassmorphic theme.

---

## 🚀 Key Features

- **Authentication & Security**: User registration and login utilizing SHA256 password hashing and JWT (JSON Web Token) bearer auth.
- **Task Management**: Full CRUD operations for TaskItems.
- **Business Rule Validations**:
  - Task Title is required.
  - Task DueDate cannot be in the past.
  - Status must be restricted to `Pending`, `InProgress`, or `Completed`.
- **Custom ADO.NET Repositories**: Pure ADO.NET data access using parameterized SQL queries with `Microsoft.Data.SqlClient`—no Entity Framework, Dapper, or MediatR.
- **Database Auto-Migration & Seeding**: Auto-checks DB schemas on startup and seeds default admin credentials and sample tasks.
- **Unit Testing**: Strict Test-Driven Development (TDD) coverage using xUnit.
- **UI Design**: Angular Material components paired with custom CSS for a premium dark glassmorphism feel (smooth hover glows, backdrop blurs, clean transitions).
- **Separation of Concerns**: Strict smart (container) and dumb (presentation) component separation.

---

## 📁 Repository Structure

```
/home/monzork/code/NET-BLA/
├── NET-BLA.slnx          # .NET Solution XML
├── docker-compose.yml    # Docker configuration for Database & API
├── database/
│   └── schema.sql        # Reference SQL schema
├── frontend/             # Angular 21 client application
│   ├── src/
│   │   ├── app/
│   │   │   ├── components/
│   │   │   │   ├── dumb/   # Presentational (dumb) components
│   │   │   │   └── smart/  # Container (smart) components
│   │   │   │
│   │   │   ├── services/   # AuthService (Signals) and TaskService
│   │   │   └── ...
│   │   └── styles.css      # Premium dark-glassmorphic styling
│   └── package.json
│
├── src/                  # ASP.NET Core Backend
│   ├── Domain/           # Entities and Enums
│   ├── Application/      # Interfaces, DTOs, and Services (Business logic)
│   ├── Infrastructure/   # Repositories (ADO.NET), Seeder, and JWT generator
│   └── API/              # Controllers, Exception Middleware, and Dockerfile
│
└── tests/
    └── Application.UnitTests/  # Business rule validation tests (xUnit)
```

---

## 🛠️ Prerequisites

To run this application, make sure you have the following installed:
- **Docker & Docker Compose**
- **Node.js** (v18+ recommended) and **npm**
- **.NET 10 SDK** (if running tests or building the backend locally without Docker)

---

## ⚡ Getting Started

### Step 1: Start the Backend & Database (via Docker)
Run the following command in the root folder to start SQL Server and the API:
```bash
docker compose up --build -d
```
- **Database Server**: Exposes SQL Server on port `1433`.
- **Web API**: Exposed on port `5000` (mapped from container port `8080`).
- **Swagger Documentation**: Available at `http://localhost:5000/swagger` (if in development) or test connectivity at `http://localhost:5000/api/tasks`.

> [!NOTE]
> On startup, the API container executes `DbSeeder` which automatically creates the `TaskDb` database, defines tables, and seeds default records.

### Step 2: Start the Frontend Application
Navigate into the `frontend` folder, install dependencies, and launch the Angular development server:
```bash
cd frontend
npm install
npm start
```
The client will run on **`http://localhost:4200`**.

---

## 🔑 Default Seeded Credentials

You can use the seeded administrator account to immediately log in and manage tasks:
- **Email**: `admin@example.com`
- **Password**: `Password123!`

---

## 🧪 Running Tests

The application features 17 unit tests verifying the user password hashes and task-item creation/modification validation rules.

To run the backend tests:
```bash
dotnet test
```

---

## 🛡️ Architecture Highlights

### Backend Architecture
- **Domain**: Pure domain models (`User`, `TaskItem`) and core enums (`TaskItemStatus`).
- **Application**: Contains DTO definitions, interfaces, and core domain services (`UserService`, `TaskService`). All input and state validations occur here.
- **Infrastructure**: Impements data access (`TaskRepository`, `UserRepository`) with pure parameterized ADO.NET SQL queries, security tokens (`JwtProvider`), and database initialization schemas (`DbSeeder`).
- **API**: Maps routing, consumes controllers, utilizes custom Global Exception Middleware to translate custom exceptions into correct RESTful status codes, and establishes CORS.

### Frontend Architecture
- **Smart Components**: Containers (like `TaskDashboardContainer`) inject services, maintain Signal states, and delegate data flow.
- **Dumb Components**: Presentation layers (like `TaskList` and `LoginForm`) output event emitters and accept input properties. They have no direct service injection.
- **Signal-Based AuthService**: Uses Angular Signals to maintain reactive, read-only authentication states across the application.
