using Cadenza.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cadenza.Package;
using NUnit.Framework;

namespace Cadenza.Tests.Unit.Package;

[TestFixture]
public class PackageManagerTests
{
    private string _tempDir;
    private PackageManager _packageManager;

    [SetUp]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        Directory.SetCurrentDirectory(_tempDir);
        
        // Create a basic cadenzac.json
        var config = new EnhancedFlowcConfig(
            Name: "test-project",
            Version: "1.0.0",
            Description: "Test project for package manager"
        );
        ConfigurationManager.SaveConfigAsync(config).Wait();
        
        _packageManager = new PackageManager();
    }

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Test]
    public async Task AddPackage_Should_AddToConfig()
    {
        // Act
        var result = await _packageManager.AddPackageAsync("TestPackage@1.0.0");
        
        // Assert
        Assert.That(result.Success, Is.True);
        
        var config = await ConfigurationManager.LoadConfigAsync();
        Assert.That(config.Dependencies.ContainsKey("TestPackage"), Is.True);
        Assert.That(config.Dependencies["TestPackage"], Is.EqualTo("1.0.0"));
    }

    [Test]
    public async Task AddPackage_WithDevFlag_Should_AddToDevDependencies()
    {
        // Act
        var result = await _packageManager.AddPackageAsync("TestDevPackage@1.0.0", isDev: true);
        
        // Assert
        Assert.That(result.Success, Is.True);
        
        var config = await ConfigurationManager.LoadConfigAsync();
        Assert.That(config.DevDependencies.ContainsKey("TestDevPackage"), Is.True);
        Assert.That(config.DevDependencies["TestDevPackage"], Is.EqualTo("1.0.0"));
    }

    [Test]
    public async Task RemovePackage_Should_RemoveFromConfig()
    {
        // Arrange
        await _packageManager.AddPackageAsync("TestPackage@1.0.0");
        
        // Act
        var result = await _packageManager.RemovePackageAsync("TestPackage");
        
        // Assert
        Assert.That(result.Success, Is.True);
        
        var config = await ConfigurationManager.LoadConfigAsync();
        Assert.That(config.Dependencies.ContainsKey("TestPackage"), Is.False);
    }

    [Test]
    public async Task SearchPackages_Should_ReturnResults()
    {
        // Act
        var results = await _packageManager.SearchPackagesAsync("json");
        
        // Assert
        Assert.That(results, Is.Not.Null);
        // Note: Actual search would require network access
        // In real tests, we'd mock the registry clients
    }
}

[TestFixture]
public class DependencyResolverTests
{
    private MockPackageRegistry _mockRegistry;
    private MockNuGetClient _mockNuGetClient;
    private DependencyResolver _resolver;

    [SetUp]
    public void Setup()
    {
        _mockRegistry = new MockPackageRegistry();
        _mockNuGetClient = new MockNuGetClient();
        _resolver = new DependencyResolver(_mockRegistry, _mockNuGetClient);
    }

    [Test]
    public async Task ResolveAsync_WithSimpleDependency_Should_Succeed()
    {
        // Arrange
        var config = new EnhancedFlowcConfig(
            Dependencies: new Dictionary<string, string> { { "TestPackage", "1.0.0" } }
        );
        
        _mockRegistry.AddPackage(new PackageMetadata(
            "TestPackage", "1.0.0", "Test package", Dependencies: new()));

        // Act
        var result = await _resolver.ResolveAsync(config);

        // Assert
        Assert.That(result.IsSuccessful, Is.True);
        Assert.That(result.ResolvedPackages.Count, Is.EqualTo(1));
        Assert.That(result.ResolvedPackages.ContainsKey("TestPackage"), Is.True);
    }

    [Test]
    public async Task ResolveAsync_WithVersionConflict_Should_ReportConflict()
    {
        // Arrange
        var config = new EnhancedFlowcConfig(
            Dependencies: new Dictionary<string, string> 
            { 
                { "PackageA", "1.0.0" },
                { "PackageB", "1.0.0" }
            }
        );
        
        _mockRegistry.AddPackage(new PackageMetadata(
            "PackageA", "1.0.0", "Package A", 
            Dependencies: new Dictionary<string, string> { { "SharedDep", "1.0.0" } }));
        _mockRegistry.AddPackage(new PackageMetadata(
            "PackageB", "1.0.0", "Package B",
            Dependencies: new Dictionary<string, string> { { "SharedDep", "2.0.0" } }));
        _mockRegistry.AddPackage(new PackageMetadata(
            "SharedDep", "1.0.0", "Shared dependency"));
        _mockRegistry.AddPackage(new PackageMetadata(
            "SharedDep", "2.0.0", "Shared dependency"));

        // Act & Assert
        Assert.ThrowsAsync<DependencyResolutionException>(
            () => _resolver.ResolveAsync(config));
    }
}

