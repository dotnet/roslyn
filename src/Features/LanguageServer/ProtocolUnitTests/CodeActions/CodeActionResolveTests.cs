// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.CodeActions
{
    public class CodeActionResolveTests : AbstractLanguageServerProtocolTests
    {
        public CodeActionResolveTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [WpfTheory, CombinatorialData]
        public async Task TestCodeActionResolveHandlerAsync(bool mutatingLspWorkspace)
        {
            var initialMarkup =
@"class A
{
    void M()
    {
        {|caret:|}int i = 1;
    }
}";
            await using var testLspServer = await CreateTestLspServerAsync(initialMarkup, mutatingLspWorkspace);

            var unresolvedCodeAction = CodeActionsTests.CreateCodeAction(
                title: CSharpAnalyzersResources.Use_implicit_type,
                kind: CodeActionKind.Refactor,
                children: Array.Empty<LSP.VSInternalCodeAction>(),
                data: CreateCodeActionResolveData(CSharpAnalyzersResources.Use_implicit_type, testLspServer.GetLocations("caret").Single()),
                priority: VSInternalPriorityLevel.Low,
                groupName: "Roslyn1",
                applicableRange: new LSP.Range { Start = new Position { Line = 4, Character = 8 }, End = new Position { Line = 4, Character = 11 } },
                diagnostics: null);

            // Expected text after edit:
            //     class A
            //     {
            //         void M()
            //         {
            //             var i = 1;
            //         }
            //     }
            var expectedTextEdits = new LSP.TextEdit[]
            {
                GenerateTextEdit("var", new LSP.Range { Start = new Position(4, 8), End = new Position(4, 11) })
            };

            var expectedResolvedAction = CodeActionsTests.CreateCodeAction(
                title: CSharpAnalyzersResources.Use_implicit_type,
                kind: CodeActionKind.Refactor,
                children: Array.Empty<LSP.VSInternalCodeAction>(),
                data: CreateCodeActionResolveData(CSharpAnalyzersResources.Use_implicit_type, testLspServer.GetLocations("caret").Single()),
                priority: VSInternalPriorityLevel.Low,
                groupName: "Roslyn1",
                diagnostics: null,
                applicableRange: new LSP.Range { Start = new Position { Line = 4, Character = 8 }, End = new Position { Line = 4, Character = 11 } },
                edit: GenerateWorkspaceEdit(testLspServer.GetLocations("caret"), expectedTextEdits));

            var actualResolvedAction = await RunGetCodeActionResolveAsync(testLspServer, unresolvedCodeAction);
            AssertJsonEquals(expectedResolvedAction, actualResolvedAction);
        }

        [WpfTheory, CombinatorialData]
        public async Task TestCodeActionResolveHandlerAsync_NestedAction(bool mutatingLspWorkspace)
        {
            var initialMarkup =
@"class A
{
    void M()
    {
        int {|caret:|}i = 1;
    }
}";
            await using var testLspServer = await CreateTestLspServerAsync(initialMarkup, mutatingLspWorkspace);

            var unresolvedCodeAction = CodeActionsTests.CreateCodeAction(
                title: string.Format(FeaturesResources.Introduce_constant_for_0, "1"),
                kind: CodeActionKind.Refactor,
                children: Array.Empty<LSP.VSInternalCodeAction>(),
                data: CreateCodeActionResolveData(
                    FeaturesResources.Introduce_constant + "|" + string.Format(FeaturesResources.Introduce_constant_for_0, "1"),
                    testLspServer.GetLocations("caret").Single()),
                priority: VSInternalPriorityLevel.Normal,
                groupName: "Roslyn2",
                applicableRange: new LSP.Range { Start = new Position { Line = 4, Character = 8 }, End = new Position { Line = 4, Character = 11 } },
                diagnostics: null);

            // Expected text after edits:
            //     class A
            //     {
            //         private const int V = 1;
            //
            //         void M()
            //         {
            //             int i = V;
            //         }
            //     }
            var expectedTextEdits = new LSP.TextEdit[]
            {
                GenerateTextEdit(@"private const int V = 1;

", new LSP.Range { Start = new Position(2, 4), End = new Position(2, 4) }),
                GenerateTextEdit("V", new LSP.Range { Start = new Position(4, 16), End = new Position(4, 17) })
            };

            var expectedResolvedAction = CodeActionsTests.CreateCodeAction(
                title: string.Format(FeaturesResources.Introduce_constant_for_0, "1"),
                kind: CodeActionKind.Refactor,
                children: Array.Empty<LSP.VSInternalCodeAction>(),
                data: CreateCodeActionResolveData(
                    FeaturesResources.Introduce_constant + "|" + string.Format(FeaturesResources.Introduce_constant_for_0, "1"),
                    testLspServer.GetLocations("caret").Single()),
                priority: VSInternalPriorityLevel.Normal,
                groupName: "Roslyn2",
                applicableRange: new LSP.Range { Start = new Position { Line = 4, Character = 8 }, End = new Position { Line = 4, Character = 11 } },
                diagnostics: null,
                edit: GenerateWorkspaceEdit(
                    testLspServer.GetLocations("caret"), expectedTextEdits));

            var actualResolvedAction = await RunGetCodeActionResolveAsync(testLspServer, unresolvedCodeAction);
            AssertJsonEquals(expectedResolvedAction, actualResolvedAction);
        }

        [WpfTheory, CombinatorialData]
        public async Task TestRename(bool mutatingLspWorkspace)
        {
            var markUp = @"
class {|caret:ABC|}
{
}";

            await using var testLspServer = await CreateTestLspServerAsync(markUp, mutatingLspWorkspace, new InitializationOptions
            {
                ClientCapabilities = new ClientCapabilities()
                {
                    Workspace = new WorkspaceClientCapabilities
                    {
                        WorkspaceEdit = new WorkspaceEditSetting
                        {
                            ResourceOperations = new ResourceOperationKind[] { ResourceOperationKind.Rename }
                        }
                    }
                }
            });
            var unresolvedCodeAction = CodeActionsTests.CreateCodeAction(
                title: string.Format(FeaturesResources.Rename_file_to_0, "ABC.cs"),
                kind: CodeActionKind.Refactor,
                children: Array.Empty<LSP.VSInternalCodeAction>(),
                data: CreateCodeActionResolveData(
                    string.Format(FeaturesResources.Rename_file_to_0, "ABC.cs"),
                    testLspServer.GetLocations("caret").Single()),
                priority: VSInternalPriorityLevel.Normal,
                groupName: "Roslyn2",
                applicableRange: new LSP.Range { Start = new Position { Line = 0, Character = 6 }, End = new Position { Line = 0, Character = 9 } },
                diagnostics: null);

            var testWorkspace = testLspServer.TestWorkspace;
            var documentBefore = testWorkspace.CurrentSolution.GetDocument(testWorkspace.Documents.Single().Id);
            var documentUriBefore = documentBefore.GetUriFromName();

            var actualResolvedAction = await RunGetCodeActionResolveAsync(testLspServer, unresolvedCodeAction);

            var documentAfter = testWorkspace.CurrentSolution.GetDocument(testWorkspace.Documents.Single().Id);
            var documentUriAfter = documentBefore.WithName("ABC.cs").GetUriFromName();

            var expectedCodeAction = CodeActionsTests.CreateCodeAction(
                title: string.Format(FeaturesResources.Rename_file_to_0, "ABC.cs"),
                kind: CodeActionKind.Refactor,
                children: Array.Empty<LSP.VSInternalCodeAction>(),
                data: CreateCodeActionResolveData(
                    string.Format(FeaturesResources.Rename_file_to_0, "ABC.cs"),
                    testLspServer.GetLocations("caret").Single()),
                priority: VSInternalPriorityLevel.Normal,
                groupName: "Roslyn2",
                applicableRange: new LSP.Range { Start = new Position { Line = 0, Character = 6 }, End = new Position { Line = 0, Character = 9 } },
                diagnostics: null,
                edit: GenerateRenameFileEdit(new List<(Uri, Uri)> { (documentUriBefore, documentUriAfter) }));

            AssertJsonEquals(expectedCodeAction, actualResolvedAction);
        }

        private static async Task<LSP.VSInternalCodeAction> RunGetCodeActionResolveAsync(
            TestLspServer testLspServer,
            VSInternalCodeAction unresolvedCodeAction)
        {
            var result = (VSInternalCodeAction)await testLspServer.ExecuteRequestAsync<LSP.CodeAction, LSP.CodeAction>(
                LSP.Methods.CodeActionResolveName, unresolvedCodeAction, CancellationToken.None);
            return result;
        }

        private static LSP.TextEdit GenerateTextEdit(string newText, LSP.Range range)
            => new LSP.TextEdit
            {
                NewText = newText,
                Range = range
            };

        private static WorkspaceEdit GenerateWorkspaceEdit(
            IList<LSP.Location> locations,
            TextEdit[] edits)
            => new LSP.WorkspaceEdit
            {
                DocumentChanges = new TextDocumentEdit[]
                {
                    new TextDocumentEdit
                    {
                        TextDocument = new OptionalVersionedTextDocumentIdentifier
                        {
                            Uri = locations.Single().Uri
                        },
                        Edits = edits,
                    }
                }
            };

        private static WorkspaceEdit GenerateRenameFileEdit(IList<(Uri oldUri, Uri newUri)> renameLocations)
            => new()
            {
                DocumentChanges = renameLocations.Select(
                    locations => new SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>(new RenameFile() { OldUri = locations.oldUri, NewUri = locations.newUri })).ToArray()
            };
    }
}
