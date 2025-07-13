using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlowLang.Compiler;

namespace FlowLang.Targets
{
    /// <summary>
    /// Java target generator for FlowLang
    /// Transpiles FlowLang to Java with JVM compatibility
    /// </summary>
    public class JavaGenerator : ITargetGenerator
    {
        public async Task<TargetGenerationResult> GenerateAsync(Program program, TargetConfiguration config)
        {
            var sourceCode = await GenerateJavaCodeAsync(program, config);
            var additionalFiles = GenerateAdditionalFiles(program, config);
            
            return new TargetGenerationResult
            {
                SourceCode = sourceCode,
                AdditionalFiles = additionalFiles,
                Dependencies = GetRequiredDependencies(),
                BuildInstructions = GetBuildInstructions()
            };
        }

        public string GetTargetName() => "Java";

        public List<string> GetSupportedFeatures()
        {
            return new List<string>
            {
                "Result Types",
                "Effect System", 
                "String Interpolation",
                "Guard Clauses",
                "Module System",
                "Async Operations",
                "Saga Patterns",
                "Observability"
            };
        }

        public TargetCapabilities GetCapabilities()
        {
            return new TargetCapabilities
            {
                SupportsAsync = true,
                SupportsParallelism = true,
                SupportsGarbageCollection = true,
                SupportsReflection = true,
                SupportsExceptions = true,
                SupportsInterop = true,
                SupportedEffects = new List<string> { "Database", "Network", "Logging", "FileSystem", "Memory", "IO" }
            };
        }

        private async Task<string> GenerateJavaCodeAsync(Program program, TargetConfiguration config)
        {
            var sb = new StringBuilder();
            
            // Java package and imports
            sb.AppendLine("package com.flowlang.generated;");
            sb.AppendLine();
            sb.AppendLine("import java.util.*;");
            sb.AppendLine("import java.util.concurrent.*;");
            sb.AppendLine("import java.util.function.*;");
            sb.AppendLine("import com.flowlang.runtime.*;");
            sb.AppendLine("import com.flowlang.types.*;");
            sb.AppendLine();

            // Generate main class
            sb.AppendLine("public class FlowLangProgram {");
            
            // Generate Result type implementation
            GenerateJavaResultType(sb);
            
            // Generate functions
            foreach (var statement in program.Statements)
            {
                if (statement is FunctionDeclaration func)
                {
                    GenerateJavaFunction(sb, func);
                }
            }
            
            sb.AppendLine("}");
            
            return sb.ToString();
        }

        private void GenerateJavaResultType(StringBuilder sb)
        {
            sb.AppendLine("    // FlowLang Result type implementation");
            sb.AppendLine("    public static abstract class Result<T, E> {");
            sb.AppendLine("        public abstract boolean isOk();");
            sb.AppendLine("        public abstract boolean isError();");
            sb.AppendLine("        public abstract T getValue();");
            sb.AppendLine("        public abstract E getError();");
            sb.AppendLine();
            sb.AppendLine("        public static <T, E> Result<T, E> ok(T value) {");
            sb.AppendLine("            return new Ok<>(value);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public static <T, E> Result<T, E> error(E error) {");
            sb.AppendLine("            return new Error<>(error);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public static class Ok<T, E> extends Result<T, E> {");
            sb.AppendLine("            private final T value;");
            sb.AppendLine("            public Ok(T value) { this.value = value; }");
            sb.AppendLine("            public boolean isOk() { return true; }");
            sb.AppendLine("            public boolean isError() { return false; }");
            sb.AppendLine("            public T getValue() { return value; }");
            sb.AppendLine("            public E getError() { throw new RuntimeException(\"Called getError on Ok result\"); }");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public static class Error<T, E> extends Result<T, E> {");
            sb.AppendLine("            private final E error;");
            sb.AppendLine("            public Error(E error) { this.error = error; }");
            sb.AppendLine("            public boolean isOk() { return false; }");
            sb.AppendLine("            public boolean isError() { return true; }");
            sb.AppendLine("            public T getValue() { throw new RuntimeException(\"Called getValue on Error result\"); }");
            sb.AppendLine("            public E getError() { return error; }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        private void GenerateJavaFunction(StringBuilder sb, FunctionDeclaration function)
        {
            // Generate Java method signature
            var returnType = ConvertTypeToJava(function.ReturnType);
            var parameters = string.Join(", ", function.Parameters.Select(p => 
                $"{ConvertTypeToJava(p.Type)} {p.Name}"));
            
            // Add observability annotation if function has effects
            if (function.Effects?.Any() == true)
            {
                sb.AppendLine($"    @Observed");
                sb.AppendLine($"    @Effects({{{string.Join(", ", function.Effects.Select(e => $"\"{e}\""))}}})");
            }
            
            sb.AppendLine($"    public static {returnType} {function.Name}({parameters}) {{");
            
