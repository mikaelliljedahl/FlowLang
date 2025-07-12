# FlowLang Package Manager

The FlowLang Package Manager provides seamless dependency management with .NET ecosystem integration, automatic effect inference, and comprehensive security scanning.

## Overview

FlowLang's enhanced package manager extends the basic `flowc.json` configuration to provide:

- **Semantic versioning** with conflict resolution
- **NuGet ecosystem integration** with automatic FlowLang bindings
- **Effect inference** for external dependencies
- **Security vulnerability scanning** and automated fixes
- **Workspace management** for multi-project solutions
- **FlowLang-specific package registry** for publishing and discovery

## Installation and Setup

The package manager is built into the FlowLang CLI. No additional installation is required.

```bash
# Check if package management is available
flowc --help

# Create a new project with package management
flowc new my-project
cd my-project
```

## Project Configuration

### Enhanced flowc.json

The enhanced `flowc.json` provides comprehensive package management configuration:

```json
{
  "name": "my-flowlang-app",
  "version": "1.2.0",
  "description": "A FlowLang application with package management",
  "dependencies": {
    "FlowLang.Core": "^2.1.0",
    "FlowLang.Database": "~1.5.2",
    "Newtonsoft.Json": "13.0.3"
  },
  "devDependencies": {
    "FlowLang.Testing": "^1.0.0"
  },
  "nugetSources": [
    "https://api.nuget.org/v3/index.json",
    "https://nuget.mycompany.com/v3/index.json"
  ],
  "flowlangRegistry": "https://packages.flowlang.org",
  "effectMappings": {
    "System.Data.SqlClient": ["Database"],
    "System.Net.Http": ["Network"],
    "Microsoft.Extensions.Logging": ["Logging"]
  },
  "workspace": {
    "projects": ["./src", "./libs/utils"],
    "exclude": ["./examples", "./tests"]
  },
  "publishConfig": {
    "registry": "https://packages.flowlang.org",
    "access": "public"
  }
}
```

### Lock File (flowc.lock)

The lock file ensures reproducible builds across environments:

```json
{
  "lockfileVersion": 2,
  "resolved": {
    "FlowLang.Core@2.1.3": {
      "version": "2.1.3",
      "resolved": "https://packages.flowlang.org/FlowLang.Core/-/FlowLang.Core-2.1.3.tgz",
      "integrity": "sha512-abc123...",
      "dependencies": {
        "Newtonsoft.Json": "13.0.3"
      },
      "effects": ["Memory", "IO"]
    }
  },
  "generatedAt": "2024-01-15T10:30:00.000Z"
}
```

## Package Management Commands

### Adding Packages

```bash
# Add runtime dependency
flowc add FlowLang.Database

# Add specific version
flowc add Newtonsoft.Json@13.0.3

# Add with version constraint
flowc add FlowLang.Testing@^1.0.0 --dev

# Add from NuGet
flowc add Microsoft.Extensions.Logging
```

### Removing Packages

```bash
# Remove package
flowc remove FlowLang.Database

# Remove development dependency
flowc remove FlowLang.Testing
```

### Installing Dependencies

```bash
# Install all dependencies
flowc install

# Install only production dependencies
flowc install --production

# Install in workspace (all projects)
flowc workspace install
```

### Updating Packages

```bash
# Update all packages to latest compatible versions
flowc update

# Update specific package
flowc update FlowLang.Core

# Show outdated packages
flowc outdated
```

### Package Discovery

```bash
# Search for packages
flowc search http

# Get package information
flowc info FlowLang.Database

# List installed packages
flowc list
```

## Effect System Integration

### Automatic Effect Inference

The package manager automatically infers effects for external dependencies:

```flowlang
// NuGet package: EntityFramework
import EntityFramework.{DbContext, SaveChanges}

function save_user(context: DbContext, user: User) uses [Database] -> Result<Unit, DbError> {
    // Effect automatically inferred from EntityFramework usage
    return context.SaveChanges(user)
}
```

### Effect Mapping Configuration

Configure custom effect mappings in `flowc.json`:

```json
{
  "effectMappings": {
    "System.Data.SqlClient.*": ["Database"],
    "System.Net.Http.*": ["Network"],
    "Microsoft.Extensions.Logging.*": ["Logging"],
    "Azure.Storage.*": ["Network", "FileSystem"],
    "Redis.StackExchange.*": ["Network", "Memory"]
  }
}
```

## NuGet Integration

### Automatic FlowLang Bindings

The package manager automatically generates FlowLang bindings for .NET libraries:

```csharp
// C# library: System.Net.Http
namespace System.Net.Http {
    public class HttpClient { 
        public Task<string> GetStringAsync(string url); 
    }
}
```

