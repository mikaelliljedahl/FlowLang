using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Cadenza.Core;
using LspPosition = Microsoft.VisualStudio.LanguageServer.Protocol.Position;

namespace Cadenza.LanguageServer;

/// <summary>
/// Provides go-to-definition and symbol navigation for Cadenza
/// Handles function definitions, module references, variable declarations, and imports
/// </summary>
public class DefinitionProvider
{
    private readonly DocumentManager _documentManager;

    public DefinitionProvider(DocumentManager documentManager)
    {
        _documentManager = documentManager;
    }

    /// <summary>
    /// Get the definition location for the symbol at the specified position
    /// </summary>
    public Location? GetDefinition(ManagedDocument document, LspPosition position)
    {
        var token = _documentManager.GetTokenAtPosition(document, position);
        if (token == null || token.Type != TokenType.Identifier) return null;

        var location = FindDefinitionLocation(document, token, position);
        return location;
    }

    /// <summary>
    /// Get document symbols for outline/navigation view
    /// </summary>
    public SymbolInformation[]? GetDocumentSymbols(ManagedDocument document)
    {
        if (document.AST == null) return null;

        var symbols = new List<SymbolInformation>();

        foreach (var statement in document.AST.Statements)
        {
            switch (statement)
            {
                case FunctionDeclaration func:
                    symbols.Add(CreateFunctionSymbol(func, document.Uri));
                    break;

                case ModuleDeclaration module:
                    symbols.Add(CreateModuleSymbol(module, document.Uri));
                    // Add module functions as child symbols
                    foreach (var moduleStmt in module.Body)
                    {
                        if (moduleStmt is FunctionDeclaration moduleFunc)
                        {
                            symbols.Add(CreateFunctionSymbol(moduleFunc, document.Uri, module.Name));
                        }
                    }
                    break;
            }
        }

        return symbols.ToArray();
    }

    /// <summary>
    /// Find the definition location for a given token
    /// </summary>
    private Location? FindDefinitionLocation(ManagedDocument document, Token token, Position position)
    {
        if (document.AST == null) return null;

        var identifier = token.Lexeme;

        // Check for function definitions
        var functionLocation = FindFunctionDefinition(document, identifier);
        if (functionLocation != null) return functionLocation;

        // Check for module definitions
        var moduleLocation = FindModuleDefinition(document, identifier);
        if (moduleLocation != null) return moduleLocation;

        // Check for qualified names (Module.Function)
        var qualifiedLocation = FindQualifiedDefinition(document, token, position);
        if (qualifiedLocation != null) return qualifiedLocation;

        // Check for parameter definitions
        var parameterLocation = FindParameterDefinition(document, identifier, position);
        if (parameterLocation != null) return parameterLocation;

        // Check for variable definitions (let statements)
        var variableLocation = FindVariableDefinition(document, identifier, position);
        if (variableLocation != null) return variableLocation;

        return null;
    }