            // Add effect tracking
            if (function.Effects?.Any() == true)
            {
                sb.AppendLine($"        ObservabilityRuntime.recordFunctionEntry(\"{function.Name}\", Arrays.asList({string.Join(", ", function.Effects.Select(e => $"\"{e}\""))}));");
            }
            
            // Generate function body
            foreach (var statement in function.Body)
            {
                GenerateJavaStatement(sb, statement, 2);
            }
            
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        private void GenerateJavaStatement(StringBuilder sb, ASTNode statement, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 4);
            
            switch (statement)
            {
                case ReturnStatement ret:
                    sb.AppendLine($"{indent}return {GenerateJavaExpression(ret.Expression)};");
                    break;
                    
                case BinaryExpression binary:
                    sb.AppendLine($"{indent}{GenerateJavaExpression(binary)};");
                    break;
                    
                default:
                    sb.AppendLine($"{indent}// TODO: Implement {statement.GetType().Name}");
                    break;
            }
        }

        private string GenerateJavaExpression(ASTNode expression)
        {
            return expression switch
            {
                NumberLiteral num => num.Value.ToString(),
                Identifier id => id.Name,
                BinaryExpression binary => $"({GenerateJavaExpression(binary.Left)} {ConvertOperatorToJava(binary.Operator)} {GenerateJavaExpression(binary.Right)})",
                _ => "/* Unknown expression */"
            };
        }

        private string ConvertTypeToJava(string flowLangType)
        {
            return flowLangType switch
            {
                "int" => "int",
                "string" => "String",
                "bool" => "boolean",
                var type when type.StartsWith("Result<") => $"Result<{ExtractResultTypes(type)}>",
                _ => flowLangType
            };
        }

        private string ConvertOperatorToJava(string op)
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

        private string ExtractResultTypes(string resultType)
        {
            // Extract T, E from Result<T, E>
            var inner = resultType.Substring(7, resultType.Length - 8); // Remove "Result<" and ">"
            var parts = inner.Split(',');
            if (parts.Length == 2)
            {
                return $"{ConvertTypeToJava(parts[0].Trim())}, {ConvertTypeToJava(parts[1].Trim())}";
            }
            return "Object, String";
        }

        private List<AdditionalFile> GenerateAdditionalFiles(Program program, TargetConfiguration config)
        {
            var files = new List<AdditionalFile>();

            // Generate Maven pom.xml
            files.Add(new AdditionalFile
            {
                Name = "pom.xml",
                Type = "build",
                Content = GenerateMavenPom()
            });

            // Generate FlowLang runtime dependencies
            files.Add(new AdditionalFile
            {
                Name = "FlowLangRuntime.java",
                Type = "runtime",
                Content = GenerateJavaRuntime()
            });

            return files;
        }

        private string GenerateMavenPom()
        {
            return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<project xmlns=""http://maven.apache.org/POM/4.0.0""
         xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
         xsi:schemaLocation=""http://maven.apache.org/POM/4.0.0 
         http://maven.apache.org/xsd/maven-4.0.0.xsd"">
    <modelVersion>4.0.0</modelVersion>
    
    <groupId>com.flowlang</groupId>
    <artifactId>generated-program</artifactId>
    <version>1.0.0</version>
    <packaging>jar</packaging>
    
    <properties>
        <maven.compiler.source>17</maven.compiler.source>
        <maven.compiler.target>17</maven.compiler.target>
        <project.build.sourceEncoding>UTF-8</project.build.sourceEncoding>
    </properties>
    
    <dependencies>
        <dependency>
            <groupId>com.flowlang</groupId>
            <artifactId>flowlang-runtime</artifactId>
            <version>1.0.0</version>
        </dependency>
    </dependencies>
    
    <build>
        <plugins>
            <plugin>
                <groupId>org.apache.maven.plugins</groupId>
                <artifactId>maven-compiler-plugin</artifactId>
                <version>3.11.0</version>
                <configuration>
                    <source>17</source>
                    <target>17</target>
                </configuration>
            </plugin>
        </plugins>
    </build>
</project>";
        }

        private string GenerateJavaRuntime()
        {
            return @"package com.flowlang.runtime;

import java.util.List;
import java.util.concurrent.CompletableFuture;

public class ObservabilityRuntime {
    public static void recordFunctionEntry(String functionName, List<String> effects) {
        // Implementation for observability
        System.out.println(""[OBSERVABILITY] Function entry: "" + functionName + "" with effects: "" + effects);
    }
    
    public static void recordFunctionExit(String functionName, long durationMs) {
        System.out.println(""[OBSERVABILITY] Function exit: "" + functionName + "" in "" + durationMs + ""ms"");
    }
}

// Additional runtime classes would be generated here
";
        }

        private List<string> GetRequiredDependencies()
        {
            return new List<string>
            {
                "com.flowlang:flowlang-runtime:1.0.0",
                "org.slf4j:slf4j-api:2.0.7",
                "ch.qos.logback:logback-classic:1.4.7"
            };
        }

        private Dictionary<string, string> GetBuildInstructions()
        {
            return new Dictionary<string, string>
            {
                ["build"] = "mvn compile",
                ["test"] = "mvn test",
                ["package"] = "mvn package",
                ["run"] = "java -cp target/classes com.flowlang.generated.FlowLangProgram"
            };
        }
    }
}