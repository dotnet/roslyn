﻿// Licensed to the .NET Foundation under one or more agreements.
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

        private void VerifyActiveStatementsAndExceptionRegions(
            ActiveStatementsDescription description,
            IReadOnlyList<ActiveStatement> oldActiveStatements,
            SourceText oldText,
            SourceText newText,
            SyntaxNode oldRoot,
            IReadOnlyList<ActiveStatement> actualNewActiveStatements,
            IReadOnlyList<ImmutableArray<LinePositionSpan>> actualNewExceptionRegions)
        {
            // check active statements:
            AssertSpansEqual(description.NewSpans, actualNewActiveStatements.Select(s => s.Span), newText);

            // check old exception regions:
            for (var i = 0; i < oldActiveStatements.Count; i++)
            {
                var oldRegions = Analyzer.GetExceptionRegions(
                    oldText,
                    oldRoot,
                    oldActiveStatements[i].Span,
                    isNonLeaf: oldActiveStatements[i].IsNonLeaf,
                    out _);

                AssertSpansEqual(description.OldRegions[i], oldRegions, oldText);
            }

            // check new exception regions:
            Assert.Equal(description.NewRegions.Length, actualNewExceptionRegions.Count);
            for (var i = 0; i < description.NewRegions.Length; i++)
            {
                AssertSpansEqual(description.NewRegions[i], actualNewExceptionRegions[i], newText);
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

        internal void VerifySemantics(EditScript<SyntaxNode>[] editScripts, TargetFramework targetFramework, DocumentAnalysisResultsDescription[] expectedResults)
        {
            Assert.True(editScripts.Length == expectedResults.Length);
            var documentCount = expectedResults.Length;

            using var workspace = new AdhocWorkspace(FeaturesTestCompositions.Features.GetHostServices());
            CreateProjects(editScripts, workspace, targetFramework, out var oldProject, out var newProject);

            var oldDocuments = oldProject.Documents.ToArray();
            var newDocuments = newProject.Documents.ToArray();

            Debug.Assert(oldDocuments.Length == newDocuments.Length);

            var oldTrees = oldDocuments.Select(d => d.GetSyntaxTreeSynchronously(default)!).ToArray();
            var newTrees = newDocuments.Select(d => d.GetSyntaxTreeSynchronously(default)!).ToArray();

            var testAccessor = Analyzer.GetTestAccessor();
            var allEdits = new List<SemanticEditInfo>();

            for (var documentIndex = 0; documentIndex < documentCount; documentIndex++)
            {
                var expectedResult = expectedResults[documentIndex];

                var oldActiveStatements = expectedResult.ActiveStatements.OldStatements.ToImmutableArray();

                var includeFirstLineInDiagnostics = expectedResult.Diagnostics.Any(d => d.FirstLine != null) == true;
                var newActiveStatementSpans = expectedResult.ActiveStatements.OldTrackingSpans.ToImmutableArrayOrEmpty();

                // we need to rebuild the edit script, so that it operates on nodes associated with the same syntax trees backing the documents:
                var oldTree = oldTrees[documentIndex];
                var newTree = newTrees[documentIndex];
                var oldRoot = oldTree.GetRoot();
                var newRoot = newTree.GetRoot();

                var oldDocument = oldDocuments[documentIndex];
                var newDocument = newDocuments[documentIndex];

                var oldModel = oldDocument.GetSemanticModelAsync().Result;
                var newModel = newDocument.GetSemanticModelAsync().Result;
                Contract.ThrowIfNull(oldModel);
                Contract.ThrowIfNull(newModel);

                var result = Analyzer.AnalyzeDocumentAsync(oldProject, oldActiveStatements, newDocument, newActiveStatementSpans, CancellationToken.None).Result;
                var oldText = oldDocument.GetTextSynchronously(default);
                var newText = newDocument.GetTextSynchronously(default);

                VerifyDiagnostics(expectedResult.Diagnostics, result.RudeEditErrors.ToDescription(newText, includeFirstLineInDiagnostics));

                if (!expectedResult.SemanticEdits.IsDefault)
                {
                    VerifySemanticEdits(expectedResult.SemanticEdits, result.SemanticEdits, oldModel.Compilation, newModel.Compilation, oldRoot, newRoot);

                    allEdits.AddRange(result.SemanticEdits);
                }

                if (expectedResult.Diagnostics.IsEmpty)
                {
                    VerifyActiveStatementsAndExceptionRegions(
                        expectedResult.ActiveStatements,
                        oldActiveStatements,
                        oldText,
                        newText,
                        oldRoot,
                        result.ActiveStatements,
                        result.ExceptionRegions);
                }
                else
                {
                    Assert.True(result.ExceptionRegions.IsDefault);
                }
            }

            // check if we can merge edits without throwing:
            EditSession.MergePartialEdits(oldProject.GetCompilationAsync().Result!, newProject.GetCompilationAsync().Result!, allEdits, out var _, out var _, CancellationToken.None);
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
                actualSemanticEdits.NullToEmpty().Select(e => $"{e.Kind}: {e.Symbol.Resolve(newCompilation).Symbol}"));

            for (var i = 0; i < actualSemanticEdits.Length; i++)
            {
                var expectedSemanticEdit = expectedSemanticEdits[i];
                var actualSemanticEdit = actualSemanticEdits[i];
                var editKind = expectedSemanticEdit.Kind;

                Assert.Equal(editKind, actualSemanticEdit.Kind);

                var expectedOldSymbol = (editKind == SemanticEditKind.Update) ? expectedSemanticEdit.SymbolProvider(oldCompilation) : null;
                var expectedNewSymbol = expectedSemanticEdit.SymbolProvider(newCompilation);
                var symbolKey = actualSemanticEdit.Symbol;

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

                // Partial types must match:
                Assert.Equal(
                    expectedSemanticEdit.PartialType?.Invoke(newCompilation),
                    actualSemanticEdit.PartialType?.Resolve(newCompilation, ignoreAssemblyKey: true).Symbol);

                // Edit is expected to have a syntax map:
                var actualSyntaxMap = actualSemanticEdit.SyntaxMap;
                Assert.Equal(expectedSemanticEdit.HasSyntaxMap, actualSyntaxMap != null);

                // If expected map is specified validate its mappings with the actual one:
                var expectedSyntaxMap = expectedSemanticEdit.SyntaxMap;

                if (expectedSyntaxMap != null)
                {
                    Contract.ThrowIfNull(actualSyntaxMap);
                    VerifySyntaxMap(oldRoot, newRoot, expectedSyntaxMap, actualSyntaxMap);
                }
            }
        }

        private void VerifySyntaxMap(
            SyntaxNode oldRoot,
            SyntaxNode newRoot,
            IEnumerable<KeyValuePair<TextSpan, TextSpan>> expectedSyntaxMap,
            Func<SyntaxNode, SyntaxNode?> actualSyntaxMap)
        {
            foreach (var expectedSpanMapping in expectedSyntaxMap)
            {
                var newNode = FindNode(newRoot, expectedSpanMapping.Value);
                var expectedOldNode = FindNode(oldRoot, expectedSpanMapping.Key);
                var actualOldNode = actualSyntaxMap(newNode);

                Assert.Equal(expectedOldNode, actualOldNode);
            }
        }

        private void CreateProjects(EditScript<SyntaxNode>[] editScripts, AdhocWorkspace workspace, TargetFramework targetFramework, out Project oldProject, out Project newProject)
        {
            oldProject = workspace.AddProject("project", LanguageName).WithMetadataReferences(TargetFrameworkUtil.GetReferences(targetFramework));
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
