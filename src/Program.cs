#:package Microsoft.CodeAnalysis@4.5.0
#:package Microsoft.CodeAnalysis.CSharp@4.5.0

using System;
using System.IO;
using System.Threading.Tasks;
using FlowLang.Compiler;

namespace FlowLang.CLI;

// FlowLang to C# Transpiler
public class FlowLangTranspiler
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length < 2 || args[0] != "--input")
        {
            Console.WriteLine("Usage: dotnet run Program.cs --input <input.flow> [--output <output.cs>]");
            return 1;
        }

        var inputPath = args[1];
        var outputPath = args.Length > 3 && args[2] == "--output" ? args[3] : null;

        try
        {
            var transpiler = new FlowLangTranspiler();
            await transpiler.TranspileAsync(inputPath, outputPath);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    public async Task TranspileAsync(string inputPath, string? outputPath = null)
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException($"Input file not found: {inputPath}");
        }

        var flowLangSource = await File.ReadAllTextAsync(inputPath);
        var csharpCode = TranspileToCS(flowLangSource);

        if (outputPath == null)
        {
            outputPath = Path.ChangeExtension(inputPath, ".cs");
        }

        await File.WriteAllTextAsync(outputPath, csharpCode);
        Console.WriteLine($"Transpiled {inputPath} -> {outputPath}");
    }

    public string TranspileToCS(string flowLangSource)
    {
        var lexer = new FlowLangLexer(flowLangSource);
        var tokens = lexer.Tokenize();
        
        var parser = new FlowLangParser(tokens);
        var ast = parser.Parse();
        
        var generator = new CSharpGenerator();
        var syntaxTree = generator.GenerateFromAST(ast);
        
        return syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();
    }
}