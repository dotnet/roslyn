// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Queue to store analyzer diagnostics on the <see cref="AnalyzerDriver"/>.
    /// </summary>
    internal abstract class DiagnosticQueue
    {
        public abstract bool TryComplete();
        public abstract bool TryDequeue(out Diagnostic d);
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

            public override bool TryDequeue(out Diagnostic d)
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
            private Dictionary<DiagnosticAnalyzer, SimpleDiagnosticQueue> _lazyLocalSemanticDiagnostics;
            private Dictionary<DiagnosticAnalyzer, SimpleDiagnosticQueue> _lazyLocalSyntaxDiagnostics;
            private Dictionary<DiagnosticAnalyzer, SimpleDiagnosticQueue> _lazyNonLocalDiagnostics;

            public CategorizedDiagnosticQueue()
            {
                _lazyLocalSemanticDiagnostics = null;
                _lazyLocalSyntaxDiagnostics = null;
                _lazyNonLocalDiagnostics = null;
            }

            public override void Enqueue(Diagnostic diagnostic)
            {
                throw new InvalidOperationException();
            }

            public override void EnqueueLocal(Diagnostic diagnostic, DiagnosticAnalyzer analyzer, bool isSyntaxDiagnostic)
            {
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

            private void EnqueueCore(ref Dictionary<DiagnosticAnalyzer, SimpleDiagnosticQueue> lazyDiagnosticsMap, Diagnostic diagnostic, DiagnosticAnalyzer analyzer)
            {
                lock (_gate)
                {
                    lazyDiagnosticsMap = lazyDiagnosticsMap ?? new Dictionary<DiagnosticAnalyzer, SimpleDiagnosticQueue>();
                    EnqueueCore_NoLock(lazyDiagnosticsMap, diagnostic, analyzer);
                }
            }

            private static void EnqueueCore_NoLock(Dictionary<DiagnosticAnalyzer, SimpleDiagnosticQueue> diagnosticsMap, Diagnostic diagnostic, DiagnosticAnalyzer analyzer)
            {
                SimpleDiagnosticQueue queue;
                if (diagnosticsMap.TryGetValue(analyzer, out queue))
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

            public override bool TryDequeue(out Diagnostic d)
            {
                lock (_gate)
                {
                    return TryDequeue_NoLock(out d);
                }
            }

            private bool TryDequeue_NoLock(out Diagnostic d)
            {
                return TryDequeue_NoLock(_lazyLocalSemanticDiagnostics, out d) ||
                    TryDequeue_NoLock(_lazyLocalSyntaxDiagnostics, out d) ||
                    TryDequeue_NoLock(_lazyNonLocalDiagnostics, out d);
            }

            private static bool TryDequeue_NoLock(Dictionary<DiagnosticAnalyzer, SimpleDiagnosticQueue> lazyDiagnosticsMap, out Diagnostic d)
            {
                Diagnostic diag = null;
                if (lazyDiagnosticsMap != null && lazyDiagnosticsMap.Any(kvp => kvp.Value.TryDequeue(out diag)))
                {
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

            private ImmutableArray<Diagnostic> DequeueDiagnosticsCore(DiagnosticAnalyzer analyzer, Dictionary<DiagnosticAnalyzer, SimpleDiagnosticQueue> diagnosticsMap)
            {
                SimpleDiagnosticQueue queue;
                if (TryGetDiagnosticsQueue(analyzer, diagnosticsMap, out queue))
                {
                    var builder = ImmutableArray.CreateBuilder<Diagnostic>();
                    Diagnostic d;
                    while (queue.TryDequeue(out d))
                    {
                        builder.Add(d);
                    }

                    return builder.ToImmutable();
                }

                return ImmutableArray<Diagnostic>.Empty;
            }

            private bool TryGetDiagnosticsQueue(DiagnosticAnalyzer analyzer, Dictionary<DiagnosticAnalyzer, SimpleDiagnosticQueue> diagnosticsMap, out SimpleDiagnosticQueue queue)
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
