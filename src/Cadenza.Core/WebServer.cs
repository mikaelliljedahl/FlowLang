// Cadenza Core Compiler - Embedded Web Server Infrastructure
// Self-contained web runtime for Cadenza components

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;

namespace Cadenza.Core;

// =============================================================================
// EMBEDDED WEB SERVER
// =============================================================================

/// <summary>
/// Embedded Kestrel web server for serving Cadenza components as Blazor applications
/// </summary>
public class CadenzaWebServer
{
    private readonly CadenzaWebServerOptions _options;
    private readonly BlazorProjectGenerator _projectGenerator;
    
    public CadenzaWebServer(CadenzaWebServerOptions options)
    {
        _options = options;
        _projectGenerator = new BlazorProjectGenerator();
    }

    /// <summary>
    /// Starts the embedded web server and serves the Cadenza component
    /// </summary>
    public async Task StartAsync()
    {
        Console.WriteLine($"🌐 Starting Cadenza web server on port {_options.Port}...");
        
        // Create a temporary directory for the generated Blazor project
        var tempProjectDir = Path.Combine(Path.GetTempPath(), $"cadenza-web-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempProjectDir);
        
        try
        {
            // Generate the Blazor project from the Cadenza component
            await _projectGenerator.GenerateBlazorProjectAsync(_options.InputFile, tempProjectDir);
            
            // Create and start the web host
            var host = CreateWebHost(tempProjectDir);
            
            Console.WriteLine($"✅ Web server ready!");
            Console.WriteLine($"   URL: http://localhost:{_options.Port}");
            Console.WriteLine($"   Component: {_options.InputFile}");
            Console.WriteLine($"   Hot reload: {(_options.HotReload ? "enabled" : "disabled")}");
            
            if (_options.OpenBrowser)
            {
                OpenBrowser($"http://localhost:{_options.Port}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Press Ctrl+C to stop the server.");
            
            await host.RunAsync();
        }
        finally
        {
            // Clean up temporary directory
            if (Directory.Exists(tempProjectDir))
            {
                Directory.Delete(tempProjectDir, true);
            }
        }
    }

    private IHost CreateWebHost(string contentRoot)
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                    .UseContentRoot(contentRoot)
                    .UseUrls($"http://localhost:{_options.Port}")
                    .ConfigureServices(services =>
                    {
                        services.AddRazorPages();
                        services.AddServerSideBlazor();
                        
                        // Add Cadenza-specific services
                        services.AddSingleton(_options);
                        
                        if (_options.HotReload)
                        {
                            services.AddSingleton<CadenzaHotReloadService>();
                        }
                    })
                    .Configure(app =>
                    {
                        var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
                        
                        if (env.IsDevelopment())
                        {
                            app.UseDeveloperExceptionPage();
                        }
                        
                        app.UseStaticFiles();
                        app.UseRouting();
                        
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapRazorPages();
                            endpoints.MapBlazorHub();
                            endpoints.MapFallbackToPage("/_Host");
                            
                            if (_options.HotReload)
                            {
                                endpoints.MapPost("/_cadenza/reload", async context =>
                                {
                                    var hotReload = context.RequestServices.GetRequiredService<CadenzaHotReloadService>();
                                    await hotReload.TriggerReloadAsync();
                                    context.Response.StatusCode = 200;
                                });
                            }
                        });
                    });
            });
            
        return builder.Build();
    }
    
    private void OpenBrowser(string url)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open browser: {ex.Message}");
        }
    }
}

// =============================================================================
// WEB SERVER OPTIONS
// =============================================================================

/// <summary>
/// Configuration options for the Cadenza web server
/// </summary>
public class CadenzaWebServerOptions
{
    public required string InputFile { get; init; }
    public int Port { get; init; } = 5000;
    public bool OpenBrowser { get; init; } = true;
    public bool HotReload { get; init; } = true;
}

// =============================================================================
// BLAZOR PROJECT GENERATOR
// =============================================================================

/// <summary>
/// Generates a complete Blazor Server project from Cadenza components
/// </summary>
public class BlazorProjectGenerator
{
    private readonly EnhancedBlazorGenerator _blazorGenerator;
    
    public BlazorProjectGenerator()
    {
        _blazorGenerator = new EnhancedBlazorGenerator();
    }
    
