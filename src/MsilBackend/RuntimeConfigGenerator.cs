using System.Text.Json;
using System.Text.Json.Serialization;

namespace MsilBackend;

public static class RuntimeConfigGenerator
{
    private const string AppRuntimeName = "Microsoft.NETCore.App";

    public static void SaveRuntimeConfig(string outputPath)
    {
        Version netVersion = Environment.Version;
        RuntimeConfig config = new($"net{netVersion.Major}.0", $"{netVersion.Major}.0.0");
        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        string json = JsonSerializer.Serialize(config, options);
        File.WriteAllText(outputPath, json);
    }

    public sealed class FrameworkInfo(string version)
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = AppRuntimeName;

        [JsonPropertyName("version")]
        public string Version { get; init; } = version;
    }

    public sealed class RuntimeOptions(string targetFramework, string version)
    {
        [JsonPropertyName("tfm")]
        public string Tfm { get; init; } = targetFramework;

        [JsonPropertyName("framework")]
        public FrameworkInfo Framework { get; init; } = new(version);
    }

    public sealed class RuntimeConfig(string targetFramework, string version)
    {
        [JsonPropertyName("runtimeOptions")]
        public RuntimeOptions RuntimeOptions { get; init; } = new(targetFramework, version);
    }
}
