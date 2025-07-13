using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlowLang.Compiler;

namespace FlowLang.Targets
{
    /// <summary>
    /// WebAssembly target generator for FlowLang
    /// Transpiles FlowLang to WebAssembly Text Format (WAT)
    /// </summary>
    public class WebAssemblyGenerator : ITargetGenerator
    {
        public async Task<TargetGenerationResult> GenerateAsync(Program program, TargetConfiguration config)
        {
            var sourceCode = await GenerateWatCodeAsync(program, config);
            var additionalFiles = GenerateAdditionalFiles(program, config);
            
            return new TargetGenerationResult
            {
                SourceCode = sourceCode,
                AdditionalFiles = additionalFiles,
                Dependencies = GetRequiredDependencies(),
                BuildInstructions = GetBuildInstructions()
            };
        }

        public string GetTargetName() => "WebAssembly";

        public List<string> GetSupportedFeatures()
        {
            return new List<string>
            {
                "Result Types (Limited)",
                "Basic Functions",
                "Integer Operations",
                "Memory Management",
                "Module System (Basic)"
            };
        }

        public TargetCapabilities GetCapabilities()
        {
            return new TargetCapabilities
            {
                SupportsAsync = false, // Limited in WASM
                SupportsParallelism = false, // Limited in WASM  
                SupportsGarbageCollection = false,
                SupportsReflection = false,
                SupportsExceptions = false, // Not in MVP
                SupportsInterop = true,
                SupportedEffects = new List<string> { "Memory", "IO" } // Limited effect support
            };
        }

        private async Task<string> GenerateWatCodeAsync(Program program, TargetConfiguration config)
        {
            var sb = new StringBuilder();
            
            // WAT module header
            sb.AppendLine("(module");
            
            // Import memory if needed
            sb.AppendLine("  (import \"env\" \"memory\" (memory 1))");
            
            // Export functions that should be accessible from JavaScript
            var exportedFunctions = new List<string>();
            
            // Generate functions
            foreach (var statement in program.Statements)
            {
                if (statement is FunctionDeclaration func)
                {
                    GenerateWatFunction(sb, func);
                    if (func.Name == "main" || ShouldExportFunction(func))
                    {
                        exportedFunctions.Add(func.Name);
                    }
                }
            }
            
            // Generate exports
            foreach (var funcName in exportedFunctions)
            {
                sb.AppendLine($"  (export \"{funcName}\" (func ${funcName}))");
            }
            
            sb.AppendLine(")");
            
            return sb.ToString();
        }

        private void GenerateWatFunction(StringBuilder sb, FunctionDeclaration function)
        {
            // Convert FlowLang types to WASM types
            var paramTypes = function.Parameters.Select(p => ConvertTypeToWasm(p.Type));
            var resultType = ConvertTypeToWasm(function.ReturnType);
            
            // Generate function signature
            sb.AppendLine($"  (func ${function.Name}");
            
            // Parameters
            foreach (var (param, type) in function.Parameters.Zip(paramTypes))
            {
                sb.AppendLine($"    (param ${param.Name} {type})");
            }
            
            // Result
            if (resultType != "")
            {
                sb.AppendLine($"    (result {resultType})");
            }
            
            // Local variables (would need more sophisticated analysis)
            sb.AppendLine("    (local $temp i32)");
            
            // Function body
            GenerateWatFunctionBody(sb, function.Body);
            
            sb.AppendLine("  )");
            sb.AppendLine();
        }

        private void GenerateWatFunctionBody(StringBuilder sb, List<ASTNode> body)
        {
            foreach (var statement in body)
            {
                switch (statement)
                {
                    case ReturnStatement ret:
                        GenerateWatExpression(sb, ret.Expression);
                        break;
                        
                    case BinaryExpression binary:
                        GenerateWatExpression(sb, binary);
                        break;
                        
                    default:
                        sb.AppendLine("    ;; TODO: Implement " + statement.GetType().Name);
                        break;
                }
            }
        }

        private void GenerateWatExpression(StringBuilder sb, ASTNode expression)
        {
            switch (expression)
            {
                case NumberLiteral num:
                    sb.AppendLine($"    i32.const {num.Value}");
                    break;
                    
                case Identifier id:
                    sb.AppendLine($"    local.get ${id.Name}");
                    break;
                    
                case BinaryExpression binary:
                    // Generate left operand
                    GenerateWatExpression(sb, binary.Left);
                    // Generate right operand  
                    GenerateWatExpression(sb, binary.Right);
                    // Generate operation
                    var wasmOp = ConvertOperatorToWasm(binary.Operator);
                    sb.AppendLine($"    {wasmOp}");
                    break;
                    
                default:
                    sb.AppendLine("    ;; Unknown expression");
                    sb.AppendLine("    i32.const 0");
                    break;
            }
        }

