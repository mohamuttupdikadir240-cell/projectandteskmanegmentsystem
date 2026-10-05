# Real Estate Project Management System (PMS)

A comprehensive real estate project management system built with ASP.NET Core 9 MVC, EF Core, and ASP.NET Core Identity.

## Live Deployment on Railway

The app is containerized and configured for zero-friction Railway deployment:
- **Public Domain**: `https://projectandteskmanegmentsystem-production.up.railway.app`
- **Port**: Listens on the dynamic `$PORT` supplied by Railway (or 8080 by default).
- **Database**:
  - Automatically connects to Railway's managed **PostgreSQL** if `DATABASE_URL` is present (add PostgreSQL with 1-click in Railway).
  - Automatically falls back to an embedded **SQLite** database (`App_Data/realestate.db`) if no PostgreSQL service is attached, ensuring the app is immediately alive without runtime crash!

## Demo Logins

| Role | Email | Password |
|---|---|---|
| **System Admin** | `admin@realestate.com` | `Admin@12345` |
| **Project Manager** | `pm@realestate.com` | `Manager@12345` |
| **Sales Agent** | `agent1@realestate.com` | `Agent@12345` |
| **Sales Agent** | `agent2@realestate.com` | `Agent@12345` |
| **Accountant** | `accountant@realestate.com` | `Accountant@12345` |
| **Customer** | `customer@realestate.com` | `Customer@12345` |

## Deployment Steps

1. Push changes to GitHub:
   ```bash
   git add .
   git commit -m "Fix database connection, add SQLite fallback, and configure Railway deployment"
   git push origin main
   ```
2. Railway detects the commit on `main` and automatically rebuilds the Docker container.
3. Once the build completes, visit your domain:
   **`https://projectandteskmanegmentsystem-production.up.railway.app`**
