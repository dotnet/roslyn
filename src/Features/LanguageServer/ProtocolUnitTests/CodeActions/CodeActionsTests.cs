// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.AddImport;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;
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
            using var testLspServer = await CreateTestLspServerAsync(markup);

            var caretLocation = testLspServer.GetLocations("caret").Single();
            var expected = CreateCodeAction(
                title: CSharpAnalyzersResources.Use_implicit_type,
                kind: CodeActionKind.Refactor,
                children: Array.Empty<LSP.VSInternalCodeAction>(),
                data: CreateCodeActionResolveData(
                    CSharpAnalyzersResources.Use_implicit_type,
                    caretLocation,
                    customTags: new[] { PredefinedCodeRefactoringProviderNames.UseImplicitType }),
                priority: VSInternalPriorityLevel.Low,
                groupName: "Roslyn1",
                applicableRange: new LSP.Range { Start = new Position { Line = 4, Character = 8 }, End = new Position { Line = 4, Character = 11 } },
                diagnostics: null);

            var results = await RunGetCodeActionsAsync(testLspServer, CreateCodeActionParams(caretLocation));
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
            using var testLspServer = await CreateTestLspServerAsync(markup);

            var caretLocation = testLspServer.GetLocations("caret").Single();
            var expected = CreateCodeAction(
                title: string.Format(FeaturesResources.Introduce_constant_for_0, "1"),
                kind: CodeActionKind.Refactor,
                children: Array.Empty<LSP.VSInternalCodeAction>(),
                data: CreateCodeActionResolveData(
                    FeaturesResources.Introduce_constant + '|' + string.Format(FeaturesResources.Introduce_constant_for_0, "1"),
                    caretLocation),
                priority: VSInternalPriorityLevel.Normal,
                groupName: "Roslyn2",
                applicableRange: new LSP.Range { Start = new Position { Line = 4, Character = 12 }, End = new Position { Line = 4, Character = 12 } },
                diagnostics: null);

            var results = await RunGetCodeActionsAsync(testLspServer, CreateCodeActionParams(caretLocation));
            var introduceConstant = results[0].Children.FirstOrDefault(
                r => ((JObject)r.Data).ToObject<CodeActionResolveData>().UniqueIdentifier == FeaturesResources.Introduce_constant
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
            using var testLspServer = await CreateTestLspServerAsync(markup);
            var cache = GetCodeActionsCache(testLspServer);
            var testAccessor = cache.GetTestAccessor();

            // This test assumes that the maximum cache size is 3, and will have to modified if this number changes.
            Assert.True(CodeActionsCache.TestAccessor.MaximumCacheSize == 3);

            var caretLocation = testLspServer.GetLocations("caret").Single();
            var document = GetDocument(testLspServer.TestWorkspace, CreateTextDocumentIdentifier(caretLocation.Uri));

            // 1. Invoking code actions on document with empty cache.
            await RunCodeActionsAndAssertActionsInCacheAsync(testLspServer, cache, caretLocation, document);

            // Ensuring contents of cache are as expected.
            var docAndRange = testAccessor.GetDocumentsAndRangesInCache().Single();
            AssertRangeAndDocEqual(caretLocation.Range, document, docAndRange);

            // 2. Invoking code actions on the same unmodified document and range should use the existing cached item
            // instead of generating a new cached item.
            await RunCodeActionsAndAssertActionsInCacheAsync(testLspServer, cache, caretLocation, document);

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

            await RunCodeActionsAndAssertActionsInCacheAsync(testLspServer, cache, caretLocation, document);

            // Ensuring contents of cache are as expected.
            var docsAndRanges = testAccessor.GetDocumentsAndRangesInCache();
            Assert.True(docsAndRanges.Count == 2);
            AssertRangeAndDocEqual(originalRange, document, docsAndRanges[0]);
            AssertRangeAndDocEqual(caretLocation.Range, document, docsAndRanges[1]);

            // 4. Changing the document should generate a new cached item.
            var currentDocText = await document.GetTextAsync();
            var changedSourceText = currentDocText.WithChanges(new TextChange(new TextSpan(0, 0), "class D { } \n"));
            testLspServer.TestWorkspace.TryApplyChanges(document.WithText(changedSourceText).Project.Solution);

            var docId = testLspServer.TestWorkspace.Documents.First().Id;
            await testLspServer.TestWorkspace.ChangeDocumentAsync(docId, changedSourceText);

            var updatedDocument = GetDocument(testLspServer.TestWorkspace, CreateTextDocumentIdentifier(caretLocation.Uri));

            await RunCodeActionsAndAssertActionsInCacheAsync(testLspServer, cache, caretLocation, updatedDocument);

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

            await RunCodeActionsAndAssertActionsInCacheAsync(testLspServer, cache, caretLocation, updatedDocument);

            // Ensuring contents of cache are as expected.
            docsAndRanges = testAccessor.GetDocumentsAndRangesInCache();
            AssertRangeAndDocEqual(updatedRange, document, docsAndRanges[0]);
            AssertRangeAndDocEqual(updatedRange, updatedDocument, docsAndRanges[1]);
            AssertRangeAndDocEqual(caretLocation.Range, updatedDocument, docsAndRanges[2]);
        }

        [WpfFact]
        public async Task TestCodeActionHasCorrectDiagnostics()
        {
            var markup =
@"class A
{
    void M()
    {
        {|caret:|}Task.Delay(1);
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);

            testLspServer.InitializeDiagnostics(BackgroundAnalysisScope.ActiveFile, DiagnosticMode.Default,
                new TestAnalyzerReferenceByLanguage(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap()));
            await testLspServer.WaitForDiagnosticsAsync();

            var caret = testLspServer.GetLocations("caret").Single();
            var codeActionParams = new LSP.CodeActionParams
            {
                TextDocument = CreateTextDocumentIdentifier(caret.Uri),
                Range = caret.Range,
                Context = new LSP.CodeActionContext
                {
                    Diagnostics = new[]
                    {
                        new LSP.Diagnostic
                        {
                            Code = AddImportDiagnosticIds.CS0103
                        },
                        new LSP.Diagnostic
                        {
                            Code = "SomeCode"
                        }
                    }
                }
            };

            var results = await RunGetCodeActionsAsync(testLspServer, codeActionParams);
            var addImport = results.FirstOrDefault(r => r.Title.Contains($"using System.Threading.Tasks"));
            Assert.Equal(1, addImport.Diagnostics.Length);
            Assert.Equal(AddImportDiagnosticIds.CS0103, addImport.Diagnostics.Single().Code.Value);
        }

        private static async Task RunCodeActionsAndAssertActionsInCacheAsync(
            TestLspServer testLspServer,
            CodeActionsCache cache,
            LSP.Location caretLocation,
            Document document)
        {
            await RunGetCodeActionsAsync(testLspServer, CreateCodeActionParams(caretLocation));
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

        private static async Task<LSP.VSInternalCodeAction[]> RunGetCodeActionsAsync(
            TestLspServer testLspServer,
            CodeActionParams codeActionParams)
        {
            var result = await testLspServer.ExecuteRequestAsync<LSP.CodeActionParams, LSP.CodeAction[]>(
                LSP.Methods.TextDocumentCodeActionName, codeActionParams, CancellationToken.None);
            return result.Cast<LSP.VSInternalCodeAction>().ToArray();
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

        internal static LSP.VSInternalCodeAction CreateCodeAction(
            string title, LSP.CodeActionKind kind, LSP.VSInternalCodeAction[] children,
            CodeActionResolveData data, LSP.Diagnostic[] diagnostics,
            LSP.VSInternalPriorityLevel? priority, string groupName, LSP.Range applicableRange,
            LSP.WorkspaceEdit edit = null, LSP.Command command = null)
        {
            var action = new LSP.VSInternalCodeAction
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

        private static CodeActionsCache GetCodeActionsCache(TestLspServer testLspServer)
        {
            var dispatchAccessor = testLspServer.GetDispatcherAccessor();
            var handler = (CodeActionsHandler)dispatchAccessor.GetHandler<LSP.CodeActionParams, LSP.CodeAction[]>(LSP.Methods.TextDocumentCodeActionName);
            Assert.NotNull(handler);
            var cache = handler.GetTestAccessor().GetCache();
            return Assert.IsType<CodeActionsCache>(cache);
        }

        private static Document GetDocument(Workspace workspace, LSP.TextDocumentIdentifier textDocument)
        {
            return workspace.CurrentSolution.GetDocument(textDocument);
        }
    }
}
