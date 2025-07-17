using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using NUnit.Framework;

namespace Cadenza.Tests.Framework
{
    public class QuickNameVerifier
    {
        public static void Main(string[] args)
        {
            // Load the test assembly
            var assemblyPath = Path.Combine(Directory.GetCurrentDirectory(), "tests", "bin", "Debug", "net10.0", "Cadenza.Tests.dll");
            
            if (!File.Exists(assemblyPath))
            {
                Console.WriteLine($"Assembly not found at: {assemblyPath}");
                return;
            }

            var assembly = Assembly.LoadFrom(assemblyPath);
            var testClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttributes<TestFixtureAttribute>().Any())
                .ToList();

            var badlyNamedTests = new List<string>();
            var validPrefixes = new[] { 
                "Lexer_", "Parser_", "CodeGenerator_", "Transpiler_", "GoldenFile_", "Regression_", "Discovery_",
                "Ast_", "Tokens_", "PackageManager_", "Analysis_"
            };

            foreach (var testClass in testClasses)
            {
                var testMethods = testClass.GetMethods()
                    .Where(m => m.GetCustomAttributes<TestAttribute>().Any())
                    .ToList();

                foreach (var method in testMethods)
                {
                    var name = method.Name;
                    if (!validPrefixes.Any(prefix => name.StartsWith(prefix)))
                    {
                        badlyNamedTests.Add($"{testClass.Name}.{name}");
                    }
                    else if (!name.Contains("Should") && !name.StartsWith("Discovery_") && !name.StartsWith("Regression_"))
                    {
                        badlyNamedTests.Add($"{testClass.Name}.{name}");
                    }
                }
            }

            if (badlyNamedTests.Any())
            {
                Console.WriteLine($"Found {badlyNamedTests.Count} poorly named tests:");
                foreach (var test in badlyNamedTests)
                {
                    Console.WriteLine($"  - {test}");
                }
            }
            else
            {
                Console.WriteLine("All tests are properly named!");
            }
        }
    }
}