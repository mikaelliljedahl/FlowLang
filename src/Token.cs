namespace FlowLang.Compiler;

// Token types for FlowLang
public enum TokenType
{
    // Literals
    Identifier,
    Number,
    String,
    
    // Keywords
    Function,
    Return,
    If,
    Else,
    Effects,
    Pure,
    
    // Types
    Int,
    String_Type,
    Bool,
    
    // Symbols
    LeftParen,
    RightParen,
    LeftBrace,
    RightBrace,
    LeftBracket,
    RightBracket,
    Comma,
    Semicolon,
    Colon,
    Arrow,
    Assign,
    
    // Operators
    Plus,
    Minus,
    Multiply,
    Divide,
    Greater,
    Less,
    GreaterEqual,
    LessEqual,
    
    // Special
    EOF,
    Newline
}

public record Token(TokenType Type, string Value, int Line, int Column);