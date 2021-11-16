// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Queue to store analyzer diagnostics on the <see cref="AnalyzerDriver"/>.
    /// </summary>
    internal abstract class DiagnosticQueue
    {
        public abstract bool TryComplete();
        public abstract bool TryDequeue([NotNullWhen(returnValue: true)] out Diagnostic? d);
        public abstract void Enqueue(Diagnostic diagnostic);

        // Methods specific to CategorizedDiagnosticQueue
        public abstract void EnqueueLocal(Diagnostic diagnostic, DiagnosticAnalyzer analyzer, bool isSyntaxDiagnostic);
        public abstract void EnqueueNonLocal(Diagnostic diagnostic, DiagnosticAnalyzer analyzer);
        public abstract ImmutableArray<Diagnostic> DequeueLocalSyntaxDiagnostics(DiagnosticAnalyzer analyzer);
        public abstract ImmutableArray<Diagnostic> DequeueLocalSemanticDiagnostics(DiagnosticAnalyzer analyzer);
        public abstract ImmutableArray<Diagnostic> DequeueNonLocalDiagnostics(DiagnosticAnalyzer analyzer);

        public static DiagnosticQueue Create(bool categorized = false)
        {
            return categorized ? (DiagnosticQueue)new CategorizedDiagnosticQueue() : new SimpleDiagnosticQueue();
        }

        /// <summary>
        /// Simple diagnostics queue: maintains all diagnostics reported by all analyzers in a single queue.
        /// </summary>
        private sealed class SimpleDiagnosticQueue : DiagnosticQueue
        {
            private readonly AsyncQueue<Diagnostic> _queue;

            public SimpleDiagnosticQueue()
            {
                _queue = new AsyncQueue<Diagnostic>();
            }

            public SimpleDiagnosticQueue(Diagnostic diagnostic)
            {
                _queue = new AsyncQueue<Diagnostic>();
                _queue.Enqueue(diagnostic);
            }

            public override void Enqueue(Diagnostic diagnostic)
            {
                _queue.Enqueue(diagnostic);
            }

            public override void EnqueueLocal(Diagnostic diagnostic, DiagnosticAnalyzer analyzer, bool isSyntaxDiagnostic)
            {
                _queue.Enqueue(diagnostic);
            }

            public override void EnqueueNonLocal(Diagnostic diagnostic, DiagnosticAnalyzer analyzer)
            {
                _queue.Enqueue(diagnostic);
            }

            public override ImmutableArray<Diagnostic> DequeueLocalSemanticDiagnostics(DiagnosticAnalyzer analyzer)
            {
                throw new NotImplementedException();
            }

            public override ImmutableArray<Diagnostic> DequeueLocalSyntaxDiagnostics(DiagnosticAnalyzer analyzer)
            {
                throw new NotImplementedException();
            }

            public override ImmutableArray<Diagnostic> DequeueNonLocalDiagnostics(DiagnosticAnalyzer analyzer)
            {
                throw new NotImplementedException();
            }

            public override bool TryComplete()
            {
                return _queue.TryComplete();
            }

            public override bool TryDequeue([NotNullWhen(returnValue: true)] out Diagnostic? d)
            {
                return _queue.TryDequeue(out d);
            }
        }

        /// <summary>
        /// Categorized diagnostics queue: maintains separate set of simple diagnostic queues for local semantic, local syntax and non-local diagnostics for every analyzer.
        /// </summary>
        private sealed class CategorizedDiagnosticQueue : DiagnosticQueue
        {
            private readonly object _gate = new object();
            private Dictionary<DiagnosticAnalyzer, SimpleDiagnosticQueue>? _lazyLocalSemanticDiagnostics;
            private Dictionary<DiagnosticAnalyzer, SimpleDiagnosticQueue>? _lazyLocalSyntaxDiagnostics;
            private Dictionary<DiagnosticAnalyzer, SimpleDiagnosticQueue>? _lazyNonLocalDiagnostics;

            public override void Enqueue(Diagnostic diagnostic)
            {
                throw new InvalidOperationException();
            }

            public override void EnqueueLocal(Diagnostic diagnostic, DiagnosticAnalyzer analyzer, bool isSyntaxDiagnostic)
            {
                Debug.Assert(diagnostic.Location.Kind == LocationKind.SourceFile || diagnostic.Location.Kind == LocationKind.ExternalFile);
                if (isSyntaxDiagnostic)
                {
                    EnqueueCore(ref _lazyLocalSyntaxDiagnostics, diagnostic, analyzer);
                }
                else
                {
                    EnqueueCore(ref _lazyLocalSemanticDiagnostics, diagnostic, analyzer);
                }
            }

            public override void EnqueueNonLocal(Diagnostic diagnostic, DiagnosticAnalyzer analyzer)
            {
                EnqueueCore(ref _lazyNonLocalDiagnostics, diagnostic, analyzer);
            }

            private void EnqueueCore(
                [NotNull] ref Dictionary<DiagnosticAnalyzer, SimpleDiagnosticQueue>? lazyDiagnosticsMap,
                Diagnostic diagnostic,
                DiagnosticAnalyzer analyzer)
            {
                lock (_gate)
                {
                    lazyDiagnosticsMap ??= new Dictionary<DiagnosticAnalyzer, SimpleDiagnosticQueue>();
                    EnqueueCore_NoLock(lazyDiagnosticsMap, diagnostic, analyzer);
                }
            }

            private static void EnqueueCore_NoLock(Dictionary<DiagnosticAnalyzer, SimpleDiagnosticQueue> diagnosticsMap, Diagnostic diagnostic, DiagnosticAnalyzer analyzer)
            {
                if (diagnosticsMap.TryGetValue(analyzer, out var queue))
                {
                    queue.Enqueue(diagnostic);
                }
                else
                {
                    diagnosticsMap[analyzer] = new SimpleDiagnosticQueue(diagnostic);
                }
            }

            public override bool TryComplete()
            {
                return true;
            }

            public override bool TryDequeue([NotNullWhen(returnValue: true)] out Diagnostic? d)
            {
                lock (_gate)
                {
                    return TryDequeue_NoLock(out d);
                }
            }

            private bool TryDequeue_NoLock([NotNullWhen(returnValue: true)] out Diagnostic? d)
            {
                return TryDequeue_NoLock(_lazyLocalSemanticDiagnostics, out d) ||
                    TryDequeue_NoLock(_lazyLocalSyntaxDiagnostics, out d) ||
                    TryDequeue_NoLock(_lazyNonLocalDiagnostics, out d);
            }

            private static bool TryDequeue_NoLock(Dictionary<DiagnosticAnalyzer, SimpleDiagnosticQueue>? lazyDiagnosticsMap, [NotNullWhen(returnValue: true)] out Diagnostic? d)
            {
                Diagnostic? diag = null;
                if (lazyDiagnosticsMap != null && lazyDiagnosticsMap.Any(kvp => kvp.Value.TryDequeue(out diag)))
                {
                    Debug.Assert(diag != null);
                    d = diag;
                    return true;
                }

                d = null;
                return false;
            }

            public override ImmutableArray<Diagnostic> DequeueLocalSyntaxDiagnostics(DiagnosticAnalyzer analyzer)
            {
                return DequeueDiagnosticsCore(analyzer, _lazyLocalSyntaxDiagnostics);
            }

            public override ImmutableArray<Diagnostic> DequeueLocalSemanticDiagnostics(DiagnosticAnalyzer analyzer)
            {
                return DequeueDiagnosticsCore(analyzer, _lazyLocalSemanticDiagnostics);
            }

            public override ImmutableArray<Diagnostic> DequeueNonLocalDiagnostics(DiagnosticAnalyzer analyzer)
            {
                return DequeueDiagnosticsCore(analyzer, _lazyNonLocalDiagnostics);
            }

            private ImmutableArray<Diagnostic> DequeueDiagnosticsCore(DiagnosticAnalyzer analyzer, Dictionary<DiagnosticAnalyzer, SimpleDiagnosticQueue>? lazyDiagnosticsMap)
            {
                if (TryGetDiagnosticsQueue(analyzer, lazyDiagnosticsMap, out var queue))
                {
                    var builder = ImmutableArray.CreateBuilder<Diagnostic>();
                    while (queue.TryDequeue(out var d))
                    {
                        builder.Add(d);
                    }

                    return builder.ToImmutable();
                }

                return ImmutableArray<Diagnostic>.Empty;
            }

            private bool TryGetDiagnosticsQueue(
                DiagnosticAnalyzer analyzer,
                Dictionary<DiagnosticAnalyzer, SimpleDiagnosticQueue>? diagnosticsMap,
                [NotNullWhen(returnValue: true)] out SimpleDiagnosticQueue? queue)
            {
                queue = null;

                lock (_gate)
                {
                    return diagnosticsMap != null && diagnosticsMap.TryGetValue(analyzer, out queue);
                }
            }
        }
    }
}
