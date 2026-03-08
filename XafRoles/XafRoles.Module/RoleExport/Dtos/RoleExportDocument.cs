namespace XafRoles.Module.RoleExport.Dtos;

public class RoleExportDocument
{
    public DateTime ExportedAt { get; set; }
    public List<RoleDto> Roles { get; set; } = [];
}
