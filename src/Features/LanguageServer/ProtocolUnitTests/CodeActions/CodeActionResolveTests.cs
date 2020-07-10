// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.CodeActions
{
    public class CodeActionResolveTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task TestCodeActionResolveHandlerAsync()
        {
            var initialMarkup =
@"class A
{
    void M()
    {
        {|caret:|}int i = 1;
    }
}";
            using var workspace = CreateTestWorkspace(initialMarkup, out var locations);

            var unresolvedCodeAction = CodeActionsTests.CreateCodeAction(
                title: CSharpAnalyzersResources.Use_implicit_type,
                kind: CodeActionKind.Refactor,
                children: Array.Empty<LSP.VSCodeAction>(),
                data: CreateCodeActionResolveData(CSharpAnalyzersResources.Use_implicit_type, locations["caret"].Single()),
                diagnostics: null);

            var expectedMarkup =
@"class A
{
    void M()
    {
        var i = 1;
    }
}";
            var expected = CodeActionsTests.CreateCodeAction(
                title: CSharpAnalyzersResources.Use_implicit_type,
                kind: CodeActionKind.Refactor,
                children: Array.Empty<LSP.VSCodeAction>(),
                data: CreateCodeActionResolveData(CSharpAnalyzersResources.Use_implicit_type, locations["caret"].Single()),
                diagnostics: null,
                edit: GenerateWorkspaceEdit(
                    locations, expectedMarkup, new LSP.Range { Start = new Position(0, 0), End = new Position(6, 1) }));

            var result = await RunGetCodeActionResolveAsync(workspace.CurrentSolution, unresolvedCodeAction);
            AssertJsonEquals(expected, result);
        }

        [Fact]
        public async Task TestCodeActionResolveHandlerAsync_NestedAction()
        {
            var initialMarkup =
@"class A
{
    void M()
    {
        int {|caret:|}i = 1;
    }
}";
            using var workspace = CreateTestWorkspace(initialMarkup, out var locations);

            var unresolvedCodeAction = CodeActionsTests.CreateCodeAction(
                title: string.Format(FeaturesResources.Introduce_constant_for_0, "1"),
                kind: CodeActionKind.Refactor,
                children: Array.Empty<LSP.VSCodeAction>(),
                data: CreateCodeActionResolveData(
                    FeaturesResources.Introduce_constant + "|" + string.Format(FeaturesResources.Introduce_constant_for_0, "1"),
                    locations["caret"].Single()),
                diagnostics: null);

            var expectedMarkup =
@"class A
{
    private const int V = 1;

    void M()
    {
        int i = V;
    }
}";

            var expected = CodeActionsTests.CreateCodeAction(
                title: string.Format(FeaturesResources.Introduce_constant_for_0, "1"),
                kind: CodeActionKind.Refactor,
                children: Array.Empty<LSP.VSCodeAction>(),
                data: CreateCodeActionResolveData(
                    FeaturesResources.Introduce_constant + "|" + string.Format(FeaturesResources.Introduce_constant_for_0, "1"),
                    locations["caret"].Single()),
                diagnostics: null,
                edit: GenerateWorkspaceEdit(
                    locations, expectedMarkup, new LSP.Range { Start = new Position(0, 0), End = new Position(6, 1) }));

            var result = await RunGetCodeActionResolveAsync(workspace.CurrentSolution, unresolvedCodeAction);
            AssertJsonEquals(expected, result);
        }

        private static async Task<LSP.VSCodeAction> RunGetCodeActionResolveAsync(
            Solution solution,
            VSCodeAction unresolvedCodeAction,
            LSP.ClientCapabilities clientCapabilities = null)
        {
            var result = await GetLanguageServer(solution).ExecuteRequestAsync<LSP.VSCodeAction, LSP.VSCodeAction>(
                LSP.MSLSPMethods.TextDocumentCodeActionResolveName, unresolvedCodeAction,
                clientCapabilities, null, CancellationToken.None);
            return result;
        }

        private static WorkspaceEdit GenerateWorkspaceEdit(
            Dictionary<string, IList<LSP.Location>> locations,
            string expectedMarkup,
            LSP.Range range)
            => new LSP.WorkspaceEdit
            {
                DocumentChanges = new TextDocumentEdit[]
                {
                    new TextDocumentEdit
                    {
                        TextDocument = new VersionedTextDocumentIdentifier
                        {
                            Uri = locations["caret"].Single().Uri
                        },
                        Edits = new TextEdit[]
                        {
                            new TextEdit
                            {
                                NewText = expectedMarkup,
                                Range = range
                            }
                        }
                    }
                }
            };
    }
}