Generated FlowLang binding:
```flowlang
module System_Net_Http {
    function http_get(url: string) uses [Network] -> Result<string, HttpError> {
        // Implementation bridges to System.Net.Http.HttpClient
    }
    
    export { http_get }
}
```

### Supported .NET Frameworks

- .NET 8.0+ (primary target)
- .NET Standard 2.1
- .NET Framework 4.8 (compatibility mode)

## Security and Vulnerability Management

### Security Scanning

```bash
# Scan for vulnerabilities
flowc audit

# Scan with detailed output
flowc audit --verbose

# Automatically fix vulnerabilities
flowc audit fix

# Scan specific categories
flowc audit --effects --security
```

### Vulnerability Report Example

```bash
$ flowc audit
Security audit completed for 15 packages
Vulnerabilities found: 3
  Critical: 1
  High: 1
  Medium: 1
  Low: 0

Vulnerable packages:
  System.Net.Http@4.3.0: CVE-2017-0247 (Critical)
    Fixed in: 4.3.1
  
Run 'flowc audit fix' to automatically fix compatible issues.
```

### Security Databases

The scanner integrates with multiple vulnerability databases:
- **GitHub Advisory Database** (NuGet packages)
- **OSV (Open Source Vulnerabilities)** database
- **FlowLang Security Database** (FlowLang-specific packages)

## Workspace Management

### Multi-Project Setup

```json
{
  "name": "my-workspace",
  "workspace": {
    "projects": ["./apps/*", "./libs/*"],
    "exclude": ["./examples"]
  }
}
```

### Workspace Commands

```bash
# List workspace projects
flowc workspace list

# Install dependencies for all projects
flowc workspace install

# Run command across workspace
flowc workspace run build
flowc workspace run test
```

### Workspace Structure Example

```
my-workspace/
├── flowc.json (workspace root)
├── apps/
│   ├── web-app/
│   │   ├── flowc.json
│   │   └── src/main.flow
│   └── api-service/
│       ├── flowc.json
│       └── src/main.flow
├── libs/
│   ├── utils/
│   │   ├── flowc.json
│   │   └── src/helpers.flow
│   └── data/
│       ├── flowc.json
│       └── src/models.flow
└── flowc.lock (unified dependencies)
```

## Package Publishing

### Creating a Package

```bash
# Create package archive
flowc pack

# Create and publish
flowc publish

# Dry run (test without publishing)
flowc publish --dry-run

# Private package
flowc publish --private
```

### Package Structure

A FlowLang package includes:

```
sample-package-1.0.0.tgz
├── flowc.json
├── src/
│   └── main.flow
├── README.md
└── LICENSE
```

### Publishing Requirements

- Valid `flowc.json` with name, version, and description
- Semantic versioning (e.g., 1.2.3)
- At least one `.flow` source file
- README.md (recommended)
- LICENSE file (recommended)

## Version Management

### Semantic Versioning

