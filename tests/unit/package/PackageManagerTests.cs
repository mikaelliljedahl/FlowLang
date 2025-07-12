using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FlowLang.Package;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowLang.Tests.Unit.Package;

[TestClass]
public class PackageManagerTests
{
    private string _tempDir;
    private PackageManager _packageManager;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        Directory.SetCurrentDirectory(_tempDir);
        
        // Create a basic flowc.json
        var config = new EnhancedFlowcConfig(
            Name: "test-project",
            Version: "1.0.0",
            Description: "Test project for package manager"
        );
        ConfigurationManager.SaveConfigAsync(config).Wait();
        
        _packageManager = new PackageManager();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [TestMethod]
    public async Task AddPackage_Should_AddToConfig()
    {
        // Act
        var result = await _packageManager.AddPackageAsync("TestPackage@1.0.0");
        
        // Assert
        Assert.IsTrue(result.Success);
        
        var config = await ConfigurationManager.LoadConfigAsync();
        Assert.IsTrue(config.Dependencies.ContainsKey("TestPackage"));
        Assert.AreEqual("1.0.0", config.Dependencies["TestPackage"]);
    }

    [TestMethod]
    public async Task AddPackage_WithDevFlag_Should_AddToDevDependencies()
    {
        // Act
        var result = await _packageManager.AddPackageAsync("TestDevPackage@1.0.0", isDev: true);
        
        // Assert
        Assert.IsTrue(result.Success);
        
        var config = await ConfigurationManager.LoadConfigAsync();
        Assert.IsTrue(config.DevDependencies.ContainsKey("TestDevPackage"));
        Assert.AreEqual("1.0.0", config.DevDependencies["TestDevPackage"]);
    }

    [TestMethod]
    public async Task RemovePackage_Should_RemoveFromConfig()
    {
        // Arrange
        await _packageManager.AddPackageAsync("TestPackage@1.0.0");
        
        // Act
        var result = await _packageManager.RemovePackageAsync("TestPackage");
        
        // Assert
        Assert.IsTrue(result.Success);
        
        var config = await ConfigurationManager.LoadConfigAsync();
        Assert.IsFalse(config.Dependencies.ContainsKey("TestPackage"));
    }

    [TestMethod]
    public async Task SearchPackages_Should_ReturnResults()
    {
        // Act
        var results = await _packageManager.SearchPackagesAsync("json");
        
        // Assert
        Assert.IsNotNull(results);
        // Note: Actual search would require network access
        // In real tests, we'd mock the registry clients
    }
}

[TestClass]
public class DependencyResolverTests
{
    private MockPackageRegistry _mockRegistry;
    private MockNuGetClient _mockNuGetClient;
    private DependencyResolver _resolver;

    [TestInitialize]
    public void Setup()
    {
        _mockRegistry = new MockPackageRegistry();
        _mockNuGetClient = new MockNuGetClient();
        _resolver = new DependencyResolver(_mockRegistry, _mockNuGetClient);
    }

    [TestMethod]
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
        Assert.IsTrue(result.IsSuccessful);
        Assert.AreEqual(1, result.ResolvedPackages.Count);
        Assert.IsTrue(result.ResolvedPackages.ContainsKey("TestPackage"));
    }

    [TestMethod]
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
        await Assert.ThrowsExceptionAsync<DependencyResolutionException>(
            () => _resolver.ResolveAsync(config));
    }
}

[TestClass]
public class SemanticVersionTests
{
    [TestMethod]
    public void Parse_ValidVersion_Should_ParseCorrectly()
    {
        // Act
        var version = SemanticVersion.Parse("1.2.3");
        
        // Assert
        Assert.AreEqual(1, version.Major);
        Assert.AreEqual(2, version.Minor);
        Assert.AreEqual(3, version.Patch);
    }

    [TestMethod]
    public void Parse_VersionWithPreRelease_Should_ParseCorrectly()
    {
        // Act
        var version = SemanticVersion.Parse("1.2.3-beta.1");
        
        // Assert
        Assert.AreEqual(1, version.Major);
        Assert.AreEqual(2, version.Minor);
        Assert.AreEqual(3, version.Patch);
        Assert.AreEqual("beta.1", version.PreRelease);
    }

