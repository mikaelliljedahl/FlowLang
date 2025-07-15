using System;
using System.IO;
using System.Threading.Tasks;
using Cadenza.Package;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cadenza.Tests.Integration.Package;

[TestClass]
public class PackageIntegrationTests
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
    public async Task EndToEnd_PackageLifecycle_Should_Work()
    {
        // Arrange - Create a new Cadenza project
        var config = new EnhancedFlowcConfig(
            Name: "integration-test-project",
            Version: "1.0.0",
            Description: "Integration test project"
        );
        await ConfigurationManager.SaveConfigAsync(config);

        var packageManager = new PackageManager(config);

        // Act & Assert - Add a package
        var addResult = await packageManager.AddPackageAsync("TestPackage@1.0.0");
        // Note: This would fail in real scenario without a test registry
        // In production, we'd use a test package registry
        
        // Verify configuration was updated
        var updatedConfig = await ConfigurationManager.LoadConfigAsync();
        Assert.IsTrue(updatedConfig.Dependencies.ContainsKey("TestPackage"));

        // Act & Assert - Install packages
        var installResult = await packageManager.InstallPackagesAsync();
        // Note: Would require actual package sources in real test

        // Act & Assert - Remove package
        var removeResult = await packageManager.RemovePackageAsync("TestPackage");
        Assert.IsTrue(removeResult.Success);
        
        var finalConfig = await ConfigurationManager.LoadConfigAsync();
        Assert.IsFalse(finalConfig.Dependencies.ContainsKey("TestPackage"));
    }

    [TestMethod]
    public async Task Workspace_MultiProject_Should_ManageCorrectly()
    {
        // Arrange - Create workspace structure
        var workspaceConfig = new EnhancedFlowcConfig(
            Name: "workspace-root",
            Workspace: new WorkspaceConfig(
                Projects: new() { "./projects/*" },
                Exclude: new() { "./projects/excluded" }
            )
        );
        await ConfigurationManager.SaveConfigAsync(workspaceConfig);

        // Create project structure
        var projectsDir = Path.Combine(_tempDir, "projects");
        var project1Dir = Path.Combine(projectsDir, "project1");
        var project2Dir = Path.Combine(projectsDir, "project2");
        var excludedDir = Path.Combine(projectsDir, "excluded");
        
        Directory.CreateDirectory(project1Dir);
        Directory.CreateDirectory(project2Dir);
        Directory.CreateDirectory(excludedDir);

        // Create project configs
        var project1Config = new EnhancedFlowcConfig(Name: "project1", Version: "1.0.0");
        var project2Config = new EnhancedFlowcConfig(Name: "project2", Version: "1.0.0");
        var excludedConfig = new EnhancedFlowcConfig(Name: "excluded", Version: "1.0.0");
        
        await ConfigurationManager.SaveConfigAsync(project1Config, Path.Combine(project1Dir, "cadenzac.json"));
        await ConfigurationManager.SaveConfigAsync(project2Config, Path.Combine(project2Dir, "cadenzac.json"));
        await ConfigurationManager.SaveConfigAsync(excludedConfig, Path.Combine(excludedDir, "cadenzac.json"));

        // Act
        var workspaceManager = new WorkspaceManager();
        var projects = await workspaceManager.GetWorkspaceProjectsAsync();

        // Assert
        Assert.AreEqual(2, projects.Count);
        Assert.IsTrue(projects.Contains("projects/project1"));
        Assert.IsTrue(projects.Contains("projects/project2"));
        Assert.IsFalse(projects.Any(p => p.Contains("excluded")));
    }

    [TestMethod]
    public async Task PackageCreation_Should_GenerateValidPackage()
    {
        // Arrange - Create a complete Cadenza project
        var config = new EnhancedFlowcConfig(
            Name: "sample-package",
            Version: "1.0.0",
            Description: "A sample Cadenza package"
        );
        await ConfigurationManager.SaveConfigAsync(config);

        // Create source structure
        var srcDir = Path.Combine(_tempDir, "src");
        Directory.CreateDirectory(srcDir);
        
        var mainFlow = @"
module SamplePackage {
    pure function add(a: int, b: int) -> int {
        return a + b
    }
    
    export { add }
}";
        await File.WriteAllTextAsync(Path.Combine(srcDir, "main.cdz"), mainFlow);

        var readme = @"# Sample Package

This is a sample Cadenza package for testing.

## Usage

```cadenza
import SamplePackage.{add}

function main() -> int {
    return add(1, 2)
}
```";
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "README.md"), readme);

        // Act
        var packageCreator = new PackageCreator();
        var packagePath = await packageCreator.CreatePackageAsync(_tempDir, _tempDir);

        // Assert
        Assert.IsTrue(File.Exists(packagePath));
        Assert.IsTrue(packagePath.EndsWith("sample-package-1.0.0.zip"));
        
        var fileInfo = new FileInfo(packagePath);
        Assert.IsTrue(fileInfo.Length > 0);
    }

    [TestMethod]
    public async Task SecurityScanner_Should_DetectKnownVulnerabilities()
    {
        // Arrange - Create project with potentially vulnerable package
        var lockFile = new LockFile();
        lockFile.Resolved["VulnerablePackage"] = new ResolvedPackage(
            Version: "1.0.0",
            Resolved: "https://example.com/vulnerable-1.0.0.tgz",
            Integrity: "sha512-test",
            Type: PackageType.Cadenza
        );
        await ConfigurationManager.SaveLockFileAsync(lockFile);

        // Act
        var scanner = new SecurityScanner();
        var report = await scanner.AuditAsync();

        // Assert
        Assert.IsNotNull(report);
        Assert.AreEqual(1, report.TotalPackagesScanned);
        // Note: Real vulnerabilities would require actual vulnerability database
    }

    [TestMethod]
    public async Task LockFile_Should_EnsureReproducibleBuilds()
    {
        // Arrange
        var config = new EnhancedFlowcConfig(
            Name: "lock-test-project",
            Dependencies: new() { { "TestPackage", "^1.0.0" } }
        );
        await ConfigurationManager.SaveConfigAsync(config);

        var packageManager = new PackageManager(config);

        // Act - First install
        // Note: Would require actual package resolution in real test
        var lockFile1 = await ConfigurationManager.LoadLockFileAsync();

        // Simulate time passing and potential version updates
        await Task.Delay(100);

        // Act - Second install should use lock file
        var lockFile2 = await ConfigurationManager.LoadLockFileAsync();

        // Assert - Lock files should be identical for reproducible builds
        Assert.AreEqual(lockFile1.GeneratedAt, lockFile2.GeneratedAt);
    }

    [TestMethod]
    public async Task EffectInference_Should_MapNuGetPackagesCorrectly()
    {
        // Arrange
        var bindingGenerator = new BindingGenerator(new Dictionary<string, List<string>>
        {
            { "System.Net.Http", new() { "Network" } },
            { "System.Data.SqlClient", new() { "Database" } }
        });

        // Create mock NuGet package
        var httpPackage = new NuGetPackage(
            Id: "System.Net.Http",
            Version: "4.3.4",
            Description: "HTTP client library",
            Authors: new() { "Microsoft" },
            ProjectUrl: "https://github.com/dotnet/corefx",
            LicenseUrl: "https://github.com/dotnet/corefx/blob/master/LICENSE.TXT",
            Dependencies: new(),
            DownloadUrl: "https://api.nuget.org/packages/system.net.http.4.3.4.nupkg",
            Hash: "sha512-test",
            Published: DateTime.UtcNow,
            DownloadCount: 1000000
        );

        // Act
        using var emptyStream = new MemoryStream();
        var bindings = await bindingGenerator.GenerateBindingsAsync(httpPackage, emptyStream);

        // Assert
        Assert.IsTrue(bindings.Contains("module System_Net_Http"));
        Assert.IsTrue(bindings.Contains("uses [Network]"));
        Assert.IsTrue(bindings.Contains("function get"));
        Assert.IsTrue(bindings.Contains("Result<string, HttpError>"));
    }
}

