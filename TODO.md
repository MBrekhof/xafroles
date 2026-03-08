# TODO

## Completed

- [x] Design role export/import feature (see `docs/plans/2026-03-08-role-export-import-design.md`)
- [x] Create DTO classes for JSON serialization
- [x] Create RoleExportService (ObjectSpace → JSON)
- [x] Create RoleImportService (JSON → upsert via ObjectSpace)
- [x] Create RoleExportImportController (XAF actions on Role ListView)
- [x] Add Manager and ReadOnly sample roles to Updater.cs
- [x] Fix review issues (shared JSON options, descriptive parse errors, null TargetType handling)

## Pending

- [ ] Manual integration test: run app, export roles, delete a role, re-import, verify round-trip
- [ ] Blazor file download for export (currently writes to server temp path)
- [ ] WinForms SaveFileDialog/OpenFileDialog for export/import
- [ ] Consider adding `exportedAt` timestamp to import summary message
