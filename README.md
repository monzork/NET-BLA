A full-stack Task Management application featuring a .NET 10.0 Clean Architecture Web API and an Angular 21 standalone frontend styled with a dark-glassmorphism theme.

---

## 📖 User Story & Acceptance Criteria

### User Story
> **As a** software developer / project manager,  
> **I want to** securely register and log into a dashboard, where I can manage my project tasks (create, read, update, delete) with due dates and statuses,  
> **So that** I can track my project progress and milestones in real-time.

### Acceptance Criteria
- **Security & Identity**: Users must register with a username, email, and password. Passwords are securely hashed before storage.
- **Session Auth**: Logging in issues a JSON Web Token (JWT) injected in requests via a frontend HTTP Interceptor.
- **Data Ownership**: Authenticated users can only see, edit, or delete tasks belonging to their own user profile.
- **Business Validations**:
  - Task Title is mandatory.
  - DueDate must not be in the past (validated with timezone tolerance).
  - Status must cycle strictly through `Pending`, `In Progress`, and `Completed`.
- **Session Termination**: Logging out clears stored local credentials, invalidates authentication status, and redirects back to the login screen.

---

## 🚀 Key Features

- **Authentication & Security**: Secure login utilizing **BCrypt password hashing** (`BCrypt.Net-Next`) and JWT token authentication.
- **Refresh Token Rotation (RTR)**: Long-lived session refresh tokens (7 days) paired with short-lived access tokens (15 minutes). Implements strict rotation where old refresh tokens are invalidated on renewal, and **breach detection** immediately revokes all user sessions if a replayed refresh token is detected.
- **JWT Logout Session Revocation (Blocklist)**: Explicit logout revokes active refresh tokens and blocklists the current access token.
- **Rich Domain Model (DDD)**: Encapsulated domain models (`User`, `TaskItem`, `RefreshToken`) with private setters, constructor validation rules, and explicit mutation methods protecting domain invariants.
- **Cursor-Based Pagination**: Dynamic cursor-based pagination (`(CreatedAt, Id)` composite cursors) to fetch tasks efficiently and prevent skipped or duplicate items during active task modifications.
- **Viewport-Specific Paging UX**:
  - **Desktop**: Loads and renders all matching tasks at once (un-paginated).
  - **Mobile**: Loads tasks in batches of 10 and triggers an automatic **Infinite Scroll** via `IntersectionObserver` as the user reaches the bottom.
- **Search Input Debouncing**: Implements a `300ms` typing debounce utilizing an RxJS `Subject` to optimize database traffic and avoid asynchronous API race conditions.
- **Task Management**: Full CRUD operations for TaskItems.
- **API Protection & Rate Limiting**: Native ASP.NET Core rate-limiting middleware featuring a **fixed-window limiter** (10 requests/min) on auth endpoints and a **sliding-window limiter** (100 requests/min) on general CRUD routes.
- **Custom ADO.NET Repositories**: Pure ADO.NET data access using parameterized SQL queries with `Microsoft.Data.SqlClient`—no Entity Framework, Dapper, or MediatR.
- **Database Migrations (DbUp)**: Incremental SQL migration scripts executed automatically on startup.
- **Unit Testing**: Complete test coverage using xUnit (**46 tests** covering domain models, application services, token rotation breach checks, and API controllers).
- **UI Design**: Angular Material components styled with dark glassmorphism layouts, full layout responsiveness, and viewport optimization down to `320px` width.
- **Separation of Concerns**: Smart (container) and dumb (presentation) component separation.

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
│   │   └── styles.css      # Dark-glassmorphic layout styling
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

## 🧪 Running Tests

The application features **46 unit tests** verifying user password hashes, domain entity constructor invariants, task-item validation rules, token rotation breach checks, token revocation repository checks, and REST API controller CRUD/Auth endpoints.

### Method 1: Running in Docker Container (Recommended)
Because the test project tests API controllers, it requires the ASP.NET Core 10.0 runtime (`Microsoft.AspNetCore.App`). If you do not have the preview ASP.NET Core 10.0 runtime installed locally on your host machine, you can run the tests instantly inside the .NET 10.0 SDK container:
```bash
docker run --rm -v $(pwd):/app -w /app mcr.microsoft.com/dotnet/sdk:10.0 dotnet test
```

### Method 2: Running Locally
If you have the ASP.NET Core 10.0 shared framework runtime installed on your host, you can run:
```bash
dotnet test
```

---

## 🛡️ Architecture Highlights

### Backend Architecture
- **Domain**: Pure domain models (`User`, `TaskItem`) and core enums (`TaskItemStatus`). Enforces core domain invariants and business rules on construction and state mutation.
- **Application**: Contains DTO definitions, interfaces, and application services (`UserService`, `TaskService`). Responsible for workflow orchestration, request validation, and mapping security contexts.
- **Infrastructure**: Impements data access (`TaskRepository`, `UserRepository`) with pure parameterized ADO.NET SQL queries, security tokens (`JwtProvider`), and database initialization schemas (`DbSeeder`).
- **API**: Maps routing, consumes controllers, utilizes custom Global Exception Middleware to translate custom exceptions into correct RESTful status codes, and establishes CORS.

### Frontend Architecture
- **Smart Components**: Containers (like `TaskDashboardContainer`) inject services, maintain Signal states, and delegate data flow.
- **Dumb Components**: Presentation layers (like `TaskList` and `LoginForm`) output event emitters and accept input properties. They have no direct service injection.
- **Signal-Based AuthService**: Uses Angular Signals to maintain reactive, read-only authentication states across the application.
