using Cadenza.Core;
using System;
using System.Linq;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using NUnit.Framework;
using LspPosition = Microsoft.VisualStudio.LanguageServer.Protocol.Position;
using LspRange = Microsoft.VisualStudio.LanguageServer.Protocol.Range;
using LspTextDocumentContentChangeEvent = Microsoft.VisualStudio.LanguageServer.Protocol.TextDocumentContentChangeEvent;

namespace Cadenza.Tests.Unit.LSP
{
    /// <summary>
    /// Tests for DocumentManager functionality
    /// </summary>
    public class DocumentManagerTests
    {
        [Test]
        public void OpenDocument_ShouldParseAndStoreDocument()
        {
            // Arrange
            var manager = new DocumentManager();
            var uri = "file:///test.cdz";
            var content = "function test() -> int { return 42 }";

            // Act
            manager.OpenDocument(uri, content, 1);

            // Assert
            var document = manager.GetDocument(uri);
            Assert.That(document, Is.Not.Null);
            Assert.That(document.Uri, Is.EqualTo(uri));
            Assert.That(document.Content, Is.EqualTo(content));
            Assert.That(document.Version, Is.EqualTo(1));
            Assert.That(document.Tokens.Count > 0, Is.True);
        }

        [Test]
        public void UpdateDocument_ShouldApplyFullTextChange()
        {
            // Arrange
            var manager = new DocumentManager();
            var uri = "file:///test.cdz";
            var initialContent = "function test() -> int { return 42 }";
            var updatedContent = "function test() -> string { return \"hello\" }";

            manager.OpenDocument(uri, initialContent, 1);

            var changes = new[]
            {
                new Cadenza.Core.TextDocumentContentChangeEvent
                {
                    Text = updatedContent
                }
            };

            // Act
            manager.UpdateDocument(uri, changes, 2);

            // Assert
            var document = manager.GetDocument(uri);
            Assert.That(document, Is.Not.Null);
            Assert.That(document.Content, Is.EqualTo(updatedContent));
            Assert.That(document.Version, Is.EqualTo(2));
        }

        [Test]
        public void UpdateDocument_ShouldApplyIncrementalChange()
        {
            // Arrange
            var manager = new DocumentManager();
            var uri = "file:///test.cdz";
            var content = "function test() -> int { return 42 }";

            manager.OpenDocument(uri, content, 1);

            var changes = new[]
            {
                new Cadenza.Core.TextDocumentContentChangeEvent
                {
                    Range = new Cadenza.Core.Range(
                        new Cadenza.Core.Position(0, 19), // Start after "int"
                        new Cadenza.Core.Position(0, 22)  // End after "int"
                    ),
                    Text = "string"
                }
            };

            // Act
            manager.UpdateDocument(uri, changes, 2);

            // Assert
            var document = manager.GetDocument(uri);
            Assert.That(document, Is.Not.Null);
            Assert.That(document.Content, Does.Contain("string"));
            Assert.That(document.Content, Does.Not.Contain("int"));
        }

        [Test]
        public void CloseDocument_ShouldRemoveDocument()
        {
            // Arrange
            var manager = new DocumentManager();
            var uri = "file:///test.cdz";
            var content = "function test() -> int { return 42 }";

            manager.OpenDocument(uri, content, 1);
            Assert.That(manager.HasDocument(uri));

            // Act
            manager.CloseDocument(uri);

            // Assert
            Assert.That(manager.HasDocument(uri), Is.False);
            Assert.That(manager.GetDocument(uri), Is.Null);
        }

        [Test]
        public void GetTokenAtPosition_ShouldReturnCorrectToken()
        {
            // Arrange
            var manager = new DocumentManager();
            var uri = "file:///test.cdz";
            var content = "function test() -> int { return 42 }";

            manager.OpenDocument(uri, content, 1);
            var document = manager.GetDocument(uri);

            // Act
            var token = manager.GetTokenAtPosition(document!, new Cadenza.Core.Position(0, 9)); // Position at "test"

            // Assert
            Assert.That(token, Is.Not.Null);
            Assert.That(token.Lexeme, Is.EqualTo("test"));
            Assert.That(token.Type, Is.EqualTo(TokenType.Identifier));
        }

        [Test]
        public void GetWordAtPosition_ShouldReturnCorrectWord()
        {
            // Arrange
            var manager = new DocumentManager();
            var uri = "file:///test.cdz";
            var content = "function test_function() -> int { return value }";

            manager.OpenDocument(uri, content, 1);
            var document = manager.GetDocument(uri);

            // Act
            var word = manager.GetWordAtPosition(document!, new Cadenza.Core.Position(0, 12)); // Position in "test_function"

            // Assert
            Assert.That(word, Is.EqualTo("test_function"));
        }

        [Test]
        public void PositionToOffset_ShouldCalculateCorrectOffset()
        {
            // Arrange
            var document = new ManagedDocument
            {
                Content = "function test() {\n  return 42\n}"
            };

            // Act & Assert
            Assert.That(document.PositionToOffset(new Cadenza.Core.Position(0, 0)), Is.EqualTo(0)); // Start of first line
            Assert.That(document.PositionToOffset(new Cadenza.Core.Position(0, 9)), Is.EqualTo(9)); // At "test"
            Assert.That(document.PositionToOffset(new Cadenza.Core.Position(1, 0)), Is.EqualTo(18)); // Start of second line
            Assert.That(document.PositionToOffset(new Cadenza.Core.Position(1, 8)), Is.EqualTo(26)); // At "42"
        }

        [Test]
        public void LSP_DocumentManager_ShouldCalculateCorrectPosition_FromOffset()
        {
            // Arrange
            var document = new ManagedDocument
            {
                Content = "function test() {\n  return 42\n}"
            };

            // Act & Assert
            var pos1 = document.OffsetToPosition(0);
            Assert.That(pos1.Line, Is.EqualTo(0));
            Assert.That(pos1.Character, Is.EqualTo(0));

            var pos2 = document.OffsetToPosition(9);
            Assert.That(pos2.Line, Is.EqualTo(0));
            Assert.That(pos2.Character, Is.EqualTo(9));

            var pos3 = document.OffsetToPosition(18);
            Assert.That(pos3.Line, Is.EqualTo(1));
            Assert.That(pos3.Character, Is.EqualTo(0));
        }
    }
}