[TestFixture]
public class SemanticVersionTests
{
    [Test]
    public void Parse_ValidVersion_Should_ParseCorrectly()
    {
        // Act
        var version = SemanticVersion.Parse("1.2.3");
        
        // Assert
        Assert.That(version.Major, Is.EqualTo(1));
        Assert.That(version.Minor, Is.EqualTo(2));
        Assert.That(version.Patch, Is.EqualTo(3));
    }

    [Test]
    public void Parse_VersionWithPreRelease_Should_ParseCorrectly()
    {
        // Act
        var version = SemanticVersion.Parse("1.2.3-beta.1");
        
        // Assert
        Assert.That(version.Major, Is.EqualTo(1));
        Assert.That(version.Minor, Is.EqualTo(2));
        Assert.That(version.Patch, Is.EqualTo(3));
        Assert.That(version.PreRelease, Is.EqualTo("beta.1"));
    }

    [Test]
    public void CompareTo_Should_OrderVersionsCorrectly()
    {
        // Arrange
        var v1 = SemanticVersion.Parse("1.0.0");
        var v2 = SemanticVersion.Parse("1.0.1");
        var v3 = SemanticVersion.Parse("1.1.0");
        var v4 = SemanticVersion.Parse("2.0.0");
        
        // Assert
        Assert.That(v1 < v2, Is.True);
        Assert.That(v2 < v3, Is.True);
        Assert.That(v3 < v4, Is.True);
    }

    [Test]
    public void IsVersionCompatible_WithCaretRange_Should_HandleCorrectly()
    {
        // Arrange
        var resolver = new DependencyResolver(null!, null!);
        
        // Assert
        Assert.That(resolver.IsVersionCompatible("1.2.3", "^1.0.0"), Is.True);
        Assert.That(resolver.IsVersionCompatible("1.9.9", "^1.0.0"), Is.True);
        Assert.That(resolver.IsVersionCompatible("2.0.0", "^1.0.0"), Is.False);
        Assert.That(resolver.IsVersionCompatible("0.9.9", "^1.0.0"), Is.False);
    }

    [Test]
    public void IsVersionCompatible_WithTildeRange_Should_HandleCorrectly()
    {
        // Arrange
        var resolver = new DependencyResolver(null!, null!);
        
        // Assert
        Assert.That(resolver.IsVersionCompatible("1.2.3", "~1.2.0"), Is.True);
        Assert.That(resolver.IsVersionCompatible("1.2.9", "~1.2.0"), Is.True);
        Assert.That(resolver.IsVersionCompatible("1.3.0", "~1.2.0"), Is.False);
        Assert.That(resolver.IsVersionCompatible("1.1.9", "~1.2.0"), Is.False);
    }
}

[TestFixture]
public class SecurityScannerTests
{
    private SecurityScanner _scanner;
    private string _tempDir;

    [SetUp]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        Directory.SetCurrentDirectory(_tempDir);
        
        _scanner = new SecurityScanner();
    }

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
        _scanner?.Dispose();
    }

    [Test]
    public async Task AuditAsync_WithNoPackages_Should_ReturnCleanReport()
    {
        // Arrange
        var lockFile = new LockFile();
        await ConfigurationManager.SaveLockFileAsync(lockFile);
        
        // Act
        var report = await _scanner.AuditAsync();
        
        // Assert
        Assert.That(report.HasVulnerabilities, Is.False);
        Assert.That(report.TotalPackagesScanned, Is.EqualTo(0));
    }

    [Test]
    public async Task ScanPackage_Should_InferEffectsCorrectly()
    {
        // Act
        var vulnerabilities = await _scanner.ScanPackageAsync("System.Net.Http", "4.3.0", PackageType.NuGet);
        
        // Assert
        Assert.That(vulnerabilities, Is.Not.Null);
        // Note: Real implementation would check actual vulnerability databases
    }
}

[TestFixture]
public class ConfigurationManagerTests
{
    private string _tempDir;