    /// <summary>
    /// Find function definition location
    /// </summary>
    private Location? FindFunctionDefinition(ManagedDocument document, string functionName)
    {
        if (document.AST == null) return null;

        foreach (var statement in document.AST.Statements)
        {
            if (statement is FunctionDeclaration func && func.Name == functionName)
            {
                // Find the function keyword token for this function
                var functionToken = FindFunctionKeywordToken(document, func);
                if (functionToken != null)
                {
                    return new Location
                    {
                        Uri = new Uri(document.Uri),
                        Range = TokenToRange(functionToken)
                    };
                }
            }
            else if (statement is ModuleDeclaration module)
            {
                foreach (var moduleStmt in module.Body)
                {
                    if (moduleStmt is FunctionDeclaration moduleFunc && moduleFunc.Name == functionName)
                    {
                        var functionToken = FindFunctionKeywordToken(document, moduleFunc);
                        if (functionToken != null)
                        {
                            return new Location
                            {
                                Uri = new Uri(document.Uri),
                                Range = TokenToRange(functionToken)
                            };
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Find module definition location
    /// </summary>
    private Location? FindModuleDefinition(ManagedDocument document, string moduleName)
    {
        if (document.AST == null) return null;

        foreach (var statement in document.AST.Statements)
        {
            if (statement is ModuleDeclaration module && module.Name == moduleName)
            {
                var moduleToken = FindModuleKeywordToken(document, module);
                if (moduleToken != null)
                {
                    return new Location
                    {
                        Uri = new Uri(document.Uri),
                        Range = TokenToRange(moduleToken)
                    };
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Find qualified name definition (Module.Function)
    /// </summary>
    private Location? FindQualifiedDefinition(ManagedDocument document, Token token, Position position)
    {
        // Check if the previous token is a dot, indicating a qualified name
        var tokenIndex = document.Tokens.IndexOf(token);
        if (tokenIndex > 0 && document.Tokens[tokenIndex - 1].Type == TokenType.Dot)
        {
            // This is the function part of Module.Function
            if (tokenIndex > 1)
            {
                var moduleToken = document.Tokens[tokenIndex - 2];
                if (moduleToken.Type == TokenType.Identifier)
                {
                    return FindQualifiedFunctionDefinition(document, moduleToken.Lexeme, token.Lexeme);
                }
            }
        }
        else if (tokenIndex < document.Tokens.Count - 2 && 
                 document.Tokens[tokenIndex + 1].Type == TokenType.Dot)
        {
            // This is the module part of Module.Function
            var functionToken = document.Tokens[tokenIndex + 2];
            if (functionToken.Type == TokenType.Identifier)
            {
                return FindQualifiedFunctionDefinition(document, token.Lexeme, functionToken.Lexeme);
            }
        }

        return null;
    }

    /// <summary>
    /// Find qualified function definition (Module.Function)
    /// </summary>
    private Location? FindQualifiedFunctionDefinition(ManagedDocument document, string moduleName, string functionName)
    {
        if (document.AST == null) return null;

        foreach (var statement in document.AST.Statements)
        {
            if (statement is ModuleDeclaration module && module.Name == moduleName)
            {
                foreach (var moduleStmt in module.Body)
                {
                    if (moduleStmt is FunctionDeclaration func && func.Name == functionName)
                    {
                        var functionToken = FindFunctionKeywordToken(document, func);
                        if (functionToken != null)
                        {
                            return new Location
                            {
                                Uri = new Uri(document.Uri),
                                Range = TokenToRange(functionToken)
                            };
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Find parameter definition location
    /// </summary>
    private Location? FindParameterDefinition(ManagedDocument document, string parameterName, Position position)
    {
        var containingFunction = FindContainingFunction(document, position);
        if (containingFunction == null) return null;

        var parameter = containingFunction.Parameters.FirstOrDefault(p => p.Name == parameterName);
        if (parameter == null) return null;

        // Find the parameter token in the function declaration
        var parameterToken = FindParameterToken(document, containingFunction, parameter);
        if (parameterToken != null)
        {
            return new Location
            {
                Uri = new Uri(document.Uri),
                Range = TokenToRange(parameterToken)
            };
        }

        return null;
    }

    /// <summary>
    /// Find variable definition location (let statements)
    /// </summary>
    private Location? FindVariableDefinition(ManagedDocument document, string variableName, Position position)
    {
        // This is a simplified implementation
        // In a full implementation, we'd need to track scope and find the nearest let statement
        
        var letTokens = document.Tokens
            .Where(t => t.Type == TokenType.Let)
            .ToArray();

        foreach (var letToken in letTokens)
        {
            var letIndex = document.Tokens.IndexOf(letToken);
            if (letIndex < document.Tokens.Count - 1)
            {
                var nextToken = document.Tokens[letIndex + 1];
                if (nextToken.Type == TokenType.Identifier && nextToken.Lexeme == variableName)
                {
                    return new Location
                    {
                        Uri = new Uri(document.Uri),
                        Range = TokenToRange(nextToken)
                    };
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Find the containing function for a given position
    /// </summary>
    private FunctionDeclaration? FindContainingFunction(ManagedDocument document, Position position)
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
    /// Find the function keyword token for a function declaration
    /// </summary>
    private Token? FindFunctionKeywordToken(ManagedDocument document, FunctionDeclaration func)
    {
        // This is simplified - in a full implementation, we'd track exact positions
        // For now, find a function token followed by an identifier matching the function name
        for (int i = 0; i < document.Tokens.Count - 1; i++)
        {
            var token = document.Tokens[i];
            var nextToken = document.Tokens[i + 1];

            if ((token.Type == TokenType.Function || 
                 (token.Type == TokenType.Pure && i + 2 < document.Tokens.Count && document.Tokens[i + 1].Type == TokenType.Function)) &&
                                                nextToken.Type == TokenType.Identifier && nextToken.Lexeme == func.Name)
            {
                return nextToken; // Return the function name token
            }
        }

        return null;
    }

    /// <summary>
    /// Find the module keyword token for a module declaration
    /// </summary>
    private Token? FindModuleKeywordToken(ManagedDocument document, ModuleDeclaration module)
    {
        for (int i = 0; i < document.Tokens.Count - 1; i++)
        {
            var token = document.Tokens[i];
            var nextToken = document.Tokens[i + 1];

            if (token.Type == TokenType.Module && 
                nextToken.Type == TokenType.Identifier && nextToken.Lexeme == module.Name)
            {
                return nextToken; // Return the module name token
            }
        }

        return null;
    }

    /// <summary>
    /// Find the parameter token in a function declaration
    /// </summary>
    private Token? FindParameterToken(ManagedDocument document, FunctionDeclaration func, Parameter parameter)
    {
        // Simplified implementation - find identifier tokens that match parameter names
        var functionNameToken = FindFunctionKeywordToken(document, func);
        if (functionNameToken == null) return null;

        var functionIndex = document.Tokens.IndexOf(functionNameToken);
        
        // Look for the parameter name after the function declaration
        for (int i = functionIndex; i < document.Tokens.Count; i++)
        {
            var token = document.Tokens[i];
            if (token.Type == TokenType.Identifier && token.Lexeme == parameter.Name)
            {
                // Check if this is followed by a colon (parameter syntax)
                if (i + 1 < document.Tokens.Count && document.Tokens[i + 1].Type == TokenType.Colon)
                {
                    return token;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Create a symbol information for a function
    /// </summary>
    private SymbolInformation CreateFunctionSymbol(FunctionDeclaration func, string uri, string? containerName = null)
    {
        return new SymbolInformation
        {
            Name = func.Name,
            Kind = SymbolKind.Function,
            Location = new Location
            {
                Uri = new Uri(uri),
                Range = new Microsoft.VisualStudio.LanguageServer.Protocol.Range { Start = new Position(0, 0), End = new Position(0, 0) } // TODO: Get actual range
            },
            ContainerName = containerName
        };
    }

    /// <summary>
    /// Create a symbol information for a module
    /// </summary>
    private SymbolInformation CreateModuleSymbol(ModuleDeclaration module, string uri)
    {
        return new SymbolInformation
        {
            Name = module.Name,
            Kind = SymbolKind.Module,
            Location = new Location
            {
                Uri = new Uri(uri),
                Range = new Microsoft.VisualStudio.LanguageServer.Protocol.Range { Start = new Position(0, 0), End = new Position(0, 0) } // TODO: Get actual range
            }
        };
    }

    /// <summary>
    /// Convert a token to an LSP range
    /// </summary>
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
}