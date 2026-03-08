using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;

namespace XafRoles.Module.RoleExport;

public class RoleExportImportController : ViewController<ListView>
{
    private readonly PopupWindowShowAction _exportAction;
    private readonly PopupWindowShowAction _importAction;

    public RoleExportImportController()
    {
        TargetObjectType = typeof(PermissionPolicyRole);

        _exportAction = new PopupWindowShowAction(this, "ExportRoles", "Edit")
        {
            Caption = "Export Roles",
            ImageName = "Action_Export",
            SelectionDependencyType = SelectionDependencyType.Independent
        };
        _exportAction.CustomizePopupWindowParams += ExportAction_CustomizePopup;
        _exportAction.Execute += ExportAction_Execute;

        _importAction = new PopupWindowShowAction(this, "ImportRoles", "Edit")
        {
            Caption = "Import Roles",
            ImageName = "Action_Import",
            SelectionDependencyType = SelectionDependencyType.Independent
        };
        _importAction.CustomizePopupWindowParams += ImportAction_CustomizePopup;
        _importAction.Execute += ImportAction_Execute;
    }

    private void ExportAction_CustomizePopup(object sender, CustomizePopupWindowParamsEventArgs e)
    {
        var os = Application.CreateObjectSpace(typeof(RoleExportParameters));
        var parameters = os.CreateObject<RoleExportParameters>();
        e.View = Application.CreateDetailView(os, parameters);
        e.DialogController.SaveOnAccept = false;
    }

    private void ExportAction_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
    {
        var parameters = (RoleExportParameters)e.PopupWindowViewCurrentObject;
        var filePath = parameters.FilePath;

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var json = RoleExportService.Export(ObjectSpace);
        File.WriteAllText(filePath, json);

        Application.ShowViewStrategy.ShowMessage(
            $"Roles exported to: {filePath}",
            InformationType.Success);
    }

    private void ImportAction_CustomizePopup(object sender, CustomizePopupWindowParamsEventArgs e)
    {
        var os = Application.CreateObjectSpace(typeof(RoleImportParameters));
        var parameters = os.CreateObject<RoleImportParameters>();
        e.View = Application.CreateDetailView(os, parameters);
        e.DialogController.SaveOnAccept = false;
    }

    private void ImportAction_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
    {
        var parameters = (RoleImportParameters)e.PopupWindowViewCurrentObject;
        var filePath = parameters.FilePath;

        if (!File.Exists(filePath))
        {
            Application.ShowViewStrategy.ShowMessage(
                $"File not found: {filePath}",
                InformationType.Error);
            return;
        }

        var json = File.ReadAllText(filePath);
        var result = RoleImportService.Import(ObjectSpace, json);
        ObjectSpace.Refresh();
        View.Refresh();

        Application.ShowViewStrategy.ShowMessage(
            $"Import complete: {result.Created} created, {result.Updated} updated.",
            InformationType.Success);
    }
}
