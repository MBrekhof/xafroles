# Role Export/Import Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Enable JSON export/import of XAF security roles for DEV/TEST/PROD synchronization.

**Architecture:** DTO-based serialization of the PermissionPolicyRole aggregate via XAF ObjectSpace. A ViewController provides Export/Import actions on the Role ListView. Import uses merge/upsert by role name.

**Tech Stack:** .NET 8, DevExpress XAF 25.2.3, EF Core, System.Text.Json

**Design doc:** `docs/plans/2026-03-08-role-export-import-design.md`

---

### Task 1: Create DTO classes

**Files:**
- Create: `XafRoles/XafRoles.Module/RoleExport/Dtos/RoleExportDocument.cs`
- Create: `XafRoles/XafRoles.Module/RoleExport/Dtos/RoleDto.cs`
- Create: `XafRoles/XafRoles.Module/RoleExport/Dtos/TypePermissionDto.cs`
- Create: `XafRoles/XafRoles.Module/RoleExport/Dtos/ObjectPermissionDto.cs`
- Create: `XafRoles/XafRoles.Module/RoleExport/Dtos/MemberPermissionDto.cs`
- Create: `XafRoles/XafRoles.Module/RoleExport/Dtos/NavigationPermissionDto.cs`
- Create: `XafRoles/XafRoles.Module/RoleExport/Dtos/ActionPermissionDto.cs`

**Step 1: Create all DTO files**

```csharp
// RoleExportDocument.cs
namespace XafRoles.Module.RoleExport.Dtos;

public class RoleExportDocument
{
    public DateTime ExportedAt { get; set; }
    public List<RoleDto> Roles { get; set; } = [];
}
```

```csharp
// RoleDto.cs
namespace XafRoles.Module.RoleExport.Dtos;

public class RoleDto
{
    public string Name { get; set; } = "";
    public bool IsAdministrative { get; set; }
    public bool CanEditModel { get; set; }
    public string PermissionPolicy { get; set; } = "DenyAllByDefault";
    public List<TypePermissionDto> TypePermissions { get; set; } = [];
    public List<NavigationPermissionDto> NavigationPermissions { get; set; } = [];
    public List<ActionPermissionDto> ActionPermissions { get; set; } = [];
}
```

```csharp
// TypePermissionDto.cs
namespace XafRoles.Module.RoleExport.Dtos;

public class TypePermissionDto
{
    public string TargetType { get; set; } = "";
    public string? ReadState { get; set; }
    public string? WriteState { get; set; }
    public string? CreateState { get; set; }
    public string? DeleteState { get; set; }
    public string? NavigateState { get; set; }
    public List<ObjectPermissionDto> ObjectPermissions { get; set; } = [];
    public List<MemberPermissionDto> MemberPermissions { get; set; } = [];
}
```

```csharp
// ObjectPermissionDto.cs
namespace XafRoles.Module.RoleExport.Dtos;

public class ObjectPermissionDto
{
    public string? Criteria { get; set; }
    public string? ReadState { get; set; }
    public string? WriteState { get; set; }
    public string? DeleteState { get; set; }
    public string? NavigateState { get; set; }
}
```

```csharp
// MemberPermissionDto.cs
namespace XafRoles.Module.RoleExport.Dtos;

public class MemberPermissionDto
{
    public string? Members { get; set; }
    public string? Criteria { get; set; }
    public string? ReadState { get; set; }
    public string? WriteState { get; set; }
}
```

```csharp
// NavigationPermissionDto.cs
namespace XafRoles.Module.RoleExport.Dtos;

public class NavigationPermissionDto
{
    public string ItemPath { get; set; } = "";
    public string? NavigateState { get; set; }
}
```

```csharp
// ActionPermissionDto.cs
namespace XafRoles.Module.RoleExport.Dtos;

public class ActionPermissionDto
{
    public string ActionId { get; set; } = "";
}
```

**Step 2: Build to verify compilation**

Run: `dotnet build XafRoles/XafRoles.Module/XafRoles.Module.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add XafRoles/XafRoles.Module/RoleExport/Dtos/
git commit -m "feat: add DTO classes for role export/import"
```

---

### Task 2: Create RoleExportService

**Files:**
- Create: `XafRoles/XafRoles.Module/RoleExport/RoleExportService.cs`