        private string ConvertTypeToWasm(string flowLangType)
        {
            return flowLangType switch
            {
                "int" => "i32",
                "bool" => "i32", // Booleans as integers in WASM
                "string" => "", // Strings need special handling in WASM
                var type when type.StartsWith("Result<") => "i32", // Results as status codes
                _ => "i32" // Default to i32
            };
        }

        private string ConvertOperatorToWasm(string op)
        {
            return op switch
            {
                "+" => "i32.add",
                "-" => "i32.sub", 
                "*" => "i32.mul",
                "/" => "i32.div_s",
                ">" => "i32.gt_s",
                "<" => "i32.lt_s",
                ">=" => "i32.ge_s",
                "<=" => "i32.le_s",
                "==" => "i32.eq",
                "!=" => "i32.ne",
                _ => "i32.add" // Default
            };
        }

        private bool ShouldExportFunction(FunctionDeclaration function)
        {
            // Export functions that don't have complex types
            return function.Parameters.All(p => IsSimpleType(p.Type)) && 
                   IsSimpleType(function.ReturnType);
        }

        private bool IsSimpleType(string type)
        {
            return type == "int" || type == "bool";
        }

        private List<AdditionalFile> GenerateAdditionalFiles(Program program, TargetConfiguration config)
        {
            var files = new List<AdditionalFile>();

            // Generate HTML wrapper for testing
            files.Add(new AdditionalFile
            {
                Name = "index.html",
                Type = "html",
                Content = GenerateHtmlWrapper()
            });

            // Generate JavaScript loader
            files.Add(new AdditionalFile
            {
                Name = "loader.js", 
                Type = "javascript",
                Content = GenerateJavaScriptLoader(program)
            });

            return files;
        }

        private string GenerateHtmlWrapper()
        {
            return @"<!DOCTYPE html>
<html>
<head>
    <title>FlowLang WebAssembly Program</title>
</head>
<body>
    <h1>FlowLang WebAssembly Program</h1>
    <div id=""output""></div>
    <script src=""loader.js""></script>
</body>
</html>";
        }

        private string GenerateJavaScriptLoader(Program program)
        {
            var exportedFunctions = program.Statements
                .OfType<FunctionDeclaration>()
                .Where(f => f.Name == "main" || ShouldExportFunction(f))
                .Select(f => f.Name);

            var sb = new StringBuilder();
            sb.AppendLine("// FlowLang WebAssembly Loader");
            sb.AppendLine("async function loadFlowLangModule() {");
            sb.AppendLine("    try {");
            sb.AppendLine("        const wasmModule = await WebAssembly.instantiateStreaming(");
            sb.AppendLine("            fetch('program.wasm'),");
            sb.AppendLine("            {");
            sb.AppendLine("                env: {");
            sb.AppendLine("                    memory: new WebAssembly.Memory({ initial: 1 })");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("        );");
            sb.AppendLine();
            sb.AppendLine("        const exports = wasmModule.instance.exports;");
            sb.AppendLine();
            
            foreach (var funcName in exportedFunctions)
            {
                sb.AppendLine($"        // Export {funcName} function");
                sb.AppendLine($"        window.flowlang_{funcName} = exports.{funcName};");
            }
            
            sb.AppendLine();
            sb.AppendLine("        // Run main function if it exists");
            sb.AppendLine("        if (exports.main) {");
            sb.AppendLine("            const result = exports.main();");
            sb.AppendLine("            document.getElementById('output').textContent = `Main function result: ${result}`;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        console.log('FlowLang WebAssembly module loaded successfully');");
            sb.AppendLine("    } catch (error) {");
            sb.AppendLine("        console.error('Failed to load FlowLang WebAssembly module:', error);");
            sb.AppendLine("        document.getElementById('output').textContent = `Error: ${error.message}`;");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("// Load the module when the page loads");
            sb.AppendLine("loadFlowLangModule();");

            return sb.ToString();
        }

        private List<string> GetRequiredDependencies()
        {
            return new List<string>
            {
                "wabt", // WebAssembly Binary Toolkit
                "wasm-pack" // For building
            };
        }

        private Dictionary<string, string> GetBuildInstructions()
        {
            return new Dictionary<string, string>
            {
                ["compile"] = "wat2wasm program.wat -o program.wasm",
                ["serve"] = "python3 -m http.server 8000",
                ["validate"] = "wasm-validate program.wasm",
                ["optimize"] = "wasm-opt -O3 program.wasm -o program-optimized.wasm"
            };
        }
    }
}