// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal static class Extensions
    {
        public static IEnumerable<RudeEditDiagnosticDescription> ToDescription(this IEnumerable<RudeEditDiagnostic> diagnostics, SourceText newSource, bool includeFirstLines)
        {
            return diagnostics.Select(d => new RudeEditDiagnosticDescription(
                d.Kind,
                d.Span == default ? null : newSource.ToString(d.Span),
                d.Arguments,
                firstLine: includeFirstLines ? newSource.Lines.GetLineFromPosition(d.Span.Start).ToString().Trim() : null));
        }

        private const string LineSeparator = "\r\n";

        public static IEnumerable<string> ToLines(this string str)
        {
            var i = 0;
            while (true)
            {
                var eoln = str.IndexOf(LineSeparator, i, StringComparison.Ordinal);
                if (eoln < 0)
                {
                    yield return str[i..];
                    yield break;
                }

                yield return str[i..eoln];
                i = eoln + LineSeparator.Length;
            }
        }
    }
}