**Docs to check:** The `PermissionPolicyRoleBase` properties from DevExpress docs:
- `TypePermissions` → `IList<PermissionPolicyTypePermissionObject>` — each has `TargetType` (string), `ReadState`, `WriteState`, `CreateState`, `DeleteState`, `NavigateState` (all `SecurityPermissionState?`), plus `ObjectPermissions` and `MemberPermissions` collections
- `NavigationPermissions` → `IList<PermissionPolicyNavigationPermissionObject>` — each has `ItemPath` (string) and `NavigateState`
- `ActionPermissions` → `IList<PermissionPolicyActionPermissionObject>` — each has `ActionId`
- `PermissionPolicyObjectPermissionsObject` has `Criteria`, `ReadState`, `WriteState`, `DeleteState`, `NavigateState`
- `PermissionPolicyMemberPermissionsObject` has `Members`, `Criteria`, `ReadState`, `WriteState`
- `PermissionPolicy` property is of type `SecurityPermissionPolicyPermissionPolicy` enum (DenyAllByDefault=0, ReadAllByDefault=1, AllowAllByDefault=2)

**Step 1: Write the export service**

```csharp
// RoleExportService.cs
using System.Text.Json;
using System.Text.Json.Serialization;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using XafRoles.Module.RoleExport.Dtos;

namespace XafRoles.Module.RoleExport;

public static class RoleExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Export(IObjectSpace objectSpace)
    {
        var roles = objectSpace.GetObjectsQuery<PermissionPolicyRole>(true).ToList();
        var document = new RoleExportDocument
        {
            ExportedAt = DateTime.UtcNow,
            Roles = roles.Select(MapRole).ToList()
        };
        return JsonSerializer.Serialize(document, JsonOptions);
    }

    private static RoleDto MapRole(PermissionPolicyRole role)
    {
        return new RoleDto
        {
            Name = role.Name,
            IsAdministrative = role.IsAdministrative,
            CanEditModel = role.CanEditModel,
            PermissionPolicy = role.PermissionPolicy.ToString(),
            TypePermissions = role.TypePermissions.Select(MapTypePermission).ToList(),
            NavigationPermissions = role.NavigationPermissions.Select(MapNavigationPermission).ToList(),
            ActionPermissions = role.ActionPermissions.Select(MapActionPermission).ToList()
        };
    }

    private static TypePermissionDto MapTypePermission(PermissionPolicyTypePermissionObject tp)
    {
        return new TypePermissionDto
        {
            TargetType = tp.TargetType,
            ReadState = tp.ReadState?.ToString(),
            WriteState = tp.WriteState?.ToString(),
            CreateState = tp.CreateState?.ToString(),
            DeleteState = tp.DeleteState?.ToString(),
            NavigateState = tp.NavigateState?.ToString(),
            ObjectPermissions = tp.ObjectPermissions.Select(MapObjectPermission).ToList(),
            MemberPermissions = tp.MemberPermissions.Select(MapMemberPermission).ToList()
        };
    }

    private static ObjectPermissionDto MapObjectPermission(PermissionPolicyObjectPermissionsObject op)
    {
        return new ObjectPermissionDto
        {
            Criteria = op.Criteria,
            ReadState = op.ReadState?.ToString(),
            WriteState = op.WriteState?.ToString(),
            DeleteState = op.DeleteState?.ToString(),
            NavigateState = op.NavigateState?.ToString()
        };
    }

    private static MemberPermissionDto MapMemberPermission(PermissionPolicyMemberPermissionsObject mp)
    {
        return new MemberPermissionDto
        {
            Members = mp.Members,
            Criteria = mp.Criteria,
            ReadState = mp.ReadState?.ToString(),
            WriteState = mp.WriteState?.ToString()
        };
    }

    private static NavigationPermissionDto MapNavigationPermission(PermissionPolicyNavigationPermissionObject np)
    {
        return new NavigationPermissionDto
        {
            ItemPath = np.ItemPath,
            NavigateState = np.NavigateState?.ToString()
        };
    }

    private static ActionPermissionDto MapActionPermission(PermissionPolicyActionPermissionObject ap)
    {
        return new ActionPermissionDto
        {
            ActionId = ap.ActionId
        };
    }
}
```

**Step 2: Build to verify compilation**

Run: `dotnet build XafRoles/XafRoles.Module/XafRoles.Module.csproj`
Expected: Build succeeded

**Note:** The `PermissionPolicyTypePermissionObject` type name `TargetType` is a string property. The `ObjectPermissions` and `MemberPermissions` are navigation collections on the type permission object. If `TargetType` is actually a `Type` property, adjust to use `tp.TargetType.FullName` instead — check the DevExpress MCP docs for `PermissionPolicyTypePermissionObject.TargetType` to confirm.

