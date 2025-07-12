using System;
using System.Linq;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Xunit;
using FlowLang.LSP;

namespace FlowLang.Tests.Unit.LSP
{
    /// <summary>
    /// Tests for DocumentManager functionality
    /// </summary>
    public class DocumentManagerTests
    {
        [Fact]
        public void OpenDocument_ShouldParseAndStoreDocument()
        {
            // Arrange
            var manager = new DocumentManager();
            var uri = "file:///test.flow";
            var content = "function test() -> int { return 42 }";

            // Act
            manager.OpenDocument(uri, content, 1);

            // Assert
            var document = manager.GetDocument(uri);
            Assert.NotNull(document);
            Assert.Equal(uri, document.Uri);
            Assert.Equal(content, document.Content);
            Assert.Equal(1, document.Version);
            Assert.True(document.Tokens.Count > 0);
        }

        [Fact]
        public void UpdateDocument_ShouldApplyFullTextChange()
        {
            // Arrange
            var manager = new DocumentManager();
            var uri = "file:///test.flow";
            var initialContent = "function test() -> int { return 42 }";
            var updatedContent = "function test() -> string { return \"hello\" }";

            manager.OpenDocument(uri, initialContent, 1);

            var changes = new[]
            {
                new TextDocumentContentChangeEvent
                {
                    Text = updatedContent
                }
            };

            // Act
            manager.UpdateDocument(uri, changes, 2);

            // Assert
            var document = manager.GetDocument(uri);
            Assert.NotNull(document);
            Assert.Equal(updatedContent, document.Content);
            Assert.Equal(2, document.Version);
        }

        [Fact]
        public void UpdateDocument_ShouldApplyIncrementalChange()
        {
            // Arrange
            var manager = new DocumentManager();
            var uri = "file:///test.flow";
            var content = "function test() -> int { return 42 }";

            manager.OpenDocument(uri, content, 1);

            var changes = new[]
            {
                new TextDocumentContentChangeEvent
                {
                    Range = new Range(
                        new Position(0, 19), // Start after "int"
                        new Position(0, 22)  // End after "int"
                    ),
                    Text = "string"
                }
            };

            // Act
            manager.UpdateDocument(uri, changes, 2);

            // Assert
            var document = manager.GetDocument(uri);
            Assert.NotNull(document);
            Assert.Contains("string", document.Content);
            Assert.DoesNotContain("int", document.Content);
        }

        [Fact]
        public void CloseDocument_ShouldRemoveDocument()
        {
            // Arrange
            var manager = new DocumentManager();
            var uri = "file:///test.flow";
            var content = "function test() -> int { return 42 }";

            manager.OpenDocument(uri, content, 1);
            Assert.True(manager.HasDocument(uri));

            // Act
            manager.CloseDocument(uri);

            // Assert
            Assert.False(manager.HasDocument(uri));
            Assert.Null(manager.GetDocument(uri));
        }

        [Fact]
        public void GetTokenAtPosition_ShouldReturnCorrectToken()
        {
            // Arrange
            var manager = new DocumentManager();
            var uri = "file:///test.flow";
            var content = "function test() -> int { return 42 }";

            manager.OpenDocument(uri, content, 1);
            var document = manager.GetDocument(uri);

            // Act
            var token = manager.GetTokenAtPosition(document!, new Position(0, 9)); // Position at "test"

            // Assert
            Assert.NotNull(token);
            Assert.Equal("test", token.Value);
            Assert.Equal(TokenType.Identifier, token.Type);
        }

        [Fact]
        public void GetWordAtPosition_ShouldReturnCorrectWord()
        {
            // Arrange
            var manager = new DocumentManager();
            var uri = "file:///test.flow";
            var content = "function test_function() -> int { return value }";

            manager.OpenDocument(uri, content, 1);
            var document = manager.GetDocument(uri);

            // Act
            var word = manager.GetWordAtPosition(document!, new Position(0, 12)); // Position in "test_function"

            // Assert
            Assert.Equal("test_function", word);
        }

        [Fact]
        public void PositionToOffset_ShouldCalculateCorrectOffset()
        {
            // Arrange
            var document = new ManagedDocument
            {
                Content = "function test() {\n  return 42\n}"
            };

            // Act & Assert
            Assert.Equal(0, document.PositionToOffset(new Position(0, 0))); // Start of first line
            Assert.Equal(9, document.PositionToOffset(new Position(0, 9))); // At "test"
            Assert.Equal(18, document.PositionToOffset(new Position(1, 0))); // Start of second line
            Assert.Equal(26, document.PositionToOffset(new Position(1, 8))); // At "42"
        }

        [Fact]
        public void OffsetToPosition_ShouldCalculateCorrectPosition()
        {
            // Arrange
            var document = new ManagedDocument
            {
                Content = "function test() {\n  return 42\n}"
            };

            // Act & Assert
            var pos1 = document.OffsetToPosition(0);
            Assert.Equal(0, pos1.Line);
            Assert.Equal(0, pos1.Character);

            var pos2 = document.OffsetToPosition(9);
            Assert.Equal(0, pos2.Line);
            Assert.Equal(9, pos2.Character);

            var pos3 = document.OffsetToPosition(18);
            Assert.Equal(1, pos3.Line);
            Assert.Equal(0, pos3.Character);
        }
    }
}