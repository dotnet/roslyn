// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.AddImport;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.CodeActions;

public class CodeActionsTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    [WpfTheory, CombinatorialData]
    public async Task TestCodeActionHandlerAsync(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {
                    {|caret:|}int i = 1;
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, initializationOptions: new InitializationOptions() { ClientCapabilities = new VSInternalClientCapabilities { SupportsVisualStudioExtensions = true } });

        var caretLocation = testLspServer.GetLocations("caret").Single();
        var expected = CreateCodeAction(
            title: CSharpAnalyzersResources.Use_implicit_type,
            kind: CodeActionKind.Refactor,
            children: Array.Empty<VSInternalCodeAction>(),
            data: CreateCodeActionResolveData(
                CSharpAnalyzersResources.Use_implicit_type,
                caretLocation,
                customTags: new[] { PredefinedCodeRefactoringProviderNames.UseImplicitType }),
            priority: VSInternalPriorityLevel.Low,
            groupName: "Roslyn2",
            applicableRange: new LSP.Range { Start = new Position { Line = 4, Character = 8 }, End = new Position { Line = 4, Character = 11 } },
            diagnostics: null);

        var results = await RunGetCodeActionsAsync(testLspServer, CreateCodeActionParams(caretLocation));
        var useImplicitType = results.FirstOrDefault(r => r.Title == CSharpAnalyzersResources.Use_implicit_type);

        AssertJsonEquals(expected, useImplicitType);
    }

    [WpfTheory, CombinatorialData]
    public async Task TestCodeActionHandlerAsync_NestedAction(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {
                    int {|caret:|}i = 1;
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);

        var caretLocation = testLspServer.GetLocations("caret").Single();
        var expected = CreateCodeAction(
            title: string.Format(FeaturesResources.Introduce_constant_for_0, "1"),
            kind: CodeActionKind.Refactor,
            children: Array.Empty<VSInternalCodeAction>(),
            data: CreateCodeActionResolveData(
                FeaturesResources.Introduce_constant + '|' + string.Format(FeaturesResources.Introduce_constant_for_0, "1"),
                caretLocation),
            priority: VSInternalPriorityLevel.Normal,
            groupName: "Roslyn3",
            applicableRange: new LSP.Range { Start = new Position { Line = 4, Character = 12 }, End = new Position { Line = 4, Character = 12 } },
            diagnostics: null);

        var results = await RunGetCodeActionsAsync(testLspServer, CreateCodeActionParams(caretLocation));

        var topLevelAction = Assert.Single(results.Where(action => action.Title == FeaturesResources.Introduce_constant));
        var expectedChildActionTitle = FeaturesResources.Introduce_constant + '|' + string.Format(FeaturesResources.Introduce_constant_for_0, "1");
        var introduceConstant = topLevelAction.Children.FirstOrDefault(
            r => ((JObject)r.Data!).ToObject<CodeActionResolveData>()!.UniqueIdentifier == expectedChildActionTitle);

        AssertJsonEquals(expected, introduceConstant);
    }

    [WpfTheory, CombinatorialData]
    public async Task TestCodeActionHasCorrectDiagnostics(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {
                    {|caret:|}Task.Delay(1);
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var caret = testLspServer.GetLocations("caret").Single();
        var codeActionParams = new CodeActionParams
        {
            TextDocument = CreateTextDocumentIdentifier(caret.Uri),
            Range = caret.Range,
            Context = new CodeActionContext
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
        Assert.Equal(1, addImport.Diagnostics!.Length);
        Assert.Equal(AddImportDiagnosticIds.CS0103, addImport.Diagnostics.Single().Code!.Value);
    }

    [WpfTheory, CombinatorialData]
    public async Task TestNoSuppressionFixerInStandardLSP(bool mutatingLspWorkspace)
    {
        var markup = """
            class ABC
            {
                private static async void {|caret:XYZ|}()
                {
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var caret = testLspServer.GetLocations("caret").Single();
        var codeActionParams = new CodeActionParams
        {
            TextDocument = CreateTextDocumentIdentifier(caret.Uri),
            Range = caret.Range,
            Context = new CodeActionContext
            {
                Diagnostics = new[]
                {
                    new LSP.Diagnostic
                    {
                        // async method lack of await.
                        Code = "CS1998"
                    }
                }
            }
        };

        var results = await RunGetCodeActionsAsync(testLspServer, codeActionParams);
        Assert.Single(results);
        Assert.Equal("Make method synchronous", results[0].Title);
    }

    [WpfTheory, CombinatorialData]
    public async Task TestStandardLspNestedCodeAction(bool mutatingLspWorkspace)
    {
        var markup = """
            class ABC
            {
                private void XYZ()
                {
                    var a = {|caret:A()|};
                }

                private int A() => 1;
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var caret = testLspServer.GetLocations("caret").Single();
        var codeActionParams = new CodeActionParams
        {
            TextDocument = CreateTextDocumentIdentifier(caret.Uri),
            Range = caret.Range,
            Context = new CodeActionContext
            {
            }
        };

        var results = await RunGetCodeActionsAsync(testLspServer, codeActionParams);
        var resultsTitles = results.Select(r => r.Title).ToArray();
        // Inline method refactoring provide nested code actions.
        // Make sure it is correctly displayed.
        Assert.True(resultsTitles.Contains("Inline 'A()' -> Inline 'A()'"));
        Assert.True(resultsTitles.Contains("Inline 'A()' -> Inline and keep 'A()'"));
    }

    private static async Task<VSInternalCodeAction[]> RunGetCodeActionsAsync(
        TestLspServer testLspServer,
        CodeActionParams codeActionParams)
    {
        var result = await testLspServer.ExecuteRequestAsync<CodeActionParams, CodeAction[]>(
            LSP.Methods.TextDocumentCodeActionName, codeActionParams, CancellationToken.None);
        return result.Cast<VSInternalCodeAction>().ToArray();
    }

    internal static CodeActionParams CreateCodeActionParams(LSP.Location caret)
        => new CodeActionParams
        {
            TextDocument = CreateTextDocumentIdentifier(caret.Uri),
            Range = caret.Range,
            Context = new CodeActionContext
            {
                // TODO - Code actions should respect context.
            }
        };

    internal static VSInternalCodeAction CreateCodeAction(
        string title, CodeActionKind kind, VSInternalCodeAction[] children,
        CodeActionResolveData data, LSP.Diagnostic[]? diagnostics,
        VSInternalPriorityLevel? priority, string groupName, LSP.Range applicableRange,
        WorkspaceEdit? edit = null, Command? command = null)
    {
        var action = new VSInternalCodeAction
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
}