**Step 3: Commit**

```bash
git add XafRoles/XafRoles.Module/RoleExport/RoleExportService.cs
git commit -m "feat: add RoleExportService to serialize roles to JSON"
```

---

### Task 3: Create RoleImportService

**Files:**
- Create: `XafRoles/XafRoles.Module/RoleExport/RoleImportService.cs`

**Step 1: Write the import service**

```csharp
// RoleImportService.cs
using System.Text.Json;
using System.Text.Json.Serialization;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using XafRoles.Module.RoleExport.Dtos;

namespace XafRoles.Module.RoleExport;

public record ImportResult(int Updated, int Created);

public static class RoleImportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static ImportResult Import(IObjectSpace objectSpace, string json)
    {
        var document = JsonSerializer.Deserialize<RoleExportDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("Invalid role export file.");

        int updated = 0;
        int created = 0;

        foreach (var roleDto in document.Roles)
        {
            var existingRole = objectSpace.FirstOrDefault<PermissionPolicyRole>(r => r.Name == roleDto.Name);
            if (existingRole != null)
            {
                UpdateRole(objectSpace, existingRole, roleDto);
                updated++;
            }
            else
            {
                CreateRole(objectSpace, roleDto);
                created++;
            }
        }

        objectSpace.CommitChanges();
        return new ImportResult(updated, created);
    }

    private static void UpdateRole(IObjectSpace objectSpace, PermissionPolicyRole role, RoleDto dto)
    {
        role.IsAdministrative = dto.IsAdministrative;
        role.CanEditModel = dto.CanEditModel;
        role.PermissionPolicy = ParsePermissionPolicy(dto.PermissionPolicy);

        ClearPermissions(objectSpace, role);
        ApplyPermissions(objectSpace, role, dto);
    }

    private static void CreateRole(IObjectSpace objectSpace, RoleDto dto)
    {
        var role = objectSpace.CreateObject<PermissionPolicyRole>();
        role.Name = dto.Name;
        role.IsAdministrative = dto.IsAdministrative;
        role.CanEditModel = dto.CanEditModel;
        role.PermissionPolicy = ParsePermissionPolicy(dto.PermissionPolicy);

        ApplyPermissions(objectSpace, role, dto);
    }

    private static void ClearPermissions(IObjectSpace objectSpace, PermissionPolicyRole role)
    {
        // Delete all child permission objects — they are aggregated (owned) by the role.
        while (role.TypePermissions.Count > 0)
        {
            var tp = role.TypePermissions[0];
            while (tp.ObjectPermissions.Count > 0)
                objectSpace.Delete(tp.ObjectPermissions[0]);
            while (tp.MemberPermissions.Count > 0)
                objectSpace.Delete(tp.MemberPermissions[0]);
            objectSpace.Delete(tp);
        }
        while (role.NavigationPermissions.Count > 0)
            objectSpace.Delete(role.NavigationPermissions[0]);
        while (role.ActionPermissions.Count > 0)
            objectSpace.Delete(role.ActionPermissions[0]);
    }

    private static void ApplyPermissions(IObjectSpace objectSpace, PermissionPolicyRole role, RoleDto dto)
    {
        foreach (var tpDto in dto.TypePermissions)
        {
            var tp = objectSpace.CreateObject<PermissionPolicyTypePermissionObject>();
            tp.TargetType = tpDto.TargetType;
            tp.ReadState = ParseState(tpDto.ReadState);
            tp.WriteState = ParseState(tpDto.WriteState);
            tp.CreateState = ParseState(tpDto.CreateState);
            tp.DeleteState = ParseState(tpDto.DeleteState);
            tp.NavigateState = ParseState(tpDto.NavigateState);
            role.TypePermissions.Add(tp);

            foreach (var opDto in tpDto.ObjectPermissions)
            {
                var op = objectSpace.CreateObject<PermissionPolicyObjectPermissionsObject>();
                op.Criteria = opDto.Criteria;
                op.ReadState = ParseState(opDto.ReadState);
                op.WriteState = ParseState(opDto.WriteState);
                op.DeleteState = ParseState(opDto.DeleteState);
                op.NavigateState = ParseState(opDto.NavigateState);
                tp.ObjectPermissions.Add(op);
            }

            foreach (var mpDto in tpDto.MemberPermissions)
            {
                var mp = objectSpace.CreateObject<PermissionPolicyMemberPermissionsObject>();
                mp.Members = mpDto.Members;
                mp.Criteria = mpDto.Criteria;
                mp.ReadState = ParseState(mpDto.ReadState);
                mp.WriteState = ParseState(mpDto.WriteState);
                tp.MemberPermissions.Add(mp);
            }
        }

        foreach (var navDto in dto.NavigationPermissions)
        {
            var np = objectSpace.CreateObject<PermissionPolicyNavigationPermissionObject>();
            np.ItemPath = navDto.ItemPath;
            np.NavigateState = ParseState(navDto.NavigateState);
            role.NavigationPermissions.Add(np);
        }

        foreach (var actDto in dto.ActionPermissions)
        {
            var ap = objectSpace.CreateObject<PermissionPolicyActionPermissionObject>();
            ap.ActionId = actDto.ActionId;
            role.ActionPermissions.Add(ap);
        }
    }

    private static SecurityPermissionState? ParseState(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        return Enum.Parse<SecurityPermissionState>(value);
    }

    private static SecurityPermissionPolicyPermissionPolicy ParsePermissionPolicy(string value)
    {
        return Enum.Parse<SecurityPermissionPolicyPermissionPolicy>(value);
    }
}
```

