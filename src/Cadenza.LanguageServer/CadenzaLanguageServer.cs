using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Cadenza.Core;
using StreamJsonRpc;
using Cadenza.Core;

namespace Cadenza.LanguageServer;

/// <summary>
/// Cadenza Language Server Protocol implementation
/// Provides IDE integration for Cadenza with real-time diagnostics, completion, hover, and navigation
/// </summary>
public class CadenzaLanguageServer
{
    private readonly DocumentManager _documentManager;
    private readonly DiagnosticsProvider _diagnosticsProvider;
    private readonly CompletionProvider _completionProvider;
    private readonly HoverProvider _hoverProvider;
    private readonly DefinitionProvider _definitionProvider;
    private readonly JsonRpc _jsonRpc;
    private bool _isInitialized = false;

    public CadenzaLanguageServer(Stream input, Stream output)
    {
        _documentManager = new DocumentManager();
        _diagnosticsProvider = new DiagnosticsProvider();
        _completionProvider = new CompletionProvider();
        _hoverProvider = new HoverProvider();
        _definitionProvider = new DefinitionProvider();

        _jsonRpc = JsonRpc.Attach(output, input, this);
    }

    /// <summary>
    /// Initialize the language server with client capabilities
    /// </summary>
    [JsonRpcMethod(Methods.InitializeName)]
    public InitializeResult Initialize(InitializeParams param)
    {
        var serverCapabilities = new ServerCapabilities
        {
            TextDocumentSync = new TextDocumentSyncOptions
            {
                OpenClose = true,
                Change = TextDocumentSyncKind.Incremental,
                Save = new SaveOptions { IncludeText = true }
            },
            CompletionProvider = new CompletionOptions
            {
                ResolveProvider = false,
                TriggerCharacters = new[] { ".", "(", "[", "\"", "$" }
            },
            HoverProvider = true,
            DefinitionProvider = true,
            DocumentSymbolProvider = true,
            WorkspaceSymbolProvider = true,
            DocumentFormattingProvider = false, // TODO: Implement formatting
            DocumentRangeFormattingProvider = false,
            DocumentOnTypeFormattingProvider = null,
            RenameProvider = false, // TODO: Implement rename
            SignatureHelpProvider = new SignatureHelpOptions
            {
                TriggerCharacters = new[] { "(", "," }
            }
        };

        return new InitializeResult
        {
            Capabilities = serverCapabilities,
            ServerInfo = new ServerInfo
            {
                Name = "Cadenza Language Server",
                Version = "1.0.0"
            }
        };
    }

    /// <summary>
    /// Called after the client receives the initialize result
    /// </summary>
    [JsonRpcMethod(Methods.InitializedName)]
    public void Initialized(InitializedParams param)
    {
        _isInitialized = true;
    }

    /// <summary>
    /// Handle shutdown request from client
    /// </summary>
    [JsonRpcMethod(Methods.ShutdownName)]
    public object? Shutdown()
    {
        return null;
    }

    /// <summary>
    /// Handle exit notification from client
    /// </summary>
    [JsonRpcMethod(Methods.ExitName)]
    public void Exit()
    {
        Environment.Exit(0);
    }

    #region Document Synchronization

    /// <summary>
    /// Handle document open notifications
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentDidOpenName)]
    public async Task DidOpenTextDocument(DidOpenTextDocumentParams param)
    {
        if (!_isInitialized) return;

        var document = param.TextDocument;
        _documentManager.OpenDocument(document.Uri, document.Text, document.Version);

        // Provide initial diagnostics
        await PublishDiagnostics(document.Uri);
    }

    /// <summary>
    /// Handle document change notifications
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentDidChangeName)]
    public async Task DidChangeTextDocument(DidChangeTextDocumentParams param)
    {
        if (!_isInitialized) return;

        var document = param.TextDocument;
        _documentManager.UpdateDocument(document.Uri, param.ContentChanges, document.Version);

        // Provide updated diagnostics
        await PublishDiagnostics(document.Uri);
    }

    /// <summary>
    /// Handle document save notifications
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentDidSaveName)]
    public async Task DidSaveTextDocument(DidSaveTextDocumentParams param)
    {
        if (!_isInitialized) return;

        // Re-analyze document on save
        await PublishDiagnostics(param.TextDocument.Uri);
    }

    /// <summary>
    /// Handle document close notifications
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentDidCloseName)]
    public void DidCloseTextDocument(DidCloseTextDocumentParams param)
    {
        if (!_isInitialized) return;

        _documentManager.CloseDocument(param.TextDocument.Uri);

        // Clear diagnostics for closed document
        _jsonRpc.NotifyAsync(Methods.TextDocumentPublishDiagnosticsName, new PublishDiagnosticsParams
        {
            Uri = param.TextDocument.Uri,
            Diagnostics = Array.Empty<Diagnostic>()
        });
    }

    #endregion

    #region Language Features

    /// <summary>
    /// Provide completion suggestions
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentCompletionName)]
    public CompletionList? Completion(CompletionParams param)
    {
        if (!_isInitialized) return null;

        var document = _documentManager.GetDocument(param.TextDocument.Uri);
        if (document == null) return null;

        return _completionProvider.GetCompletions(document, param.Position);
    }

    /// <summary>
    /// Provide hover information
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentHoverName)]
    public Hover? Hover(TextDocumentPositionParams param)
    {
        if (!_isInitialized) return null;

        var document = _documentManager.GetDocument(param.TextDocument.Uri);
        if (document == null) return null;

        return _hoverProvider.GetHover(document, param.Position);
    }

    /// <summary>
    /// Provide go-to-definition functionality
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentDefinitionName)]
    public Location? Definition(TextDocumentPositionParams param)
    {
        if (!_isInitialized) return null;

        var document = _documentManager.GetDocument(param.TextDocument.Uri);
        if (document == null) return null;

        return _definitionProvider.GetDefinition(document, param.Position);
    }

    /// <summary>
    /// Provide document symbols for outline view
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentDocumentSymbolName)]
    public SymbolInformation[]? DocumentSymbol(DocumentSymbolParams param)
    {
        if (!_isInitialized) return null;

        var document = _documentManager.GetDocument(param.TextDocument.Uri);
        if (document == null) return null;

        return _definitionProvider.GetDocumentSymbols(document);
    }

    #endregion

    #region Diagnostics

    /// <summary>
    /// Analyze document and publish diagnostics to client
    /// </summary>
    private async Task PublishDiagnostics(string uri)
    {
        try
        {
            var document = _documentManager.GetDocument(uri);
            if (document == null) return;

            var diagnostics = _diagnosticsProvider.GetDiagnostics(document);

            await _jsonRpc.NotifyAsync(Methods.TextDocumentPublishDiagnosticsName, new PublishDiagnosticsParams
            {
                Uri = uri,
                Diagnostics = diagnostics
            });
        }
        catch (Exception ex)
        {
            // Log error but don't crash the server
            Console.Error.WriteLine($"Error publishing diagnostics for {uri}: {ex.Message}");
        }
    }

    #endregion

    /// <summary>
    /// Start the language server and listen for client requests
    /// </summary>
    public async Task StartAsync()
    {
        await _jsonRpc.Completion;
    }

    public void Dispose()
    {
        _jsonRpc?.Dispose();
    }
}