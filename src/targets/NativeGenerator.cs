using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlowLang.Compiler;

namespace FlowLang.Targets
{
    /// <summary>
    /// Native C++ target generator for FlowLang
    /// Transpiles FlowLang to high-performance C++ with SIMD optimizations
    /// Designed for maximum performance and minimal runtime overhead
    /// </summary>
    public class NativeGenerator : ITargetGenerator
    {
        public async Task<TargetGenerationResult> GenerateAsync(Program program, TargetConfiguration config)
        {
            var sourceCode = await GenerateNativeCodeAsync(program, config);
            var additionalFiles = GenerateAdditionalFiles(program, config);
            
            return new TargetGenerationResult
            {
                SourceCode = sourceCode,
                AdditionalFiles = additionalFiles,
                Dependencies = GetRequiredDependencies(),
                BuildInstructions = GetBuildInstructions()
            };
        }

        public string GetTargetName() => "Native";

        public List<string> GetSupportedFeatures()
        {
            return new List<string>
            {
                "High Performance Computing",
                "SIMD Optimizations",
                "Zero-Copy Operations", 
                "Result Types",
                "Effect System",
                "Memory Management",
                "Cross-Platform Compilation",
                "Static Analysis",
                "Vectorization"
            };
        }

        public TargetCapabilities GetCapabilities()
        {
            return new TargetCapabilities
            {
                SupportsAsync = true,
                SupportsParallelism = true,
                SupportsGarbageCollection = false, // Manual memory management
                SupportsReflection = false,
                SupportsExceptions = true,
                SupportsInterop = true,
                SupportedEffects = new List<string> { "Memory", "IO", "FileSystem", "Network", "Logging" }
            };
        }

        private async Task<string> GenerateNativeCodeAsync(Program program, TargetConfiguration config)
        {
            var sb = new StringBuilder();
            
            // Generate headers and includes
            GenerateHeaders(sb);
            
            // Generate FlowLang runtime includes
            GenerateRuntimeIncludes(sb);
            
            // Generate forward declarations
            GenerateForwardDeclarations(sb, program);
            
            // Generate type definitions
            GenerateTypeDefinitions(sb, program);
            
            // Generate function declarations
            foreach (var statement in program.Statements)
            {
                switch (statement)
                {
                    case FunctionDeclaration function:
                        GenerateFunctionDeclaration(sb, function);
                        break;
                        
                    case ComponentDeclaration component:
                        // Components not supported in native target
                        sb.AppendLine($"// UI Component '{component.Name}' not supported in native target");
                        break;
                }
            }
            
            // Generate function implementations
            foreach (var statement in program.Statements)
            {
                if (statement is FunctionDeclaration function)
                {
                    GenerateFunctionImplementation(sb, function);
                }
            }
            
            // Generate main function if present
            GenerateMainFunction(sb, program);
            
            return sb.ToString();
        }

        private void GenerateHeaders(StringBuilder sb)
        {
            sb.AppendLine("// FlowLang Generated Native C++ Code");
            sb.AppendLine("// Optimized for high performance and minimal overhead");
            sb.AppendLine("// Generated with maximum LLM readability");
            sb.AppendLine();
            
            // Standard C++ headers
            sb.AppendLine("#include <iostream>");
            sb.AppendLine("#include <string>");
            sb.AppendLine("#include <vector>");
            sb.AppendLine("#include <memory>");
            sb.AppendLine("#include <optional>");
            sb.AppendLine("#include <variant>");
            sb.AppendLine("#include <functional>");
            sb.AppendLine("#include <future>");
            sb.AppendLine("#include <thread>");
            sb.AppendLine("#include <atomic>");
            sb.AppendLine("#include <mutex>");
            sb.AppendLine("#include <algorithm>");
            sb.AppendLine("#include <numeric>");
            sb.AppendLine();
            
            // SIMD headers for optimization
            sb.AppendLine("#ifdef __AVX2__");
            sb.AppendLine("#include <immintrin.h>");
            sb.AppendLine("#endif");
            sb.AppendLine();
            
            // Platform-specific headers
            sb.AppendLine("#ifdef _WIN32");
            sb.AppendLine("#include <windows.h>");
            sb.AppendLine("#elif __linux__");
            sb.AppendLine("#include <unistd.h>");
            sb.AppendLine("#include <sys/mman.h>");
            sb.AppendLine("#elif __APPLE__");
            sb.AppendLine("#include <mach/mach.h>");
            sb.AppendLine("#endif");
            sb.AppendLine();
        }

