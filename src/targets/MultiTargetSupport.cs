using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FlowLang.Compiler;

namespace FlowLang.Targets
{
    /// <summary>
    /// FlowLang Multi-Target Compilation Support
    /// Enables compilation to multiple platforms: C#, JVM, Native, WebAssembly
    /// </summary>
    public class MultiTargetCompiler
    {
        private readonly Dictionary<TargetPlatform, ITargetGenerator> _generators;
        private readonly TargetConfiguration _config;

        public MultiTargetCompiler(TargetConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _generators = InitializeGenerators();
        }

        /// <summary>
        /// Compile FlowLang program to specified target platform
        /// </summary>
        public async Task<CompilationResult> CompileToTargetAsync(
            Program program, 
            TargetPlatform target, 
            string outputPath)
        {
            if (!_generators.TryGetValue(target, out var generator))
            {
                return CompilationResult.Failed($"Target platform {target} not supported");
            }

            try
            {
                var targetCode = await generator.GenerateAsync(program, _config);
                
                await File.WriteAllTextAsync(outputPath, targetCode.SourceCode);
                
                // Copy additional files if needed
                if (targetCode.AdditionalFiles.Any())
                {
                    var outputDir = Path.GetDirectoryName(outputPath) ?? "";
                    foreach (var additionalFile in targetCode.AdditionalFiles)
                    {
                        var filePath = Path.Combine(outputDir, additionalFile.Name);
                        await File.WriteAllTextAsync(filePath, additionalFile.Content);
                    }
                }

                return CompilationResult.Success(outputPath, target, targetCode);
            }
            catch (Exception ex)
            {
                return CompilationResult.Failed($"Compilation to {target} failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Compile to multiple targets simultaneously
        /// </summary>
        public async Task<MultiTargetResult> CompileToMultipleTargetsAsync(
            Program program,
            List<TargetPlatform> targets,
            string outputDirectory)
        {
            var results = new Dictionary<TargetPlatform, CompilationResult>();
            var tasks = new List<Task<(TargetPlatform, CompilationResult)>>();

            foreach (var target in targets)
            {
                var outputFile = Path.Combine(outputDirectory, GetTargetFileName(target));
                tasks.Add(CompileToTargetWithPlatformAsync(program, target, outputFile));
            }

            var completedTasks = await Task.WhenAll(tasks);

            foreach (var (platform, result) in completedTasks)
            {
                results[platform] = result;
            }

            var successCount = results.Values.Count(r => r.IsSuccess);
            var totalCount = results.Count;

            return new MultiTargetResult
            {
                Results = results,
                OverallSuccess = successCount == totalCount,
                SuccessCount = successCount,
                TotalCount = totalCount
            };
        }

        /// <summary>
        /// Get available target platforms
        /// </summary>
        public List<TargetPlatform> GetAvailableTargets()
        {
            return _generators.Keys.ToList();
        }

        /// <summary>
        /// Get target-specific configuration options
        /// </summary>
        public TargetOptions GetTargetOptions(TargetPlatform target)
        {
            return target switch
            {
                TargetPlatform.CSharp => new CSharpTargetOptions(),
                TargetPlatform.Java => new JavaTargetOptions(),
                TargetPlatform.Native => new NativeTargetOptions(),
                TargetPlatform.WebAssembly => new WebAssemblyTargetOptions(),
                TargetPlatform.JavaScript => new JavaScriptTargetOptions(),
                _ => new TargetOptions()
            };
        }

        private Dictionary<TargetPlatform, ITargetGenerator> InitializeGenerators()
        {
            return new Dictionary<TargetPlatform, ITargetGenerator>
            {
                [TargetPlatform.CSharp] = new CSharpGenerator(),
                [TargetPlatform.Java] = new JavaGenerator(),
                [TargetPlatform.Native] = new NativeGenerator(),
                [TargetPlatform.WebAssembly] = new WebAssemblyGenerator(),
                [TargetPlatform.JavaScript] = new JavaScriptGenerator()
            };
        }

        private async Task<(TargetPlatform, CompilationResult)> CompileToTargetWithPlatformAsync(
            Program program, 
            TargetPlatform target, 
            string outputPath)
        {
            var result = await CompileToTargetAsync(program, target, outputPath);
            return (target, result);
        }

        private string GetTargetFileName(TargetPlatform target)
        {
            return target switch
            {
                TargetPlatform.CSharp => "Program.cs",
                TargetPlatform.Java => "Program.java",
                TargetPlatform.Native => "program.cpp",
                TargetPlatform.WebAssembly => "program.wat",
                TargetPlatform.JavaScript => "program.js",
                _ => "program.txt"
            };
        }
    }

    /// <summary>
    /// Interface for target-specific code generators
    /// </summary>
    public interface ITargetGenerator
    {
        Task<TargetGenerationResult> GenerateAsync(Program program, TargetConfiguration config);
        string GetTargetName();
        List<string> GetSupportedFeatures();
        TargetCapabilities GetCapabilities();
    }

    /// <summary>
    /// Target platform enumeration
    /// </summary>
    public enum TargetPlatform
    {
        CSharp,
        Java,
        Native,
        WebAssembly,
        JavaScript
    }

    /// <summary>
    /// Target configuration settings
    /// </summary>
    public class TargetConfiguration
    {
        public bool OptimizeForSize { get; set; } = false;
        public bool OptimizeForSpeed { get; set; } = true;
        public bool IncludeDebugInfo { get; set; } = true;
        public bool EnableRuntimeChecks { get; set; } = true;
        public string RuntimeVersion { get; set; } = "latest";
        public Dictionary<string, object> TargetSpecificOptions { get; set; } = new();
    }

    /// <summary>
    /// Target generation result
    /// </summary>
    public class TargetGenerationResult
    {
        public string SourceCode { get; set; } = "";
        public List<AdditionalFile> AdditionalFiles { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public Dictionary<string, string> BuildInstructions { get; set; } = new();
    }

    /// <summary>
    /// Additional file for target output
    /// </summary>
    public class AdditionalFile
    {
        public string Name { get; set; } = "";
        public string Content { get; set; } = "";
        public string Type { get; set; } = "";
    }

    /// <summary>
    /// Compilation result
    /// </summary>
    public class CompilationResult
    {
        public bool IsSuccess { get; private set; }
        public string? Error { get; private set; }
        public string? OutputPath { get; private set; }
        public TargetPlatform? Target { get; private set; }
        public TargetGenerationResult? GenerationResult { get; private set; }

        private CompilationResult() { }

        public static CompilationResult Success(string outputPath, TargetPlatform target, TargetGenerationResult result)
        {
            return new CompilationResult
            {
                IsSuccess = true,
                OutputPath = outputPath,
                Target = target,
                GenerationResult = result
            };
        }

        public static CompilationResult Failed(string error)
        {
            return new CompilationResult
            {
                IsSuccess = false,
                Error = error
            };
        }
    }

    /// <summary>
    /// Multi-target compilation result
    /// </summary>
    public class MultiTargetResult
    {
        public Dictionary<TargetPlatform, CompilationResult> Results { get; set; } = new();
        public bool OverallSuccess { get; set; }
        public int SuccessCount { get; set; }
        public int TotalCount { get; set; }

        public List<TargetPlatform> SuccessfulTargets => Results
            .Where(kvp => kvp.Value.IsSuccess)
            .Select(kvp => kvp.Key)
            .ToList();

        public List<TargetPlatform> FailedTargets => Results
            .Where(kvp => !kvp.Value.IsSuccess)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    /// <summary>
    /// Target capabilities
    /// </summary>
    public class TargetCapabilities
    {
        public bool SupportsAsync { get; set; }
        public bool SupportsParallelism { get; set; }
        public bool SupportsGarbageCollection { get; set; }
        public bool SupportsReflection { get; set; }
        public bool SupportsExceptions { get; set; }
        public bool SupportsInterop { get; set; }
        public List<string> SupportedEffects { get; set; } = new();
    }

    /// <summary>
    /// Base target options
    /// </summary>
    public class TargetOptions
    {
        public virtual Dictionary<string, object> GetOptions() => new();
    }

    /// <summary>
    /// C# target options
    /// </summary>
    public class CSharpTargetOptions : TargetOptions
    {
        public string TargetFramework { get; set; } = "net8.0";
        public bool UseNullableReferenceTypes { get; set; } = true;
        public bool GenerateXmlDocumentation { get; set; } = true;
        public CSharpLanguageVersion LanguageVersion { get; set; } = CSharpLanguageVersion.Latest;

        public override Dictionary<string, object> GetOptions()
        {
            return new Dictionary<string, object>
            {
                ["TargetFramework"] = TargetFramework,
                ["UseNullableReferenceTypes"] = UseNullableReferenceTypes,
                ["GenerateXmlDocumentation"] = GenerateXmlDocumentation,
                ["LanguageVersion"] = LanguageVersion
            };
        }
    }

    /// <summary>
    /// Java target options
    /// </summary>
    public class JavaTargetOptions : TargetOptions
    {
        public string JavaVersion { get; set; } = "17";
        public bool UseRecords { get; set; } = true;
        public bool GenerateJavadoc { get; set; } = true;
        public PackageStructure PackageStructure { get; set; } = PackageStructure.Default;

        public override Dictionary<string, object> GetOptions()
        {
            return new Dictionary<string, object>
            {
                ["JavaVersion"] = JavaVersion,
                ["UseRecords"] = UseRecords,
                ["GenerateJavadoc"] = GenerateJavadoc,
                ["PackageStructure"] = PackageStructure
            };
        }
    }

    /// <summary>
    /// Native target options
    /// </summary>
    public class NativeTargetOptions : TargetOptions
    {
        public NativeRuntime Runtime { get; set; } = NativeRuntime.LLVM;
        public OptimizationLevel OptimizationLevel { get; set; } = OptimizationLevel.O2;
        public bool IncludeGarbageCollector { get; set; } = true;
        public string TargetArchitecture { get; set; } = "x64";

        public override Dictionary<string, object> GetOptions()
        {
            return new Dictionary<string, object>
            {
                ["Runtime"] = Runtime,
                ["OptimizationLevel"] = OptimizationLevel,
                ["IncludeGarbageCollector"] = IncludeGarbageCollector,
                ["TargetArchitecture"] = TargetArchitecture
            };
        }
    }

    /// <summary>
    /// WebAssembly target options
    /// </summary>
    public class WebAssemblyTargetOptions : TargetOptions
    {
        public WasmFormat Format { get; set; } = WasmFormat.Wat;
        public bool OptimizeForSize { get; set; } = true;
        public bool EnableSIMD { get; set; } = false;
        public bool EnableThreads { get; set; } = false;

        public override Dictionary<string, object> GetOptions()
        {
            return new Dictionary<string, object>
            {
                ["Format"] = Format,
                ["OptimizeForSize"] = OptimizeForSize,
                ["EnableSIMD"] = EnableSIMD,
                ["EnableThreads"] = EnableThreads
            };
        }
    }

    /// <summary>
    /// JavaScript target options
    /// </summary>
    public class JavaScriptTargetOptions : TargetOptions
    {
        public ECMAScriptVersion ECMAVersion { get; set; } = ECMAScriptVersion.ES2022;
        public bool UseTypeScript { get; set; } = false;
        public bool GenerateSourceMaps { get; set; } = true;
        public ModuleSystem ModuleSystem { get; set; } = ModuleSystem.ESModules;

        public override Dictionary<string, object> GetOptions()
        {
            return new Dictionary<string, object>
            {
                ["ECMAVersion"] = ECMAVersion,
                ["UseTypeScript"] = UseTypeScript,
                ["GenerateSourceMaps"] = GenerateSourceMaps,
                ["ModuleSystem"] = ModuleSystem
            };
        }
    }

    // Supporting enums
    public enum CSharpLanguageVersion { Latest, CSharp11, CSharp10, CSharp9 }
    public enum PackageStructure { Default, Flat, Hierarchical }
    public enum NativeRuntime { LLVM, GCC, MSVC }
    public enum OptimizationLevel { O0, O1, O2, O3 }
    public enum WasmFormat { Wat, Wasm }
    public enum ECMAScriptVersion { ES2018, ES2019, ES2020, ES2021, ES2022 }
    public enum ModuleSystem { CommonJS, ESModules, UMD }
}