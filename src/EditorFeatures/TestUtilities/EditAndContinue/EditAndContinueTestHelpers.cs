// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

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
            TextSpan?[] trackingSpansOpt,
            TextSpan[] expectedNewActiveStatements,
            ImmutableArray<TextSpan>[] expectedOldExceptionRegions,
            ImmutableArray<TextSpan>[] expectedNewExceptionRegions)
        {
            var text = SourceText.From(source);
            var tree = ParseText(source);
            var root = tree.GetRoot();

            tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Verify();

            var documentId = DocumentId.CreateNewId(ProjectId.CreateNewId("TestEnCProject"), "TestEnCDocument");

            TestActiveStatementTrackingService trackingService;
            if (trackingSpansOpt != null)
            {
                trackingService = new TestActiveStatementTrackingService(documentId, trackingSpansOpt);
            }
            else
            {
                trackingService = null;
            }

            var actualNewActiveStatements = new ActiveStatement[oldActiveStatements.Length];
            var actualNewExceptionRegions = new ImmutableArray<LinePositionSpan>[oldActiveStatements.Length];

            Analyzer.GetTestAccessor().AnalyzeUnchangedDocument(
                oldActiveStatements.AsImmutable(),
                text,
                root,
                documentId,
                trackingService,
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
            var updatedActiveMethodMatches = new List<AbstractEditAndContinueAnalyzer.UpdatedMemberInfo>();
            var editMap = Analyzer.BuildEditMap(editScript);

            var documentId = DocumentId.CreateNewId(ProjectId.CreateNewId("TestEnCProject"), "TestEnCDocument");

            TestActiveStatementTrackingService trackingService;
            if (description.OldTrackingSpans != null)
            {
                trackingService = new TestActiveStatementTrackingService(documentId, description.OldTrackingSpans);
            }
            else
            {
                trackingService = null;
            }

            Analyzer.GetTestAccessor().AnalyzeSyntax(
                editScript,
                editMap,
                oldText,
                newText,
                documentId,
                trackingService,
                oldActiveStatements.AsImmutable(),
                actualNewActiveStatements,
                actualNewExceptionRegions,
                updatedActiveMethodMatches,
                diagnostics);

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

            if (description.OldTrackingSpans != null)
            {
                // Verify that the new tracking spans are equal to the new active statements.
                AssertEx.Equal(trackingService.TrackingSpans, description.NewSpans.Select(s => (TextSpan?)s));
            }
        }

        internal void VerifyLineEdits(
            EditScript<SyntaxNode> editScript,
            IEnumerable<LineChange> expectedLineEdits,
            IEnumerable<string> expectedNodeUpdates,
            RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            var newSource = editScript.Match.NewRoot.SyntaxTree.ToString();
            var oldSource = editScript.Match.OldRoot.SyntaxTree.ToString();

            var oldText = SourceText.From(oldSource);
            var newText = SourceText.From(newSource);

            var diagnostics = new List<RudeEditDiagnostic>();
            var editMap = Analyzer.BuildEditMap(editScript);

            var triviaEdits = new List<(SyntaxNode OldNode, SyntaxNode NewNode)>();
            var actualLineEdits = new List<LineChange>();

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
            EditScript<SyntaxNode> editScript,
            ActiveStatementsDescription activeStatements = null,
            IEnumerable<string> additionalOldSources = null,
            IEnumerable<string> additionalNewSources = null,
            SemanticEditDescription[] expectedSemanticEdits = null,
            DiagnosticDescription expectedDeclarationError = null,
            RudeEditDiagnosticDescription[] expectedDiagnostics = null)
        {
            activeStatements ??= ActiveStatementsDescription.Empty;

            var editMap = Analyzer.BuildEditMap(editScript);

            var oldRoot = editScript.Match.OldRoot;
            var newRoot = editScript.Match.NewRoot;

            var oldSource = oldRoot.SyntaxTree.ToString();
            var newSource = newRoot.SyntaxTree.ToString();

            var oldText = SourceText.From(oldSource);
            var newText = SourceText.From(newSource);

            IEnumerable<SyntaxTree> oldTrees = new[] { oldRoot.SyntaxTree };
            IEnumerable<SyntaxTree> newTrees = new[] { newRoot.SyntaxTree };

            if (additionalOldSources != null)
            {
                oldTrees = oldTrees.Concat(additionalOldSources.Select(s => ParseText(s)));
            }

            if (additionalOldSources != null)
            {
                newTrees = newTrees.Concat(additionalNewSources.Select(s => ParseText(s)));
            }

            var oldCompilation = CreateLibraryCompilation("Old", oldTrees);
            var newCompilation = CreateLibraryCompilation("New", newTrees);

            var oldModel = oldCompilation.GetSemanticModel(oldRoot.SyntaxTree);
            var newModel = newCompilation.GetSemanticModel(newRoot.SyntaxTree);

            var oldActiveStatements = activeStatements.OldStatements.AsImmutable();
            var updatedActiveMethodMatches = new List<AbstractEditAndContinueAnalyzer.UpdatedMemberInfo>();
            var triviaEdits = new List<(SyntaxNode OldNode, SyntaxNode NewNode)>();
            var actualLineEdits = new List<LineChange>();
            var actualSemanticEdits = new List<SemanticEdit>();
            var diagnostics = new List<RudeEditDiagnostic>();

            var actualNewActiveStatements = new ActiveStatement[activeStatements.OldStatements.Length];
            var actualNewExceptionRegions = new ImmutableArray<LinePositionSpan>[activeStatements.OldStatements.Length];

            Analyzer.GetTestAccessor().AnalyzeSyntax(
                editScript,
                editMap,
                oldText,
                newText,
                null,
                null,
                oldActiveStatements,
                actualNewActiveStatements,
                actualNewExceptionRegions,
                updatedActiveMethodMatches,
                diagnostics);

            diagnostics.Verify(newSource);

            Analyzer.GetTestAccessor().AnalyzeTrivia(
                oldText,
                newText,
                editScript.Match,
                editMap,
                triviaEdits,
                actualLineEdits,
                diagnostics,
                CancellationToken.None);

            diagnostics.Verify(newSource);

            Analyzer.GetTestAccessor().AnalyzeSemantics(
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
                out var firstDeclarationErrorOpt,
                CancellationToken.None);

            var actualDeclarationErrors = (firstDeclarationErrorOpt != null) ? new[] { firstDeclarationErrorOpt } : Array.Empty<Diagnostic>();
            var expectedDeclarationErrors = (expectedDeclarationError != null) ? new[] { expectedDeclarationError } : Array.Empty<DiagnosticDescription>();
            actualDeclarationErrors.Verify(expectedDeclarationErrors);

            diagnostics.Verify(newSource, expectedDiagnostics);

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
                var actualSyntaxMap = actualSemanticEdits[i].SyntaxMap;

                Assert.Equal(expectedSemanticEdits[i].PreserveLocalVariables, actualSemanticEdits[i].PreserveLocalVariables);

                if (expectedSyntaxMap != null)
                {
                    Assert.NotNull(actualSyntaxMap);
                    Assert.True(expectedSemanticEdits[i].PreserveLocalVariables);

                    var newNodes = new List<SyntaxNode>();

                    foreach (var expectedSpanMapping in expectedSyntaxMap)
                    {
                        var newNode = FindNode(newRoot, expectedSpanMapping.Value);
                        var expectedOldNode = FindNode(oldRoot, expectedSpanMapping.Key);
                        var actualOldNode = actualSyntaxMap(newNode);

                        Assert.Equal(expectedOldNode, actualOldNode);

                        newNodes.Add(newNode);
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
        {
            return span + ": [" + source.Substring(span.Start, span.Length).Replace("\r\n", " ") + "]";
        }

        internal static IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>> GetMethodMatches(AbstractEditAndContinueAnalyzer analyzer, Match<SyntaxNode> bodyMatch)
        {
            Dictionary<SyntaxNode, AbstractEditAndContinueAnalyzer.LambdaInfo> lazyActiveOrMatchedLambdas = null;
            var map = analyzer.GetTestAccessor().ComputeMap(bodyMatch, Array.Empty<AbstractEditAndContinueAnalyzer.ActiveNode>(), ref lazyActiveOrMatchedLambdas, new List<RudeEditDiagnostic>());

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
        {
            return ToMatchingPairs(match.Matches.Where(partners => partners.Key != match.OldRoot));
        }

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

        private static IEnumerable<KeyValuePair<K, V>> ReverseMapping<K, V>(IEnumerable<KeyValuePair<V, K>> mapping)
        {
            foreach (var pair in mapping)
            {
                yield return KeyValuePairUtil.Create(pair.Value, pair.Key);
            }
        }
    }

    internal static class EditScriptTestUtils
    {
        public static void VerifyEdits<TNode>(this EditScript<TNode> actual, params string[] expected)
        {
            AssertEx.Equal(expected, actual.Edits.Select(e => e.GetDebuggerDisplay()), itemSeparator: ",\r\n");
        }
    }
}
