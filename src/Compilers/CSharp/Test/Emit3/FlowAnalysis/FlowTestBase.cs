// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public abstract class FlowTestBase : SemanticModelTestBase
    {
        internal ImmutableArray<Diagnostic> FlowDiagnostics(CSharpCompilation compilation)
        {
            var flowDiagnostics = BindingDiagnosticBag.GetInstance();
            foreach (var method in AllMethods(compilation.SourceModule.GlobalNamespace))
            {
                var sourceSymbol = method as SourceMemberMethodSymbol;
                if (sourceSymbol == null || sourceSymbol.ContainingType.IsDelegateType())
                {
                    continue;
                }

                var compilationState = new TypeCompilationState(sourceSymbol.ContainingType, compilation, null);
                var diagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);

                var boundBody = MethodCompiler.BindSynthesizedMethodBody(sourceSymbol, compilationState, diagnostics);
                if (boundBody != null)
                {
                    FlowAnalysisPass.Rewrite(sourceSymbol, boundBody, compilationState, flowDiagnostics, hasTrailingExpression: false, originalBodyNested: false);
                }

                diagnostics.Free();
            }

            return flowDiagnostics.ToReadOnlyAndFree().Diagnostics;
        }

        protected static void VerifyDataFlowAnalysis(string expected, DataFlowAnalysis result)
        {
            var actual = $$"""
VariablesDeclared: {{GetSymbolNamesJoined(result.VariablesDeclared)}}
AlwaysAssigned: {{GetSymbolNamesJoined(result.AlwaysAssigned)}}
Captured: {{GetSymbolNamesJoined(result.Captured)}}
CapturedInside: {{GetSymbolNamesJoined(result.CapturedInside)}}
CapturedOutside: {{GetSymbolNamesJoined(result.CapturedOutside)}}
DataFlowsIn: {{GetSymbolNamesJoined(result.DataFlowsIn)}}
DataFlowsOut: {{GetSymbolNamesJoined(result.DataFlowsOut)}}
DefinitelyAssignedOnEntry: {{GetSymbolNamesJoined(result.DefinitelyAssignedOnEntry)}}
DefinitelyAssignedOnExit: {{GetSymbolNamesJoined(result.DefinitelyAssignedOnExit)}}
ReadInside: {{GetSymbolNamesJoined(result.ReadInside)}}
ReadOutside: {{GetSymbolNamesJoined(result.ReadOutside)}}
WrittenInside: {{GetSymbolNamesJoined(result.WrittenInside)}}
WrittenOutside: {{GetSymbolNamesJoined(result.WrittenOutside)}}
""";
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, actual);
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

        protected DataFlowAnalysis CompileAndAnalyzeDataFlowExpression(string program, params MetadataReference[] references)
        {
            return CompileAndGetModelAndExpression(program, (model, expression) => model.AnalyzeDataFlow(expression), references);
        }

        protected DataFlowAnalysis CompileAndAnalyzeDataFlowExpression(string program, TargetFramework targetFramework, params MetadataReference[] references)
        {
            return CompileAndGetModelAndExpression(program, (model, expression) => model.AnalyzeDataFlow(expression), targetFramework, assertNoDiagnostics: true, references);
        }

        protected DataFlowAnalysis CompileAndAnalyzeDataFlowConstructorInitializer(string program, params MetadataReference[] references)
        {
            return CompileAndGetModelAndConstructorInitializer(program, (model, constructorInitializer) => model.AnalyzeDataFlow(constructorInitializer), references);
        }

        protected DataFlowAnalysis CompileAndAnalyzeDataFlowPrimaryConstructorInitializer(string program, params MetadataReference[] references)
        {
            return CompileAndGetModelAndPrimaryConstructorInitializer(program, (model, primaryConstructorInitializer) => model.AnalyzeDataFlow(primaryConstructorInitializer), references);
        }

        protected DataFlowAnalysis CompileAndAnalyzeDataFlowStatements(string program)
        {
            return CompileAndGetModelAndStatements(program, (model, stmt1, stmt2) => model.AnalyzeDataFlow(stmt1, stmt2));
        }

        protected (ControlFlowAnalysis controlFlowAnalysis, DataFlowAnalysis dataFlowAnalysis) CompileAndAnalyzeControlAndDataFlowStatements(string program)
        {
            return CompileAndGetModelAndStatements(program, (model, stmt1, stmt2) => (model.AnalyzeControlFlow(stmt1, stmt2), model.AnalyzeDataFlow(stmt1, stmt2)));
        }

        protected T CompileAndGetModelAndConstructorInitializer<T>(string program, Func<SemanticModel, ConstructorInitializerSyntax, T> analysisDelegate, params MetadataReference[] references)
        {
            var comp = CreateCompilation(program, parseOptions: TestOptions.RegularPreview, references: references);
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            int start = program.IndexOf(StartString, StringComparison.Ordinal) + StartString.Length;
            int end = program.IndexOf(EndString, StringComparison.Ordinal);
            ConstructorInitializerSyntax syntaxToBind = null;
            foreach (var expr in GetSyntaxNodeList(tree).OfType<ConstructorInitializerSyntax>())
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

        protected T CompileAndGetModelAndPrimaryConstructorInitializer<T>(string program, Func<SemanticModel, PrimaryConstructorBaseTypeSyntax, T> analysisDelegate, params MetadataReference[] references)
        {
            var comp = CreateCompilation(program, parseOptions: TestOptions.RegularPreview, references: references);
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            int start = program.IndexOf(StartString, StringComparison.Ordinal) + StartString.Length;
            int end = program.IndexOf(EndString, StringComparison.Ordinal);
            PrimaryConstructorBaseTypeSyntax syntaxToBind = null;
            foreach (var expr in GetSyntaxNodeList(tree).OfType<PrimaryConstructorBaseTypeSyntax>())
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

        protected T CompileAndGetModelAndExpression<T>(string program, Func<SemanticModel, ExpressionSyntax, T> analysisDelegate, params MetadataReference[] references)
        {
            return CompileAndGetModelAndExpression<T>(program, analysisDelegate, TargetFramework.Standard, assertNoDiagnostics: false, references);
        }

        protected T CompileAndGetModelAndExpression<T>(string program, Func<SemanticModel, ExpressionSyntax, T> analysisDelegate, TargetFramework targetFramework, bool assertNoDiagnostics, params MetadataReference[] references)
        {
            var comp = CreateCompilation(program, parseOptions: TestOptions.RegularPreview, targetFramework: targetFramework, references: references);

            if (assertNoDiagnostics)
            {
                comp.VerifyDiagnostics();
            }

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            int start = program.IndexOf(StartString, StringComparison.Ordinal) + StartString.Length;
            int end = program.IndexOf(EndString, StringComparison.Ordinal);
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
            var comp = CreateCompilation(program);
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            int start = program.IndexOf(StartString, StringComparison.Ordinal) + StartString.Length;
            int end = program.IndexOf(EndString, StringComparison.Ordinal);
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

        protected static string GetSymbolNamesJoined<T>(IEnumerable<T> symbols, bool sort = false) where T : ISymbol
        {
            if (sort)
            {
                symbols = symbols.OrderBy(n => n.Name);
            }

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

        protected (IEnumerable<ControlFlowAnalysis>, IEnumerable<DataFlowAnalysis>) CompileAndAnalyzeControlAndDataFlowMultipleStatements(string program, int treeindex = 0, int which = -1)
        {
            return (CompileAndAnalyzeMultipleControlFlowStatements(program, treeindex, which), CompileAndAnalyzeMultipleDataFlowStatements(program, treeindex, which));
        }

        protected IEnumerable<T> CompileAndGetModelAndMultipleExpressions<T>(string program, Func<SemanticModel, ExpressionSyntax, T> analysisDelegate, int treeindex = 0, int which = -1)
        {
            var comp = CreateCompilation(program);
            var tuple = GetBindingNodesAndModel<ExpressionSyntax>(comp, treeindex, which);

            foreach (var expr in tuple.Item1)
            {
                yield return analysisDelegate(tuple.Item2, expr);
            }
        }

        protected IEnumerable<T> CompileAndGetModelAndMultipleStatements<T>(string program, Func<SemanticModel, StatementSyntax, T> analysisDelegate, int treeindex = 0, int which = -1)
        {
            var comp = CreateCompilation(program);
            var tuple = GetBindingNodesAndModel<StatementSyntax>(comp, treeindex, which);

            foreach (var stmt in tuple.Item1)
            {
                yield return analysisDelegate(tuple.Item2, stmt);
            }
        }

        #endregion
    }
}
