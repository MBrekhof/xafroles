using System.Text.Json;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using XafRoles.Module.RoleExport.Dtos;

namespace XafRoles.Module.RoleExport;

public static class RoleExportService
{
    public static string Export(IObjectSpace objectSpace)
    {
        var roles = objectSpace.GetObjectsQuery<PermissionPolicyRole>(true).ToList();
        var document = new RoleExportDocument
        {
            ExportedAt = DateTime.UtcNow,
            Roles = roles.Select(MapRole).ToList()
        };
        return JsonSerializer.Serialize(document, JsonSettings.Options);
    }

    private static RoleDto MapRole(PermissionPolicyRole role)
    {
        return new RoleDto
        {
            Name = role.Name,
            IsAdministrative = role.IsAdministrative,
            CanEditModel = role.CanEditModel,
            PermissionPolicy = role.PermissionPolicy.ToString(),
            TypePermissions = role.TypePermissions
                .Where(tp => tp.TargetType != null)
                .Select(MapTypePermission).ToList(),
            NavigationPermissions = role.NavigationPermissions.Select(MapNavigationPermission).ToList(),
            ActionPermissions = role.ActionPermissions.Select(MapActionPermission).ToList()
        };
    }

    private static TypePermissionDto MapTypePermission(PermissionPolicyTypePermissionObject tp)
    {
        return new TypePermissionDto
        {
            TargetType = tp.TargetType?.FullName ?? "",
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
            ItemPath = np.ItemPath ?? "",
            NavigateState = np.NavigateState?.ToString()
        };
    }

    private static ActionPermissionDto MapActionPermission(PermissionPolicyActionPermissionObject ap)
    {
        return new ActionPermissionDto
        {
            ActionId = ap.ActionId ?? ""
        };
    }
}
