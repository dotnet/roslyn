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

        public static IEnumerable<Diagnostic> GetDiagnostics(Compilation compilation, IEnumerable<IDiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            Action<Diagnostic> addDiagnostic = diagnostics.Add;
            Action<Diagnostic> addDiagnosticWithGlobalSuppression = GetDiagnosticSinkWithSuppressionBasedOnSymbol(compilation, symbolOpt: null, addDiagnosticCore: addDiagnostic);

            var compilationAnalyzers = ArrayBuilder<ICompilationEndedAnalyzer>.GetInstance();
            foreach (var factory in analyzers.OfType<ICompilationStartedAnalyzer>())
            {
                // Catch Exception from factory.OnCompilationStarted
                ExecuteAndCatchIfThrows(factory, addDiagnostic, cancellationToken, () =>
                {
                    var a = factory.OnCompilationStarted(compilation, addDiagnosticWithGlobalSuppression, cancellationToken);
                    if (a != null && a != factory) compilationAnalyzers.Add(a);
                });
            }

            var analyzersArray = analyzers.Concat(compilationAnalyzers).ToImmutableArray();

            foreach (var tree in compilation.SyntaxTrees)
            {
                var model = compilation.GetSemanticModel(tree);
                RunAnalyzers(model, tree.GetRoot().FullSpan, analyzersArray, addDiagnostic, cancellationToken);
            }

            foreach (var a in compilationAnalyzers.Concat(analyzers.OfType<ICompilationEndedAnalyzer>()))
            {
                // Catch Exception from a.OnCompilationEnded
                ExecuteAndCatchIfThrows(a, addDiagnostic, cancellationToken, () => { a.OnCompilationEnded(compilation, addDiagnosticWithGlobalSuppression, cancellationToken); });
            }

            // Before returning diagnostics, we filter warnings
            var filteredDiagnostics = DiagnosticBag.GetInstance();
            compilation.FilterAndAppendAndFreeDiagnostics(filteredDiagnostics, ref diagnostics);
            return filteredDiagnostics.ToReadOnlyAndFree();
        }

        internal static void RunAnalyzers(
            SemanticModel model,
            TextSpan span,
            ImmutableArray<IDiagnosticAnalyzer> analyzers,
            Action<Diagnostic> addDiagnostic,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            model.RunAnalyzersCore(span, analyzers, addDiagnostic, cancellationToken);
        }

        internal static void RunAnalyzersCore<TSyntaxKind>(
            SemanticModel model,
            TextSpan span,
            ImmutableArray<IDiagnosticAnalyzer> analyzers,
            Func<SyntaxNode, TSyntaxKind> getKind,
            Action<Diagnostic> addDiagnostic,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Action<Diagnostic> addDiagnosticWithLocationFilter = d =>
            {
                if (d.Location == Location.None || d.ContainsLocation(model.SyntaxTree, span)) addDiagnostic(d);
            };

            RunAnalyzersCoreInternal(model, span, analyzers, getKind, addDiagnosticWithLocationFilter, cancellationToken);
        }

        private static void RunAnalyzersCoreInternal<TSyntaxKind>(
            SemanticModel model,
            TextSpan span,
            ImmutableArray<IDiagnosticAnalyzer> analyzers,
            Func<SyntaxNode, TSyntaxKind> getKind,
            Action<Diagnostic> addDiagnosticWithLocationFilter,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (analyzers.Length == 0) return;

            var declaredSymbolsInTree = new HashSet<ISymbol>();

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

                declaredSymbolsInTree.Add(symbol);

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
                            var addNamespaceDiagnostic = GetDiagnosticSinkWithSuppressionBasedOnSymbol(model.Compilation, ns, addDiagnosticWithLocationFilter);

                            foreach (var da in declarationAnalyzers)
                            {
                                // Catch Exception from da.SymbolKindsOfInterest and da.AnalyzeSymbol
                                ExecuteAndCatchIfThrows(da, addDiagnosticWithLocationFilter, cancellationToken, () =>
                                {
                                    if (da.SymbolKindsOfInterest.Contains(SymbolKind.Namespace))
                                    {
                                        cancellationToken.ThrowIfCancellationRequested();
                                        da.AnalyzeSymbol(ns, model.Compilation, addNamespaceDiagnostic, cancellationToken);
                                    }
                                });
                            }
                        }
                    }
                }

                var addSymbolDiagnostic = GetDiagnosticSinkWithSuppressionBasedOnSymbol(model.Compilation, symbol, addDiagnosticWithLocationFilter);
                foreach (var da in declarationAnalyzers)
                {
                    // Catch Exception from da.SymbolKindsOfInterest and da.AnalyzeSymbol
                    ExecuteAndCatchIfThrows(da, addDiagnosticWithLocationFilter, cancellationToken, () =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (da.SymbolKindsOfInterest.Contains(symbol.Kind))
                        {
                            da.AnalyzeSymbol(symbol, model.Compilation, addSymbolDiagnostic, cancellationToken);
                        }
                    });
                }
            }

            // execute the tree based analyzers.
            var addTreeAnalyzerDiagnostic = GetDiagnosticSinkWithSuppressionBasedOnLocation(model.Compilation, declaredSymbolsInTree, addDiagnosticWithLocationFilter);
            foreach (var a in analyzers.OfType<ICompilationUnitAnalyzer>())
            {
                // Catch Exception from a.AnalyzeCompilationUnit
                ExecuteAndCatchIfThrows(a, addDiagnosticWithLocationFilter, cancellationToken, () => { a.AnalyzeCompilationUnit(model.SyntaxTree, model, addTreeAnalyzerDiagnostic, cancellationToken); });
            }

            foreach (var a in analyzers.OfType<ISyntaxAnalyzer>())
            {
                // Catch Exception from a.AnalyzeTree
                ExecuteAndCatchIfThrows(a, addDiagnosticWithLocationFilter, cancellationToken, () => { a.AnalyzeTree(model.SyntaxTree, addTreeAnalyzerDiagnostic, cancellationToken); });
            }

            // execute the executable code based analyzers.
            ProcessBodies(model, analyzers, cancellationToken, declarations, addDiagnosticWithLocationFilter, getKind);
        }

        private static void ProcessBodies<TSyntaxKind>(
            SemanticModel model,
            ImmutableArray<IDiagnosticAnalyzer> analyzers,
            CancellationToken cancellationToken,
            ImmutableArray<SemanticModel.DeclarationInSpan> declarations,
            Action<Diagnostic> addDiagnostic,
            Func<SyntaxNode, TSyntaxKind> getKind)
        {
            var bodyAnalyzers = analyzers.OfType<ICodeBlockStartedAnalyzer>().ToArray();

            // process the bodies
            foreach (var d in declarations)
            {
                if (d.Body == null) continue;
                ISymbol symbol = model.GetDeclaredSymbolForNode(d.Declaration, cancellationToken);
                if (symbol == null) continue;
                var addBodyDiagnostic = GetDiagnosticSinkWithSuppressionBasedOnSymbol(model.Compilation, symbol, addDiagnostic);
                ProcessBody<TSyntaxKind>(model, analyzers, bodyAnalyzers, symbol, d.Body, cancellationToken, addBodyDiagnostic, getKind);
            }
        }

        private static void ProcessBody<TSyntaxKind>(
            SemanticModel semanticModel,
            ImmutableArray<IDiagnosticAnalyzer> analyzers,
            ICodeBlockStartedAnalyzer[] bodyAnalyzers,
            ISymbol symbol,
            SyntaxNode syntax,
            CancellationToken cancellationToken,
            Action<Diagnostic> addDiagnostic,
            Func<SyntaxNode, TSyntaxKind> getKind)
        {
            var endedAnalyzers = ArrayBuilder<ICodeBlockEndedAnalyzer>.GetInstance();
            PooledDictionary<TSyntaxKind, ArrayBuilder<ISyntaxNodeAnalyzer<TSyntaxKind>>> nodeAnalyzersByKind = null;

            foreach (var a in bodyAnalyzers)
            {
                // Catch Exception from a.OnCodeBlockStarted
                ExecuteAndCatchIfThrows(a, addDiagnostic, cancellationToken, () =>
                {
                    var analyzer = a.OnCodeBlockStarted(syntax, symbol, semanticModel, addDiagnostic, cancellationToken);
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
                            ExecuteAndCatchIfThrows(analyzer, addDiagnostic, cancellationToken, () => { analyzer.AnalyzeNode(child, semanticModel, addDiagnostic, cancellationToken); });
                        }
                    }
                }

                foreach (var b in nodeAnalyzersByKind.Values)
                {
                    b.Free();
                }
                nodeAnalyzersByKind.Free();
            }

            foreach (var a in endedAnalyzers.Concat(analyzers.OfType<ICodeBlockEndedAnalyzer>()))
            {
                // Catch Exception from a.OnCodeBlockEnded
                ExecuteAndCatchIfThrows(a, addDiagnostic, cancellationToken, () => { a.OnCodeBlockEnded(syntax, symbol, semanticModel, addDiagnostic, cancellationToken); });
            }

            endedAnalyzers.Free();
        }

       private static Action<Diagnostic> GetDiagnosticSinkWithSuppressionBasedOnSymbol(Compilation compilation, ISymbol symbolOpt, Action<Diagnostic> addDiagnosticCore)
        {
            return diagnostic =>
                {
                    var suppressMessageState = suppressMessageStateByCompilation.GetValue(compilation, (c) => new SuppressMessageAttributeState(c));
                    if (!suppressMessageState.IsDiagnosticSuppressed(diagnostic.Id, symbolOpt))
                    {
                        addDiagnosticCore(diagnostic);
                    }
                };
        }

        private static Action<Diagnostic> GetDiagnosticSinkWithSuppressionBasedOnLocation(Compilation compilation, IEnumerable<ISymbol> symbolsOfInterest, Action<Diagnostic> addDiagnosticCore)
        {
            return diagnostic =>
                {
                    var suppressMessageState = suppressMessageStateByCompilation.GetValue(compilation, (c) => new SuppressMessageAttributeState(c));

                    // Crack open suppress message for all symbols of interest.
                    // CONSIDER: We should consider replacing the below logic with actually computing the symbols of interest from the diagnostic location.
                    //           All this logic should go into IsDiagnosticSyntacticallySuppressed API.
                    foreach (var symbol in symbolsOfInterest)
                    {
                        suppressMessageState.DecodeSuppressMessageAttributes(symbol);
                    }

                    if (!suppressMessageState.IsDiagnosticSyntacticallySuppressed(diagnostic.Id, diagnostic.Location))
                    {
                        addDiagnosticCore(diagnostic);
                    }
                };
        }

        public static bool IsDiagnosticSuppressed(Compilation compilation, Diagnostic diagnostic, ISymbol associatedSymbolOpt)
        {
            var suppressMessageState = suppressMessageStateByCompilation.GetValue(compilation, (c) => new SuppressMessageAttributeState(c));
            return suppressMessageState.IsDiagnosticSuppressed(diagnostic.Id, associatedSymbolOpt);
        }

        private static void ExecuteAndCatchIfThrows(IDiagnosticAnalyzer a, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken, Action analyze)
        {
            try
            {
                analyze();
            }
            catch (OperationCanceledException oce)
            {
                if (oce.CancellationToken != cancellationToken)
                {
                    // Create a info diagnostic saying that the analyzer failed
                    addDiagnostic(GetAnalyzerDiagnostic(a, oce));
                }
            }
            catch (Exception e)
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
                DiagnosticId,
                string.Format(CodeAnalysisResources.CompilerAnalyzerThrows, analyzerName, message),
                category: DiagnosticId,
                severity: DiagnosticSeverity.Info);
        }
    }
}