    [SetUp]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        Directory.SetCurrentDirectory(_tempDir);
    }

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Test]
    public async Task LoadConfigAsync_WithNoFile_Should_ReturnDefault()
    {
        // Act
        var config = await ConfigurationManager.LoadConfigAsync();
        
        // Assert
        Assert.That(config.Name, Is.EqualTo("my-project"));
        Assert.That(config.Version, Is.EqualTo("1.0.0"));
    }

    [Test]
    public async Task SaveAndLoadConfig_Should_Persist()
    {
        // Arrange
        var originalConfig = new EnhancedFlowcConfig(
            Name: "test-project",
            Version: "2.1.0",
            Description: "Test description",
            Dependencies: new Dictionary<string, string> { { "TestPkg", "1.0.0" } }
        );
        
        // Act
        await ConfigurationManager.SaveConfigAsync(originalConfig);
        var loadedConfig = await ConfigurationManager.LoadConfigAsync();
        
        // Assert
        Assert.That(loadedConfig.Name, Is.EqualTo("test-project"));
        Assert.That(loadedConfig.Version, Is.EqualTo("2.1.0"));
        Assert.That(loadedConfig.Description, Is.EqualTo("Test description"));
        Assert.That(loadedConfig.Dependencies.ContainsKey("TestPkg"), Is.True);
        Assert.That(loadedConfig.Dependencies["TestPkg"], Is.EqualTo("1.0.0"));
    }

    [Test]
    public async Task IsWorkspaceRoot_WithWorkspaceConfig_Should_ReturnTrue()
    {
        // Arrange
        var config = new EnhancedFlowcConfig(
            Workspace: new WorkspaceConfig(Projects: new List<string> { "./packages/*" })
        );
        await ConfigurationManager.SaveConfigAsync(config);
        
        // Act
        var isWorkspace = ConfigurationManager.IsWorkspaceRoot();
        
        // Assert
        Assert.That(isWorkspace, Is.True);
    }

    [Test]
    public async Task DiscoverWorkspaceProjects_Should_FindProjects()
    {
        // Arrange
        var config = new EnhancedFlowcConfig(
            Workspace: new WorkspaceConfig(Projects: new List<string> { "./libs/*" })
        );
        await ConfigurationManager.SaveConfigAsync(config);
        
        // Create mock project structure
        var libsDir = Path.Combine(_tempDir, "libs");
        var project1Dir = Path.Combine(libsDir, "project1");
        Directory.CreateDirectory(project1Dir);
        
        var project1Config = new EnhancedFlowcConfig(Name: "project1");
        await ConfigurationManager.SaveConfigAsync(project1Config, Path.Combine(project1Dir, "cadenzac.json"));
        
        // Act
        var projects = await ConfigurationManager.DiscoverWorkspaceProjectsAsync();
        
        // Assert
        Assert.That(projects.Count, Is.EqualTo(1));
        Assert.That(projects.Contains("libs/project1"), Is.True);
    }
}

// Mock implementations for testing
public class MockPackageRegistry : IPackageRegistry
{
    private readonly Dictionary<string, PackageMetadata> _packages = new();

    public void AddPackage(PackageMetadata package)
    {
        _packages[$"{package.Name}@{package.Version}"] = package;
    }

    public Task<PackageMetadata?> GetPackageMetadataAsync(string packageName, string? version = null)
    {
        var key = version != null ? $"{packageName}@{version}" : _packages.Keys.FirstOrDefault(k => k.StartsWith($"{packageName}@"));
        return Task.FromResult(key != null ? _packages[key] : null);
    }

    public Task<List<PackageMetadata>> SearchPackagesAsync(string query, int skip = 0, int take = 20)
    {
        return Task.FromResult(_packages.Values.Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList());
    }

    public Task<Stream> DownloadPackageAsync(string packageName, string version)
    {
        return Task.FromResult<Stream>(new MemoryStream());
    }

    public Task<PublishResult> PublishPackageAsync(string packagePath, PublishOptions options)
    {
        return Task.FromResult(new PublishResult(true, "Mock publish successful"));
    }

    public Task<List<string>> GetVersionsAsync(string packageName)
    {
        var versions = _packages.Keys
            .Where(k => k.StartsWith($"{packageName}@"))
            .Select(k => k.Split('@')[1])
            .ToList();
        return Task.FromResult(versions);
    }

    public Task<bool> PackageExistsAsync(string packageName, string version)
    {
        return Task.FromResult(_packages.ContainsKey($"{packageName}@{version}"));
    }

    public Task<RegistryInfo> GetRegistryInfoAsync()
    {
        return Task.FromResult(new RegistryInfo("Mock Registry", "1.0.0", "Mock registry for testing"));
    }
}

public class MockNuGetClient : INuGetClient
{
    private readonly Dictionary<string, NuGetPackage> _packages = new();

    public void AddPackage(NuGetPackage package)
    {
        _packages[$"{package.Id}@{package.Version}"] = package;
    }

    public Task<NuGetPackage?> GetPackageAsync(string packageId, string versionSpec)
    {
        var package = _packages.Values.FirstOrDefault(p => p.Id == packageId);
        return Task.FromResult(package);
    }

    public Task<List<NuGetPackage>> SearchPackagesAsync(string query, int skip = 0, int take = 20)
    {
        return Task.FromResult(_packages.Values.Where(p => p.Id.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList());
    }

    public Task<List<string>> GetVersionsAsync(string packageId)
    {
        var versions = _packages.Values
            .Where(p => p.Id == packageId)
            .Select(p => p.Version)
            .ToList();
        return Task.FromResult(versions);
    }

    public Task<Stream> DownloadPackageAsync(string packageId, string version)
    {
        return Task.FromResult<Stream>(new MemoryStream());
    }

    public Task<bool> PackageExistsAsync(string packageId, string version)
    {
        return Task.FromResult(_packages.ContainsKey($"{packageId}@{version}"));
    }
}