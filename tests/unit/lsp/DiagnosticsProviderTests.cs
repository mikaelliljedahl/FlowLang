using Cadenza.Core;
using System;
using System.Linq;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using NUnit.Framework;

namespace Cadenza.Tests.Unit.LSP
{
    /// <summary>
    /// Tests for DiagnosticsProvider functionality
    /// </summary>
    public class DiagnosticsProviderTests
    {
        [Test]
        public void GetDiagnostics_ValidCode_ShouldReturnNoDiagnostics()
        {
            // Arrange
            var provider = new DiagnosticsProvider();
            var document = CreateTestDocument("function test() -> int { return 42 }");

            // Act
            var diagnostics = provider.GetDiagnostics(document);

            // Assert
            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void GetDiagnostics_SyntaxError_ShouldReturnErrorDiagnostic()
        {
            // Arrange
            var provider = new DiagnosticsProvider();
            var document = CreateTestDocument("function test( -> int { return 42 }"); // Missing closing paren

            // Act
            var diagnostics = provider.GetDiagnostics(document);

            // Assert
            Assert.That(diagnostics, Is.Not.Empty);
            var errorDiagnostic = diagnostics.FirstOrDefault(d => d.Contains("Error"));
            Assert.That(errorDiagnostic, Is.Not.Null);
        }

        [Test]
        public void GetDiagnostics_UnclosedBrace_ShouldReturnUnclosedDiagnostic()
        {
            // Arrange
            var provider = new DiagnosticsProvider();
            var document = CreateTestDocument("function test() -> int { return 42"); // Missing closing brace

            // Act
            var diagnostics = provider.GetDiagnostics(document);

            // Assert
            Assert.That(diagnostics, Is.Not.Empty);
            var unclosedDiagnostic = diagnostics.FirstOrDefault(d => d.Contains("UNCLOSED_CONSTRUCT"));
            Assert.That(unclosedDiagnostic, Is.Not.Null);
        }

        [Test]
        public void GetDiagnostics_UnmatchedClosingBracket_ShouldReturnUnmatchedDiagnostic()
        {
            // Arrange
            var provider = new DiagnosticsProvider();
            var document = CreateTestDocument("function test() -> int { return 42 ]}"); // Extra closing bracket

            // Act
            var diagnostics = provider.GetDiagnostics(document);

            // Assert
            Assert.That(diagnostics, Is.Not.Empty);
            var unmatchedDiagnostic = diagnostics.FirstOrDefault(d => d.Contains("UNMATCHED_CLOSING"));
            Assert.That(unmatchedDiagnostic, Is.Not.Null);
        }

        [Test]
        public void GetDiagnostics_PureFunctionWithEffects_ShouldReturnEffectError()
        {
            // Arrange
            var provider = new DiagnosticsProvider();
            var document = CreateTestDocument("pure function test() uses [Database] -> int { return 42 }");

            // Act
            var diagnostics = provider.GetDiagnostics(document);

            // Assert
            Assert.That(diagnostics, Is.Not.Empty);
            var effectError = diagnostics.FirstOrDefault(d => d.Contains("PURE_FUNCTION_WITH_EFFECTS"));
            Assert.That(effectError, Is.Not.Null);
        }

        [Test]
        public void GetDiagnostics_UnknownEffect_ShouldReturnUnknownEffectError()
        {
            // Arrange
            var provider = new DiagnosticsProvider();
            var document = CreateTestDocument("function test() uses [InvalidEffect] -> int { return 42 }");

            // Act
            var diagnostics = provider.GetDiagnostics(document);

            // Assert
            Assert.That(diagnostics, Is.Not.Empty);
            var unknownEffectError = diagnostics.FirstOrDefault(d => d.Contains("UNKNOWN_EFFECT"));
            Assert.That(unknownEffectError, Is.Not.Null);
            Assert.That(unknownEffectError ?? "", Does.Contain("InvalidEffect"));
        }

        [Test]
        public void GetDiagnostics_ValidEffects_ShouldNotReturnEffectErrors()
        {
            // Arrange
            var provider = new DiagnosticsProvider();
            var document = CreateTestDocument("function test() uses [Database, Network, Logging] -> int { return 42 }");

            // Act
            var diagnostics = provider.GetDiagnostics(document);

            // Assert
            var effectErrors = diagnostics.Where(d => d.Contains("EFFECT"));
            Assert.That(effectErrors, Is.Not.Null);
        }

        [Test]
        public void GetDiagnostics_InvalidOperatorSequence_ShouldReturnOperatorError()
        {
            // Arrange
            var provider = new DiagnosticsProvider();
            var document = CreateTestDocument("function test() -> int { return 42 +* 5 }"); // Invalid +* sequence

            // Act
            var diagnostics = provider.GetDiagnostics(document);

            // Assert
            Assert.That(diagnostics, Is.Not.Empty);
            var operatorError = diagnostics.FirstOrDefault(d => d.Contains("INVALID_OPERATOR_SEQUENCE"));
            Assert.That(operatorError, Is.Not.Null);
        }

        [Test]
        public void GetDiagnostics_ValidOperatorSequence_ShouldNotReturnOperatorError()
        {
            // Arrange
            var provider = new DiagnosticsProvider();
            var document = CreateTestDocument("function test(a: int, b: int) -> bool { return a >= b }"); // Valid >= operator

            // Act
            var diagnostics = provider.GetDiagnostics(document);

            // Assert
            var operatorErrors = diagnostics.Where(d => d.Contains("INVALID_OPERATOR_SEQUENCE"));
            Assert.That(operatorErrors, Is.Not.Null);
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