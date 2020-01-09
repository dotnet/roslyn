// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Testing
{
    internal static class IEnumerableExtensions
    {
        private static readonly Func<object?, bool> s_notNullTest = x => x is object;

        public static DiagnosticResult[] ToOrderedArray(this IEnumerable<DiagnosticResult> diagnosticResults)
        {
            return diagnosticResults
                .OrderBy(diagnosticResult => diagnosticResult.Spans.FirstOrDefault().Span.Path, StringComparer.Ordinal)
                .ThenBy(diagnosticResult => diagnosticResult.Spans.FirstOrDefault().Span.Span.Start.Line)
                .ThenBy(diagnosticResult => diagnosticResult.Spans.FirstOrDefault().Span.Span.Start.Character)
                .ThenBy(diagnosticResult => diagnosticResult.Spans.FirstOrDefault().Span.Span.End.Line)
                .ThenBy(diagnosticResult => diagnosticResult.Spans.FirstOrDefault().Span.Span.End.Character)
                .ThenBy(diagnosticResult => diagnosticResult.Id, StringComparer.Ordinal)
                .ToArray();
        }

        internal static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
            where T : class
        {
            return source.Where<T?>(s_notNullTest)!;
        }
    }
}
