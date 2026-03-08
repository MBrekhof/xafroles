using System.Text.Json;
using System.Text.Json.Serialization;

namespace XafRoles.Module.RoleExport;

internal static class JsonSettings
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
