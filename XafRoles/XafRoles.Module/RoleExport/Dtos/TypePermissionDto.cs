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