**Step 2: Build to verify compilation**

Run: `dotnet build XafRoles/XafRoles.Module/XafRoles.Module.csproj`
Expected: Build succeeded

**Note:** `TargetType` on the type permission object may be a `Type` property rather than `string`. If so, the import will need `tp.TargetType = objectSpace.TypesInfo.FindTypeInfo(tpDto.TargetType)?.Type` or similar. Check compilation and adjust. Also, `SecurityPermissionPolicyPermissionPolicy` is the enum name — verify in DevExpress docs if the exact name differs (it may just be `SecurityPermissionPolicy`).

**Step 3: Commit**

```bash
git add XafRoles/XafRoles.Module/RoleExport/RoleImportService.cs
git commit -m "feat: add RoleImportService to import roles from JSON"
```

---

### Task 4: Create the XAF Controller

**Files:**
- Create: `XafRoles/XafRoles.Module/RoleExport/RoleExportImportController.cs`

**Docs to check:** XAF ViewController pattern — `ViewController<ListView>`, `SimpleAction`, `TargetObjectType`. For file operations in a platform-agnostic module, we use `Application.ShowViewStrategy` and write to a temp file, then let the user handle it. However, since Blazor needs a download and WinForms needs a SaveFileDialog, the simplest cross-platform approach is:
- Export: write JSON to a temp file and open a `DetailView` or use `Application.MainWindow` — OR, simpler: just use the XAF `PopupWindowShowAction` with a non-persistent file-holder object. Simplest: copy JSON to clipboard or save to a well-known path.

For maximum simplicity in this first iteration, use a **`PopupWindowShowAction`** for import (file selection) and a **`SimpleAction`** for export that saves to a file and shows the path. Platform-specific file dialogs can be refined later.

**Step 1: Write the controller**

```csharp
// RoleExportImportController.cs
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;

namespace XafRoles.Module.RoleExport;

public class RoleExportImportController : ViewController<ListView>
{
    private readonly SimpleAction _exportAction;
    private readonly SimpleAction _importAction;

    public RoleExportImportController()
    {
        TargetObjectType = typeof(PermissionPolicyRole);

        _exportAction = new SimpleAction(this, "ExportRoles", "Edit")
        {
            Caption = "Export Roles",
            ImageName = "Action_Export",
            SelectionDependencyType = SelectionDependencyType.Independent
        };
        _exportAction.Execute += ExportAction_Execute;

        _importAction = new SimpleAction(this, "ImportRoles", "Edit")
        {
            Caption = "Import Roles",
            ImageName = "Action_Import",
            SelectionDependencyType = SelectionDependencyType.Independent
        };
        _importAction.Execute += ImportAction_Execute;
    }

    private void ExportAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        var json = RoleExportService.Export(ObjectSpace);
        var fileName = $"roles-export-{DateTime.Now:yyyy-MM-dd-HHmmss}.json";
        var filePath = Path.Combine(Path.GetTempPath(), fileName);
        File.WriteAllText(filePath, json);

        Application.ShowViewStrategy.ShowMessage(
            $"Roles exported to: {filePath}",
            InformationType.Success);
    }

    private void ImportAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        // For now, look for a file named "roles-import.json" in the app's working directory.
        // Platform-specific file dialogs can be added later as Blazor/WinForms overrides.
        var importPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "roles-import.json");
        if (!File.Exists(importPath))
        {
            Application.ShowViewStrategy.ShowMessage(
                $"Place the import file at: {importPath}",
                InformationType.Warning);
            return;
        }

        var json = File.ReadAllText(importPath);
        var result = RoleImportService.Import(ObjectSpace, json);
        ObjectSpace.Refresh();
        View.Refresh();

        Application.ShowViewStrategy.ShowMessage(
            $"Import complete: {result.Created} created, {result.Updated} updated.",
            InformationType.Success);
    }
}
```

