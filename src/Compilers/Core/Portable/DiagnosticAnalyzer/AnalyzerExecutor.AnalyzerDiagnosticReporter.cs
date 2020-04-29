// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class AnalyzerExecutor
    {
        /// <summary>
        /// Pooled object that carries the info needed to process
        /// a reported diagnostic from a syntax node action.
        /// </summary>
        private sealed class AnalyzerDiagnosticReporter
        {
            public readonly Action<Diagnostic> AddDiagnosticAction;

            private static readonly ObjectPool<AnalyzerDiagnosticReporter> s_objectPool =
                new ObjectPool<AnalyzerDiagnosticReporter>(() => new AnalyzerDiagnosticReporter(), 10);

            public static AnalyzerDiagnosticReporter GetInstance(
                SyntaxTree contextTree,
                TextSpan? span,
                Compilation compilation,
                DiagnosticAnalyzer analyzer,
                bool isSyntaxDiagnostic,
                Action<Diagnostic> addNonCategorizedDiagnosticOpt,
                Action<Diagnostic, DiagnosticAnalyzer, bool> addCategorizedLocalDiagnosticOpt,
                Action<Diagnostic, DiagnosticAnalyzer> addCategorizedNonLocalDiagnosticOpt,
                Func<Diagnostic, DiagnosticAnalyzer, Compilation, CancellationToken, bool> shouldSuppressGeneratedCodeDiagnostic,
                CancellationToken cancellationToken)
            {
                var item = s_objectPool.Allocate();
                item._contextTree = contextTree;
                item._span = span;
                item._compilation = compilation;
                item._analyzer = analyzer;
                item._isSyntaxDiagnostic = isSyntaxDiagnostic;
                item._addNonCategorizedDiagnosticOpt = addNonCategorizedDiagnosticOpt;
                item._addCategorizedLocalDiagnosticOpt = addCategorizedLocalDiagnosticOpt;
                item._addCategorizedNonLocalDiagnosticOpt = addCategorizedNonLocalDiagnosticOpt;
                item._shouldSuppressGeneratedCodeDiagnostic = shouldSuppressGeneratedCodeDiagnostic;
                item._cancellationToken = cancellationToken;
                return item;
            }

            public void Free()
            {
                _contextTree = null!;
                _span = null;
                _compilation = null!;
                _analyzer = null!;
                _isSyntaxDiagnostic = default;
                _addNonCategorizedDiagnosticOpt = null!;
                _addCategorizedLocalDiagnosticOpt = null!;
                _addCategorizedNonLocalDiagnosticOpt = null!;
                _shouldSuppressGeneratedCodeDiagnostic = null!;
                _cancellationToken = default;
                s_objectPool.Free(this);
            }

            private SyntaxTree _contextTree;
            private TextSpan? _span;
            private Compilation _compilation;
            private DiagnosticAnalyzer _analyzer;
            private bool _isSyntaxDiagnostic;
            private Action<Diagnostic> _addNonCategorizedDiagnosticOpt;
            private Action<Diagnostic, DiagnosticAnalyzer, bool> _addCategorizedLocalDiagnosticOpt;
            private Action<Diagnostic, DiagnosticAnalyzer> _addCategorizedNonLocalDiagnosticOpt;
            private Func<Diagnostic, DiagnosticAnalyzer, Compilation, CancellationToken, bool> _shouldSuppressGeneratedCodeDiagnostic;
            private CancellationToken _cancellationToken;

            // Pooled objects are initialized in their GetInstance method
#pragma warning disable 8618
            private AnalyzerDiagnosticReporter()
            {
                AddDiagnosticAction = AddDiagnostic;
            }
#pragma warning restore 8618

            private void AddDiagnostic(Diagnostic diagnostic)
            {
                if (_shouldSuppressGeneratedCodeDiagnostic(diagnostic, _analyzer, _compilation, _cancellationToken))
                {
                    return;
                }

                if (_addCategorizedLocalDiagnosticOpt == null)
                {
                    RoslynDebug.Assert(_addNonCategorizedDiagnosticOpt != null);
                    _addNonCategorizedDiagnosticOpt(diagnostic);
                    return;
                }

                Debug.Assert(_addNonCategorizedDiagnosticOpt == null);
                RoslynDebug.Assert(_addCategorizedNonLocalDiagnosticOpt != null);

                if (diagnostic.Location.IsInSource &&
                    _contextTree == diagnostic.Location.SourceTree &&
                    (!_span.HasValue || _span.Value.IntersectsWith(diagnostic.Location.SourceSpan)))
                {
                    _addCategorizedLocalDiagnosticOpt(diagnostic, _analyzer, _isSyntaxDiagnostic);
                }
                else
                {
                    _addCategorizedNonLocalDiagnosticOpt(diagnostic, _analyzer);
                }
            }
        }
    }
}
