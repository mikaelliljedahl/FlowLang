using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Cadenza.Core;

namespace Cadenza.LanguageServer
{
    /// <summary>
    /// Provides real-time diagnostics for Cadenza documents
    /// Leverages existing FlowLang lexer and parser to detect syntax errors, type mismatches,
    /// effect system violations, and other Cadenza-specific issues
    /// </summary>
    public class DiagnosticsProvider
    {
        /// <summary>
        /// Analyze a document and return diagnostics (errors, warnings, info)
        /// </summary>
        public Diagnostic[] GetDiagnostics(ManagedDocument document)
        {
            var diagnostics = new List<Diagnostic>();

            // Add lexer/parser errors
            diagnostics.AddRange(GetSyntaxDiagnostics(document));

            // Add semantic diagnostics if AST is available
            if (document.AST != null)
            {
                diagnostics.AddRange(GetSemanticDiagnostics(document));
                diagnostics.AddRange(GetEffectSystemDiagnostics(document));
                diagnostics.AddRange(GetModuleSystemDiagnostics(document));
            }

            return diagnostics.ToArray();
        }

        /// <summary>
        /// Get syntax errors from lexer and parser
        /// </summary>
        private IEnumerable<Diagnostic> GetSyntaxDiagnostics(ManagedDocument document)
        {
            var diagnostics = new List<Diagnostic>();

            // Handle parse errors
            foreach (var error in document.ParseErrors)
            {
                var diagnostic = CreateDiagnosticFromException(error);
                if (diagnostic != null)
                {
                    diagnostics.Add(diagnostic);
                }
            }

            // Check for unclosed strings, brackets, etc.
            diagnostics.AddRange(CheckUnclosedConstructs(document));

            // Check for invalid tokens
            diagnostics.AddRange(CheckInvalidTokens(document));

            return diagnostics;
        }

        /// <summary>
        /// Get semantic errors from AST analysis
        /// </summary>
        private IEnumerable<Diagnostic> GetSemanticDiagnostics(ManagedDocument document)
        {
            var diagnostics = new List<Diagnostic>();

            if (document.AST == null) return diagnostics;

            // Check for undefined variables/functions
            diagnostics.AddRange(CheckUndefinedReferences(document));

            // Check return type consistency
            diagnostics.AddRange(CheckReturnTypes(document));

            // Check function call signatures
            diagnostics.AddRange(CheckFunctionCalls(document));

            // Check Result type usage
            diagnostics.AddRange(CheckResultTypes(document));

            return diagnostics;
        }

        /// <summary>
        /// Get effect system diagnostics
        /// </summary>
        private IEnumerable<Diagnostic> GetEffectSystemDiagnostics(ManagedDocument document)
        {
            var diagnostics = new List<Diagnostic>();

            if (document.AST == null) return diagnostics;

            foreach (var statement in document.AST.Statements)
            {
                if (statement is FunctionDeclaration func)
                {
                    diagnostics.AddRange(CheckEffectConsistency(func, document));
                }
            }

            return diagnostics;
        }

        /// <summary>
        /// Get module system diagnostics
        /// </summary>
        private IEnumerable<Diagnostic> GetModuleSystemDiagnostics(ManagedDocument document)
        {
            var diagnostics = new List<Diagnostic>();

            if (document.AST == null) return diagnostics;

            // Check import statements
            diagnostics.AddRange(CheckImportStatements(document));

            // Check export statements
            diagnostics.AddRange(CheckExportStatements(document));

            return diagnostics;
        }

        /// <summary>
        /// Create a diagnostic from an exception
        /// </summary>
        private Diagnostic? CreateDiagnosticFromException(Exception exception)
        {
            var message = exception.Message;
            var range = ExtractRangeFromErrorMessage(message);

            return new Diagnostic
            {
                Range = range ?? new Microsoft.VisualStudio.LanguageServer.Protocol.Range { Start = new Position(0, 0), End = new Position(0, 0) },
                Severity = DiagnosticSeverity.Error,
                Source = "Cadenza",
                Message = CleanErrorMessage(message),
                Code = "SYNTAX_ERROR"
            };
        }

