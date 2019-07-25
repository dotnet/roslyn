// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal static class Extensions
    {
        public static void Verify(this IEnumerable<RudeEditDiagnostic> diagnostics, string newSource, params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            expectedDiagnostics = expectedDiagnostics ?? Array.Empty<RudeEditDiagnosticDescription>();
            var actualDiagnostics = diagnostics.ToDescription(newSource, expectedDiagnostics.Any(d => d.FirstLine != null)).ToArray();
            AssertEx.SetEqual(expectedDiagnostics, actualDiagnostics, itemSeparator: ",\r\n");
        }

        private static IEnumerable<RudeEditDiagnosticDescription> ToDescription(this IEnumerable<RudeEditDiagnostic> diagnostics, string newSource, bool includeFirstLines)
        {
            return diagnostics.Select(d => new RudeEditDiagnosticDescription(
                d.Kind,
                d.Span == default ? null : newSource.Substring(d.Span.Start, d.Span.Length),
                d.Arguments,
                firstLine: includeFirstLines ? GetLineAt(newSource, d.Span.Start) : null));
        }

        private const string LineSeparator = "\r\n";

        private static string GetLineAt(string source, int position)
        {
            var start = source.LastIndexOf(LineSeparator, position, position);
            var end = source.IndexOf(LineSeparator, position);
            return source.Substring(start + 1, end - start).Trim();
        }

        public static IEnumerable<string> ToLines(this string str)
        {
            var i = 0;
            while (true)
            {
                var eoln = str.IndexOf(LineSeparator, i, StringComparison.Ordinal);
                if (eoln < 0)
                {
                    yield return str.Substring(i);
                    yield break;
                }

                yield return str.Substring(i, eoln - i);
                i = eoln + LineSeparator.Length;
            }
        }
    }
}