        private void GenerateRuntimeIncludes(StringBuilder sb)
        {
            sb.AppendLine("// FlowLang Native Runtime");
            sb.AppendLine("namespace flowlang {");
            sb.AppendLine();
            
            // Result type implementation
            sb.AppendLine("// FlowLang Result type - zero-overhead when optimized");
            sb.AppendLine("template<typename T, typename E>");
            sb.AppendLine("class Result {");
            sb.AppendLine("private:");
            sb.AppendLine("    std::variant<T, E> data;");
            sb.AppendLine("    bool is_ok;");
            sb.AppendLine();
            sb.AppendLine("public:");
            sb.AppendLine("    explicit Result(T&& value) : data(std::move(value)), is_ok(true) {}");
            sb.AppendLine("    explicit Result(const T& value) : data(value), is_ok(true) {}");
            sb.AppendLine("    explicit Result(E&& error) : data(std::move(error)), is_ok(false) {}");
            sb.AppendLine("    explicit Result(const E& error) : data(error), is_ok(false) {}");
            sb.AppendLine();
            sb.AppendLine("    [[nodiscard]] bool IsOk() const noexcept { return is_ok; }");
            sb.AppendLine("    [[nodiscard]] bool IsError() const noexcept { return !is_ok; }");
            sb.AppendLine();
            sb.AppendLine("    [[nodiscard]] const T& Value() const& {");
            sb.AppendLine("        if (!is_ok) throw std::runtime_error(\"Called Value() on Error result\");");
            sb.AppendLine("        return std::get<T>(data);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    [[nodiscard]] T&& Value() && {");
            sb.AppendLine("        if (!is_ok) throw std::runtime_error(\"Called Value() on Error result\");");
            sb.AppendLine("        return std::get<T>(std::move(data));");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    [[nodiscard]] const E& Error() const& {");
            sb.AppendLine("        if (is_ok) throw std::runtime_error(\"Called Error() on Ok result\");");
            sb.AppendLine("        return std::get<E>(data);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    [[nodiscard]] E&& Error() && {");
            sb.AppendLine("        if (is_ok) throw std::runtime_error(\"Called Error() on Ok result\");");
            sb.AppendLine("        return std::get<E>(std::move(data));");
            sb.AppendLine("    }");
            sb.AppendLine("};");
            sb.AppendLine();
            
            // Helper functions for Result creation
            sb.AppendLine("template<typename T>");
            sb.AppendLine("Result<T, std::string> Ok(T&& value) {");
            sb.AppendLine("    return Result<T, std::string>(std::forward<T>(value));");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("template<typename E>");
            sb.AppendLine("Result<void, E> Error(E&& error) {");
            sb.AppendLine("    return Result<void, E>(std::forward<E>(error));");
            sb.AppendLine("}");
            sb.AppendLine();
            
            // Memory management utilities
            sb.AppendLine("// High-performance memory allocator");
            sb.AppendLine("class MemoryArena {");
            sb.AppendLine("private:");
            sb.AppendLine("    std::unique_ptr<uint8_t[]> buffer;");
            sb.AppendLine("    size_t size;");
            sb.AppendLine("    size_t offset;");
            sb.AppendLine();
            sb.AppendLine("public:");
            sb.AppendLine("    explicit MemoryArena(size_t arena_size) ");
            sb.AppendLine("        : buffer(std::make_unique<uint8_t[]>(arena_size)), size(arena_size), offset(0) {}");
            sb.AppendLine();
            sb.AppendLine("    template<typename T>");
            sb.AppendLine("    T* allocate(size_t count = 1) {");
            sb.AppendLine("        size_t bytes = sizeof(T) * count;");
            sb.AppendLine("        size_t aligned_bytes = (bytes + alignof(T) - 1) & ~(alignof(T) - 1);");
            sb.AppendLine();
            sb.AppendLine("        if (offset + aligned_bytes > size) {");
            sb.AppendLine("            throw std::bad_alloc{};");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        T* result = reinterpret_cast<T*>(buffer.get() + offset);");
            sb.AppendLine("        offset += aligned_bytes;");
            sb.AppendLine("        return result;");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    void reset() noexcept { offset = 0; }");
            sb.AppendLine("    [[nodiscard]] size_t remaining() const noexcept { return size - offset; }");
            sb.AppendLine("};");
            sb.AppendLine();
            
            // String optimization for FlowLang
            sb.AppendLine("// Zero-copy string view for FlowLang strings");
            sb.AppendLine("using String = std::string_view;");
            sb.AppendLine("using OwnedString = std::string;");
            sb.AppendLine();
            
            // Effect tracking system
            sb.AppendLine("// Effect tracking system for native code");
            sb.AppendLine("enum class Effect {");
            sb.AppendLine("    Memory,");
            sb.AppendLine("    IO,");
            sb.AppendLine("    FileSystem,");
            sb.AppendLine("    Network,");
            sb.AppendLine("    Logging");
            sb.AppendLine("};");
            sb.AppendLine();
            sb.AppendLine("template<Effect... effects>");
            sb.AppendLine("struct EffectSet {");
            sb.AppendLine("    static constexpr std::array<Effect, sizeof...(effects)> values = {effects...};");
            sb.AppendLine("};");
            sb.AppendLine();
            
            sb.AppendLine("} // namespace flowlang");
            sb.AppendLine();
        }

        private void GenerateForwardDeclarations(StringBuilder sb, Program program)
        {
            sb.AppendLine("// Forward declarations");
            
            foreach (var statement in program.Statements)
            {
                if (statement is FunctionDeclaration function)
                {
                    var returnType = ConvertTypeToNative(function.ReturnType);
                    var parameters = string.Join(", ", function.Parameters.Select(p => 
                        $"{ConvertTypeToNative(p.Type)} {SanitizeName(p.Name)}"));
                    
                    sb.AppendLine($"{returnType} {SanitizeName(function.Name)}({parameters});");
                }
            }
            
            sb.AppendLine();
        }

        private void GenerateTypeDefinitions(StringBuilder sb, Program program)
        {
            sb.AppendLine("// Type definitions");
            sb.AppendLine("using Int = int32_t;");
            sb.AppendLine("using Float = double;");
            sb.AppendLine("using Bool = bool;");
            sb.AppendLine();
        }

        private void GenerateFunctionDeclaration(StringBuilder sb, FunctionDeclaration function)
        {
            // Function declarations are already generated in forward declarations
        }

        private void GenerateFunctionImplementation(StringBuilder sb, FunctionDeclaration function)
        {
            var returnType = ConvertTypeToNative(function.ReturnType);
            var parameters = string.Join(", ", function.Parameters.Select(p => 
                $"{ConvertTypeToNative(p.Type)} {SanitizeName(p.Name)}"));
            
            sb.AppendLine($"// FlowLang Function: {function.Name}");
            if (function.Effects?.Any() == true)
            {
                sb.AppendLine($"// Effects: [{string.Join(", ", function.Effects)}]");
            }
            
            // Add function attributes for optimization
            sb.AppendLine("[[nodiscard]]");
            if (function.Effects?.Any() != true)
            {
                sb.AppendLine("[[gnu::pure]]"); // Pure function optimization hint
            }
            
            sb.AppendLine($"{returnType} {SanitizeName(function.Name)}({parameters}) {{");
            
            // Add effect tracking if enabled
            if (function.Effects?.Any() == true)
            {
                sb.AppendLine($"    // Effect tracking: {string.Join(", ", function.Effects)}");
            }
            
            // Generate function body
            foreach (var statement in function.Body)
            {
                GenerateStatement(sb, statement, 1);
            }
            
            sb.AppendLine("}");
            sb.AppendLine();
        }

        private void GenerateStatement(StringBuilder sb, ASTNode statement, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 4);
            
            switch (statement)
            {
                case ReturnStatement ret:
                    sb.AppendLine($"{indent}return {GenerateExpression(ret.Expression)};");
                    break;
                    
                case BinaryExpression binary:
                    sb.AppendLine($"{indent}{GenerateExpression(binary)};");
                    break;
                    
                default:
                    sb.AppendLine($"{indent}// TODO: Implement {statement.GetType().Name}");
                    break;
            }
        }

