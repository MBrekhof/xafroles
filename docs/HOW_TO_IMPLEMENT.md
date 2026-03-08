# How to Implement Role Export/Import in Your Own XAF Application

This guide explains how to add the role export/import feature to any XAF application using EF Core and `PermissionPolicyRole`.

## Prerequisites

- XAF application with EF Core (not XPO)
- `PermissionPolicyRole` as the role type (or a subclass of it)
- `System.Text.Json` (included in .NET 8+)

## Step 1: Copy the RoleExport folder

Copy the entire `XafRoles.Module/RoleExport/` folder into your own shared Module project:

```
YourApp.Module/
  RoleExport/
    Dtos/
      RoleExportDocument.cs
      RoleDto.cs
      TypePermissionDto.cs
      ObjectPermissionDto.cs
      MemberPermissionDto.cs
      NavigationPermissionDto.cs
      ActionPermissionDto.cs
    JsonSettings.cs
    RoleExportService.cs
    RoleImportService.cs
    RoleExportImportController.cs
```

## Step 2: Update namespaces

Find and replace `XafRoles.Module.RoleExport` with `YourApp.Module.RoleExport` across all copied files.

## Step 3: Adjust for custom role types

If you use a custom role class (e.g., `ExtendedSecurityRole` inheriting from `PermissionPolicyRole`):

**RoleExportImportController.cs** — change the `TargetObjectType`:
```csharp
TargetObjectType = typeof(ExtendedSecurityRole);
```

**RoleExportService.cs** — change the query type:
```csharp
var roles = objectSpace.GetObjectsQuery<ExtendedSecurityRole>(true).ToList();
```

**RoleImportService.cs** — change `FirstOrDefault` and `CreateObject`:
```csharp
var existingRole = objectSpace.FirstOrDefault<ExtendedSecurityRole>(r => r.Name == roleDto.Name);
// ...
var role = objectSpace.CreateObject<ExtendedSecurityRole>();
```

If your custom role has additional properties (e.g., `CanExport`), add them to `RoleDto` and update the mapping in both services.

## Step 4: Build and verify

```bash
dotnet build YourApp.Module/YourApp.Module.csproj
```

The controller automatically registers with XAF — no additional wiring needed. It activates on the Role ListView.

## Step 5: Test the round-trip

1. Run your app and create some roles with various permissions
2. Navigate to Roles list, click **Export Roles**
3. Open the exported JSON and inspect it
4. Copy it to `roles-import.json` in your app's base directory
5. Delete or modify a role in the UI
6. Click **Import Roles**
7. Verify the role was restored/created

## How It Works

### Export

`RoleExportService.Export(IObjectSpace)` walks the `PermissionPolicyRole` aggregate:

```
Role
├── Name, IsAdministrative, CanEditModel, PermissionPolicy
├── TypePermissions[] (per business object type)
│   ├── TargetType (full CLR name), Read/Write/Create/Delete/Navigate states
│   ├── ObjectPermissions[] (criteria-based row-level)
│   └── MemberPermissions[] (field-level)
├── NavigationPermissions[] (menu items)
└── ActionPermissions[] (XAF actions)
```

Each level is mapped to a DTO and serialized with `System.Text.Json` (camelCase, null-skipping).

### Import

`RoleImportService.Import(IObjectSpace, string)` deserializes the JSON and for each role:

1. **Find by name** — if exists, clear all permission collections (children first, then parents)
2. **Create if new** — create the role object
3. **Apply permissions** — recreate all permission objects from the DTOs
4. **Commit** — single `ObjectSpace.CommitChanges()` at the end

This "clear + recreate" approach avoids complex diffing of individual permissions.

### Type Resolution

The `TargetType` on `PermissionPolicyTypePermissionObject` is `System.Type`. On export, it's stored as the full CLR type name (`Type.FullName`). On import, it's resolved through three fallbacks:

1. `XafTypesInfo.Instance.FindTypeInfo(name)` — the XAF type system
2. `Type.GetType(name)` — standard .NET resolution
3. Assembly scan — iterates loaded assemblies as a last resort

## Customization

### Adding custom properties to the export

If your roles have custom properties:

1. Add the property to `RoleDto.cs`
2. Map it in `RoleExportService.MapRole()`
3. Set it in `RoleImportService.CreateRole()` and `UpdateRole()`

### Changing the import file location

Edit `RoleExportImportController.ImportAction_Execute()` — change the `importPath` variable to your preferred convention.

### Adding Blazor file download

Override the controller in your Blazor project to use `IJSRuntime` for browser file downloads instead of writing to the server filesystem.

### Adding WinForms file dialogs

Override the controller in your Win project to use `SaveFileDialog` / `OpenFileDialog` instead of the convention-based paths.

## Limitations

- Does not export/import user-role assignments (users differ across environments)
- Import is merge/upsert only — it never deletes roles not present in the file
- No conflict detection — importing always overwrites the target role's permissions
- The Blazor export currently writes to the server's temp folder (no browser download yet)
