using System;
using Cadenza.Core;

namespace DebugLexer
{
    class Program
    {
        static void Main(string[] args)
        {
            var source = "function\ntest\n()";
            var lexer = new CadenzaLexer(source);
            var tokens = lexer.ScanTokens();

            for (int i = 0; i < tokens.Count; i++)
            {
                Console.WriteLine($"Token {i}: Type={tokens[i].Type}, Lexeme='{tokens[i].Lexeme}', Line={tokens[i].Line}, Column={tokens[i].Column}");
            }
        }
    }
}