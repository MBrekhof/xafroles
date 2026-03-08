using System.ComponentModel;
using DevExpress.ExpressApp.DC;

namespace XafRoles.Module.RoleExport;

[DomainComponent]
public class RoleExportParameters
{
    [DisplayName("Export File Path")]
    public string FilePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        $"roles-export-{DateTime.Now:yyyy-MM-dd-HHmmss}.json");
}

[DomainComponent]
public class RoleImportParameters
{
    [DisplayName("Import File Path")]
    public string FilePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        "roles-export.json");
}
