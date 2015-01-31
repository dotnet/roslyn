// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public abstract class RudeEditTestBase : CSharpTestBase
    {
        internal static readonly CSharpEditAndContinueAnalyzer Analyzer = new CSharpEditAndContinueAnalyzer();

        internal enum StateMachineKind
        {
            None,
            Async,
            Iterator,
        }

        internal static SemanticEditDescription[] NoSemanticEdits = new SemanticEditDescription[0];

        internal static RudeEditDiagnosticDescription Diagnostic(RudeEditKind rudeEditKind, string squiggle, params string[] arguments)
        {
            return new RudeEditDiagnosticDescription(rudeEditKind, squiggle, arguments);
        }

        internal static SemanticEditDescription SemanticEdit(SemanticEditKind kind, Func<Compilation, ISymbol> symbolProvider, IEnumerable<KeyValuePair<TextSpan, TextSpan>> syntaxMap)
        {
            Assert.NotNull(syntaxMap);
            return new SemanticEditDescription(kind, symbolProvider, syntaxMap, preserveLocalVariables: true);
        }

        internal static SemanticEditDescription SemanticEdit(SemanticEditKind kind, Func<Compilation, ISymbol> symbolProvider, bool preserveLocalVariables = false)
        {
            return new SemanticEditDescription(kind, symbolProvider, null, preserveLocalVariables);
        }

        private static SyntaxTree ParseSource(string source, ParseOptions options = null)
        {
            return SyntaxFactory.ParseSyntaxTree(ActiveStatementsDescription.ClearTags(source), options: options);
        }

        internal static EditScript<SyntaxNode> GetTopEdits(string src1, string src2, ParseOptions options = null)
        {
            var tree1 = ParseSource(src1, options: options);
            var tree2 = ParseSource(src2, options: options);

            tree1.GetDiagnostics().Verify();
            tree2.GetDiagnostics().Verify();

            var match = TopSyntaxComparer.Instance.ComputeMatch(tree1.GetRoot(), tree2.GetRoot());
            return match.GetTreeEdits();
        }

        internal static EditScript<SyntaxNode> GetMethodEdits(string src1, string src2, ParseOptions options = null, StateMachineKind stateMachine = StateMachineKind.None)
        {
            var match = GetMethodMatch(src1, src2, options, stateMachine);
            return match.GetTreeEdits();
        }

        internal static Match<SyntaxNode> GetMethodMatch(string src1, string src2, ParseOptions options = null, StateMachineKind stateMachine = StateMachineKind.None)
        {
            var m1 = MakeMethodBody(src1, options, stateMachine);
            var m2 = MakeMethodBody(src2, options, stateMachine);

            var diagnostics = new List<RudeEditDiagnostic>();
            bool needsSyntaxMap;
            var match = Analyzer.ComputeBodyMatch(m1, m2, new AbstractEditAndContinueAnalyzer.ActiveNode[0], diagnostics, out needsSyntaxMap);

            Assert.Equal(stateMachine != StateMachineKind.None, needsSyntaxMap);

            if (stateMachine == StateMachineKind.None)
            {
                Assert.Empty(diagnostics);
            }

            return match;
        }

        internal static IEnumerable<Match<SyntaxNode>> GetMethodMatches(string src1, string src2, ParseOptions options = null, StateMachineKind stateMachine = StateMachineKind.None)
        {
            var methodMatch = GetMethodMatch(src1, src2, options, stateMachine);

            var queue = new Queue<Match<SyntaxNode>>();
            queue.Enqueue(methodMatch);

            while (queue.Count > 0)
            {
                var match = queue.Dequeue();
                yield return match;

                foreach (var m in match.Matches)
                {
                    if (m.Key == match.OldRoot)
                    {
                        Assert.Equal(match.NewRoot, m.Value);
                        continue;
                    }

                    foreach (var body in GetLambdaBodies(m.Key, m.Value))
                    {
                        var lambdaMatch = new StatementSyntaxComparer(body.Item1, body.Item2).ComputeMatch(m.Key, m.Value);
                        queue.Enqueue(lambdaMatch);
                    }
                }
            }
        }

        public static MatchingPairs ToMatchingPairs(Match<SyntaxNode> match)
        {
            return new MatchingPairs(ToMatchingPairs(match.Matches.Where(partners => partners.Key != match.OldRoot)));
        }

        public static MatchingPairs ToMatchingPairs(IEnumerable<Match<SyntaxNode>> match)
        {
            return new MatchingPairs(ToMatchingPairs(match.SelectMany(m => m.Matches.Where(partners => partners.Key != m.OldRoot))));
        }

        private static IEnumerable<MatchingPair> ToMatchingPairs(IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>> matches)
        {
            return matches
                .OrderBy(partners => partners.Key.GetLocation().SourceSpan.Start)
                .ThenByDescending(partners => partners.Key.Span.Length)
                .Select(partners => new MatchingPair { Old = partners.Key.ToString().Replace("\r\n", " "), New = partners.Value.ToString().Replace("\r\n", " ") });
        }

        internal static BlockSyntax MakeMethodBody(
            string bodySource,
            ParseOptions options = null,
            StateMachineKind stateMachine = StateMachineKind.None)
        {
            string source;
            switch (stateMachine)
            {
                case StateMachineKind.Iterator:
                    source = "class C { IEnumerable<int> F() { " + bodySource + " } }";
                    break;

                case StateMachineKind.Async:
                    source = "class C { async Task<int> F() { " + bodySource + " } }";
                    break;

                default:
                    source = "class C { void F() { " + bodySource + " } }";
                    break;
            }

            var tree = ParseSource(source, options: options);
            var root = tree.GetRoot();

            tree.GetDiagnostics().Verify();

            var declaration = (MethodDeclarationSyntax)((ClassDeclarationSyntax)((CompilationUnitSyntax)root).Members[0]).Members[0];

            // We need to preserve the parent node to allow detection of state machine methods in the analyzer.
            // If we are not testing a state machine method we only use the body to avoid updating positions in all existing tests.
            if (stateMachine != StateMachineKind.None)
            {
                return ((MethodDeclarationSyntax)SyntaxFactory.SyntaxTree(declaration).GetRoot()).Body;
            }

            return (BlockSyntax)SyntaxFactory.SyntaxTree(declaration.Body).GetRoot();
        }

        internal static ActiveStatementsDescription GetActiveStatements(string oldSource, string newSource)
        {
            return new ActiveStatementsDescription(oldSource, newSource);
        }

        internal static SyntaxMapDescription GetSyntaxMap(string oldSource, string newSource)
        {
            return new SyntaxMapDescription(oldSource, newSource);
        }

        private static ImmutableArray<ValueTuple<SyntaxNode, SyntaxNode>> GetLambdaBodies(SyntaxNode oldNode, SyntaxNode newNode)
        {
            switch (oldNode.Kind())
            {
                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.AnonymousMethodExpression:
                    return ImmutableArray.Create(ValueTuple.Create<SyntaxNode, SyntaxNode>(((AnonymousFunctionExpressionSyntax)oldNode).Body, ((AnonymousFunctionExpressionSyntax)newNode).Body));

                case SyntaxKind.FromClause:
                    return ImmutableArray.Create(ValueTuple.Create<SyntaxNode, SyntaxNode>(((FromClauseSyntax)oldNode).Expression, ((FromClauseSyntax)newNode).Expression));

                case SyntaxKind.LetClause:
                    return ImmutableArray.Create(ValueTuple.Create<SyntaxNode, SyntaxNode>(((LetClauseSyntax)oldNode).Expression, ((LetClauseSyntax)newNode).Expression));

                case SyntaxKind.WhereClause:
                    return ImmutableArray.Create(ValueTuple.Create<SyntaxNode, SyntaxNode>(((WhereClauseSyntax)oldNode).Condition, ((WhereClauseSyntax)newNode).Condition));

                case SyntaxKind.AscendingOrdering:
                case SyntaxKind.DescendingOrdering:
                    return ImmutableArray.Create(ValueTuple.Create<SyntaxNode, SyntaxNode>(((OrderingSyntax)oldNode).Expression, ((OrderingSyntax)newNode).Expression));

                case SyntaxKind.SelectClause:
                    return ImmutableArray.Create(ValueTuple.Create<SyntaxNode, SyntaxNode>(((SelectClauseSyntax)oldNode).Expression, ((SelectClauseSyntax)newNode).Expression));

                case SyntaxKind.JoinClause:
                    return ImmutableArray.Create(
                        ValueTuple.Create<SyntaxNode, SyntaxNode>(((JoinClauseSyntax)oldNode).LeftExpression, ((JoinClauseSyntax)newNode).LeftExpression),
                        ValueTuple.Create<SyntaxNode, SyntaxNode>(((JoinClauseSyntax)oldNode).RightExpression, ((JoinClauseSyntax)newNode).RightExpression));

                case SyntaxKind.GroupClause:
                    return ImmutableArray.Create(
                        ValueTuple.Create<SyntaxNode, SyntaxNode>(((GroupClauseSyntax)oldNode).GroupExpression, ((GroupClauseSyntax)newNode).ByExpression),
                        ValueTuple.Create<SyntaxNode, SyntaxNode>(((GroupClauseSyntax)oldNode).GroupExpression, ((GroupClauseSyntax)newNode).ByExpression));

                default:
                    return ImmutableArray<ValueTuple<SyntaxNode, SyntaxNode>>.Empty;
            }
        }

        internal static void VerifyPreserveLocalVariables(EditScript<SyntaxNode> edits, bool preserveLocalVariables)
        {
            var decl1 = (MethodDeclarationSyntax)((ClassDeclarationSyntax)((CompilationUnitSyntax)edits.Match.OldRoot).Members[0]).Members[0];
            var body1 = ((MethodDeclarationSyntax)SyntaxFactory.SyntaxTree(decl1).GetRoot()).Body;

            var decl2 = (MethodDeclarationSyntax)((ClassDeclarationSyntax)((CompilationUnitSyntax)edits.Match.NewRoot).Members[0]).Members[0];
            var body2 = ((MethodDeclarationSyntax)SyntaxFactory.SyntaxTree(decl2).GetRoot()).Body;

            var diagnostics = new List<RudeEditDiagnostic>();
            bool isActiveMethod;
            var match = Analyzer.ComputeBodyMatch(body1, body2, new AbstractEditAndContinueAnalyzer.ActiveNode[0], diagnostics, out isActiveMethod);

            // Active methods are detected to preserve local variables for variable mapping and
            // edited async/iterator methods are considered active.
            Assert.Equal(preserveLocalVariables, isActiveMethod);
        }
    }
}
