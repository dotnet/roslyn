// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public abstract class FlowTestBase : SemanticModelTestBase
    {
        internal ImmutableArray<Diagnostic> FlowDiagnostics(CSharpCompilation compilation)
        {
            var flowDiagnostics = DiagnosticBag.GetInstance();
            foreach (var method in AllMethods(compilation.SourceModule.GlobalNamespace))
            {
                var sourceSymbol = method as SourceMethodSymbol;
                if (sourceSymbol == null)
                {
                    continue;
                }

                var boundBody = MethodCompiler.BindMethodBody(sourceSymbol, new TypeCompilationState(sourceSymbol.ContainingType, compilation, null), new DiagnosticBag());
                if (boundBody != null)
                {
                    FlowAnalysisPass.Rewrite(sourceSymbol, boundBody, flowDiagnostics, hasTrailingExpression: false);
                }
            }

            return flowDiagnostics.ToReadOnlyAndFree<Diagnostic>();
        }

        private IEnumerable<MethodSymbol> AllMethods(Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                    yield return symbol as MethodSymbol;
                    yield break;

                case SymbolKind.NamedType:
                    foreach (var m in (symbol as NamedTypeSymbol).GetMembers())
                    {
                        foreach (var s in AllMethods(m))
                        {
                            yield return s;
                        }
                    }
                    yield break;

                case SymbolKind.Namespace:
                    foreach (var m in (symbol as NamespaceSymbol).GetMembers())
                    {
                        foreach (var s in AllMethods(m))
                        {
                            yield return s;
                        }
                    }
                    yield break;

                // TODO: properties?
                default:
                    yield break;
            }
        }

        #region "Flow Analysis Utilities"
        protected ControlFlowAnalysis CompileAndAnalyzeControlFlowStatements(string program)
        {
            return CompileAndGetModelAndStatements(program, (model, stmt1, stmt2) => model.AnalyzeControlFlow(stmt1, stmt2));
        }

        protected DataFlowAnalysis CompileAndAnalyzeDataFlowExpression(string program)
        {
            return CompileAndGetModelAndExpression(program, (model, expression) => model.AnalyzeDataFlow(expression));
        }

        protected DataFlowAnalysis CompileAndAnalyzeDataFlowStatements(string program)
        {
            return CompileAndGetModelAndStatements(program, (model, stmt1, stmt2) => model.AnalyzeDataFlow(stmt1, stmt2));
        }

        protected Tuple<ControlFlowAnalysis, DataFlowAnalysis> CompileAndAnalyzeControlAndDataFlowStatements(string program)
        {
            return CompileAndGetModelAndStatements(program, (model, stmt1, stmt2) => Tuple.Create(model.AnalyzeControlFlow(stmt1, stmt2), model.AnalyzeDataFlow(stmt1, stmt2)));
        }

        protected T CompileAndGetModelAndExpression<T>(string program, Func<SemanticModel, ExpressionSyntax, T> analysisDelegate)
        {
            var comp = CreateCompilationWithMscorlib(program, new[] { LinqAssemblyRef });
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            int start = program.IndexOf(startString, StringComparison.Ordinal) + startString.Length;
            int end = program.IndexOf(endString, StringComparison.Ordinal);
            ExpressionSyntax syntaxToBind = null;
            foreach (var expr in GetSyntaxNodeList(tree).OfType<ExpressionSyntax>())
            {
                if (expr.SpanStart >= start && expr.Span.End <= end)
                {
                    syntaxToBind = expr;
                    break;
                }
            }

            Assert.NotNull(syntaxToBind);
            return analysisDelegate(model, syntaxToBind);
        }

        protected T CompileAndGetModelAndStatements<T>(string program, Func<SemanticModel, StatementSyntax, StatementSyntax, T> analysisDelegate)
        {
            var comp = CreateCompilationWithMscorlib(program, new[] { LinqAssemblyRef });
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            int start = program.IndexOf(startString, StringComparison.Ordinal) + startString.Length;
            int end = program.IndexOf(endString, StringComparison.Ordinal);
            StatementSyntax firstStatement = null, lastStatement = null;
            foreach (var stmt in GetSyntaxNodeList(tree).OfType<StatementSyntax>())
            {
                if (firstStatement == null && stmt.SpanStart >= start)
                {
                    firstStatement = stmt;
                }

                if (firstStatement != null && stmt.Span.End <= end && stmt.Parent == firstStatement.Parent)
                {
                    lastStatement = stmt;
                }
            }

            return analysisDelegate(model, firstStatement, lastStatement);
        }

        protected T GetFirstNode<T>(SyntaxTree tree, int offset)
            where T : CSharpSyntaxNode
        {
            return GetSyntaxNodeList(tree).OfType<T>().Where(n => n.Span.Contains(offset)).FirstOrDefault();
        }

        protected T GetLastNode<T>(SyntaxTree tree, int offset)
            where T : CSharpSyntaxNode
        {
            return GetSyntaxNodeList(tree).OfType<T>().Where(n => n.Span.Contains(offset)).Last();
        }

        protected static string GetSymbolNamesJoined<T>(IEnumerable<T> symbols) where T : ISymbol
        {
            return symbols.Any() ? string.Join(", ", symbols.Select(symbol => symbol.Name)) : null;
        }

        /// <summary>
        /// for multiple separated statements or expressions - can be nested
        /// </summary>
        /// <param name="program"></param>
        /// <param name="treeindex">syntax tree index</param>
        /// <param name="which">-1: all</param>
        /// <returns></returns>
        protected IEnumerable<ControlFlowAnalysis> CompileAndAnalyzeMultipleControlFlowStatements(string program, int treeindex = 0, int which = -1)
        {
            return CompileAndGetModelAndMultipleStatements(program, (model, stmt) => model.AnalyzeControlFlow(stmt), treeindex, which);
        }

        protected IEnumerable<DataFlowAnalysis> CompileAndAnalyzeMultipleDataFlowStatements(string program, int treeindex = 0, int which = -1)
        {
            return CompileAndGetModelAndMultipleStatements(program, (model, stmt) => model.AnalyzeDataFlow(stmt), treeindex, which);
        }

        protected IEnumerable<DataFlowAnalysis> CompileAndAnalyzeDataFlowMultipleExpressions(string program, int treeindex = 0, int which = -1)
        {
            return CompileAndGetModelAndMultipleExpressions(program, (model, expression) => model.AnalyzeDataFlow(expression), treeindex, which);
        }

        protected Tuple<IEnumerable<ControlFlowAnalysis>, IEnumerable<DataFlowAnalysis>> CompileAndAnalyzeControlAndDataFlowMultipleStatements(string program, int treeindex = 0, int which = -1)
        {
            return Tuple.Create(CompileAndAnalyzeMultipleControlFlowStatements(program, treeindex, which), CompileAndAnalyzeMultipleDataFlowStatements(program, treeindex, which));
        }

        protected IEnumerable<T> CompileAndGetModelAndMultipleExpressions<T>(string program, Func<SemanticModel, ExpressionSyntax, T> analysisDelegate, int treeindex = 0, int which = -1)
        {
            var comp = CreateCompilationWithMscorlib(program, new[] { LinqAssemblyRef });
            var tuple = GetBindingNodesAndModel<ExpressionSyntax>(comp, treeindex, which);

            foreach (var expr in tuple.Item1)
            {
                yield return analysisDelegate(tuple.Item2, expr);
            }
        }

        protected IEnumerable<T> CompileAndGetModelAndMultipleStatements<T>(string program, Func<SemanticModel, StatementSyntax, T> analysisDelegate, int treeindex = 0, int which = -1)
        {
            var comp = CreateCompilationWithMscorlib(program, new[] { LinqAssemblyRef });
            var tuple = GetBindingNodesAndModel<StatementSyntax>(comp, treeindex, which);

            foreach (var stmt in tuple.Item1)
            {
                yield return analysisDelegate(tuple.Item2, stmt);
            }
        }

        #endregion
    }
}
