﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
        internal static CSharpEditAndContinueAnalyzer CreateAnalyzer()
        {
            return new CSharpEditAndContinueAnalyzer(testFaultInjector: null);
        }

        internal enum MethodKind
        {
            Regular,
            Async,
            Iterator,
            ConstructorWithParameters
        }

        internal static SemanticEditDescription[] NoSemanticEdits = Array.Empty<SemanticEditDescription>();

        internal static RudeEditDiagnosticDescription Diagnostic(RudeEditKind rudeEditKind, string squiggle, params string[] arguments)
            => new RudeEditDiagnosticDescription(rudeEditKind, squiggle, arguments, firstLine: null);

        internal static SemanticEditDescription SemanticEdit(SemanticEditKind kind, Func<Compilation, ISymbol> symbolProvider, IEnumerable<KeyValuePair<TextSpan, TextSpan>> syntaxMap)
        {
            Assert.NotNull(syntaxMap);
            return new SemanticEditDescription(kind, symbolProvider, syntaxMap, preserveLocalVariables: true);
        }

        internal static SemanticEditDescription SemanticEdit(SemanticEditKind kind, Func<Compilation, ISymbol> symbolProvider, bool preserveLocalVariables = false)
            => new SemanticEditDescription(kind, symbolProvider, null, preserveLocalVariables);

        private static SyntaxTree ParseSource(string source)
            => CSharpEditAndContinueTestHelpers.CreateInstance().ParseText(ActiveStatementsDescription.ClearTags(source));

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
            var match = CreateAnalyzer().GetTestAccessor().ComputeBodyMatch(m1, m2, Array.Empty<AbstractEditAndContinueAnalyzer.ActiveNode>(), diagnostics, out var oldHasStateMachineSuspensionPoint, out var newHasStateMachineSuspensionPoint);
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
            return EditAndContinueTestHelpers.GetMethodMatches(CreateAnalyzer(), methodMatch);
        }

        public static MatchingPairs ToMatchingPairs(Match<SyntaxNode> match)
            => EditAndContinueTestHelpers.ToMatchingPairs(match);

        public static MatchingPairs ToMatchingPairs(IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>> matches)
            => EditAndContinueTestHelpers.ToMatchingPairs(matches);

        internal static BlockSyntax MakeMethodBody(
            string bodySource,
            MethodKind kind = MethodKind.Regular)
        {
            var source = kind switch
            {
                MethodKind.Iterator => "class C { IEnumerable<int> F() { " + bodySource + " } }",
                MethodKind.Async => "class C { async Task<int> F() { " + bodySource + " } }",
                MethodKind.ConstructorWithParameters => "class C { C" + bodySource + " }",
                _ => "class C { void F() { " + bodySource + " } }",
            };

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
            => new ActiveStatementsDescription(oldSource, newSource);

        internal static SyntaxMapDescription GetSyntaxMap(string oldSource, string newSource)
            => new SyntaxMapDescription(oldSource, newSource);

        internal static void VerifyPreserveLocalVariables(EditScript<SyntaxNode> edits, bool preserveLocalVariables)
        {
            var decl1 = (MethodDeclarationSyntax)((ClassDeclarationSyntax)((CompilationUnitSyntax)edits.Match.OldRoot).Members[0]).Members[0];
            var body1 = ((MethodDeclarationSyntax)SyntaxFactory.SyntaxTree(decl1).GetRoot()).Body;

            var decl2 = (MethodDeclarationSyntax)((ClassDeclarationSyntax)((CompilationUnitSyntax)edits.Match.NewRoot).Members[0]).Members[0];
            var body2 = ((MethodDeclarationSyntax)SyntaxFactory.SyntaxTree(decl2).GetRoot()).Body;

            var diagnostics = new List<RudeEditDiagnostic>();
            _ = CreateAnalyzer().GetTestAccessor().ComputeBodyMatch(body1, body2, Array.Empty<AbstractEditAndContinueAnalyzer.ActiveNode>(), diagnostics, out var oldHasStateMachineSuspensionPoint, out var newHasStateMachineSuspensionPoint);
            var needsSyntaxMap = oldHasStateMachineSuspensionPoint && newHasStateMachineSuspensionPoint;

            // Active methods are detected to preserve local variables for variable mapping and
            // edited async/iterator methods are considered active.
            Assert.Equal(preserveLocalVariables, needsSyntaxMap);
        }
    }
}
