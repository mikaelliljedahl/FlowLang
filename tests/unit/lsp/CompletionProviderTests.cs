using Cadenza.Core;
using Cadenza.LanguageServer;
using System;
using System.Linq;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using NUnit.Framework;

namespace Cadenza.Tests.Unit.LSP
{
    /// <summary>
    /// Tests for CompletionProvider functionality
    /// </summary>
    public class CompletionProviderTests
    {
        [Test]
        public void GetCompletions_EmptyDocument_ShouldReturnKeywords()
        {
            // Arrange
            var provider = new CompletionProvider();
            var document = CreateTestDocument("");

            // Act
            var completions = provider.GetCompletions(document, 0, 0);

            // Assert
            Assert.That(completions, Is.Not.Null);
            Assert.That(completions.Length > 0, Is.True);
            
            var functionCompletion = completions.FirstOrDefault(c => c == "function");
            Assert.That(functionCompletion, Is.Not.Null);
            Assert.That(functionCompletion, Is.EqualTo("function"));
        }

        [Test]
        public void GetCompletions_AfterArrow_ShouldReturnTypes()
        {
            // Arrange
            var provider = new CompletionProvider();
            var document = CreateTestDocument("function test() -> ");

            // Act
            var completions = provider.GetCompletions(document, 0, 19);

            // Assert
            Assert.That(completions, Is.Not.Null);
            
            var intCompletion = completions.FirstOrDefault(c => c == "int");
            Assert.That(intCompletion, Is.Not.Null);
            Assert.That(intCompletion, Is.EqualTo("int"));

            var resultCompletion = completions.FirstOrDefault(c => c == "Result");
            Assert.That(resultCompletion, Is.Not.Null);
            Assert.That(resultCompletion, Is.EqualTo("Result"));
        }

        [Test]
        public void GetCompletions_AfterUses_ShouldReturnEffects()
        {
            // Arrange
            var provider = new CompletionProvider();
            var document = CreateTestDocument("function test() uses [");

            // Act
            var completions = provider.GetCompletions(document, 0, 22);

            // Assert
            Assert.That(completions, Is.Not.Null);
            
            var databaseCompletion = completions.FirstOrDefault(c => c == "Database");
            Assert.That(databaseCompletion, Is.Not.Null);
            Assert.That(databaseCompletion, Is.EqualTo("Database"));

            var networkCompletion = completions.FirstOrDefault(c => c == "Network");
            Assert.That(networkCompletion, Is.Not.Null);
            Assert.That(networkCompletion, Is.EqualTo("Network"));
        }

        [Test]
        public void GetCompletions_AfterColon_ShouldReturnTypes()
        {
            // Arrange
            var provider = new CompletionProvider();
            var document = CreateTestDocument("function test(param: ");

            // Act
            var completions = provider.GetCompletions(document, 0, 21);

            // Assert
            Assert.That(completions, Is.Not.Null);
            
            var stringCompletion = completions.FirstOrDefault(c => c == "string");
            Assert.That(stringCompletion, Is.Not.Null);
        }

        [Test]
        public void GetCompletions_WithExistingFunction_ShouldIncludeFunctionInGeneral()
        {
            // Arrange
            var provider = new CompletionProvider();
            var document = CreateTestDocument(@"
function existingFunc() -> int { return 42 }
function test() -> int { 
    return 
");

            // Act
            var completions = provider.GetCompletions(document, 3, 11);

            // Assert
            Assert.That(completions, Is.Not.Null);
            
            var existingFuncCompletion = completions.FirstOrDefault(c => c == "existingFunc");
            Assert.That(existingFuncCompletion, Is.Not.Null);
        }

        [Test]
        public void GetCompletions_WithPrefix_ShouldFilterResults()
        {
            // Arrange
            var provider = new CompletionProvider();
            var document = CreateTestDocument("fun");

            // Act
            var completions = provider.GetCompletions(document, 0, 3);

            // Assert
            Assert.That(completions, Is.Not.Null);
            
            var functionCompletion = completions.FirstOrDefault(c => c == "function");
            Assert.That(functionCompletion, Is.Not.Null);
            
            // Should not include completions that don't start with "fun"
            var letCompletion = completions.FirstOrDefault(c => c == "let");
            Assert.That(letCompletion, Is.Null);
        }

        [Test]
        public void GetCompletions_InStringInterpolation_ShouldReturnIdentifiers()
        {
            // Arrange
            var provider = new CompletionProvider();
            var document = CreateTestDocument(@"
function test(name: string) -> string {
    return $""Hello {
");

            // Act
            var completions = provider.GetCompletions(document, 2, 19);

            // Assert
            Assert.That(completions, Is.Not.Null);
            
            // Should include the parameter 'name'
            var nameCompletion = completions.FirstOrDefault(c => c == "name");
            Assert.That(nameCompletion, Is.Not.Null);
        }

        [Test]
        public void GetCompletions_ModuleAccess_ShouldReturnModuleFunctions()
        {
            // Arrange
            var provider = new CompletionProvider();
            var document = CreateTestDocument(@"
module Math {
    function add(a: int, b: int) -> int { return a + b }
}

function test() -> int {
    return Math.
");

            // Act
            var completions = provider.GetCompletions(document, 6, 16);

            // Assert
            Assert.That(completions, Is.Not.Null);
            
            var addCompletion = completions.FirstOrDefault(c => c == "add");
            Assert.That(addCompletion, Is.Not.Null);
        }

        [Test]
        public void GetCompletions_FunctionCall_ShouldIncludeFunctionSnippet()
        {
            // Arrange
            var provider = new CompletionProvider();
            var document = CreateTestDocument(@"
function calculate(x: int, y: int) -> int { return x + y }
function test() -> int {
    return calcul
");

            // Act
            var completions = provider.GetCompletions(document, 3, 17);

            // Assert
            Assert.That(completions, Is.Not.Null);
            
            var calculateCompletion = completions.FirstOrDefault(c => c == "calculate");
            Assert.That(calculateCompletion, Is.Not.Null);
        }

        private ManagedDocument CreateTestDocument(string content)
        {
            var document = new ManagedDocument
            {
                Uri = "file:///test.cdz",
                Content = content,
                Version = 1
            };

            // Parse the document
            try
            {
                var lexer = new CadenzaLexer(content);
                document.Tokens = lexer.ScanTokens();

                var parser = new CadenzaParser(document.Tokens);
                document.AST = parser.Parse();
            }
            catch (Exception ex)
            {
                document.ParseErrors.Add(ex.Message);
            }

            return document;
        }
    }
}