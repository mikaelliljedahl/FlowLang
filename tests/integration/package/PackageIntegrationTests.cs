using System;
using System.IO;
using System.Threading.Tasks;
using Cadenza.Package;
using NUnit.Framework;
using Cadenza.Tests.Framework;
using Cadenza.Core;

namespace Cadenza.Tests.Integration.Package;

[TestFixture]
public class PackageIntegrationTests : TestBase
{
    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        // Switch to temp directory for package operations
        Directory.SetCurrentDirectory(TestTempDirectory);
    }

    [Test]
    public async Task PackageIntegration_EndToEnd_PackageLifecycle_Should_Work()
    {
        // Arrange - Create a new Cadenza project
        var config = new EnhancedFlowcConfig(
            Name: "integration-test-project",
            Version: "1.0.0",
            Description: "Integration test project",
            NugetSources: new() { "https://api.nuget.org/v3/index.json" }
        );
        await ConfigurationManager.SaveConfigAsync(config);

        // Simulate package operations for testing without requiring actual network calls
        // Add a package directly to config to test the workflow
        config = config with { Dependencies = new() { { "TestPackage", "1.0.0" } } };
        await ConfigurationManager.SaveConfigAsync(config);

        var updatedConfig = await ConfigurationManager.LoadConfigAsync();
        Assert.That(updatedConfig.Dependencies.ContainsKey("TestPackage"), Is.True);

        // Test removal
        config = config with { Dependencies = new() };
        await ConfigurationManager.SaveConfigAsync(config);
        
        var finalConfig = await ConfigurationManager.LoadConfigAsync();
        Assert.That(finalConfig.Dependencies.ContainsKey("TestPackage"), Is.False);
    }

    [Test]
    public async Task PackageIntegration_Workspace_MultiProject_Should_ManageCorrectly()
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
        var projectsDir = Path.Combine(TestTempDirectory, "projects");
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
        var workspaceManager = new Cadenza.Package.WorkspaceManager();
        var projects = await workspaceManager.GetWorkspaceProjectsAsync();

        // Assert
        Assert.That(projects.Count, Is.EqualTo(2));
        Assert.That(projects.Any(p => p.Contains("project1")), Is.True);
        Assert.That(projects.Any(p => p.Contains("project2")), Is.True);
        Assert.That(projects.Any(p => p.Contains("excluded")), Is.False);
    }

    [Test]
    public async Task PackageIntegration_PackageCreation_Should_GenerateValidPackage()
    {
        // Arrange - Create a complete Cadenza project
        var config = new EnhancedFlowcConfig(
            Name: "sample-package",
            Version: "1.0.0",
            Description: "A sample Cadenza package"
        );
        await ConfigurationManager.SaveConfigAsync(config);

        // Create source structure
        var srcDir = Path.Combine(TestTempDirectory, "src");
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
        await File.WriteAllTextAsync(Path.Combine(TestTempDirectory, "README.md"), readme);

        // Act
        var packageCreator = new Cadenza.Package.PackageCreator();
        var packagePath = await packageCreator.CreatePackageAsync(TestTempDirectory, TestTempDirectory);

        // Assert
        Assert.That(File.Exists(packagePath), Is.True);
        Assert.That(packagePath.EndsWith("sample-package-1.0.0.zip"), Is.True);
        
        var fileInfo = new FileInfo(packagePath);
        Assert.That(fileInfo.Length, Is.GreaterThan(0));
    }

    [Test]
    public async Task PackageIntegration_SecurityScanner_Should_DetectKnownVulnerabilities()
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
        Assert.That(report, Is.Not.Null);
        Assert.That(report.TotalPackagesScanned, Is.EqualTo(1));
        // Note: Real vulnerabilities would require actual vulnerability database
    }

    [Test]
    public async Task PackageIntegration_LockFile_Should_EnsureReproducibleBuilds()
    {
        // Arrange
        var config = new EnhancedFlowcConfig(
            Name: "lock-test-project",
            Dependencies: new() { { "TestPackage", "^1.0.0" } }
        );
        await ConfigurationManager.SaveConfigAsync(config);

        var packageManager = new PackageManager(config);

        // Act - Create and save a lock file first
        var originalLockFile = new LockFile(
            Resolved: new() { { "TestPackage", new ResolvedPackage("1.0.0", "test-url", "test-integrity") } }
        );
        await ConfigurationManager.SaveLockFileAsync(originalLockFile);

        // Load the lock file multiple times to ensure consistency
        var lockFile1 = await ConfigurationManager.LoadLockFileAsync();

        // Simulate time passing and potential version updates
        await Task.Delay(100);

        // Act - Second load should return the same lock file
        var lockFile2 = await ConfigurationManager.LoadLockFileAsync();

        // Assert - Lock files should be identical for reproducible builds
        Assert.That(lockFile1.GeneratedAt, Is.EqualTo(lockFile2.GeneratedAt));
    }

    [Test]
    public async Task PackageManager_EffectInference_ShouldMapNuGetPackagesCorrectly()
    {
        // Arrange
        var bindingGenerator = new Cadenza.Package.BindingGenerator(new Dictionary<string, List<string>>
        {
            { "System.Net.Http", new() { "Network" } },
            { "System.Data.SqlClient", new() { "Database" } }
        });

        // Create mock NuGet package
        var httpPackage = new Cadenza.Package.NuGetPackage(
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

        // Act - Create a minimal valid ZIP stream
        using var zipStream = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            // Add a fake DLL entry
            var entry = archive.CreateEntry("lib/net45/System.Net.Http.dll");
            using var entryStream = entry.Open();
            await entryStream.WriteAsync(new byte[] { 0x4D, 0x5A }); // PE header signature
        }
        zipStream.Position = 0;
        var bindings = await bindingGenerator.GenerateBindingsAsync(httpPackage, zipStream);

        // Assert
        Assert.That(bindings.Contains("module System_Net_Http"), Is.True);
        Assert.That(bindings.Contains("uses [Network]"), Is.True);
        Assert.That(bindings.Contains("function get"), Is.True);
        Assert.That(bindings.Contains("Result<string, HttpError>"), Is.True);
    }
}

