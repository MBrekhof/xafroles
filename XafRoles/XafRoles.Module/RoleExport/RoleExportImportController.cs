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
