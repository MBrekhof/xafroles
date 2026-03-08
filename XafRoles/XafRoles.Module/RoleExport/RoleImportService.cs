using System.Text.Json;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using XafRoles.Module.RoleExport.Dtos;

namespace XafRoles.Module.RoleExport;

public record ImportResult(int Updated, int Created);

public static class RoleImportService
{
    public static ImportResult Import(IObjectSpace objectSpace, string json)
    {
        var document = JsonSerializer.Deserialize<RoleExportDocument>(json, JsonSettings.Options)
            ?? throw new InvalidOperationException("Invalid role export file.");

        int updated = 0, created = 0;

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
        // Delete nested permissions first, then the parent type permission
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
            tp.Role = role;
            tp.TargetType = ResolveType(tpDto.TargetType);
            tp.ReadState = ParseState(tpDto.ReadState);
            tp.WriteState = ParseState(tpDto.WriteState);
            tp.CreateState = ParseState(tpDto.CreateState);
            tp.DeleteState = ParseState(tpDto.DeleteState);
            tp.NavigateState = ParseState(tpDto.NavigateState);

            foreach (var opDto in tpDto.ObjectPermissions)
            {
                var op = objectSpace.CreateObject<PermissionPolicyObjectPermissionsObject>();
                op.TypePermissionObject = tp;
                op.Criteria = opDto.Criteria;
                op.ReadState = ParseState(opDto.ReadState);
                op.WriteState = ParseState(opDto.WriteState);
                op.DeleteState = ParseState(opDto.DeleteState);
                op.NavigateState = ParseState(opDto.NavigateState);
            }

            foreach (var mpDto in tpDto.MemberPermissions)
            {
                var mp = objectSpace.CreateObject<PermissionPolicyMemberPermissionsObject>();
                mp.TypePermissionObject = tp;
                mp.Members = mpDto.Members;
                mp.Criteria = mpDto.Criteria;
                mp.ReadState = ParseState(mpDto.ReadState);
                mp.WriteState = ParseState(mpDto.WriteState);
            }
        }

        foreach (var npDto in dto.NavigationPermissions)
        {
            var np = objectSpace.CreateObject<PermissionPolicyNavigationPermissionObject>();
            np.Role = role;
            np.ItemPath = npDto.ItemPath;
            np.NavigateState = ParseState(npDto.NavigateState);
        }

        foreach (var apDto in dto.ActionPermissions)
        {
            var ap = objectSpace.CreateObject<PermissionPolicyActionPermissionObject>();
            ap.Role = role;
            ap.ActionId = apDto.ActionId;
        }
    }

    private static Type? ResolveType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        // Try XAF's type info system first (most reliable for XAF domain types)
        var type = XafTypesInfo.Instance.FindTypeInfo(typeName)?.Type;
        if (type != null)
            return type;

        // Fallback to standard .NET type resolution
        type = Type.GetType(typeName);
        if (type != null)
            return type;

        // Last resort: scan loaded assemblies
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return []; }
            })
            .FirstOrDefault(t => t.FullName == typeName);
    }

    private static SecurityPermissionState? ParseState(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        if (!Enum.TryParse<SecurityPermissionState>(value, out var state))
            throw new InvalidOperationException($"Invalid permission state: '{value}'. Expected: Allow, Deny.");
        return state;
    }

    private static SecurityPermissionPolicy ParsePermissionPolicy(string value)
    {
        if (!Enum.TryParse<SecurityPermissionPolicy>(value, out var policy))
            throw new InvalidOperationException($"Invalid permission policy: '{value}'. Expected: DenyAllByDefault, ReadAllByDefault, AllowAllByDefault.");
        return policy;
    }
}
