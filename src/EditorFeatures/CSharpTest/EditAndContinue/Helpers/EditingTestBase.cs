// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EditAndContinue;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public abstract class EditingTestBase : CSharpTestBase
    {
        internal static readonly CSharpEditAndContinueAnalyzer Analyzer = new CSharpEditAndContinueAnalyzer();

        internal enum MethodKind
        {
            Regular,
            Async,
            Iterator,
            ConstructorWithParameters
        }

        internal static SemanticEditDescription[] NoSemanticEdits = Array.Empty<SemanticEditDescription>();

        internal static RudeEditDiagnosticDescription Diagnostic(RudeEditKind rudeEditKind, string squiggle, params string[] arguments)
        {
            return new RudeEditDiagnosticDescription(rudeEditKind, squiggle, arguments, firstLine: null);
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

        private static SyntaxTree ParseSource(string source)
            => CSharpEditAndContinueTestHelpers.Instance.ParseText(ActiveStatementsDescription.ClearTags(source));

        internal static EditScript<SyntaxNode> GetTopEdits(string src1, string src2)
        {
            var tree1 = ParseSource(src1);
            var tree2 = ParseSource(src2);

            tree1.GetDiagnostics().Verify();
            tree2.GetDiagnostics().Verify();

            var match = TopSyntaxComparer.Instance.ComputeMatch(tree1.GetRoot(), tree2.GetRoot());
            return match.GetTreeEdits();
        }

        /// <summary>
        /// Gets method edits on the current level of the source hierarchy. This means that edits on lower labeled levels of the hierarchy are not expected to be returned.
        /// </summary>
        internal static EditScript<SyntaxNode> GetMethodEdits(string src1, string src2, MethodKind kind = MethodKind.Regular)
        {
            var match = GetMethodMatch(src1, src2, kind);
            return match.GetTreeEdits();
        }

        internal static Match<SyntaxNode> GetMethodMatch(string src1, string src2, MethodKind kind = MethodKind.Regular)
        {
            var m1 = MakeMethodBody(src1, kind);
            var m2 = MakeMethodBody(src2, kind);

            var diagnostics = new List<RudeEditDiagnostic>();
            var match = Analyzer.GetTestAccessor().ComputeBodyMatch(m1, m2, Array.Empty<AbstractEditAndContinueAnalyzer.ActiveNode>(), diagnostics, out var oldHasStateMachineSuspensionPoint, out var newHasStateMachineSuspensionPoint);
            var needsSyntaxMap = oldHasStateMachineSuspensionPoint && newHasStateMachineSuspensionPoint;

            Assert.Equal(kind != MethodKind.Regular && kind != MethodKind.ConstructorWithParameters, needsSyntaxMap);

            if (kind == MethodKind.Regular || kind == MethodKind.ConstructorWithParameters)
            {
                Assert.Empty(diagnostics);
            }

            return match;
        }

        internal static IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>> GetMethodMatches(string src1, string src2, MethodKind kind = MethodKind.Regular)
        {
            var methodMatch = GetMethodMatch(src1, src2, kind);
            return EditAndContinueTestHelpers.GetMethodMatches(Analyzer, methodMatch);
        }

        public static MatchingPairs ToMatchingPairs(Match<SyntaxNode> match)
        {
            return EditAndContinueTestHelpers.ToMatchingPairs(match);
        }

        public static MatchingPairs ToMatchingPairs(IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>> matches)
        {
            return EditAndContinueTestHelpers.ToMatchingPairs(matches);
        }

        internal static BlockSyntax MakeMethodBody(
            string bodySource,
            MethodKind kind = MethodKind.Regular)
        {
            string source;
            switch (kind)
            {
                case MethodKind.Iterator:
                    source = "class C { IEnumerable<int> F() { " + bodySource + " } }";
                    break;

                case MethodKind.Async:
                    source = "class C { async Task<int> F() { " + bodySource + " } }";
                    break;

                case MethodKind.ConstructorWithParameters:
                    source = "class C { C" + bodySource + " }";
                    break;

                default:
                    source = "class C { void F() { " + bodySource + " } }";
                    break;
            }

            var tree = ParseSource(source);
            var root = tree.GetRoot();

            tree.GetDiagnostics().Verify();

            var declaration = (BaseMethodDeclarationSyntax)((ClassDeclarationSyntax)((CompilationUnitSyntax)root).Members[0]).Members[0];

            // We need to preserve the parent node to allow detection of state machine methods in the analyzer.
            // If we are not testing a state machine method we only use the body to avoid updating positions in all existing tests.
            if (kind != MethodKind.Regular)
            {
                return ((BaseMethodDeclarationSyntax)SyntaxFactory.SyntaxTree(declaration).GetRoot()).Body;
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

        internal static void VerifyPreserveLocalVariables(EditScript<SyntaxNode> edits, bool preserveLocalVariables)
        {
            var decl1 = (MethodDeclarationSyntax)((ClassDeclarationSyntax)((CompilationUnitSyntax)edits.Match.OldRoot).Members[0]).Members[0];
            var body1 = ((MethodDeclarationSyntax)SyntaxFactory.SyntaxTree(decl1).GetRoot()).Body;

            var decl2 = (MethodDeclarationSyntax)((ClassDeclarationSyntax)((CompilationUnitSyntax)edits.Match.NewRoot).Members[0]).Members[0];
            var body2 = ((MethodDeclarationSyntax)SyntaxFactory.SyntaxTree(decl2).GetRoot()).Body;

            var diagnostics = new List<RudeEditDiagnostic>();
            var match = Analyzer.GetTestAccessor().ComputeBodyMatch(body1, body2, Array.Empty<AbstractEditAndContinueAnalyzer.ActiveNode>(), diagnostics, out var oldHasStateMachineSuspensionPoint, out var newHasStateMachineSuspensionPoint);
            var needsSyntaxMap = oldHasStateMachineSuspensionPoint && newHasStateMachineSuspensionPoint;

            // Active methods are detected to preserve local variables for variable mapping and
            // edited async/iterator methods are considered active.
            Assert.Equal(preserveLocalVariables, needsSyntaxMap);
        }
    }
}