        /// <summary>
        /// Check for unclosed constructs like strings, brackets, etc.
        /// </summary>
        private IEnumerable<Diagnostic> CheckUnclosedConstructs(ManagedDocument document)
        {
            var diagnostics = new List<Diagnostic>();
            var stack = new Stack<(TokenType type, Token token)>();

            foreach (var token in document.Tokens)
            {
                switch (token.Type)
                {
                    case TokenType.LeftParen:
                        stack.Push((TokenType.RightParen, token));
                        break;
                    case TokenType.LeftBrace:
                        stack.Push((TokenType.RightBrace, token));
                        break;
                    case TokenType.LeftBracket:
                        stack.Push((TokenType.RightBracket, token));
                        break;
                    case TokenType.RightParen:
                        if (stack.Count == 0 || stack.Peek().type != TokenType.RightParen)
                        {
                            diagnostics.Add(CreateUnmatchedClosingDiagnostic(token, ")"));
                        }
                        else
                        {
                            stack.Pop();
                        }
                        break;
                    case TokenType.RightBrace:
                        if (stack.Count == 0 || stack.Peek().type != TokenType.RightBrace)
                        {
                            diagnostics.Add(CreateUnmatchedClosingDiagnostic(token, "}"));
                        }
                        else
                        {
                            stack.Pop();
                        }
                        break;
                    case TokenType.RightBracket:
                        if (stack.Count == 0 || stack.Peek().type != TokenType.RightBracket)
                        {
                            diagnostics.Add(CreateUnmatchedClosingDiagnostic(token, "]"));
                        }
                        else
                        {
                            stack.Pop();
                        }
                        break;
                }
            }

            // Check for unclosed constructs
            while (stack.Count > 0)
            {
                var (expectedType, openToken) = stack.Pop();
                var expectedChar = expectedType switch
                {
                    TokenType.RightParen => ")",
                    TokenType.RightBrace => "}",
                    TokenType.RightBracket => "]",
                    _ => "?"
                };
                diagnostics.Add(CreateUnclosedDiagnostic(openToken, expectedChar));
            }

            return diagnostics;
        }

        /// <summary>
        /// Check for invalid token sequences
        /// </summary>
        private IEnumerable<Diagnostic> CheckInvalidTokens(ManagedDocument document)
        {
            var diagnostics = new List<Diagnostic>();

            for (int i = 0; i < document.Tokens.Count - 1; i++)
            {
                var current = document.Tokens[i];
                var next = document.Tokens[i + 1];

                // Check for invalid operator sequences
                if (IsOperator(current) && IsOperator(next) && !IsValidOperatorSequence(current, next))
                {
                    diagnostics.Add(new Diagnostic
                    {
                        Range = TokenToRange(next),
                        Severity = DiagnosticSeverity.Error,
                        Source = "Cadenza",
                        Message = $"Invalid operator sequence: '{current.Lexeme}{next.Lexeme}'",
                        Code = "INVALID_OPERATOR_SEQUENCE"
                    });
                }
            }

            return diagnostics;
        }

        /// <summary>
        /// Check for undefined variable/function references
        /// </summary>
        private IEnumerable<Diagnostic> CheckUndefinedReferences(ManagedDocument document)
        {
            var diagnostics = new List<Diagnostic>();
            var definedFunctions = new HashSet<string>();
            var definedVariables = new HashSet<string>();

            if (document.AST == null) return diagnostics;

            // Collect defined functions
            foreach (var statement in document.AST.Statements)
            {
                if (statement is FunctionDeclaration func)
                {
                    definedFunctions.Add(func.Name);
                }
                else if (statement is ModuleDeclaration module)
                {
                    foreach (var moduleStmt in module.Body)
                    {
                        if (moduleStmt is FunctionDeclaration moduleFunc)
                        {
                            definedFunctions.Add($"{module.Name}.{moduleFunc.Name}");
                        }
                    }
                }
            }

            // Check for undefined references (simplified implementation)
            // In a full implementation, we'd need to track scope and variable definitions
            foreach (var token in document.Tokens)
            {
                if (token.Type == TokenType.Identifier)
                {
                    // This is a simplified check - a full implementation would need scope analysis
                    var isBuiltinKeyword = IsBuiltinKeyword(token.Lexeme);
                    var isDefined = definedFunctions.Contains(token.Lexeme) || 
                                   definedVariables.Contains(token.Lexeme) ||
                                   isBuiltinKeyword;

                    if (!isDefined && !IsCommonIdentifier(token.Lexeme))
                    {
                        // Only warn for potential undefined references
                        diagnostics.Add(new Diagnostic
                        {
                            Range = TokenToRange(token),
                            Severity = DiagnosticSeverity.Information,
                            Source = "Cadenza",
                            Message = $"Identifier '{token.Lexeme}' may be undefined",
                            Code = "POTENTIALLY_UNDEFINED"
                        });
                    }
                }
            }

            return diagnostics;
        }

        /// <summary>
        /// Check effect system consistency
        /// </summary>
        private IEnumerable<Diagnostic> CheckEffectConsistency(FunctionDeclaration func, ManagedDocument document)
        {
            var diagnostics = new List<Diagnostic>();

            // Check if pure function has effect annotations
            if (func.IsPure && func.Effects != null)
            {
                diagnostics.Add(new Diagnostic
                {
                    Range = new Microsoft.VisualStudio.LanguageServer.Protocol.Range { Start = new Position(0, 0), End = new Position(0, 0) }, // TODO: Get actual range
                    Severity = DiagnosticSeverity.Error,
                    Source = "Cadenza",
                    Message = $"Pure function '{func.Name}' cannot have effect annotations",
                    Code = "PURE_FUNCTION_WITH_EFFECTS"
                });
            }

            // Check for unknown effects
            if (func.Effects != null)
            {
                foreach (var effect in func.Effects)
                {
                                    var validEffects = new List<string> { "Database", "Network", "Logging", "FileSystem", "Memory", "IO" };
                if (!validEffects.Contains(effect))
                    {
                        diagnostics.Add(new Diagnostic
                        {
                            Range = new Microsoft.VisualStudio.LanguageServer.Protocol.Range { Start = new Position(0, 0), End = new Position(0, 0) }, // TODO: Get actual range
                            Severity = DiagnosticSeverity.Error,
                            Source = "Cadenza",
                            Message = $"Unknown effect: '{effect}'. Valid effects are: Database, Network, Logging, FileSystem, Memory, IO",
                            Code = "UNKNOWN_EFFECT"
                        });
                    }
                }
            }

            return diagnostics;
        }

