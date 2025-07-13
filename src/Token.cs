namespace FlowLang.Compiler;

// Token types for FlowLang
public enum TokenType
{
    // Literals
    Identifier,
    Number,
    String,
    InterpolatedString,
    
    // Keywords
    Function,
    Return,
    If,
    Else,
    For,
    In,
    Where,
    Effects,
    Pure,
    
    // Saga keywords
    Saga,
    Step,
    Compensate,
    Transaction,
    
    // UI Component keywords
    Component,
    State,
    Events,
    Render,
    OnMount,
    OnUnmount,
    OnUpdate,
    EventHandler,
    DeclareState,
    SetState,
    Container,
    
    // State management keywords
    AppState,
    Action,
    Updates,
    
    // API client keywords
    ApiClient,
    From,
    Endpoint,
    
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
    Modulo,
    Greater,
    Less,
    GreaterEqual,
    LessEqual,
    Equal,
    NotEqual,
    LogicalAnd,
    LogicalOr,
    LogicalNot,
    Question,
    Dot,
    
    // Special
    EOF,
    Newline
}

public record Token(TokenType Type, string Value, int Line, int Column);