    /// <summary>
    /// Generates a complete Blazor project structure from a Cadenza component
    /// </summary>
    public async Task GenerateBlazorProjectAsync(string cadenzaFile, string outputDir)
    {
        Console.WriteLine($"📦 Generating Blazor project...");
        
        // Parse the Cadenza component
        var source = await File.ReadAllTextAsync(cadenzaFile);
        var lexer = new CadenzaLexer(source);
        var tokens = lexer.ScanTokens();
        var parser = new CadenzaParser(tokens);
        var ast = parser.Parse();
        
        // Create project structure
        await CreateProjectStructureAsync(outputDir);
        
        // Generate Blazor components from Cadenza AST
        await GenerateBlazorComponentsAsync(ast, outputDir);
        
        // Generate project files
        await GenerateProjectFilesAsync(outputDir);
        
        Console.WriteLine($"   Generated project in: {outputDir}");
    }
    
    private async Task CreateProjectStructureAsync(string outputDir)
    {
        // Create necessary directories
        Directory.CreateDirectory(Path.Combine(outputDir, "Pages"));
        Directory.CreateDirectory(Path.Combine(outputDir, "Components"));
        Directory.CreateDirectory(Path.Combine(outputDir, "wwwroot"));
        Directory.CreateDirectory(Path.Combine(outputDir, "wwwroot", "css"));
        Directory.CreateDirectory(Path.Combine(outputDir, "wwwroot", "js"));
    }
    
    private async Task GenerateBlazorComponentsAsync(ProgramNode ast, string outputDir)
    {
        var allComponentCSS = new StringBuilder();
        
        // Find all component declarations in the AST
        foreach (var statement in ast.Statements)
        {
            if (statement is ComponentDeclaration component)
            {
                // Generate Blazor component using enhanced BlazorGenerator
                var blazorCode = _blazorGenerator.GenerateBlazorComponent(component);
                
                // Write to Components directory
                var componentPath = Path.Combine(outputDir, "Components", $"{component.Name}.cs");
                await File.WriteAllTextAsync(componentPath, blazorCode);
                
                // Generate semantic CSS for this component
                var componentCSS = _blazorGenerator.GenerateComponentCSS(component);
                Console.WriteLine($"   Generated CSS for {component.Name}: {componentCSS.Length} characters");
                allComponentCSS.AppendLine($"/* Component: {component.Name} */");
                allComponentCSS.AppendLine(componentCSS);
                allComponentCSS.AppendLine();
                
                // Also create a page that hosts this component
                var pagePath = Path.Combine(outputDir, "Pages", "Index.razor");
                var pageContent = GenerateIndexPage(component.Name);
                await File.WriteAllTextAsync(pagePath, pageContent);
            }
        }
        
        // Write the combined CSS file
        var cssPath = Path.Combine(outputDir, "wwwroot", "css", "components.css");
        await File.WriteAllTextAsync(cssPath, allComponentCSS.ToString());
        
        // If no components found, create a default one
        if (!ast.Statements.Any(s => s is ComponentDeclaration))
        {
            await CreateDefaultComponentAsync(outputDir);
        }
    }
    
    private async Task CreateDefaultComponentAsync(string outputDir)
    {
        var defaultComponent = @"@page ""/""
@using Microsoft.AspNetCore.Components

<h1>Cadenza Application</h1>
<p>This is a default component generated from your Cadenza code.</p>
<p>Welcome to your self-contained web application!</p>

@code {
    // Component logic will be generated here
}";
        
        var pagePath = Path.Combine(outputDir, "Pages", "Index.razor");
        await File.WriteAllTextAsync(pagePath, defaultComponent);
    }
    
    private string GenerateIndexPage(string componentName)
    {
        return $@"@page ""/""
@using Components

<PageTitle>Cadenza App</PageTitle>

<h1>Cadenza Application</h1>

<{componentName} />

@code {{
    // Main page hosting the Cadenza component
}}";
    }
    