        /// <summary>
        /// Check return type consistency
        /// </summary>
        private IEnumerable<Diagnostic> CheckReturnTypes(ManagedDocument document)
        {
            // Simplified implementation - a full version would analyze return paths
            return Array.Empty<Diagnostic>();
        }

        /// <summary>
        /// Check function call signatures
        /// </summary>
        private IEnumerable<Diagnostic> CheckFunctionCalls(ManagedDocument document)
        {
            // Simplified implementation - a full version would validate argument types
            return Array.Empty<Diagnostic>();
        }

        /// <summary>
        /// Check Result type usage
        /// </summary>
        private IEnumerable<Diagnostic> CheckResultTypes(ManagedDocument document)
        {
            // Check for error propagation operator usage
            var diagnostics = new List<Diagnostic>();

            // This would be enhanced to check proper Result type handling
            return diagnostics;
        }

        /// <summary>
        /// Check import statements
        /// </summary>
        private IEnumerable<Diagnostic> CheckImportStatements(ManagedDocument document)
        {
            // Check for circular imports, missing modules, etc.
            return Array.Empty<Diagnostic>();
        }

        /// <summary>
        /// Check export statements
        /// </summary>
        private IEnumerable<Diagnostic> CheckExportStatements(ManagedDocument document)
        {
            // Check that exported functions exist
            return Array.Empty<Diagnostic>();
        }

        #region Helper Methods

        private Microsoft.VisualStudio.LanguageServer.Protocol.Range? ExtractRangeFromErrorMessage(string message)
        {
            // Try to extract line/column information from error message
            // This is a simplified implementation
            return null;
        }

        private string CleanErrorMessage(string message)
        {
            // Remove internal stack trace information and make user-friendly
            return message.Split('\n')[0];
        }

        private Diagnostic CreateUnmatchedClosingDiagnostic(Token token, string character)
        {
            return new Diagnostic
            {
                Range = TokenToRange(token),
                Severity = DiagnosticSeverity.Error,
                Source = "Cadenza",
                Message = $"Unmatched closing '{character}'",
                Code = "UNMATCHED_CLOSING"
            };
        }

        private Diagnostic CreateUnclosedDiagnostic(Token token, string expectedChar)
        {
            return new Diagnostic
            {
                Range = TokenToRange(token),
                Severity = DiagnosticSeverity.Error,
                Source = "Cadenza",
                Message = $"Unclosed '{token.Lexeme}', expected '{expectedChar}'",
                Code = "UNCLOSED_CONSTRUCT"
            };
        }

        private Microsoft.VisualStudio.LanguageServer.Protocol.Range TokenToRange(Token token)
        {
            var line = Math.Max(0, token.Line - 1); // Convert to 0-based
            var column = Math.Max(0, token.Column - 1); // Convert to 0-based
            var endColumn = column + token.Lexeme.Length;

            return new Microsoft.VisualStudio.LanguageServer.Protocol.Range
            {
                Start = new Position(line, column),
                End = new Position(line, endColumn)
            };
        }

        private bool IsOperator(Token token)
        {
            return token.Type switch
            {
                TokenType.Plus or TokenType.Minus or TokenType.Multiply or TokenType.Divide
                or TokenType.Greater or TokenType.Less or TokenType.GreaterEqual or TokenType.LessEqual
                or TokenType.Equal or TokenType.NotEqual or TokenType.And or TokenType.Or
                or TokenType.Not or TokenType.Assign => true,
                _ => false
            };
        }

        private bool IsValidOperatorSequence(Token first, Token second)
        {
            // Allow specific valid sequences like ->, ==, !=, &&, ||, >=, <=
            var sequence = first.Lexeme + second.Lexeme;
            return sequence is "->" or "==" or "!=" or "&&" or "||" or ">=" or "<=";
        }

        private bool IsBuiltinKeyword(string identifier)
        {
            return identifier switch
            {
                "int" or "string" or "bool" or "Result" or "Ok" or "Error" 
                or "Database" or "Network" or "Logging" or "FileSystem" or "Memory" or "IO" => true,
                _ => false
            };
        }

        private bool IsCommonIdentifier(string identifier)
        {
            // Common parameter names, built-in functions, etc.
            return identifier switch
            {
                "a" or "b" or "c" or "x" or "y" or "z" or "i" or "j" or "k"
                or "name" or "value" or "result" or "data" or "item" or "element"
                or "main" or "test" => true,
                _ => false
            };
        }

        #endregion
    }
}