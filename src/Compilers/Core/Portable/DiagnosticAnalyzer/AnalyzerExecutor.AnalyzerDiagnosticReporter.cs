// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
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
                SourceOrAdditionalFile contextFile,
                TextSpan? span,
                Compilation compilation,
                DiagnosticAnalyzer analyzer,
                AnalyzerOptions analyzerOptions,
                bool isSyntaxDiagnostic,
                Action<Diagnostic, AnalyzerOptions, CancellationToken>? addNonCategorizedDiagnostic,
                Action<Diagnostic, DiagnosticAnalyzer, AnalyzerOptions, bool, CancellationToken>? addCategorizedLocalDiagnostic,
                Action<Diagnostic, DiagnosticAnalyzer, AnalyzerOptions, CancellationToken>? addCategorizedNonLocalDiagnostic,
                Func<Diagnostic, DiagnosticAnalyzer, Compilation, CancellationToken, bool> shouldSuppressGeneratedCodeDiagnostic,
                CancellationToken cancellationToken)
            {
                var item = s_objectPool.Allocate();
                item._contextFile = contextFile;
                item.FilterSpanForLocalDiagnostics = span;
                item._compilation = compilation;
                item._analyzer = analyzer;
                item._analyzerOptions = analyzerOptions;
                item._isSyntaxDiagnostic = isSyntaxDiagnostic;
                item._addNonCategorizedDiagnostic = addNonCategorizedDiagnostic;
                item._addCategorizedLocalDiagnostic = addCategorizedLocalDiagnostic;
                item._addCategorizedNonLocalDiagnostic = addCategorizedNonLocalDiagnostic;
                item._shouldSuppressGeneratedCodeDiagnostic = shouldSuppressGeneratedCodeDiagnostic;
                item._cancellationToken = cancellationToken;
                return item;
            }

            public void Free()
            {
                _contextFile = null!;
                FilterSpanForLocalDiagnostics = null;
                _compilation = null!;
                _analyzer = null!;
                _analyzerOptions = null!;
                _isSyntaxDiagnostic = default;
                _addNonCategorizedDiagnostic = null!;
                _addCategorizedLocalDiagnostic = null!;
                _addCategorizedNonLocalDiagnostic = null!;
                _shouldSuppressGeneratedCodeDiagnostic = null!;
                _cancellationToken = default;
                s_objectPool.Free(this);
            }

            private SourceOrAdditionalFile? _contextFile;
            private Compilation _compilation;
            private DiagnosticAnalyzer _analyzer;
            private AnalyzerOptions _analyzerOptions;
            private bool _isSyntaxDiagnostic;
            private Action<Diagnostic, AnalyzerOptions, CancellationToken>? _addNonCategorizedDiagnostic;
            private Action<Diagnostic, DiagnosticAnalyzer, AnalyzerOptions, bool, CancellationToken>? _addCategorizedLocalDiagnostic;
            private Action<Diagnostic, DiagnosticAnalyzer, AnalyzerOptions, CancellationToken>? _addCategorizedNonLocalDiagnostic;
            private Func<Diagnostic, DiagnosticAnalyzer, Compilation, CancellationToken, bool> _shouldSuppressGeneratedCodeDiagnostic;
            private CancellationToken _cancellationToken;

            /// <summary>
            /// An optional filter span, which if non-null, indicates that diagnostics reported within this span
            /// are considered local diagnostics, and those reported outside this span are considered non-local.
            /// 
            /// NOTE: <see cref="AnalyzerDiagnosticReporter"/> is a pooled type that is always used from a single
            /// thread, hence it is safe to expose a public mutable field.
            /// </summary>
            public TextSpan? FilterSpanForLocalDiagnostics;

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

                if (_addCategorizedLocalDiagnostic == null)
                {
                    Debug.Assert(_addNonCategorizedDiagnostic != null);
                    _addNonCategorizedDiagnostic(diagnostic, _analyzerOptions, _cancellationToken);
                    return;
                }

                Debug.Assert(_addNonCategorizedDiagnostic == null);
                Debug.Assert(_addCategorizedNonLocalDiagnostic != null);

                if (isLocalDiagnostic(diagnostic) &&
                    (!FilterSpanForLocalDiagnostics.HasValue || FilterSpanForLocalDiagnostics.Value.IntersectsWith(diagnostic.Location.SourceSpan)))
                {
                    _addCategorizedLocalDiagnostic(diagnostic, _analyzer, _analyzerOptions, _isSyntaxDiagnostic, _cancellationToken);
                }
                else
                {
                    _addCategorizedNonLocalDiagnostic(diagnostic, _analyzer, _analyzerOptions, _cancellationToken);
                }

                return;

                bool isLocalDiagnostic(Diagnostic diagnostic)
                {
                    if (diagnostic.Location.IsInSource)
                    {
                        return _contextFile?.SourceTree != null &&
                            _contextFile.Value.SourceTree == diagnostic.Location.SourceTree;
                    }

                    if (_contextFile?.AdditionalFile != null &&
                        diagnostic.Location is ExternalFileLocation externalFileLocation)
                    {
                        return PathUtilities.Comparer.Equals(_contextFile.Value.AdditionalFile.Path, externalFileLocation.GetLineSpan().Path);
                    }

                    return false;
                }
            }
        }
    }
}
