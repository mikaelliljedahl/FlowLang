using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Cadenza.Core;
using LspPosition = Microsoft.VisualStudio.LanguageServer.Protocol.Position;
using LspTextDocumentContentChangeEvent = Microsoft.VisualStudio.LanguageServer.Protocol.TextDocumentContentChangeEvent;

namespace Cadenza.LanguageServer;

/// <summary>
/// Represents a managed document with content and metadata
/// </summary>
public class ManagedDocument
{
    public string Uri { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Version { get; set; }
    public List<Token> Tokens { get; set; } = new();
    public Program? AST { get; set; }
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public List<Exception> ParseErrors { get; set; } = new();

    /// <summary>
    /// Get line content for a specific line number (0-based)
    /// </summary>
    public string GetLine(int lineNumber)
    {
        var lines = Content.Split('\n');
        return lineNumber >= 0 && lineNumber < lines.Length ? lines[lineNumber] : string.Empty;
    }

    /// <summary>
    /// Get total number of lines
    /// </summary>
    public int LineCount => Content.Split('\n').Length;

    /// <summary>
    /// Convert a Position to a character offset in the document
    /// </summary>
    public int PositionToOffset(Position position)
    {
        var lines = Content.Split('\n');
        var offset = 0;
        
        for (int i = 0; i < position.Line && i < lines.Length; i++)
        {
            offset += lines[i].Length + 1; // +1 for newline character
        }
        
        offset += Math.Min(position.Character, lines.Length > position.Line ? lines[position.Line].Length : 0);
        return offset;
    }

    /// <summary>
    /// Convert a character offset to a Position
    /// </summary>
    public Position OffsetToPosition(int offset)
    {
        var lines = Content.Split('\n');
        var currentOffset = 0;
        
        for (int line = 0; line < lines.Length; line++)
        {
            var lineLength = lines[line].Length;
            if (currentOffset + lineLength >= offset)
            {
                return new Position(line, offset - currentOffset);
            }
            currentOffset += lineLength + 1; // +1 for newline
        }
        
        // If offset is beyond document end, return last position
        return new Position(lines.Length - 1, lines.Length > 0 ? lines[^1].Length : 0);
    }
}

/// <summary>
/// Manages document state and synchronization for the LSP server
/// Tracks open documents, handles incremental updates, and maintains parsed state
/// </summary>
public class DocumentManager
{
    private readonly ConcurrentDictionary<string, ManagedDocument> _documents = new();

    /// <summary>
    /// Open a new document and parse it
    /// </summary>
    public void OpenDocument(string uri, string content, int version)
    {
        var document = new ManagedDocument
        {
            Uri = uri,
            Content = content,
            Version = version,
            LastModified = DateTime.UtcNow
        };

        ParseDocument(document);
        _documents[uri] = document;
    }

    /// <summary>
    /// Update document content with incremental changes
    /// </summary>
    public void UpdateDocument(string uri, TextDocumentContentChangeEvent[] changes, int version)
    {
        if (!_documents.TryGetValue(uri, out var document))
        {
            return; // Document not found
        }

        // Apply changes in order
        foreach (var change in changes)
        {
            if (change.Range == null)
            {
                // Full document replacement
                document.Content = change.Text;
            }
            else
            {
                // Incremental change
                ApplyIncrementalChange(document, change);
            }
        }

        document.Version = version;
        document.LastModified = DateTime.UtcNow;

        // Re-parse the document
        ParseDocument(document);
    }

    /// <summary>
    /// Close a document and remove it from management
    /// </summary>
    public void CloseDocument(string uri)
    {
        _documents.TryRemove(uri, out _);
    }

    /// <summary>
    /// Get a managed document by URI
    /// </summary>
    public ManagedDocument? GetDocument(string uri)
    {
        return _documents.TryGetValue(uri, out var document) ? document : null;
    }

    /// <summary>
    /// Get all managed documents
    /// </summary>
    public IEnumerable<ManagedDocument> GetAllDocuments()
    {
        return _documents.Values;
    }

    /// <summary>
    /// Check if a document is managed
    /// </summary>
    public bool HasDocument(string uri)
    {
        return _documents.ContainsKey(uri);
    }

    /// <summary>
    /// Apply an incremental text change to a document
    /// </summary>
    private void ApplyIncrementalChange(ManagedDocument document, TextDocumentContentChangeEvent change)
    {
        if (change.Range == null) return;

        var startOffset = document.PositionToOffset(change.Range.Start);
        var endOffset = document.PositionToOffset(change.Range.End);

        // Replace the text in the specified range
        var before = document.Content.Substring(0, startOffset);
        var after = document.Content.Substring(endOffset);
        document.Content = before + change.Text + after;
    }

    /// <summary>
    /// Parse a document and update its tokens and AST
    /// </summary>
    private void ParseDocument(ManagedDocument document)
    {
        try
        {
            // Clear previous parse errors
            document.ParseErrors.Clear();

            // Tokenize the document
            var lexer = new CadenzaLexer(document.Content);
            document.Tokens = lexer.Tokenize();

            // Parse into AST
            var parser = new CadenzaParser(document.Tokens);
            document.AST = parser.Parse();
        }
        catch (Exception ex)
        {
            // Store parse errors for diagnostics
            document.ParseErrors.Add(ex);
            document.AST = null;
        }
    }

    /// <summary>
    /// Find the token at a specific position
    /// </summary>
    public Token? GetTokenAtPosition(ManagedDocument document, Position position)
    {
        var line = position.Line + 1; // Tokens use 1-based line numbers
        var column = position.Character + 1; // Tokens use 1-based column numbers

        return document.Tokens.FirstOrDefault(token =>
            token.Line == line &&
            column >= token.Column &&
            column < token.Column + token.Value.Length);
    }

    /// <summary>
    /// Find all tokens in a specific line
    /// </summary>
    public IEnumerable<Token> GetTokensInLine(ManagedDocument document, int line)
    {
        var targetLine = line + 1; // Convert to 1-based
        return document.Tokens.Where(token => token.Line == targetLine);
    }

    /// <summary>
    /// Find the AST node at a specific position
    /// </summary>
    public ASTNode? GetASTNodeAtPosition(ManagedDocument document, Position position)
    {
        if (document.AST == null) return null;

        // This is a simplified implementation
        // In a full implementation, we'd need to track position information in AST nodes
        var token = GetTokenAtPosition(document, position);
        if (token == null) return null;

        // For now, return the AST root - this should be enhanced to find the exact node
        return document.AST;
    }

    /// <summary>
    /// Get word at position for completion and hover
    /// </summary>
    public string GetWordAtPosition(ManagedDocument document, Position position)
    {
        var line = document.GetLine(position.Line);
        if (string.IsNullOrEmpty(line)) return string.Empty;

        var start = position.Character;
        var end = position.Character;

        // Find word boundaries
        while (start > 0 && IsWordCharacter(line[start - 1]))
        {
            start--;
        }

        while (end < line.Length && IsWordCharacter(line[end]))
        {
            end++;
        }

        return start < end ? line.Substring(start, end - start) : string.Empty;
    }

    /// <summary>
    /// Check if a character is part of a word (identifier)
    /// </summary>
    private static bool IsWordCharacter(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }
}