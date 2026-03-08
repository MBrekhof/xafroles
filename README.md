# XafRoles

DevExpress XAF application demonstrating role-based security with export/import capability for synchronizing roles across DEV/TEST/PROD environments.

## Prerequisites

- .NET 8 SDK
- SQL Server LocalDB (included with Visual Studio)
- DevExpress XAF 25.2.3 license/NuGet feed

## Quick Start

```bash
dotnet build XafRoles.slnx
dotnet run --project XafRoles/XafRoles.Blazor.Server/XafRoles.Blazor.Server.csproj
```

Login: `Admin` / (empty password)

## Solution Structure

| Project | Description |
|---------|-------------|
| `XafRoles.Module` | Shared module — business objects, security, role export/import |
| `XafRoles.Blazor.Server` | Blazor Server frontend + Web API (JWT, Swagger, OData) |
| `XafRoles.Win` | WinForms desktop client |

## Role Export/Import

Enables synchronizing XAF security roles between environments via JSON files.

### Exporting

1. Navigate to the **Roles** list view
2. Click **Export Roles** in the toolbar
3. All roles are exported to a JSON file — the path is shown in a success message

### Importing

1. Copy your JSON file to the application's base directory as `roles-import.json`
2. Navigate to the **Roles** list view
3. Click **Import Roles**
4. Roles are merged by name: existing roles are updated, new roles are created, roles not in the file are left untouched

### JSON Format

The export produces a file containing all roles with their full permission hierarchy:

```json
{
  "exportedAt": "2026-03-08T14:30:00Z",
  "roles": [
    {
      "name": "Manager",
      "isAdministrative": false,
      "canEditModel": false,
      "permissionPolicy": "DenyAllByDefault",
      "typePermissions": [ ... ],
      "navigationPermissions": [ ... ],
      "actionPermissions": [ ... ]
    }
  ]
}
```

Each type permission includes nested object permissions (criteria-based row filtering) and member permissions (field-level access).

### Typical Workflow

```
DEV                          PROD
 |                            |
 |  Configure roles in UI    |
 |  Click "Export Roles"      |
 |  Commit JSON to git        |
 |                            |
 |  ---- deploy + JSON ---->  |
 |                            |
 |                  Place roles-import.json
 |                  Click "Import Roles"
 |                  Roles synchronized
```

## Seed Data

The application creates four roles on first run (non-Release builds):

| Role | Description |
|------|-------------|
| Administrators | Full access (`IsAdministrative = true`) |
| Default | Basic self-service (own profile, password change) |
| Manager | CRUD on users, read-only on roles, navigation to user/role lists |
| ReadOnly | Read-only access to users and roles, MyDetails navigation only |

## Database

SQL Server LocalDB, auto-created on first run:
- Connection string in `appsettings.json`
- Database: `XafRoles`
- No EF Core migrations — XAF handles schema updates automatically
