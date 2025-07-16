// Cadenza Transpiler - Extracted from cadenzac-core.cs

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cadenza.Core;

// =============================================================================
// TRANSPILER
// =============================================================================

public class CadenzaTranspiler
{
    public async Task<string> TranspileAsync(string sourceFile, string? outputFile = null)
    {
        // Read source file
        var source = await File.ReadAllTextAsync(sourceFile);
        
        // Lex
        var lexer = new CadenzaLexer(source);
        var tokens = lexer.ScanTokens();
        
        // Parse
        var parser = new CadenzaParser(tokens);
        var ast = parser.Parse();
        
        // Generate C#
        var generator = new CSharpGenerator();
        var syntaxTree = generator.GenerateFromAST(ast);
        
        var csharpCode = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();
        
        // Write to output file if specified
        if (outputFile != null)
        {
            await File.WriteAllTextAsync(outputFile, csharpCode);
        }
        
        return csharpCode;
    }
    
    public async Task<string> TranspileToJavaScriptAsync(string sourceFile, string? outputFile = null)
    {
        // For now, return a placeholder - full JavaScript generation would be implemented here
        var jsCode = "// JavaScript generation not yet implemented in core compiler\n// Use cadenzac-dev tool for JavaScript compilation";
        
        if (outputFile != null)
        {
            await File.WriteAllTextAsync(outputFile, jsCode);
        }
        
        return jsCode;
    }
    
    public async Task<string> TranspileToBlazorAsync(string sourceFile, string? outputFile = null)
    {
        // Read source file
        var source = await File.ReadAllTextAsync(sourceFile);
        
        // Lex
        var lexer = new CadenzaLexer(source);
        var tokens = lexer.ScanTokens();
        
        // Parse
        var parser = new CadenzaParser(tokens);
        var ast = parser.Parse();
        
        // Generate Blazor
        var blazorGenerator = new BlazorGenerator();
        var blazorContent = new System.Text.StringBuilder();
        
        // Generate Blazor components from AST
        foreach (var statement in ast.Statements)
        {
            if (statement is ComponentDeclaration component)
            {
                blazorContent.AppendLine(blazorGenerator.GenerateBlazorComponent(component));
            }
        }
        
        var blazorCode = blazorContent.ToString();
        
        // Write to output file if specified
        if (outputFile != null)
        {
            await File.WriteAllTextAsync(outputFile, blazorCode);
        }
        
        return blazorCode;
    }
}