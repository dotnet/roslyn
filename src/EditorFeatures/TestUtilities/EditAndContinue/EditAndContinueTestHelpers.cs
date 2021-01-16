// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Xunit;
using static Microsoft.CodeAnalysis.EditAndContinue.AbstractEditAndContinueAnalyzer;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal abstract class EditAndContinueTestHelpers
    {
        public abstract AbstractEditAndContinueAnalyzer Analyzer { get; }
        public abstract SyntaxNode FindNode(SyntaxNode root, TextSpan span);
        public abstract SyntaxTree ParseText(string source);
        public abstract Compilation CreateLibraryCompilation(string name, IEnumerable<SyntaxTree> trees);
        public abstract ImmutableArray<SyntaxNode> GetDeclarators(ISymbol method);

        internal void VerifyUnchangedDocument(
            string source,
            ActiveStatement[] oldActiveStatements,
            TextSpan[] expectedNewActiveStatements,
            ImmutableArray<TextSpan>[] expectedNewExceptionRegions)
        {
            var text = SourceText.From(source);
            var tree = ParseText(source);
            var root = tree.GetRoot();

            tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Verify();

            var documentId = DocumentId.CreateNewId(ProjectId.CreateNewId("TestEnCProject"), "TestEnCDocument");

            var actualNewActiveStatements = new ActiveStatement[oldActiveStatements.Length];
            var actualNewExceptionRegions = new ImmutableArray<LinePositionSpan>[oldActiveStatements.Length];

            Analyzer.GetTestAccessor().AnalyzeUnchangedDocument(
                oldActiveStatements.AsImmutable(),
                text,
                root,
                actualNewActiveStatements,
                actualNewExceptionRegions);

            // check active statements:
            AssertSpansEqual(expectedNewActiveStatements, actualNewActiveStatements.Select(s => s.Span), source, text);

            // check new exception regions:
            Assert.Equal(expectedNewExceptionRegions.Length, actualNewExceptionRegions.Length);
            for (var i = 0; i < expectedNewExceptionRegions.Length; i++)
            {
                AssertSpansEqual(expectedNewExceptionRegions[i], actualNewExceptionRegions[i], source, text);
            }
        }

        internal void VerifyRudeDiagnostics(
            EditScript<SyntaxNode> editScript,
            ActiveStatementsDescription description,
            RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            var oldActiveStatements = description.OldStatements;

            if (description.OldTrackingSpans != null)
            {
                Assert.Equal(oldActiveStatements.Length, description.OldTrackingSpans.Length);
            }

            var newSource = editScript.Match.NewRoot.SyntaxTree.ToString();
            var oldSource = editScript.Match.OldRoot.SyntaxTree.ToString();

            var oldText = SourceText.From(oldSource);
            var newText = SourceText.From(newSource);

            var diagnostics = new List<RudeEditDiagnostic>();
            var actualNewActiveStatements = new ActiveStatement[oldActiveStatements.Length];
            var actualNewExceptionRegions = new ImmutableArray<LinePositionSpan>[oldActiveStatements.Length];
            var updatedActiveMethodMatches = new List<UpdatedMemberInfo>();
            var editMap = BuildEditMap(editScript);

            var documentId = DocumentId.CreateNewId(ProjectId.CreateNewId("TestEnCProject"), "TestEnCDocument");
            var testAccessor = Analyzer.GetTestAccessor();

            testAccessor.AnalyzeMemberBodiesSyntax(
                editScript,
                editMap,
                oldText,
                newText,
                oldActiveStatements.AsImmutable(),
                description.OldTrackingSpans.ToImmutableArrayOrEmpty(),
                actualNewActiveStatements,
                actualNewExceptionRegions,
                updatedActiveMethodMatches,
                diagnostics);

            testAccessor.ReportTopLevelSynctactiveRudeEdits(diagnostics, editScript, editMap);

            diagnostics.Verify(newSource, expectedDiagnostics);

            // check active statements:
            AssertSpansEqual(description.NewSpans, actualNewActiveStatements.Select(s => s.Span), newSource, newText);

            if (diagnostics.Count == 0)
            {
                // check old exception regions:
                for (var i = 0; i < oldActiveStatements.Length; i++)
                {
                    var actualOldExceptionRegions = Analyzer.GetExceptionRegions(
                        oldText,
                        editScript.Match.OldRoot,
                        oldActiveStatements[i].Span,
                        isNonLeaf: oldActiveStatements[i].IsNonLeaf,
                        out _);

                    AssertSpansEqual(description.OldRegions[i], actualOldExceptionRegions, oldSource, oldText);
                }

                // check new exception regions:
                Assert.Equal(description.NewRegions.Length, actualNewExceptionRegions.Length);
                for (var i = 0; i < description.NewRegions.Length; i++)
                {
                    AssertSpansEqual(description.NewRegions[i], actualNewExceptionRegions[i], newSource, newText);
                }
            }
            else
            {
                for (var i = 0; i < oldActiveStatements.Length; i++)
                {
                    Assert.Equal(0, description.NewRegions[i].Length);
                }
            }
        }

        internal void VerifyLineEdits(
            EditScript<SyntaxNode> editScript,
            IEnumerable<SourceLineUpdate> expectedLineEdits,
            IEnumerable<string> expectedNodeUpdates,
            RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            var newSource = editScript.Match.NewRoot.SyntaxTree.ToString();
            var oldSource = editScript.Match.OldRoot.SyntaxTree.ToString();

            var oldText = SourceText.From(oldSource);
            var newText = SourceText.From(newSource);

            var diagnostics = new List<RudeEditDiagnostic>();
            var editMap = BuildEditMap(editScript);

            var triviaEdits = new List<(SyntaxNode OldNode, SyntaxNode NewNode)>();
            var actualLineEdits = new List<SourceLineUpdate>();

            Analyzer.GetTestAccessor().AnalyzeTrivia(
                oldText,
                newText,
                editScript.Match,
                editMap,
                triviaEdits,
                actualLineEdits,
                diagnostics,
                default);

            diagnostics.Verify(newSource, expectedDiagnostics);

            AssertEx.Equal(expectedLineEdits, actualLineEdits, itemSeparator: ",\r\n");

            var actualNodeUpdates = triviaEdits.Select(e => e.NewNode.ToString().ToLines().First());
            AssertEx.Equal(expectedNodeUpdates, actualNodeUpdates, itemSeparator: ",\r\n");
        }

        internal void VerifySemantics(
            IEnumerable<EditScript<SyntaxNode>> editScripts,
            ActiveStatementsDescription? activeStatements = null,
            SemanticEditDescription[]? expectedSemanticEdits = null,
            RudeEditDiagnosticDescription[]? expectedDiagnostics = null)
        {
            activeStatements ??= ActiveStatementsDescription.Empty;

            var oldTrees = editScripts.Select(editScript => editScript.Match.OldRoot.SyntaxTree).ToArray();
            var newTrees = editScripts.Select(editScript => editScript.Match.NewRoot.SyntaxTree).ToArray();

            var oldCompilation = CreateLibraryCompilation("Old", oldTrees);
            var newCompilation = CreateLibraryCompilation("New", newTrees);

            var oldActiveStatements = activeStatements.OldStatements.AsImmutable();
            var triviaEdits = new List<(SyntaxNode OldNode, SyntaxNode NewNode)>();
            var actualLineEdits = new List<SourceLineUpdate>();
            var actualSemanticEdits = new List<SemanticEdit>();
            var includeFirstLineInDiagnostics = expectedDiagnostics?.Any(d => d.FirstLine != null) == true;
            var actualDiagnosticDescriptions = new List<RudeEditDiagnosticDescription>();
            var actualDeclarationErrors = new List<Diagnostic>();

            var actualNewActiveStatements = new ActiveStatement[activeStatements.OldStatements.Length];
            var actualNewExceptionRegions = new ImmutableArray<LinePositionSpan>[activeStatements.OldStatements.Length];
            var testAccessor = Analyzer.GetTestAccessor();

            foreach (var editScript in editScripts)
            {
                var oldRoot = editScript.Match.OldRoot;
                var newRoot = editScript.Match.NewRoot;
                var oldSource = oldRoot.SyntaxTree.ToString();
                var newSource = newRoot.SyntaxTree.ToString();

                var editMap = BuildEditMap(editScript);
                var oldText = SourceText.From(oldSource);
                var newText = SourceText.From(newSource);
                var oldModel = oldCompilation.GetSemanticModel(oldRoot.SyntaxTree);
                var newModel = newCompilation.GetSemanticModel(newRoot.SyntaxTree);

                var diagnostics = new List<RudeEditDiagnostic>();
                var updatedActiveMethodMatches = new List<UpdatedMemberInfo>();

                testAccessor.AnalyzeMemberBodiesSyntax(
                    editScript,
                    editMap,
                    oldText,
                    newText,
                    oldActiveStatements,
                    activeStatements.OldTrackingSpans.ToImmutableArrayOrEmpty(),
                    actualNewActiveStatements,
                    actualNewExceptionRegions,
                    updatedActiveMethodMatches,
                    diagnostics);

                testAccessor.ReportTopLevelSynctactiveRudeEdits(diagnostics, editScript, editMap);

                testAccessor.AnalyzeTrivia(
                    oldText,
                    newText,
                    editScript.Match,
                    editMap,
                    triviaEdits,
                    actualLineEdits,
                    diagnostics,
                    CancellationToken.None);

                testAccessor.AnalyzeSemantics(
                    editScript,
                    editMap,
                    oldText,
                    oldActiveStatements,
                    triviaEdits,
                    updatedActiveMethodMatches,
                    oldModel,
                    newModel,
                    actualSemanticEdits,
                    diagnostics,
                    CancellationToken.None);

                actualDiagnosticDescriptions.AddRange(diagnostics.ToDescription(newSource, includeFirstLineInDiagnostics));
            }

            actualDiagnosticDescriptions.Verify(expectedDiagnostics);

            if (expectedSemanticEdits == null)
            {
                return;
            }

            Assert.Equal(expectedSemanticEdits.Length, actualSemanticEdits.Count);

            for (var i = 0; i < actualSemanticEdits.Count; i++)
            {
                var editKind = expectedSemanticEdits[i].Kind;

                Assert.Equal(editKind, actualSemanticEdits[i].Kind);

                var expectedOldSymbol = (editKind == SemanticEditKind.Update) ? expectedSemanticEdits[i].SymbolProvider(oldCompilation) : null;
                var expectedNewSymbol = expectedSemanticEdits[i].SymbolProvider(newCompilation);
                var actualOldSymbol = actualSemanticEdits[i].OldSymbol;
                var actualNewSymbol = actualSemanticEdits[i].NewSymbol;

                Assert.Equal(expectedOldSymbol, actualOldSymbol);
                Assert.Equal(expectedNewSymbol, actualNewSymbol);

                var expectedSyntaxMap = expectedSemanticEdits[i].SyntaxMap;
                var syntaxTreeOrdinal = expectedSemanticEdits[i].SyntaxTreeOrdinal;
                var oldRoot = oldTrees[syntaxTreeOrdinal].GetRoot();
                var newRoot = newTrees[syntaxTreeOrdinal].GetRoot();
                var actualSyntaxMap = actualSemanticEdits[i].SyntaxMap;

                Assert.Equal(expectedSemanticEdits[i].PreserveLocalVariables, actualSemanticEdits[i].PreserveLocalVariables);

                if (expectedSyntaxMap != null)
                {
                    Contract.ThrowIfNull(actualSyntaxMap);
                    Assert.True(expectedSemanticEdits[i].PreserveLocalVariables);

                    foreach (var expectedSpanMapping in expectedSyntaxMap)
                    {
                        var newNode = FindNode(newRoot, expectedSpanMapping.Value);
                        var expectedOldNode = FindNode(oldRoot, expectedSpanMapping.Key);
                        var actualOldNode = actualSyntaxMap(newNode);

                        Assert.Equal(expectedOldNode, actualOldNode);
                    }
                }
                else if (!expectedSemanticEdits[i].PreserveLocalVariables)
                {
                    Assert.Null(actualSyntaxMap);
                }
            }
        }

        private static void AssertSpansEqual(IEnumerable<TextSpan> expected, IEnumerable<LinePositionSpan> actual, string newSource, SourceText newText)
        {
            AssertEx.Equal(
                expected,
                actual.Select(span => newText.Lines.GetTextSpan(span)),
                itemSeparator: "\r\n",
                itemInspector: s => DisplaySpan(newSource, s));
        }

        private static string DisplaySpan(string source, TextSpan span)
            => span + ": [" + source.Substring(span.Start, span.Length).Replace("\r\n", " ") + "]";

        internal static IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>> GetMethodMatches(AbstractEditAndContinueAnalyzer analyzer, Match<SyntaxNode> bodyMatch)
        {
            Dictionary<SyntaxNode, LambdaInfo>? lazyActiveOrMatchedLambdas = null;
            var map = analyzer.GetTestAccessor().ComputeMap(bodyMatch, Array.Empty<ActiveNode>(), ref lazyActiveOrMatchedLambdas, new List<RudeEditDiagnostic>());

            var result = new Dictionary<SyntaxNode, SyntaxNode>();
            foreach (var pair in map.Forward)
            {
                if (pair.Value == bodyMatch.NewRoot)
                {
                    Assert.Same(pair.Key, bodyMatch.OldRoot);
                    continue;
                }

                result.Add(pair.Key, pair.Value);
            }

            return result;
        }

        public static MatchingPairs ToMatchingPairs(Match<SyntaxNode> match)
            => ToMatchingPairs(match.Matches.Where(partners => partners.Key != match.OldRoot));

        public static MatchingPairs ToMatchingPairs(IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>> matches)
        {
            return new MatchingPairs(matches
                .OrderBy(partners => partners.Key.GetLocation().SourceSpan.Start)
                .ThenByDescending(partners => partners.Key.Span.Length)
                .Select(partners => new MatchingPair
                {
                    Old = partners.Key.ToString().Replace("\r\n", " ").Replace("\n", " "),
                    New = partners.Value.ToString().Replace("\r\n", " ").Replace("\n", " ")
                }));
        }
    }

    internal static class EditScriptTestUtils
    {
        public static void VerifyEdits<TNode>(this EditScript<TNode> actual, params string[] expected)
            => AssertEx.Equal(expected, actual.Edits.Select(e => e.GetDebuggerDisplay()), itemSeparator: ",\r\n");
    }
}