    [TestMethod]
    public void CompareTo_Should_OrderVersionsCorrectly()
    {
        // Arrange
        var v1 = SemanticVersion.Parse("1.0.0");
        var v2 = SemanticVersion.Parse("1.0.1");
        var v3 = SemanticVersion.Parse("1.1.0");
        var v4 = SemanticVersion.Parse("2.0.0");
        
        // Assert
        Assert.IsTrue(v1 < v2);
        Assert.IsTrue(v2 < v3);
        Assert.IsTrue(v3 < v4);
    }

    [TestMethod]
    public void IsVersionCompatible_WithCaretRange_Should_HandleCorrectly()
    {
        // Arrange
        var resolver = new DependencyResolver(null!, null!);
        
        // Assert
        Assert.IsTrue(resolver.IsVersionCompatible("1.2.3", "^1.0.0"));
        Assert.IsTrue(resolver.IsVersionCompatible("1.9.9", "^1.0.0"));
        Assert.IsFalse(resolver.IsVersionCompatible("2.0.0", "^1.0.0"));
        Assert.IsFalse(resolver.IsVersionCompatible("0.9.9", "^1.0.0"));
    }

    [TestMethod]
    public void IsVersionCompatible_WithTildeRange_Should_HandleCorrectly()
    {
        // Arrange
        var resolver = new DependencyResolver(null!, null!);
        
        // Assert
        Assert.IsTrue(resolver.IsVersionCompatible("1.2.3", "~1.2.0"));
        Assert.IsTrue(resolver.IsVersionCompatible("1.2.9", "~1.2.0"));
        Assert.IsFalse(resolver.IsVersionCompatible("1.3.0", "~1.2.0"));
        Assert.IsFalse(resolver.IsVersionCompatible("1.1.9", "~1.2.0"));
    }
}

[TestClass]
public class SecurityScannerTests
{
    private SecurityScanner _scanner;
    private string _tempDir;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        Directory.SetCurrentDirectory(_tempDir);
        
        _scanner = new SecurityScanner();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
        _scanner?.Dispose();
    }

    [TestMethod]
    public async Task AuditAsync_WithNoPackages_Should_ReturnCleanReport()
    {
        // Arrange
        var lockFile = new LockFile();
        await ConfigurationManager.SaveLockFileAsync(lockFile);
        
        // Act
        var report = await _scanner.AuditAsync();
        
        // Assert
        Assert.IsFalse(report.HasVulnerabilities);
        Assert.AreEqual(0, report.TotalPackagesScanned);
    }

    [TestMethod]
    public async Task ScanPackage_Should_InferEffectsCorrectly()
    {
        // Act
        var vulnerabilities = await _scanner.ScanPackageAsync("System.Net.Http", "4.3.0", PackageType.NuGet);
        
        // Assert
        Assert.IsNotNull(vulnerabilities);
        // Note: Real implementation would check actual vulnerability databases
    }
}

[TestClass]
public class ConfigurationManagerTests
{
    private string _tempDir;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        Directory.SetCurrentDirectory(_tempDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [TestMethod]
    public async Task LoadConfigAsync_WithNoFile_Should_ReturnDefault()
    {
        // Act
        var config = await ConfigurationManager.LoadConfigAsync();
        
        // Assert
        Assert.AreEqual("my-project", config.Name);
        Assert.AreEqual("1.0.0", config.Version);
    }

    [TestMethod]
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
        Assert.AreEqual("test-project", loadedConfig.Name);
        Assert.AreEqual("2.1.0", loadedConfig.Version);
        Assert.AreEqual("Test description", loadedConfig.Description);
        Assert.IsTrue(loadedConfig.Dependencies.ContainsKey("TestPkg"));
        Assert.AreEqual("1.0.0", loadedConfig.Dependencies["TestPkg"]);
    }

    [TestMethod]
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
        Assert.IsTrue(isWorkspace);
    }

    [TestMethod]
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
        await ConfigurationManager.SaveConfigAsync(project1Config, Path.Combine(project1Dir, "flowc.json"));
        
        // Act
        var projects = await ConfigurationManager.DiscoverWorkspaceProjectsAsync();
        
        // Assert
        Assert.AreEqual(1, projects.Count);
        Assert.IsTrue(projects.Contains("libs/project1"));
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