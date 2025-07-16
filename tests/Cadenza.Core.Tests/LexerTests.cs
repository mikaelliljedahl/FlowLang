using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Cadenza.Core;

namespace Cadenza.Core.Tests;

[TestFixture]
public class LexerTests
{
    private CadenzaLexer _lexer;

    [Test]
    public void Lexer_EmptyString_ShouldReturnEOFToken()
    {
        // Arrange
        _lexer = new CadenzaLexer("");

        // Act
        var tokens = _lexer.ScanTokens();

        // Assert
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(TokenType.EOF, tokens[0].Type);
    }

    [Test]
    public void Lexer_SimpleFunction_ShouldTokenizeCorrectly()
    {
        // Arrange
        var source = "function test() -> int { return 42; }";
        _lexer = new CadenzaLexer(source);

        // Act
        var tokens = _lexer.ScanTokens();

        // Assert
        Assert.Greater(tokens.Count, 1);
        Assert.AreEqual(TokenType.Function, tokens[0].Type);
        Assert.AreEqual("function", tokens[0].Lexeme);
        Assert.AreEqual(TokenType.Identifier, tokens[1].Type);
        Assert.AreEqual("test", tokens[1].Lexeme);
        Assert.AreEqual(TokenType.EOF, tokens[tokens.Count - 1].Type);
    }

    [Test]
    public void Lexer_Keywords_ShouldBeIdentifiedCorrectly()
    {
        // Arrange
        var keywords = new Dictionary<string, TokenType>
        {
            {"function", TokenType.Function},
            {"return", TokenType.Return},
            {"if", TokenType.If},
            {"else", TokenType.Else},
            {"let", TokenType.Let},
            {"match", TokenType.Match},
            {"Ok", TokenType.Ok},
            {"Error", TokenType.Error},
            {"guard", TokenType.Guard},
            {"module", TokenType.Module},
            {"import", TokenType.Import},
            {"export", TokenType.Export}
        };

        foreach (var kvp in keywords)
        {
            // Act
            _lexer = new CadenzaLexer(kvp.Key);
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.AreEqual(2, tokens.Count); // keyword + EOF
            Assert.AreEqual(kvp.Value, tokens[0].Type);
            Assert.AreEqual(kvp.Key, tokens[0].Lexeme);
        }
    }

    [Test]
    public void Lexer_Numbers_ShouldTokenizeCorrectly()
    {
        // Arrange
        var source = "42 3.14 0 123";
        _lexer = new CadenzaLexer(source);

        // Act
        var tokens = _lexer.ScanTokens();

        // Assert
        var numberTokens = tokens.Where(t => t.Type == TokenType.Number).ToList();
        Assert.AreEqual(4, numberTokens.Count);
        Assert.AreEqual(42, numberTokens[0].Literal);
        Assert.AreEqual(3.14, numberTokens[1].Literal);
        Assert.AreEqual(0, numberTokens[2].Literal);
        Assert.AreEqual(123, numberTokens[3].Literal);
    }

    [Test]
    public void Lexer_Strings_ShouldTokenizeCorrectly()
    {
        // Arrange
        var source = @"""Hello World"" ""Test string""";
        _lexer = new CadenzaLexer(source);

        // Act
        var tokens = _lexer.ScanTokens();

        // Assert
        var stringTokens = tokens.Where(t => t.Type == TokenType.String).ToList();
        Assert.AreEqual(2, stringTokens.Count);
        Assert.AreEqual("Hello World", stringTokens[0].Literal);
        Assert.AreEqual("Test string", stringTokens[1].Literal);
    }

    [Test]
    public void Lexer_StringInterpolation_ShouldTokenizeCorrectly()
    {
        // Arrange
        var source = @"$""Hello {name}!""";
        _lexer = new CadenzaLexer(source);

        // Act
        var tokens = _lexer.ScanTokens();

        // Assert
        var interpolationTokens = tokens.Where(t => t.Type == TokenType.StringInterpolation).ToList();
        Assert.AreEqual(1, interpolationTokens.Count);
        Assert.IsNotNull(interpolationTokens[0].Literal);
    }

    [Test]
    public void Lexer_Operators_ShouldTokenizeCorrectly()
    {
        // Arrange
        var operators = new Dictionary<string, TokenType>
        {
            {"+", TokenType.Plus},
            {"-", TokenType.Minus},
            {"*", TokenType.Multiply},
            {"/", TokenType.Divide},
            {"==", TokenType.Equal},
            {"!=", TokenType.NotEqual},
            {"<", TokenType.Less},
            {">", TokenType.Greater},
            {"<=", TokenType.LessEqual},
            {">=", TokenType.GreaterEqual},
            {"&&", TokenType.And},
            {"||", TokenType.Or},
            {"!", TokenType.Not},
            {"?", TokenType.Question}
        };

        foreach (var kvp in operators)
        {
            // Act
            _lexer = new CadenzaLexer(kvp.Key);
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.AreEqual(2, tokens.Count); // operator + EOF
            Assert.AreEqual(kvp.Value, tokens[0].Type);
            Assert.AreEqual(kvp.Key, tokens[0].Lexeme);
        }
    }

