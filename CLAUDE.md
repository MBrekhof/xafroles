# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Session Startup (READ FIRST)

At the start of every session, **before doing any work**, read these files:

1. `CLAUDE.md` (this file)
2. `TODO.md` — current task list and progress
3. `SESSION_HANDOFF.md` — context from the previous session

These files ensure continuity between sessions. Do not skip this step.

## Workflow

1. **Plan first.** Every task must be planned before implementation begins. Discuss the approach, break it into steps, and get confirmation before writing code.
2. **Track work in `TODO.md`.** Keep it updated as tasks are started, completed, or added. Use it as the single source of truth for what needs to be done.
3. **Write `SESSION_HANDOFF.md` at the end.** Before the session ends, write a handoff summarizing: what was done, what's in progress, what's next, and any blockers or decisions made. This is the briefing for the next session.

## Project Overview

XafRoles is a DevExpress XAF (eXpressApp Framework) application with role-based security. It uses EF Core with SQL Server (localdb) and has two frontends: Blazor Server and WinForms.

## Solution Structure

```
XafRoles.slnx
├── XafRoles.Module          — Shared module: business objects, DbContext, security config, DB updater
├── XafRoles.Blazor.Server   — Blazor Server app with Web API (JWT + Cookie auth, Swagger, OData)
└── XafRoles.Win             — WinForms desktop app (net8.0-windows)
```

- **XafRoles.Module** is the core — both frontends reference it. All business objects and EF Core DbContext (`XafRolesEFCoreDbContext`) live here.
- **Blazor.Server** hosts the XAF Blazor UI plus a Web API layer with JWT authentication and Swagger/OData endpoints.
- **Win** is a standard XAF WinForms client.

## Build Commands

```bash
# Build entire solution
dotnet build XafRoles.slnx

# Build specific project
dotnet build XafRoles/XafRoles.Blazor.Server/XafRoles.Blazor.Server.csproj

# Run Blazor Server app
dotnet run --project XafRoles/XafRoles.Blazor.Server/XafRoles.Blazor.Server.csproj
```

The solution has three build configurations: `Debug`, `Release`, and `EasyTest`. The `EasyTest` config uses a separate connection string.

## Key Technical Details

- **.NET 8** / **DevExpress v25.2.3** across all projects
- **Database:** SQL Server LocalDB (`(localdb)\mssqllocaldb`), catalog `XafRoles`
- **ORM:** EF Core (not XPO) — DbContext is `XafRolesEFCoreDbContext` in `XafRoles.Module/BusinessObjects/XafRolesDbContext.cs`
- **Security:** XAF integrated security mode with `PermissionPolicyRole` and custom `ApplicationUser` type
- **Auth (Blazor):** Cookie auth for UI, JWT Bearer for API. JWT key configured in `appsettings.json` under `Authentication:Jwt`
- **API:** OData endpoints at `/api/odata`, Swagger at `/swagger` (dev only)
- **Seed data:** `DatabaseUpdate/Updater.cs` creates "Admin" and "User" accounts with empty passwords, plus "Administrators" and "Default" roles (non-Release builds only via `#if !RELEASE`)
- **Drawing:** Blazor project uses `DevExpress.Drawing.Skia` (relevant for Docker/Linux deployment)
- **PermissionsReloadMode:** Set to `NoCache` — permissions are reloaded from DB on every access

## EF Core Migrations

The Module project includes `Microsoft.EntityFrameworkCore.Design`. Run migrations from the Module project directory:

```bash
cd XafRoles/XafRoles.Module
dotnet ef migrations add <MigrationName> --startup-project ../XafRoles.Blazor.Server
dotnet ef database update --startup-project ../XafRoles.Blazor.Server
```

## Adding Business Objects

1. Create entity class in `XafRoles.Module/BusinessObjects/`
2. Add `DbSet<T>` to `XafRolesEFCoreDbContext`
3. To expose via Web API, uncomment/add `options.BusinessObject<T>()` in `Startup.cs` `AddXafWebApi` configuration
