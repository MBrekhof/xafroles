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
