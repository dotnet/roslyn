// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public static class AnalyzerDriver
    {
        private const string DiagnosticId = "AnalyzerDriver";
        private static readonly ConditionalWeakTable<Compilation, SuppressMessageAttributeState> suppressMessageStateByCompilation = new ConditionalWeakTable<Compilation, SuppressMessageAttributeState>();

        /// <summary>
        /// Executes the given diagnostic analyzers, <paramref name="analyzers"/>, on the given <paramref name="compilation"/> and returns the generated diagnostics.
        /// <paramref name="continueOnError"/> says whether the caller would like the exception thrown by the analyzers to be handled or not. If true - Handles ; False - Not handled.
        /// </summary>
        public static IEnumerable<Diagnostic> GetDiagnostics(Compilation compilation, IEnumerable<IDiagnosticAnalyzer> analyzers, AnalyzerOptions analyzerOptions, CancellationToken cancellationToken, bool continueOnError = true)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException("compilation");
            }

            if (analyzers == null)
            {
                throw new ArgumentNullException("analyzers");
            }

            var allDiagnostics = DiagnosticBag.GetInstance();
            Action<Diagnostic> addDiagnostic = allDiagnostics.Add;
            var effectiveAnalyzers = GetEffectiveAnalyzers(analyzers, compilation.Options, addDiagnostic, continueOnError, cancellationToken);
            GetDiagnosticsCore(compilation, effectiveAnalyzers, addDiagnostic, analyzerOptions, continueOnError, cancellationToken);

            // Before returning diagnostics, we filter warnings
            var filteredDiagnostics = DiagnosticBag.GetInstance();
            compilation.FilterAndAppendAndFreeDiagnostics(filteredDiagnostics, ref allDiagnostics);
            return filteredDiagnostics.ToReadOnlyAndFree();
        }

        /// <summary>
        /// Given a set of compiler or <see cref="IDiagnosticAnalyzer"/> generated <paramref name="diagnostics"/>, returns the effective diagnostics after applying the below filters:
        /// 1) <see cref="CompilationOptions.SpecificDiagnosticOptions"/> specified for the given <paramref name="compilation"/>.
        /// 2) <see cref="CompilationOptions.GeneralDiagnosticOption"/> specified for the given <paramref name="compilation"/>.
        /// 3) Diagnostic suppression through applied <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute"/>.
        /// 4) Pragma directives for the given <paramref name="compilation"/>.
        /// </summary>
        public static IEnumerable<Diagnostic> GetEffectiveDiagnostics(IEnumerable<Diagnostic> diagnostics, Compilation compilation)
        {
            if (diagnostics == null)
            {
                throw new ArgumentNullException("diagnostics");
            }

            if (compilation == null)
            {
                throw new ArgumentNullException("compilation");
            }

            var suppressMessageState = suppressMessageStateByCompilation.GetValue(compilation, (c) => new SuppressMessageAttributeState(c));
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic != null)
                {
                    var effectiveDiagnostic = compilation.FilterDiagnostic(diagnostic);
                    if (effectiveDiagnostic != null && !suppressMessageState.IsDiagnosticSuppressed(effectiveDiagnostic.Id, effectiveDiagnostic.Location))
                    {
                        yield return effectiveDiagnostic;
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if all the diagnostics that can be produced by this analyzer are suppressed through options.
        /// <paramref name="continueOnError"/> says whether the caller would like the exception thrown by the analyzers to be handled or not. If true - Handles ; False - Not handled.
        /// </summary>
        public static bool IsDiagnosticAnalyzerSuppressed(IDiagnosticAnalyzer analyzer, CompilationOptions options, bool continueOnError = true)
        {
            if (analyzer == null)
            {
                throw new ArgumentNullException("analyzer");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            Action<Diagnostic> dummy = _ => { };
            return IsDiagnosticAnalyzerSuppressed(analyzer, options, dummy, continueOnError, CancellationToken.None);
        }

        private static ImmutableArray<IDiagnosticAnalyzer> GetEffectiveAnalyzers(IEnumerable<IDiagnosticAnalyzer> analyzers, CompilationOptions options, Action<Diagnostic> addDiagnostic, bool continueOnError, CancellationToken cancellationToken)
        {
            var effectiveAnalyzers = ImmutableArray.CreateBuilder<IDiagnosticAnalyzer>();
            foreach (var analyzer in analyzers)
            {
                if (analyzer != null && !IsDiagnosticAnalyzerSuppressed(analyzer, options, addDiagnostic, continueOnError, cancellationToken))
                {
                    effectiveAnalyzers.Add(analyzer);
                }
            }

            return effectiveAnalyzers.ToImmutable();
        }

        /// <summary>
        /// Returns true if all the diagnostics that can be produced by this analyzer are suppressed through options.
        /// </summary>
        private static bool IsDiagnosticAnalyzerSuppressed(IDiagnosticAnalyzer analyzer, CompilationOptions options, Action<Diagnostic> addDiagnostic, bool continueOnError, CancellationToken cancellationToken)
        {
            var supportedDiagnostics = ImmutableArray<DiagnosticDescriptor>.Empty;
            
            // Catch Exception from analyzer.SupportedDiagnostics
            ExecuteAndCatchIfThrows(analyzer, addDiagnostic, continueOnError, cancellationToken, () => { supportedDiagnostics = analyzer.SupportedDiagnostics; });

            var diagnosticOptions = options.SpecificDiagnosticOptions;

            foreach (var diag in supportedDiagnostics)
            {
                // Is this diagnostic suppressed by default (as written by the rule author)
                var isSuppressed = !diag.IsEnabledByDefault;

                // If the user said something about it, that overrides the author.
                if (diagnosticOptions.ContainsKey(diag.Id))
                {
                    isSuppressed = diagnosticOptions[diag.Id] == ReportDiagnostic.Suppress;
                }

                if (isSuppressed)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private static void GetDiagnosticsCore(Compilation compilation, ImmutableArray<IDiagnosticAnalyzer> analyzers, Action<Diagnostic> addDiagnostic, AnalyzerOptions analyzerOptions, bool continueOnError, CancellationToken cancellationToken)
        {
            Action<Diagnostic> addDiagnosticWithGlobalSuppression = GetDiagnosticSinkWithSuppression(compilation, addDiagnosticCore: addDiagnostic, symbolOpt: null);

            var compilationAnalyzers = ArrayBuilder<IDiagnosticAnalyzer>.GetInstance();
            foreach (var factory in analyzers.OfType<ICompilationNestedAnalyzerFactory>())
            {
                // Catch Exception from factory.OnCompilationStarted
                ExecuteAndCatchIfThrows(factory, addDiagnostic, continueOnError, cancellationToken, () =>
                {
                    var a = factory.CreateAnalyzerWithinCompilation(compilation, analyzerOptions, cancellationToken);
                    if (a != null && a != factory) compilationAnalyzers.Add(a);
                });
            }

            var analyzersArray = analyzers.Concat(compilationAnalyzers).ToImmutableArray();

            foreach (var tree in compilation.SyntaxTrees)
            {
                var model = compilation.GetSemanticModel(tree);
                RunAnalyzers(model, tree.GetRoot().FullSpan, analyzersArray, addDiagnostic, analyzerOptions, continueOnError, cancellationToken);
            }

            foreach (var a in compilationAnalyzers.Concat(analyzers.OfType<ICompilationAnalyzer>()).OfType<ICompilationAnalyzer>())
            {
                // Catch Exception from a.OnCompilationEnded
                ExecuteAndCatchIfThrows(a, addDiagnostic, continueOnError, cancellationToken, () => { a.AnalyzeCompilation(compilation, addDiagnosticWithGlobalSuppression, analyzerOptions, cancellationToken); });
            }
        }

        internal static void RunAnalyzers(
            SemanticModel model,
            TextSpan span,
            ImmutableArray<IDiagnosticAnalyzer> analyzers,
            Action<Diagnostic> addDiagnostic,
            AnalyzerOptions analyzerOptions,
            bool continueOnError,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            model.RunAnalyzersCore(span, analyzers, addDiagnostic, analyzerOptions, continueOnError, cancellationToken);
        }

        internal static void RunAnalyzersCore<TSyntaxKind>(
            SemanticModel model,
            TextSpan span,
            ImmutableArray<IDiagnosticAnalyzer> analyzers,
            Func<SyntaxNode, TSyntaxKind> getKind,
            Action<Diagnostic> addDiagnostic,
            AnalyzerOptions analyzerOptions,
            bool continueOnError,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Action<Diagnostic> addDiagnosticWithLocationFilter = d =>
            {
                if (d.Location == Location.None || d.ContainsLocation(model.SyntaxTree, span)) addDiagnostic(d);
            };

            RunAnalyzersCoreInternal(model, span, analyzers, getKind, addDiagnosticWithLocationFilter, analyzerOptions, continueOnError, cancellationToken);
        }

        private static void RunAnalyzersCoreInternal<TSyntaxKind>(
            SemanticModel model,
            TextSpan span,
            ImmutableArray<IDiagnosticAnalyzer> analyzers,
            Func<SyntaxNode, TSyntaxKind> getKind,
            Action<Diagnostic> addDiagnosticWithLocationFilter,
            AnalyzerOptions analyzerOptions,
            bool continueOnError,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (analyzers.Length == 0) return;

            // execute the symbol based analyzers.
            var declarations = model.DeclarationsInSpanInternal(span);
            var declarationAnalyzers = analyzers.OfType<ISymbolAnalyzer>();
            foreach (var d in declarations)
            {
                ISymbol symbol = model.GetDeclaredSymbolForNode(d.Declaration, cancellationToken);
                if (symbol == null ||
                    symbol.DeclaringSyntaxReferences.Length > 1 && !d.Declaration.Span.OverlapsWith(symbol.DeclaringSyntaxReferences[0].Span))
                {
                    continue;
                }

                var namespaceSymbol = symbol as INamespaceSymbol;
                if (namespaceSymbol != null)
                {
                    // process implicitly declared parent namespaces
                    for (var ns = namespaceSymbol.ContainingNamespace; ns != null; ns = ns.ContainingNamespace)
                    {
                        bool isImplicitlyDeclaredParent = ns.DeclaringSyntaxReferences
                            .Where(r =>
                                r.SyntaxTree == model.SyntaxTree &&
                                r.Span.OverlapsWith(d.Declaration.Span))
                            .FirstOrDefault() != null;

                        if (isImplicitlyDeclaredParent)
                        {
                            var addNamespaceDiagnostic = GetDiagnosticSinkWithSuppression(model.Compilation, addDiagnosticWithLocationFilter, ns);

                            foreach (var da in declarationAnalyzers)
                            {
                                // Catch Exception from da.SymbolKindsOfInterest and da.AnalyzeSymbol
                                ExecuteAndCatchIfThrows(da, addDiagnosticWithLocationFilter, continueOnError, cancellationToken, () =>
                                {
                                    if (da.SymbolKindsOfInterest.Contains(SymbolKind.Namespace))
                                    {
                                        cancellationToken.ThrowIfCancellationRequested();
                                        da.AnalyzeSymbol(ns, model.Compilation, addNamespaceDiagnostic, analyzerOptions, cancellationToken);
                                    }
                                });
                            }
                        }
                    }
                }

                var addSymbolDiagnostic = GetDiagnosticSinkWithSuppression(model.Compilation, addDiagnosticWithLocationFilter, symbol);
                foreach (var da in declarationAnalyzers)
                {
                    // Catch Exception from da.SymbolKindsOfInterest and da.AnalyzeSymbol
                    ExecuteAndCatchIfThrows(da, addDiagnosticWithLocationFilter, continueOnError, cancellationToken, () =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (da.SymbolKindsOfInterest.Contains(symbol.Kind))
                        {
                            da.AnalyzeSymbol(symbol, model.Compilation, addSymbolDiagnostic, analyzerOptions, cancellationToken);
                        }
                    });
                }
            }

            // execute the tree based analyzers.
            var addTreeAnalyzerDiagnostic = GetDiagnosticSinkWithSuppression(model.Compilation, addDiagnosticWithLocationFilter, symbolOpt: null);
            foreach (var a in analyzers.OfType<ISemanticModelAnalyzer>())
            {
                // Catch Exception from a.AnalyzeSemanticModel
                ExecuteAndCatchIfThrows(a, addDiagnosticWithLocationFilter, continueOnError, cancellationToken, () => { a.AnalyzeSemanticModel(model, addTreeAnalyzerDiagnostic, analyzerOptions, cancellationToken); });
            }

            foreach (var a in analyzers.OfType<ISyntaxTreeAnalyzer>())
            {
                // Catch Exception from a.AnalyzeSyntaxTree
                ExecuteAndCatchIfThrows(a, addDiagnosticWithLocationFilter, continueOnError, cancellationToken, () => { a.AnalyzeSyntaxTree(model.SyntaxTree, addTreeAnalyzerDiagnostic, analyzerOptions, cancellationToken); });
            }

            // execute the executable code based analyzers.
            ProcessBodies(model, analyzers, cancellationToken, declarations, addDiagnosticWithLocationFilter, analyzerOptions, continueOnError, getKind);
        }

        private static void ProcessBodies<TSyntaxKind>(
            SemanticModel model,
            ImmutableArray<IDiagnosticAnalyzer> analyzers,
            CancellationToken cancellationToken,
            ImmutableArray<SemanticModel.DeclarationInSpan> declarations,
            Action<Diagnostic> addDiagnostic,
            AnalyzerOptions analyzerOptions,
            bool continueOnError,
            Func<SyntaxNode, TSyntaxKind> getKind)
        {
            var bodyAnalyzers = analyzers.OfType<ICodeBlockNestedAnalyzerFactory>().ToArray();

            // process the bodies
            foreach (var d in declarations)
            {
                if (d.Body == null) continue;
                ISymbol symbol = model.GetDeclaredSymbolForNode(d.Declaration, cancellationToken);
                if (symbol == null) continue;
                var addBodyDiagnostic = GetDiagnosticSinkWithSuppression(model.Compilation, addDiagnostic, symbol);
                ProcessBody<TSyntaxKind>(model, analyzers, bodyAnalyzers, symbol, d.Body, cancellationToken, addBodyDiagnostic, analyzerOptions, continueOnError, getKind);
            }
        }

        private static void ProcessBody<TSyntaxKind>(
            SemanticModel semanticModel,
            ImmutableArray<IDiagnosticAnalyzer> analyzers,
            ICodeBlockNestedAnalyzerFactory[] bodyAnalyzers,
            ISymbol symbol,
            SyntaxNode syntax,
            CancellationToken cancellationToken,
            Action<Diagnostic> addDiagnostic,
            AnalyzerOptions analyzerOptions,
            bool continueOnError,
            Func<SyntaxNode, TSyntaxKind> getKind)
        {
            var endedAnalyzers = ArrayBuilder<IDiagnosticAnalyzer>.GetInstance();
            PooledDictionary<TSyntaxKind, ArrayBuilder<ISyntaxNodeAnalyzer<TSyntaxKind>>> nodeAnalyzersByKind = null;

            foreach (var a in bodyAnalyzers)
            {
                // Catch Exception from a.OnCodeBlockStarted
                ExecuteAndCatchIfThrows(a, addDiagnostic, continueOnError, cancellationToken, () =>
                {
                    var analyzer = a.CreateAnalyzerWithinCodeBlock(syntax, symbol, semanticModel, analyzerOptions, cancellationToken);
                    if (analyzer != null && analyzer != a)
                    {
                        endedAnalyzers.Add(analyzer);
                    }
                });
            }

            foreach (var nodeAnalyzer in endedAnalyzers.Concat(analyzers).OfType<ISyntaxNodeAnalyzer<TSyntaxKind>>())
            {
                // Catch Exception from  nodeAnalyzer.SyntaxKindsOfInterest
                try
                {
                    foreach (var kind in nodeAnalyzer.SyntaxKindsOfInterest)
                    {
                        if (nodeAnalyzersByKind == null) nodeAnalyzersByKind = PooledDictionary<TSyntaxKind, ArrayBuilder<ISyntaxNodeAnalyzer<TSyntaxKind>>>.GetInstance();
                        ArrayBuilder<ISyntaxNodeAnalyzer<TSyntaxKind>> analyzersForKind;
                        if (!nodeAnalyzersByKind.TryGetValue(kind, out analyzersForKind))
                        {
                            nodeAnalyzersByKind.Add(kind, analyzersForKind = ArrayBuilder<ISyntaxNodeAnalyzer<TSyntaxKind>>.GetInstance());
                        }
                        analyzersForKind.Add(nodeAnalyzer);
                    }
                }
                catch (Exception e)
                {
                    // Create a info diagnostic saying that the analyzer failed
                    addDiagnostic(GetAnalyzerDiagnostic(nodeAnalyzer, e));
                }
            }

            if (nodeAnalyzersByKind != null)
            {
                foreach (var child in syntax.DescendantNodesAndSelf())
                {
                    ArrayBuilder<ISyntaxNodeAnalyzer<TSyntaxKind>> analyzersForKind;
                    if (nodeAnalyzersByKind.TryGetValue(getKind(child), out analyzersForKind))
                    {
                        foreach (var analyzer in analyzersForKind)
                        {
                            // Catch Exception from analyzer.AnalyzeNode
                            ExecuteAndCatchIfThrows(analyzer, addDiagnostic, continueOnError, cancellationToken, () => { analyzer.AnalyzeNode(child, semanticModel, addDiagnostic, analyzerOptions, cancellationToken); });
                        }
                    }
                }

                foreach (var b in nodeAnalyzersByKind.Values)
                {
                    b.Free();
                }
                nodeAnalyzersByKind.Free();
            }

            foreach (var a in endedAnalyzers.Concat(analyzers.OfType<ICodeBlockAnalyzer>()).OfType<ICodeBlockAnalyzer>())
            {
                // Catch Exception from a.OnCodeBlockEnded
                ExecuteAndCatchIfThrows(a, addDiagnostic, continueOnError, cancellationToken, () => { a.AnalyzeCodeBlock(syntax, symbol, semanticModel, addDiagnostic, analyzerOptions, cancellationToken); });
            }

            endedAnalyzers.Free();
        }

       private static Action<Diagnostic> GetDiagnosticSinkWithSuppression(Compilation compilation, Action<Diagnostic> addDiagnosticCore, ISymbol symbolOpt)
        {
            return diagnostic =>
                {
                    var suppressMessageState = suppressMessageStateByCompilation.GetValue(compilation, (c) => new SuppressMessageAttributeState(c));
                    if (!suppressMessageState.IsDiagnosticSuppressed(diagnostic.Id, locationOpt: diagnostic.Location, symbolOpt: symbolOpt))
                    {
                        addDiagnosticCore(diagnostic);
                    }
                };
        }

        private static void ExecuteAndCatchIfThrows(IDiagnosticAnalyzer a, Action<Diagnostic> addDiagnostic, bool continueOnError, CancellationToken cancellationToken, Action analyze)
        {
            try
            {
                analyze();
            }
            catch (OperationCanceledException oce) if (continueOnError)
            {
                if (oce.CancellationToken != cancellationToken)
                {
                    // Create a info diagnostic saying that the analyzer failed
                    addDiagnostic(GetAnalyzerDiagnostic(a, oce));
                }
            }
            catch (Exception e) if (continueOnError)
            {
                // Create a info diagnostic saying that the analyzer failed
                addDiagnostic(GetAnalyzerDiagnostic(a, e));
            }
        }

        private static Diagnostic GetAnalyzerDiagnostic(IDiagnosticAnalyzer analyzer, Exception e)
        {
            return Diagnostic.Create(GetDiagnosticDescriptor(analyzer.GetType().ToString(), e.Message), Location.None);
        }

        private static DiagnosticDescriptor GetDiagnosticDescriptor(string analyzerName, string message)
        {
            return new DiagnosticDescriptor(DiagnosticId,
                CodeAnalysisResources.CompilerAnalyzerFailure,
                string.Format(CodeAnalysisResources.CompilerAnalyzerThrows, analyzerName, message),
                category: Diagnostic.CompilerDiagnosticCategory,
                defaultSeverity: DiagnosticSeverity.Info,
                isEnabledByDefault: true);
        }
    }
}
