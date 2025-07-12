using System.Collections.Generic;

namespace FlowLang.Compiler;

// AST Node definitions
public abstract record ASTNode;

public record Program(List<ASTNode> Statements) : ASTNode;

public record FunctionDeclaration(string Name, List<Parameter> Parameters, string ReturnType, List<ASTNode> Body, List<string>? Effects = null) : ASTNode;

public record Parameter(string Name, string Type);

public record ReturnStatement(ASTNode Expression) : ASTNode;

public record BinaryExpression(ASTNode Left, string Operator, ASTNode Right) : ASTNode;

public record Identifier(string Name) : ASTNode;

public record NumberLiteral(int Value) : ASTNode;