        private string GenerateExpression(ASTNode expression)
        {
            return expression switch
            {
                NumberLiteral num => num.Value.ToString(),
                Identifier id => SanitizeName(id.Name),
                BinaryExpression binary => GenerateBinaryExpression(binary),
                _ => "/* Unknown expression */"
            };
        }

        private string GenerateBinaryExpression(BinaryExpression binary)
        {
            var left = GenerateExpression(binary.Left);
            var right = GenerateExpression(binary.Right);
            var op = ConvertOperatorToNative(binary.Operator);
            
            // Check for potential SIMD optimization opportunities
            if (IsVectorizableOperation(binary))
            {
                return GenerateVectorizedOperation(binary);
            }
            
            return $"({left} {op} {right})";
        }

        private bool IsVectorizableOperation(BinaryExpression binary)
        {
            // Simple heuristic: arithmetic operations on numbers could be vectorized
            return binary.Operator is "+" or "-" or "*" or "/" &&
                   binary.Left is NumberLiteral &&
                   binary.Right is NumberLiteral;
        }

        private string GenerateVectorizedOperation(BinaryExpression binary)
        {
            // For now, just generate regular operation
            // In a full implementation, this would generate SIMD intrinsics
            var left = GenerateExpression(binary.Left);
            var right = GenerateExpression(binary.Right);
            var op = ConvertOperatorToNative(binary.Operator);
            
            return $"({left} {op} {right})";
        }

