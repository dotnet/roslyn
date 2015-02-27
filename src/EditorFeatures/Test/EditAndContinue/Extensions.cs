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
            var actualDiagnostics = diagnostics.ToDescription(newSource).ToArray();
            AssertEx.Equal(expectedDiagnostics, actualDiagnostics, itemSeparator: ",\r\n");
        }

        internal static IEnumerable<RudeEditDiagnosticDescription> ToDescription(this IEnumerable<RudeEditDiagnostic> diagnostics, string newSource)
        {
            return diagnostics.Select(d => new RudeEditDiagnosticDescription(
                d.Kind,
                d.Span == default(TextSpan) ? null : newSource.Substring(d.Span.Start, d.Span.Length),
                d.Arguments));
        }

        public static IEnumerable<string> ToLines(this string str)
        {
            const string LineSeparator = "\r\n";

            int i = 0;
            while (true)
            {
                int eoln = str.IndexOf(LineSeparator, i, StringComparison.Ordinal);
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
