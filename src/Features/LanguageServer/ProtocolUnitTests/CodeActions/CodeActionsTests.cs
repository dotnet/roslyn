// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.CodeActions
{
    public class CodeActionsTests : AbstractLanguageServerProtocolTests
    {
        [WpfFact]
        public async Task TestCodeActionHandlerAsync()
        {
            var markup =
@"class A
{
    void M()
    {
        {|caret:|}int i = 1;
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);

            var caretLocation = locations["caret"].Single();
            var expected = CreateCodeAction(
                title: CSharpAnalyzersResources.Use_implicit_type,
                kind: CodeActionKind.Refactor,
                children: Array.Empty<LSP.VSCodeAction>(),
                data: CreateCodeActionResolveData(CSharpAnalyzersResources.Use_implicit_type, caretLocation),
                priority: PriorityLevel.Low,
                groupName: "Roslyn1",
                applicableRange: new LSP.Range { Start = new Position { Line = 4, Character = 8 }, End = new Position { Line = 4, Character = 11 } },
                diagnostics: null);

            var results = await RunGetCodeActionsAsync(workspace.CurrentSolution, caretLocation);
            var useImplicitType = results.FirstOrDefault(r => r.Title == CSharpAnalyzersResources.Use_implicit_type);

            AssertJsonEquals(expected, useImplicitType);
        }

        [WpfFact]
        public async Task TestCodeActionHandlerAsync_NestedAction()
        {
            var markup =
@"class A
{
    void M()
    {
        int {|caret:|}i = 1;
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);

            var caretLocation = locations["caret"].Single();
            var expected = CreateCodeAction(
                title: string.Format(FeaturesResources.Introduce_constant_for_0, "1"),
                kind: CodeActionKind.Refactor,
                children: Array.Empty<LSP.VSCodeAction>(),
                data: CreateCodeActionResolveData(
                    FeaturesResources.Introduce_constant + '|' + string.Format(FeaturesResources.Introduce_constant_for_0, "1"),
                    caretLocation),
                priority: PriorityLevel.Normal,
                groupName: "Roslyn2",
                applicableRange: new LSP.Range { Start = new Position { Line = 4, Character = 12 }, End = new Position { Line = 4, Character = 12 } },
                diagnostics: null);

            var results = await RunGetCodeActionsAsync(workspace.CurrentSolution, caretLocation);
            var introduceConstant = results[0].Children.FirstOrDefault(
                r => ((CodeActionResolveData)r.Data).UniqueIdentifier == FeaturesResources.Introduce_constant
                + '|' + string.Format(FeaturesResources.Introduce_constant_for_0, "1"));

            AssertJsonEquals(expected, introduceConstant);
        }

        [WpfFact]
        public async Task TestCodeActionsCacheAsync()
        {
            var markup =
@"class A
{
    void M()
    {
        {|caret:|}int i = 1;
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var cache = GetCodeActionsCache(workspace);
            var testAccessor = cache.GetTestAccessor();

            // This test assumes that the maximum cache size is 3, and will have to modified if this number changes.
            Assert.True(CodeActionsCache.TestAccessor.MaximumCacheSize == 3);

            var caretLocation = locations["caret"].Single();
            var document = GetDocument(workspace, CreateTextDocumentIdentifier(caretLocation.Uri));

            // 1. Invoking code actions on document with empty cache.
            await RunCodeActionsAndAssertActionsInCacheAsync(workspace, cache, caretLocation, document);

            // Ensuring contents of cache are as expected.
            var docAndRange = testAccessor.GetDocumentsAndRangesInCache().Single();
            AssertRangeAndDocEqual(caretLocation.Range, document, docAndRange);

            // 2. Invoking code actions on the same unmodified document and range should use the existing cached item
            // instead of generating a new cached item.
            await RunCodeActionsAndAssertActionsInCacheAsync(workspace, cache, caretLocation, document);

            // Ensuring contents of cache are as expected.
            docAndRange = testAccessor.GetDocumentsAndRangesInCache().Single();
            AssertRangeAndDocEqual(caretLocation.Range, document, docAndRange);

            var originalRange = caretLocation.Range;

            // 3. Invoking code actions on a different range should generate a new cached item.
            caretLocation.Range = new LSP.Range
            {
                Start = new LSP.Position() { Line = 0, Character = 0 },
                End = new LSP.Position() { Line = 0, Character = 0 }
            };

            await RunCodeActionsAndAssertActionsInCacheAsync(workspace, cache, caretLocation, document);

            // Ensuring contents of cache are as expected.
            var docsAndRanges = testAccessor.GetDocumentsAndRangesInCache();
            Assert.True(docsAndRanges.Count == 2);
            AssertRangeAndDocEqual(originalRange, document, docsAndRanges[0]);
            AssertRangeAndDocEqual(caretLocation.Range, document, docsAndRanges[1]);

            // 4. Changing the document should generate a new cached item.
            var currentDocText = await document.GetTextAsync();
            var changedSourceText = currentDocText.WithChanges(new TextChange(new TextSpan(0, 0), "class D { } \n"));
            var docId = workspace.Documents.First().Id;
            workspace.ChangeDocument(docId, changedSourceText);
            var updatedDocument = GetDocument(workspace, CreateTextDocumentIdentifier(caretLocation.Uri));

            await RunCodeActionsAndAssertActionsInCacheAsync(workspace, cache, caretLocation, updatedDocument);

            // Ensuring contents of cache are as expected.
            docsAndRanges = testAccessor.GetDocumentsAndRangesInCache();
            AssertRangeAndDocEqual(originalRange, document, docsAndRanges[0]);
            AssertRangeAndDocEqual(caretLocation.Range, document, docsAndRanges[1]);
            AssertRangeAndDocEqual(caretLocation.Range, updatedDocument, docsAndRanges[2]);

            var updatedRange = caretLocation.Range;

            // 5. The current cache size is 3. Adding a 4th item to the cache should still keep the cache size the same,
            // and boot out the oldest item in the cache.
            caretLocation.Range = new LSP.Range
            {
                Start = new LSP.Position() { Line = 0, Character = 0 },
                End = new LSP.Position() { Line = 0, Character = 1 }
            };

            await RunCodeActionsAndAssertActionsInCacheAsync(workspace, cache, caretLocation, updatedDocument);

            // Ensuring contents of cache are as expected.
            docsAndRanges = testAccessor.GetDocumentsAndRangesInCache();
            AssertRangeAndDocEqual(updatedRange, document, docsAndRanges[0]);
            AssertRangeAndDocEqual(updatedRange, updatedDocument, docsAndRanges[1]);
            AssertRangeAndDocEqual(caretLocation.Range, updatedDocument, docsAndRanges[2]);
        }

        private static async Task RunCodeActionsAndAssertActionsInCacheAsync(
            Workspace workspace,
            CodeActionsCache cache,
            LSP.Location caretLocation,
            Document document)
        {
            await RunGetCodeActionsAsync(workspace.CurrentSolution, caretLocation);
            var cacheResults = await cache.GetActionSetsAsync(document, caretLocation.Range, CancellationToken.None);
            Assert.NotNull(cacheResults);
        }

        private static void AssertRangeAndDocEqual(
            LSP.Range range,
            Document document,
            (Document Document, LSP.Range Range) actualDocAndRange)
        {
            Assert.Equal(document, actualDocAndRange.Document);
            Assert.Equal(range.Start, actualDocAndRange.Range.Start);
            Assert.Equal(range.End, actualDocAndRange.Range.End);
        }

        private static async Task<LSP.VSCodeAction[]> RunGetCodeActionsAsync(
            Solution solution,
            LSP.Location caret,
            LSP.ClientCapabilities clientCapabilities = null)
        {
            var queue = CreateRequestQueue(solution);
            var result = await GetLanguageServer(solution).ExecuteRequestAsync<LSP.CodeActionParams, LSP.VSCodeAction[]>(queue,
                LSP.Methods.TextDocumentCodeActionName, CreateCodeActionParams(caret),
                clientCapabilities, null, CancellationToken.None);
            return result;
        }

        internal static LSP.CodeActionParams CreateCodeActionParams(LSP.Location caret)
            => new LSP.CodeActionParams
            {
                TextDocument = CreateTextDocumentIdentifier(caret.Uri),
                Range = caret.Range,
                Context = new LSP.CodeActionContext
                {
                    // TODO - Code actions should respect context.
                }
            };

        internal static LSP.VSCodeAction CreateCodeAction(
            string title, LSP.CodeActionKind kind, LSP.VSCodeAction[] children,
            CodeActionResolveData data, LSP.Diagnostic[] diagnostics,
            LSP.PriorityLevel? priority, string groupName, LSP.Range applicableRange,
            LSP.WorkspaceEdit edit = null, LSP.Command command = null)
        {
            var action = new LSP.VSCodeAction
            {
                Title = title,
                Kind = kind,
                Children = children,
                Data = JToken.FromObject(data),
                Diagnostics = diagnostics,
                Edit = edit,
                Group = groupName,
                Priority = priority,
                ApplicableRange = applicableRange,
                Command = command
            };

            return action;
        }

        private static CodeActionsCache GetCodeActionsCache(Workspace workspace)
        {
            var exportProvider = ((TestWorkspace)workspace).ExportProvider.GetExportedValue<CodeActionsCache>();
            return Assert.IsType<CodeActionsCache>(exportProvider);
        }

        private static Document GetDocument(Workspace workspace, LSP.TextDocumentIdentifier textDocument)
        {
            return workspace.CurrentSolution.GetDocument(textDocument);
        }
    }
}