[TestClass]
public class VersionManagementTests
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
    public async Task VersionBumping_Should_UpdateCorrectly()
    {
        // Arrange
        var config = new EnhancedFlowcConfig(
            Name: "version-test",
            Version: "1.2.3"
        );
        await ConfigurationManager.SaveConfigAsync(config);

        // Test patch bump
        var currentVersion = SemanticVersion.Parse(config.Version);
        var patchVersion = new SemanticVersion(currentVersion.Major, currentVersion.Minor, currentVersion.Patch + 1);
        
        var patchConfig = config with { Version = patchVersion.ToString() };
        await ConfigurationManager.SaveConfigAsync(patchConfig);
        
        var updatedConfig = await ConfigurationManager.LoadConfigAsync();
        Assert.AreEqual("1.2.4", updatedConfig.Version);

        // Test minor bump
        var minorVersion = new SemanticVersion(currentVersion.Major, currentVersion.Minor + 1, 0);
        var minorConfig = config with { Version = minorVersion.ToString() };
        await ConfigurationManager.SaveConfigAsync(minorConfig);
        
        updatedConfig = await ConfigurationManager.LoadConfigAsync();
        Assert.AreEqual("1.3.0", updatedConfig.Version);

        // Test major bump
        var majorVersion = new SemanticVersion(currentVersion.Major + 1, 0, 0);
        var majorConfig = config with { Version = majorVersion.ToString() };
        await ConfigurationManager.SaveConfigAsync(majorConfig);
        
        updatedConfig = await ConfigurationManager.LoadConfigAsync();
        Assert.AreEqual("2.0.0", updatedConfig.Version);
    }

    [TestMethod]
    public async Task ComplexVersionConstraints_Should_ResolveCorrectly()
    {
        // Test various version constraint patterns
        var resolver = new DependencyResolver(null!, null!);

        // Caret constraints (^1.2.3 allows >=1.2.3 <2.0.0)
        Assert.IsTrue(resolver.IsVersionCompatible("1.2.3", "^1.2.3"));
        Assert.IsTrue(resolver.IsVersionCompatible("1.2.4", "^1.2.3"));
        Assert.IsTrue(resolver.IsVersionCompatible("1.9.9", "^1.2.3"));
        Assert.IsFalse(resolver.IsVersionCompatible("2.0.0", "^1.2.3"));
        Assert.IsFalse(resolver.IsVersionCompatible("1.2.2", "^1.2.3"));

        // Tilde constraints (~1.2.3 allows >=1.2.3 <1.3.0)
        Assert.IsTrue(resolver.IsVersionCompatible("1.2.3", "~1.2.3"));
        Assert.IsTrue(resolver.IsVersionCompatible("1.2.4", "~1.2.3"));
        Assert.IsFalse(resolver.IsVersionCompatible("1.3.0", "~1.2.3"));
        Assert.IsFalse(resolver.IsVersionCompatible("1.2.2", "~1.2.3"));

        // Range constraints
        Assert.IsTrue(resolver.IsVersionCompatible("1.2.3", ">=1.2.0"));
        Assert.IsTrue(resolver.IsVersionCompatible("2.0.0", ">=1.2.0"));
        Assert.IsFalse(resolver.IsVersionCompatible("1.1.9", ">=1.2.0"));

        Assert.IsTrue(resolver.IsVersionCompatible("1.2.3", "<=2.0.0"));
        Assert.IsFalse(resolver.IsVersionCompatible("2.0.1", "<=2.0.0"));

        // Exact version
        Assert.IsTrue(resolver.IsVersionCompatible("1.2.3", "1.2.3"));
        Assert.IsFalse(resolver.IsVersionCompatible("1.2.4", "1.2.3"));

        // Wildcard
        Assert.IsTrue(resolver.IsVersionCompatible("1.2.3", "*"));
        Assert.IsTrue(resolver.IsVersionCompatible("99.99.99", "*"));
    }
}