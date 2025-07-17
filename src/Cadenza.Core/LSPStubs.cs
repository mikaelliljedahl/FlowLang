namespace Cadenza.Core;

// Simple position stub
public class Position
{
    public int Line { get; set; }
    public int Character { get; set; }
    
    public Position(int line, int character)
    {
        Line = line;
        Character = character;
    }
}

// Simple range stub
public class Range
{
    public Position Start { get; set; }
    public Position End { get; set; }
    
    public Range(Position start, Position end)
    {
        Start = start;
        End = end;
    }
}

// Simple text change stub
public class TextDocumentContentChangeEvent
{
    public Range? Range { get; set; }
    public string Text { get; set; } = "";
}

/// <summary>
/// Stub implementation for LSP completion provider
/// </summary>
public class CompletionProvider
{
    public string[] GetCompletions(ManagedDocument document, int line, int column)
    {
        return new[] { "function", "if", "else", "return" };
    }
}

/// <summary>
/// Stub implementation for LSP diagnostics provider
/// </summary>
public class DiagnosticsProvider
{
    public string[] GetDiagnostics(ManagedDocument document)
    {
        return new string[0];
    }
}

/// <summary>
/// Stub implementation for LSP document manager
/// </summary>
public class DocumentManager
{
    private readonly Dictionary<string, ManagedDocument> _documents = new();
    
    public ManagedDocument? GetDocument(string uri)
    {
        _documents.TryGetValue(uri, out var document);
        return document;
    }
    
    public void OpenDocument(string uri, string content, int version)
    {
        var document = new ManagedDocument(uri, content) { Version = version };
        
        // Parse the document
        var lexer = new CadenzaLexer(content);
        document.Tokens = lexer.ScanTokens();
        
        try
        {
            var parser = new CadenzaParser(document.Tokens);
            document.AST = parser.Parse();
        }
        catch (Exception ex)
        {
            document.ParseErrors.Add(ex.Message);
        }
        
        _documents[uri] = document;
    }
    
    public void UpdateDocument(string uri, TextDocumentContentChangeEvent[] changes, int version)
    {
        if (!_documents.TryGetValue(uri, out var document))
            return;
            
        // For simplicity, just handle full text changes
        foreach (var change in changes)
        {
            if (change.Range == null)
            {
                // Full text change
                document.Content = change.Text;
                document.Version = version;
                
                // Re-parse
                var lexer = new CadenzaLexer(document.Content);
                document.Tokens = lexer.ScanTokens();
                
                try
                {
                    var parser = new CadenzaParser(document.Tokens);
                    document.AST = parser.Parse();
                    document.ParseErrors.Clear();
                }
                catch (Exception ex)
                {
                    document.ParseErrors.Add(ex.Message);
                }
            }
        }
    }
    
    public void CloseDocument(string uri)
    {
        _documents.Remove(uri);
    }
    
    public bool HasDocument(string uri)
    {
        return _documents.ContainsKey(uri);
    }
    
    public Token? GetTokenAtPosition(ManagedDocument document, Position position)
    {
        var offset = document.PositionToOffset(position);
        // Simple implementation - find token that contains the position
        foreach (var token in document.Tokens)
        {
            if (token.Column <= position.Character && token.Line == position.Line + 1)
            {
                return token;
            }
        }
        return null;
    }
    
    public string GetWordAtPosition(ManagedDocument document, Position position)
    {
        var token = GetTokenAtPosition(document, position);
        return token?.Lexeme ?? "";
    }
}

/// <summary>
/// Stub implementation for managed document
/// </summary>
public class ManagedDocument
{
    public string Uri { get; set; } = "";
    public string Content { get; set; } = "";
    public int Version { get; set; } = 0;
    public List<Token> Tokens { get; set; } = new();
    public ProgramNode? AST { get; set; }
    public List<string> ParseErrors { get; set; } = new();
    
    public ManagedDocument()
    {
    }
    
    public ManagedDocument(string uri, string content)
    {
        Uri = uri;
        Content = content;
    }
    
    public int PositionToOffset(Position position)
    {
        var lines = Content.Split('\n');
        int offset = 0;
        for (int i = 0; i < position.Line && i < lines.Length; i++)
        {
            offset += lines[i].Length + 1; // +1 for newline
        }
        return offset + position.Character;
    }
    
    public Position OffsetToPosition(int offset)
    {
        var lines = Content.Split('\n');
        int currentOffset = 0;
        for (int line = 0; line < lines.Length; line++)
        {
            if (currentOffset + lines[line].Length >= offset)
            {
                return new Position(line, offset - currentOffset);
            }
            currentOffset += lines[line].Length + 1; // +1 for newline
        }
        return new Position(lines.Length - 1, lines.LastOrDefault()?.Length ?? 0);
    }
}

