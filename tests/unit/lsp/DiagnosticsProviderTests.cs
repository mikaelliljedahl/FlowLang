using System;
using System.Linq;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Xunit;
using Cadenza.LSP;

namespace Cadenza.Tests.Unit.LSP
{
    /// <summary>
    /// Tests for DiagnosticsProvider functionality
    /// </summary>
    public class DiagnosticsProviderTests
    {
        [Fact]
        public void GetDiagnostics_ValidCode_ShouldReturnNoDiagnostics()
        {
            // Arrange
            var provider = new DiagnosticsProvider();
            var document = CreateTestDocument("function test() -> int { return 42 }");

            // Act
            var diagnostics = provider.GetDiagnostics(document);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void GetDiagnostics_SyntaxError_ShouldReturnErrorDiagnostic()
        {
            // Arrange
            var provider = new DiagnosticsProvider();
            var document = CreateTestDocument("function test( -> int { return 42 }"); // Missing closing paren

            // Act
            var diagnostics = provider.GetDiagnostics(document);

            // Assert
            Assert.NotEmpty(diagnostics);
            var errorDiagnostic = diagnostics.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
            Assert.NotNull(errorDiagnostic);
        }

        [Fact]
        public void GetDiagnostics_UnclosedBrace_ShouldReturnUnclosedDiagnostic()
        {
            // Arrange
            var provider = new DiagnosticsProvider();
            var document = CreateTestDocument("function test() -> int { return 42"); // Missing closing brace

            // Act
            var diagnostics = provider.GetDiagnostics(document);

            // Assert
            Assert.NotEmpty(diagnostics);
            var unclosedDiagnostic = diagnostics.FirstOrDefault(d => d.Code?.ToString() == "UNCLOSED_CONSTRUCT");
            Assert.NotNull(unclosedDiagnostic);
        }

        [Fact]
        public void GetDiagnostics_UnmatchedClosingBracket_ShouldReturnUnmatchedDiagnostic()
        {
            // Arrange
            var provider = new DiagnosticsProvider();
            var document = CreateTestDocument("function test() -> int { return 42 ]}"); // Extra closing bracket

            // Act
            var diagnostics = provider.GetDiagnostics(document);

            // Assert
            Assert.NotEmpty(diagnostics);
            var unmatchedDiagnostic = diagnostics.FirstOrDefault(d => d.Code?.ToString() == "UNMATCHED_CLOSING");
            Assert.NotNull(unmatchedDiagnostic);
        }

        [Fact]
        public void GetDiagnostics_PureFunctionWithEffects_ShouldReturnEffectError()
        {
            // Arrange
            var provider = new DiagnosticsProvider();
            var document = CreateTestDocument("pure function test() uses [Database] -> int { return 42 }");

            // Act
            var diagnostics = provider.GetDiagnostics(document);

            // Assert
            Assert.NotEmpty(diagnostics);
            var effectError = diagnostics.FirstOrDefault(d => d.Code?.ToString() == "PURE_FUNCTION_WITH_EFFECTS");
            Assert.NotNull(effectError);
        }

        [Fact]
        public void GetDiagnostics_UnknownEffect_ShouldReturnUnknownEffectError()
        {
            // Arrange
            var provider = new DiagnosticsProvider();
            var document = CreateTestDocument("function test() uses [InvalidEffect] -> int { return 42 }");

            // Act
            var diagnostics = provider.GetDiagnostics(document);

            // Assert
            Assert.NotEmpty(diagnostics);
            var unknownEffectError = diagnostics.FirstOrDefault(d => d.Code?.ToString() == "UNKNOWN_EFFECT");
            Assert.NotNull(unknownEffectError);
            Assert.Contains("InvalidEffect", unknownEffectError?.Message ?? "");
        }

        [Fact]
        public void GetDiagnostics_ValidEffects_ShouldNotReturnEffectErrors()
        {
            // Arrange
            var provider = new DiagnosticsProvider();
            var document = CreateTestDocument("function test() uses [Database, Network, Logging] -> int { return 42 }");

            // Act
            var diagnostics = provider.GetDiagnostics(document);

            // Assert
            var effectErrors = diagnostics.Where(d => d.Code?.ToString()?.Contains("EFFECT") == true);
            Assert.Empty(effectErrors);
        }

        [Fact]
        public void GetDiagnostics_InvalidOperatorSequence_ShouldReturnOperatorError()
        {
            // Arrange
            var provider = new DiagnosticsProvider();
            var document = CreateTestDocument("function test() -> int { return 42 +* 5 }"); // Invalid +* sequence

            // Act
            var diagnostics = provider.GetDiagnostics(document);

            // Assert
            Assert.NotEmpty(diagnostics);
            var operatorError = diagnostics.FirstOrDefault(d => d.Code?.ToString() == "INVALID_OPERATOR_SEQUENCE");
            Assert.NotNull(operatorError);
        }

        [Fact]
        public void GetDiagnostics_ValidOperatorSequence_ShouldNotReturnOperatorError()
        {
            // Arrange
            var provider = new DiagnosticsProvider();
            var document = CreateTestDocument("function test(a: int, b: int) -> bool { return a >= b }"); // Valid >= operator

            // Act
            var diagnostics = provider.GetDiagnostics(document);

            // Assert
            var operatorErrors = diagnostics.Where(d => d.Code?.ToString() == "INVALID_OPERATOR_SEQUENCE");
            Assert.Empty(operatorErrors);
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
                document.Tokens = lexer.Tokenize();

                var parser = new CadenzaParser(document.Tokens);
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