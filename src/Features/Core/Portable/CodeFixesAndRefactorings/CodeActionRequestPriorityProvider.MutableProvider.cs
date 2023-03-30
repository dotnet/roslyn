// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions
{
    internal abstract partial class CodeActionRequestPriorityProvider
    {
        private sealed class MutableProvider : CodeActionRequestPriorityProvider
        {
            private readonly object _gate;

            /// <summary>
            /// Set of analyzers which have been de-prioritized to <see cref="CodeActionRequestPriority.Low"/> bucket.
            /// </summary>
            private ImmutableHashSet<DiagnosticAnalyzer> _lowPriorityAnalyzers = ImmutableHashSet<DiagnosticAnalyzer>.Empty;

            private MutableProvider(CodeActionRequestPriority priority, ImmutableHashSet<DiagnosticAnalyzer> lowPriorityAnalyzers)
                : base(priority)
            {
                _lowPriorityAnalyzers = lowPriorityAnalyzers;
                _gate = new();
            }

            public static new MutableProvider Create(CodeActionRequestPriority priority)
            {
                Contract.ThrowIfFalse(priority != CodeActionRequestPriority.None);
                return new(priority, ImmutableHashSet<DiagnosticAnalyzer>.Empty);
            }

            protected override bool IsDeprioritizedAnalyzerWithLowPriority(DiagnosticAnalyzer analyzer)
            {
                lock (_gate)
                {
                    return _lowPriorityAnalyzers.Contains(analyzer);
                }
            }

            public override void AddDeprioritizedAnalyzerWithLowPriority(DiagnosticAnalyzer analyzer)
            {
                lock (_gate)
                {
                    _lowPriorityAnalyzers = _lowPriorityAnalyzers.Add(analyzer);
                }
            }

            public override CodeActionRequestPriorityProvider With(CodeActionRequestPriority priority)
            {
                Contract.ThrowIfFalse(priority != CodeActionRequestPriority.None);

                lock (_gate)
                {
                    return Priority == priority ? this : new MutableProvider(priority, _lowPriorityAnalyzers);
                }
            }
        }
    }
}
