// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.FxCopAnalyzers;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public static class DiagnosticAnalyzerTestsExtensions
    {
        internal static void Verify(this IEnumerable<Diagnostic> actual, params DiagnosticResult[] expected)
        {
            int expectedCount = expected.Count();
            int actualCount = actual.Count();
            var diagnosticsOutput = "    NONE.";
            if (actual.Any())
            {
                diagnosticsOutput = string.Join("," + Environment.NewLine, actual.Select(a => string.Format("@\"{0}\"", a)));
            }

            Assert.True(expectedCount == actualCount,
                string.Format("Mismatch between number of diagnostics returned, expected \"{0}\" acutal \"{1}\"\r\n\r\nDiagnostics:\r\n{2}\r\n", expectedCount, actualCount, diagnosticsOutput));

            VerifyDiagnostics(actual, expected);
        }

        private static void VerifyDiagnostics(IEnumerable<Diagnostic> actualResults, DiagnosticResult[] expectedResults)
        {
            for (int i = 0; i < expectedResults.Length; i++)
            {
                var actual = actualResults.ElementAt(i);
                var expected = expectedResults[i];

                if (expected.Line == -1 && expected.Column == -1)
                {
                    Assert.True(actual.Location == Location.None,
                        string.Format("Expected:\nA project diagnostic with No location\nActual:\n{0}", actual));
                }
                else
                {
                    VerifyDiagnosticLocation(actual, actual.Location, expected.Locations.First());
                    var additionalLocations = actual.AdditionalLocations.ToArray();

                    Assert.True(additionalLocations.Length == expected.Locations.Length - 1,
                        string.Format("Expected {0} additional locations but got {1} for Diagnostic:\r\n    {2}\r\n", expected.Locations.Length - 1, additionalLocations.Length, actual));

                    for (int j = 0; j < additionalLocations.Length; ++j)
                    {
                        VerifyDiagnosticLocation(actual, additionalLocations[j], expected.Locations[j + 1]);
                    }
                }

                Assert.True(actual.Id == expected.Id,
                    string.Format("Expected diagnostic id to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                        expected.Id, actual.Id, actual));
                Assert.True(actual.Severity == expected.Severity,
                    string.Format("Expected diagnostic severity to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                        expected.Severity, actual.Severity, actual));
                Assert.True(actual.GetMessage() == expected.Message,
                    string.Format("Expected diagnostic message to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                        expected.Message, actual.GetMessage(), actual));
            }
        }

        private static void VerifyDiagnosticLocation(Diagnostic diagnostic, Location actual, DiagnosticResultLocation expected)
        {
            var actualSpan = actual.GetLineSpan();

            Assert.True(actualSpan.Path == expected.Path || (actualSpan.Path != null && actualSpan.Path.Contains("Test0.") && expected.Path.Contains("Test.")),
                string.Format("Expected diagnostic to be in file \"{0}\" was actually in file \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                    expected.Path, actualSpan.Path, diagnostic));

            var actualLinePosition = actualSpan.StartLinePosition;

            // Only check line position if there is an actual line in the real diagnostic
            if (actualLinePosition.Line > 0)
            {
                Assert.True(actualLinePosition.Line + 1 == expected.Line,
                    string.Format("Expected diagnostic to be on line \"{0}\" was actually on line \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                        expected.Line, actualLinePosition.Line + 1, diagnostic));
            }

            // Only check column position if there is an actual column position in the real diagnostic
            if (actualLinePosition.Character > 0)
            {
                Assert.True(actualLinePosition.Character + 1 == expected.Column,
                    string.Format("Expected diagnostic to start at column \"{0}\" was actually at column \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                        expected.Column, actualLinePosition.Character + 1, diagnostic));
            }
        }
    }
}