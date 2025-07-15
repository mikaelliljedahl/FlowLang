// FlowLang Direct Compiler - Roslyn-based compilation to assemblies
// This file extends the existing transpiler with direct compilation capabilities

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;

namespace FlowLang.Core
{
    /// <summary>
    /// Result of a direct compilation operation
    /// </summary>
    public record CompilationResult(
        bool Success,
        IEnumerable<Diagnostic> Diagnostics,
        string? AssemblyPath = null,
        TimeSpan? CompilationTime = null
    );

    /// <summary>
    /// Options for direct compilation
    /// </summary>
    public record CompilationOptions(
        string SourceFile,
        string OutputPath,
        OutputKind OutputKind = OutputKind.ConsoleApplication,
        bool OptimizeCode = true,
        bool IncludeDebugSymbols = false,
        string[]? AdditionalReferences = null
    );

    /// <summary>
    /// Direct compiler that generates assemblies from FlowLang source using Roslyn
    /// </summary>
    public class DirectCompiler
    {
        private readonly FlowLangTranspiler _transpiler;
        private readonly CompilationCache _cache;

        public DirectCompiler()
        {
            _transpiler = new FlowLangTranspiler();
            _cache = new CompilationCache();
        }

        /// <summary>
        /// Compiles FlowLang source directly to an assembly
        /// </summary>
        public async Task<CompilationResult> CompileToAssemblyAsync(CompilationOptions options)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Step 1: Parse FlowLang source to AST
                var source = await File.ReadAllTextAsync(options.SourceFile);
                var lexer = new FlowLangLexer(source);
                var tokens = lexer.ScanTokens();
                var parser = new FlowLangParser(tokens);
                var ast = parser.Parse();

                // Step 2: Generate C# syntax tree
                var generator = new CSharpGenerator();
                var syntaxTree = generator.GenerateFromAST(ast);

                // Step 3: Create compilation with references
                var compilation = CreateCompilation(syntaxTree, options);

                // Step 4: Emit assembly
                var emitResult = compilation.Emit(options.OutputPath);

                var compilationTime = DateTime.UtcNow - startTime;

