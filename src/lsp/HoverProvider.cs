using System;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace FlowLang.LSP
{
    /// <summary>
    /// Provides hover information for FlowLang identifiers
    /// Shows type information, effect annotations, function signatures, and documentation
    /// </summary>
    public class HoverProvider
    {
        /// <summary>
        /// Get hover information for the symbol at the specified position
        /// </summary>
        public Hover? GetHover(ManagedDocument document, Position position)
        {
            var token = document.GetTokenAtPosition(position);
            if (token == null) return null;

            var hoverContent = GetHoverContent(document, token, position);
            if (string.IsNullOrEmpty(hoverContent)) return null;

            return new Hover
            {
                Contents = new SumType<string, MarkedString, MarkedString[], MarkupContent>(
                    new MarkupContent
                    {
                        Kind = MarkupKind.Markdown,
                        Value = hoverContent
                    }),
                Range = TokenToRange(token)
            };
        }

        /// <summary>
        /// Generate hover content based on the token and context
        /// </summary>
        private string GetHoverContent(ManagedDocument document, Token token, Position position)
        {
            switch (token.Type)
            {
                case TokenType.Identifier:
                    return GetIdentifierHover(document, token);

                case TokenType.Function:
                    return GetKeywordHover("function", "Declares a function");

                case TokenType.Pure:
                    return GetKeywordHover("pure", "Modifier for functions with no side effects");

                case TokenType.Uses:
                    return GetKeywordHover("uses", "Declares side effects that a function may perform");

                case TokenType.Result:
                    return GetResultTypeHover();

                case TokenType.Ok:
                    return GetConstructorHover("Ok", "Creates a successful Result value");

                case TokenType.Error:
                    return GetConstructorHover("Error", "Creates an error Result value");

                case TokenType.Database:
                case TokenType.Network:
                case TokenType.Logging:
                case TokenType.FileSystem:
                case TokenType.Memory:
                case TokenType.IO:
                    return GetEffectHover(token.Value);

                case TokenType.Let:
                    return GetKeywordHover("let", "Declares a variable binding");

                case TokenType.If:
                    return GetKeywordHover("if", "Conditional execution");

                case TokenType.Else:
                    return GetKeywordHover("else", "Alternative branch for if statement");

                case TokenType.Guard:
                    return GetKeywordHover("guard", "Early return condition - executes else block if condition is false");

                case TokenType.Return:
                    return GetKeywordHover("return", "Returns a value from a function");

                case TokenType.Module:
                    return GetKeywordHover("module", "Declares a module for organizing code");

                case TokenType.Import:
                    return GetKeywordHover("import", "Imports functions or modules");

                case TokenType.Export:
                    return GetKeywordHover("export", "Exports functions from a module");

                case TokenType.Int:
                    return GetTypeHover("int", "32-bit signed integer");

                case TokenType.String_Type:
                    return GetTypeHover("string", "Unicode text string");

                case TokenType.Bool:
                    return GetTypeHover("bool", "Boolean value (true or false)");

                case TokenType.Question:
                    return GetOperatorHover("?", "Error propagation operator - returns early if Result is Error");

                case TokenType.Arrow:
                    return GetOperatorHover("->", "Function return type indicator");

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Get hover information for identifiers (variables, functions, modules)
        /// </summary>
        private string GetIdentifierHover(ManagedDocument document, Token token)
        {
            if (document.AST == null) return GetGenericIdentifierHover(token.Value);

            // Look for function declarations
            foreach (var statement in document.AST.Statements)
            {
                if (statement is FunctionDeclaration func && func.Name == token.Value)
                {
                    return GetFunctionHover(func);
                }
                else if (statement is ModuleDeclaration module)
                {
                    if (module.Name == token.Value)
                    {
                        return GetModuleHover(module);
                    }

                    // Check for functions within modules
                    foreach (var moduleStmt in module.Body)
                    {
                        if (moduleStmt is FunctionDeclaration moduleFunc && moduleFunc.Name == token.Value)
                        {
                            return GetFunctionHover(moduleFunc, module.Name);
                        }
                    }
                }
            }

            // Check if it's a parameter or local variable (simplified)
            var functionContext = FindContainingFunction(document, token);
            if (functionContext != null)
            {
                var parameter = functionContext.Parameters.FirstOrDefault(p => p.Name == token.Value);
                if (parameter != null)
                {
                    return GetParameterHover(parameter, functionContext);
                }
            }

            return GetGenericIdentifierHover(token.Value);
        }

        /// <summary>
        /// Find the function that contains the given token
        /// </summary>
        private FunctionDeclaration? FindContainingFunction(ManagedDocument document, Token token)
        {
            if (document.AST == null) return null;

            // Simplified implementation - in a full version, we'd track position ranges
            foreach (var statement in document.AST.Statements)
            {
                if (statement is FunctionDeclaration func)
                {
                    return func; // For now, assume we're in the first function found
                }
            }

            return null;
        }

        /// <summary>
        /// Generate hover content for functions
        /// </summary>
        private string GetFunctionHover(FunctionDeclaration func, string? moduleName = null)
        {
            var sb = new StringBuilder();

            // Function signature
            sb.AppendLine("```flowlang");
            if (func.IsPure)
            {
                sb.Append("pure ");
            }

            if (!string.IsNullOrEmpty(moduleName))
            {
                sb.Append($"{moduleName}.");
            }

            sb.Append($"function {func.Name}(");
            sb.Append(string.Join(", ", func.Parameters.Select(p => $"{p.Name}: {p.Type}")));
            sb.Append(")");

            if (func.Effects != null && func.Effects.Effects.Count > 0)
            {
                sb.Append($" uses [{string.Join(", ", func.Effects.Effects)}]");
            }

            sb.AppendLine($" -> {func.ReturnType}");
            sb.AppendLine("```");

            // Function characteristics
            if (func.IsPure)
            {
                sb.AppendLine("**Pure function** - No side effects");
            }
            else if (func.Effects != null && func.Effects.Effects.Count > 0)
            {
                sb.AppendLine("**Effects:**");
                foreach (var effect in func.Effects.Effects)
                {
                    sb.AppendLine($"- `{effect}`: {GetEffectDescription(effect)}");
                }
            }

            // Parameter information
            if (func.Parameters.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("**Parameters:**");
                foreach (var param in func.Parameters)
                {
                    sb.AppendLine($"- `{param.Name}`: {param.Type}");
                }
            }

            // Return type information
            sb.AppendLine();
            sb.AppendLine($"**Returns:** `{func.ReturnType}`");

            if (func.ReturnType.StartsWith("Result<"))
            {
                sb.AppendLine("Use the `?` operator for error propagation.");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate hover content for modules
        /// </summary>
        private string GetModuleHover(ModuleDeclaration module)
        {
            var sb = new StringBuilder();

            sb.AppendLine("```flowlang");
            sb.AppendLine($"module {module.Name}");
            sb.AppendLine("```");

            sb.AppendLine($"**Module:** {module.Name}");

            var functions = module.Body.OfType<FunctionDeclaration>().ToArray();
            if (functions.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("**Exported functions:**");
                foreach (var func in functions)
                {
                    sb.AppendLine($"- `{func.Name}({string.Join(", ", func.Parameters.Select(p => p.Type))})`");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate hover content for parameters
        /// </summary>
        private string GetParameterHover(Parameter parameter, FunctionDeclaration function)
        {
            var sb = new StringBuilder();

            sb.AppendLine("```flowlang");
            sb.AppendLine($"{parameter.Name}: {parameter.Type}");
            sb.AppendLine("```");

            sb.AppendLine($"**Parameter** of function `{function.Name}`");
            sb.AppendLine($"**Type:** `{parameter.Type}`");

            return sb.ToString();
        }

        /// <summary>
        /// Generate hover content for keywords
        /// </summary>
        private string GetKeywordHover(string keyword, string description)
        {
            return $"```flowlang\n{keyword}\n```\n\n**FlowLang keyword**\n\n{description}";
        }

        /// <summary>
        /// Generate hover content for types
        /// </summary>
        private string GetTypeHover(string type, string description)
        {
            return $"```flowlang\n{type}\n```\n\n**FlowLang type**\n\n{description}";
        }

        /// <summary>
        /// Generate hover content for operators
        /// </summary>
        private string GetOperatorHover(string op, string description)
        {
            return $"```flowlang\n{op}\n```\n\n**FlowLang operator**\n\n{description}";
        }

        /// <summary>
        /// Generate hover content for constructors
        /// </summary>
        private string GetConstructorHover(string constructor, string description)
        {
            return $"```flowlang\n{constructor}\n```\n\n**FlowLang constructor**\n\n{description}";
        }

        /// <summary>
        /// Generate hover content for Result type
        /// </summary>
        private string GetResultTypeHover()
        {
            var sb = new StringBuilder();

            sb.AppendLine("```flowlang");
            sb.AppendLine("Result<T, E>");
            sb.AppendLine("```");

            sb.AppendLine("**FlowLang Result type**");
            sb.AppendLine();
            sb.AppendLine("Represents either a successful value (`Ok`) or an error (`Error`).");
            sb.AppendLine();
            sb.AppendLine("**Usage:**");
            sb.AppendLine("- `Ok(value)` - Success");
            sb.AppendLine("- `Error(error)` - Failure");
            sb.AppendLine("- `result?` - Error propagation");

            return sb.ToString();
        }

        /// <summary>
        /// Generate hover content for effects
        /// </summary>
        private string GetEffectHover(string effect)
        {
            var description = GetEffectDescription(effect);
            return $"```flowlang\n{effect}\n```\n\n**FlowLang effect**\n\n{description}";
        }

        /// <summary>
        /// Get description for an effect
        /// </summary>
        private string GetEffectDescription(string effect)
        {
            return effect switch
            {
                "Database" => "Database operations (read, write, transactions)",
                "Network" => "Network operations (HTTP requests, API calls)",
                "Logging" => "Logging and diagnostic output",
                "FileSystem" => "File system operations (read, write, delete files)",
                "Memory" => "Memory operations (caching, in-memory state)",
                "IO" => "General input/output operations",
                _ => "Side effect"
            };
        }

        /// <summary>
        /// Generate generic hover for unrecognized identifiers
        /// </summary>
        private string GetGenericIdentifierHover(string identifier)
        {
            return $"```flowlang\n{identifier}\n```\n\n**Identifier**";
        }

        /// <summary>
        /// Convert a token to an LSP range
        /// </summary>
        private Microsoft.VisualStudio.LanguageServer.Protocol.Range TokenToRange(Token token)
        {
            var line = Math.Max(0, token.Line - 1); // Convert to 0-based
            var column = Math.Max(0, token.Column - 1); // Convert to 0-based
            var endColumn = column + token.Value.Length;

            return new Microsoft.VisualStudio.LanguageServer.Protocol.Range(
                new Position(line, column),
                new Position(line, endColumn)
            );
        }
    }
}