// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            var caretLocation = locations["caret"].Single();
            await RunGetCodeActionsAsync(workspace.CurrentSolution, caretLocation);

            var document = GetDocument(workspace, CreateTextDocumentIdentifier(caretLocation.Uri));
            var cacheResults = await cache.GetCacheAsync(document, caretLocation.Range, CancellationToken.None);
            Assert.NotNull(cacheResults);
            Assert.True(cache.GetNumCacheItems() == 1);

            // Invoking code actions on the same unmodified document and range should use the existing cached item
            // instead of generating a new cached item.
            await RunGetCodeActionsAsync(workspace.CurrentSolution, caretLocation);
            cacheResults = await cache.GetCacheAsync(document, caretLocation.Range, CancellationToken.None);
            Assert.True(cache.GetNumCacheItems() == 1);

            // Invoking code actions on a different range should generate a new cached item.
            caretLocation.Range = new LSP.Range
            {
                Start = new LSP.Position() { Line = 0, Character = 0 },
                End = new LSP.Position() { Line = 0, Character = 0 }
            };

            await RunGetCodeActionsAsync(workspace.CurrentSolution, caretLocation);
            Assert.True(cache.GetNumCacheItems() == 2);

            // Changing the document should generate a new cached item.
            var currentDocText = await document.GetTextAsync();
            var changedSourceText = currentDocText.WithChanges(new TextChange(new TextSpan(0, 0), "class D { } \n"));
            var docId = ((TestWorkspace)workspace).Documents.First().Id;
            ((TestWorkspace)workspace).ChangeDocument(docId, changedSourceText);
            UpdateSolutionProvider((TestWorkspace)workspace, workspace.CurrentSolution);

            await RunGetCodeActionsAsync(workspace.CurrentSolution, caretLocation);
            Assert.True(cache.GetNumCacheItems() == 3);

            // The current cache size is 3. Adding a 4th item to the cache should still keep the cache size the same.
            caretLocation.Range = new LSP.Range
            {
                Start = new LSP.Position() { Line = 0, Character = 0 },
                End = new LSP.Position() { Line = 0, Character = 1 }
            };

            await RunGetCodeActionsAsync(workspace.CurrentSolution, caretLocation);
            Assert.True(cache.GetNumCacheItems() == 3);
        }

        private static async Task<LSP.VSCodeAction[]> RunGetCodeActionsAsync(
            Solution solution,
            LSP.Location caret,
            LSP.ClientCapabilities clientCapabilities = null)
        {
            var result = await GetLanguageServer(solution).ExecuteRequestAsync<LSP.CodeActionParams, LSP.VSCodeAction[]>(
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
            var exportProvider = ((TestWorkspace)workspace).ExportProvider.GetExportedValue<ILspSolutionProvider>();
            var result = Assert.IsType<TestLspSolutionProvider>(exportProvider);

            return result.GetDocument(textDocument);
        }
    }
}