                return new CompilationResult(
                    Success: emitResult.Success,
                    Diagnostics: emitResult.Diagnostics,
                    AssemblyPath: emitResult.Success ? options.OutputPath : null,
                    CompilationTime: compilationTime
                );
            }
            catch (Exception ex)
            {
                var compilationTime = DateTime.UtcNow - startTime;
                var diagnostic = Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "FL0001",
                        "Compilation Error",
                        ex.Message,
                        "Compiler",
                        DiagnosticSeverity.Error,
                        true
                    ),
                    Location.None
                );

                return new CompilationResult(
                    Success: false,
                    Diagnostics: new[] { diagnostic },
                    CompilationTime: compilationTime
                );
            }
        }

        /// <summary>
        /// Compiles and immediately executes the FlowLang program
        /// </summary>
        public async Task<(CompilationResult CompilationResult, int? ExitCode)> CompileAndRunAsync(CompilationOptions options)
        {
            var compilationResult = await CompileToAssemblyAsync(options);
            
            if (!compilationResult.Success || compilationResult.AssemblyPath == null)
            {
                return (compilationResult, null);
            }

            try
            {
                // Load and execute the assembly
                var assembly = Assembly.LoadFrom(compilationResult.AssemblyPath);
                var entryPoint = assembly.EntryPoint;

                if (entryPoint == null)
                {
                    return (compilationResult, null);
                }

                var result = entryPoint.Invoke(null, new object[] { Array.Empty<string>() });
                var exitCode = result is int code ? code : 0;

                return (compilationResult, exitCode);
            }
            catch (Exception ex)
            {
                var diagnostic = Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "FL0002",
                        "Execution Error",
                        ex.Message,
                        "Runtime",
                        DiagnosticSeverity.Error,
                        true
                    ),
                    Location.None
                );

                var updatedResult = compilationResult with
                {
                    Success = false,
                    Diagnostics = compilationResult.Diagnostics.Append(diagnostic)
                };

                return (updatedResult, null);
            }
        }

        /// <summary>
        /// Creates a CSharpCompilation with proper references and options
        /// </summary>
        private CSharpCompilation CreateCompilation(SyntaxTree syntaxTree, CompilationOptions options)
        {
            var references = GetDefaultReferences();
            
            if (options.AdditionalReferences != null)
            {
                var additionalRefs = options.AdditionalReferences
                    .Select(path => MetadataReference.CreateFromFile(path));
                references = references.Concat(additionalRefs).ToArray();
            }

            var compilationOptions = new CSharpCompilationOptions(
                outputKind: options.OutputKind,
                optimizationLevel: options.OptimizeCode ? OptimizationLevel.Release : OptimizationLevel.Debug,
                allowUnsafe: false,
                platform: Platform.AnyCpu
            );

            var assemblyName = Path.GetFileNameWithoutExtension(options.OutputPath);
            
            return CSharpCompilation.Create(
                assemblyName: assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: compilationOptions
            );
        }

        /// <summary>
        /// Gets the default .NET references needed for FlowLang programs
        /// </summary>
        private MetadataReference[] GetDefaultReferences()
        {
            return new[]
            {
                // Core .NET references
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.RuntimeHelpers).Assembly.Location),
                
                // System.Runtime reference (needed for modern .NET)
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                
                // System.Collections reference
                MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
                
                // System.Text reference (for string operations)
                MetadataReference.CreateFromFile(Assembly.Load("System.Text.RegularExpressions").Location)
            };
        }
    }

    /// <summary>
    /// Caches compilation objects for performance optimization
    /// </summary>
    public class CompilationCache
    {
        private readonly Dictionary<string, CSharpCompilation> _compilationCache = new();
        private readonly Dictionary<string, DateTime> _lastModified = new();

        public bool TryGetCachedCompilation(string sourceFile, out CSharpCompilation? compilation)
        {
            compilation = null;

            if (!_compilationCache.ContainsKey(sourceFile))
                return false;

            var fileInfo = new FileInfo(sourceFile);
            if (!fileInfo.Exists)
                return false;

            if (_lastModified.ContainsKey(sourceFile) && 
                _lastModified[sourceFile] >= fileInfo.LastWriteTime)
            {
                compilation = _compilationCache[sourceFile];
                return true;
            }

            // File has been modified, remove from cache
            _compilationCache.Remove(sourceFile);
            _lastModified.Remove(sourceFile);
            return false;
        }

        public void CacheCompilation(string sourceFile, CSharpCompilation compilation)
        {
            var fileInfo = new FileInfo(sourceFile);
            if (!fileInfo.Exists) return;

            _compilationCache[sourceFile] = compilation;
            _lastModified[sourceFile] = fileInfo.LastWriteTime;
        }

        public void ClearCache()
        {
            _compilationCache.Clear();
            _lastModified.Clear();
        }
    }

    /// <summary>
    /// Enhanced CLI program with direct compilation support
    /// </summary>
    public class DirectCompilerCLI
    {
        private readonly DirectCompiler _compiler;

        public DirectCompilerCLI()
        {
            _compiler = new DirectCompiler();
        }

        /// <summary>
        /// Enhanced main method with direct compilation support
        /// </summary>
        public async Task<int> RunAsync(string[] args)
        {
            try
            {
                var options = ParseArguments(args);
                
                if (options == null)
                {
                    ShowHelp();
                    return 1;
                }

                if (options.ShowHelp)
                {
                    ShowHelp();
                    return 0;
                }

                if (options.ShowVersion)
                {
                    Console.WriteLine("FlowLang Direct Compiler v1.0.0");
                    return 0;
                }

                if (options.CompileMode)
                {
                    return await HandleCompileMode(options);
                }
                else
                {
                    return await HandleTranspileMode(options);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> HandleCompileMode(CLIOptions options)
        {
            var compilationOptions = new CompilationOptions(
                SourceFile: options.InputFile!,
                OutputPath: options.OutputFile ?? GetDefaultOutputPath(options.InputFile!, options.Library),
                OutputKind: options.Library ? OutputKind.DynamicallyLinkedLibrary : OutputKind.ConsoleApplication,
                OptimizeCode: !options.Debug,
                IncludeDebugSymbols: options.Debug
            );

            if (options.Run)
            {
                var (result, exitCode) = await _compiler.CompileAndRunAsync(compilationOptions);
                
                if (!result.Success)
                {
                    DisplayCompilationErrors(result.Diagnostics);
                    return 1;
                }

                Console.WriteLine($"Compilation successful: {compilationOptions.OutputPath}");
                Console.WriteLine($"Compilation time: {result.CompilationTime?.TotalMilliseconds:F2}ms");
                
                if (exitCode.HasValue)
                {
                    Console.WriteLine($"Program exited with code: {exitCode.Value}");
                    return exitCode.Value;
                }
                
                return 0;
            }
            else
            {
                var result = await _compiler.CompileToAssemblyAsync(compilationOptions);
                
                if (!result.Success)
                {
                    DisplayCompilationErrors(result.Diagnostics);
                    return 1;
                }

                Console.WriteLine($"Compilation successful: {compilationOptions.OutputPath}");
                Console.WriteLine($"Compilation time: {result.CompilationTime?.TotalMilliseconds:F2}ms");
                return 0;
            }
        }

        private async Task<int> HandleTranspileMode(CLIOptions options)
        {
            // Use existing transpiler for backward compatibility
            var transpiler = new FlowLangTranspiler();
            
            switch (options.Target?.ToLowerInvariant())
            {
                case "csharp":
                case "cs":
                case null:
                    await transpiler.TranspileAsync(options.InputFile!, options.OutputFile);
                    Console.WriteLine($"Successfully transpiled {options.InputFile} -> {options.OutputFile}");
                    break;
                
                case "javascript":
                case "js":
                    await transpiler.TranspileToJavaScriptAsync(options.InputFile!, options.OutputFile);
                    Console.WriteLine($"Successfully transpiled {options.InputFile} -> {options.OutputFile} (JavaScript)");
                    break;
                
                default:
                    Console.Error.WriteLine($"Error: Unsupported target '{options.Target}'. Supported targets: csharp, javascript");
                    return 1;
            }
            
            return 0;
        }

        private void DisplayCompilationErrors(IEnumerable<Diagnostic> diagnostics)
        {
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
            
            foreach (var error in errors)
            {
                Console.Error.WriteLine($"Error: {error.GetMessage()}");
                
                if (error.Location != Location.None)
                {
                    var lineSpan = error.Location.GetLineSpan();
                    Console.Error.WriteLine($"  at line {lineSpan.StartLinePosition.Line + 1}, column {lineSpan.StartLinePosition.Character + 1}");
                }
            }
        }

        private string GetDefaultOutputPath(string inputFile, bool isLibrary)
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(inputFile);
            var extension = isLibrary ? ".dll" : ".exe";
            return nameWithoutExtension + extension;
        }

        private CLIOptions? ParseArguments(string[] args)
        {
            if (args.Length == 0)
                return new CLIOptions { ShowHelp = true };

            var options = new CLIOptions();
            
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--help":
                    case "-h":
                        options.ShowHelp = true;
                        return options;
                    
                    case "--version":
                    case "-v":
                        options.ShowVersion = true;
                        return options;
                    
                    case "--compile":
                    case "-c":
                        options.CompileMode = true;
                        break;
                    
                    case "--run":
                    case "-r":
                        options.Run = true;
                        options.CompileMode = true; // --run implies --compile
                        break;
                    
                    case "--library":
                    case "-l":
                        options.Library = true;
                        break;
                    
                    case "--debug":
                    case "-d":
                        options.Debug = true;
                        break;
                    
                    case "--output":
                    case "-o":
                        if (i + 1 < args.Length)
                        {
                            options.OutputFile = args[++i];
                        }
                        break;
                    
                    case "--target":
                    case "-t":
                        if (i + 1 < args.Length)
                        {
                            options.Target = args[++i];
                        }
                        break;
                    
                    default:
                        if (!args[i].StartsWith('-'))
                        {
                            if (options.InputFile == null)
                            {
                                options.InputFile = args[i];
                            }
                            else if (options.OutputFile == null && !options.CompileMode)
                            {
                                options.OutputFile = args[i];
                            }
                        }
                        break;
                }
            }

            // Validation
            if (options.InputFile == null && !options.ShowHelp && !options.ShowVersion)
            {
                Console.Error.WriteLine("Error: Input file required");
                return null;
            }

            // Set default output file for transpile mode
            if (!options.CompileMode && options.OutputFile == null && options.InputFile != null)
            {
                var extension = options.Target?.ToLowerInvariant() switch
                {
                    "javascript" or "js" => ".js",
                    _ => ".cs"
                };
                options.OutputFile = Path.ChangeExtension(options.InputFile, extension);
            }

            return options;
        }

        private void ShowHelp()
        {
            Console.WriteLine("FlowLang Direct Compiler - Transpilation and Direct Compilation");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  Transpile (default):");
            Console.WriteLine("    flowc-core <input.flow> [<output.cs>] [--target csharp|javascript]");
            Console.WriteLine();
            Console.WriteLine("  Direct compilation:");
            Console.WriteLine("    flowc-core --compile <input.flow> [--output <output.exe>]");
            Console.WriteLine("    flowc-core --run <input.flow>");
            Console.WriteLine("    flowc-core --library <input.flow> [--output <output.dll>]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --compile, -c   Compile directly to assembly (default: transpile)");
            Console.WriteLine("  --run, -r       Compile and run immediately");
            Console.WriteLine("  --library, -l   Generate library (.dll) instead of executable");
            Console.WriteLine("  --debug, -d     Include debug symbols and disable optimizations");
            Console.WriteLine("  --output, -o    Specify output file path");
            Console.WriteLine("  --target, -t    Target language for transpilation (csharp, javascript)");
            Console.WriteLine("  --help, -h      Show this help message");
            Console.WriteLine("  --version, -v   Show version information");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  # Transpile to C#");
            Console.WriteLine("  flowc-core hello.flow hello.cs");
            Console.WriteLine();
            Console.WriteLine("  # Direct compilation");
            Console.WriteLine("  flowc-core --compile hello.flow");
            Console.WriteLine("  flowc-core --compile hello.flow --output hello.exe");
            Console.WriteLine();
            Console.WriteLine("  # Compile and run");
            Console.WriteLine("  flowc-core --run hello.flow");
            Console.WriteLine();
            Console.WriteLine("  # Generate library");
            Console.WriteLine("  flowc-core --library math.flow --output math.dll");
        }
    }

    /// <summary>
    /// CLI options parsed from command line arguments
    /// </summary>
    public class CLIOptions
    {
        public string? InputFile { get; set; }
        public string? OutputFile { get; set; }
        public bool CompileMode { get; set; }
        public bool Run { get; set; }
        public bool Library { get; set; }
        public bool Debug { get; set; }
        public string? Target { get; set; }
        public bool ShowHelp { get; set; }
        public bool ShowVersion { get; set; }
    }
}