**Step 2: Build the entire solution**

Run: `dotnet build XafRoles.slnx`
Expected: Build succeeded

**Note:** The import uses a convention-based file path (`roles-import.json` in the app directory) as the simplest cross-platform approach. This avoids platform-specific file dialog dependencies in the shared Module. The export writes to temp and shows the path. These can be refined with platform-specific Blazor file download / WinForms dialogs in a later iteration.

**Step 3: Commit**

```bash
git add XafRoles/XafRoles.Module/RoleExport/RoleExportImportController.cs
git commit -m "feat: add XAF controller with Export/Import role actions"
```

---

### Task 5: Add sample roles to Updater.cs

**Files:**
- Modify: `XafRoles/XafRoles.Module/DatabaseUpdate/Updater.cs`

**Step 1: Add Manager and ReadOnly roles to UpdateDatabaseAfterUpdateSchema**

Add two new methods and call them from `UpdateDatabaseAfterUpdateSchema`, alongside the existing `CreateDefaultRole()` and `CreateAdminRole()` calls:

```csharp
// Add these calls after the existing role creation in UpdateDatabaseAfterUpdateSchema:
var managerRole = CreateManagerRole();
var readOnlyRole = CreateReadOnlyRole();

// Then add these methods to the Updater class:

PermissionPolicyRole CreateManagerRole()
{
    PermissionPolicyRole role = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(r => r.Name == "Manager");
    if (role == null)
    {
        role = ObjectSpace.CreateObject<PermissionPolicyRole>();
        role.Name = "Manager";
        role.PermissionPolicy = SecurityPermissionPolicyPermissionPolicy.DenyAllByDefault;

        // Full CRUD + Navigate on ApplicationUser
        role.AddTypePermissionsRecursively<ApplicationUser>(SecurityOperations.CRUDAccess, SecurityPermissionState.Allow);
        role.AddTypePermissionsRecursively<ApplicationUser>(SecurityOperations.Navigate, SecurityPermissionState.Allow);

        // Read-only on roles
        role.AddTypePermissionsRecursively<PermissionPolicyRole>(SecurityOperations.Read, SecurityPermissionState.Allow);

        // Navigate to user list and role list
        role.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/ApplicationUser_ListView", SecurityPermissionState.Allow);
        role.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/PermissionPolicyRole_ListView", SecurityPermissionState.Allow);

        // Object permission: write own password fields only
        role.AddMemberPermissionFromLambda<ApplicationUser>(
            SecurityOperations.Write, "StoredPassword",
            cm => cm.ID == (Guid)CurrentUserIdOperator.CurrentUserId(),
            SecurityPermissionState.Allow);
        role.AddMemberPermissionFromLambda<ApplicationUser>(
            SecurityOperations.Write, "ChangePasswordOnFirstLogon",
            cm => cm.ID == (Guid)CurrentUserIdOperator.CurrentUserId(),
            SecurityPermissionState.Allow);

        // ModelDifference access (standard)
        role.AddTypePermissionsRecursively<ModelDifference>(SecurityOperations.ReadWriteAccess, SecurityPermissionState.Allow);
        role.AddTypePermissionsRecursively<ModelDifference>(SecurityOperations.Create, SecurityPermissionState.Allow);
        role.AddTypePermissionsRecursively<ModelDifferenceAspect>(SecurityOperations.ReadWriteAccess, SecurityPermissionState.Allow);
        role.AddTypePermissionsRecursively<ModelDifferenceAspect>(SecurityOperations.Create, SecurityPermissionState.Allow);
    }
    return role;
}

PermissionPolicyRole CreateReadOnlyRole()
{
    PermissionPolicyRole role = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(r => r.Name == "ReadOnly");
    if (role == null)
    {
        role = ObjectSpace.CreateObject<PermissionPolicyRole>();
        role.Name = "ReadOnly";
        role.PermissionPolicy = SecurityPermissionPolicyPermissionPolicy.DenyAllByDefault;

        // Read + Navigate on ApplicationUser
        role.AddTypePermissionsRecursively<ApplicationUser>(SecurityOperations.Read, SecurityPermissionState.Allow);
        role.AddTypePermissionsRecursively<ApplicationUser>(SecurityOperations.Navigate, SecurityPermissionState.Allow);

        // Read on roles
        role.AddTypePermissionsRecursively<PermissionPolicyRole>(SecurityOperations.Read, SecurityPermissionState.Allow);

        // Navigate to MyDetails only
        role.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/MyDetails", SecurityPermissionState.Allow);

        // ModelDifference access (standard)
        role.AddObjectPermission<ModelDifference>(SecurityOperations.ReadWriteAccess, "UserId = ToStr(CurrentUserId())", SecurityPermissionState.Allow);
        role.AddObjectPermission<ModelDifferenceAspect>(SecurityOperations.ReadWriteAccess, "Owner.UserId = ToStr(CurrentUserId())", SecurityPermissionState.Allow);
        role.AddTypePermissionsRecursively<ModelDifference>(SecurityOperations.Create, SecurityPermissionState.Allow);
        role.AddTypePermissionsRecursively<ModelDifferenceAspect>(SecurityOperations.Create, SecurityPermissionState.Allow);
    }
    return role;
}
```

