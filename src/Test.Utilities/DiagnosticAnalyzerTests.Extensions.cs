// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Test.Utilities
{
    public static class DiagnosticAnalyzerTestsExtensions
    {
        public static void Verify(
            this IEnumerable<Diagnostic> actualResults,
            DiagnosticAnalyzer analyzer,
            ITestOutputHelper output,
            string expectedDiagnosticsAssertionTemplate,
            string defaultPath,
            params DiagnosticResult[] expectedResults)
        {
            if (analyzer != null && analyzer.SupportedDiagnostics.Length == 0)
            {
                // Not implemented analyzer
                return;
            }

            int expectedCount = expectedResults.Length;
            int actualCount = actualResults.Count();

            if (expectedCount != actualCount)
            {
                string diagnosticsOutput = actualResults.Any() ? FormatDiagnostics(analyzer, actualResults) : "    NONE.";

                if (output != null)
                {
                    actualResults.Print(output, expectedDiagnosticsAssertionTemplate);
                }

                AssertFalse(
                    string.Format(CultureInfo.InvariantCulture, "Mismatch between number of diagnostics returned, expected \"{0}\" actual \"{1}\"\r\n\r\nDiagnostics:\r\n{2}\r\n", expectedCount, actualCount, diagnosticsOutput));
            }

            List<Diagnostic> actualList = actualResults.ToList();

            for (int i = 0; i < expectedResults.Length; i++)
            {
                DiagnosticResult expected = expectedResults[i].WithDefaultPath(defaultPath);

                int actualIndex = actualList.FindIndex(
                    (Diagnostic a) => IsMatch(a, isAssertEnabled: false));
                if (actualIndex >= 0)
                {
                    actualList.RemoveAt(actualIndex);
                }
                else
                {
                    if (output != null)
                    {
                        actualResults.Print(output, expectedDiagnosticsAssertionTemplate);
                    }

                    // Eh...just blow up on the first actual that's left?
                    IsMatch(actualList[0], isAssertEnabled: true);
                }

                // So we can use the same checks to look for an actual that matches an expected, or assert on actual.
                bool IsMatch(Diagnostic actual, bool isAssertEnabled)
                {
                    if (!expected.HasLocation)
                    {
                        if (actual.Location != Location.None)
                        {
                            if (isAssertEnabled)
                            {
                                AssertFalse(
                                    string.Format(CultureInfo.InvariantCulture, "Expected:\nA project diagnostic with No location\nActual:\n{0}",
                                        FormatDiagnostics(analyzer, actual)));
                            }

                            return false;
                        }
                    }
                    else
                    {
                        if (!VerifyDiagnosticLocation(analyzer, actual, actual.Location, expected.Spans[0], isAssertEnabled))
                        {
                            return false;
                        }

                        Location[] additionalLocations = actual.AdditionalLocations.ToArray();

                        if (additionalLocations.Length != expected.Spans.Length - 1)
                        {
                            if (isAssertEnabled)
                            {
                                AssertFalse(
                                    string.Format(CultureInfo.InvariantCulture, "Expected {0} additional locations but got {1} for Diagnostic:\r\n    {2}\r\n",
                                        expected.Spans.Length - 1, additionalLocations.Length,
                                        FormatDiagnostics(analyzer, actual)));
                            }
                            else
                            {
                                return false;
                            }
                        }

                        for (int j = 0; j < additionalLocations.Length; ++j)
                        {
                            if (!VerifyDiagnosticLocation(analyzer, actual, additionalLocations[j], expected.Spans[j + 1], isAssertEnabled))
                            {
                                return false;
                            }
                        }
                    }

                    if (actual.Id != expected.Id)
                    {
                        if (isAssertEnabled)
                        {
                            AssertFalse(
                                string.Format(CultureInfo.InvariantCulture, "Expected diagnostic id to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                                    expected.Id, actual.Id, FormatDiagnostics(analyzer, actual)));
                        }
                        else
                        {
                            return false;
                        }
                    }

                    if (actual.Severity != expected.Severity)
                    {
                        if (isAssertEnabled)
                        {
                            AssertFalse(
                                string.Format(CultureInfo.InvariantCulture, "Expected diagnostic severity to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                                    expected.Severity, actual.Severity, FormatDiagnostics(analyzer, actual)));
                        }
                        else
                        {
                            return false;
                        }
                    }

                    if (actual.GetMessage() != expected.Message)
                    {
                        if (isAssertEnabled)
                        {
                            AssertFalse(
                                string.Format(CultureInfo.InvariantCulture, "Expected diagnostic message to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                                    expected.Message, actual.GetMessage(), FormatDiagnostics(analyzer, actual)));
                        }
                        else
                        {
                            return false;
                        }
                    }

                    // Nothing doesn't seem to not match, so it matches.
                    return true;
                }
            }
        }

        public static void Verify(this IEnumerable<Diagnostic> actualResults, DiagnosticAnalyzer analyzer, string defaultPath, params DiagnosticResult[] expectedResults)
        {
            Verify(actualResults, analyzer, null, null, defaultPath, expectedResults);
        }

        private static void AssertFalse(
            string message)
        {
            Assert.True(false, message);
        }

        public static void Print(this IEnumerable<Diagnostic> actualResults, ITestOutputHelper output, string expectedDiagnosticsAssertionTemplate)
        {
            output.WriteLine("Actual diagnostics produced:");
            output.WriteLine("============================");
            foreach (var diagnostic in actualResults)
            {
                var actualLinePosition = diagnostic.Location.GetLineSpan().StartLinePosition;
                var message = diagnostic.GetMessage();
                var lineNumber = actualLinePosition.Line + 1;
                var columnNumber = actualLinePosition.Character + 1;
                if (expectedDiagnosticsAssertionTemplate != null)
                {
                    output.WriteLine(string.Format(CultureInfo.InvariantCulture, expectedDiagnosticsAssertionTemplate, lineNumber, columnNumber, message));
                }
                else
                {
                    output.WriteLine($"(line: {lineNumber}, column: {columnNumber}, id: {diagnostic.Id}, message: {message})");
                }
            }
        }

        /// <param name="isAssertEnabled">Indicates that unit test assertions are enabled for non-matches.</param>
        /// <returns>True if actual matches expected, false otherwise.</returns>
        private static bool VerifyDiagnosticLocation(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Location actual, DiagnosticLocation expected, bool isAssertEnabled)
        {
            FileLinePositionSpan actualSpan = actual.GetLineSpan();

            if (isAssertEnabled)
            {
                Assert.True(actualSpan.Path == expected.Span.Path || (actualSpan.Path != null && actualSpan.Path.Contains("Test0.") && expected.Span.Path.Contains("Test.")),
                    string.Format(CultureInfo.InvariantCulture, "Expected diagnostic to be in file \"{0}\" was actually in file \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                        expected.Span.Path, actualSpan.Path, FormatDiagnostics(analyzer, diagnostic)));
            }
            else if (!(actualSpan.Path == expected.Span.Path || (actualSpan.Path != null && actualSpan.Path.Contains("Test0.") && expected.Span.Path.Contains("Test."))))
            {
                return false;
            }

            Microsoft.CodeAnalysis.Text.LinePosition actualLinePosition = actualSpan.StartLinePosition;

            // Only check line position if there is an actual line in the real diagnostic
            if (expected.Span.StartLinePosition.Line > 0)
            {
                if (actualLinePosition.Line != expected.Span.StartLinePosition.Line)
                {
                    if (isAssertEnabled)
                    {
                        Assert.True(false,
                            string.Format(CultureInfo.InvariantCulture, "Expected diagnostic to be on line \"{0}\" was actually on line \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                                expected.Span.StartLinePosition.Line + 1, actualLinePosition.Line + 1, FormatDiagnostics(analyzer, diagnostic)));
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            // Only check column position if there is an actual column position in the real diagnostic
            if (expected.Span.StartLinePosition.Character > 0)
            {
                if (actualLinePosition.Character != expected.Span.StartLinePosition.Character)
                {
                    if (isAssertEnabled)
                    {
                        Assert.True(false,
                            string.Format(CultureInfo.InvariantCulture, "Expected diagnostic to start at column \"{0}\" was actually at column \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                                expected.Span.StartLinePosition.Character + 1, actualLinePosition.Character + 1, FormatDiagnostics(analyzer, diagnostic)));
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            // Matches.
            return true;
        }

        private static string FormatDiagnostics(DiagnosticAnalyzer analyzer, params Diagnostic[] diagnostics)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < diagnostics.Length; ++i)
            {
                builder.AppendLine("// " + diagnostics[i].ToString());

                Type analyzerType = analyzer.GetType();
                var ruleFields = analyzerType
                    .GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

                foreach (FieldInfo field in ruleFields)
                {
                    Debug.Assert(field.IsStatic, "field is not static");

                    if (field.GetValue(null) is DiagnosticDescriptor rule && rule.Id == diagnostics[i].Id)
                    {
                        Location location = diagnostics[i].Location;
                        if (location == Location.None)
                        {
                            builder.AppendFormat(CultureInfo.InvariantCulture, "GetGlobalResult({0}.{1})", analyzerType.Name, field.Name);
                        }
                        else
                        {
                            Assert.False(location.IsInMetadata,
                                "Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata:\r\n" + diagnostics[i]);

                            string resultMethodName = GetResultMethodName(diagnostics[i]);
                            Microsoft.CodeAnalysis.Text.LinePosition linePosition = diagnostics[i].Location.GetLineSpan().StartLinePosition;

                            builder.AppendFormat(CultureInfo.InvariantCulture, "{0}({1}, {2}, {3}.{4})",
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
