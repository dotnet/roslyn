// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
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
        public abstract string LanguageName { get; }
        public abstract TreeComparer<SyntaxNode> TopSyntaxComparer { get; }

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

            var actualNewActiveStatements = ImmutableArray.CreateBuilder<ActiveStatement>(oldActiveStatements.Length);
            actualNewActiveStatements.Count = actualNewActiveStatements.Capacity;

            var actualNewExceptionRegions = ImmutableArray.CreateBuilder<ImmutableArray<LinePositionSpan>>(oldActiveStatements.Length);
            actualNewExceptionRegions.Count = actualNewExceptionRegions.Capacity;

            Analyzer.GetTestAccessor().AnalyzeUnchangedDocument(
                oldActiveStatements.AsImmutable(),
                text,
                root,
                actualNewActiveStatements,
                actualNewExceptionRegions);

            // check active statements:
            AssertSpansEqual(expectedNewActiveStatements, actualNewActiveStatements.Select(s => s.Span), text);

            // check new exception regions:
            Assert.Equal(expectedNewExceptionRegions.Length, actualNewExceptionRegions.Count);
            for (var i = 0; i < expectedNewExceptionRegions.Length; i++)
            {
                AssertSpansEqual(expectedNewExceptionRegions[i], actualNewExceptionRegions[i], text);
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

            var diagnostics = new ArrayBuilder<RudeEditDiagnostic>();
            var updatedActiveMethodMatches = new ArrayBuilder<UpdatedMemberInfo>();
            var actualNewActiveStatements = ImmutableArray.CreateBuilder<ActiveStatement>(oldActiveStatements.Length);
            actualNewActiveStatements.Count = actualNewActiveStatements.Capacity;
            var actualNewExceptionRegions = ImmutableArray.CreateBuilder<ImmutableArray<LinePositionSpan>>(oldActiveStatements.Length);
            actualNewExceptionRegions.Count = actualNewExceptionRegions.Capacity;
            var editMap = BuildEditMap(editScript);

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

            testAccessor.ReportTopLevelSyntacticRudeEdits(diagnostics, editScript, editMap);

            VerifyDiagnostics(expectedDiagnostics, diagnostics, newText);
            VerifyActiveStatementsAndExceptionRegions(editScript, description, oldActiveStatements, oldText, newText, !diagnostics.IsEmpty(), actualNewActiveStatements, actualNewExceptionRegions);
        }

        private void VerifyActiveStatementsAndExceptionRegions(
            EditScript<SyntaxNode> editScript,
            ActiveStatementsDescription description,
            IReadOnlyList<ActiveStatement> oldActiveStatements,
            SourceText oldText,
            SourceText newText,
            bool hasErrors,
            IReadOnlyList<ActiveStatement> actualNewActiveStatements,
            IReadOnlyList<ImmutableArray<LinePositionSpan>> actualNewExceptionRegions)
        {
            // check active statements:
            AssertSpansEqual(description.NewSpans, actualNewActiveStatements.Select(s => s.Span), newText);

            if (!hasErrors)
            {
                // check old exception regions:
                for (var i = 0; i < oldActiveStatements.Count; i++)
                {
                    var actualOldExceptionRegions = Analyzer.GetExceptionRegions(
                        oldText,
                        editScript.Match.OldRoot,
                        oldActiveStatements[i].Span,
                        isNonLeaf: oldActiveStatements[i].IsNonLeaf,
                        out _);

                    AssertSpansEqual(description.OldRegions[i], actualOldExceptionRegions, oldText);
                }

                // check new exception regions:
                Assert.Equal(description.NewRegions.Length, actualNewExceptionRegions.Count);
                for (var i = 0; i < description.NewRegions.Length; i++)
                {
                    AssertSpansEqual(description.NewRegions[i], actualNewExceptionRegions[i], newText);
                }
            }
            else
            {
                for (var i = 0; i < oldActiveStatements.Count; i++)
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

            var diagnostics = new ArrayBuilder<RudeEditDiagnostic>();
            var editMap = BuildEditMap(editScript);

            var triviaEdits = new ArrayBuilder<(SyntaxNode OldNode, SyntaxNode NewNode)>();
            var actualLineEdits = new ArrayBuilder<SourceLineUpdate>();

            Analyzer.GetTestAccessor().AnalyzeTrivia(
                oldText,
                newText,
                editScript.Match,
                editMap,
                triviaEdits,
                actualLineEdits,
                diagnostics,
                default);

            VerifyDiagnostics(expectedDiagnostics, diagnostics, newText);

            AssertEx.Equal(expectedLineEdits, actualLineEdits, itemSeparator: ",\r\n");

            var actualNodeUpdates = triviaEdits.Select(e => e.NewNode.ToString().ToLines().First());
            AssertEx.Equal(expectedNodeUpdates, actualNodeUpdates, itemSeparator: ",\r\n");
        }

        internal void VerifySemantics(EditScript<SyntaxNode>[] editScripts, DocumentAnalysisResultsDescription[] expectedResults)
        {
            Assert.True(editScripts.Length == expectedResults.Length);
            var documentCount = expectedResults.Length;

            using var workspace = new AdhocWorkspace(FeaturesTestCompositions.Features.GetHostServices());
            CreateProjects(editScripts, workspace, out var oldProject, out var newProject);

            var oldDocuments = oldProject.Documents.ToArray();
            var newDocuments = newProject.Documents.ToArray();

            Debug.Assert(oldDocuments.Length == newDocuments.Length);

            var oldTrees = oldDocuments.Select(d => d.GetSyntaxTreeSynchronously(default)!).ToArray();
            var newTrees = newDocuments.Select(d => d.GetSyntaxTreeSynchronously(default)!).ToArray();

            var oldCompilation = CreateLibraryCompilation("Old", oldTrees);
            var newCompilation = CreateLibraryCompilation("New", newTrees);

            var testAccessor = Analyzer.GetTestAccessor();

            for (var documentIndex = 0; documentIndex < documentCount; documentIndex++)
            {
                var expectedResult = expectedResults[documentIndex];

                var oldActiveStatements = expectedResult.ActiveStatements.OldStatements.ToImmutableArray();

                var actualNewActiveStatements = ImmutableArray.CreateBuilder<ActiveStatement>(oldActiveStatements.Length);
                actualNewActiveStatements.Count = actualNewActiveStatements.Capacity;

                var actualNewExceptionRegions = ImmutableArray.CreateBuilder<ImmutableArray<LinePositionSpan>>(oldActiveStatements.Length);
                actualNewExceptionRegions.Count = actualNewExceptionRegions.Capacity;

                var triviaEdits = new ArrayBuilder<(SyntaxNode OldNode, SyntaxNode NewNode)>();
                var actualLineEdits = new ArrayBuilder<SourceLineUpdate>();
                var includeFirstLineInDiagnostics = expectedResult.Diagnostics.Any(d => d.FirstLine != null) == true;

                // we need to rebuild the edit script, so that it operates on nodes associated with the same syntax trees backing the documents:
                var oldTree = oldTrees[documentIndex];
                var newTree = newTrees[documentIndex];
                var oldRoot = oldTree.GetRoot();
                var newRoot = newTree.GetRoot();
                var editScript = TopSyntaxComparer.ComputeMatch(oldRoot, newRoot).GetTreeEdits();
                var editMap = BuildEditMap(editScript);

                var oldModel = oldCompilation.GetSemanticModel(oldTree);
                var newModel = newCompilation.GetSemanticModel(newTree);
                var oldDocument = oldDocuments[documentIndex];
                var newDocument = newDocuments[documentIndex];
                var oldText = oldDocument.GetTextSynchronously(default);
                var newText = newDocument.GetTextSynchronously(default);

                // validate that we constructed documents and trees correctly above:
                Assert.Same(oldDocument.GetSyntaxTreeSynchronously(default), oldTree);
                Assert.Same(newDocument.GetSyntaxTreeSynchronously(default), newTree);

                var diagnostics = new ArrayBuilder<RudeEditDiagnostic>();
                var updatedActiveMethodMatches = new ArrayBuilder<UpdatedMemberInfo>();

                testAccessor.AnalyzeMemberBodiesSyntax(
                    editScript,
                    editMap,
                    oldText,
                    newText,
                    oldActiveStatements,
                    expectedResult.ActiveStatements.OldTrackingSpans.ToImmutableArrayOrEmpty(),
                    actualNewActiveStatements,
                    actualNewExceptionRegions,
                    updatedActiveMethodMatches,
                    diagnostics);

                testAccessor.ReportTopLevelSyntacticRudeEdits(diagnostics, editScript, editMap);

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
                    oldDocument,
                    newDocument,
                    diagnostics,
                    out var semanticEdits,
                    CancellationToken.None);

                VerifyDiagnostics(expectedResult.Diagnostics, diagnostics.ToDescription(newText, includeFirstLineInDiagnostics));

                if (!expectedResult.SemanticEdits.IsDefault)
                {
                    VerifySemanticEdits(expectedResult.SemanticEdits, semanticEdits, oldCompilation, newCompilation, oldRoot, newRoot);
                }

                VerifyActiveStatementsAndExceptionRegions(
                    editScript,
                    expectedResult.ActiveStatements,
                    oldActiveStatements,
                    oldText,
                    newText,
                    expectedResult.Diagnostics.IsEmpty,
                    actualNewActiveStatements,
                    actualNewExceptionRegions);
            }
        }

        public static void VerifyDiagnostics(IEnumerable<RudeEditDiagnosticDescription> expected, IEnumerable<RudeEditDiagnostic> actual, SourceText newSource)
            => VerifyDiagnostics(expected, actual.ToDescription(newSource, expected.Any(d => d.FirstLine != null)));

        public static void VerifyDiagnostics(IEnumerable<RudeEditDiagnosticDescription> expected, IEnumerable<RudeEditDiagnosticDescription> actual)
            => AssertEx.SetEqual(expected, actual, itemSeparator: ",\r\n");

        private void VerifySemanticEdits(
            ImmutableArray<SemanticEditDescription> expectedSemanticEdits,
            ImmutableArray<SemanticEditInfo> actualSemanticEdits,
            Compilation oldCompilation,
            Compilation newCompilation,
            SyntaxNode oldRoot,
            SyntaxNode newRoot)
        {
            // string comparison to simplify understanding why a test failed:
            AssertEx.Equal(
                expectedSemanticEdits.Select(e => $"{e.Kind}: {e.SymbolProvider(newCompilation)}"),
                actualSemanticEdits.Select(e => $"{e.Kind}: {e.Symbol.Resolve(newCompilation).Symbol}"));

            for (var i = 0; i < actualSemanticEdits.Length; i++)
            {
                var editKind = expectedSemanticEdits[i].Kind;

                Assert.Equal(editKind, actualSemanticEdits[i].Kind);

                var expectedOldSymbol = (editKind == SemanticEditKind.Update) ? expectedSemanticEdits[i].SymbolProvider(oldCompilation) : null;
                var expectedNewSymbol = expectedSemanticEdits[i].SymbolProvider(newCompilation);
                var symbolKey = actualSemanticEdits[i].Symbol;

                if (editKind == SemanticEditKind.Update)
                {
                    Assert.Equal(expectedOldSymbol, symbolKey.Resolve(oldCompilation, ignoreAssemblyKey: true).Symbol);
                    Assert.Equal(expectedNewSymbol, symbolKey.Resolve(newCompilation, ignoreAssemblyKey: true).Symbol);
                }
                else if (editKind == SemanticEditKind.Insert)
                {
                    Assert.Equal(expectedNewSymbol, symbolKey.Resolve(newCompilation, ignoreAssemblyKey: true).Symbol);
                }
                else
                {
                    Assert.False(true, "Only Update or Insert allowed");
                }

                // Edit is expected to have a syntax map:
                var actualSyntaxMap = actualSemanticEdits[i].SyntaxMap;
                Assert.Equal(expectedSemanticEdits[i].HasSyntaxMap, actualSyntaxMap != null);

                // If expected map is specified validate its mappings with the actual one:
                var expectedSyntaxMap = expectedSemanticEdits[i].SyntaxMap;

                if (expectedSyntaxMap != null)
                {
                    Contract.ThrowIfNull(actualSyntaxMap);

                    foreach (var expectedSpanMapping in expectedSyntaxMap)
                    {
                        var newNode = FindNode(newRoot, expectedSpanMapping.Value);
                        var expectedOldNode = FindNode(oldRoot, expectedSpanMapping.Key);
                        var actualOldNode = actualSyntaxMap(newNode);

                        Assert.Equal(expectedOldNode, actualOldNode);
                    }
                }
            }
        }

        private void CreateProjects(EditScript<SyntaxNode>[] editScripts, AdhocWorkspace workspace, out Project oldProject, out Project newProject)
        {
            oldProject = workspace.AddProject("project", LanguageName);
            var documentIndex = 0;
            foreach (var editScript in editScripts)
            {
                oldProject = oldProject.AddDocument(documentIndex.ToString(), editScript.Match.OldRoot).Project;
                documentIndex++;
            }

            var newSolution = oldProject.Solution;
            documentIndex = 0;
            foreach (var oldDocument in oldProject.Documents)
            {
                newSolution = newSolution.WithDocumentSyntaxRoot(oldDocument.Id, editScripts[documentIndex].Match.NewRoot, PreservationMode.PreserveIdentity);
                documentIndex++;
            }

            newProject = newSolution.Projects.Single();
        }

        private static void AssertSpansEqual(IEnumerable<TextSpan> expected, IEnumerable<LinePositionSpan> actual, SourceText newText)
        {
            AssertEx.Equal(
                expected,
                actual.Select(span => newText.Lines.GetTextSpan(span)),
                itemSeparator: "\r\n",
                itemInspector: s => DisplaySpan(newText, s));
        }

        private static string DisplaySpan(SourceText source, TextSpan span)
            => span + ": [" + source.GetSubText(span).ToString().Replace("\r\n", " ") + "]";

        internal static IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>> GetMethodMatches(AbstractEditAndContinueAnalyzer analyzer, Match<SyntaxNode> bodyMatch)
        {
            Dictionary<SyntaxNode, LambdaInfo>? lazyActiveOrMatchedLambdas = null;
            var map = analyzer.GetTestAccessor().ComputeMap(bodyMatch, Array.Empty<ActiveNode>(), ref lazyActiveOrMatchedLambdas, new ArrayBuilder<RudeEditDiagnostic>());

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
