using System;
using System.Linq;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Xunit;
using FlowLang.LSP;

namespace FlowLang.Tests.Unit.LSP
{
    /// <summary>
    /// Tests for CompletionProvider functionality
    /// </summary>
    public class CompletionProviderTests
    {
        [Fact]
        public void GetCompletions_EmptyDocument_ShouldReturnKeywords()
        {
            // Arrange
            var provider = new CompletionProvider();
            var document = CreateTestDocument("");

            // Act
            var completions = provider.GetCompletions(document, new Position(0, 0));

            // Assert
            Assert.NotNull(completions);
            Assert.True(completions.Items.Length > 0);
            
            var functionCompletion = completions.Items.FirstOrDefault(c => c.Label == "function");
            Assert.NotNull(functionCompletion);
            Assert.Equal(CompletionItemKind.Keyword, functionCompletion.Kind);
        }

        [Fact]
        public void GetCompletions_AfterArrow_ShouldReturnTypes()
        {
            // Arrange
            var provider = new CompletionProvider();
            var document = CreateTestDocument("function test() -> ");

            // Act
            var completions = provider.GetCompletions(document, new Position(0, 19));

            // Assert
            Assert.NotNull(completions);
            
            var intCompletion = completions.Items.FirstOrDefault(c => c.Label == "int");
            Assert.NotNull(intCompletion);
            Assert.Equal(CompletionItemKind.TypeParameter, intCompletion.Kind);

            var resultCompletion = completions.Items.FirstOrDefault(c => c.Label == "Result");
            Assert.NotNull(resultCompletion);
            Assert.Equal(CompletionItemKind.TypeParameter, resultCompletion.Kind);
        }

        [Fact]
        public void GetCompletions_AfterUses_ShouldReturnEffects()
        {
            // Arrange
            var provider = new CompletionProvider();
            var document = CreateTestDocument("function test() uses [");

            // Act
            var completions = provider.GetCompletions(document, new Position(0, 22));

            // Assert
            Assert.NotNull(completions);
            
            var databaseCompletion = completions.Items.FirstOrDefault(c => c.Label == "Database");
            Assert.NotNull(databaseCompletion);
            Assert.Equal(CompletionItemKind.EnumMember, databaseCompletion.Kind);

            var networkCompletion = completions.Items.FirstOrDefault(c => c.Label == "Network");
            Assert.NotNull(networkCompletion);
            Assert.Equal(CompletionItemKind.EnumMember, networkCompletion.Kind);
        }

        [Fact]
        public void GetCompletions_AfterColon_ShouldReturnTypes()
        {
            // Arrange
            var provider = new CompletionProvider();
            var document = CreateTestDocument("function test(param: ");

            // Act
            var completions = provider.GetCompletions(document, new Position(0, 21));

            // Assert
            Assert.NotNull(completions);
            
            var stringCompletion = completions.Items.FirstOrDefault(c => c.Label == "string");
            Assert.NotNull(stringCompletion);
            Assert.Equal(CompletionItemKind.TypeParameter, stringCompletion.Kind);
        }

        [Fact]
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
            var completions = provider.GetCompletions(document, new Position(3, 11));

            // Assert
            Assert.NotNull(completions);
            
            var existingFuncCompletion = completions.Items.FirstOrDefault(c => c.Label == "existingFunc");
            Assert.NotNull(existingFuncCompletion);
            Assert.Equal(CompletionItemKind.Function, existingFuncCompletion.Kind);
        }

        [Fact]
        public void GetCompletions_WithPrefix_ShouldFilterResults()
        {
            // Arrange
            var provider = new CompletionProvider();
            var document = CreateTestDocument("fun");

            // Act
            var completions = provider.GetCompletions(document, new Position(0, 3));

            // Assert
            Assert.NotNull(completions);
            
            var functionCompletion = completions.Items.FirstOrDefault(c => c.Label == "function");
            Assert.NotNull(functionCompletion);
            
            // Should not include completions that don't start with "fun"
            var letCompletion = completions.Items.FirstOrDefault(c => c.Label == "let");
            Assert.Null(letCompletion);
        }

        [Fact]
        public void GetCompletions_InStringInterpolation_ShouldReturnIdentifiers()
        {
            // Arrange
            var provider = new CompletionProvider();
            var document = CreateTestDocument(@"
function test(name: string) -> string {
    return $""Hello {
");

            // Act
            var completions = provider.GetCompletions(document, new Position(2, 19));

            // Assert
            Assert.NotNull(completions);
            
            // Should include the parameter 'name'
            var nameCompletion = completions.Items.FirstOrDefault(c => c.Label == "name");
            Assert.NotNull(nameCompletion);
        }

        [Fact]
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
            var completions = provider.GetCompletions(document, new Position(6, 16));

            // Assert
            Assert.NotNull(completions);
            
            var addCompletion = completions.Items.FirstOrDefault(c => c.Label == "add");
            Assert.NotNull(addCompletion);
            Assert.Equal(CompletionItemKind.Function, addCompletion.Kind);
        }

        [Fact]
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
            var completions = provider.GetCompletions(document, new Position(3, 17));

            // Assert
            Assert.NotNull(completions);
            
            var calculateCompletion = completions.Items.FirstOrDefault(c => c.Label == "calculate");
            Assert.NotNull(calculateCompletion);
            Assert.Equal(CompletionItemKind.Function, calculateCompletion.Kind);
            Assert.Contains("${1:x}", calculateCompletion.InsertText ?? "");
            Assert.Contains("${2:y}", calculateCompletion.InsertText ?? "");
        }

        private ManagedDocument CreateTestDocument(string content)
        {
            var document = new ManagedDocument
            {
                Uri = "file:///test.flow",
                Content = content,
                Version = 1
            };

            // Parse the document
            try
            {
                var lexer = new FlowLangLexer(content);
                document.Tokens = lexer.Tokenize();

                var parser = new FlowLangParser(document.Tokens);
                document.AST = parser.Parse();
            }
            catch (Exception ex)
            {
                document.ParseErrors.Add(ex);
            }

            return document;
        }
    }
}