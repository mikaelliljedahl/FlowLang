
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cadenza.Package;

/// <summary>
/// Build configuration for Cadenza projects
/// </summary>
public record BuildConfig(
    string Source = "src/",
    string Output = "build/",
    string Target = "csharp"
);

/// <summary>
/// Workspace configuration for multi-project setups
/// </summary>
public record WorkspaceConfig(
    List<string> Projects,
    List<string> Exclude
)
{
    public List<string> Projects { get; init; } = Projects ?? new();
    public List<string> Exclude { get; init; } = Exclude ?? new();
}

/// <summary>
/// Publishing configuration for package publication
/// </summary>
public record PublishConfig(
    string Registry = "https://api.nuget.org/v3/index.json",
    string Access = "public"
);

/// <summary>
/// Enhanced Cadenza project configuration with comprehensive package management support
/// </summary>
public record EnhancedFlowcConfig(
    string Name = "my-project",
    string Version = "1.0.0",
    string Description = "",
    BuildConfig? Build = null,
    Dictionary<string, string>? Dependencies = null,
    Dictionary<string, string>? DevDependencies = null,
    List<string>? NugetSources = null,
    string CadenzaRegistry = "https://api.nuget.org/v3/index.json",
    Dictionary<string, List<string>>? EffectMappings = null,
    WorkspaceConfig? Workspace = null,
    PublishConfig? PublishConfig = null,
    List<string>? Scripts = null
)
{
    public BuildConfig Build { get; init; } = Build ?? new();
    public Dictionary<string, string> Dependencies { get; init; } = Dependencies ?? new();
    public Dictionary<string, string> DevDependencies { get; init; } = DevDependencies ?? new();
    public List<string> NugetSources { get; init; } = NugetSources ?? new() { "https://api.nuget.org/v3/index.json" };
    public Dictionary<string, List<string>> EffectMappings { get; init; } = EffectMappings ?? new();
    public List<string> Scripts { get; init; } = Scripts ?? new();
}


/// <summary>
/// Lock file for reproducible builds
/// </summary>
public record LockFile(
    int LockfileVersion = 2,
    Dictionary<string, ResolvedPackage>? Resolved = null,
    Dictionary<string, string>? Integrity = null,
    string? GeneratedAt = null
)
{
    public Dictionary<string, ResolvedPackage> Resolved { get; init; } = Resolved ?? new();
    public Dictionary<string, string> Integrity { get; init; } = Integrity ?? new();
    public string GeneratedAt { get; init; } = GeneratedAt ?? DateTime.UtcNow.ToString("O");
}

public record ResolvedPackage(
    string Version,
    string Resolved,
    string Integrity,
    Dictionary<string, string>? Dependencies = null,
    List<string>? Effects = null,
    PackageType Type = PackageType.Cadenza
)
{
    public Dictionary<string, string> Dependencies { get; init; } = Dependencies ?? new();
    public List<string> Effects { get; init; } = Effects ?? new();
}

public enum PackageType
{
    Cadenza,
    NuGet,
    Mixed
}

/// <summary>
/// Package metadata for Cadenza registry
/// </summary>
public record PackageMetadata(
    string Name,
    string Version,
    string Description,
    List<string>? Keywords = null,
    string? Homepage = null,
    string? Repository = null,
    string? License = null,
    Author? Author = null,
    List<string>? Effects = null,
    Dictionary<string, string>? Dependencies = null,
    DateTime PublishedAt = default,
    long DownloadCount = 0
)
{
    public List<string> Keywords { get; init; } = Keywords ?? new();
    public List<string> Effects { get; init; } = Effects ?? new();
    public Dictionary<string, string> Dependencies { get; init; } = Dependencies ?? new();
}

public record Author(
    string Name,
    string? Email = null,
    string? Url = null
);

/// <summary>
/// Utility class for configuration management
/// </summary>
public static class ConfigurationManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task<EnhancedFlowcConfig> LoadConfigAsync(string? path = null)
    {
        path ??= "cadenzac.json";
        
        if (!File.Exists(path))
        {
            return new EnhancedFlowcConfig();
        }

        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<EnhancedFlowcConfig>(json, JsonOptions) ?? new EnhancedFlowcConfig();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid cadenzac.json format: {ex.Message}", ex);
        }
    }

    public static async Task SaveConfigAsync(EnhancedFlowcConfig config, string? path = null)
    {
        path ??= "cadenzac.json";
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    public static async Task<LockFile> LoadLockFileAsync(string? path = null)
    {
        path ??= "cadenzac.lock";
        
        if (!File.Exists(path))
        {
            return new LockFile();
        }

        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<LockFile>(json, JsonOptions) ?? new LockFile();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid cadenzac.lock format: {ex.Message}", ex);
        }
    }

    public static async Task SaveLockFileAsync(LockFile lockFile, string? path = null)
    {
        path ??= "cadenzac.lock";
        var json = JsonSerializer.Serialize(lockFile, JsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    public static bool IsWorkspaceRoot(string path = ".")
    {
        var configPath = Path.Combine(path, "cadenzac.json");
        if (!File.Exists(configPath))
            return false;

        try
        {
            var config = LoadConfigAsync(configPath).Result;
            return config.Workspace?.Projects.Any() ?? false;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<List<string>> DiscoverWorkspaceProjectsAsync(string rootPath = ".")
    {
        var config = await LoadConfigAsync(Path.Combine(rootPath, "cadenzac.json"));
        var projects = new List<string>();

        if (config.Workspace == null) return projects;

        foreach (var projectPattern in config.Workspace.Projects)
        {
            var fullPattern = Path.Combine(rootPath, projectPattern);
            var matches = Directory.GetDirectories(Path.GetDirectoryName(fullPattern) ?? ".", 
                                                   Path.GetFileName(fullPattern));
            
            foreach (var match in matches)
            {
                if (File.Exists(Path.Combine(match, "cadenzac.json")) && 
                    !(config.Workspace.Exclude?.Any(exclude => match.Contains(exclude)) ?? false))
                {
                    projects.Add(Path.GetRelativePath(rootPath, match));
                }
            }
        }

        return projects;
    }
}