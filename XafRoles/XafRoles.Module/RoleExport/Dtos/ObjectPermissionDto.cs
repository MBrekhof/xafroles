namespace XafRoles.Module.RoleExport.Dtos;

public class ObjectPermissionDto
{
    public string? Criteria { get; set; }
    public string? ReadState { get; set; }
    public string? WriteState { get; set; }
    public string? DeleteState { get; set; }
    public string? NavigateState { get; set; }
}