FlowLang follows [Semantic Versioning](https://semver.org/):

- **Major** (X.y.z): Breaking changes
- **Minor** (x.Y.z): New features, backward compatible
- **Patch** (x.y.Z): Bug fixes, backward compatible

### Version Constraints

```json
{
  "dependencies": {
    "exact": "1.2.3",
    "caret": "^1.2.3",  // >=1.2.3 <2.0.0
    "tilde": "~1.2.3",  // >=1.2.3 <1.3.0
    "range": ">=1.2.0 <2.0.0",
    "wildcard": "*"     // Any version
  }
}
```

### Version Commands

```bash
# Show current version
flowc version

# Bump patch version (1.0.0 -> 1.0.1)
flowc version patch

# Bump minor version (1.0.1 -> 1.1.0)
flowc version minor

# Bump major version (1.1.0 -> 2.0.0)
flowc version major

# Set specific version
flowc version 2.1.0-beta.1
```

## Advanced Features

### Private Package Registries

Configure private registries for enterprise use:

```json
{
  "nugetSources": [
    "https://api.nuget.org/v3/index.json",
    "https://nuget.mycompany.com/v3/index.json"
  ],
  "flowlangRegistry": "https://packages.mycompany.com"
}
```

### Dependency Resolution

The resolver handles complex scenarios:

- **Version conflicts**: Automatically resolves compatible versions
- **Circular dependencies**: Detects and reports circular references
- **Transitive dependencies**: Resolves entire dependency graph
- **Effect propagation**: Tracks effects through dependency chain

### Performance Optimization

- **Local caching**: Downloaded packages cached locally
- **Parallel downloads**: Multiple packages downloaded concurrently
- **Lock file validation**: Fast dependency checking
- **Incremental resolution**: Only re-resolve changed dependencies

## CI/CD Integration

### Continuous Integration

```yaml
# GitHub Actions example
- name: Install FlowLang dependencies
  run: flowc install --production

- name: Security audit
  run: flowc audit

- name: Build project
  run: flowc build
```

### Lock File Best Practices

- **Commit lock files** to version control
- **Validate lock files** in CI pipelines
- **Use `--production`** in production builds
- **Audit regularly** for security vulnerabilities

## Troubleshooting

### Common Issues

**Package not found:**
```bash
# Check spelling and availability
flowc search package-name
flowc info package-name
```

**Version conflicts:**
```bash
# View dependency tree
flowc list --tree

# Force resolution
flowc install --force-resolve
```

**Lock file issues:**
```bash
# Regenerate lock file
rm flowc.lock
flowc install
```

**Network issues:**
```bash
# Clear cache
flowc clean

# Use different registry
flowc add package-name --registry=https://alternate-registry.com
```

### Debug Mode

Enable verbose logging for troubleshooting:

```bash
# Verbose output
flowc install --verbose

# Debug dependency resolution
flowc install --debug

# Show package resolution steps
flowc install --trace
```

## Migration Guide

### From Basic flowc.json

Existing FlowLang projects automatically upgrade to the enhanced package manager:

```json
// Old format
{
  "name": "my-project",
  "version": "1.0.0",
  "dependencies": {
    "some-package": "1.0.0"
  }
}

// New format (backward compatible)
{
  "name": "my-project",
  "version": "1.0.0",
  "dependencies": {
    "some-package": "1.0.0"
  },
  "devDependencies": {},
  "nugetSources": ["https://api.nuget.org/v3/index.json"],
  "effectMappings": {}
}
```

### From Other Package Managers

**From npm:**
```bash
# Convert package.json dependencies
flowc migrate --from=npm

# Manual conversion
# npm: "lodash": "^4.17.21"
# FlowLang: "Lodash.Net": "^1.0.0"
```

**From NuGet packages.config:**
```bash
# Convert packages.config
flowc migrate --from=nuget-packages-config
```

## Best Practices

### Project Organization

1. **Use semantic versioning** for all packages
2. **Pin exact versions** for critical dependencies
3. **Use version ranges** for libraries to allow updates
4. **Separate dev dependencies** from runtime dependencies
5. **Configure effect mappings** for better static analysis

### Security

1. **Run security audits** regularly (`flowc audit`)
2. **Keep dependencies updated** (`flowc update`)
3. **Review dependency licenses** before adding
4. **Use private registries** for proprietary packages
5. **Validate package signatures** in production

### Performance

1. **Use lock files** for reproducible builds
2. **Clean cache** periodically (`flowc clean`)
3. **Use workspace commands** for monorepos
4. **Minimize dependency count** to reduce build time
5. **Use `--production`** in production builds

## Examples

### Basic Package Setup

```flowlang
// math_utils.flow
module MathUtils {
    pure function add(a: int, b: int) -> int {
        return a + b
    }
    
    pure function multiply(a: int, b: int) -> int {
        return a * b
    }
    
    export { add, multiply }
}
```

```json
// flowc.json
{
  "name": "math-utils",
  "version": "1.0.0",
  "description": "Mathematical utility functions",
  "dependencies": {},
  "publishConfig": {
    "access": "public"
  }
}
```

### Using External Dependencies

```flowlang
// web_service.flow
import HttpClient.{get, post}
import Database.{connect, query}

function fetch_user_data(user_id: string) uses [Database, Network] -> Result<User, Error> {
    let db = connect("postgresql://localhost/mydb")?
    let user = query(db, "SELECT * FROM users WHERE id = $1", [user_id])?
    return Ok(user)
}

function notify_user(user: User, message: string) uses [Network] -> Result<Unit, HttpError> {
    let response = post("https://api.notifications.com/send", {
        user_id: user.id,
        message: message
    })?
    return Ok(())
}
```

### Workspace Configuration

```json
// Root flowc.json
{
  "name": "my-company-platform",
  "workspace": {
    "projects": [
      "./services/*",
      "./libs/*",
      "./tools/*"
    ],
    "exclude": [
      "./services/legacy",
      "./tools/experimental"
    ]
  },
  "effectMappings": {
    "Company.Database.*": ["Database"],
    "Company.Messaging.*": ["Network"],
    "Company.Logging.*": ["Logging"]
  }
}
```

This comprehensive package management system provides FlowLang developers with powerful dependency management capabilities while maintaining the language's core principles of explicitness, safety, and effect tracking.