    private async Task GenerateProjectFilesAsync(string outputDir)
    {
        // Generate _Host.cshtml
        var hostContent = @"@page ""/"" 
@namespace CadenzaWebApp.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@{
    Layout = null;
}

<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>Cadenza Web App</title>
    <base href=""~/"" />
    <link href=""css/site.css"" rel=""stylesheet"" />
    <link href=""css/components.css"" rel=""stylesheet"" />
</head>
<body>
    <div id=""app"">
        <component type=""typeof(App)"" render-mode=""ServerPrerendered"" />
    </div>

    <script src=""_framework/blazor.server.js""></script>
</body>
</html>";
        
        var hostPath = Path.Combine(outputDir, "Pages", "_Host.cshtml");
        await File.WriteAllTextAsync(hostPath, hostContent);
        
        // Generate App.razor
        var appContent = @"<Router AppAssembly=""@typeof(App).Assembly"">
    <Found Context=""routeData"">
        <RouteView RouteData=""@routeData"" DefaultLayout=""@typeof(MainLayout)"" />
    </Found>
    <NotFound>
        <PageTitle>Not found</PageTitle>
        <LayoutView Layout=""@typeof(MainLayout)"">
            <p role=""alert"">Sorry, there's nothing at this address.</p>
        </LayoutView>
    </NotFound>
</Router>";
        
        var appPath = Path.Combine(outputDir, "App.razor");
        await File.WriteAllTextAsync(appPath, appContent);
        
        // Generate MainLayout.razor
        var layoutContent = @"@inherits LayoutView

<div class=""page"">
    <main>
        @Body
    </main>
</div>";
        
        var layoutPath = Path.Combine(outputDir, "MainLayout.razor");
        await File.WriteAllTextAsync(layoutPath, layoutContent);
        
        // Generate Program.cs
        var programContent = @"using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(""/Error"");
}

app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage(""/_Host"");

app.Run();";
        
        var programPath = Path.Combine(outputDir, "Program.cs");
        await File.WriteAllTextAsync(programPath, programContent);
        
        // Generate enhanced base CSS with semantic design system
        var cssContent = @"/* Cadenza Base Styles */
html, body {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
    line-height: 1.6;
    margin: 0;
    padding: 0;
    background-color: var(--color-background, #ffffff);
    color: var(--color-text, #1f2937);
}

*, *::before, *::after {
    box-sizing: border-box;
}

.page {
    position: relative;
    display: flex;
    flex-direction: column;
    min-height: 100vh;
}

main {
    flex: 1;
    padding: var(--spacing-md, 1rem);
}

/* Typography */
h1, h2, h3, h4, h5, h6 {
    margin: 0 0 var(--spacing-md, 1rem) 0;
    font-weight: 600;
    line-height: 1.25;
    color: var(--color-text, #1f2937);
}

h1 { font-size: var(--font-size-3xl, 1.875rem); }
h2 { font-size: var(--font-size-2xl, 1.5rem); }
h3 { font-size: var(--font-size-xl, 1.25rem); }
h4 { font-size: var(--font-size-lg, 1.125rem); }
h5 { font-size: var(--font-size-base, 1rem); }
h6 { font-size: var(--font-size-sm, 0.875rem); }

p {
    margin: 0 0 var(--spacing-md, 1rem) 0;
}

/* Interactive elements */
button {
    font-family: inherit;
    cursor: pointer;
    border: none;
    outline: none;
    transition: all 0.2s ease-in-out;
}

button:focus-visible {
    outline: 2px solid var(--color-focus, #3b82f6);
    outline-offset: 2px;
}

button:disabled {
    cursor: not-allowed;
    opacity: 0.6;
}

/* Utility classes for immediate use */
.container {
    max-width: 1200px;
    margin: 0 auto;
    padding: 0 var(--spacing-md, 1rem);
}

.sr-only {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
    border: 0;
}";
        
        var cssPath = Path.Combine(outputDir, "wwwroot", "css", "site.css");
        await File.WriteAllTextAsync(cssPath, cssContent);
    }
}

// =============================================================================
// HOT RELOAD SERVICE
// =============================================================================

/// <summary>
/// Service for handling hot reload functionality
/// </summary>
public class CadenzaHotReloadService
{
    private readonly List<Func<Task>> _reloadCallbacks = new();
    
    public void RegisterReloadCallback(Func<Task> callback)
    {
        _reloadCallbacks.Add(callback);
    }
    
    public async Task TriggerReloadAsync()
    {
        foreach (var callback in _reloadCallbacks)
        {
            try
            {
                await callback();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hot reload callback failed: {ex.Message}");
            }
        }
    }
}