// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Test.Utilities
{
    public static class DiagnosticAnalyzerTestsExtensions
    {
        public static void Verify(
            this IEnumerable<Diagnostic> actualResults,
            DiagnosticAnalyzer analyzer,
            bool printActualDiagnosticsOnFailure,
            string expectedDiagnosticsAssertionTemplate,
            params DiagnosticResult[] expectedResults)
        {
            if (analyzer != null && analyzer.SupportedDiagnostics.Length == 0)
            {
                // Not implemented analyzer
                return;
            }

            int expectedCount = expectedResults.Count();
            int actualCount = actualResults.Count();

            if (expectedCount != actualCount)
            {
                string diagnosticsOutput = actualResults.Any() ? FormatDiagnostics(analyzer, actualResults) : "    NONE.";

                AssertFalse(
                    string.Format("Mismatch between number of diagnostics returned, expected \"{0}\" actual \"{1}\"\r\n\r\nDiagnostics:\r\n{2}\r\n", expectedCount, actualCount, diagnosticsOutput),
                    printActualDiagnosticsOnFailure,
                    expectedDiagnosticsAssertionTemplate,
                    actualResults);
            }

            for (int i = 0; i < expectedResults.Length; i++)
            {
                Diagnostic actual = actualResults.ElementAt(i);
                DiagnosticResult expected = expectedResults[i];

                if (expected.Line == -1 && expected.Column == -1)
                {
                    if (actual.Location != Location.None)
                    {
                        AssertFalse(
                            string.Format("Expected:\nA project diagnostic with No location\nActual:\n{0}",
                                FormatDiagnostics(analyzer, actual)),
                            printActualDiagnosticsOnFailure,
                            expectedDiagnosticsAssertionTemplate,
                            actualResults);
                    }
                }
                else
                {
                    VerifyDiagnosticLocation(analyzer, actual, actual.Location, expected.Locations.First());
                    Location[] additionalLocations = actual.AdditionalLocations.ToArray();

                    if (additionalLocations.Length != expected.Locations.Length - 1)
                    {
                        AssertFalse(
                            string.Format("Expected {0} additional locations but got {1} for Diagnostic:\r\n    {2}\r\n",
                                expected.Locations.Length - 1, additionalLocations.Length,
                                FormatDiagnostics(analyzer, actual)),
                            printActualDiagnosticsOnFailure,
                            expectedDiagnosticsAssertionTemplate,
                            actualResults);
                    }

                    for (int j = 0; j < additionalLocations.Length; ++j)
                    {
                        VerifyDiagnosticLocation(analyzer, actual, additionalLocations[j], expected.Locations[j + 1]);
                    }
                }

                if (actual.Id != expected.Id)
                {
                    AssertFalse(
                        string.Format("Expected diagnostic id to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Id, actual.Id, FormatDiagnostics(analyzer, actual)),
                            printActualDiagnosticsOnFailure,
                            expectedDiagnosticsAssertionTemplate,
                            actualResults);
                }

                if (actual.Severity != expected.Severity)
                {
                    AssertFalse(
                        string.Format("Expected diagnostic severity to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Severity, actual.Severity, FormatDiagnostics(analyzer, actual)),
                        printActualDiagnosticsOnFailure,
                        expectedDiagnosticsAssertionTemplate,
                        actualResults);
                }

                if (actual.GetMessage() != expected.Message)
                {
                    AssertFalse(
                        string.Format("Expected diagnostic message to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Message, actual.GetMessage(), FormatDiagnostics(analyzer, actual)),
                        printActualDiagnosticsOnFailure,
                        expectedDiagnosticsAssertionTemplate,
                        actualResults);
                }
            }
        }

        public static void Verify(this IEnumerable<Diagnostic> actualResults, DiagnosticAnalyzer analyzer, params DiagnosticResult[] expectedResults)
        {
            Verify(actualResults, analyzer, false, null, expectedResults);
        }

        private static void AssertFalse(
            string message,
            bool printActualDiagnosticsOnFailure,
            string expectedDiagnosticsAssertionTemplate,
            IEnumerable<Diagnostic> actualResults)
        {
            if (printActualDiagnosticsOnFailure)
            {
                actualResults.Print(expectedDiagnosticsAssertionTemplate);
            }

            Assert.True(false, message);
        }

        public static void Print(this IEnumerable<Diagnostic> actualResults, string expectedDiagnosticsAssertionTemplate)
        {
            Console.WriteLine("Actual diagnostics produced:");
            Console.WriteLine("============================");
            foreach (var diagnostic in actualResults)
            {
                var actualLinePosition = diagnostic.Location.GetLineSpan().StartLinePosition;
                var message = diagnostic.GetMessage();
                var lineNumber = actualLinePosition.Line + 1;
                var columnNumber = actualLinePosition.Character + 1;
                if (expectedDiagnosticsAssertionTemplate != null)
                {
                    Console.WriteLine(string.Format(expectedDiagnosticsAssertionTemplate, lineNumber, columnNumber, message));
                }
                else
                {
                    Console.WriteLine($"(line: {lineNumber}, column: {columnNumber}, message: {message})");
                }
            }
        }

        private static void VerifyDiagnosticLocation(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Location actual, DiagnosticResultLocation expected)
        {
            FileLinePositionSpan actualSpan = actual.GetLineSpan();

            Assert.True(actualSpan.Path == expected.Path || (actualSpan.Path != null && actualSpan.Path.Contains("Test0.") && expected.Path.Contains("Test.")),
                string.Format("Expected diagnostic to be in file \"{0}\" was actually in file \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                    expected.Path, actualSpan.Path, FormatDiagnostics(analyzer, diagnostic)));

            Microsoft.CodeAnalysis.Text.LinePosition actualLinePosition = actualSpan.StartLinePosition;

            // Only check line position if there is an actual line in the real diagnostic
            if (actualLinePosition.Line > 0)
            {
                if (actualLinePosition.Line + 1 != expected.Line)
                {
                    Assert.True(false,
                        string.Format("Expected diagnostic to be on line \"{0}\" was actually on line \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Line, actualLinePosition.Line + 1, FormatDiagnostics(analyzer, diagnostic)));
                }
            }

            // Only check column position if there is an actual column position in the real diagnostic
            if (actualLinePosition.Character > 0)
            {
                if (actualLinePosition.Character + 1 != expected.Column)
                {
                    Assert.True(false,
                        string.Format("Expected diagnostic to start at column \"{0}\" was actually at column \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Column, actualLinePosition.Character + 1, FormatDiagnostics(analyzer, diagnostic)));
                }
            }
        }

        private static string FormatDiagnostics(DiagnosticAnalyzer analyzer, params Diagnostic[] diagnostics)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < diagnostics.Length; ++i)
            {
                builder.AppendLine("// " + diagnostics[i].ToString());

                Type analyzerType = analyzer.GetType();
                IEnumerable<FieldInfo> ruleFields = analyzerType
                    .GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
                    .Where(f => f.IsStatic && f.FieldType == typeof(DiagnosticDescriptor));

                foreach (FieldInfo field in ruleFields)
                {
                    if (field.GetValue(null) is DiagnosticDescriptor rule && rule.Id == diagnostics[i].Id)
                    {
                        Location location = diagnostics[i].Location;
                        if (location == Location.None)
                        {
                            builder.AppendFormat("GetGlobalResult({0}.{1})", analyzerType.Name, field.Name);
                        }
                        else
                        {
                            Assert.False(location.IsInMetadata,
                                "Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata:\r\n" + diagnostics[i]);

                            string resultMethodName = GetResultMethodName(diagnostics[i]);
                            Microsoft.CodeAnalysis.Text.LinePosition linePosition = diagnostics[i].Location.GetLineSpan().StartLinePosition;

                            builder.AppendFormat("{0}({1}, {2}, {3}.{4})",
                                resultMethodName,
                                linePosition.Line + 1,
                                linePosition.Character + 1,
                                field.DeclaringType.Name,
                                field.Name);
                        }

                        if (i != diagnostics.Length - 1)
                        {
                            builder.Append(',');
                        }

                        builder.AppendLine();
                        break;
                    }
                }
            }

            return builder.ToString();
        }

        private static string GetResultMethodName(Diagnostic diagnostic)
        {
            if (diagnostic.Location.IsInSource)
            {
                return diagnostic.Location.SourceTree.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ? "GetCSharpResultAt" : "GetBasicResultAt";
            }

            return "GetResultAt";
        }

        private static string FormatDiagnostics(DiagnosticAnalyzer analyzer, IEnumerable<Diagnostic> diagnostics)
        {
            return FormatDiagnostics(analyzer, diagnostics.ToArray());
        }

        public static FileAndSource[] ToFileAndSource(this string[] sources)
        {
            return sources.Select(s => new FileAndSource() { FilePath = null, Source = s }).ToArray();
        }
    }
}