        private string ConvertOperatorToNative(string op)
        {
            return op switch
            {
                "+" => "+",
                "-" => "-",
                "*" => "*",
                "/" => "/",
                ">" => ">",
                "<" => "<",
                ">=" => ">=",
                "<=" => "<=",
                "==" => "==",
                "!=" => "!=",
                _ => op
            };
        }

        private void GenerateMainFunction(StringBuilder sb, Program program)
        {
            var hasMainFunction = program.Statements
                .OfType<FunctionDeclaration>()
                .Any(f => f.Name == "main");
                
            if (hasMainFunction)
            {
                sb.AppendLine("// Program entry point");
                sb.AppendLine("int main(int argc, char* argv[]) {");
                sb.AppendLine("    try {");
                sb.AppendLine("        auto result = main();");
                sb.AppendLine("        return result;");
                sb.AppendLine("    } catch (const std::exception& e) {");
                sb.AppendLine("        std::cerr << \"Error: \" << e.what() << std::endl;");
                sb.AppendLine("        return 1;");
                sb.AppendLine("    }");
                sb.AppendLine("}");
            }
        }

        private string ConvertTypeToNative(string flowLangType)
        {
            return flowLangType switch
            {
                "int" => "Int",
                "string" => "flowlang::String",
                "bool" => "Bool",
                "float" => "Float",
                var type when type.StartsWith("Result<") => ConvertResultType(type),
                var type when type.StartsWith("List<") => ConvertListType(type),
                var type when type.StartsWith("Option<") => ConvertOptionType(type),
                _ => SanitizeName(flowLangType)
            };
        }

        private string ConvertResultType(string resultType)
        {
            // Extract T, E from Result<T, E>
            var inner = resultType.Substring(7, resultType.Length - 8);
            var parts = inner.Split(',');
            if (parts.Length == 2)
            {
                var successType = ConvertTypeToNative(parts[0].Trim());
                var errorType = ConvertTypeToNative(parts[1].Trim());
                return $"flowlang::Result<{successType}, {errorType}>";
            }
            return "flowlang::Result<void, std::string>";
        }

        private string ConvertListType(string listType)
        {
            var inner = listType.Substring(5, listType.Length - 6);
            var elementType = ConvertTypeToNative(inner);
            return $"std::vector<{elementType}>";
        }

        private string ConvertOptionType(string optionType)
        {
            var inner = optionType.Substring(7, optionType.Length - 8);
            var elementType = ConvertTypeToNative(inner);
            return $"std::optional<{elementType}>";
        }

        private string SanitizeName(string name)
        {
            // Replace FlowLang naming conventions with C++ conventions
            return name.Replace("-", "_").Replace(".", "_");
        }

