# Job Tracker

[![Build and Deploy to Azure](https://github.com/juanmunoz00/job-tracker/actions/workflows/deploy.yml/badge.svg)](https://github.com/juanmunoz00/job-tracker/actions/workflows/deploy.yml)

A full-stack, cloud-hosted web application for tracking job applications from first submission to offer or rejection. It has a **.NET 8 REST API** on Azure App Service with **JWT authentication and roles**, an **Angular** single-page application on Azure Static Web Apps, and a **PostgreSQL** database. Both parts deploy automatically through **GitHub Actions** on every push to `main`.

| | Link |
|---|---|
| **Live app (Angular UI)** | https://jobtracker.juanmunoz.dev |
| **Live API docs (Swagger UI)** | https://jobtracker-api-b5efh7c9g8crfwbh.westus3-01.azurewebsites.net/swagger |
| **Source code** | https://github.com/juanmunoz00/job-tracker |
| **Demo logins** | `admin` / `Admin123!` (full access) · `demo` / `Demo123!` (read-only). Sample data only. Sessions expire after 5 minutes. |

> Both parts run on Azure free tiers. The first request after a period of inactivity can take 20–30 seconds while the API starts.

---

## Contents

- [Part 1: What has been built](#part-1-what-has-been-built)
- [Part 2: How to build it, step by step](#part-2-how-to-build-it-step-by-step)
  - [Stage 0: Before you start](#stage-0-before-you-start)
  - [Stage 1: Install the tools and create the accounts](#stage-1-install-the-tools-and-create-the-accounts)
  - [Stage 2: Create the repository and the .NET solution](#stage-2-create-the-repository-and-the-net-solution)
  - [Stage 3: The Core project (domain model)](#stage-3-the-core-project-domain-model)
  - [Stage 4: The Infrastructure project (database access)](#stage-4-the-infrastructure-project-database-access)
  - [Stage 5: The Api project (REST endpoints)](#stage-5-the-api-project-rest-endpoints)
  - [Stage 6: The database and the first migration](#stage-6-the-database-and-the-first-migration)
  - [Stage 7: Run and test the API locally](#stage-7-run-and-test-the-api-locally)
  - [Stage 8: Unit tests](#stage-8-unit-tests)
  - [Stage 9: Commit to GitHub](#stage-9-commit-to-github)
  - [Stage 10: Host the database](#stage-10-host-the-database)
  - [Stage 11: Create the Azure resources for the API](#stage-11-create-the-azure-resources-for-the-api)
  - [Stage 12: Deploy the API with GitHub Actions](#stage-12-deploy-the-api-with-github-actions)
  - [Stage 13: Create the Angular application](#stage-13-create-the-angular-application)
  - [Stage 14: Model, service and data access in Angular](#stage-14-model-service-and-data-access-in-angular)
  - [Stage 15: The application list, status badge and filter](#stage-15-the-application-list-status-badge-and-filter)
  - [Stage 16: The add and edit form](#stage-16-the-add-and-edit-form)
  - [Stage 17: Delete with confirmation](#stage-17-delete-with-confirmation)
  - [Stage 18: Deploy the Angular app to Azure Static Web Apps](#stage-18-deploy-the-angular-app-to-azure-static-web-apps)
  - [Stage 19: Visual redesign](#stage-19-visual-redesign)
  - [Stage 20: Middleware — request logging and global error handling](#stage-20-middleware--request-logging-and-global-error-handling)
  - [Stage 21: Server-side search and pagination (API)](#stage-21-server-side-search-and-pagination-api)
  - [Stage 22: Authentication with JWT and roles (API)](#stage-22-authentication-with-jwt-and-roles-api)
  - [Stage 23: Angular — search, pagination, login and roles (final versions)](#stage-23-angular--search-pagination-login-and-roles-final-versions)
  - [Stage 24: Deploy the new version](#stage-24-deploy-the-new-version)
  - [Stage 25: Final verification checklist](#stage-25-final-verification-checklist)
  - [Troubleshooting](#troubleshooting)
  - [Cost and security notes](#cost-and-security-notes)
- [Part 3: Definitions](#part-3-definitions)

---

# Part 1: What has been built

## Features

- **Sign in** with one of two roles: **Admin** (full access) or **Read-only**. Demo credentials are shown on the login page. Sessions expire after 5 minutes.
- **Browse** applications in a table, newest first: company, position, country, date applied, a color-coded status, and when the status last changed.
- **Search** by company, position or country (case-insensitive, runs on the server, waits until you stop typing).
- **Filter** by clicking a status chip; a pipeline bar shows the mix of statuses at a glance.
- **Paginate** with Previous/Next and a choice of 5, 10 or 20 rows per page.
- **Add, edit and delete** applications (Admin only), with validation and an in-row delete confirmation. The "last updated" time only changes when the status changes.
- **API documentation** with Swagger, including an **Authorize** button for testing tokens.
- **Request logging and consistent errors**: every request is logged with its status and duration, and every error is returned in the standard ProblemDetails format.

## Architecture

```mermaid
flowchart LR
    Dev[Developer] -->|git push main| GH[GitHub Actions]
    GH -->|workflow 1: build, test, publish| API[Azure App Service<br/>.NET 8 REST API]
    GH -->|workflow 2: build Angular| SWA[Azure Static Web Apps<br/>Angular SPA]
    User[Browser] -->|loads the app| SWA
    User -->|1. POST /api/auth/login| API
    API -->|2. JWT, expires in 5 min| User
    User -->|3. HTTPS + JSON with Bearer token, allowed by CORS| API
    API -->|EF Core + Npgsql| DB[(PostgreSQL)]
```

The browser downloads the Angular application once from Azure Static Web Apps. The user signs in, and the API returns a short-lived [JWT](#jwt). From then on, the Angular code calls the API directly over [HTTPS](#https), sending the token with every request. The API checks the token and the user's role, then reads and writes the PostgreSQL database through [Entity Framework Core](#entity-framework-core). The two Azure services have different addresses, so the API explicitly allows the Angular site's address. That rule is called [CORS](#cors).

Inside the API, every request passes through a [middleware](#middleware) pipeline: request logging → CORS → global error handling → authentication → authorization → controllers.

## Tech stack

| Area | Technology |
|---|---|
| Backend language and framework | C# 12, [.NET 8](#net), [ASP.NET Core](#aspnet-core) Web API |
| Data access | [Entity Framework Core](#entity-framework-core) 8 with [Npgsql](#npgsql), code-first [migrations](#migration), server-side [pagination](#pagination) and [ILIKE](#ilike) search |
| Database | [PostgreSQL](#postgresql) (hosted) |
| Security | [JWT](#jwt) bearer authentication, role-based authorization, [password hashing](#password-hashing) (PBKDF2) |
| Cross-cutting | Custom [middleware](#middleware): [structured logging](#structured-logging) and global error handling with [ProblemDetails](#problemdetails) |
| Object mapping | [AutoMapper](#automapper) |
| API documentation | [Swagger / OpenAPI](#swagger) (Swashbuckle) |
| Backend tests | [xUnit](#xunit), [Moq](#moq), [FluentAssertions](#fluentassertions), EF Core InMemory |
| Frontend | [Angular](#angular) 22, [TypeScript](#typescript), [signals](#signal), [RxJS](#rxjs), [reactive forms](#reactive-forms), [interceptors](#interceptor), [route guards](#route-guard), [SCSS](#css-and-scss) |
| Hosting | [Azure App Service](#azure-app-service) (API, Linux, F1 Free), [Azure Static Web Apps](#azure-static-web-apps) (UI, Free) |
| CI/CD | [GitHub Actions](#github-actions), two [workflows](#workflow) |

## Repository layout

```
job-tracker/
├── .github/workflows/
│   ├── deploy.yml                          # API pipeline: build → test → publish → deploy to App Service
│   └── azure-static-web-apps-<name>.yml    # UI pipeline: build Angular → deploy to Static Web Apps
├── JobTracker.sln                          # The .NET solution file
├── JobTracker.Core/                        # Entities (Application, User), DTOs, repository interfaces (no dependencies)
├── JobTracker.Infrastructure/              # DbContext, migrations, repository implementations
├── JobTracker.Api/
│   ├── Auth/                               # JWT token service, demo user seeder
│   ├── Controllers/                        # ApplicationsController, AuthController
│   ├── Middleware/                         # Request logging, global exception handling
│   ├── Mappings/                           # AutoMapper profile
│   └── Program.cs                          # Services, authentication, middleware pipeline
├── JobTracker.Tests/                       # xUnit tests, including security tests
└── job-tracker-ui/                         # Angular application
    ├── public/                             # staticwebapp.config.json, author portrait
    └── src/
        ├── environments/                   # API URL and links for development and production
        └── app/
            ├── models/                     # TypeScript types matching the API's JSON
            ├── services/                   # ApplicationService (HTTP), AuthService (login, session, auto-logout)
            ├── interceptors/               # Adds the Bearer token, handles 401
            ├── guards/                     # Login and admin route guards
            └── components/                 # login, application-list, application-form, status-badge
```

The .NET code follows a [layered architecture](#layered-architecture). `Core` holds the business types and depends on nothing. `Infrastructure` knows how to talk to the database. `Api` exposes HTTP endpoints. Dependencies only point inward, toward `Core`.

## API endpoints

| Method | Route | What it does | Who | Success response |
|---|---|---|---|---|
| `POST` | `/api/auth/login` | Check username and password; return a JWT | Anyone | `200 OK` + token, or `401` |
| `GET` | `/api/applications` | One page of applications. Query options: `search`, `status`, `page` (default 1), `pageSize` (default 10, max 50) | Signed in | `200 OK` + page, totals and status counts, or `400` |
| `GET` | `/api/applications/{id}` | Get one application | Signed in | `200 OK`, or `404` |
| `POST` | `/api/applications` | Create an application. Status starts as `Applied` | Admin | `201 Created` |
| `PUT` | `/api/applications/{id}` | Replace an application's editable fields | Admin | `200 OK`, `400`, or `404` |
| `PATCH` | `/api/applications/{id}/status` | Change only the status | Admin | `200 OK`, `400`, or `404` |
| `DELETE` | `/api/applications/{id}` | Delete an application | Admin | `204 No Content`, or `404` |

Requests without a valid token get `401` ("Authentication required" or "Session expired"); signed-in users without the Admin role get `403`. All errors use the [ProblemDetails](#problemdetails) format.

Example: `GET /api/applications?search=engineer&status=Applied&page=1&pageSize=5` returns

```json
{
  "items": [ { "id": 7, "company": "Contoso", "position": "Senior Software Engineer", "country": "USA",
               "appliedDate": "2026-11-02", "status": "Applied", "statusDate": "2026-11-02T21:45:00Z" } ],
  "page": 1, "pageSize": 5, "totalCount": 1, "totalPages": 1,
  "allCount": 3,
  "statusCounts": { "Applied": 1, "Interview": 1, "Rejected": 1 }
}
```

Valid statuses (case-sensitive): `Applied`, `PhoneScreen`, `Interview`, `TakeHome`, `Offer`, `Rejected`, `Withdrawn`.

See [HTTP methods](#http-methods) and [HTTP status codes](#http-status-codes) in the definitions.

---
# Part 2: How to build it, step by step

This guide builds the whole project from an empty folder. It assumes you know basic C# but nothing else: no prior Angular, Azure, Git or GitHub Actions experience. Every command and every file's complete contents are included.

## Stage 0: Before you start

### How to read this guide

- Words written as [links like this](#definitions-index) jump to **Part 3: Definitions** at the end. Open one whenever a term is new to you.
- Text in `this style` is something you type exactly, or a file or folder name.
- Blocks marked `bash` are commands you run in a **terminal**. On macOS, open the **Terminal** app. On Windows, open **PowerShell**. Run them one line at a time unless the block says otherwise.
- When a step says **"replace the contents"** of a file, select everything in that file (Cmd+A on macOS, Ctrl+A on Windows), delete it, and paste the new contents. Do not add the new code next to the old code. Duplicated code is the most common cause of build errors in this guide.
- After every stage, check the result described under **"Check"** before moving on. Fixing a problem right away is much easier than finding it three stages later.

### What you will need

| Item | Cost | Why |
|---|---|---|
| A computer running macOS, Windows or Linux | — | All tools used here work on all three |
| A [GitHub](#github) account | Free | Stores the code and runs the deployments |
| An [Azure](#azure) account with a card on file | $0 if you follow this guide | Hosts the API and the UI |
| A [PostgreSQL](#postgresql) database you can reach over the internet | Free to a few dollars a month | Stores the data (Stage 10 lists options) |
| About 2–3 working days | — | Most time goes to learning, not typing |

---

## Stage 1: Install the tools and create the accounts

### 1.1 Install Git

[Git](#git) records the history of your code and sends it to GitHub.

- **macOS:** run `git --version` in Terminal. If Git isn't installed, macOS offers to install the Command Line Developer Tools. Accept.
- **Windows:** download and run the installer from https://git-scm.com/download/win, keeping the default options.

**Check:**
```bash
git --version
```
You should see something like `git version 2.4x.x`.

Tell Git who you are (it stamps this on every commit):
```bash
git config --global user.name "Your Name"
git config --global user.email "you@example.com"
```

### 1.2 Install the .NET 8 SDK

The [.NET SDK](#net) contains the compiler and the `dotnet` command used to create, build, test and publish the API.

1. Go to https://dotnet.microsoft.com/download/dotnet/8.0.
2. Under **SDK**, download the installer for your operating system and processor (on Apple Silicon Macs, choose **Arm64**).
3. Run the installer.
4. Close the terminal and open a new one, so it picks up the new command.

**Check:**
```bash
dotnet --list-sdks
```
At least one line must start with `8.`. Newer SDKs (9, 10) can be installed too. This guide pins the project to .NET 8 with `-f net8.0`.

### 1.3 Install Visual Studio Code

[VS Code](#visual-studio-code) is the code editor used in this guide.

1. Download it from https://code.visualstudio.com and install it.
2. Open VS Code, click the **Extensions** icon on the left (four squares), and install:
   - **C# Dev Kit** (by Microsoft)
   - **Angular Language Service** (by Angular)

### 1.4 Install Node.js through nvm

The Angular tools are written in JavaScript and run on [Node.js](#nodejs). [nvm](#nvm) installs and switches between Node versions. Use nvm instead of a direct installer, because different projects often need different Node versions.

**macOS / Linux:**
```bash
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.40.3/install.sh | bash
```
Close the terminal and open a new one, then:
```bash
nvm install --lts
nvm alias default 'lts/*'
```

**Windows:** install **nvm-windows** from https://github.com/coreybutler/nvm-windows/releases (download `nvm-setup.exe`), then in a new PowerShell window:
```bash
nvm install lts
nvm use lts
```

**Check:**
```bash
node -v
npm -v
```
`node -v` must show **v22 or higher** (this project was built with v24). Angular 22 does not run on Node 20.

> **If `node -v` still shows an old version:** run `which node` (macOS/Linux) or `where node` (Windows). If the path doesn't contain `.nvm` or `nvm`, another Node installation (for example from Anaconda/Conda) is taking priority. Fix that before continuing.

### 1.5 Install the Angular CLI

The [Angular CLI](#angular-cli) is the `ng` command that creates, runs and builds Angular projects.

```bash
npm install -g @angular/cli
```

`-g` means *global*: the `ng` command becomes available in every folder. With nvm you never need `sudo` for this.

**Check:**
```bash
ng version
```
You should see `Angular CLI: 22.x.x`. If it asks *"Would you like to share pseudonymous usage data…"*, answer **N**.

### 1.6 Install the Entity Framework Core tools

The `dotnet-ef` tool creates and applies database [migrations](#migration).

```bash
dotnet tool install --global dotnet-ef --version 8.*
```

**Check:**
```bash
dotnet ef --version
```
You should see `Entity Framework Core .NET Command-line Tools 8.x.x`. If the command isn't found, close and reopen the terminal.

### 1.7 Install PostgreSQL locally (for development)

You need a local [PostgreSQL](#postgresql) server to run the API on your own machine.

- **macOS:** install **Postgres.app** from https://postgresapp.com, open it, and click **Initialize**. The default user is your macOS username with no password. To match this guide, also create a `postgres` user with a password (see the Postgres.app docs) or use your username in the connection string.
- **Windows:** download the installer from https://www.postgresql.org/download/windows/ and run it. When it asks for a password for the `postgres` user, choose one and write it down. Keep port `5432`.

**Check:** the server is running (Postgres.app shows a green "Running" status; on Windows, the **postgresql** service is running in Services).

### 1.8 Create the accounts

1. **GitHub:** sign up at https://github.com/signup.
2. **Azure:** sign up at https://azure.microsoft.com/free.

> **Read this before signing up for Azure.** The Azure free account gives **$200 of credit, one time, valid for 30 days**. It does **not** renew monthly. When the 30 days end, the subscription is disabled and every resource pauses, including free-tier ones. To keep using Azure, you must upgrade to [Pay-As-You-Go](#pay-as-you-go) (Stage 11.1). Everything in this guide stays at $0 on Pay-As-You-Go if you use only the free tiers named here.

---

## Stage 2: Create the repository and the .NET solution

### 2.1 Create the GitHub repository

1. On GitHub, click **+** (top right) → **New repository**.
2. **Repository name:** `job-tracker`.
3. **Public** (so recruiters can see it).
4. Check **Add a README file** and choose a **License** (MIT is common).
5. Click **Create repository**.

### 2.2 Clone it to your computer

[Cloning](#clone) copies the repository to your machine and links the two.

1. On the repository page, click the green **Code** button and copy the **HTTPS** URL.
2. In the terminal, go to the folder where you keep projects, then clone:
```bash
cd ~/dev
git clone https://github.com/<your-username>/job-tracker.git
cd job-tracker
```
Replace `~/dev` with your own projects folder, and `<your-username>` with your GitHub username.

### 2.3 Create the solution and the four projects

A [solution](#solution) groups several projects so they build together.

Run these one at a time, from inside the `job-tracker` folder:

```bash
dotnet new sln -n JobTracker
dotnet new webapi   -n JobTracker.Api            --use-controllers -f net8.0
dotnet new classlib -n JobTracker.Core           -f net8.0
dotnet new classlib -n JobTracker.Infrastructure -f net8.0
dotnet new xunit    -n JobTracker.Tests          -f net8.0
dotnet sln add JobTracker.Api JobTracker.Core JobTracker.Infrastructure JobTracker.Tests
```

What each line does:
- `dotnet new sln` creates `JobTracker.sln`.
- `dotnet new webapi --use-controllers` creates a Web API project that uses [controllers](#controller). Without `--use-controllers`, .NET 8 creates the "minimal API" style instead, which this guide doesn't use.
- `dotnet new classlib` creates a plain class library (no web server).
- `dotnet new xunit` creates a test project using [xUnit](#xunit).
- `-f net8.0` pins each project to .NET 8, even if you have a newer SDK installed.
- `dotnet sln add` registers the four projects in the solution.

### 2.4 Delete the template sample files

The templates include sample code you won't use:

```bash
rm JobTracker.Core/Class1.cs JobTracker.Infrastructure/Class1.cs JobTracker.Tests/UnitTest1.cs
rm JobTracker.Api/WeatherForecast.cs JobTracker.Api/Controllers/WeatherForecastController.cs
```
On Windows PowerShell, use `Remove-Item` instead of `rm`, one file at a time.

### 2.5 Connect the projects to each other

Project references define which project can use which.

```bash
dotnet add JobTracker.Api reference JobTracker.Core JobTracker.Infrastructure
dotnet add JobTracker.Infrastructure reference JobTracker.Core
dotnet add JobTracker.Tests reference JobTracker.Core JobTracker.Infrastructure JobTracker.Api
```

This creates the [layered architecture](#layered-architecture): `Api` → `Infrastructure` → `Core`. `Core` references nothing, so the business types never depend on the database or the web framework.

### 2.6 Add the .gitignore file

A [.gitignore](#gitignore) file lists files Git must never commit: compiled output, editor settings and local secrets.

```bash
dotnet new gitignore
```

**Check:**
```bash
dotnet build
```
You should see `Build succeeded` with `0 Error(s)`.

---

## Stage 3: The Core project (domain model)

`JobTracker.Core` holds the [entity](#entity), the [DTOs](#dto) and the [repository](#repository-pattern) interface. Create the folders `Models`, `DTOs` and `Interfaces` inside `JobTracker.Core`.

### 3.1 The entity: `JobTracker.Core/Models/Application.cs`

The entity describes one job application as stored in the database. The [enum](#enum) lists the allowed statuses.

```csharp
namespace JobTracker.Core.Models;

public class Application
{
    public int Id { get; set; }

    public string Country { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Link { get; set; }

    public DateOnly AppliedDate { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;
    public DateTime StatusDate { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }
    public string? Compensation { get; set; }
}

public enum ApplicationStatus
{
    Applied,
    PhoneScreen,
    Interview,
    TakeHome,
    Offer,
    Rejected,
    Withdrawn
}
```

- `string?` means the value may be `null` (optional field).
- `DateOnly` stores a date with no time (the day you applied).
- `StatusDate` is stored in [UTC](#utc). Storing UTC avoids time-zone confusion.

### 3.2 The DTOs

[DTOs](#dto) are the shapes of data that travel over HTTP. They are separate from the entity so the API never exposes database details directly.

**`JobTracker.Core/DTOs/ApplicationDto.cs`**, returned by GET and sent by PUT:
```csharp
namespace JobTracker.Core.DTOs;

public class ApplicationDto
{
    public int Id { get; set; }
    public string Company { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateOnly AppliedDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StatusDate { get; set; }
}
```
`Status` is a `string` here (for example `"Interview"`), because readable text is friendlier in [JSON](#json) than the enum's number.

**`JobTracker.Core/DTOs/CreateApplicationDto.cs`**, sent by POST:
```csharp
namespace JobTracker.Core.DTOs;

public class CreateApplicationDto
{
    public string Company { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateOnly AppliedDate { get; set; }
}
```
No `Id`, `Status` or `StatusDate`: the server assigns those.

**`JobTracker.Core/DTOs/PatchStatusDto.cs`**, sent by PATCH:
```csharp
namespace JobTracker.Core.DTOs;

public class PatchStatusDto
{
    public string Status { get; set; } = string.Empty;
}
```

### 3.3 The repository interface: `JobTracker.Core/Interfaces/IApplicationRepository.cs`

The [interface](#interface) lists what the data layer can do, without saying how. Controllers depend on this interface, not on the database, which makes them easy to [unit test](#unit-test).

```csharp
using JobTracker.Core.Models;

namespace JobTracker.Core.Interfaces;

public interface IApplicationRepository
{
    Task<IEnumerable<Application>> GetAllAsync();
    Task<Application?> GetByIdAsync(int id);
    Task<Application> CreateAsync(Application application);
    Task<Application> UpdateAsync(Application application);
    Task<Application> PatchStatusAsync(int id, ApplicationStatus status);
    Task DeleteAsync(int id);
}
```

`Task<T>` and the `Async` suffix mean these methods are [asynchronous](#async-and-await): they don't block the server while waiting for the database.

**Check:** `dotnet build` succeeds.

---

## Stage 4: The Infrastructure project (database access)

### 4.1 Add the packages

[NuGet](#nuget) packages are libraries you add to a project.

```bash
dotnet add JobTracker.Infrastructure package Microsoft.EntityFrameworkCore --version 8.0.0
dotnet add JobTracker.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.0
```

- **Microsoft.EntityFrameworkCore** is [EF Core](#entity-framework-core), the [ORM](#orm).
- **Npgsql.EntityFrameworkCore.PostgreSQL** is the [Npgsql](#npgsql) provider that lets EF Core talk to PostgreSQL.

### 4.2 The DbContext: `JobTracker.Infrastructure/Data/ApplicationDbContext.cs`

Create the folder `Data`. The [DbContext](#dbcontext) represents a session with the database. Each `DbSet` is a table.

```csharp
using JobTracker.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Application> Applications => Set<Application>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Company)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.Country)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(e => e.Position)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.Status)
                  .HasConversion<int>();   // store enum as integer

            entity.Property(e => e.StatusDate)
                  .HasDefaultValueSql("NOW()");
        });
    }
}
```

- `OnModelCreating` configures the table: primary key, required columns and maximum lengths.
- `HasConversion<int>()` stores the status as a number (0 = Applied, 1 = PhoneScreen, …).
- `HasDefaultValueSql("NOW()")` makes the database fill in the current time if no value is given.

### 4.3 The repository: `JobTracker.Infrastructure/Repositories/ApplicationRepository.cs`

Create the folder `Repositories`. This class implements the interface from Stage 3.3 using EF Core.

```csharp
using JobTracker.Core.Interfaces;
using JobTracker.Core.Models;
using JobTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Infrastructure.Repositories;

public class ApplicationRepository : IApplicationRepository
{
    private readonly ApplicationDbContext _context;

    public ApplicationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Application>> GetAllAsync()
        => await _context.Applications
                         .AsNoTracking()
                         .OrderByDescending(a => a.AppliedDate)
                         .ToListAsync();

    public async Task<Application?> GetByIdAsync(int id)
        => await _context.Applications
                         .AsNoTracking()
                         .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Application> CreateAsync(Application application)
    {
        _context.Applications.Add(application);
        await _context.SaveChangesAsync();
        return application;
    }

    public async Task<Application> UpdateAsync(Application application)
    {
        _context.Applications.Update(application);
        await _context.SaveChangesAsync();
        return application;
    }

    public async Task<Application> PatchStatusAsync(int id, ApplicationStatus status)
    {
        var application = await _context.Applications.FindAsync(id)
            ?? throw new KeyNotFoundException($"Application {id} not found.");

        application.Status = status;
        application.StatusDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return application;
    }

    public async Task DeleteAsync(int id)
    {
        var application = await _context.Applications.FindAsync(id)
            ?? throw new KeyNotFoundException($"Application {id} not found.");

        _context.Applications.Remove(application);
        await _context.SaveChangesAsync();
    }
}
```

- The `DbContext` arrives through the constructor. That's [dependency injection](#dependency-injection).
- `AsNoTracking()` is faster for read-only queries: EF Core doesn't watch the results for changes.
- `SaveChangesAsync()` sends the pending inserts, updates and deletes to the database as [SQL](#sql).
- `?? throw` means "if `FindAsync` returned null, throw an exception".

**Check:** `dotnet build` succeeds.

---

## Stage 5: The Api project (REST endpoints)

### 5.1 Add the packages

```bash
dotnet add JobTracker.Api package AutoMapper --version 12.0.1
dotnet add JobTracker.Api package AutoMapper.Extensions.Microsoft.DependencyInjection --version 12.0.1
dotnet add JobTracker.Api package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add JobTracker.Api package Swashbuckle.AspNetCore --version 6.6.2
```

- **AutoMapper** and its DI extension: [AutoMapper](#automapper) copies values between entities and DTOs.
- **Microsoft.EntityFrameworkCore.Design** lets `dotnet ef` create migrations using this project's configuration.
- **Swashbuckle.AspNetCore** generates the [Swagger](#swagger) documentation page.

> **Important:** check that `Swashbuckle.AspNetCore` appears in `JobTracker.Api/JobTracker.Api.csproj`. Some template versions don't include it. If `Program.cs` calls `AddSwaggerGen()` but the package is missing, the build fails with error `CS1061 … 'AddSwaggerGen'`.

### 5.2 The mapping profile: `JobTracker.Api/Mappings/ApplicationProfile.cs`

Create the folder `Mappings`. A profile tells AutoMapper how to convert between types.

```csharp
using AutoMapper;
using JobTracker.Core.DTOs;
using JobTracker.Core.Models;

namespace JobTracker.Api.Mappings;

public class ApplicationProfile : Profile
{
    public ApplicationProfile()
    {
        CreateMap<Application, ApplicationDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<CreateApplicationDto, Application>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => ApplicationStatus.Applied))
            .ForMember(dest => dest.StatusDate, opt => opt.MapFrom(_ => DateTime.UtcNow));

        CreateMap<ApplicationDto, Application>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Enum.Parse<ApplicationStatus>(src.Status)))
            .ForMember(dest => dest.StatusDate, opt => opt.MapFrom(src => src.StatusDate.ToUniversalTime()));
    }
}
```

The three maps:
1. **Entity → `ApplicationDto`** (for every response): the status enum becomes text.
2. **`CreateApplicationDto` → entity** (for POST): new applications always start as `Applied`, with the current UTC time.
3. **`ApplicationDto` → entity** (for PUT): the text status becomes the enum, the date is forced to UTC (PostgreSQL's `timestamp with time zone` column, through Npgsql, rejects non-UTC values), and `Id` is ignored because the id comes from the URL.

> Every map used in a controller must be declared here. A missing map throws an exception at runtime, which the browser reports as a misleading CORS error (see [Troubleshooting](#troubleshooting)).

### 5.3 The controller: `JobTracker.Api/Controllers/ApplicationsController.cs`

A [controller](#controller) receives HTTP requests and returns responses. Each method below is one [endpoint](#endpoint).

```csharp
using AutoMapper;
using JobTracker.Core.DTOs;
using JobTracker.Core.Interfaces;
using JobTracker.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationRepository _repo;
    private readonly IMapper _mapper;

    public ApplicationsController(IApplicationRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var apps = await _repo.GetAllAsync();
        return Ok(_mapper.Map<IEnumerable<ApplicationDto>>(apps));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var app = await _repo.GetByIdAsync(id);
        return app is null ? NotFound() : Ok(_mapper.Map<ApplicationDto>(app));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateApplicationDto dto)
    {
        var app = _mapper.Map<Application>(dto);
        var created = await _repo.CreateAsync(app);
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            _mapper.Map<ApplicationDto>(created));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ApplicationDto dto)
    {
        if (!Enum.TryParse<ApplicationStatus>(dto.Status, out _))
            return BadRequest($"Invalid status: {dto.Status}");

        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
            return NotFound();

        _mapper.Map(dto, existing);
        var updated = await _repo.UpdateAsync(existing);
        return Ok(_mapper.Map<ApplicationDto>(updated));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> PatchStatus(int id, PatchStatusDto dto)
    {
        if (!Enum.TryParse<ApplicationStatus>(dto.Status, out var status))
            return BadRequest($"Invalid status: {dto.Status}");

        var patched = await _repo.PatchStatusAsync(id, status);
        return Ok(_mapper.Map<ApplicationDto>(patched));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _repo.DeleteAsync(id);
        return NoContent();
    }
}
```

- `[Route("api/[controller]")]`: `[controller]` is replaced by the class name minus "Controller", giving `/api/applications`.
- `[HttpGet("{id}")]`: `{id}` is a [route parameter](#route-parameter). ASP.NET Core copies it into the `int id` argument through [model binding](#model-binding).
- `Ok(...)`, `NotFound()`, `BadRequest(...)`, `NoContent()` and `CreatedAtAction(...)` produce the [HTTP status codes](#http-status-codes) 200, 404, 400, 204 and 201.
- **`Update` loads the existing record first**, copies the DTO's values onto it with `_mapper.Map(dto, existing)`, then saves. That keeps the fields the DTO doesn't carry (`Description`, `Link`, `Notes`, `Compensation`). Mapping a brand-new entity and saving it would overwrite those columns with `null`.

### 5.4 Program.cs: `JobTracker.Api/Program.cs`

`Program.cs` is the API's entry point. It registers services for [dependency injection](#dependency-injection), then sets up the request [middleware](#middleware) pipeline.

Replace the contents with:

```csharp
using JobTracker.Core.Interfaces;
using JobTracker.Infrastructure.Data;
using JobTracker.Infrastructure.Repositories;
using JobTracker.Api.Mappings;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddAutoMapper(typeof(ApplicationProfile).Assembly);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddPolicy("AngularClient", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AngularClient");
app.UseAuthorization();
app.MapControllers();

app.Run();
```

Line by line:
- Everything **before** `builder.Build()` registers services. **Nothing may be added to `builder.Services` after `builder.Build()`.** The service collection is read-only by then, and the app crashes on startup.
- `AddDbContext` + `UseNpgsql(...)` connect EF Core to PostgreSQL, using the [connection string](#connection-string) named `DefaultConnection` from [configuration](#configuration).
- `AddScoped<IApplicationRepository, ApplicationRepository>()`: whenever a class asks for `IApplicationRepository`, give it an `ApplicationRepository`, one instance per HTTP request ("scoped").
- `AddSwaggerGen()` + `UseSwagger()` + `UseSwaggerUI()` serve the docs at `/swagger`. They're enabled in every environment so the live API shows its documentation.
- The [CORS](#cors) policy allows the origins listed in configuration under `Cors:AllowedOrigins`, so new front-end addresses can be added without changing code.
- After `Build()`, the `app.Use...` lines run **in order** for every request.

### 5.5 Configuration files

**Replace the contents of `JobTracker.Api/appsettings.json`:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:4200" ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```
- The connection string is **intentionally empty**. Real values come from [user secrets](#user-secrets) locally and from App Service settings in Azure. **Never put a real password in a file that is committed to Git.**
- `http://localhost:4200` is where the Angular development server runs (Stage 13).

**Replace the contents of `JobTracker.Api/appsettings.Development.json`:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=jobtracker;Username=postgres;Password=yourpassword"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```
This placeholder only documents the expected format. The real local value goes into user secrets in the next step, which overrides it.

### 5.6 Store your local connection string in user secrets

[User secrets](#user-secrets) are stored in your user profile folder, outside the repository, so they can never be committed.

```bash
dotnet user-secrets init --project JobTracker.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=jobtracker;Username=postgres;Password=<your-local-password>" --project JobTracker.Api
```

- `init` adds a `<UserSecretsId>` line to `JobTracker.Api.csproj`. That line is safe to commit.
- Replace `<your-local-password>` with the password you chose in Stage 1.7.
- In zsh (macOS), if the password contains `$` or `!`, use single quotes `'...'` around the connection string instead of double quotes.

**Check:** `dotnet build` succeeds.

---

## Stage 6: The database and the first migration

A [migration](#migration) is a versioned C# script that creates or changes database tables to match your entities.

### 6.1 Create the local database

```bash
psql -U postgres -h localhost -c "CREATE DATABASE jobtracker;"
```
It asks for the `postgres` password. On macOS with Postgres.app, `psql` is available from the app's menu (**Open psql**) if the command isn't found in Terminal.

### 6.2 Create the migration

From the repository root (the folder containing `JobTracker.sln`):

```bash
dotnet ef migrations add InitialCreate -p JobTracker.Infrastructure -s JobTracker.Api
```

- `-p JobTracker.Infrastructure` (*project*): where the migration files are written. That's where the `DbContext` lives.
- `-s JobTracker.Api` (*startup project*): which project's configuration and services to use, including the connection string.

This creates a `Migrations` folder in `JobTracker.Infrastructure` with three files. Commit them. They are the history of your database schema.

### 6.3 Apply it

```bash
dotnet ef database update -p JobTracker.Infrastructure -s JobTracker.Api
```

**Check:** the output ends with `Done.` The `jobtracker` database now has an `Applications` table and an `__EFMigrationsHistory` table.

---

## Stage 7: Run and test the API locally

```bash
dotnet run --project JobTracker.Api
```

The output shows a line like `Now listening on: http://localhost:5296` (the port comes from `JobTracker.Api/Properties/launchSettings.json`).

1. Open `http://localhost:<port>/swagger` in your browser.
2. Expand **POST /api/applications** → **Try it out** → paste:
   ```json
   {
     "company": "Contoso",
     "country": "USA",
     "position": "Senior Software Engineer",
     "appliedDate": "2026-10-20"
   }
   ```
   → **Execute**. Expect **201**, with a response body containing an `id` and `"status": "Applied"`.
3. Run **GET /api/applications**. Expect **200**, with your row in the list.
4. Run **PATCH /api/applications/{id}/status** with body `{ "status": "Interview" }`. Expect **200**.
5. Run **PUT /api/applications/{id}** with the full object from step 3, changing `position`. Expect **200**.
6. Run **DELETE /api/applications/{id}**. Expect **204**.

Press **Ctrl+C** in the terminal to stop the API.

---

## Stage 8: Unit tests

[Unit tests](#unit-test) check the controller's behavior without a real database, using [Moq](#moq) to fake the repository.

### 8.1 Add the test packages

```bash
dotnet add JobTracker.Tests package Moq --version 4.20.72
dotnet add JobTracker.Tests package FluentAssertions --version 8.11.0
dotnet add JobTracker.Tests package AutoMapper --version 12.0.1
dotnet add JobTracker.Tests package Microsoft.EntityFrameworkCore.InMemory --version 8.0.0
```

> **License note:** FluentAssertions version 8 and later requires a paid license for commercial use. It's free for non-commercial and open-source projects like this one. For company work, use version 7.x or check the license terms.

### 8.2 The controller tests: `JobTracker.Tests/Controllers/ApplicationsControllerTests.cs`

Create the folder `Controllers` inside `JobTracker.Tests`.

```csharp
using AutoMapper;
using FluentAssertions;
using JobTracker.Api.Controllers;
using JobTracker.Core.DTOs;
using JobTracker.Core.Interfaces;
using JobTracker.Core.Models;
using JobTracker.Api.Mappings;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace JobTracker.Tests.Controllers;

public class ApplicationsControllerTests
{
    private readonly Mock<IApplicationRepository> _repoMock;
    private readonly IMapper _mapper;
    private readonly ApplicationsController _controller;

    public ApplicationsControllerTests()
    {
        _repoMock = new Mock<IApplicationRepository>();
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ApplicationProfile>());
        _mapper = config.CreateMapper();
        _controller = new ApplicationsController(_repoMock.Object, _mapper);
    }

    private static Application MakeApp(int id = 1) => new()
    {
        Id = id, Company = "Acme Corp", Country = "US",
        Position = "Senior Software Engineer",
        AppliedDate = new DateOnly(2026, 9, 21),
        Status = ApplicationStatus.Applied,
        StatusDate = DateTime.UtcNow
    };

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([MakeApp()]);
        var result = await _controller.GetAll();
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<ApplicationDto>>();
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeApp());
        var result = await _controller.GetById(1);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_MissingId_ReturnsNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Application?)null);
        var result = await _controller.GetById(999);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ValidDto_ReturnsCreatedAtAction()
    {
        var dto = new CreateApplicationDto { Company = "Acme", Country = "US",
            Position = "Engineer", AppliedDate = new DateOnly(2026, 9, 21) };
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Application>()))
                 .ReturnsAsync(MakeApp());
        var result = await _controller.Create(dto);
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task PatchStatus_ValidStatus_ReturnsOk()
    {
        _repoMock.Setup(r => r.PatchStatusAsync(1, ApplicationStatus.Interview))
                 .ReturnsAsync(MakeApp());
        var result = await _controller.PatchStatus(1, new PatchStatusDto { Status = "Interview" });
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PatchStatus_InvalidStatus_ReturnsBadRequest()
    {
        var result = await _controller.PatchStatus(1, new PatchStatusDto { Status = "NotReal" });
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        _repoMock.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);
        var result = await _controller.Delete(1);
        result.Should().BeOfType<NoContentResult>();
    }
}
```

The test pattern:
- **Arrange:** `_repoMock.Setup(...)` tells the fake repository what to return.
- **Act:** call the controller method.
- **Assert:** `result.Should().BeOfType<...>()` checks the type of response.
- `[Fact]` marks a method as a test.

### 8.3 Run the tests

```bash
dotnet test
```

**Check:** `Passed!` with 0 failed.

---

## Stage 9: Commit to GitHub

A [commit](#commit) is a saved snapshot of your code. [Pushing](#push) sends commits to GitHub.

```bash
git status
```
Read the list. It must **not** contain `bin/`, `obj/` or any file with a real password. If `bin/` or `obj/` appear, your `.gitignore` is missing (go back to Stage 2.6).

```bash
git add .
git commit -m "Add .NET 8 API with EF Core, PostgreSQL, Swagger and tests"
git push origin main
```

**Check:** refresh the repository page on GitHub. Your folders appear there.

---
## Stage 10: Host the database

The deployed API needs a PostgreSQL database it can reach over the internet. Your local database isn't reachable from Azure.

### 10.1 Choose a host

| Option | Cost | Notes |
|---|---|---|
| **Shared hosting that includes PostgreSQL** (this project uses SmarterASP.NET) | Included in the hosting plan | Usually connects through port `6432` via [PgBouncer](#pgbouncer) |
| **Neon** (neon.tech) | Free tier (about 1 GB) | The database sleeps when idle, so the first query after idle is slower |
| **Azure Database for PostgreSQL Flexible Server** | About $15–20/month after the free trial | The most "Azure-native", but the only option here that costs money every month |

This project started on Azure Database for PostgreSQL, then moved to a shared host's PostgreSQL when the Azure free trial ended, to keep the monthly cost at $0. The code didn't change, because PostgreSQL is PostgreSQL wherever it runs. Only the connection string changed.

### 10.2 Create the database on the host

The exact screens differ by host, but the steps are the same:

1. Find **Databases** (or similar) in the host's control panel and choose **PostgreSQL**.
2. Create a database. Many shared hosts add an account prefix to the name and user (for example `db_ab1234_jobtracker` and `ab1234_jobtracker`).
3. Set the database user's password. **Avoid `;`, `'` and `"` in the password**, because they break connection strings.
4. Write down the **host/server name**, **port**, **database name**, **username** and **password**.
5. If the host has a **remote access** or **allowed IP** setting, enable remote access. Both your computer and Azure connect from outside the host's network.

### 10.3 Build the connection string

```
Host=<server>;Port=<port>;Database=<database>;Username=<user>;Password=<password>;Ssl Mode=Prefer;Trust Server Certificate=true
```

- `Ssl Mode=Prefer` encrypts the connection with [SSL/TLS](#ssl-and-tls) when the server supports it. Azure Database for PostgreSQL *requires* encryption, so use `Ssl Mode=Require` there.
- `Trust Server Certificate=true` accepts the server's certificate without checking who issued it. That's common with shared hosts.
- If you see errors mentioning **prepared statements** with port `6432`, add `;No Reset On Close=true;Max Auto Prepare=0` to the end.

**Keep this string private.** Store it in a password manager. Never paste it into a committed file, a public issue or a chat you share.

### 10.4 Create the tables on the hosted database

Apply the same migration from Stage 6, this time pointed at the hosted database with `--connection`. From the repository root:

```bash
dotnet ef database update -p JobTracker.Infrastructure -s JobTracker.Api --connection '<your hosted connection string>'
```

- **Use single quotes** around the connection string. In zsh and bash, an unquoted `;` ends the command, which splits the string apart and produces errors like `command not found: Ssl`.
- **Do not delete the Migrations folder.** `database update` replays your existing migrations against any PostgreSQL database. You'd only regenerate migrations if you switched database engines (for example to SQL Server).

**Check:** the output ends with `Applying migration '..._InitialCreate'.` and `Done.`

| If the error contains | Cause | Fix |
|---|---|---|
| `Build failed` | The code doesn't compile | Run `dotnet build` and fix the errors it lists |
| `Timeout during connection attempt` | The network is blocked | Enable remote access on the host. If the host has an IP allow-list, add your public IP (`curl -4 ifconfig.me`). Try without a VPN |
| `password authentication failed` | Wrong username or password | Recheck both, including any account prefix |
| `database "…" does not exist` | Wrong database name | Copy the exact name from the control panel |

---

## Stage 11: Create the Azure resources for the API

### 11.1 Make sure your subscription is active

1. Sign in at https://portal.azure.com.
2. Search **Subscriptions** in the top bar and open your subscription.
3. If you see **"Your free trial has expired"**, or **Status** is not **Active**, click **Upgrade**:
   - Choose **Pay-As-You-Go**.
   - **Support plan: choose Basic** (free). The other support plans bill every month.
   - Confirm your card and submit. After a few minutes, **Status** shows **Active**.

[Pay-As-You-Go](#pay-as-you-go) charges only for what you use above free amounts. This guide uses only free tiers, so the bill stays at $0. A [free tier](#f1-free-tier) still needs an *active* subscription: an expired trial runs nothing.

### 11.2 Create a budget alert (do this first)

A [budget](#azure-budget) emails you before costs surprise you. Pay-As-You-Go has **no hard spending cap**, so a budget only warns you. It doesn't stop charges.

1. Search **Cost Management** → **Budgets** → **+ Add**.
2. **Scope:** your subscription (or your billing account, which covers all subscriptions).
3. **Name:** `monthly-cap` · **Reset period:** Monthly · **Expiration:** a few years out · **Amount:** `5`.
4. **Next** → add two alert conditions:

   | Type | % of budget | Fires when |
   |---|---|---|
   | Actual | 20 | You've actually been charged $1 |
   | Forecasted | 100 | Azure predicts you'll reach $5 this month |

5. **Alert recipients:** your email address. **Create**.
6. Add `azure-noreply@microsoft.com` to your email's safe senders so alerts don't land in spam.

### 11.3 Create a resource group

A [resource group](#resource-group) is a folder for everything this project owns. Deleting it deletes all of it.

1. Search **Resource groups** → **+ Create**.
2. **Name:** `jobtracker-rg` · **Region:** one near you (this project uses West US 3).
3. **Review + create** → **Create**.

### 11.4 Create the App Service (the API's host)

[Azure App Service](#azure-app-service) runs your API.

1. Search **App Services** → **+ Create** → **Web App**.
2. **Basics** tab:
   - **Resource group:** `jobtracker-rg`
   - **Name:** `jobtracker-api` (must be unique across Azure)
   - **Publish:** Code
   - **Runtime stack:** **.NET 8 (LTS)**
   - **Operating System:** Linux
   - **Region:** the same as the resource group
   - **Pricing plan:** choose **Free F1**. Click **Explore pricing plans** if it isn't selected, and pick F1 under **Dev/Test**.
3. **Review + create** → **Create**, then **Go to resource**.

### 11.5 Note the real address (the default domain)

On the App Service's **Overview**, copy the **Default domain**. Newer App Services get a unique name like:

```
jobtracker-api-b5efh7c9g8crfwbh.westus3-01.azurewebsites.net
```

> **The plain `jobtracker-api.azurewebsites.net` address does not point to your app.** It returns 404. Always use the default domain shown on Overview.

### 11.6 Add the connection string as an app setting

1. In `jobtracker-api` → **Settings** → **Environment variables** → **App settings** tab → **+ Add**.
2. **Name:** `ConnectionStrings__DefaultConnection` (two underscores).
3. **Value:** your hosted connection string from Stage 10.3, without quotes.
4. Leave **Deployment slot setting** unchecked → **Apply** → **Apply** (bottom of page) → **Confirm**.

In .NET [configuration](#configuration), `__` (double underscore) separates sections, so this setting becomes `ConnectionStrings:DefaultConnection`, exactly what `GetConnectionString("DefaultConnection")` reads. Using an App setting instead of the separate "Connection strings" tab avoids a known Linux issue where the value comes back null.

### 11.7 Enable publish credentials

1. In `jobtracker-api` → **Settings** → **Configuration** → **General settings** tab.
2. Confirm **Stack: .NET**, **Version: .NET 8 (LTS)**.
3. Set **SCM Basic Auth Publishing Credentials** to **On** → **Save** → **Continue**.

New App Services turn this off by default. Without it, the publish profile needed in Stage 12 can't be downloaded or doesn't work.

### 11.8 Confirm everything is free

1. Search **All resources** (not "Recently viewed", which only shows pages you've opened).
2. You should see only the App Service and its **App Service plan** (named like `ASP-jobtrackerrg-xxxx`).
3. Open the plan → **Overview** → **Pricing plan** must say **Free (F1)**.

---

## Stage 12: Deploy the API with GitHub Actions

[GitHub Actions](#github-actions) builds, tests and deploys the API automatically every time you push to `main`. That's [CI/CD](#cicd).

### 12.1 Download the publish profile

1. `jobtracker-api` → **Overview** → **Download publish profile**. A `.PublishSettings` file downloads.
2. Open it in a text editor (TextEdit on macOS, Notepad on Windows). It's XML starting with `<publishData>`.
3. Select all and copy.

The [publish profile](#publish-profile) contains deployment credentials, so treat it like a password.

### 12.2 Store it as a GitHub secret

1. On GitHub, open your repository → **Settings** → **Secrets and variables** → **Actions** → **New repository secret**.
2. **Name:** `AZURE_WEBAPP_PUBLISH_PROFILE`.
3. **Secret:** paste the whole XML → **Add secret**.
4. **Delete the downloaded `.PublishSettings` file** and empty the trash.

A [GitHub secret](#github-secret) is encrypted. Workflows can use it, but nobody can read it back, not even you.

### 12.3 Create the workflow: `.github/workflows/deploy.yml`

Create the folders `.github/workflows` at the repository root (note the leading dot), and a file named `deploy.yml` inside:

```yaml
name: Build and Deploy to Azure

on:
  push:
    branches: [ "main" ]
  workflow_dispatch:

env:
  DOTNET_VERSION: '8.0.x'
  AZURE_WEBAPP_NAME: 'jobtracker-api'
  AZURE_WEBAPP_PACKAGE_PATH: './publish'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Set up .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore
        run: dotnet restore JobTracker.sln

      - name: Build
        run: dotnet build JobTracker.sln --configuration Release --no-restore

      - name: Test
        run: dotnet test JobTracker.sln --no-build --configuration Release

      - name: Publish
        run: |
          dotnet publish JobTracker.Api/JobTracker.Api.csproj \
            --configuration Release \
            --no-build \
            --output ${{ env.AZURE_WEBAPP_PACKAGE_PATH }}

      - name: Deploy to Azure Web App
        uses: azure/webapps-deploy@v3
        with:
          app-name: ${{ env.AZURE_WEBAPP_NAME }}
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: ${{ env.AZURE_WEBAPP_PACKAGE_PATH }}
```

How to read it ([YAML](#yaml) uses indentation to show structure, so keep the spaces exactly as shown):
- `on:` lists **only triggers**: every push to `main`, plus `workflow_dispatch`, which adds a **Run workflow** button in the Actions tab. Don't put settings like `app-name` under `on:`; GitHub rejects the file.
- `env:` defines values reused below.
- `runs-on: ubuntu-latest`: GitHub runs the job on a fresh Linux machine (a [runner](#runner)).
- `steps:` run in order: download the code → install .NET 8 → restore packages → build → **run the tests** (a failing test stops the deploy) → publish → deploy using the secret.

### 12.4 Push and watch

```bash
git add .github/workflows/deploy.yml
git commit -m "Add GitHub Actions workflow to deploy the API to Azure"
git push origin main
```

1. On GitHub, open the **Actions** tab.
2. Click the running workflow and watch each step turn green.
3. If a step turns red, click it and read the error (see [Troubleshooting](#troubleshooting)).

### 12.5 Check the live API

Using your **default domain** from Stage 11.5:

- `https://<default-domain>/swagger` shows the Swagger UI.
- `https://<default-domain>/api/applications` returns `[]` (an empty list) or your rows.

The first request after idle can take 20–30 seconds, because F1 apps sleep when unused. If you get a 500 error, open `jobtracker-api` → **Monitoring** → **Log stream**, reload the page, and read the error.

---
## Stage 13: Create the Angular application

The front end is an [Angular](#angular) [single-page application](#spa) written in [TypeScript](#typescript). The browser downloads it once, then it exchanges only [JSON](#json) data with the API.

### 13.1 Create the project

From the **repository root** (the folder containing `JobTracker.sln`):

```bash
ng new job-tracker-ui --style=scss --ssr=false --skip-git
```

- `job-tracker-ui`: the folder and project name. It sits next to the .NET projects in the same repository.
- `--style=scss`: write styles in [SCSS](#css-and-scss).
- `--ssr=false`: no [server-side rendering](#ssr). It isn't needed here and would complicate hosting.
- `--skip-git`: don't create a second Git repository inside the first one.

If it asks more questions:
- *"Which AI tools do you want to configure?"*: choose **None**.
- *"Do you want to create a 'zoneless' application?"*: answer **Yes** (see [zoneless](#zoneless)).

It then downloads the libraries with [npm](#npm), which takes 1–2 minutes and ends with **"Packages installed successfully."**

### 13.2 Run it

```bash
cd job-tracker-ui
ng serve
```

`ng serve` builds the app and starts a development web server on `http://localhost:4200` ([localhost and ports](#localhost-and-port)). Open that address in your browser to see Angular's welcome page.

**Keep `ng serve` running** in this terminal. It rebuilds and reloads the browser every time you save a file. Open a **second terminal tab** (Cmd+T on macOS, or the **+** in Windows Terminal) for all other commands, and run `cd job-tracker-ui` in it. Stop the server with **Ctrl+C**.

### 13.3 Tour of the project

| File / folder | What it is | .NET equivalent |
|---|---|---|
| `package.json` | Lists the libraries and scripts | `.csproj` package references |
| `node_modules/` | Downloaded libraries (huge, never committed) | NuGet cache |
| `angular.json` | Build settings | `.csproj` build settings |
| `tsconfig.json` | TypeScript compiler settings | `<Nullable>`, `<LangVersion>` |
| `public/` | Files copied to the site as-is | `wwwroot/` |
| `src/index.html` | The single HTML page the whole app lives in | Host page |
| `src/main.ts` | Entry point that starts the app | `Program.cs` → `app.Run()` |
| `src/styles.scss` | Styles for the whole app | Site-wide CSS |
| `src/app/app.config.ts` | Registers app-wide services | `builder.Services.Add...` |
| `src/app/app.routes.ts` | URL → page mapping | `[Route]` attributes |
| `src/app/app.ts` / `.html` / `.scss` | The root [component](#component): class, [template](#template), styles | A controller + view |
| `*.spec.ts` | Unit tests ([Vitest](#vitest)) | xUnit test classes |

A **component** is one piece of the screen, made of a TypeScript class (data and logic), an HTML template (what's displayed) and its own styles.

### 13.4 Environment files

[Environment files](#environment-files) hold values that differ between local development and production, here the API's address.

In the second terminal tab, inside `job-tracker-ui`:
```bash
ng generate environments
```
This creates `src/environments/environment.ts` (used by production builds) and `src/environments/environment.development.ts` (used by `ng serve`).

**Replace the contents of `src/environments/environment.ts`:**
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://<your-api-default-domain>/api'
};
```

**Replace the contents of `src/environments/environment.development.ts`:**
```typescript
export const environment = {
  production: false,
  apiUrl: 'https://<your-api-default-domain>/api'
};
```

Replace `<your-api-default-domain>` with the default domain from Stage 11.5 (for example `jobtracker-api-b5efh7c9g8crfwbh.westus3-01.azurewebsites.net`). Both files point at the live API, so you don't need to run the .NET API locally while building the UI. To use a local API instead, set the development file to `http://localhost:<port>/api`.

> Each file must contain **one** `export const environment` block only. A second block gives error `TS2451: Cannot redeclare block-scoped variable 'environment'`.

- `export` makes the constant usable from other files (like `public`).
- `const` means it can't be reassigned (like `readonly`).

---

## Stage 14: Model, service and data access in Angular

### 14.1 The model: `src/app/models/application.ts`

Create the folder `src/app/models` (inside `app`) and the file `application.ts`. These TypeScript types describe the JSON the API sends and receives.

```typescript
export type ApplicationStatus =
  | 'Applied'
  | 'PhoneScreen'
  | 'Interview'
  | 'TakeHome'
  | 'Offer'
  | 'Rejected'
  | 'Withdrawn';

export interface Application {
  id: number;
  company: string;
  country: string;
  position: string;
  appliedDate: string;
  status: ApplicationStatus;
  statusDate: string;
}

export interface CreateApplication {
  company: string;
  country: string;
  position: string;
  appliedDate: string;
}

export const STATUSES: ApplicationStatus[] = [
  'Applied',
  'PhoneScreen',
  'Interview',
  'TakeHome',
  'Offer',
  'Rejected',
  'Withdrawn'
];

export const STATUS_LABELS: Record<ApplicationStatus, string> = {
  Applied: 'Applied',
  PhoneScreen: 'Phone screen',
  Interview: 'Interview',
  TakeHome: 'Take-home',
  Offer: 'Offer',
  Rejected: 'Rejected',
  Withdrawn: 'Withdrawn'
};
```

| TypeScript | Meaning | C# equivalent |
|---|---|---|
| `type ApplicationStatus = 'Applied' \| ...` | A [union type](#union-type): the value must be one of these exact strings | `enum` |
| `interface Application { id: number; ... }` | The shape of an object. It exists only at compile time, so you can't `new` it | A DTO class |
| `id: number` | Name first, type after the colon | `public int Id` |
| `appliedDate: string` | JSON has no date type, so dates arrive as text (`"2026-10-20"`) | — |
| `STATUSES` | The statuses in display order, for dropdowns | `static readonly` array |
| `Record<ApplicationStatus, string>` | An object with exactly one entry per status. Forgetting one is a compile error | `Dictionary<,>` |

Property names are **camelCase** (`appliedDate`) because ASP.NET Core sends JSON in camelCase, even though the C# properties are PascalCase.

### 14.2 Turn on HttpClient: `src/app/app.config.ts`

[HttpClient](#httpclient) is Angular's class for HTTP calls. It must be registered before it can be [injected](#dependency-injection).

**Replace the contents of `src/app/app.config.ts`:**
```typescript
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient, withFetch } from '@angular/common/http';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withFetch())
  ]
};
```

- `providers` is Angular's equivalent of `builder.Services`.
- `provideRouter(routes, withComponentInputBinding())` turns on [routing](#routing) and passes URL parameters (like the `5` in `/edit/5`) into component [inputs](#input) automatically. Stage 16 relies on this.
- `provideHttpClient(withFetch())` registers HttpClient, using the browser's modern `fetch` feature.

### 14.3 The service: `src/app/services/application.service.ts`

Create the folder `src/app/services` (inside `app`, not inside `src` directly) and the file `application.service.ts`. A [service](#service) holds logic shared by many components. This one makes every call to the API, like the repository does on the backend.

```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Application, ApplicationStatus, CreateApplication } from '../models/application';

@Injectable({ providedIn: 'root' })
export class ApplicationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/applications`;

  getAll(): Observable<Application[]> {
    return this.http.get<Application[]>(this.baseUrl);
  }

  getById(id: number): Observable<Application> {
    return this.http.get<Application>(`${this.baseUrl}/${id}`);
  }

  create(app: CreateApplication): Observable<Application> {
    return this.http.post<Application>(this.baseUrl, app);
  }

  update(id: number, app: Application): Observable<Application> {
    return this.http.put<Application>(`${this.baseUrl}/${id}`, app);
  }

  patchStatus(id: number, status: ApplicationStatus): Observable<Application> {
    return this.http.patch<Application>(`${this.baseUrl}/${id}/status`, { status });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
```

| TypeScript | Meaning | C# equivalent |
|---|---|---|
| `import { X } from '...'` | Use code from another file or library | `using` |
| `@Injectable({ providedIn: 'root' })` | A [decorator](#decorator): one shared instance for the whole app | `AddSingleton<T>()` |
| `inject(HttpClient)` | Ask [DI](#dependency-injection) for an HttpClient | Constructor injection |
| `` `${environment.apiUrl}/applications` `` | A [template literal](#template-literal) (string interpolation with backticks) | `$"{apiUrl}/applications"` |
| `get<Application[]>(...)` | Generic: "the response is an array of Application" | `GetFromJsonAsync<List<T>>()` |
| `Observable<T>` | A value that arrives later ([Observable](#observable)) | `Task<T>` |
| `{ status }` | Shorthand for `{ status: status }` | `new { Status = status }` |

**An Observable does nothing until someone calls `.subscribe()`.** Calling `getAll()` alone sends no request. The components in the next stages subscribe.

The file is created by hand, not with `ng generate service`, because Angular 22 would name the generated class `Application`, which clashes with the `Application` model.

### 14.4 The root component

The root component becomes a simple layout: a header, then a placeholder where the current page appears.

**Replace the contents of `src/app/app.ts`:**
```typescript
import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {}
```

**Replace the entire contents of `src/app/app.html`** (the generated welcome page is about 240 lines; delete all of it):
```html
<header class="app-header">
  <h1>Job Tracker</h1>
</header>

<main>
  <router-outlet />
</main>
```

- `@Component({...})` is a [decorator](#decorator) that turns the class into a component. `selector: 'app-root'` matches the `<app-root>` tag in `src/index.html`.
- `<router-outlet />` is where the page matching the current URL is displayed (like `@RenderBody()` in a Razor layout).

### 14.5 Global styles: `src/styles.scss`

**Replace the contents of `src/styles.scss`** (this file applies to the whole app, unlike component `.scss` files):

```scss
/* You can add global styles to this file, and also import other style files */
body {
  font-family: system-ui, -apple-system, "Segoe UI", sans-serif;
  color: #1a2233;
  margin: 0;
}

.btn {
  display: inline-block;
  padding: 0.45rem 0.9rem;
  border-radius: 6px;
  border: 1px solid transparent;
  font: inherit;
  font-size: 0.9rem;
  text-decoration: none;
  cursor: pointer;
}

.btn--primary { background: #1f5fbf; color: #fff; }
.btn--primary:hover { background: #194f9f; }
.btn--primary:disabled { opacity: 0.6; cursor: default; }

.btn--ghost { background: transparent; color: #3d4a63; border-color: #c9d1de; }
.btn--ghost:hover { background: #f3f5f9; }
```

`.btn--primary` follows the [BEM](#bem) naming convention: `btn` is the block, `--primary` is a variation.

---

## Stage 15: The application list, status badge and filter

### 15.1 Generate the two components

In the second terminal tab, inside `job-tracker-ui`:

```bash
ng generate component components/application-list
ng generate component components/status-badge
```

Each command creates a folder under `src/app/components/` with four files: `.ts` (class), `.html` (template), `.scss` (styles) and `.spec.ts` (test).

### 15.2 The status badge class: `src/app/components/status-badge/status-badge.ts`

A badge is a small colored "pill" showing a status. The list passes each row's status to it.

**Replace the contents:**
```typescript
import { Component, computed, input } from '@angular/core';
import { ApplicationStatus, STATUS_LABELS } from '../../models/application';

@Component({
  selector: 'app-status-badge',
  templateUrl: './status-badge.html',
  styleUrl: './status-badge.scss'
})
export class StatusBadge {
  readonly status = input.required<ApplicationStatus>();

  protected readonly label = computed(() => STATUS_LABELS[this.status()]);
  protected readonly cssClass = computed(() => `badge badge--${this.status().toLowerCase()}`);
}
```

- `input.required<ApplicationStatus>()` declares an [input](#input), a value the parent component must pass in. Inputs are [signals](#signal), so you read one by calling it: `this.status()`.
- `computed(() => ...)` creates a [computed signal](#computed-signal), recalculated automatically when the signals it reads change. `label` turns `PhoneScreen` into `Phone screen`, and `cssClass` turns it into `badge badge--phonescreen`.

### 15.3 The status badge template: `status-badge.html`

**Replace the contents:**
```html
<span [class]="cssClass()">{{ label() }}</span>
```

- `[class]="cssClass()"` is [property binding](#property-binding): the square brackets mean the value is an expression, not literal text.
- `{{ label() }}` is [interpolation](#interpolation): it inserts the value into the page.

### 15.4 The status badge styles: `status-badge.scss`

**Replace the contents:**
```scss
.badge {
  display: inline-block;
  padding: 0.15rem 0.6rem;
  border-radius: 999px;
  font-size: 0.8rem;
  font-weight: 500;
  white-space: nowrap;
  border: 1px solid transparent;
}

.badge--applied     { background: #eef2f8; color: #3d4a63; border-color: #d6deeb; }
.badge--phonescreen { background: #e6f1fb; color: #0b5cad; border-color: #c4ddf5; }
.badge--interview   { background: #ece8fb; color: #5b3cc4; border-color: #d6cdf5; }
.badge--takehome    { background: #fdf3e1; color: #9a5b00; border-color: #f4dcae; }
.badge--offer       { background: #e3f6ec; color: #11724a; border-color: #bce6cf; }
.badge--rejected    { background: #fdecec; color: #b42318; border-color: #f6caca; }
.badge--withdrawn   { background: #f1f1f1; color: #6b6b6b; border-color: #dcdcdc; }
```

`border-radius: 999px` rounds the box into a pill shape. Colors are [hex codes](#hex-color).

### 15.5 The list class: `src/app/components/application-list/application-list.ts`

This is the final version of the list class. It loads the applications, filters them by status, and handles delete (Stage 17 explains the delete parts).

**Replace the contents:**
```typescript
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApplicationService } from '../../services/application.service';
import { Application, ApplicationStatus, STATUSES, STATUS_LABELS } from '../../models/application';
import { StatusBadge } from '../status-badge/status-badge';

@Component({
  selector: 'app-application-list',
  imports: [DatePipe, FormsModule, RouterLink, StatusBadge],
  templateUrl: './application-list.html',
  styleUrl: './application-list.scss'
})
export class ApplicationList implements OnInit {
  private readonly applicationService = inject(ApplicationService);

  protected readonly applications = signal<Application[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  protected readonly statuses = STATUSES;
  protected readonly statusLabels = STATUS_LABELS;
  protected readonly statusFilter = signal<ApplicationStatus | 'All'>('All');

  protected readonly filteredApplications = computed(() => {
    const filter = this.statusFilter();
    const apps = this.applications();
    return filter === 'All' ? apps : apps.filter(app => app.status === filter);
  });

  protected readonly filterLabel = computed(() => {
    const filter = this.statusFilter();
    return filter === 'All' ? 'All' : STATUS_LABELS[filter];
  });

  protected readonly confirmingId = signal<number | null>(null);
  protected readonly deletingId = signal<number | null>(null);
  protected readonly deleteError = signal<string | null>(null);

  ngOnInit(): void {
    this.applicationService.getAll().subscribe({
      next: (apps) => {
        this.applications.set(apps);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load applications. Please try again.');
        this.loading.set(false);
      }
    });
  }

  protected askDelete(id: number): void {
    this.deleteError.set(null);
    this.confirmingId.set(id);
  }

  protected cancelDelete(): void {
    this.confirmingId.set(null);
  }

  protected confirmDelete(id: number): void {
    this.deletingId.set(id);

    this.applicationService.delete(id).subscribe({
      next: () => {
        this.applications.update(apps => apps.filter(app => app.id !== id));
        this.confirmingId.set(null);
        this.deletingId.set(null);
      },
      error: () => {
        this.deleteError.set('Could not delete the application. Please try again.');
        this.deletingId.set(null);
      }
    });
  }
}
```

The key ideas:

- **[Signals](#signal):** `signal<Application[]>([])` creates a box holding a value. Read it with `applications()`, change it with `.set(newValue)`. When a signal changes, Angular redraws only the parts of the screen that read it. This app is [zoneless](#zoneless), so a plain field assigned later would **not** update the screen. Signals are how Angular learns that something changed.
- **`ngOnInit()`** is a [lifecycle hook](#lifecycle-hook): Angular calls it once, right after creating the component. It's the conventional place to load data.
- **`.subscribe({ next, error })`** starts the HTTP request. `next` runs when the data arrives (like code after `await`), and `error` runs on failure (like `catch`). HttpClient Observables complete after one response, so there's nothing to unsubscribe.
- **`filteredApplications`** is a computed signal that reads **two** signals (`statusFilter` and `applications`). Changing either one recalculates it automatically. `apps.filter(...)` is like LINQ's `.Where(...)`.
- **`filterLabel`** uses [type narrowing](#type-narrowing): after the `filter === 'All'` check, TypeScript knows `filter` must be a real status, so `STATUS_LABELS[filter]` is safe.
- **`protected`** members can be used by this component's template, while `private` members can't.
- **`string | null`** is a union type meaning "a string or null" (like `string?`).
- **`imports: [...]`** lists what the template uses: `DatePipe` for dates, `FormsModule` for the filter dropdown, `RouterLink` for links, and `StatusBadge` for badges.

### 15.6 The list template: `application-list.html`

**Replace the contents:**
```html
<section class="list">

  <div class="toolbar">
    <h2>Applications</h2>

    <div class="toolbar-actions">
      <label class="filter">
        Status
        <select [(ngModel)]="statusFilter">
          <option value="All">All</option>
          @for (status of statuses; track status) {
            <option [value]="status">{{ statusLabels[status] }}</option>
          }
        </select>
      </label>

      <a routerLink="/add" class="btn btn--primary">Add application</a>
    </div>
  </div>

  @if (loading()) {
    <p class="muted">Loading applications…</p>
  } @else if (error()) {
    <p class="error">{{ error() }}</p>
  } @else if (applications().length === 0) {
    <p class="muted">No applications yet.</p>
  } @else if (filteredApplications().length === 0) {
    <p class="muted">No applications with status "{{ filterLabel() }}".</p>
  } @else {
    @if (deleteError()) {
      <p class="error">{{ deleteError() }}</p>
    }
    <p class="muted count">
      Showing {{ filteredApplications().length }} of {{ applications().length }}
    </p>

    <table>
      <thead>
        <tr>
          <th>Company</th>
          <th>Position</th>
          <th>Country</th>
          <th>Applied</th>
          <th>Status</th>
          <th>Updated</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        @for (app of filteredApplications(); track app.id) {
          <tr>
            <td>{{ app.company }}</td>
            <td>{{ app.position }}</td>
            <td>{{ app.country }}</td>
            <td>{{ app.appliedDate | date: 'mediumDate' }}</td>
            <td><app-status-badge [status]="app.status" /></td>
            <td>{{ app.statusDate | date: 'short' }}</td>
            <td class="row-actions">
              @if (confirmingId() === app.id) {
                <span class="confirm-text">Delete?</span>
                <button type="button" class="link link--danger"
                        (click)="confirmDelete(app.id)"
                        [disabled]="deletingId() === app.id">
                  {{ deletingId() === app.id ? 'Deleting…' : 'Yes' }}
                </button>
                <button type="button" class="link" (click)="cancelDelete()">No</button>
              } @else {
                <a [routerLink]="['/edit', app.id]" class="link">Edit</a>
                <button type="button" class="link link--danger" (click)="askDelete(app.id)">Delete</button>
              }
            </td>
          </tr>
        }
      </tbody>
    </table>
  }
</section>
```

| Template syntax | Meaning | Razor equivalent |
|---|---|---|
| `{{ app.company }}` | [Interpolation](#interpolation) | `@app.Company` |
| `@if (...) { } @else if { } @else { }` | Show a block conditionally ([control flow](#control-flow-blocks)) | `@if / else` |
| `@for (app of list; track app.id)` | Repeat a block per item. `track` tells Angular how to identify rows so it redraws only changed ones | `@foreach` |
| `loading()` | Reading a signal: note the parentheses | — |
| `app.appliedDate \| date: 'mediumDate'` | A [pipe](#pipe) formats a value for display (`Oct 20, 2026`) | `.ToString("MMM d, yyyy")` |
| `[(ngModel)]="statusFilter"` | [Two-way binding](#two-way-binding): the dropdown shows the signal's value, and choosing an option updates the signal. Pass the signal itself, **without** parentheses | `@bind` in Blazor |
| `<app-status-badge [status]="app.status" />` | Places the child component and passes this row's status into its input | A partial view with a parameter |
| `routerLink="/add"` | A link to another page inside the SPA, without reloading the page | `asp-action` link |
| `[routerLink]="['/edit', app.id]"` | Builds the URL from parts: `/edit/1`, `/edit/2`, … | — |
| `(click)="askDelete(app.id)"` | [Event binding](#event-binding): run a method on click | `@onclick` |

HTML table tags: `<table>` is the table, `<thead>` the header section, `<tbody>` the body, `<tr>` a row, `<th>` a header cell, and `<td>` a data cell.

### 15.7 The list styles: `application-list.scss`

**Replace the contents:**
```scss
.list {
  max-width: 960px;
  margin: 0 auto;
  padding: 1rem;
}

table {
  width: 100%;
  border-collapse: collapse;
}

th, td {
  text-align: left;
  padding: 0.6rem 0.75rem;
  border-bottom: 1px solid #e2e6ee;
}

th {
  font-size: 0.8rem;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: #5a6478;
}

.muted { color: #5a6478; }
.error { color: #b42318; }

.toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  flex-wrap: wrap;
}

.filter {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.9rem;
  color: #5a6478;
}

select {
  padding: 0.35rem 0.5rem;
  border: 1px solid #c9d1de;
  border-radius: 6px;
  font: inherit;
  color: #1a2233;
  background: #fff;
}

.count {
  font-size: 0.85rem;
  margin: 0.25rem 0 0.75rem;
}

.toolbar-actions {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.row-actions {
  text-align: right;
  white-space: nowrap;
}

.link {
  color: #1f5fbf;
  text-decoration: none;
  font: inherit;
  font-size: 0.9rem;
  background: none;
  border: 0;
  padding: 0;
  cursor: pointer;
}

.link:hover {
  text-decoration: underline;
}

.link:disabled {
  opacity: 0.6;
  cursor: default;
}

.link--danger {
  color: #b42318;
}

.row-actions > * + * {
  margin-left: 0.75rem;
}

.confirm-text {
  font-size: 0.9rem;
  color: #3d4a63;
}
```

- `rem` is a size relative to the base font (`1rem` is usually 16px).
- `display: flex` ([Flexbox](#flexbox)) lays items out in a row. `justify-content: space-between` pushes the title left and the actions right, and `flex-wrap: wrap` stacks them on narrow screens.
- `margin: 0 auto` centers a block horizontally.
- The `.link` rules remove the default button look so the buttons look like links.
- `.row-actions > * + *` adds space between items in the cell, but not before the first one.
- Component styles affect only that component. Angular rewrites the selectors behind the scenes.

### 15.8 Add the routes: `src/app/app.routes.ts`

[Routes](#routing) map URLs to components. This is the final version, including the form routes from Stage 16. Create the form component (Stage 16.1) before saving this file, or the import will fail.

**Replace the contents:**
```typescript
import { Routes } from '@angular/router';
import { ApplicationList } from './components/application-list/application-list';
import { ApplicationForm } from './components/application-form/application-form';

export const routes: Routes = [
  { path: '', component: ApplicationList },
  { path: 'add', component: ApplicationForm },
  { path: 'edit/:id', component: ApplicationForm },
  { path: '**', redirectTo: '' }
];
```

- `path: ''` is the home page (`/`).
- `'edit/:id'`: `:id` is a [route parameter](#route-parameter), passed into the form's `id` input thanks to `withComponentInputBinding()` (Stage 14.2).
- `'**'` is a wildcard that sends any unknown URL back to the list. It **must stay last**, because routes are checked in order.

---

## Stage 16: The add and edit form

One component handles both pages. At `/add` it shows an empty form and sends a POST. At `/edit/5` it loads application 5, fills in the form, shows a **Status** dropdown, and sends a PUT.

### 16.1 Generate the component

```bash
ng generate component components/application-form
```

### 16.2 The form class: `src/app/components/application-form/application-form.ts`

**Replace the contents:**
```typescript
import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { ApplicationService } from '../../services/application.service';
import { Application, ApplicationStatus, STATUSES, STATUS_LABELS } from '../../models/application';

function today(): string {
  const d = new Date();
  d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
  return d.toISOString().slice(0, 10);
}

@Component({
  selector: 'app-application-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './application-form.html',
  styleUrl: './application-form.scss'
})
export class ApplicationForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly applicationService = inject(ApplicationService);
  private readonly router = inject(Router);

  readonly id = input<string>();
  protected readonly isEdit = computed(() => !!this.id());

  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly statuses = STATUSES;
  protected readonly statusLabels = STATUS_LABELS;

  private original: Application | null = null;

  protected readonly form = this.fb.nonNullable.group({
    company:     ['', [Validators.required, Validators.maxLength(100)]],
    position:    ['', [Validators.required, Validators.maxLength(100)]],
    country:     ['USA', [Validators.required, Validators.maxLength(60)]],
    appliedDate: [today(), Validators.required],
    status:      ['Applied' as ApplicationStatus, Validators.required]
  });

  ngOnInit(): void {
    const id = this.id();
    if (!id) {
      return;   // /add — nothing to load
    }

    this.loading.set(true);
    this.applicationService.getById(Number(id)).subscribe({
      next: (app) => {
        this.original = app;
        this.form.patchValue({
          company: app.company,
          position: app.position,
          country: app.country,
          appliedDate: app.appliedDate,
          status: app.status
        });
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load this application.');
        this.loading.set(false);
      }
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const v = this.form.getRawValue();
    let request$: Observable<unknown>;

    if (this.original) {
      const statusChanged = v.status !== this.original.status;
      request$ = this.applicationService.update(this.original.id, {
        ...this.original,
        company: v.company,
        position: v.position,
        country: v.country,
        appliedDate: v.appliedDate,
        status: v.status,
        statusDate: statusChanged ? new Date().toISOString() : this.original.statusDate
      });
    } else {
      request$ = this.applicationService.create({
        company: v.company,
        position: v.position,
        country: v.country,
        appliedDate: v.appliedDate
      });
    }

    request$.subscribe({
      next: () => this.router.navigate(['/']),
      error: () => {
        this.error.set('Could not save the application. Please try again.');
        this.saving.set(false);
      }
    });
  }
}
```

| Code | Meaning | C# / .NET equivalent |
|---|---|---|
| `today()` | Today's date in **your** time zone. `toISOString()` alone uses [UTC](#utc), which shows "tomorrow" in the evening in the Americas | — |
| `readonly id = input<string>()` | Optional input filled from the URL's `:id`. It's `undefined` on `/add` | `[FromRoute] string? id` |
| `!!this.id()` | Converts to `true`/`false` | `!string.IsNullOrEmpty(id)` |
| `this.fb.nonNullable.group({...})` | Defines the [reactive form](#reactive-forms): `[default value, validation rules]` per field | A view model with `[Required][MaxLength]` |
| `Validators.required`, `Validators.maxLength(100)` | [Validators](#validators), the validation rules | DataAnnotations |
| `nonNullable` | Resetting a field returns it to its default, not `null` | — |
| `Number(id)` | Text `"5"` → number `5`. URL parameters are always text | `int.Parse` |
| `this.form.patchValue({...})` | Fills the form from an object | Setting view model properties |
| `this.form.invalid` / `markAllAsTouched()` | Validation failed / show every error message | `!ModelState.IsValid` |
| `getRawValue()` | The form's values as a plain object | Model binding |
| `{ ...this.original, company: v.company, ... }` | The [spread operator](#spread-operator): copy all properties, then override some | Record `with { ... }` |
| `let request$: Observable<unknown>` | Holds either the PUT or the POST. The `$` suffix is a naming convention for Observables | A `Task` variable |
| `this.router.navigate(['/'])` | Go to the list from code | `RedirectToAction` |

- **Why spread `original`:** the API's PUT expects the full `ApplicationDto`, including `id` and `statusDate`. Spreading keeps fields the form doesn't edit.
- **`statusDate` logic:** it's stamped with the current time only when the status changed, so "Updated" stays meaningful.
- **`saving`** disables Save while the request runs, so a double click can't create duplicates.
- A field is **"touched"** once the user clicks into it and leaves. Errors only show for touched fields, so a fresh form isn't covered in red.

### 16.3 The form template: `application-form.html`

**Replace the contents:**
```html
<section class="form-page">
  <h2>{{ isEdit() ? 'Edit application' : 'Add application' }}</h2>

  @if (loading()) {
    <p class="muted">Loading application…</p>
  } @else {
    <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <div class="field">
        <label for="company">Company</label>
        <input id="company" type="text" formControlName="company" />
        @if (form.controls.company.touched && form.controls.company.hasError('required')) {
          <small class="field-error">Company is required.</small>
        }
        @if (form.controls.company.hasError('maxlength')) {
          <small class="field-error">Keep it under 100 characters.</small>
        }
      </div>

      <div class="field">
        <label for="position">Position</label>
        <input id="position" type="text" formControlName="position" />
        @if (form.controls.position.touched && form.controls.position.hasError('required')) {
          <small class="field-error">Position is required.</small>
        }
        @if (form.controls.position.hasError('maxlength')) {
          <small class="field-error">Keep it under 100 characters.</small>
        }
      </div>

      <div class="field">
        <label for="country">Country</label>
        <input id="country" type="text" formControlName="country" />
        @if (form.controls.country.touched && form.controls.country.hasError('required')) {
          <small class="field-error">Country is required.</small>
        }
      </div>

      <div class="field">
        <label for="appliedDate">Applied on</label>
        <input id="appliedDate" type="date" formControlName="appliedDate" />
        @if (form.controls.appliedDate.touched && form.controls.appliedDate.hasError('required')) {
          <small class="field-error">Pick the date you applied.</small>
        }
      </div>

      @if (isEdit()) {
        <div class="field">
          <label for="status">Status</label>
          <select id="status" formControlName="status">
            @for (status of statuses; track status) {
              <option [value]="status">{{ statusLabels[status] }}</option>
            }
          </select>
        </div>
      }

      @if (error()) {
        <p class="error">{{ error() }}</p>
      }

      <div class="actions">
        <a routerLink="/" class="btn btn--ghost">Cancel</a>
        <button type="submit" class="btn btn--primary" [disabled]="saving()">
          {{ saving() ? 'Saving…' : 'Save' }}
        </button>
      </div>
    </form>
  }
</section>
```

| Syntax | Meaning |
|---|---|
| `[formGroup]="form"` | Connects the HTML form to the `form` object in the class |
| `formControlName="company"` | Connects this input to the `company` field, both ways |
| `(ngSubmit)="submit()"` | [Event binding](#event-binding): call `submit()` when the form is submitted (button or Enter key) |
| `novalidate` | Turns off the browser's own validation popups, so only Angular's messages show |
| `hasError('required')` | `true` when that rule fails |
| `type="date"` | The browser's date picker. Its value is text like `"2026-11-02"`, exactly what the API's `DateOnly` expects |
| `<label for="company">` + `id="company"` | Links the label to the input: clicking the label focuses the input, and screen readers announce it |
| `@if (isEdit())` | The Status dropdown only appears when editing. New applications always start as `Applied` |

### 16.4 The form styles: `application-form.scss`

**Replace the contents:**
```scss
.form-page {
  max-width: 520px;
  margin: 0 auto;
  padding: 1rem;
}

form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
}

label {
  font-size: 0.85rem;
  font-weight: 500;
  color: #3d4a63;
}

input,
select {
  padding: 0.5rem 0.6rem;
  border: 1px solid #c9d1de;
  border-radius: 6px;
  font: inherit;
  background: #fff;
}

.muted { color: #5a6478; }

input.ng-touched.ng-invalid {
  border-color: #b42318;
}

.field-error,
.error {
  color: #b42318;
  font-size: 0.8rem;
}

.actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
  margin-top: 0.5rem;
}
```

Angular automatically adds CSS classes to form fields based on their state: `ng-touched` / `ng-untouched`, `ng-valid` / `ng-invalid`, and `ng-dirty` / `ng-pristine`. The rule `input.ng-touched.ng-invalid` turns the border red on any visited field that fails validation.

**Check:** with `ng serve` running, open `http://localhost:4200`. Click **Add application**, try **Save** with empty fields (red messages appear), then fill it in and save. You land back on the list with the new row. Click **Edit** on a row, change the status and save. The badge changes and **Updated** shows the current time.

---

## Stage 17: Delete with confirmation

The delete feature is already in the files from Stage 15. Here's how it works:

1. **Delete** calls `askDelete(id)`, which stores the row's id in the `confirmingId` signal.
2. The template's `@if (confirmingId() === app.id)` switches **only that row** to "Delete? Yes / No".
3. **No** calls `cancelDelete()`, which sets `confirmingId` back to `null`.
4. **Yes** calls `confirmDelete(id)`. That sets `deletingId` (the button shows "Deleting…" and is disabled), then calls the API.
5. On success, `this.applications.update(apps => apps.filter(app => app.id !== id))` replaces the list with a **new** array without that item. `filteredApplications` recalculates and the row disappears. No page reload is needed.
6. On failure, `deleteError` shows a message above the table. It's separate from `error`, which would hide the whole table.

**Why a new array?** Signals notice a change when they're given a different value. Removing an item from the same array in place (for example with `splice`) leaves it the same object, so the screen wouldn't update. Creating a new array (an [immutable update](#immutable-update)) is the safe pattern.

**Check:** add a throwaway application, click **Delete** → **No** (nothing happens), then **Delete** → **Yes** (the row disappears). Refresh the page: the row is still gone.

Commit your work:
```bash
cd ..
git add job-tracker-ui
git status
git commit -m "Add Angular frontend with list, filter, status badges, add/edit form and delete"
git push origin main
```
In `git status`, confirm that **no** `node_modules/` or `.angular/` paths are listed. Angular's own `job-tracker-ui/.gitignore` excludes them.

---
## Stage 18: Deploy the Angular app to Azure Static Web Apps

[Azure Static Web Apps](#azure-static-web-apps) hosts the built Angular files for free and serves them over HTTPS from a [CDN](#cdn). Like the API, it redeploys automatically on every push.

### 18.1 Add the routing fallback: `job-tracker-ui/public/staticwebapp.config.json`

In a [SPA](#spa), every page is really `index.html`. If someone opens `/edit/5` directly or presses refresh there, Azure looks for a file called `edit/5`, doesn't find it, and returns 404. This file tells Azure to serve `index.html` for any path that isn't a real file, so Angular's router can handle it.

Create the file inside the existing `public` folder:
```json
{
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": ["/*.{js,css,ico,png,svg,jpg,webp,woff,woff2,json,txt}"]
  }
}
```

- `rewrite`: serve `index.html` for unknown paths.
- `exclude`: real asset files (scripts, styles, images, fonts) must still return 404 if missing.
- Angular copies everything in `public/` into the build output, so this file lands at the root of the deployed site, where Azure looks for it.

### 18.2 Run a production build locally

Inside `job-tracker-ui`:
```bash
ng build
```

`ng build` creates the optimized production version, using `environment.ts`. It's the equivalent of `dotnet publish -c Release`. When it finishes:
```bash
ls dist/job-tracker-ui/browser
```
You should see `index.html`, several `.js` and `.css` files, and `staticwebapp.config.json`. **Note the path `dist/job-tracker-ui/browser`**, because Azure will ask for it. Don't commit `dist/`; it's already ignored.

Warnings about "budgets" (bundle size limits) are fine. Errors are not.

### 18.3 Commit and push the config file

```bash
cd ..
git add job-tracker-ui/public/staticwebapp.config.json
git commit -m "Add Static Web Apps navigation fallback for Angular routes"
git push origin main
```

### 18.4 Create the Static Web App

1. In the Azure portal, search **Static Web Apps** → **+ Create**.
2. **Basics:**
   - **Resource group:** `jobtracker-rg`
   - **Name:** `jobtracker-ui`
   - **Plan type:** **Free**. Double-check this, because Standard costs money.
   - **Region for Azure Functions API and staging environments:** any listed US region (for example West US 2). The site itself is served globally regardless.
3. **Deployment details:**
   - **Source:** GitHub → **Sign in with GitHub** and authorize Azure.
   - **Organization:** your GitHub username · **Repository:** `job-tracker` · **Branch:** `main`.
4. **Build details:**
   - **Build presets:** Angular
   - **App location:** `./job-tracker-ui` (the folder containing `package.json`; `./` means "from the repository root")
   - **Api location:** leave **empty** (the API is the separate App Service)
   - **Output location:** `dist/job-tracker-ui/browser` (relative to the app location)
5. **Review + create**. Confirm **SKU: Free** → **Create**.

When it finishes, Azure has done three things:
- Created the Static Web App.
- Added a [GitHub secret](#github-secret) named `AZURE_STATIC_WEB_APPS_API_TOKEN_<RANDOM_WORDS>` to your repository (the deploy credential).
- **Committed a new [workflow](#workflow) file** to your repository, `.github/workflows/azure-static-web-apps-<random-words>.yml`, and started its first run.

### 18.5 Get the new workflow file locally

Azure's commit exists on GitHub but not on your computer yet. Pull it now, or your next push will be rejected:

```bash
git pull origin main
```

The generated workflow looks like this. **Don't change it.** The secret name in yours will differ.

```yaml
name: Azure Static Web Apps CI/CD

on:
  push:
    branches:
      - main
  pull_request:
    types: [opened, synchronize, reopened, closed]
    branches:
      - main

jobs:
  build_and_deploy_job:
    if: github.event_name == 'push' || (github.event_name == 'pull_request' && github.event.action != 'closed')
    runs-on: ubuntu-latest
    name: Build and Deploy Job
    steps:
      - uses: actions/checkout@v3
        with:
          submodules: true
          lfs: false
      - name: Build And Deploy
        id: builddeploy
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN_<RANDOM_WORDS> }}
          repo_token: ${{ secrets.GITHUB_TOKEN }} # Used for Github integrations (i.e. PR comments)
          action: "upload"
          app_location: "./job-tracker-ui" # App source code path
          api_location: "" # Api source code path - optional
          output_location: "dist/job-tracker-ui/browser" # Built app content directory - optional

  close_pull_request_job:
    if: github.event_name == 'pull_request' && github.event.action == 'closed'
    runs-on: ubuntu-latest
    name: Close Pull Request Job
    steps:
      - name: Close Pull Request
        id: closepullrequest
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN_<RANDOM_WORDS> }}
          action: "close"
```

- The `Azure/static-web-apps-deploy` action runs `npm install` and `npm run build` inside `app_location`, then uploads `output_location`. The build is done by [Oryx](#oryx), Microsoft's automatic build tool.
- On pull requests, it also creates a temporary preview site, and closes it when the pull request closes.

**Check:** on GitHub → **Actions**, the **Azure Static Web Apps CI/CD** run is green. If the build step fails with a Node.js version error, see [Troubleshooting](#troubleshooting).

### 18.6 Allow the new site in the API's CORS settings

1. Open your Static Web App → **Overview** → copy the **URL** (for example `https://victorious-pebble-08457a31e.3.azurestaticapps.net`).
2. Open the site. The header appears, but the list says **"Could not load applications"**, and the browser console (Cmd+Option+I on macOS, F12 on Windows → **Console**) shows a [CORS](#cors) error. That's expected: the API only allows `http://localhost:4200` so far.
3. In the Azure portal, open **`jobtracker-api`** (the App Service) → **Settings** → **Environment variables** → **App settings** → **+ Add**:
   - **Name:** `Cors__AllowedOrigins__1`
   - **Value:** the Static Web App URL, including `https://` and **without a trailing slash**
4. **Apply** → **Apply** → **Confirm**. The API restarts.

Why `__1`: `appsettings.json` already defines `AllowedOrigins` as a list whose item **0** is `http://localhost:4200`. In .NET [configuration](#configuration), a number after `__` is a list position, so `__1` **adds** a second origin. Using `__0` would **replace** localhost and break local development. No code change or redeploy is needed, because `Program.cs` reads this list at startup.

### 18.7 Test the live site

Wait about 30 seconds, then refresh the Static Web App URL and check each feature:

1. The list loads with colored badges.
2. The status filter works.
3. **Add application** creates a row.
4. **Edit** changes a status, and the badge updates.
5. **Delete** removes a row.
6. On an Edit page, press refresh (Cmd+R / F5). The page reloads correctly instead of returning 404. That's the fallback file from 18.1 at work.

---

## Stage 19: Visual redesign

Stages 13–18 built a working first version with a plain look. This stage gives the app its own identity: warm off-white background, a navy header band, amber accents, status **dots** instead of pills, and clickable **status chips** (the chips are finished in Stage 23, which contains the final list component).

> **How to read Stages 19–24.** They evolve the app built in Stages 1–18. When a stage says **"replace the contents"** of a file you created earlier, the newer version is the one to keep. Stage 23 contains the **final** version of every Angular file that changed.

### 19.1 Load the fonts: `job-tracker-ui/src/index.html`

The design uses three [Google Fonts](#google-fonts): **Sora** (headings), **Source Sans 3** (body text) and **IBM Plex Mono** (dates and counts).

**Replace the contents:**
```html
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <title>Job Tracker</title>
  <base href="/">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <link rel="icon" type="image/x-icon" href="favicon.ico">
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
  <link href="https://fonts.googleapis.com/css2?family=IBM+Plex+Mono:wght@400;500&family=Sora:wght@500;600;700&family=Source+Sans+3:wght@400;500;600&display=swap" rel="stylesheet">
</head>
<body>
  <app-root></app-root>
</body>
</html>
```

### 19.2 One palette for the whole app: `job-tracker-ui/src/styles.scss`

Every color, font and shadow is defined once as a [CSS variable](#css-variables) on `:root`, then used everywhere with `var(--name)`. Changing a color later is a one-line edit. The status colors (`--st-…`) and the `.dot--…` classes live here because three components use them (the badge, the chips and the pipeline bar).

**Replace the contents:**
```scss
:root {
  --navy: #1c2b4d;
  --navy-text: #f4f1ea;
  --navy-muted: #c9d1e3;
  --amber: #e8a33d;
  --amber-ink: #1c1406;
  --bg: #f7f5f1;
  --surface: #ffffff;
  --text: #1a2235;
  --muted: #4f5870;
  --border: #dcdfe6;
  --link: #1d3a6e;
  --danger: #a3281c;
  --shadow: 0 1px 2px rgba(20, 30, 60, 0.06), 0 8px 24px rgba(20, 30, 60, 0.06);

  --st-applied: #7d869c;
  --st-phonescreen: #2f6bc0;
  --st-interview: #7a4fc4;
  --st-takehome: #c98a1a;
  --st-offer: #2e8b57;
  --st-rejected: #c4473a;
  --st-withdrawn: #c5c9d3;

  --font-display: 'Sora', system-ui, sans-serif;
  --font-body: 'Source Sans 3', system-ui, sans-serif;
  --font-mono: 'IBM Plex Mono', ui-monospace, monospace;
}

body {
  margin: 0;
  font-family: var(--font-body);
  color: var(--text);
  background: var(--bg);
}

a { color: var(--link); }

.mono {
  font-family: var(--font-mono);
  font-size: 0.85rem;
  color: var(--muted);
}

/* Buttons */
.btn {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  min-height: 44px;
  box-sizing: border-box;
  padding: 0.6rem 1.2rem;
  border-radius: 999px;
  border: 1px solid transparent;
  font: inherit;
  font-size: 0.95rem;
  font-weight: 600;
  text-decoration: none;
  cursor: pointer;
}
.btn--accent { background: var(--amber); color: var(--amber-ink); }
.btn--accent:hover { filter: brightness(1.05); }
.btn--primary { background: var(--navy); color: var(--navy-text); }
.btn--primary:hover { filter: brightness(1.15); }
.btn--primary:disabled { opacity: 0.6; cursor: default; }
.btn--ghost { background: var(--surface); color: var(--text); border-color: var(--border); }
.btn--ghost:hover { background: #f1f2f6; }

/* Status dots, shared by the badge, chips and pipeline bar */
.dot {
  display: inline-block;
  width: 9px;
  height: 9px;
  border-radius: 999px;
  flex-shrink: 0;
}
.dot--applied     { background: var(--st-applied); }
.dot--phonescreen { background: var(--st-phonescreen); }
.dot--interview   { background: var(--st-interview); }
.dot--takehome    { background: var(--st-takehome); }
.dot--offer       { background: var(--st-offer); }
.dot--rejected    { background: var(--st-rejected); }
.dot--withdrawn   { background: var(--st-withdrawn); box-shadow: inset 0 0 0 1px #9aa0ae; }
```

| Rule | Purpose |
|---|---|
| `:root { --navy: …; }` | Declares the variables. `:root` means "the whole page" |
| `.btn--accent`, `.btn--primary`, `.btn--ghost` | Three button styles (amber, navy, outlined) used across the app |
| `.dot` + `.dot--<status>` | A small colored circle per status, e.g. `.dot--phonescreen` |
| `.mono` | Monospace text for dates, so digits line up in columns |

### 19.3 Status badge as a dot

The badge now shows a colored dot plus the friendly label. The dot colors come from `styles.scss`.

**`job-tracker-ui/src/app/components/status-badge/status-badge.ts` — replace the contents:**
```typescript
import { Component, computed, input } from '@angular/core';
import { ApplicationStatus, STATUS_LABELS } from '../../models/application';

@Component({
  selector: 'app-status-badge',
  templateUrl: './status-badge.html',
  styleUrl: './status-badge.scss'
})
export class StatusBadge {
  readonly status = input.required<ApplicationStatus>();

  protected readonly label = computed(() => STATUS_LABELS[this.status()]);
  protected readonly dotClass = computed(() => `dot dot--${this.status().toLowerCase()}`);
}
```

**`status-badge.html` — replace the contents:**
```html
<span class="status"><span [class]="dotClass()"></span>{{ label() }}</span>
```

**`status-badge.scss` — replace the contents:**
```scss
.status {
  display: inline-flex;
  align-items: center;
  gap: 7px;
  font-size: 0.92rem;
  font-weight: 500;
  white-space: nowrap;
}
```

`dotClass` builds a class string such as `dot dot--phonescreen` from the status, so each status picks up its color automatically.

### 19.4 Form styles: `job-tracker-ui/src/app/components/application-form/application-form.scss`

The form now sits in a white card, uses the palette variables, and shows an amber outline on the focused field. The form's HTML and TypeScript don't change.

**Replace the contents:**
```scss
.form-page {
  max-width: 560px;
  margin: 0 auto;
  padding: 28px 24px;
  box-sizing: border-box;
}

.form-page h2 {
  margin: 0 0 20px;
  font-family: var(--font-display);
  font-size: 1.5rem;
}

form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  background: var(--surface);
  border-radius: 16px;
  padding: 24px;
  box-shadow: var(--shadow);
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

label {
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--text);
}

input,
select {
  min-height: 44px;
  box-sizing: border-box;
  padding: 0.5rem 0.7rem;
  border: 1px solid var(--border);
  border-radius: 10px;
  font: inherit;
  background: var(--surface);
  color: var(--text);
}

input:focus,
select:focus {
  outline: 2px solid var(--amber);
  outline-offset: 1px;
}

.muted { color: var(--muted); }

input.ng-touched.ng-invalid { border-color: var(--danger); }

.field-error,
.error {
  color: var(--danger);
  font-size: 0.85rem;
}

.actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.6rem;
  margin-top: 0.5rem;
}
```

### 19.5 Add the author portrait

The "Built by" card and the login page show a small portrait. Images should be resized before they go into a web app: a 3 MB photo displayed at 96 px wastes bandwidth and slows the first load.

On macOS, the built-in `sips` tool resizes and converts in one command. From the repository root:

```bash
sips -s format jpeg -s formatOptions 80 -Z 240 "<path to your image>.png" --out job-tracker-ui/public/juan-avatar.jpg
ls -lh job-tracker-ui/public/juan-avatar.jpg
```

- `-Z 240` resizes so the longest side is at most 240 px; `formatOptions 80` sets JPEG quality; `--out` writes the new file.
- The result should be roughly **20–40 KB**.
- Files in `public/` are served from the site root, so the page refers to it simply as `juan-avatar.jpg`.
- Keep the large original **outside** the repository so it's never committed by accident.

On Windows, use any image editor (Paint can resize and "Save as" JPEG).

**Check:** with `ng serve` running, the page background turns warm off-white and the fonts change. The header and list get their final look in Stage 23.

---

## Stage 20: Middleware — request logging and global error handling

[Middleware](#middleware) is code that every HTTP request passes through, in order, before it reaches a controller — and that every response passes back through on the way out. Think of layers of an onion: each layer can act before and after the inner layers run. This stage adds two layers of your own.

| Layer | What it does |
|---|---|
| **Request logging** (outermost) | Starts a stopwatch, lets the request through, then logs the method, path, query string, status code and duration. |
| **Global error handling** (just inside CORS) | Wraps everything deeper in a `try/catch`. Any unhandled exception becomes a clean JSON error ([ProblemDetails](#problemdetails)) with the right status code. |

**Why the error handler must sit after `UseCors`.** The CORS middleware adds the `Access-Control-Allow-Origin` header *before* passing the request inward. The error handler then writes its error response **without clearing the headers**, so the CORS header survives and the browser shows the real error (404, 500) instead of a misleading CORS error. ASP.NET Core's default error handling clears headers, which is what caused the "CORS error that wasn't CORS" problem in [Troubleshooting](#troubleshooting).

### 20.1 The error handler: `JobTracker.Api/Middleware/ExceptionHandlingMiddleware.cs`

Create the folder `JobTracker.Api/Middleware` and this file:
```csharp
using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(ex, "Exception after the response started for {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                throw;
            }

            var (statusCode, title) = ex switch
            {
                KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
                ArgumentException    => (StatusCodes.Status400BadRequest, "Invalid request"),
                _                    => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
            };

            if (statusCode == StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            else
                _logger.LogWarning("Handled {ExceptionType} for {Method} {Path}: {Message}",
                    ex.GetType().Name, context.Request.Method, context.Request.Path, ex.Message);

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = statusCode == StatusCodes.Status500InternalServerError && !_environment.IsDevelopment()
                    ? null
                    : ex.Message,
                Instance = context.Request.Path
            };
            problem.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
        }
    }
}
```

| Code | What it does |
|---|---|
| `RequestDelegate _next` | The next layer in the pipeline. `await _next(context)` passes the request inward. |
| `InvokeAsync(HttpContext context)` | ASP.NET Core calls this for every request. The name and signature are a convention it looks for. |
| `try { await _next(context); } catch` | Anything thrown deeper (controller, AutoMapper, EF Core) lands here. |
| `context.Response.HasStarted` | If bytes were already sent, the status code can't change any more, so log and rethrow. |
| `ex switch { … }` | A C# [switch expression](#switch-expression) mapping exception types to status codes. The repository throws `KeyNotFoundException` for a missing id, so DELETE or PATCH of a missing record now returns **404** instead of **500**. |
| `Detail = … null` for 500s outside Development | Internal error messages are never shown to the public; they go to the logs instead. |
| `traceId` | A unique id per request. If a user reports an error, this id finds the exact log line. |
| No `Response.Clear()` | **This is what keeps the CORS header.** |

### 20.2 The request logger: `JobTracker.Api/Middleware/RequestLoggingMiddleware.cs`

```csharp
using System.Diagnostics;

namespace JobTracker.Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation("HTTP {Method} {Path}{Query} responded {StatusCode} in {ElapsedMs} ms",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
```

- `Stopwatch` is .NET's high-precision timer.
- `finally` runs even if something throws, so **every** request is logged.
- `{Method}`, `{Path}`, … are [structured logging](#structured-logging) placeholders: each value is stored as a named field, so log tools can filter by them (for example "every request slower than 1000 ms"). Never build log messages with `$"…"` interpolation, which loses that structure.

### 20.3 Register them in `Program.cs`

The two classes only run once they're added to the pipeline with `app.UseMiddleware<T>()`, and **the order of the `app.Use…` lines is the order of the layers**. The complete final `Program.cs` is in Stage 22.4; the pipeline part looks like this:

```csharp
app.UseMiddleware<RequestLoggingMiddleware>();      // 1. outermost: times and logs every request
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("AngularClient");                       // 2. CORS headers before going inward
app.UseMiddleware<ExceptionHandlingMiddleware>();   // 3. errors → ProblemDetails, CORS kept
app.UseAuthentication();                            // 4. (Stage 22) reads the token: who is this?
app.UseAuthorization();                             // 5. checks permissions: are they allowed?
app.MapControllers();                               // 6. innermost: your controllers
```

Add `using JobTracker.Api.Middleware;` at the top of `Program.cs`. If you're following along before Stage 22, leave out `UseAuthentication()` for now.

### 20.4 Check locally

```bash
dotnet build
dotnet test
dotnet run --project JobTracker.Api
```

1. In Swagger, run **GET /api/applications**. The terminal shows a line such as:
   ```
   info: JobTracker.Api.Middleware.RequestLoggingMiddleware[0]
         HTTP GET /api/applications responded 200 in 143 ms
   ```
2. Run **DELETE /api/applications/99999** (an id that doesn't exist). You get **404** with a body like:
   ```json
   {
     "title": "Resource not found",
     "status": 404,
     "detail": "Application 99999 not found.",
     "instance": "/api/applications/99999",
     "traceId": "0HN8..."
   }
   ```
   The terminal shows the error handler's warning first, then the logger's line — the inner layer finishes before the outer one.

> If you get **500** with `"detail": "08P01: SASL authentication failed"`, the middleware is working correctly and the database password in your [user secrets](#user-secrets) is wrong (common right after changing the database password). Copy the current connection string from the App Service setting and set it again with `dotnet user-secrets set`.

### 20.5 See the logs in Azure

Linux App Services don't keep console output until application logging is turned on:

1. Portal → `jobtracker-api` → **Monitoring** → **App Service logs**.
2. **Application logging:** **File System** · **Retention period (days):** `3` → **Save**.
3. **Monitoring** → **Log stream**, then use the live app in another tab. One line appears per request.

---

## Stage 21: Server-side search and pagination (API)

Until now, `GET /api/applications` returned **every** row and the browser filtered them. That's fine for 7 rows and wasteful for 7,000. In this stage the API accepts options in the URL and returns **one page**:

```
GET /api/applications?search=engineer&status=Applied&page=2&pageSize=5
```

PostgreSQL does the searching, filtering and paging, so only the requested rows travel over the network. The response also carries the **total count** (for "Page 2 of 4") and the **count per status** (for the chips and the pipeline bar).

> **This changes what the endpoint returns.** The Angular app must be updated at the same time (Stage 23), so build both locally and push them together.

### 21.1 The new types

**`JobTracker.Core/Models/ApplicationPage.cs`** — how the repository hands back one page:
```csharp
namespace JobTracker.Core.Models;

public record ApplicationPage(
    IReadOnlyList<Application> Items,
    int TotalCount,
    IReadOnlyDictionary<ApplicationStatus, int> StatusCounts);
```

A [record](#record) is a C# type made for carrying data: one line declares the properties and the constructor, and it's immutable by default.

**`JobTracker.Core/DTOs/ApplicationQueryParameters.cs`** — the options read from the URL:
```csharp
namespace JobTracker.Core.DTOs;

public class ApplicationQueryParameters
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
```

ASP.NET Core fills this from the query string through [model binding](#model-binding). Values the URL leaves out keep their defaults (page 1, 10 per page).

**`JobTracker.Core/DTOs/PagedApplicationsDto.cs`** — the JSON the API returns:
```csharp
namespace JobTracker.Core.DTOs;

public class PagedApplicationsDto
{
    public IEnumerable<ApplicationDto> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int AllCount { get; set; }
    public Dictionary<string, int> StatusCounts { get; set; } = [];
}
```

`AllCount` feeds the **All** chip (everything matching the search, in any status). `StatusCounts` uses text keys (`"PhoneScreen"`) so the JSON is readable.

### 21.2 The repository interface: `JobTracker.Core/Interfaces/IApplicationRepository.cs`

**Replace the contents** (one new method, `GetPagedAsync`):
```csharp
using JobTracker.Core.Models;

namespace JobTracker.Core.Interfaces;

public interface IApplicationRepository
{
    Task<ApplicationPage> GetPagedAsync(string? search, ApplicationStatus? status, int page, int pageSize);
    Task<IEnumerable<Application>> GetAllAsync();
    Task<Application?> GetByIdAsync(int id);
    Task<Application> CreateAsync(Application application);
    Task<Application> UpdateAsync(Application application);
    Task<Application> PatchStatusAsync(int id, ApplicationStatus status);
    Task DeleteAsync(int id);
}
```

### 21.3 The repository: `JobTracker.Infrastructure/Repositories/ApplicationRepository.cs`

**Replace the contents:**
```csharp
using JobTracker.Core.Interfaces;
using JobTracker.Core.Models;
using JobTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Infrastructure.Repositories;

public class ApplicationRepository : IApplicationRepository
{
    private readonly ApplicationDbContext _context;

    public ApplicationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Application>> GetAllAsync()
        => await _context.Applications
                         .AsNoTracking()
                         .OrderByDescending(a => a.AppliedDate)
                         .ToListAsync();

    public async Task<Application?> GetByIdAsync(int id)
        => await _context.Applications
                         .AsNoTracking()
                         .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<ApplicationPage> GetPagedAsync(string? search, ApplicationStatus? status, int page, int pageSize)
    {
        IQueryable<Application> query = _context.Applications.AsNoTracking();

        // 1. Search: case-insensitive match on company, position or country
        if (!string.IsNullOrWhiteSpace(search))
        {
            var escaped = search.Trim()
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
            var pattern = $"%{escaped}%";

            query = query.Where(a =>
                EF.Functions.ILike(a.Company, pattern) ||
                EF.Functions.ILike(a.Position, pattern) ||
                EF.Functions.ILike(a.Country, pattern));
        }

        // 2. Count per status (after search, before the status filter) for the chips
        var statusCounts = await query
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        // 3. Status filter
        if (status is not null)
        {
            var selected = status.Value;
            query = query.Where(a => a.Status == selected);
        }

        // 4. Total rows matching search + status (for "Page X of Y")
        var totalCount = await query.CountAsync();

        // 5. Sort and take one page
        var items = await query
            .OrderByDescending(a => a.AppliedDate)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new ApplicationPage(items, totalCount, statusCounts);
    }

    public async Task<Application> CreateAsync(Application application)
    {
        _context.Applications.Add(application);
        await _context.SaveChangesAsync();
        return application;
    }

    public async Task<Application> UpdateAsync(Application application)
    {
        _context.Applications.Update(application);
        await _context.SaveChangesAsync();
        return application;
    }

    public async Task<Application> PatchStatusAsync(int id, ApplicationStatus status)
    {
        var application = await _context.Applications.FindAsync(id)
            ?? throw new KeyNotFoundException($"Application {id} not found.");

        application.Status = status;
        application.StatusDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return application;
    }

    public async Task DeleteAsync(int id)
    {
        var application = await _context.Applications.FindAsync(id)
            ?? throw new KeyNotFoundException($"Application {id} not found.");

        _context.Applications.Remove(application);
        await _context.SaveChangesAsync();
    }
}
```

`GetPagedAsync` builds one query step by step: start with all rows → narrow by search → count per status → narrow by status → count the total → sort → take one page.

| Part | Meaning | SQL it becomes (roughly) |
|---|---|---|
| `IQueryable<Application> query` | An [IQueryable](#iqueryable): a query **under construction**. Nothing runs until a method like `ToListAsync()` | — |
| `EF.Functions.ILike(a.Company, pattern)` | PostgreSQL's **case-insensitive** LIKE ([ILIKE](#ilike)) | `WHERE "Company" ILIKE '%engineer%'` |
| `.Replace("%", "\\%")`… | **Escaping**: a search for `50%` treats `%` as a literal character, not a wildcard | — |
| `GroupBy(a => a.Status)` + `Count()` | Counts per status, in the database. Done **before** the status filter so other chips keep their numbers while one is selected | `SELECT "Status", COUNT(*) … GROUP BY "Status"` |
| `CountAsync()` | Total matching rows across all pages | `SELECT COUNT(*) …` |
| `ThenByDescending(a => a.Id)` | A tiebreaker: rows with the same date otherwise come back in an unpredictable order, and a row could appear on two pages | `ORDER BY …, "Id" DESC` |
| `Skip((page - 1) * pageSize)` / `Take(pageSize)` | [Pagination](#pagination): skip earlier pages, return one page | `OFFSET … LIMIT …` |

`EF.Functions.ILike` comes from the Npgsql provider and works with PostgreSQL only.

### 21.4 The controller's `GetAll`

The complete final controller is in Stage 22.3. The new `GetAll` reads the query parameters, validates them, and asks the repository for one page:

```csharp
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ApplicationQueryParameters query)
    {
        ApplicationStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            !string.Equals(query.Status, "All", StringComparison.OrdinalIgnoreCase))
        {
            if (!Enum.TryParse<ApplicationStatus>(query.Status, out var parsed) ||
                !Enum.IsDefined(parsed))
                return BadRequest($"Invalid status: {query.Status}");

            status = parsed;
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var result = await _repo.GetPagedAsync(query.Search, status, page, pageSize);

        return Ok(new PagedApplicationsDto { … });
    }
```

- `[FromQuery]` fills `query` from the URL.
- `Enum.IsDefined` rejects numbers such as `?status=42`, which `TryParse` would otherwise accept.
- `Math.Max(1, …)` blocks page 0 or negative pages; `Math.Clamp(…, 1, 50)` stops anyone requesting a million rows at once.
- `Math.Ceiling(total / (double)pageSize)` gives 7 rows ÷ 5 = **2 pages**. The `(double)` cast avoids integer division.

### 21.5 Check in Swagger

Run the API locally and open **GET /api/applications** in Swagger. It now shows four inputs: **Search**, **Status**, **Page**, **PageSize**.

| # | Fill in | Expect |
|---|---|---|
| 1 | nothing | Up to 10 items, plus `totalCount`, `totalPages`, `allCount`, `statusCounts` |
| 2 | PageSize `3`, Page `3` | Only the last row(s); no row repeats across pages |
| 3 | Search `engineer` | Only matching rows, in any capitalization |
| 4 | Status `Withdrawn` | Only Withdrawn rows, while `statusCounts` still lists every status |
| 5 | Status `NotReal` | **400** `Invalid status: NotReal` |
| 6 | PageSize `500` | Capped at 50 per page |

---

## Stage 22: Authentication with JWT and roles (API)

The app now requires a login, with two roles:

| Role | Can do |
|---|---|
| **Admin** | Everything: read, add, edit, change status, delete |
| **ReadOnly** | Read, search and filter only |

How it works:
1. The user sends a username and password to `POST /api/auth/login`.
2. The API checks the password against a stored **hash** and, if it matches, returns a [JWT](#jwt): a small signed "pass" containing the user's id, username, role and an expiry time **5 minutes** later.
3. The browser sends that token with every request in an `Authorization: Bearer <token>` header.
4. The API checks the token's signature and expiry, then allows or refuses the request based on the role.

Because the token itself proves who the caller is, the server keeps no session — JWT authentication is **stateless**.

Users are stored in your own `Users` table, with passwords **hashed** — never stored as plain text. A [hash](#password-hashing) is a one-way scramble: the API can check "does this password produce the same hash?", but nobody can turn the hash back into the password.

### 22.1 Add the JWT package

From the repository root (in zsh, keep the quotes around the version):
```bash
dotnet add JobTracker.Api package Microsoft.AspNetCore.Authentication.JwtBearer --version "8.0.*"
```

### 22.2 Users in the database

**`JobTracker.Core/Models/User.cs`:**
```csharp
namespace JobTracker.Core.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.ReadOnly;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum UserRole
{
    Admin,
    ReadOnly
}
```

**`JobTracker.Infrastructure/Data/ApplicationDbContext.cs` — replace the contents** (adds the `Users` table):
```csharp
using JobTracker.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Application> Applications => Set<Application>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Company)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.Country)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(e => e.Position)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.Status)
                  .HasConversion<int>();   // store enum as integer

            entity.Property(e => e.StatusDate)
                  .HasDefaultValueSql("NOW()");
        });
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Username)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.HasIndex(e => e.Username)
                  .IsUnique();

            entity.Property(e => e.PasswordHash)
                  .IsRequired();

            entity.Property(e => e.Role)
                  .HasConversion<string>()
                  .HasMaxLength(20);
        });        
    }
}
```

- `HasIndex(e => e.Username).IsUnique()` makes the database itself refuse two users with the same username.
- `HasConversion<string>()` stores the role as readable text (`"Admin"`), not a number.

**Create and apply the migration:**
```bash
dotnet build
dotnet ef migrations add AddUsers -p JobTracker.Infrastructure -s JobTracker.Api
dotnet ef database update -p JobTracker.Infrastructure -s JobTracker.Api
```
`database update` uses the connection string in your user secrets. The migration only **adds** a table, so the live app keeps working.

**Settings: `JobTracker.Api/appsettings.json` — replace the contents:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Cors": {
  "AllowedOrigins": [
      "http://localhost:4200",
      "https://victorious-pebble-08457a31e.4.azurestaticapps.net",
      "https://jobtracker.juanmunoz.dev"
    ]
  },
  "DemoUsers": {
    "Admin":    { "Username": "admin", "Password": "Admin123!" },
    "ReadOnly": { "Username": "demo",  "Password": "Demo123!" }
  },
  "Jwt": {
    "Issuer": "jobtracker-api",
    "Audience": "jobtracker-ui",
    "ExpiryMinutes": 5
  },    
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- `Cors:AllowedOrigins` lists every address allowed to call the API: local development, the Static Web App, and the custom domain. (Origins can also be added in Azure as `Cors__AllowedOrigins__N` app settings, as in Stage 18.6.)
- `DemoUsers` are deliberately public: this demo uses sample data, and the login page shows these credentials so visitors can try both roles. **Never do this with real accounts.**
- `Jwt:Issuer` (who created the token), `Jwt:Audience` (who it's for) and `Jwt:ExpiryMinutes` are public settings.
- **The signing key is not here.** Whoever has it can create valid tokens for any role, so it's a secret.

**Store the signing key in user secrets** (a random 48-byte key):
```bash
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)" --project JobTracker.Api
```
`openssl rand -base64 48` generates the random key; `$( … )` inserts it into the command. On Windows without OpenSSL, generate a long random string with a password manager instead.

**Demo users created on startup: `JobTracker.Api/Auth/DemoUserSeeder.cs`:**
```csharp
using JobTracker.Core.Models;
using JobTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Auth;

public static class DemoUserSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DemoUserSeeder");
        var hasher = new PasswordHasher<User>();

        var demoUsers = new[]
        {
            (Username: config["DemoUsers:Admin:Username"] ?? "admin",
             Password: config["DemoUsers:Admin:Password"] ?? "Admin123!",
             Role: UserRole.Admin),
            (Username: config["DemoUsers:ReadOnly:Username"] ?? "demo",
             Password: config["DemoUsers:ReadOnly:Password"] ?? "Demo123!",
             Role: UserRole.ReadOnly)
        };

        foreach (var (username, password, role) in demoUsers)
        {
            if (await db.Users.AnyAsync(u => u.Username == username))
                continue;

            var user = new User { Username = username, Role = role };
            user.PasswordHash = hasher.HashPassword(user, password);
            db.Users.Add(user);

            logger.LogInformation("Seeded demo user {Username} ({Role})", username, role);
        }

        await db.SaveChangesAsync();
    }
}
```

| Code | Meaning |
|---|---|
| `services.CreateScope()` | The `DbContext` is registered per request. At startup there's no request, so a temporary scope is created. |
| `PasswordHasher<User>` | ASP.NET Core's built-in hasher: [PBKDF2](#password-hashing) with a random **salt** and many iterations, so the same password produces a different hash every time and guessing is slow. |
| `AnyAsync(...)` then `continue` | Only adds users that don't exist, so it's safe to run on every startup. |

### 22.3 Login: DTOs, repository, token service and controllers

**`JobTracker.Core/DTOs/LoginRequestDto.cs`:**
```csharp
namespace JobTracker.Core.DTOs;

public class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

**`JobTracker.Core/DTOs/LoginResponseDto.cs`:**
```csharp
namespace JobTracker.Core.DTOs;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
```

`ExpiresAt`, `Username` and `Role` are returned with the token so the Angular app can show who's signed in, hide admin buttons and schedule the automatic logout without decoding the token.

**`JobTracker.Core/Interfaces/IUserRepository.cs`:**
```csharp
using JobTracker.Core.Models;

namespace JobTracker.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
}
```

**`JobTracker.Infrastructure/Repositories/UserRepository.cs`:**
```csharp
using JobTracker.Core.Interfaces;
using JobTracker.Core.Models;
using JobTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByUsernameAsync(string username)
        => await _context.Users
                         .AsNoTracking()
                         .FirstOrDefaultAsync(u => u.Username == username);
}
```

**`JobTracker.Api/Auth/JwtTokenService.cs`** — creates the token:
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JobTracker.Core.Models;
using Microsoft.IdentityModel.Tokens;

namespace JobTracker.Api.Auth;

public class JwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    public (string Token, DateTime ExpiresAt) CreateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.AddMinutes(_config.GetValue("Jwt:ExpiryMinutes", 5));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
```

| Code | Meaning |
|---|---|
| `SymmetricSecurityKey` | The secret key as bytes. "Symmetric": the same key signs and verifies. |
| `HmacSha256` | The signing algorithm (HS256). |
| `Claim(Sub, …)`, `Claim(ClaimTypes.Name, …)`, `Claim(ClaimTypes.Role, …)` | [Claims](#claims): facts about the user carried inside the token. `[Authorize(Roles = "Admin")]` reads the role claim. |
| `Claim(Jti, Guid)` | A unique id per token. |
| `WriteToken(token)` | Produces the final `header.payload.signature` string. |

> The payload of a JWT is **encoded, not encrypted** — anyone can read it (try pasting one into jwt.io). The signature only makes it impossible to *change* without the key. Never put passwords or secrets inside a token.

**`JobTracker.Api/Controllers/AuthController.cs`:**
```csharp
using JobTracker.Api.Auth;
using JobTracker.Core.DTOs;
using JobTracker.Core.Interfaces;
using JobTracker.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly JwtTokenService _tokens;
    private readonly PasswordHasher<User> _hasher = new();

    public AuthController(IUserRepository users, JwtTokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto dto)
    {
        var user = await _users.GetByUsernameAsync(dto.Username.Trim());

        if (user is null ||
            _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password) == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var (token, expiresAt) = _tokens.CreateToken(user);

        return Ok(new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            Username = user.Username,
            Role = user.Role.ToString()
        });
    }
}
```

- `VerifyHashedPassword` hashes the submitted password with the stored salt and compares the result.
- **The same message for an unknown user and a wrong password** is deliberate: different messages would let attackers discover which usernames exist.

**`JobTracker.Api/Controllers/ApplicationsController.cs` — replace the contents** (final version, with pagination and role locks):
```csharp
using AutoMapper;
using JobTracker.Core.DTOs;
using JobTracker.Core.Interfaces;
using JobTracker.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
namespace JobTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationRepository _repo;
    private readonly IMapper _mapper;

    public ApplicationsController(IApplicationRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ApplicationQueryParameters query)
    {
        ApplicationStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            !string.Equals(query.Status, "All", StringComparison.OrdinalIgnoreCase))
        {
            if (!Enum.TryParse<ApplicationStatus>(query.Status, out var parsed) ||
                !Enum.IsDefined(parsed))
                return BadRequest($"Invalid status: {query.Status}");

            status = parsed;
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var result = await _repo.GetPagedAsync(query.Search, status, page, pageSize);

        return Ok(new PagedApplicationsDto
        {
            Items = _mapper.Map<IEnumerable<ApplicationDto>>(result.Items),
            Page = page,
            PageSize = pageSize,
            TotalCount = result.TotalCount,
            TotalPages = (int)Math.Ceiling(result.TotalCount / (double)pageSize),
            AllCount = result.StatusCounts.Values.Sum(),
            StatusCounts = result.StatusCounts.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value)
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var app = await _repo.GetByIdAsync(id);
        return app is null ? NotFound() : Ok(_mapper.Map<ApplicationDto>(app));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateApplicationDto dto)
    {
        var app = _mapper.Map<Application>(dto);
        var created = await _repo.CreateAsync(app);
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            _mapper.Map<ApplicationDto>(created));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, ApplicationDto dto)
    {
        if (!Enum.TryParse<ApplicationStatus>(dto.Status, out _))
            return BadRequest($"Invalid status: {dto.Status}");

        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
            return NotFound();

        _mapper.Map(dto, existing);
        var updated = await _repo.UpdateAsync(existing);
        return Ok(_mapper.Map<ApplicationDto>(updated));
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PatchStatus(int id, PatchStatusDto dto)
    {
        if (!Enum.TryParse<ApplicationStatus>(dto.Status, out var status))
            return BadRequest($"Invalid status: {dto.Status}");

        var patched = await _repo.PatchStatusAsync(id, status);
        return Ok(_mapper.Map<ApplicationDto>(patched));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _repo.DeleteAsync(id);
        return NoContent();
    }
}
```

| Endpoint | Who can call it |
|---|---|
| `POST /api/auth/login` | Anyone |
| `GET /api/applications`, `GET /api/applications/{id}` | Any signed-in user (`[Authorize]` on the class) |
| `POST`, `PUT`, `PATCH`, `DELETE` | Admin only (`[Authorize(Roles = "Admin")]` on each action) |

### 22.4 The final `Program.cs`

**`JobTracker.Api/Program.cs` — replace the contents:**
```csharp
using System.Text;
using JobTracker.Api.Auth;
using JobTracker.Api.Mappings;
using JobTracker.Api.Middleware;
using JobTracker.Core.Interfaces;
using JobTracker.Infrastructure.Data;
using JobTracker.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddAutoMapper(typeof(ApplicationProfile).Assembly);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<JwtTokenService>();

// --- JWT authentication ---
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key is missing or shorter than 32 bytes. Set it in user secrets or app settings.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();

                var expired = context.AuthenticateFailure is SecurityTokenExpiredException;
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = expired ? "Session expired" : "Authentication required",
                    Detail = expired
                        ? "Your session has expired. Please log in again."
                        : "Log in to get a token, then send it as 'Authorization: Bearer <token>'.",
                    Instance = context.Request.Path
                };

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
            },
            OnForbidden = async context =>
            {
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Detail = "Your account has read-only access. Log in as admin to make changes.",
                    Instance = context.Request.Path
                };

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
            }
        };        
    });

builder.Services.AddAuthorization();

// --- Swagger, with an "Authorize" button for testing tokens ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the token from POST /api/auth/login"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddPolicy("AngularClient", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

// Middleware pipeline — order matters.
app.UseMiddleware<RequestLoggingMiddleware>();      // 1. outermost: times and logs every request
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("AngularClient");                       // 2. CORS headers before going inward
app.UseMiddleware<ExceptionHandlingMiddleware>();   // 3. errors → ProblemDetails, CORS kept
app.UseAuthentication();                            // 4. reads the token: who is this?
app.UseAuthorization();                             // 5. checks permissions: are they allowed?
app.MapControllers();                               // 6. innermost: your controllers

try
{
    await DemoUserSeeder.SeedAsync(app.Services);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Demo user seeding failed");
}

app.Run();
```

| Part | Meaning |
|---|---|
| Key check + `throw` | **Fail fast**: if `Jwt:Key` is missing or too short, the API refuses to start with a clear message instead of running insecurely. |
| `TokenValidationParameters` | For every request with a token: check issuer, audience, signature and expiry. |
| `ClockSkew = 30 seconds` | The default tolerance is 5 minutes, which would silently turn a 5-minute token into 10. |
| `OnChallenge` | Runs when a login is required but the token is missing, invalid or expired → **401** as ProblemDetails, with "Session expired" when the token has expired. `HandleResponse()` stops ASP.NET Core adding its default empty response. |
| `OnForbidden` | Runs when the user is signed in but lacks the role → **403** as ProblemDetails. |
| Swagger security definition | Adds an **Authorize** button to Swagger for pasting a token. |
| `UseAuthentication()` before `UseAuthorization()` | First work out *who* the caller is, then *whether* they're allowed. |
| Seeder in `try/catch` | If the database is briefly unreachable at startup (free tiers sleep), the API still starts and logs the problem. |

### 22.5 Security tests: `JobTracker.Tests/Controllers/ApplicationsControllerTests.cs`

The controller tests call methods directly, which bypasses the `[Authorize]` attributes. The extra tests at the end check, through [reflection](#reflection), that the locks themselves are in place — so nobody can remove one without a failing test. The `GetAll` tests use the new paged result.

**Replace the contents:**
```csharp
using AutoMapper;
using FluentAssertions;
using JobTracker.Api.Controllers;
using JobTracker.Core.DTOs;
using JobTracker.Core.Interfaces;
using JobTracker.Core.Models;
using JobTracker.Api.Mappings;  
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Moq;

namespace JobTracker.Tests.Controllers;

public class ApplicationsControllerTests
{
    private readonly Mock<IApplicationRepository> _repoMock;
    private readonly IMapper _mapper;
    private readonly ApplicationsController _controller;

    public ApplicationsControllerTests()
    {
        _repoMock = new Mock<IApplicationRepository>();
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ApplicationProfile>());
        _mapper = config.CreateMapper();
        _controller = new ApplicationsController(_repoMock.Object, _mapper);
    }

    private static Application MakeApp(int id = 1) => new()
    {
        Id = id, Company = "Acme Corp", Country = "US",
        Position = "Senior Software Engineer",
        AppliedDate = new DateOnly(2026, 9, 21),
        Status = ApplicationStatus.Applied,
        StatusDate = DateTime.UtcNow
    };

    [Fact]
    public async Task GetAll_ReturnsOkWithPagedResult()
    {
        var counts = new Dictionary<ApplicationStatus, int> { [ApplicationStatus.Applied] = 1 };
        _repoMock.Setup(r => r.GetPagedAsync(null, null, 1, 10))
                 .ReturnsAsync(new ApplicationPage([MakeApp()], 1, counts));

        var result = await _controller.GetAll(new ApplicationQueryParameters());

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var page = ok.Value.Should().BeOfType<PagedApplicationsDto>().Subject;
        page.TotalCount.Should().Be(1);
        page.TotalPages.Should().Be(1);
        page.AllCount.Should().Be(1);
        page.StatusCounts["Applied"].Should().Be(1);
    }

    [Fact]
    public async Task GetAll_InvalidStatus_ReturnsBadRequest()
    {
        var result = await _controller.GetAll(new ApplicationQueryParameters { Status = "NotReal" });
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeApp());
        var result = await _controller.GetById(1);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_MissingId_ReturnsNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Application?)null);
        var result = await _controller.GetById(999);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ValidDto_ReturnsCreatedAtAction()
    {
        var dto = new CreateApplicationDto { Company = "Acme", Country = "US",
            Position = "Engineer", AppliedDate = new DateOnly(2026, 9, 21) };
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Application>()))
                 .ReturnsAsync(MakeApp());
        var result = await _controller.Create(dto);
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task PatchStatus_ValidStatus_ReturnsOk()
    {
        _repoMock.Setup(r => r.PatchStatusAsync(1, ApplicationStatus.Interview))
                 .ReturnsAsync(MakeApp());
        var result = await _controller.PatchStatus(1, new PatchStatusDto { Status = "Interview" });
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PatchStatus_InvalidStatus_ReturnsBadRequest()
    {
        var result = await _controller.PatchStatus(1, new PatchStatusDto { Status = "NotReal" });
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        _repoMock.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);
        var result = await _controller.Delete(1);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public void Controller_RequiresAuthentication()
    {
        typeof(ApplicationsController)
            .GetCustomAttribute<AuthorizeAttribute>()
            .Should().NotBeNull("every applications endpoint must require a logged-in user");
    }

    [Theory]
    [InlineData(nameof(ApplicationsController.Create))]
    [InlineData(nameof(ApplicationsController.Update))]
    [InlineData(nameof(ApplicationsController.PatchStatus))]
    [InlineData(nameof(ApplicationsController.Delete))]
    public void WriteActions_RequireAdminRole(string actionName)
    {
        var attribute = typeof(ApplicationsController)
            .GetMethod(actionName)!
            .GetCustomAttribute<AuthorizeAttribute>();

        attribute.Should().NotBeNull($"{actionName} must be protected");
        attribute!.Roles.Should().Be("Admin");
    }

    [Theory]
    [InlineData(nameof(ApplicationsController.GetAll))]
    [InlineData(nameof(ApplicationsController.GetById))]
    public void ReadActions_DoNotRequireAdminRole(string actionName)
    {
        typeof(ApplicationsController)
            .GetMethod(actionName)!
            .GetCustomAttribute<AuthorizeAttribute>()
            .Should().BeNull($"{actionName} should be readable by any logged-in user");
    }    
}
```

- `[Theory]` + `[InlineData(…)]` runs the same test once per value.
- `nameof(…)` gives a method name as text, checked by the compiler, so renaming a method can't silently break a test.

> Earlier versions of this repository contained a file `JobTracker.Tests/Repositories/ApplicationRepositoryTests.cs` that held a copy of the repository class instead of tests. It breaks the build as soon as the interface gains a method. If you have it, delete it: `git rm JobTracker.Tests/Repositories/ApplicationRepositoryTests.cs`.

### 22.6 Check in Swagger

```bash
dotnet build
dotnet test
dotnet run --project JobTracker.Api
```

In Swagger, use **Authorize** (top right) to paste a token — **only the token**, Swagger adds `Bearer ` itself — and **Logout** in the same dialog to remove it.

| # | Token | Call | Expect |
|---|---|---|---|
| 1 | none | GET /api/applications | **401** "Authentication required" |
| 2 | `demo` / `Demo123!` | GET /api/applications | **200** |
| 3 | `demo` | DELETE /api/applications/{id} | **403** "Your account has read-only access…" |
| 4 | `admin` / `Admin123!` | POST an application | **201** |
| 5 | `admin` | DELETE it | **204** |
| 6 | admin token, after waiting 5½ minutes | GET /api/applications | **401** "Session expired" |

The first run also logs `Seeded demo user admin (Admin)` and `Seeded demo user demo (ReadOnly)`.

---

## Stage 23: Angular — search, pagination, login and roles (final versions)

This stage brings the Angular app up to date with Stages 21 and 22. Every file below is shown in its **final** form.

| Piece | Job |
|---|---|
| **Models and services** | Ask the API for one page at a time; log in, keep the token, know the role, log out automatically when the token expires |
| **[Interceptor](#interceptor)** | Attach `Authorization: Bearer <token>` to every API call; log out when the API answers 401 |
| **[Guards](#route-guard)** | Keep pages behind login: the list needs any user, Add/Edit need Admin |
| **Login page** | Portrait, explanation, the two demo accounts with **Use** buttons, and an "expired" message |
| **Header** | Signed-in user and role, **Log out**, and **New application** for admins only |
| **List** | Debounced search, status chips and pipeline bar from server counts, pager, and Edit/Delete for admins only |

### 23.1 Run everything locally

While developing, Angular must talk to the API on your machine (the live API only changes when you push). You need three terminal tabs:

| Tab | Folder | Command |
|---|---|---|
| 1 | repository root | `dotnet run --project JobTracker.Api` |
| 2 | `job-tracker-ui` | `ng serve` |
| 3 | either | git and other commands |

### 23.2 Environment files

**`job-tracker-ui/src/environments/environment.development.ts`** (used by `ng serve`; points at the local API):
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5296/api',
  swaggerUrl: 'https://jobtracker-api-b5efh7c9g8crfwbh.westus3-01.azurewebsites.net/swagger',
  portfolioUrl: 'https://juanmunoz.dev',
  repoUrl: 'https://github.com/juanmunoz00/job-tracker'
};
```

**`job-tracker-ui/src/environments/environment.ts`** (used by production builds; points at Azure):
```typescript
export const environment = {
  production: true,   // false in environment.development.ts
  apiUrl: 'https://jobtracker-api-b5efh7c9g8crfwbh.westus3-01.azurewebsites.net/api',
  swaggerUrl: 'https://jobtracker-api-b5efh7c9g8crfwbh.westus3-01.azurewebsites.net/swagger',
  portfolioUrl: 'https://juanmunoz.dev',
  repoUrl: 'https://github.com/juanmunoz00/job-tracker'
};
```

Use the port your `dotnet run` shows. Each file must contain exactly **one** `export const environment` block, and both files need the **same property names** — a property missing from the development file gives `TS2339: Property '…' does not exist`.

### 23.3 Models

**`job-tracker-ui/src/app/models/application.ts` — replace the contents:**
```typescript
export type ApplicationStatus =
  | 'Applied'
  | 'PhoneScreen'
  | 'Interview'
  | 'TakeHome'
  | 'Offer'
  | 'Rejected'
  | 'Withdrawn';

export interface Application {
  id: number;
  company: string;
  country: string;
  position: string;
  appliedDate: string;
  status: ApplicationStatus;
  statusDate: string;
}

export interface CreateApplication {
  company: string;
  country: string;
  position: string;
  appliedDate: string;
}

export const STATUSES: ApplicationStatus[] = [
  'Applied',
  'PhoneScreen',
  'Interview',
  'TakeHome',
  'Offer',
  'Rejected',
  'Withdrawn'
];

export const STATUS_LABELS: Record<ApplicationStatus, string> = {
  Applied: 'Applied',
  PhoneScreen: 'Phone screen',
  Interview: 'Interview',
  TakeHome: 'Take-home',
  Offer: 'Offer',
  Rejected: 'Rejected',
  Withdrawn: 'Withdrawn'
};

export interface PagedApplications {
  items: Application[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  allCount: number;
  statusCounts: Partial<Record<ApplicationStatus, number>>;
}

export interface ApplicationQuery {
  search: string;
  status: ApplicationStatus | 'All';
  page: number;
  pageSize: number;
}
```

`PagedApplications` mirrors `PagedApplicationsDto`. `Partial<Record<…>>` means "an object with **some** of the status keys" — the API only includes statuses that have rows.

**`job-tracker-ui/src/app/models/auth.ts`:**
```typescript
export type UserRole = 'Admin' | 'ReadOnly';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;   // ISO date in UTC, e.g. "2026-11-05T03:10:00Z"
  username: string;
  role: UserRole;
}
```

### 23.4 Services

**`job-tracker-ui/src/app/services/application.service.ts` — replace the contents:**
```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Application, ApplicationQuery, ApplicationStatus, CreateApplication, PagedApplications } from '../models/application';

@Injectable({ providedIn: 'root' })
export class ApplicationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/applications`;

  getPage(query: ApplicationQuery): Observable<PagedApplications> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    if (query.search.trim()) {
      params = params.set('search', query.search.trim());
    }
    if (query.status !== 'All') {
      params = params.set('status', query.status);
    }

    return this.http.get<PagedApplications>(this.baseUrl, { params });
  }

  getById(id: number): Observable<Application> {
    return this.http.get<Application>(`${this.baseUrl}/${id}`);
  }

  create(app: CreateApplication): Observable<Application> {
    return this.http.post<Application>(this.baseUrl, app);
  }

  update(id: number, app: Application): Observable<Application> {
    return this.http.put<Application>(`${this.baseUrl}/${id}`, app);
  }

  patchStatus(id: number, status: ApplicationStatus): Observable<Application> {
    return this.http.patch<Application>(`${this.baseUrl}/${id}/status`, { status });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
```

- [HttpParams](#httpparams) builds the query string safely, including URL-encoding (a search like `C/C++` doesn't break the URL).
- `HttpParams` is immutable: `.set()` returns a **new** object, hence `params = params.set(…)`.
- `getAll()` is gone: the endpoint now returns a page, not a plain array.

**`job-tracker-ui/src/app/services/auth.service.ts`:**
```typescript
import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginRequest, LoginResponse } from '../models/auth';

const STORAGE_KEY = 'jobtracker.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly session = signal<LoginResponse | null>(null);
  private logoutTimer: ReturnType<typeof setTimeout> | null = null;

  readonly isLoggedIn = computed(() => this.session() !== null);
  readonly isAdmin = computed(() => this.session()?.role === 'Admin');
  readonly username = computed(() => this.session()?.username ?? '');

  constructor() {
    this.restore();
  }

  get token(): string | null {
    return this.session()?.token ?? null;
  }

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/auth/login`, credentials)
      .pipe(tap(response => this.start(response)));
  }

  logout(reason?: 'expired'): void {
    this.clearTimer();
    this.session.set(null);
    try { localStorage.removeItem(STORAGE_KEY); } catch { /* storage unavailable */ }
    this.router.navigate(['/login'], reason ? { queryParams: { reason } } : {});
  }

  private start(session: LoginResponse): void {
    const msLeft = new Date(session.expiresAt).getTime() - Date.now();
    if (msLeft <= 0) {
      this.logout('expired');
      return;
    }

    this.session.set(session);
    try { localStorage.setItem(STORAGE_KEY, JSON.stringify(session)); } catch { /* storage unavailable */ }

    this.clearTimer();
    this.logoutTimer = setTimeout(() => this.logout('expired'), msLeft);
  }

  private restore(): void {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return;

      const saved = JSON.parse(raw) as LoginResponse;
      if (new Date(saved.expiresAt).getTime() > Date.now()) {
        this.start(saved);
      } else {
        localStorage.removeItem(STORAGE_KEY);
      }
    } catch { /* corrupt or unavailable storage: start logged out */ }
  }

  private clearTimer(): void {
    if (this.logoutTimer) {
      clearTimeout(this.logoutTimer);
      this.logoutTimer = null;
    }
  }
}
```

| Code | Meaning |
|---|---|
| `session` signal | The current login (token, username, role, expiry) or `null`. |
| `isLoggedIn`, `isAdmin`, `username` | Computed signals read by templates and guards. |
| `login(…)` + `tap(…)` | Calls the login endpoint; `tap` runs `start()` with the response without changing it. |
| `start(…)` | Saves the session to [localStorage](#localstorage) (so a refresh keeps you signed in) and sets a timer for the exact expiry moment. |
| `setTimeout(…, msLeft)` | The **fixed 5-minute expiry**: when time is up, log out and go to `/login?reason=expired`. |
| `restore()` | On page load, restores a saved session **only if it hasn't expired**. |

> **About localStorage:** it's the simplest place for a token and common in demos. The trade-off: if an attacker ever injected script into the page ([XSS](#xss)), they could read it. Production systems often use HttpOnly cookies, which scripts can't read.

### 23.5 Interceptor, guards and app configuration

Create the folder `job-tracker-ui/src/app/interceptors` and **`auth.interceptor.ts`**:
```typescript
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token;
  const isApiCall = req.url.startsWith(environment.apiUrl);
  const isLoginCall = req.url.endsWith('/auth/login');

  const request = token && isApiCall
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && isApiCall && !isLoginCall) {
        auth.logout(token ? 'expired' : undefined);
      }
      return throwError(() => error);
    })
  );
};
```

- Requests are immutable, so `req.clone({ setHeaders: … })` makes a copy with the header added.
- The token is only sent to **your** API (`isApiCall`), never to other sites.
- On a 401 (other than a failed login), the user is logged out and sent to the login page.

Create the folder `job-tracker-ui/src/app/guards` and **`auth.guards.ts`**:
```typescript
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/** Any logged-in user (Admin or ReadOnly). */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isLoggedIn() ? true : router.createUrlTree(['/login']);
};

/** Admin only. Read-only users are sent back to the list. */
export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isLoggedIn()) return router.createUrlTree(['/login']);
  return auth.isAdmin() ? true : router.createUrlTree(['/']);
};

/** Login page only for logged-out visitors. */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isLoggedIn() ? router.createUrlTree(['/']) : true;
};
```

A guard runs **before** a route opens: returning `true` lets the user in, returning a `UrlTree` redirects. Guards are a convenience — anyone can bypass them in their own browser. **The real protection is `[Authorize]` on the API.**

**`job-tracker-ui/src/app/app.config.ts` — replace the contents:**
```typescript
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './interceptors/auth.interceptor';

export const appConfig: ApplicationConfig = {
   providers: [
     provideBrowserGlobalErrorListeners(),
     provideRouter(routes, withComponentInputBinding()),
         provideHttpClient(withFetch(), withInterceptors([authInterceptor]))
   ]
};
```

`withInterceptors([authInterceptor])` puts the interceptor in front of every HttpClient request.

**`job-tracker-ui/src/app/app.routes.ts` — replace the contents:**
```typescript
import { Routes } from '@angular/router';
import { ApplicationList } from './components/application-list/application-list';
import { ApplicationForm } from './components/application-form/application-form';
import { Login } from './components/login/login';
import { adminGuard, authGuard, guestGuard } from './guards/auth.guards';

export const routes: Routes = [
  { path: 'login', component: Login, canActivate: [guestGuard] },
  { path: '', component: ApplicationList, canActivate: [authGuard] },
  { path: 'add', component: ApplicationForm, canActivate: [adminGuard] },
  { path: 'edit/:id', component: ApplicationForm, canActivate: [adminGuard] },
  { path: '**', redirectTo: '' }
];
```

### 23.6 Header, layout and "Built by" card

**`job-tracker-ui/src/app/app.ts` — replace the contents:**
```typescript
import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { environment } from '../environments/environment';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly auth = inject(AuthService);

  protected readonly swaggerUrl = environment.swaggerUrl;
  protected readonly repoUrl = environment.repoUrl;
  protected readonly portfolioUrl = environment.portfolioUrl;

  protected logout(): void {
    this.auth.logout();
  }
}
```

**`job-tracker-ui/src/app/app.html` — replace the contents:**
```html
<header class="band">
  <div class="band-inner">
    <div class="band-top">
      <a routerLink="/" class="brand">
        Job Tracker <span class="demo-tag">Demo</span>
      </a>
      <nav class="nav">
        @if (auth.isLoggedIn()) {
          <a routerLink="/" class="nav-link nav-link--current">Applications</a>
        }
        <a [href]="swaggerUrl" target="_blank" rel="noopener" class="nav-link">API docs</a>
        <a [href]="repoUrl" target="_blank" rel="noopener" class="nav-link">GitHub</a>
        <a [href]="portfolioUrl" class="nav-link">← juanmunoz.dev</a>
        @if (auth.isLoggedIn()) {
          <span class="user-pill">
            {{ auth.username() }}
            <span class="user-role">{{ auth.isAdmin() ? 'Admin' : 'Read-only' }}</span>
          </span>
          <button type="button" class="logout" (click)="logout()">Log out</button>
        }
      </nav>
    </div>

    @if (auth.isLoggedIn()) {
      <div class="band-hero">
        <div class="hero-text">
          <h1>Application pipeline</h1>
          <p>Track every application from first submission to offer. A demo of a .NET 8 REST API and an Angular front end, deployed to Azure. Sample data only.</p>
        </div>
        @if (auth.isAdmin()) {
          <a routerLink="/add" class="btn btn--accent">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" aria-hidden="true"><path d="M12 5v14M5 12h14"></path></svg>
            New application
          </a>
        }
      </div>
    }
  </div>
</header>

<main class="page">
  <router-outlet />
</main>

@if (auth.isLoggedIn()) {
  <footer class="page">
    <section class="about" aria-label="About this demo">
      <img src="juan-avatar.jpg" alt="Cartoon portrait of Juan Muñoz at his desk" class="about-avatar" width="96" height="96" />
      <div class="about-text">
        <span class="about-title">Built by Juan Muñoz</span>
        <p>Senior software engineer, 20+ years in .NET. This demo shows a layered ASP.NET Core API, EF Core with PostgreSQL, JWT authentication with roles, an Angular SPA, unit tests, and CI/CD to Azure with GitHub Actions.</p>
        <p class="about-note">Heads up: the first load can take 30–60 seconds while the free-tier server wakes up.</p>
      </div>
      <div class="about-links">
        <a [href]="portfolioUrl" class="btn btn--accent">View my portfolio</a>
        <a [href]="swaggerUrl" target="_blank" rel="noopener" class="btn btn--ghost">API docs</a>
        <a [href]="repoUrl" target="_blank" rel="noopener" class="btn btn--ghost">GitHub repo</a>
      </div>
    </section>
  </footer>
}
```

- Signed out: a slim header (brand, API docs, GitHub, portfolio). The login page has its own portrait, so the footer card is hidden.
- Signed in: username and role, **Log out**, the hero, and the "Built by" card. **New application** shows only for admins.
- External links use `[href]` with `target="_blank" rel="noopener"` (new tab, safely); the portfolio link opens in the same tab.

**`job-tracker-ui/src/app/app.scss` — replace the contents** (the whole file — if only part of it is pasted, the header loses its styling and the white **Log out** text becomes invisible on the light background):
```scss
.band {
  background: var(--navy);
  color: var(--navy-text);
}

.band-inner,
.page {
  max-width: 1120px;
  margin: 0 auto;
  padding-left: 24px;
  padding-right: 24px;
}

.band-inner {
  padding-top: 18px;
  padding-bottom: 36px;
  display: flex;
  flex-direction: column;
  gap: 30px;
}

.band-top,
.band-hero {
  display: flex;
  justify-content: space-between;
  gap: 16px 24px;
  flex-wrap: wrap;
}

.band-top { align-items: center; }
.band-hero { align-items: flex-end; }

.brand {
  display: inline-flex;
  align-items: baseline;
  gap: 10px;
  font-family: var(--font-display);
  font-size: 1.15rem;
  font-weight: 700;
  color: var(--navy-text);
  text-decoration: none;
}

.demo-tag {
  font-family: var(--font-body);
  font-size: 0.72rem;
  font-weight: 600;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--amber-ink);
  background: var(--amber);
  padding: 2px 9px;
  border-radius: 999px;
}

.nav {
  display: flex;
  align-items: center;
  gap: 22px;
  flex-wrap: wrap;
}

.nav-link {
  color: var(--navy-muted);
  text-decoration: none;
  font-size: 0.95rem;
}
.nav-link:hover,
.nav-link--current { color: var(--navy-text); }

.hero-text {
  display: flex;
  flex-direction: column;
  gap: 10px;
  max-width: 640px;
}

.hero-text h1 {
  margin: 0;
  font-family: var(--font-display);
  font-size: 2.1rem;
  line-height: 1.15;
}

.hero-text p {
  margin: 0;
  color: var(--navy-muted);
  font-size: 1rem;
  line-height: 1.55;
}

/* Signed-in user + logout */
.user-pill {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 4px 6px 4px 12px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.08);
  color: var(--navy-text);
  font-size: 0.9rem;
}

.user-role {
  font-size: 0.72rem;
  font-weight: 600;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  color: var(--amber-ink);
  background: var(--amber);
  padding: 2px 8px;
  border-radius: 999px;
}

.logout {
  min-height: 36px;
  padding: 0 14px;
  border: 1px solid rgba(255, 255, 255, 0.25);
  border-radius: 999px;
  background: transparent;
  color: var(--navy-text);
  font: inherit;
  font-size: 0.9rem;
  cursor: pointer;
}
.logout:hover { background: rgba(255, 255, 255, 0.08); }

/* Built by card */
footer.page { padding-bottom: 40px; }

.about {
  display: flex;
  gap: 24px;
  align-items: center;
  flex-wrap: wrap;
  background: var(--surface);
  border-radius: 16px;
  padding: 22px 24px;
  box-shadow: var(--shadow);
}

.about-avatar {
  width: 96px;
  height: 96px;
  border-radius: 999px;
  object-fit: cover;
  object-position: 50% 30%;
  box-shadow: 0 0 0 3px var(--amber);
}

.about-text {
  flex: 999 1 420px;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.about-text p {
  margin: 0;
  color: var(--muted);
  line-height: 1.55;
}

.about-title {
  font-family: var(--font-display);
  font-size: 1.05rem;
  font-weight: 600;
}

.about-note {
  font-size: 0.9rem;
  color: #6b5a3a !important;
}

.about-links {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}
```

### 23.7 The login page

```bash
ng generate component components/login
```

**`login.ts` — replace the contents:**
```typescript
import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

interface DemoAccount {
  username: string;
  password: string;
  role: string;
  description: string;
}

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly demoAccounts: DemoAccount[] = [
    { username: 'admin', password: 'Admin123!', role: 'Admin', description: 'Add, edit and delete applications' },
    { username: 'demo', password: 'Demo123!', role: 'Read-only', description: 'Browse, search and filter' }
  ];

  protected readonly expired = this.route.snapshot.queryParamMap.get('reason') === 'expired';
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly showPassword = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    username: ['', Validators.required],
    password: ['', Validators.required]
  });

  protected useAccount(account: DemoAccount): void {
    this.form.setValue({ username: account.username, password: account.password });
    this.error.set(null);
  }

  protected togglePassword(): void {
    this.showPassword.update(shown => !shown);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => this.router.navigate(['/']),
      error: (err: HttpErrorResponse) => {
        this.error.set(err.status === 401
          ? 'Invalid username or password.'
          : 'Could not reach the server. It may be waking up, so try again in a few seconds.');
        this.submitting.set(false);
      }
    });
  }
}
```

- `ActivatedRoute` reads `?reason=expired`, added by the automatic logout.
- `form.setValue(…)` fills both fields when a **Use** button is clicked.
- A 401 means wrong credentials; any other error means the server couldn't be reached (often a cold start).
- The demo credentials appear here and in `appsettings.json`. Change both together.

**`login.html` — replace the contents:**
```html
<section class="login">
  <div class="login-card">

    <aside class="intro">
      <img src="juan-avatar.jpg" alt="Cartoon portrait of Juan Muñoz at his desk" class="avatar" width="120" height="120" />
      <h1>Welcome to Job Tracker</h1>
      <p>
        This is a portfolio demo with sample data, so the login credentials are shared below.
        Sign in as <strong>admin</strong> to add, edit and delete applications, or as
        <strong>demo</strong> to explore with read-only access.
      </p>
      <p class="intro-note">
        Sessions end automatically after 5 minutes. The first sign-in can take up to a minute
        while the free-tier server wakes up.
      </p>
      <span class="signature">Built by Juan Muñoz</span>
    </aside>

    <div class="form-side">
      <h2>Sign in</h2>

      @if (expired) {
        <p class="notice" role="status">Your session expired. Please sign in again.</p>
      }

      <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
        <div class="field">
          <label for="username">Username</label>
          <input id="username" type="text" formControlName="username" autocomplete="username" />
          @if (form.controls.username.touched && form.controls.username.hasError('required')) {
            <small class="field-error">Enter your username.</small>
          }
        </div>

        <div class="field">
          <label for="password">Password</label>
          <div class="password-wrap">
            <input id="password" [type]="showPassword() ? 'text' : 'password'"
                   formControlName="password" autocomplete="current-password" />
            <button type="button" class="toggle" (click)="togglePassword()"
                    [attr.aria-label]="showPassword() ? 'Hide password' : 'Show password'">
              {{ showPassword() ? 'Hide' : 'Show' }}
            </button>
          </div>
          @if (form.controls.password.touched && form.controls.password.hasError('required')) {
            <small class="field-error">Enter your password.</small>
          }
        </div>

        @if (error()) {
          <p class="error" role="alert">{{ error() }}</p>
        }

        <button type="submit" class="btn btn--primary submit" [disabled]="submitting()">
          {{ submitting() ? 'Signing in…' : 'Sign in' }}
        </button>
      </form>

      <div class="demo">
        <span class="demo-title">Demo accounts</span>
        @for (account of demoAccounts; track account.username) {
          <div class="demo-account">
            <div class="demo-info">
              <span class="demo-role">{{ account.role }}</span>
              <span class="mono">{{ account.username }} / {{ account.password }}</span>
              <span class="demo-desc">{{ account.description }}</span>
            </div>
            <button type="button" class="btn btn--ghost" (click)="useAccount(account)">Use</button>
          </div>
        }
      </div>
    </div>

  </div>
</section>
```

- `autocomplete="username"` / `"current-password"` help browsers and password managers.
- `[type]="showPassword() ? 'text' : 'password'"` toggles password visibility.
- `role="status"` and `role="alert"` make screen readers announce messages.

**`login.scss` — replace the contents:**
```scss
.login {
  max-width: 980px;
  margin: 0 auto;
  padding: 40px 24px;
  box-sizing: border-box;
}

.login-card {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  background: var(--surface);
  border-radius: 20px;
  box-shadow: var(--shadow);
  overflow: hidden;
}

@media (max-width: 760px) {
  .login-card { grid-template-columns: minmax(0, 1fr); }
}

/* Left: intro panel */
.intro {
  background: var(--navy);
  color: var(--navy-text);
  padding: 40px 36px;
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.avatar {
  width: 120px;
  height: 120px;
  border-radius: 999px;
  object-fit: cover;
  object-position: 50% 30%;
  box-shadow: 0 0 0 4px var(--amber);
}

.intro h1 {
  margin: 8px 0 0;
  font-family: var(--font-display);
  font-size: 1.7rem;
  line-height: 1.2;
}

.intro p {
  margin: 0;
  color: var(--navy-muted);
  line-height: 1.6;
}

.intro strong { color: var(--navy-text); }

.intro-note {
  font-size: 0.92rem;
  border-left: 3px solid var(--amber);
  padding-left: 12px;
}

.signature {
  margin-top: auto;
  padding-top: 12px;
  font-family: var(--font-display);
  font-size: 0.95rem;
  color: var(--amber);
}

/* Right: form */
.form-side {
  padding: 40px 36px;
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.form-side h2 {
  margin: 0;
  font-family: var(--font-display);
  font-size: 1.4rem;
}

.notice {
  margin: 0;
  padding: 12px 14px;
  border-radius: 10px;
  background: #fdf3e1;
  color: #7a4b00;
  font-size: 0.95rem;
}

form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

label {
  font-size: 0.9rem;
  font-weight: 600;
}

input {
  width: 100%;
  min-height: 44px;
  box-sizing: border-box;
  padding: 0 12px;
  border: 1px solid var(--border);
  border-radius: 10px;
  font: inherit;
  color: var(--text);
  background: var(--surface);
}

input:focus {
  outline: 2px solid var(--amber);
  outline-offset: 1px;
}

input.ng-touched.ng-invalid { border-color: var(--danger); }

.password-wrap { position: relative; }
.password-wrap input { padding-right: 64px; }

.toggle {
  position: absolute;
  right: 6px;
  top: 50%;
  transform: translateY(-50%);
  min-height: 32px;
  padding: 0 10px;
  border: 0;
  border-radius: 8px;
  background: transparent;
  color: var(--link);
  font: inherit;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
}
.toggle:hover { background: #f1f2f6; }

.field-error,
.error {
  margin: 0;
  color: var(--danger);
  font-size: 0.88rem;
}

.submit {
  justify-content: center;
  width: 100%;
}

/* Demo accounts */
.demo {
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding-top: 18px;
  border-top: 1px solid #eceef3;
}

.demo-title {
  font-size: 0.8rem;
  font-weight: 600;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--muted);
}

.demo-account {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  padding: 12px 14px;
  border: 1px solid var(--border);
  border-radius: 12px;
}

.demo-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.demo-role { font-weight: 600; }

.demo-desc {
  font-size: 0.88rem;
  color: var(--muted);
}
```

Two equal columns (navy intro, white form) that stack into one column below 760 px.

### 23.8 The list: search, chips, pager and roles

**`application-list.ts` — replace the contents:**
```typescript
import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subject, catchError, debounceTime, distinctUntilChanged, map, of, switchMap, tap } from 'rxjs';
import { ApplicationService } from '../../services/application.service';
import { ApplicationStatus, PagedApplications, STATUSES, STATUS_LABELS } from '../../models/application';
import { StatusBadge } from '../status-badge/status-badge';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-application-list',
  imports: [DatePipe, RouterLink, StatusBadge],
  templateUrl: './application-list.html',
  styleUrl: './application-list.scss'
})
export class ApplicationList {
  private readonly applicationService = inject(ApplicationService);
    protected readonly auth = inject(AuthService);

  protected readonly statusLabels = STATUS_LABELS;
  protected readonly pageSizes = [5, 10, 20];

  // What the user asked for
  protected readonly search = signal('');
  protected readonly statusFilter = signal<ApplicationStatus | 'All'>('All');
  protected readonly page = signal(1);
  protected readonly pageSize = signal(5);

  // What the server answered
  protected readonly result = signal<PagedApplications | null>(null);
  protected readonly loading = signal(true);      // first load only
  protected readonly refreshing = signal(false);  // later loads
  protected readonly error = signal<string | null>(null);

  protected readonly counts = computed(() => this.result()?.statusCounts ?? {});

  protected readonly visibleStatuses = computed(() =>
    STATUSES.filter(status => (this.counts()[status] ?? 0) > 0 || status === this.statusFilter())
  );

  protected readonly rangeStart = computed(() => {
    const r = this.result();
    return !r || r.totalCount === 0 ? 0 : (r.page - 1) * r.pageSize + 1;
  });

  protected readonly rangeEnd = computed(() => {
    const r = this.result();
    return r ? Math.min(r.page * r.pageSize, r.totalCount) : 0;
  });

  // Delete confirmation
  protected readonly confirmingId = signal<number | null>(null);
  protected readonly deletingId = signal<number | null>(null);
  protected readonly deleteError = signal<string | null>(null);

  private readonly searchInput$ = new Subject<string>();
  private readonly reload$ = new Subject<void>();

  constructor() {
    // Search box: wait for a pause in typing, ignore repeats, then reload from page 1
    this.searchInput$.pipe(
      map(text => text.trim()),
      debounceTime(300),
      distinctUntilChanged(),
      takeUntilDestroyed()
    ).subscribe(text => {
      this.search.set(text);
      this.page.set(1);
      this.reload();
    });

    // Every reload: call the API; a newer reload cancels an older one still in flight
    this.reload$.pipe(
      tap(() => this.refreshing.set(true)),
      switchMap(() =>
        this.applicationService.getPage({
          search: this.search(),
          status: this.statusFilter(),
          page: this.page(),
          pageSize: this.pageSize()
        }).pipe(catchError(() => of(null)))
      ),
      takeUntilDestroyed()
    ).subscribe(result => {
      this.loading.set(false);
      this.refreshing.set(false);

      if (!result) {
        this.error.set('Could not load applications. Please try again.');
        return;
      }
      this.error.set(null);

      // The page became empty (e.g. last row on it was deleted): step back one page
      if (result.items.length === 0 && result.page > 1) {
        this.page.set(result.page - 1);
        this.reload();
        return;
      }

      this.result.set(result);
    });

    this.reload();
  }

  private reload(): void {
    this.reload$.next();
  }

  protected onSearch(text: string): void {
    this.searchInput$.next(text);
  }

  protected clearSearch(input: HTMLInputElement): void {
    input.value = '';
    this.searchInput$.next('');
  }

  protected setFilter(filter: ApplicationStatus | 'All'): void {
    this.statusFilter.set(filter);
    this.page.set(1);
    this.reload();
  }

  protected setPageSize(size: string): void {
    this.pageSize.set(Number(size));
    this.page.set(1);
    this.reload();
  }

  protected goToPage(target: number): void {
    const total = this.result()?.totalPages ?? 1;
    if (target < 1 || target > total) return;
    this.page.set(target);
    this.reload();
  }

  protected askDelete(id: number): void {
    this.deleteError.set(null);
    this.confirmingId.set(id);
  }

  protected cancelDelete(): void {
    this.confirmingId.set(null);
  }

  protected confirmDelete(id: number): void {
    this.deletingId.set(id);

    this.applicationService.delete(id).subscribe({
      next: () => {
        this.confirmingId.set(null);
        this.deletingId.set(null);
        this.reload();
      },
      error: () => {
        this.deleteError.set('Could not delete the application. Please try again.');
        this.deletingId.set(null);
      }
    });
  }
}
```

The list keeps two kinds of state: **what the user asked for** (search, status, page, page size) and **what the server answered** (one page plus counts). Two [RxJS](#rxjs) pipelines handle the timing:

| Code | Meaning | C# / .NET equivalent |
|---|---|---|
| `new Subject<string>()` | A stream you push values into with `.next(value)` | An event you raise |
| `debounceTime(300)` | Pass a value only after 300 ms without a new one — typing "engineer" sends **one** request, not eight | A timer reset on every keystroke |
| `distinctUntilChanged()` | Skip a value equal to the previous one | — |
| `switchMap(() => getPage(…))` | Start a request and **cancel** the previous one if it's still running, so a slow old response can never overwrite a newer one | `CancellationToken` on the old task |
| `catchError(() => of(null))` inside `switchMap` | A failed request becomes `null` instead of ending the stream; outside, one failure would stop all future reloads | `try/catch` inside a loop |
| `takeUntilDestroyed()` | Unsubscribes automatically when the component is destroyed, preventing memory leaks | `IDisposable` |
| `auth = inject(AuthService)` | Lets the template hide admin-only actions | — |

- The subscriptions are created in the **constructor** because `takeUntilDestroyed()` must be called there.
- `loading` is only for the first load ("Loading…"); later loads set `refreshing`, which just dims the table.
- After a delete the list **reloads from the server**, keeping counts, pages and totals accurate. If the current page becomes empty, it steps back one page.

**`application-list.html` — replace the contents:**
```html
<section class="list">

  @if (loading()) {
    <p class="muted">Loading applications… the first load can take 30–60 seconds while the server wakes up.</p>
  } @else if (error() && !result()) {
    <p class="error">{{ error() }}</p>
  } @else if (result(); as r) {

    <div class="toolbar">
      <div class="search">
        <label for="search" class="sr-only">Search applications</label>
        <svg class="search-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true"><circle cx="11" cy="11" r="7"></circle><path d="m20 20-3.5-3.5"></path></svg>
        <input #searchBox id="search" type="search"
               placeholder="Search company, position or country"
               autocomplete="off"
               (input)="onSearch(searchBox.value)" />
        @if (search()) {
          <button type="button" class="clear" (click)="clearSearch(searchBox)" aria-label="Clear search">×</button>
        }
      </div>
    </div>

    @if (r.allCount > 0 || statusFilter() !== 'All') {
      <div class="pipeline">
        <div class="bar" aria-hidden="true">
          @for (status of visibleStatuses(); track status) {
            <div [class]="'seg dot--' + status.toLowerCase()" [style.flex]="counts()[status] ?? 0"></div>
          }
        </div>

        <span id="filter-hint" class="filter-hint">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 5h18l-7 8v6l-4 2v-8z"></path></svg>
          Click a status to filter the list
        </span>

        <div class="chips" role="group" aria-labelledby="filter-hint">
          <button type="button" class="chip" [class.chip--active]="statusFilter() === 'All'" (click)="setFilter('All')">
            All <span class="chip-count">{{ r.allCount }}</span>
          </button>
          @for (status of visibleStatuses(); track status) {
            <button type="button" class="chip" [class.chip--active]="statusFilter() === status" (click)="setFilter(status)">
              <span [class]="'dot dot--' + status.toLowerCase()"></span>
              {{ statusLabels[status] }}
              <span class="chip-count">{{ counts()[status] ?? 0 }}</span>
            </button>
          }
        </div>
      </div>
    }

    @if (error()) {
      <p class="error">{{ error() }}</p>
    }
    @if (deleteError()) {
      <p class="error">{{ deleteError() }}</p>
    }

    @if (r.items.length === 0) {
      <p class="muted empty">
        @if (search()) {
          No applications match "{{ search() }}".
        } @else if (statusFilter() !== 'All') {
          No applications with this status.
        } @else {
          No applications yet. Use <strong>New application</strong> to add the first one.
        }
      </p>
    } @else {
      <div class="card" [class.is-refreshing]="refreshing()">
        <div class="table-scroll">
          <table>
            <thead>
              <tr>
                <th>Company</th>
                <th>Position</th>
                <th>Country</th>
                <th>Applied</th>
                <th>Status</th>
                <th>Updated</th>
                @if (auth.isAdmin()) {
                  <th><span class="sr-only">Actions</span></th>
                }                
              </tr>
            </thead>
            <tbody>
              @for (app of r.items; track app.id) {
                <tr>
                  <td class="strong">{{ app.company }}</td>
                  <td>{{ app.position }}</td>
                  <td>{{ app.country }}</td>
                  <td class="mono">{{ app.appliedDate | date: 'yyyy-MM-dd' }}</td>
                  <td><app-status-badge [status]="app.status" /></td>
                  <td class="mono">{{ app.statusDate | date: 'MM-dd HH:mm' }}</td>
                  <td class="row-actions">
                    @if (auth.isAdmin()) {
                      @if (confirmingId() === app.id) {
                        <span class="confirm-text">Delete?</span>
                        <button type="button" class="link link--danger"
                                (click)="confirmDelete(app.id)"
                                [disabled]="deletingId() === app.id">
                          {{ deletingId() === app.id ? 'Deleting…' : 'Yes' }}
                        </button>
                        <button type="button" class="link" (click)="cancelDelete()">No</button>
                      } @else {
                        <a [routerLink]="['/edit', app.id]" class="link">Edit</a>
                        <button type="button" class="link link--danger" (click)="askDelete(app.id)">Delete</button>
                      }
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <nav class="pager" aria-label="Pagination">
          <span class="pager-info">
            Showing <strong>{{ rangeStart() }}–{{ rangeEnd() }}</strong> of <strong>{{ r.totalCount }}</strong>
          </span>

          <div class="pager-controls">
            <label for="page-size" class="pager-size">
              Rows
              <select #sizeSelect id="page-size" (change)="setPageSize(sizeSelect.value)">
                @for (size of pageSizes; track size) {
                  <option [value]="size" [selected]="size === pageSize()">{{ size }}</option>
                }
              </select>
            </label>

            <button type="button" class="btn btn--ghost" (click)="goToPage(r.page - 1)" [disabled]="r.page <= 1">
              ← Previous
            </button>
            <span class="pager-page">Page {{ r.page }} of {{ r.totalPages }}</span>
            <button type="button" class="btn btn--ghost" (click)="goToPage(r.page + 1)" [disabled]="r.page >= r.totalPages">
              Next →
            </button>
          </div>
        </nav>
      </div>
    }
  }
</section>
```

| Syntax | Meaning |
|---|---|
| `@if (result(); as r)` | If `result()` isn't null, call it `r` inside the block |
| `#searchBox` | A [template reference variable](#template-reference-variable): a name for the input element |
| `(input)="onSearch(searchBox.value)"` | Fires on every keystroke; the debounce decides when to search |
| `[class.chip--active]="…"` | Adds one CSS class only when the condition is true |
| `[style.flex]="counts()[status] ?? 0"` | Each bar segment's width is proportional to its count |
| `role="group" aria-labelledby="filter-hint"` | Screen readers announce the chips as one group named by the hint |
| `@if (auth.isAdmin())` around the actions header **and** cells | Read-only users don't see Edit/Delete. Wrap both together, or the columns won't line up |
| `<nav aria-label="Pagination">` | Tells screen readers this is page navigation |

**`application-list.scss` — replace the contents:**
```scss
.list {
  max-width: 1120px;
  margin: 0 auto;
  padding: 28px 24px;
  display: flex;
  flex-direction: column;
  gap: 24px;
  box-sizing: border-box;
}

.muted { color: var(--muted); }
.error { color: var(--danger); margin: 0; }

/* Pipeline */
.pipeline {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.bar {
  display: flex;
  height: 12px;
  gap: 3px;
  border-radius: 999px;
  overflow: hidden;
}

.chips {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

.chip {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  min-height: 44px;
  padding: 0 16px;
  border-radius: 999px;
  border: 1px solid var(--border);
  background: var(--surface);
  color: var(--text);
  font: inherit;
  font-size: 0.9rem;
  cursor: pointer;
}
.chip:hover { border-color: #b9bfcc; }

.chip--active {
  background: var(--navy);
  border-color: var(--navy);
  color: var(--navy-text);
  font-weight: 600;
}

.chip-count {
  font-family: var(--font-mono);
  color: var(--muted);
}
.chip--active .chip-count { color: var(--amber); }

/* Table card */
.card {
  background: var(--surface);
  border-radius: 16px;
  box-shadow: var(--shadow);
  overflow: hidden;
}

.table-scroll { overflow-x: auto; }

table {
  width: 100%;
  min-width: 820px;
  border-collapse: collapse;
  font-size: 0.95rem;
}

th, td {
  text-align: left;
  padding: 14px 22px;
}

th {
  font-size: 0.82rem;
  font-weight: 600;
  color: var(--muted);
  border-bottom: 1px solid #eceef3;
}

tbody tr + tr td { border-top: 1px solid #f0f1f5; }

.strong { font-weight: 600; }

.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  overflow: hidden;
  clip: rect(0 0 0 0);
}

/* Row actions */
.row-actions {
  text-align: right;
  white-space: nowrap;
}
.row-actions > * + * { margin-left: 0.9rem; }

.link {
  color: var(--link);
  font: inherit;
  font-size: 0.92rem;
  font-weight: 600;
  text-decoration: none;
  background: none;
  border: 0;
  padding: 0;
  cursor: pointer;
}
.link:hover { text-decoration: underline; }
.link:disabled { opacity: 0.6; cursor: default; }
.link--danger { color: var(--danger); }

.confirm-text {
  font-size: 0.92rem;
  color: var(--muted);
}

.filter-hint {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 0.85rem;
  color: var(--muted);
}

/* Toolbar + search */
.toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.search {
  position: relative;
  flex: 1 1 320px;
  max-width: 460px;
}

.search-icon {
  position: absolute;
  left: 14px;
  top: 50%;
  transform: translateY(-50%);
  color: var(--muted);
  pointer-events: none;
}

.search input {
  width: 100%;
  min-height: 44px;
  box-sizing: border-box;
  padding: 0 40px 0 40px;
  border: 1px solid var(--border);
  border-radius: 999px;
  background: var(--surface);
  font: inherit;
  font-size: 0.95rem;
  color: var(--text);
}

.search input:focus {
  outline: 2px solid var(--amber);
  outline-offset: 1px;
}

/* Hide the browser's own clear "×" so only ours shows */
.search input::-webkit-search-cancel-button { display: none; }

.clear {
  position: absolute;
  right: 6px;
  top: 50%;
  transform: translateY(-50%);
  width: 32px;
  height: 32px;
  border: 0;
  border-radius: 999px;
  background: transparent;
  color: var(--muted);
  font-size: 1.25rem;
  line-height: 1;
  cursor: pointer;
}
.clear:hover { background: #f1f2f6; color: var(--text); }

/* Empty state */
.empty {
  background: var(--surface);
  border-radius: 16px;
  padding: 28px;
  text-align: center;
  box-shadow: var(--shadow);
}

/* Dim the table while a new page loads */
.card { transition: opacity 0.15s ease; }
.card.is-refreshing { opacity: 0.55; }

/* Pager */
.pager {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px 20px;
  flex-wrap: wrap;
  padding: 14px 22px;
  border-top: 1px solid #eceef3;
}

.pager-info {
  font-size: 0.92rem;
  color: var(--muted);
}
.pager-info strong { color: var(--text); }

.pager-controls {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.pager-size {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-size: 0.9rem;
  color: var(--muted);
}

.pager-size select {
  min-height: 40px;
  padding: 0 10px;
  border: 1px solid var(--border);
  border-radius: 10px;
  background: var(--surface);
  font: inherit;
  color: var(--text);
}

.pager-page {
  font-family: var(--font-mono);
  font-size: 0.85rem;
  color: var(--muted);
  min-width: 96px;
  text-align: center;
}

.pager .btn:disabled {
  opacity: 0.45;
  cursor: default;
}
```

- The search icon is `position: absolute` inside a `position: relative` wrapper; the input's left padding leaves room for it.
- `::-webkit-search-cancel-button { display: none; }` hides the browser's own clear button so only the styled one shows.
- `.sr-only` hides text visually while keeping it for screen readers.

### 23.9 Check locally

| # | Do this | Expect |
|---|---|---|
| 1 | Open `http://localhost:4200` | Redirected to `/login` |
| 2 | **Use** → admin → **Sign in** | The list loads; header shows **admin · ADMIN**, **New application**, **Log out** |
| 3 | Refresh | Still signed in |
| 4 | Click **Next →** | The next page; "Showing 6–8 of 8" |
| 5 | Type `engineer` slowly | One update ~0.3 s after you stop; the API terminal shows one `GET …search=engineer` |
| 6 | Click a chip | Only that status; other chips keep their counts |
| 7 | Log out, sign in as **demo** | No New application, no Edit/Delete |
| 8 | As demo, open `/add` | Sent back to the list |
| 9 | Wait 5 minutes | Sent to the login page with "Your session expired" |

---

## Stage 24: Deploy the new version

### 24.1 Add the JWT key in Azure (required)

The API refuses to start without `Jwt:Key`, and your local key lives in user secrets, which Azure can't see.

1. Generate a **new** key for production (don't reuse the local one): `openssl rand -base64 48`
2. Portal → `jobtracker-api` → **Settings** → **Environment variables** → **App settings** → **+ Add**:
   - **Name:** `Jwt__Key`
   - **Value:** the generated key
3. **Apply** → **Apply** → **Confirm**.

### 24.2 Commit and push the API and the UI together

The endpoint's response changed and every request now needs a token, so both halves must ship in the same push:

```bash
git add -A JobTracker.Api JobTracker.Core JobTracker.Infrastructure JobTracker.Tests job-tracker-ui
git status
git commit -m "Add middleware, server-side search and pagination, and JWT authentication with roles"
git push origin main
```

Check that `git status` lists the new migration files, the `Auth/` and `Middleware/` folders and the new Angular files, and **no** secrets.

### 24.3 Verify

1. Wait for **both** workflows to turn green.
2. Open the live app in a new private/incognito window: it should open on the login page.
3. Try **demo** (read-only) and **admin** (full access).
4. In **Log stream**, the first start shows `Seeded demo user …`, then one line per request.

If the API returns 500/503 right after the deploy, check **Log stream** for `Jwt:Key is missing` — the most common cause is a missing or misspelled `Jwt__Key` setting.

---

## Stage 25: Final verification checklist

| # | Check | Expected |
|---|---|---|
| 1 | `dotnet build` and `dotnet test` locally | Build succeeded, all tests passed |
| 2 | GitHub → Actions → **Build and Deploy to Azure** | Green |
| 3 | GitHub → Actions → **Azure Static Web Apps CI/CD** | Green |
| 4 | `https://<api-default-domain>/swagger` | Swagger UI with the Auth and Applications endpoints and an **Authorize** button |
| 5 | GET /api/applications in Swagger **without** a token | `401` ProblemDetails |
| 6 | Open the live app in a private window | The login page |
| 7 | Sign in as `demo` | List loads; no New application, Edit or Delete |
| 8 | Sign in as `admin` | Add, edit, filter, search, paginate and delete all work |
| 9 | Refresh on `/edit/<id>` as admin | No 404 |
| 10 | Wait 5 minutes after signing in | Back on the login page with "Your session expired" |
| 11 | App Service → **Log stream** | One line per request with status and duration |
| 12 | `git status` on a clean checkout | No `bin/`, `obj/`, `node_modules/`, `.PublishSettings`, passwords or JWT keys committed |
| 13 | Azure → **All resources** | Only the App Service, its plan (**F1**) and the Static Web App (**Free**) |
| 14 | Azure → **Cost Management** → **Cost analysis** | $0.00 (data can lag 8–24 hours) |

---

## Troubleshooting

Every problem below actually happened while building this project.

| Symptom | Cause | Fix |
|---|---|---|
| Publish profile download does nothing; banner says the free trial expired | The Azure trial's 30 days ended; the subscription is disabled | Upgrade to Pay-As-You-Go (Stage 11.1). Free tiers still cost $0 |
| `dotnet ef ...` prints `command not found: Ssl` and `command not found: Trust` | The connection string wasn't quoted, so the shell treated each `;` as the end of a command | Wrap the string in single quotes after `--connection` |
| `Unable to retrieve project metadata` from `dotnet ef` | The connection string was placed after `-p`, which expects a project path | Use `-p JobTracker.Infrastructure -s JobTracker.Api --connection '<string>'` |
| `dotnet ef` says only `Build failed` | The code doesn't compile | Run `dotnet build` to see the real errors |
| `CS1061: 'IServiceCollection' does not contain a definition for 'AddSwaggerGen'` | The Swashbuckle package is missing | `dotnet add JobTracker.Api package Swashbuckle.AspNetCore --version 6.6.2` |
| `Timeout during connection attempt` from Npgsql | Network blocked: firewall, remote access off, server stopped, or VPN | Allow your IP / enable remote access, check the server is running, try without a VPN |
| Workflow green, but the API URL returns 404 | Using `name.azurewebsites.net` instead of the unique default domain | Use the **Default domain** from the App Service Overview |
| API returns 404 or 500 for every route right after a deploy | The app crashes on startup, for example because a service was registered after `builder.Build()` | Register all services before `Build()`. Check **Log stream** |
| A real password shows in `git diff` for `appsettings.Development.json` | A connection string was saved into a committed file | `git checkout -- <file>`, move the value to user secrets, change the password |
| Hundreds of `bin/` and `obj/` files in `git status` | No `.gitignore` | `dotnet new gitignore`, then `git rm -r --cached <project>/bin <project>/obj` for each project |
| GitHub rejects the workflow: `Unexpected value 'app-name'` | Settings were placed under `on:` | `on:` holds only triggers. Settings go in a step's `with:` block |
| `node -v` shows v20 after installing a newer Node | nvm still defaults to the old version, or another Node (e.g. Conda) comes first in the PATH | `nvm alias default 'lts/*'`, open a new terminal, check `which node` |
| `TS2451: Cannot redeclare block-scoped variable 'environment'` | The new code was added next to the generated code instead of replacing it | Leave exactly one `export const environment` block in the file |
| `Could not resolve "./services/application.service"` | The `services` folder was created in `src/` instead of `src/app/` | Move it into `src/app/` |
| `TS2339: Property 'title' does not exist on type 'App'` | The generated `app.html` still uses `{{ title() }}`, which the new `app.ts` removed | Replace `app.html` as in Stage 14.4 |
| `TS2304: Cannot find name 'ApplicationStatus'` | The model file was replaced instead of appended, deleting the type | Restore the full `application.ts` from Stage 14.1 |
| `TS7053: Element implicitly has an 'any' type…` in a template | A template indexed `STATUS_LABELS` with an `any` value | Compute the label in the class with type narrowing (`filterLabel` in Stage 15.5) |
| Saving an edit fails; the console shows a **CORS error** for the PUT only | The **API threw an exception** (missing AutoMapper map). Unhandled exceptions return a 500 without CORS headers, which the browser reports as CORS | **If only one endpoint shows CORS, suspect a server error.** Check **Log stream**. Add the missing map (Stage 5.2) |
| Live site loads but says "Could not load applications"; console shows CORS | The Static Web App's origin isn't allowed by the API | Add `Cors__AllowedOrigins__1` (Stage 18.6), with `https://` and no trailing slash |
| Refreshing `/edit/5` on the live site returns 404 | No navigation fallback | Add `public/staticwebapp.config.json` (Stage 18.1) |
| Static Web Apps build fails with a Node.js version error | Oryx picked an older Node than Angular 22 requires | Add `"engines": { "node": ">=22" }` to `job-tracker-ui/package.json`, or build in the workflow with `actions/setup-node` and set `skip_app_build: true` |
| `zsh: no matches found: --include=*.ts` or `… 8.0.*` | zsh tried to expand `*` as a file pattern | Put the pattern in quotes: `--include="*.ts"`, `--version "8.0.*"` |
| `CS0246: The type or namespace name '…Middleware' could not be found` | The file was never saved, or was saved in another folder | `find . -name "*Middleware*.cs" -not -path "*/bin/*"`, then create or move the file into `JobTracker.Api/Middleware/` |
| `500` with `"detail": "08P01: SASL authentication failed"` | PostgreSQL rejected the password (local user secret not updated after a password change) | Copy the connection string from the App Service setting and run `dotnet user-secrets set` again |
| `CS0535: 'ApplicationRepository' does not implement interface member 'GetPagedAsync'` in the test project | A stray copy of the repository class in `JobTracker.Tests/Repositories/ApplicationRepositoryTests.cs` | Delete that file (`git rm …`) |
| `TS2339: Property 'portfolioUrl' does not exist` (or another environment property) | One environment file is missing the property | Give both environment files the same properties |
| After deploying auth, the API returns 500/503 and Log stream says `Jwt:Key is missing` | No `Jwt__Key` app setting in Azure | Add it (Stage 24.1) |
| Every API call returns 401 after deploying auth | The browser has no token yet, or it expired | Sign in again; the interceptor and guards send you to the login page automatically |
| Header looks unstyled and **Log out** is invisible | `app.scss` was replaced with only part of its rules | Restore the complete `app.scss` from Stage 23.6 |

---

## Cost and security notes

**Cost.** At the time of writing, this setup costs **$0/month**:
- App Service plan **F1 (Free)**: 60 CPU minutes per day. If that's exceeded, the app pauses until the next day; it's not billed. Free apps can't use custom domains.
- Static Web Apps **Free**: up to 10 apps per subscription, 2 custom domains per app, and 100 GB of bandwidth per month (it stops serving beyond that instead of billing).
- Charges would come only from changing a tier or creating new paid resources. The budget alerts (Stage 11.2) catch that.

**Security.**
- **Authentication and roles:** every applications endpoint requires a JWT; only Admin can change data. Tokens expire after 5 minutes and are signed with a secret key kept in user secrets (locally) and an App Service setting (Azure), never in the repository.
- **The demo credentials are public on purpose**, because the app holds only sample data. Anyone with the admin login can change or delete records, so never put real data in this demo.
- Passwords are stored as salted PBKDF2 hashes, never as plain text.
- Secrets live in exactly three places: local [user secrets](#user-secrets), Azure App Service settings, and [GitHub secrets](#github-secret). **Never** in committed files.
- The Angular app keeps the token in [localStorage](#localstorage), a common demo choice; production systems often prefer HttpOnly cookies because of [XSS](#xss) risk.
- If a password or key is ever shared (in a chat, a screenshot or a commit), replace it: change it at the source, then update the App Service setting and your user secret.
- Delete downloaded `.PublishSettings` files after use.

---
# Part 3: Definitions

## Definitions index

Terms are in alphabetical order. Each entry explains the term in plain language and, where it helps, compares it to something familiar from C# and .NET.

[Angular](#angular) · [Angular CLI](#angular-cli) · [ASP.NET Core](#aspnet-core) · [async and await](#async-and-await) · [AutoMapper](#automapper) · [Azure](#azure) · [Azure App Service](#azure-app-service) · [Azure budget](#azure-budget) · [Azure Static Web Apps](#azure-static-web-apps) · [BEM](#bem) · [CDN](#cdn) · [CI/CD](#cicd) · [Claims](#claims) · [Clone](#clone) · [Commit](#commit) · [Component](#component) · [Computed signal](#computed-signal) · [Configuration](#configuration) · [Connection string](#connection-string) · [Control flow blocks](#control-flow-blocks) · [Controller](#controller) · [CORS](#cors) · [CSS and SCSS](#css-and-scss) · [CSS variables](#css-variables) · [DbContext](#dbcontext) · [Decorator](#decorator) · [Dependency injection](#dependency-injection) · [DTO](#dto) · [Endpoint](#endpoint) · [Entity](#entity) · [Entity Framework Core](#entity-framework-core) · [Enum](#enum) · [Environment files](#environment-files) · [Event binding](#event-binding) · [F1 Free tier](#f1-free-tier) · [Flexbox](#flexbox) · [FluentAssertions](#fluentassertions) · [Git](#git) · [GitHub](#github) · [GitHub Actions](#github-actions) · [GitHub secret](#github-secret) · [.gitignore](#gitignore) · [Google Fonts](#google-fonts) · [Hex color](#hex-color) · [HTTP methods](#http-methods) · [HTTP status codes](#http-status-codes) · [HttpClient](#httpclient) · [HttpParams](#httpparams) · [HTTPS](#https) · [ILIKE](#ilike) · [Immutable update](#immutable-update) · [Input](#input) · [Interceptor](#interceptor) · [Interface](#interface) · [Interpolation](#interpolation) · [IQueryable](#iqueryable) · [JSON](#json) · [JWT](#jwt) · [Layered architecture](#layered-architecture) · [Lifecycle hook](#lifecycle-hook) · [localhost and port](#localhost-and-port) · [localStorage](#localstorage) · [Middleware](#middleware) · [Migration](#migration) · [Model binding](#model-binding) · [Moq](#moq) · [.NET](#net) · [Node.js](#nodejs) · [Npgsql](#npgsql) · [npm](#npm) · [NuGet](#nuget) · [nvm](#nvm) · [Observable](#observable) · [ORM](#orm) · [Oryx](#oryx) · [Pagination](#pagination) · [Password hashing](#password-hashing) · [Pay-As-You-Go](#pay-as-you-go) · [PgBouncer](#pgbouncer) · [Pipe](#pipe) · [PostgreSQL](#postgresql) · [ProblemDetails](#problemdetails) · [Property binding](#property-binding) · [Publish profile](#publish-profile) · [Push](#push) · [Reactive forms](#reactive-forms) · [Record](#record) · [Reflection](#reflection) · [Repository pattern](#repository-pattern) · [Resource group](#resource-group) · [Route guard](#route-guard) · [Route parameter](#route-parameter) · [Routing](#routing) · [Runner](#runner) · [RxJS](#rxjs) · [Service](#service) · [Signal](#signal) · [Solution](#solution) · [SPA](#spa) · [Spread operator](#spread-operator) · [SQL](#sql) · [SSL and TLS](#ssl-and-tls) · [SSR](#ssr) · [Structured logging](#structured-logging) · [Swagger](#swagger) · [Switch expression](#switch-expression) · [Template](#template) · [Template literal](#template-literal) · [Template reference variable](#template-reference-variable) · [Two-way binding](#two-way-binding) · [Type narrowing](#type-narrowing) · [TypeScript](#typescript) · [Union type](#union-type) · [Unit test](#unit-test) · [User secrets](#user-secrets) · [UTC](#utc) · [Validators](#validators) · [Visual Studio Code](#visual-studio-code) · [Vitest](#vitest) · [Workflow](#workflow) · [XSS](#xss) · [xUnit](#xunit) · [YAML](#yaml) · [Zoneless](#zoneless)

---

### Angular
A front-end framework from Google for building web applications in TypeScript. An Angular app is made of [components](#component), [services](#service) and [routes](#routing). It runs entirely in the browser.

### Angular CLI
The `ng` command-line tool (**CLI** = Command-Line Interface). `ng new` creates a project, `ng generate` creates components and other files, `ng serve` runs a development server, and `ng build` produces the production files. Roughly the Angular equivalent of the `dotnet` command.

### ASP.NET Core
The part of .NET for building web applications and web APIs. It receives HTTP requests, runs them through [middleware](#middleware), routes them to [controllers](#controller), and sends back responses.

### async and await
C# keywords for **asynchronous** code. A method marked `async` returning `Task<T>` can `await` slow operations (database queries, HTTP calls) without blocking the thread, so the server can handle other requests while waiting. The TypeScript equivalent in this project is the [Observable](#observable).

### AutoMapper
A .NET library that copies values between objects of different types, for example from an [entity](#entity) to a [DTO](#dto). You declare each conversion once in a **profile** (`CreateMap<Source, Destination>()`), then call `_mapper.Map<Destination>(source)`. If a conversion isn't declared, AutoMapper throws an exception at runtime.

### Azure
Microsoft's cloud platform: computers, databases, storage and services you rent by usage instead of owning. You manage it at https://portal.azure.com. Resources live inside a **subscription** (the billing account) and are organized into [resource groups](#resource-group).

### Azure App Service
An Azure service that runs web applications and APIs without you managing a server. You deploy your published code, and Azure provides the operating system, the .NET runtime, HTTPS and a public address. Its computing power comes from an **App Service plan**; this project uses the [F1 Free tier](#f1-free-tier).

### Azure budget
A spending threshold in Azure Cost Management that emails you when actual or forecasted costs reach a percentage of it. It **warns** only. It never stops resources or charges.

### Azure Static Web Apps
An Azure service for hosting static front-end files (HTML, JavaScript, CSS) such as a built Angular app. It connects to a GitHub repository, builds and deploys on every push, serves files over HTTPS from a [CDN](#cdn), and supports custom domains. The Free plan allows up to 10 apps per subscription.

### BEM
**Block Element Modifier**, a CSS class-naming convention. `badge` is a block, and `badge--offer` is a modifier (a variation) of it. It keeps class names predictable and avoids conflicts.

### CDN
**Content Delivery Network**: a group of servers around the world that keep copies of your files, so each visitor downloads them from a nearby server.

### CI/CD
**Continuous Integration / Continuous Deployment**. CI automatically builds and tests the code on every change. CD automatically deploys it when the tests pass. Here, both are done by [GitHub Actions](#github-actions).

### Claims
Facts about a user carried inside a [JWT](#jwt), such as their id (`sub`), username (`ClaimTypes.Name`) and role (`ClaimTypes.Role`). ASP.NET Core reads them on every request; `[Authorize(Roles = "Admin")]` checks the role claim.

### Clone
Copying a [Git](#git) repository from GitHub to your computer with `git clone <url>`. The copy stays linked to GitHub, so you can [push](#push) changes back.

### Commit
A saved snapshot of your code in [Git](#git), with a message describing the change. `git add` chooses which changes go into the next commit, and `git commit -m "message"` saves it locally.

### Component
The basic building block of an Angular app: one piece of the screen. It's a TypeScript class marked with `@Component`, plus a [template](#template) (HTML) and styles. Its `selector` (for example `app-status-badge`) becomes an HTML tag other templates can use.

### Computed signal
A [signal](#signal) whose value is calculated from other signals with `computed(() => ...)`. Angular recalculates it automatically whenever any signal it reads changes, and caches the result otherwise. Similar to a getter-only C# property that updates itself.

### Configuration
How .NET apps read settings. Values come from several sources, in order, and later sources override earlier ones: `appsettings.json` → `appsettings.{Environment}.json` → [user secrets](#user-secrets) (in Development) → environment variables (such as Azure App Service settings). Sections are separated by `:` in code (`ConnectionStrings:DefaultConnection`) and by `__` in environment variable names (`ConnectionStrings__DefaultConnection`). A number after `__` refers to a position in a list (`Cors__AllowedOrigins__1`).

### Connection string
A single line of text with everything needed to connect to a database: server, port, database name, username, password and options such as SSL, separated by semicolons. It contains a password, so it's a secret.

### Control flow blocks
Angular's template syntax for logic: `@if (...) { } @else { }` shows blocks conditionally, and `@for (item of list; track item.id) { }` repeats a block for each item. `track` tells Angular how to identify each item so it only redraws the ones that changed.

### Controller
In ASP.NET Core, a class (here inheriting `ControllerBase`) whose methods handle HTTP requests. Attributes like `[HttpGet]` and `[Route]` decide which URL and [HTTP method](#http-methods) reach which method.

### CORS
**Cross-Origin Resource Sharing**. Browsers block a web page from reading responses from a different **origin** (a different protocol, domain or port) unless that server explicitly allows it with the `Access-Control-Allow-Origin` response header. Your Angular site and your API have different origins, so the API must list the Angular site's origin as allowed. Two important details: origins must match exactly (`https://`, no trailing slash), and when the server crashes with a 500 error, the response has no CORS header, so the browser reports a misleading CORS error.

### CSS and SCSS
**CSS** (Cascading Style Sheets) is the language for styling web pages: colors, sizes, spacing and layout. **SCSS** (Sassy CSS) is CSS with extras such as variables and nesting. It's compiled to plain CSS during the build.

### CSS variables
Named values defined once, usually on `:root`, and reused with `var(--name)`, for example `--navy: #1c2b4d;` then `background: var(--navy);`. Officially called "custom properties". They keep a whole design consistent and make color changes one-line edits.

### DbContext
The EF Core class representing a session with the database. Each `DbSet<T>` property is a table. You query through it and save changes with `SaveChangesAsync()`.

### Decorator
In TypeScript, a function written as `@Name(...)` just above a class that adds metadata or behavior. `@Component` marks a class as a component, and `@Injectable` marks it as available for [dependency injection](#dependency-injection). Similar to C# attributes like `[ApiController]`.

### Dependency injection
A design pattern where a class receives the objects it needs (its dependencies) instead of creating them. In .NET you register services in `Program.cs` (`builder.Services.Add...`) and receive them through constructors. In Angular you register them in `providers` or with `@Injectable({ providedIn: 'root' })` and receive them with `inject(...)`. It makes code easier to test and change. Often shortened to **DI**.

### DTO
**Data Transfer Object**: a simple class whose only job is to carry data over HTTP, for example `ApplicationDto` or `CreateApplicationDto`. DTOs keep the API's public shape separate from the database [entity](#entity).

### Endpoint
One URL + [HTTP method](#http-methods) combination that the API answers, for example `GET /api/applications/{id}`.

### Entity
A class that maps to a database table, where each instance is a row. Here, `Application` is the entity.

### Entity Framework Core
Microsoft's [ORM](#orm) for .NET, often shortened to **EF Core**. It translates C# (LINQ queries, `Add`, `Remove`, `SaveChangesAsync`) into [SQL](#sql), and manages the database schema with [migrations](#migration).

### Enum
A type with a fixed set of named values, like `ApplicationStatus { Applied, PhoneScreen, ... }`. In this project, the database stores it as a number and the API sends it as text.

### Environment files
In Angular, `src/environments/environment.ts` (production) and `environment.development.ts` (`ng serve`) hold values that differ by environment, like the API address. The build swaps in the right one automatically. Similar to `appsettings.json` versus `appsettings.Development.json`.

### Event binding
Angular template syntax with round brackets, `(event)="method()"`, that runs a method when something happens on screen: `(click)` for clicks, `(ngSubmit)` for form submission. Data flows from the screen to the class.

### F1 Free tier
The free App Service plan. It costs $0, gives 60 CPU minutes per day and 1 GB of memory, puts the app to sleep when idle (the first request afterward is slow), and doesn't support custom domains. It still requires an active Azure subscription.

### Flexbox
A CSS layout mode (`display: flex`) that arranges child elements in a row or column, with control over alignment, spacing (`gap`) and wrapping.

### FluentAssertions
A .NET testing library that makes assertions read like sentences: `result.Should().BeOfType<OkObjectResult>()`. Version 8 and later requires a paid license for commercial use.

### Git
A version control system that records every change to your code as [commits](#commit), lets you go back to any earlier version, and syncs with remote copies such as [GitHub](#github).

### GitHub
A website that hosts Git repositories, with collaboration features (pull requests, issues) and automation ([GitHub Actions](#github-actions)).

### GitHub Actions
GitHub's automation service. [Workflow](#workflow) files in `.github/workflows/` define jobs that run on GitHub's computers when something happens, for example a push to `main`.

### GitHub secret
An encrypted value stored in a repository's settings (**Settings → Secrets and variables → Actions**). Workflows reference it as `${{ secrets.NAME }}`. Nobody can read it back after saving, which makes it the right place for deployment credentials.

### .gitignore
A file listing paths [Git](#git) must ignore: build output (`bin/`, `obj/`, `dist/`), downloaded libraries (`node_modules/`), editor settings and local secrets. `dotnet new gitignore` creates a standard one for .NET.

### Google Fonts
A free library of web fonts. A `<link>` tag in `index.html` loads them, then CSS uses them by name, always with a fallback (`'Sora', system-ui, sans-serif`) in case the font doesn't load.

### Hex color
A color written as `#RRGGBB`: the amounts of red, green and blue, each from `00` to `ff` in base 16. For example, `#b42318` is a dark red.

### HTTP methods
The verbs of HTTP requests. **GET** reads data, **POST** creates, **PUT** replaces a whole record, **PATCH** changes part of a record, and **DELETE** removes. Together, create/read/update/delete is often called **CRUD**.

### HTTP status codes
Three-digit numbers in every HTTP response. **200** OK, **201** Created, **204** No Content (success with an empty body), **400** Bad Request (invalid input), **404** Not Found, and **500** Internal Server Error (the server crashed). 2xx means success, 4xx means the client sent something wrong, and 5xx means the server failed.

### HttpClient
The class for making HTTP requests. In Angular it's registered with `provideHttpClient()` and its methods (`get`, `post`, `put`, `patch`, `delete`) return [Observables](#observable). .NET has a class with the same name and purpose.

### HttpParams
Angular's immutable builder for URL query strings: `new HttpParams().set('page', 1).set('search', 'engineer')` produces `?page=1&search=engineer`, with URL-encoding handled automatically. Each `.set()` returns a new object.

### HTTPS
HTTP encrypted with [TLS](#ssl-and-tls), so nobody between the browser and the server can read or change the data. Both Azure services in this project provide HTTPS automatically.

### ILIKE
PostgreSQL's case-insensitive version of SQL `LIKE`. `ILIKE '%engineer%'` matches "Engineer", "ENGINEER" and "Software engineer". `%` means "any characters". In EF Core with Npgsql it's written `EF.Functions.ILike(column, pattern)`.

### Immutable update
Changing data by creating a new object or array instead of modifying the existing one, for example `apps.filter(...)`, which returns a new array. [Signals](#signal) detect changes by comparing values, so an in-place change to the same array wouldn't be noticed.

### Input
A property of an Angular component that its parent sets, declared with `input()` or `input.required()`. The parent passes it with [property binding](#property-binding): `<app-status-badge [status]="app.status" />`. With `withComponentInputBinding()`, the router can also fill inputs from URL [route parameters](#route-parameter). Inputs are [signals](#signal).

### Interceptor
In Angular, a function every [HttpClient](#httpclient) request and response passes through — the client-side equivalent of [middleware](#middleware). This project's interceptor adds the `Authorization: Bearer <token>` header and logs the user out when the API answers 401.

### Interface
A contract listing members without implementing them. In C#, `IApplicationRepository` lists the data operations, and classes implement it. In TypeScript, an `interface` describes the shape of an object and exists only at compile time.

### Interpolation
Inserting a value into text. In Angular templates it's written `{{ value }}`. In C# it's `$"{value}"`, and in TypeScript it's a [template literal](#template-literal).

### IQueryable
A .NET interface for a query that hasn't run yet. Each `.Where(…)`, `.OrderBy(…)`, `.Skip(…)` adds to it, and EF Core translates the whole chain into one SQL statement when you finally call something like `ToListAsync()` or `CountAsync()`. Filtering an `IQueryable` happens in the database; filtering a `List` happens in memory.

### JSON
**JavaScript Object Notation**, the text format APIs use to exchange data, for example `{"company": "Contoso", "id": 1}`. ASP.NET Core writes property names in camelCase.

### JWT
**JSON Web Token**: a compact, signed token in three dot-separated parts, `header.payload.signature`. The payload holds [claims](#claims) and an expiry time; the signature, made with a secret key, proves the token was issued by the API and hasn't been changed. The payload is only encoded, not encrypted, so anyone can read it — never put secrets inside. Tokens are sent in the `Authorization: Bearer <token>` header.

### Layered architecture
Organizing code into layers with one-way dependencies. Here: **Core** (business types, no dependencies) ← **Infrastructure** (database access) ← **Api** (HTTP). Inner layers never depend on outer ones, so the database or web framework can change without touching the business types.

### Lifecycle hook
A method Angular calls automatically at a specific moment in a component's life. `ngOnInit()` runs once after the component is created and its inputs are set, which makes it the conventional place to load data.

### localhost and port
`localhost` means "this computer". A **port** is a numbered door a server listens on, so `http://localhost:4200` is the Angular development server on your own machine, port 4200.

### localStorage
Browser storage that keeps small text values per site, even after the page is closed. Readable by any script on that site, so a token stored there could be stolen through [XSS](#xss); HttpOnly cookies are the more secure alternative for production.

### Middleware
In ASP.NET Core, the components every request passes through, in order, configured with `app.Use...` in `Program.cs`: HTTPS redirection, CORS, authorization, routing to controllers. Order matters.

### Migration
A versioned C# file, generated by `dotnet ef migrations add`, that describes how to change the database schema (create tables, add columns). `dotnet ef database update` applies any migrations the database doesn't have yet, and records them in the `__EFMigrationsHistory` table.

### Model binding
ASP.NET Core automatically filling controller method parameters from the request: route values (`{id}` → `int id`), query strings, and the JSON body (→ a DTO).

### Moq
A .NET library for creating fake objects (**mocks**) in tests. `new Mock<IApplicationRepository>()` creates a fake repository whose results you control with `.Setup(...).ReturnsAsync(...)`, so tests don't need a real database.

### .NET
Microsoft's free, cross-platform platform for building applications in C#. The **.NET SDK** (Software Development Kit) is what you install to write code: the compiler plus the `dotnet` command (`dotnet new`, `dotnet build`, `dotnet test`, `dotnet run`, `dotnet publish`). The **runtime** is what runs the compiled app; Azure App Service provides it. This project uses .NET 8, a **Long-Term Support (LTS)** release that receives security fixes for three years.

### Node.js
A program that runs JavaScript outside the browser. Angular's build tools are JavaScript programs, so they need Node.js installed.

### Npgsql
The .NET driver for [PostgreSQL](#postgresql). Its EF Core provider (`Npgsql.EntityFrameworkCore.PostgreSQL`) lets EF Core work with PostgreSQL, enabled with `UseNpgsql(...)`.

### npm
**Node Package Manager**: installs JavaScript libraries listed in `package.json` into `node_modules/`. It comes with Node.js. Roughly the JavaScript equivalent of NuGet.

### NuGet
The package manager for .NET. `dotnet add package <Name> --version <x>` adds a library reference to a `.csproj` file.

### nvm
**Node Version Manager**: installs several Node.js versions side by side and switches between them (`nvm install --lts`, `nvm alias default`). On Windows the equivalent tool is nvm-windows.

### Observable
From the **RxJS** library (Reactive Extensions for JavaScript), included with Angular. An Observable represents a value (or stream of values) that arrives later. Unlike a C# `Task`, it does **nothing until you call `.subscribe()`**. `subscribe({ next, error })` takes a function for the result and one for failures.

### ORM
**Object-Relational Mapper**: a library that lets you work with database rows as objects in your programming language, so you write C# instead of SQL for most operations. [EF Core](#entity-framework-core) is an ORM.

### Oryx
Microsoft's automatic build system, used by Azure Static Web Apps (and App Service) to detect a project's type and build it, for example running `npm install` and `npm run build` for Angular.

### Pagination
Returning a large result in pages. The client sends the page number and page size; the server skips the earlier rows and returns one page, plus the total count so the client can show "Page 2 of 4". In EF Core: `.Skip((page - 1) * pageSize).Take(pageSize)`, which becomes SQL `OFFSET … LIMIT …`. A stable sort order (with a unique tiebreaker such as `Id`) is required, or rows can repeat or go missing between pages.

### Password hashing
Storing a one-way scramble of a password instead of the password itself. At login, the submitted password is hashed the same way and compared. ASP.NET Core's `PasswordHasher` uses **PBKDF2**: a random **salt** per password (so identical passwords produce different hashes) and many iterations (so guessing is slow). If the database ever leaked, the passwords wouldn't.

### Pay-As-You-Go
An Azure billing plan where you pay monthly only for what you use beyond free amounts. You must upgrade to it to keep using Azure after the free trial's 30 days. Resources on free tiers still cost $0.

### PgBouncer
A connection pooler for PostgreSQL that keeps a set of database connections open and shares them between clients. Shared hosts often expose it on port `6432` instead of PostgreSQL's usual `5432`.

### Pipe
In Angular templates, a function that formats a value for display, written with `|`. `{{ app.appliedDate | date: 'mediumDate' }}` turns `2026-10-20` into `Oct 20, 2026`.

### PostgreSQL
A free, open-source relational database, often called **Postgres**. This project stores its data in it.

### ProblemDetails
The standard JSON format for HTTP API errors (RFC 9457): `{ "type", "title", "status", "detail", "instance" }`, with content type `application/problem+json`. Clients and tools understand it, so every error from the API has the same shape.

### Property binding
Angular template syntax with square brackets, `[property]="expression"`, that sets an element's property or a component's input from a value in the class. Data flows from the class to the screen.

### Publish profile
An XML file downloaded from an App Service that contains the credentials GitHub Actions uses to deploy to it. It's a secret: store it as a [GitHub secret](#github-secret) and delete the file afterward.

### Push
Sending your local [commits](#commit) to GitHub with `git push origin main`. In this project, each push to `main` starts both deployment workflows.

### Reactive forms
Angular's approach where the form is defined in TypeScript (fields, default values, [validators](#validators)) with `FormBuilder`, then connected to the HTML with `[formGroup]` and `formControlName`. Similar to a C# view model with validation attributes.

### Record
A C# type designed to carry data: `public record ApplicationPage(IReadOnlyList<Application> Items, int TotalCount, …);` declares the properties and the constructor in one line. Records are immutable by default and compare by value.

### Reflection
.NET's ability to inspect types, methods and attributes while the program runs, for example `typeof(ApplicationsController).GetMethod("Delete").GetCustomAttribute<AuthorizeAttribute>()`. The security tests use it to check that the `[Authorize]` locks are in place.

### Repository pattern
Putting all data access behind an [interface](#interface) (`IApplicationRepository`) with methods like `GetAllAsync`, `CreateAsync` and `DeleteAsync`. Controllers use the interface and never touch the database directly, which makes them easy to test with [mocks](#moq).

### Resource group
An Azure folder that holds related resources. Deleting the group deletes everything inside it.

### Route guard
In Angular, a function that runs before a route opens and returns `true` (let the user in) or a `UrlTree` (redirect). This project's guards keep the list behind login and Add/Edit for admins. Guards only improve the user experience; the API's `[Authorize]` attributes are the real protection.

### Route parameter
A placeholder in a URL pattern that captures part of the address: `{id}` in `[HttpGet("{id}")]` on the API, and `:id` in Angular's `'edit/:id'` route.

### Routing
Matching a URL to the code that handles it. In ASP.NET Core, attributes on controllers. In Angular, the `routes` list in `app.routes.ts`, which maps paths to components displayed in `<router-outlet />`.

### Runner
The temporary computer GitHub Actions uses to run a workflow job, chosen with `runs-on:` (for example `ubuntu-latest`). It starts empty each time, which is why workflows begin by checking out the code and installing tools.

### RxJS
**Reactive Extensions for JavaScript**, the library behind Angular's [Observables](#observable). Its operators transform streams of values over time: `debounceTime` (wait for a pause), `distinctUntilChanged` (skip repeats), `switchMap` (start a new request and cancel the previous one), `catchError` (handle failures), `tap` (side effects). A `Subject` is a stream you push values into yourself.

### Service
In Angular, a class marked `@Injectable` that holds logic or data shared by several components, such as `ApplicationService`, which makes every API call. Components get it through [dependency injection](#dependency-injection).

### Signal
Angular's reactive value container, created with `signal(initialValue)`. Read it by calling it (`loading()`), change it with `.set(value)` or `.update(fn)`. When a signal changes, Angular redraws only the parts of the screen that read it. In a [zoneless](#zoneless) app, signals are how Angular knows the screen needs updating.

### Solution
A `.sln` file that groups several .NET projects so they build, test and publish together with one command.

### SPA
**Single-Page Application**: a web app that loads one HTML page once, then changes what's displayed with JavaScript instead of loading new pages from the server. Angular apps are SPAs. Because every "page" is really `index.html`, the host needs a fallback rule (`staticwebapp.config.json`) for direct links and refreshes.

### Spread operator
TypeScript/JavaScript `...`, which copies all properties of an object into a new one: `{ ...original, company: 'New' }` makes a copy of `original` with `company` changed. Similar to a C# record's `with` expression.

### SQL
**Structured Query Language**, the language relational databases understand (`SELECT`, `INSERT`, `UPDATE`, `DELETE`). EF Core writes it for you.

### SSL and TLS
**Secure Sockets Layer** and its modern replacement, **Transport Layer Security**: the encryption that protects data in transit. HTTPS uses it, and database connections can use it (`Ssl Mode` in the connection string).

### SSR
**Server-Side Rendering**: generating a page's HTML on a server before sending it, mainly to help search engines. Not used in this project.

### Structured logging
Logging with named placeholders, `_logger.LogInformation("HTTP {Method} {Path} responded {StatusCode}", …)`, so each value is stored as a separate field rather than baked into text. Log tools can then filter and aggregate by field (for example every request with `StatusCode` 500). Avoid `$"…"` interpolation in log messages.

### Swagger
A tool that generates interactive documentation for an API from its code. You can read every endpoint and send test requests at `/swagger`. It's based on the **OpenAPI** specification. In .NET it's provided by the Swashbuckle package.

### Switch expression
A compact C# form of `switch` that returns a value: `var code = ex switch { KeyNotFoundException => 404, ArgumentException => 400, _ => 500 };`. `_` is the default case.

### Template
The HTML part of an Angular component, which may use Angular syntax: [interpolation](#interpolation), [bindings](#property-binding), [control flow blocks](#control-flow-blocks) and [pipes](#pipe).

### Template literal
A TypeScript/JavaScript string written with backticks that can embed values with `${...}`, for example `` `${baseUrl}/${id}` ``. Equivalent to C# `$"{baseUrl}/{id}"`.

### Template reference variable
In an Angular template, `#name` on an element gives it a name the template can use, for example `<input #searchBox (input)="onSearch(searchBox.value)">`.

### Two-way binding
Angular's `[(ngModel)]="signal"` syntax, nicknamed "banana in a box". It keeps a form control and a value in sync in both directions: the control shows the value, and user changes update the value. It requires `FormsModule`.

### Type narrowing
TypeScript working out a more specific type after a check. After `if (filter === 'All') return ...;`, TypeScript knows `filter` can only be a real status in the remaining code. Similar to C# `if (x is string s)`.

### TypeScript
JavaScript with static types, from Microsoft. Types are checked when compiling (catching mistakes early), then removed, so the browser runs plain JavaScript. Angular is written in TypeScript.

### Union type
A TypeScript type allowing one of several types or values, written with `|`: `'Applied' | 'Offer'`, or `string | null`. Here it plays the role a C# `enum` or nullable type would.

### Unit test
An automated test that checks one small piece of code (here, one controller method) in isolation, with dependencies replaced by [mocks](#moq). Run with `dotnet test`.

### User secrets
A .NET development feature that stores sensitive settings (like connection strings) in a file in your user profile folder, outside the repository, so they can't be committed. Set them with `dotnet user-secrets set`. They're read automatically when running in Development and override `appsettings` files.

### UTC
**Coordinated Universal Time**, the world's reference time zone with no daylight saving changes. Servers store timestamps in UTC and convert to local time only for display. Dates ending in `Z` (like `2026-10-26T04:39:10Z`) are in UTC.

### Validators
Rules attached to form fields in Angular [reactive forms](#reactive-forms), such as `Validators.required` and `Validators.maxLength(100)`. Failed rules are checked with `hasError('required')`, and Angular adds CSS classes like `ng-invalid` to the field.

### Visual Studio Code
A free code editor from Microsoft, often called **VS Code**, with extensions for C# and Angular.

### Vitest
The test runner used by new Angular 22 projects for `*.spec.ts` files (run with `ng test`). It replaces the older Karma runner.

### Workflow
A [YAML](#yaml) file in `.github/workflows/` defining an automated process: **when** it runs (`on:`), **where** (`runs-on:`), and **what** it does (`steps:`). This project has two: one for the API, one for the Angular app.

### XSS
**Cross-Site Scripting**: an attack where malicious script gets into a page (for example through unescaped user input) and runs with the page's permissions, able to read anything scripts can read, including [localStorage](#localstorage). Angular escapes interpolated values by default, which prevents most XSS.

### xUnit
A .NET testing framework. Methods marked `[Fact]` are tests, discovered and run by `dotnet test`.

### YAML
A text format for configuration that uses indentation (spaces, never tabs) to show structure. GitHub Actions workflows are written in YAML. Wrong indentation changes the meaning or breaks the file.

### Zoneless
An Angular mode (the default in recent versions) that doesn't use the old `zone.js` library to detect changes automatically. Instead, Angular updates the screen when [signals](#signal) change or when template events occur. Data that arrives later (such as an HTTP response) must be stored in signals to appear on screen.