**Step 2: Build**

Run: `dotnet build XafRoles/XafRoles.Module/XafRoles.Module.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add XafRoles/XafRoles.Module/DatabaseUpdate/Updater.cs
git commit -m "feat: add Manager and ReadOnly sample roles for testing"
```

---

### Task 6: Manual integration test — export and re-import

This task is a manual verification step. No code changes.

**Step 1: Run the Blazor app**

Run: `dotnet run --project XafRoles/XafRoles.Blazor.Server/XafRoles.Blazor.Server.csproj`

Log in as Admin (empty password). The database updater will create all 4 roles.

**Step 2: Navigate to Roles ListView**

Verify the "Export Roles" and "Import Roles" buttons appear in the toolbar.

**Step 3: Export**

Click "Export Roles". Note the file path from the success message. Open the JSON file and verify it contains all 4 roles with their permissions.

**Step 4: Round-trip test**

1. Copy the exported JSON to `roles-import.json` in the Blazor app's `bin/Debug/net8.0/` directory
2. Delete the "Manager" role from the UI (to test creation on import)
3. Edit the "ReadOnly" role — change `CanEditModel` to true (to test update on import)
4. Click "Import Roles"
5. Verify: "Manager" is recreated, "ReadOnly" is reverted, "Administrators" and "Default" unchanged
6. Export again and diff the two JSON files — they should be identical (except `exportedAt`)

**Step 5: Commit (if any fixes were needed)**

```bash
git add -A
git commit -m "fix: adjustments from integration testing"
```

---

### Task 7: Fix compilation issues and API mismatches

This is a buffer task. During Tasks 2-4, the exact DevExpress API types may differ from what's documented:

**Known risks to check:**
1. `PermissionPolicyTypePermissionObject.TargetType` — may be `Type` not `string`. If so, export needs `.FullName`, import needs type resolution via `XafTypesInfo`.
2. `SecurityPermissionPolicyPermissionPolicy` enum name — may be shorter. Check actual enum in DevExpress namespace.
3. `PermissionPolicyNavigationPermissionObject` — property may be `ItemPath` or `NavigationItemPath`. Verify.
4. `PermissionPolicyActionPermissionObject` — property may be `ActionId` or `Name`. Verify.
5. `Application.ShowViewStrategy.ShowMessage` — may require different signature in XAF 25.2.

**How to resolve:** Use the DevExpress MCP docs tool to search for the exact property names. Fix compilation errors one by one. Each fix should be committed individually.

---

## Execution Notes

- Tasks 1-4 are the core implementation and should be done sequentially.
- Task 5 (sample roles) can be done in parallel with Tasks 2-4 since it only modifies Updater.cs.
- Task 6 requires a running database — it's a manual smoke test.
- Task 7 is a catch-all for API mismatches discovered during compilation.
- The controller (Task 4) uses a simple file-based approach for import. Platform-specific file dialogs (Blazor download, WinForms SaveFileDialog) are a natural follow-up but out of scope for this plan.
