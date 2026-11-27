
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators
{
    internal static class IncrementalValuesProviderExtensions
    {
        internal static IncrementalValueProvider<T> WithLambdaComparer<T>(this IncrementalValueProvider<T> source, Func<T?, T?, bool> equal)
        {
            var comparer = new LambdaComparer<T>(equal);
            return source.WithComparer(comparer);
        }

        internal static IncrementalValuesProvider<T> WithLambdaComparer<T>(this IncrementalValuesProvider<T> source, Func<T?, T?, bool> equal)
        {
            var comparer = new LambdaComparer<T>(equal);
            return source.WithComparer(comparer);
        }

        internal static IncrementalValuesProvider<TSource> ReportDiagnostics<TSource>(this IncrementalValuesProvider<(TSource?, Diagnostic?)> source, IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(source, (spc, source) =>
            {
                var (_, diagnostic) = source;
                if (diagnostic != null)
                {
                    spc.ReportDiagnostic(diagnostic);
                }
            });

            return source.Where((pair) => pair.Item1 != null).Select((pair, ct) => pair.Item1!);
        }

        internal static IncrementalValueProvider<TSource> ReportDiagnostics<TSource>(this IncrementalValueProvider<(TSource?, Diagnostic?)> source, IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(source, (spc, source) =>
            {
                var (_, diagnostic) = source;
                if (diagnostic != null)
                {
                    spc.ReportDiagnostic(diagnostic);
                }
            });

            return source.Select((pair, ct) => pair.Item1!);
        }

        internal static IncrementalValueProvider<TSource> ReportDiagnostics<TSource>(this IncrementalValueProvider<(TSource?, ImmutableArray<Diagnostic>)> source, IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(source, static (spc, source) =>
            {
                var (_, diagnostics) = source;
                foreach (var diagnostic in diagnostics)
                {
                    spc.ReportDiagnostic(diagnostic);
                }
            });

            return source.Select(static (pair, ct) => pair.Item1!);
        }
    }

    internal sealed class LambdaComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T?, T?, bool> _equal;

        public LambdaComparer(Func<T?, T?, bool> equal)
        {
            _equal = equal;
        }

        public bool Equals(T? x, T? y) => _equal(x, y);

        public int GetHashCode(T obj) => Assumed.Unreachable<int>();
    }
}
