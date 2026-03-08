# Role Export/Import Design

## Problem

DEV/TEST/PROD deployments need synchronized security roles. Currently roles are only managed through the XAF UI or seed code, with no way to transfer role definitions between environments.

## Solution

JSON-based export/import of XAF security roles, triggered via XAF Controller actions on the Role ListView.

## Decisions

- **Format:** JSON — human-readable, git-diffable
- **Trigger:** XAF Controller actions (Export/Import buttons on Role ListView)
- **Import strategy:** Merge/upsert only — match by role name, update if found, create if new, never delete
- **Scope:** Roles and permissions only — no user-role assignments
- **Location:** All code in XafRoles.Module so it works in both Blazor and WinForms

## JSON Structure

```json
{
  "exportedAt": "2026-03-08T14:30:00Z",
  "roles": [
    {
      "name": "Manager",
      "isAdministrative": false,
      "canEditModel": false,
      "permissionPolicy": "DenyAllByDefault",
      "typePermissions": [
        {
          "targetType": "XafRoles.Module.BusinessObjects.ApplicationUser",
          "readState": "Allow",
          "writeState": "Allow",
          "createState": null,
          "deleteState": "Deny",
          "navigateState": "Allow",
          "objectPermissions": [
            {
              "criteria": "ID = CurrentUserId()",
              "readState": "Allow",
              "writeState": "Allow",
              "deleteState": null,
              "navigateState": null
            }
          ],
          "memberPermissions": [
            {
              "members": "StoredPassword;ChangePasswordOnFirstLogon",
              "criteria": "ID = CurrentUserId()",
              "readState": null,
              "writeState": "Allow"
            }
          ]
        }
      ],
      "navigationPermissions": [
        {
          "itemPath": "Application/NavigationItems/Items/Default/Items/MyDetails",
          "navigateState": "Allow"
        }
      ],
      "actionPermissions": [
        { "actionId": "ExportAction" }
      ]
    }
  ]
}
```

- Roles matched by **name** across environments
- Type permissions use full CLR type name string (as stored by XAF)
- `null` states = not explicitly set (inherits from role's permissionPolicy)
- One file contains all roles

## Architecture

All code in `XafRoles.Module/RoleExport/`:

```
RoleExport/
  Dtos/
    RoleExportDocument.cs      — root DTO: metadata + List<RoleDto>
    RoleDto.cs                 — role props + child permission collections
    TypePermissionDto.cs       — target type + CRUD states + nested object/member DTOs
    ObjectPermissionDto.cs     — criteria + CRUD states
    MemberPermissionDto.cs     — members + criteria + read/write states
    NavigationPermissionDto.cs — item path + state
    ActionPermissionDto.cs     — action id
  RoleExportService.cs         — ObjectSpace → DTOs → JSON
  RoleImportService.cs         — JSON → DTOs → upsert via ObjectSpace
  RoleExportImportController.cs — ViewController on Role ListView with Export/Import actions
```

### Export Flow

Role ListView → "Export Roles" action → RoleExportService reads all roles via ObjectSpace → serializes to JSON → file download (Blazor) / SaveFileDialog (WinForms)

### Import Flow

Role ListView → "Import Roles" action → file picker → RoleImportService deserializes JSON → for each role:
- Find by name → clear all permission collections → recreate from JSON
- Not found → create new role with all permissions
- Roles in DB but not in JSON → left untouched

→ ObjectSpace.CommitChanges() → refresh view → summary popup ("Imported 4 roles: 2 updated, 2 created")

## Sample Roles for Testing

Four roles to exercise all permission types:

**Administrators** (existing): `IsAdministrative = true`

**Default** (existing): kept as-is with object/member/navigation/type permissions

**Manager** (new):
- `PermissionPolicy = DenyAllByDefault`
- Type: Read+Write+Create+Navigate on ApplicationUser, Read on PermissionPolicyRole
- Navigation: Users list, Roles list
- Object: on ApplicationUser, write where ID = CurrentUserId()
- Member: deny write on StoredPassword for other users

**ReadOnly** (new):
- `PermissionPolicy = DenyAllByDefault`
- Type: Read+Navigate on ApplicationUser, Read on PermissionPolicyRole
- Navigation: MyDetails only
- No write/create/delete

Coverage: administrative flag, permission policy enum, type CRUD, object criteria, member criteria, navigation permissions, null/unset states.

## Controller UX

- Activates on PermissionPolicyRole ListView only
- **Export:** SimpleAction, exports all roles, file download
- **Import:** SimpleAction, file upload, merge/upsert, summary popup after completion
- No confirmation needed (merge-only is safe)