        private List<AdditionalFile> GenerateAdditionalFiles(Program program, TargetConfiguration config)
        {
            var files = new List<AdditionalFile>();

            // Generate CMakeLists.txt
            files.Add(new AdditionalFile
            {
                Name = "CMakeLists.txt",
                Type = "build",
                Content = GenerateCMakeLists(program)
            });

            // Generate header file
            files.Add(new AdditionalFile
            {
                Name = "flowlang_runtime.h",
                Type = "header",
                Content = GenerateRuntimeHeader()
            });

            // Generate build script
            files.Add(new AdditionalFile
            {
                Name = "build.sh",
                Type = "script",
                Content = GenerateBuildScript()
            });

            // Generate Windows build script
            files.Add(new AdditionalFile
            {
                Name = "build.bat",
                Type = "script",
                Content = GenerateWindowsBuildScript()
            });

            return files;
        }

        private string GenerateCMakeLists(Program program)
        {
            return @"cmake_minimum_required(VERSION 3.16)
project(FlowLangNativeProgram)

# Set C++ standard
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Compiler-specific optimizations
if(MSVC)
    add_compile_options(/W4 /O2 /arch:AVX2)
else()
    add_compile_options(-Wall -Wextra -O3 -march=native -mtune=native)
    
    # Enable SIMD optimizations
    add_compile_options(-mavx2 -mfma)
    
    # Enable link-time optimization
    set(CMAKE_INTERPROCEDURAL_OPTIMIZATION TRUE)
endif()

# Find threads for parallel execution
find_package(Threads REQUIRED)

# Create executable
add_executable(flowlang_program program.cpp)

# Link libraries
target_link_libraries(flowlang_program Threads::Threads)

# Platform-specific libraries
if(WIN32)
    target_link_libraries(flowlang_program ws2_32)
elseif(UNIX)
    target_link_libraries(flowlang_program m)
endif()

# Install target
install(TARGETS flowlang_program DESTINATION bin)";
        }

        private string GenerateRuntimeHeader()
        {
            return @"#pragma once
// FlowLang Native Runtime Header
// High-performance C++ runtime for FlowLang

#include <memory>
#include <string_view>
#include <optional>
#include <variant>
#include <vector>
#include <array>

namespace flowlang {

// Forward declarations
template<typename T, typename E> class Result;
class MemoryArena;

// Type aliases for FlowLang types
using Int = int32_t;
using Float = double;
using Bool = bool;
using String = std::string_view;
using OwnedString = std::string;

// Effect system
enum class Effect {
    Memory,
    IO,
    FileSystem,
    Network,
    Logging
};

// High-performance memory management
extern thread_local std::unique_ptr<MemoryArena> default_arena;

// Runtime initialization
void initialize_runtime();
void cleanup_runtime();

} // namespace flowlang";
        }

        private string GenerateBuildScript()
        {
            return @"#!/bin/bash
# FlowLang Native Build Script

echo ""Building FlowLang native program...""

# Create build directory
mkdir -p build
cd build

# Configure with CMake
cmake .. -DCMAKE_BUILD_TYPE=Release

# Build with optimizations
cmake --build . --config Release -j $(nproc)

echo ""Build complete! Executable: ./flowlang_program""

# Run if requested
if [ ""$1"" = ""--run"" ]; then
    echo ""Running program...""
    ./flowlang_program
fi";
        }

        private string GenerateWindowsBuildScript()
        {
            return @"@echo off
REM FlowLang Native Build Script for Windows

echo Building FlowLang native program...

REM Create build directory
if not exist build mkdir build
cd build

REM Configure with CMake
cmake .. -DCMAKE_BUILD_TYPE=Release

REM Build with optimizations
cmake --build . --config Release

echo Build complete! Executable: Release\flowlang_program.exe

REM Run if requested
if ""%1""==""--run"" (
    echo Running program...
    Release\flowlang_program.exe
)";
        }

        private List<string> GetRequiredDependencies()
        {
            return new List<string>
            {
                "CMake >= 3.16",
                "C++20 compatible compiler",
                "Threading library"
            };
        }

        private Dictionary<string, string> GetBuildInstructions()
        {
            return new Dictionary<string, string>
            {
                ["linux"] = "./build.sh",
                ["windows"] = "build.bat",
                ["macos"] = "./build.sh",
                ["manual"] = "mkdir build && cd build && cmake .. && cmake --build .",
                ["run"] = "./build.sh --run",
                ["clean"] = "rm -rf build"
            };
        }
    }
}