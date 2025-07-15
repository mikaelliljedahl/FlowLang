using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Cadenza.LSP
{
    /// <summary>
    /// Provides intelligent auto-completion for Cadenza
    /// Supports keywords, identifiers, effects, Result types, and context-aware suggestions
    /// </summary>
    public class CompletionProvider
    {
        private static readonly CompletionItem[] CadenzaKeywords = new[]
        {
            CreateKeywordCompletion("function", "function name() -> type {\n    $0\n}", "Declare a function"),
            CreateKeywordCompletion("pure", "pure function", "Pure function modifier"),
            CreateKeywordCompletion("return", "return", "Return statement"),
            CreateKeywordCompletion("if", "if condition {\n    $0\n}", "If statement"),
            CreateKeywordCompletion("else", "else {\n    $0\n}", "Else clause"),
            CreateKeywordCompletion("let", "let name = ", "Variable declaration"),
            CreateKeywordCompletion("guard", "guard condition else {\n    $0\n}", "Guard statement"),
            CreateKeywordCompletion("uses", "uses [${1:Effect}]", "Effect annotation"),
            CreateKeywordCompletion("module", "module ${1:Name} {\n    $0\n}", "Module declaration"),
            CreateKeywordCompletion("import", "import ${1:Module}", "Import statement"),
            CreateKeywordCompletion("export", "export {${1:function}}", "Export statement"),
        };

        private static readonly CompletionItem[] CadenzaTypes = new[]
        {
            CreateTypeCompletion("int", "Integer type"),
            CreateTypeCompletion("string", "String type"),
            CreateTypeCompletion("bool", "Boolean type"),
            CreateTypeCompletion("Result", "Result<${1:T}, ${2:E}>", "Result type for error handling"),
        };

        private static readonly CompletionItem[] CadenzaEffects = new[]
        {
            CreateEffectCompletion("Database", "Database operations"),
            CreateEffectCompletion("Network", "Network operations"),
            CreateEffectCompletion("Logging", "Logging operations"),
            CreateEffectCompletion("FileSystem", "File system operations"),
            CreateEffectCompletion("Memory", "Memory operations"),
            CreateEffectCompletion("IO", "Input/output operations"),
        };

        private static readonly CompletionItem[] CadenzaConstructors = new[]
        {
            CreateConstructorCompletion("Ok", "Ok(${1:value})", "Create successful Result"),
            CreateConstructorCompletion("Error", "Error(${1:error})", "Create error Result"),
        };

        /// <summary>
        /// Get completion suggestions for a document at a specific position
        /// </summary>
        public CompletionList? GetCompletions(ManagedDocument document, Position position)
        {
            var context = AnalyzeCompletionContext(document, position);
            var completions = new List<CompletionItem>();

            switch (context.Type)
            {
                case CompletionType.Keywords:
                    completions.AddRange(CadenzaKeywords);
                    break;

                case CompletionType.Types:
                    completions.AddRange(CadenzaTypes);
                    break;

                case CompletionType.Effects:
                    completions.AddRange(CadenzaEffects);
                    break;

                case CompletionType.FunctionCall:
                    completions.AddRange(GetFunctionCompletions(document, context));
                    break;

                case CompletionType.ModuleAccess:
                    completions.AddRange(GetModuleCompletions(document, context));
                    break;

                case CompletionType.StringInterpolation:
                    completions.AddRange(GetStringInterpolationCompletions(document, context));
                    break;

                case CompletionType.General:
                default:
                    completions.AddRange(CadenzaKeywords);
                    completions.AddRange(CadenzaTypes);
                    completions.AddRange(CadenzaConstructors);
                    completions.AddRange(GetIdentifierCompletions(document, context));
                    break;
            }

            // Filter completions based on prefix
            if (!string.IsNullOrEmpty(context.Prefix))
            {
                completions = completions
                    .Where(c => c.Label.StartsWith(context.Prefix, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return new CompletionList
            {
                IsIncomplete = false,
                Items = completions.ToArray()
            };
        }

        /// <summary>
        /// Analyze the context around the completion position
        /// </summary>
        private CompletionContext AnalyzeCompletionContext(ManagedDocument document, Position position)
        {
            var line = document.GetLine(position.Line);
            var beforeCursor = line.Substring(0, Math.Min(position.Character, line.Length));
            var afterCursor = line.Substring(Math.Min(position.Character, line.Length));

            var context = new CompletionContext
            {
                Type = CompletionType.General,
                Position = position,
                Line = line,
                BeforeCursor = beforeCursor,
                AfterCursor = afterCursor,
                Prefix = ExtractPrefix(beforeCursor)
            };

            // Determine completion type based on context
            if (beforeCursor.TrimEnd().EndsWith("->"))
            {
                context.Type = CompletionType.Types;
            }
            else if (beforeCursor.TrimEnd().EndsWith("uses [") || beforeCursor.Contains("uses ["))
            {
                context.Type = CompletionType.Effects;
            }
            else if (beforeCursor.TrimEnd().EndsWith(":"))
            {
                context.Type = CompletionType.Types;
            }
            else if (beforeCursor.Contains(".") && !beforeCursor.TrimEnd().EndsWith("."))
            {
                context.Type = CompletionType.ModuleAccess;
                context.ModuleName = ExtractModuleName(beforeCursor);
            }
            else if (beforeCursor.Contains("$\"") && !beforeCursor.EndsWith("\""))
            {
                context.Type = CompletionType.StringInterpolation;
            }
            else if (beforeCursor.TrimEnd().EndsWith("(") || 
                     (beforeCursor.Contains("(") && !beforeCursor.Contains(")")))
            {
                context.Type = CompletionType.FunctionCall;
                context.FunctionName = ExtractFunctionName(beforeCursor);
            }

            return context;
        }

        /// <summary>
        /// Get function-specific completions
        /// </summary>
        private IEnumerable<CompletionItem> GetFunctionCompletions(ManagedDocument document, CompletionContext context)
        {
            var completions = new List<CompletionItem>();

            // Add available functions from the document
            if (document.AST != null)
            {
                foreach (var statement in document.AST.Statements)
                {
                    if (statement is FunctionDeclaration func)
                    {
                        var signature = BuildFunctionSignature(func);
                        completions.Add(new CompletionItem
                        {
                            Label = func.Name,
                            Kind = CompletionItemKind.Function,
                            Detail = signature,
                            Documentation = BuildFunctionDocumentation(func),
                            InsertText = BuildFunctionCallSnippet(func),
                            InsertTextFormat = InsertTextFormat.Snippet
                        });
                    }
                }
            }

            return completions;
        }

        /// <summary>
        /// Get module member completions
        /// </summary>
        private IEnumerable<CompletionItem> GetModuleCompletions(ManagedDocument document, CompletionContext context)
        {
            var completions = new List<CompletionItem>();

            if (document.AST != null && !string.IsNullOrEmpty(context.ModuleName))
            {
                foreach (var statement in document.AST.Statements)
                {
                    if (statement is ModuleDeclaration module && module.Name == context.ModuleName)
                    {
                        foreach (var memberStmt in module.Body)
                        {
                            if (memberStmt is FunctionDeclaration func)
                            {
                                var signature = BuildFunctionSignature(func);
                                completions.Add(new CompletionItem
                                {
                                    Label = func.Name,
                                    Kind = CompletionItemKind.Function,
                                    Detail = $"{module.Name}.{signature}",
                                    Documentation = BuildFunctionDocumentation(func),
                                    InsertText = BuildFunctionCallSnippet(func),
                                    InsertTextFormat = InsertTextFormat.Snippet
                                });
                            }
                        }
                    }
                }
            }

            return completions;
        }

        /// <summary>
        /// Get string interpolation completions
        /// </summary>
        private IEnumerable<CompletionItem> GetStringInterpolationCompletions(ManagedDocument document, CompletionContext context)
        {
            // Return available variables and simple expressions
            return GetIdentifierCompletions(document, context);
        }

        /// <summary>
        /// Get identifier completions (variables, parameters, functions)
        /// </summary>
        private IEnumerable<CompletionItem> GetIdentifierCompletions(ManagedDocument document, CompletionContext context)
        {
            var completions = new List<CompletionItem>();

            if (document.AST != null)
            {
                // Add function names
                foreach (var statement in document.AST.Statements)
                {
                    if (statement is FunctionDeclaration func)
                    {
                        completions.Add(new CompletionItem
                        {
                            Label = func.Name,
                            Kind = CompletionItemKind.Function,
                            Detail = BuildFunctionSignature(func),
                            Documentation = BuildFunctionDocumentation(func)
                        });
                    }
                }

                // Add common variable names based on context
                var commonVariables = GetCommonVariableNames();
                foreach (var varName in commonVariables)
                {
                    completions.Add(new CompletionItem
                    {
                        Label = varName,
                        Kind = CompletionItemKind.Variable,
                        Detail = "variable"
                    });
                }
            }

            return completions;
        }

        #region Helper Methods

        private string ExtractPrefix(string beforeCursor)
        {
            var words = beforeCursor.Split(new[] { ' ', '\t', '(', ')', '[', ']', '{', '}', ',', ';', ':', '.', '+', '-', '*', '/', '=', '!', '<', '>' }, StringSplitOptions.RemoveEmptyEntries);
            return words.Length > 0 ? words[^1] : string.Empty;
        }

        private string ExtractModuleName(string beforeCursor)
        {
            var dotIndex = beforeCursor.LastIndexOf('.');
            if (dotIndex > 0)
            {
                var beforeDot = beforeCursor.Substring(0, dotIndex);
                var words = beforeDot.Split(new[] { ' ', '\t', '(', ')', '[', ']', '{', '}', ',', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);
                return words.Length > 0 ? words[^1] : string.Empty;
            }
            return string.Empty;
        }

        private string ExtractFunctionName(string beforeCursor)
        {
            var parenIndex = beforeCursor.LastIndexOf('(');
            if (parenIndex > 0)
            {
                var beforeParen = beforeCursor.Substring(0, parenIndex).TrimEnd();
                var words = beforeParen.Split(new[] { ' ', '\t', '(', ')', '[', ']', '{', '}', ',', ';', ':', '.', '+', '-', '*', '/', '=', '!', '<', '>' }, StringSplitOptions.RemoveEmptyEntries);
                return words.Length > 0 ? words[^1] : string.Empty;
            }
            return string.Empty;
        }

        private string BuildFunctionSignature(FunctionDeclaration func)
        {
            var parameters = string.Join(", ", func.Parameters.Select(p => $"{p.Name}: {p.Type}"));
            var effectsStr = func.Effects != null ? $" uses [{string.Join(", ", func.Effects.Effects)}]" : "";
            var pureStr = func.IsPure ? "pure " : "";
            return $"{pureStr}function {func.Name}({parameters}){effectsStr} -> {func.ReturnType}";
        }

        private string BuildFunctionDocumentation(FunctionDeclaration func)
        {
            var doc = $"Function: {func.Name}\n";
            if (func.IsPure)
            {
                doc += "Pure function - no side effects\n";
            }
            if (func.Effects != null && func.Effects.Effects.Count > 0)
            {
                doc += $"Effects: {string.Join(", ", func.Effects.Effects)}\n";
            }
            doc += $"Returns: {func.ReturnType}";
            return doc;
        }

        private string BuildFunctionCallSnippet(FunctionDeclaration func)
        {
            if (func.Parameters.Count == 0)
            {
                return $"{func.Name}()";
            }

            var parameters = func.Parameters.Select((p, i) => $"${{{i + 1}:{p.Name}}}").ToArray();
            return $"{func.Name}({string.Join(", ", parameters)})";
        }

        private string[] GetCommonVariableNames()
        {
            return new[] { "result", "value", "data", "item", "element", "name", "id", "count", "index", "temp" };
        }

        private static CompletionItem CreateKeywordCompletion(string keyword, string insertText = null, string documentation = null)
        {
            return new CompletionItem
            {
                Label = keyword,
                Kind = CompletionItemKind.Keyword,
                Detail = $"Cadenza keyword",
                Documentation = documentation ?? $"Cadenza '{keyword}' keyword",
                InsertText = insertText ?? keyword,
                InsertTextFormat = insertText?.Contains("$") == true ? InsertTextFormat.Snippet : InsertTextFormat.PlainText
            };
        }

        private static CompletionItem CreateTypeCompletion(string type, string insertText = null, string documentation = null)
        {
            return new CompletionItem
            {
                Label = type,
                Kind = CompletionItemKind.TypeParameter,
                Detail = "Cadenza type",
                Documentation = documentation ?? $"Cadenza '{type}' type",
                InsertText = insertText ?? type,
                InsertTextFormat = insertText?.Contains("$") == true ? InsertTextFormat.Snippet : InsertTextFormat.PlainText
            };
        }

        private static CompletionItem CreateEffectCompletion(string effect, string documentation = null)
        {
            return new CompletionItem
            {
                Label = effect,
                Kind = CompletionItemKind.EnumMember,
                Detail = "Cadenza effect",
                Documentation = documentation ?? $"Cadenza '{effect}' effect",
                InsertText = effect
            };
        }

        private static CompletionItem CreateConstructorCompletion(string constructor, string insertText, string documentation = null)
        {
            return new CompletionItem
            {
                Label = constructor,
                Kind = CompletionItemKind.Constructor,
                Detail = "Cadenza constructor",
                Documentation = documentation ?? $"Cadenza '{constructor}' constructor",
                InsertText = insertText,
                InsertTextFormat = InsertTextFormat.Snippet
            };
        }

        #endregion
    }

    /// <summary>
    /// Represents the context for completion analysis
    /// </summary>
    public class CompletionContext
    {
        public CompletionType Type { get; set; } = CompletionType.General;
        public Position Position { get; set; } = new();
        public string Line { get; set; } = string.Empty;
        public string BeforeCursor { get; set; } = string.Empty;
        public string AfterCursor { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Types of completion contexts
    /// </summary>
    public enum CompletionType
    {
        General,
        Keywords,
        Types,
        Effects,
        FunctionCall,
        ModuleAccess,
        StringInterpolation
    }
}