    [Test]
    public void Lexer_Symbols_ShouldTokenizeCorrectly()
    {
        // Arrange
        var symbols = new Dictionary<string, TokenType>
        {
            {"(", TokenType.LeftParen},
            {")", TokenType.RightParen},
            {"{", TokenType.LeftBrace},
            {"}", TokenType.RightBrace},
            {"[", TokenType.LeftBracket},
            {"]", TokenType.RightBracket},
            {",", TokenType.Comma},
            {";", TokenType.Semicolon},
            {":", TokenType.Colon},
            {"->", TokenType.Arrow},
            {"=", TokenType.Assign},
            {".", TokenType.Dot}
        };

        foreach (var kvp in symbols)
        {
            // Act
            _lexer = new CadenzaLexer(kvp.Key);
            var tokens = _lexer.ScanTokens();

            // Assert
            Assert.AreEqual(2, tokens.Count); // symbol + EOF
            Assert.AreEqual(kvp.Value, tokens[0].Type);
            Assert.AreEqual(kvp.Key, tokens[0].Lexeme);
        }
    }

    [Test]
    public void Lexer_LineComments_ShouldBeIgnored()
    {
        // Arrange
        var source = @"
function test() // This is a comment
{
    return 42; // Another comment
}";
        _lexer = new CadenzaLexer(source);

        // Act
        var tokens = _lexer.ScanTokens();

        // Assert
        // Should not contain any comment tokens
        Assert.IsFalse(tokens.Any(t => t.Lexeme.Contains("comment")));
        Assert.IsTrue(tokens.Any(t => t.Type == TokenType.Function));
        Assert.IsTrue(tokens.Any(t => t.Type == TokenType.Return));
    }

    [Test]
    public void Lexer_BlockComments_ShouldBeIgnored()
    {
        // Arrange
        var source = @"
function test() /* This is a block comment */ {
    return 42;
}";
        _lexer = new CadenzaLexer(source);

        // Act
        var tokens = _lexer.ScanTokens();

        // Assert
        // Should not contain any comment tokens
        Assert.IsFalse(tokens.Any(t => t.Lexeme.Contains("comment")));
        Assert.IsTrue(tokens.Any(t => t.Type == TokenType.Function));
        Assert.IsTrue(tokens.Any(t => t.Type == TokenType.Return));
    }

    [Test]
    public void Lexer_SpecificationBlocks_ShouldBeTokenized()
    {
        // Arrange
        var source = @"
/*spec
This is a specification block
with multiple lines
spec*/
function test() -> int {
    return 42;
}";
        _lexer = new CadenzaLexer(source);

        // Act
        var tokens = _lexer.ScanTokens();

        // Assert
        var specTokens = tokens.Where(t => t.Type == TokenType.SpecStart).ToList();
        Assert.AreEqual(1, specTokens.Count);
        Assert.IsNotNull(specTokens[0].Literal);
        Assert.IsTrue(specTokens[0].Literal.ToString().Contains("This is a specification block"));
    }

    [Test]
    public void Lexer_MatchExpression_ShouldTokenizeCorrectly()
    {
        // Arrange
        var source = @"
match result {
    Ok(value) -> value
    Error(err) -> 0
}";
        _lexer = new CadenzaLexer(source);

        // Act
        var tokens = _lexer.ScanTokens();

        // Assert
        Assert.IsTrue(tokens.Any(t => t.Type == TokenType.Match));
        Assert.IsTrue(tokens.Any(t => t.Type == TokenType.Ok));
        Assert.IsTrue(tokens.Any(t => t.Type == TokenType.Error));
        Assert.IsTrue(tokens.Any(t => t.Type == TokenType.Arrow));
    }

    [Test]
    public void Lexer_InvalidCharacter_ShouldThrowException()
    {
        // Arrange
        var source = "function test() { @ }"; // @ is not a valid character
        _lexer = new CadenzaLexer(source);

        // Act & Assert
        Assert.Throws<System.Exception>(() => _lexer.ScanTokens());
    }

    [Test]
    public void Lexer_UnterminatedString_ShouldThrowException()
    {
        // Arrange
        var source = @"function test() { return ""unterminated string; }";
        _lexer = new CadenzaLexer(source);

        // Act & Assert
        Assert.Throws<System.Exception>(() => _lexer.ScanTokens());
    }

    [Test]
    public void Lexer_LineAndColumnNumbers_ShouldBeCorrect()
    {
        // Arrange
        var source = @"
function test() {
    return 42;
}";
        _lexer = new CadenzaLexer(source);

        // Act
        var tokens = _lexer.ScanTokens();

        // Assert
        var functionToken = tokens.First(t => t.Type == TokenType.Function);
        Assert.AreEqual(2, functionToken.Line); // function is on line 2

        var returnToken = tokens.First(t => t.Type == TokenType.Return);
        Assert.AreEqual(3, returnToken.Line); // return is on line 3
    }
}