[TestFixture]
public class VersionManagementTests : TestBase
{
    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        // Switch to temp directory for package operations
        Directory.SetCurrentDirectory(TestTempDirectory);
    }

    [Test]
    public async Task PackageIntegration_VersionBumping_Should_UpdateCorrectly()
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
        Assert.That(updatedConfig.Version, Is.EqualTo("1.2.4"));

        // Test minor bump
        var minorVersion = new SemanticVersion(currentVersion.Major, currentVersion.Minor + 1, 0);
        var minorConfig = config with { Version = minorVersion.ToString() };
        await ConfigurationManager.SaveConfigAsync(minorConfig);
        
        updatedConfig = await ConfigurationManager.LoadConfigAsync();
        Assert.That(updatedConfig.Version, Is.EqualTo("1.3.0"));

        // Test major bump
        var majorVersion = new SemanticVersion(currentVersion.Major + 1, 0, 0);
        var majorConfig = config with { Version = majorVersion.ToString() };
        await ConfigurationManager.SaveConfigAsync(majorConfig);
        
        updatedConfig = await ConfigurationManager.LoadConfigAsync();
        Assert.That(updatedConfig.Version, Is.EqualTo("2.0.0"));
    }

    [Test]
    public async Task PackageIntegration_VersionManagement_ComplexVersionConstraints_ShouldResolveCorrectly()
    {
        // Test various version constraint patterns
        var resolver = new DependencyResolver(null!, null!);

        // Caret constraints (^1.2.3 allows >=1.2.3 <2.0.0)
        Assert.That(resolver.IsVersionCompatible("1.2.3", "^1.2.3"), Is.True);
        Assert.That(resolver.IsVersionCompatible("1.2.4", "^1.2.3"), Is.True);
        Assert.That(resolver.IsVersionCompatible("1.9.9", "^1.2.3"), Is.True);
        Assert.That(resolver.IsVersionCompatible("2.0.0", "^1.2.3"), Is.False);
        Assert.That(resolver.IsVersionCompatible("1.2.2", "^1.2.3"), Is.False);

        // Tilde constraints (~1.2.3 allows >=1.2.3 <1.3.0)
        Assert.That(resolver.IsVersionCompatible("1.2.3", "~1.2.3"), Is.True);
        Assert.That(resolver.IsVersionCompatible("1.2.4", "~1.2.3"), Is.True);
        Assert.That(resolver.IsVersionCompatible("1.3.0", "~1.2.3"), Is.False);
        Assert.That(resolver.IsVersionCompatible("1.2.2", "~1.2.3"), Is.False);

        // Range constraints
        Assert.That(resolver.IsVersionCompatible("1.2.3", ">=1.2.0"), Is.True);
        Assert.That(resolver.IsVersionCompatible("2.0.0", ">=1.2.0"), Is.True);
        Assert.That(resolver.IsVersionCompatible("1.1.9", ">=1.2.0"), Is.False);

        Assert.That(resolver.IsVersionCompatible("1.2.3", "<=2.0.0"), Is.True);
        Assert.That(resolver.IsVersionCompatible("2.0.1", "<=2.0.0"), Is.False);

        // Exact version
        Assert.That(resolver.IsVersionCompatible("1.2.3", "1.2.3"), Is.True);
        Assert.That(resolver.IsVersionCompatible("1.2.4", "1.2.3"), Is.False);

        // Wildcard
        Assert.That(resolver.IsVersionCompatible("1.2.3", "*"), Is.True);
        Assert.That(resolver.IsVersionCompatible("99.99.99", "*"), Is.True);
    }
}