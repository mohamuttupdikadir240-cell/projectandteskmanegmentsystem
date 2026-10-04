# Real Estate Project Management System

A complete real estate project management system built with ASP.NET Core 9 MVC, Entity Framework Core, SQL Server, and ASP.NET Core Identity.

## Tech Stack

- ASP.NET Core 9 MVC (Controllers + Razor Views)
- Entity Framework Core 9 (Code First) + SQL Server
- ASP.NET Core Identity (roles, authentication, authorization)
- Bootstrap 5, Bootstrap Icons, Chart.js
- QuestPDF (PDF export) and ClosedXML (Excel export)

## Project Structure

```
RealEstatePMS/
  Controllers/       MVC controllers (one per module)
  Models/
    Entities/         EF Core entities
    Enums/            Status/type enums
  Data/               ApplicationDbContext + DbInitializer (seed data)
  Repositories/        Generic repository (IRepository<T> / Repository<T>)
  Services/            Business logic (Sales, Rentals, Payments, Notifications, Export, File storage, Dashboard)
  ViewModels/           View-specific models (Account, Dashboard, Reports, Property form, etc.)
  ViewComponents/       NotificationBell dropdown
  Helpers/              PaginatedList<T>, StatusBadge
  Views/                Razor views, organized by controller
  wwwroot/              Static assets, uploaded files under wwwroot/uploads
  Migrations/           EF Core Code First migrations
```

## 1. Configure the SQL Server connection

Open `appsettings.json` and set `ConnectionStrings:DefaultConnection` to point at your SQL Server instance.

**Option A – SQL Server LocalDB** (comes with Visual Studio):
```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=RealEstatePMS;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

**Option B – A full SQL Server instance (Express/Developer/Standard) with Windows auth:**
```json
"DefaultConnection": "Server=localhost;Database=RealEstatePMS;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```
(the project currently ships configured this way, since that's what was available to develop against)

**Option C – SQL Server auth (username/password), e.g. a named instance or Azure SQL:**
```json
"DefaultConnection": "Server=YOUR_SERVER;Database=RealEstatePMS;User Id=YOUR_USER;Password=YOUR_PASSWORD;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

The database itself does not need to exist beforehand — the migration step below creates it.

## 2. Run the EF Core migrations

From the `RealEstatePMS` project folder:

```bash
dotnet tool install --global dotnet-ef   # only if you don't already have it
dotnet ef database update
```

Or, in Visual Studio's **Package Manager Console** (with `RealEstatePMS` set as the default project):

```powershell
Update-Database
```

This creates the `RealEstatePMS` database with all tables, foreign keys, and indexes.

If you ever change the entity models, generate a new migration first:

```bash
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

## 3. Run the application

**From Visual Studio:** open `RealEstatePMS.sln`, set `RealEstatePMS` as the startup project, and press F5 (or Ctrl+F5).

**From the CLI:**
```bash
dotnet run
```

On first run, `DbInitializer` automatically:
- Applies any pending migrations
- Creates the 5 roles (Admin, ProjectManager, SalesAgent, Accountant, Customer)
- Seeds demo user accounts (see below)
- Seeds 3 sample projects, 5 properties, 4 customers, a sample sale, a sample rental, related payments, and sample project tasks

## Demo Accounts

| Role            | Email                        | Password         |
|-----------------|------------------------------|-------------------|
| Admin           | admin@realestate.com         | Admin@12345       |
| Project Manager | pm@realestate.com            | Manager@12345     |
| Sales Agent     | agent1@realestate.com        | Agent@12345       |
| Sales Agent     | agent2@realestate.com        | Agent@12345       |
| Accountant      | accountant@realestate.com    | Accountant@12345  |
| Customer        | customer@realestate.com      | Customer@12345    |

## Notes

- **File uploads** (property images, profile photos, documents) are stored under `wwwroot/uploads/` and served as static files. This is fine for local/single-instance deployments; for production behind a load balancer or in a container, point this at shared/blob storage instead.
- **Forgot Password** does not send a real email (no SMTP provider is configured). The reset link is written to the application log and also shown directly on the confirmation page, so the full flow can still be exercised end-to-end. Wire up `IEmailSender`/SMTP (or SendGrid, etc.) before shipping to production.
- **QuestPDF** is used under its free Community license (set in `Program.cs`); this is valid for small businesses/individuals per QuestPDF's licensing terms — review it if this project is used commercially at scale.
- Role-based authorization is enforced at the controller level via `[Authorize(Roles = "...")]`; see `Models/Roles.cs` for the role-group constants used across controllers.
- Business rules that are enforced automatically:
  - Creating a **Sale** marks the property **Sold** and records the deposit as an initial payment.
  - Creating a **Rental** marks the property **Rented** and records the deposit as an initial payment.
  - Deleting a Sale/Rental releases the property back to **Available**.
  - Recording/deleting a **Payment** recalculates the related Sale/Rental's remaining balance and payment status automatically.
  - A background check (`INotificationService.GenerateSystemNotificationsAsync`, run on every dashboard load) raises notifications for pending payments, contracts expiring within 30 days, and delayed projects.

## Cleanup

An earlier, empty `real state/` folder (default Razor Pages template scaffold, unrelated to this project) is still present one level up in `Desktop/realstate/`. It isn't referenced by anything here and can be deleted manually.
