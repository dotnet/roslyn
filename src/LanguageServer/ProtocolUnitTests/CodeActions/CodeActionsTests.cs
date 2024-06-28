// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.AddImport;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;
using System.Text.Json;

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

        var titlePath = new[] { CSharpAnalyzersResources.Use_implicit_type };
        var caretLocation = testLspServer.GetLocations("caret").Single();
        var expected = CreateCodeAction(
            title: CSharpAnalyzersResources.Use_implicit_type,
            kind: CodeActionKind.Refactor,
            children: [],
            data: CreateCodeActionResolveData(
                CSharpAnalyzersResources.Use_implicit_type,
                caretLocation,
                codeActionPath: titlePath,
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
        var titlePath = new[] { FeaturesResources.Introduce_constant, string.Format(FeaturesResources.Introduce_constant_for_0, "1") };
        var expected = CreateCodeAction(
            title: string.Format(FeaturesResources.Introduce_constant_for_0, "1"),
            kind: CodeActionKind.Refactor,
            children: [],
            data: CreateCodeActionResolveData(
                string.Format(FeaturesResources.Introduce_constant_for_0, "1"),
                caretLocation,
                codeActionPath: titlePath),
            priority: VSInternalPriorityLevel.Normal,
            groupName: "Roslyn3",
            applicableRange: new LSP.Range { Start = new Position { Line = 4, Character = 12 }, End = new Position { Line = 4, Character = 12 } },
            diagnostics: null);

        var results = await RunGetCodeActionsAsync(testLspServer, CreateCodeActionParams(caretLocation));

        var topLevelAction = Assert.Single(results.Where(action => action.Title == titlePath[0]));
        var introduceConstant = topLevelAction.Children.FirstOrDefault(
            r => JsonSerializer.Deserialize<CodeActionResolveData>((JsonElement)r.Data!, ProtocolConversions.LspJsonSerializerOptions)!.UniqueIdentifier == titlePath[1]);

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
                Diagnostics =
                [
                    new LSP.Diagnostic
                    {
                        Code = AddImportDiagnosticIds.CS0103
                    },
                    new LSP.Diagnostic
                    {
                        Code = "SomeCode"
                    }
                ]
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
                Diagnostics =
                [
                    new LSP.Diagnostic
                    {
                        // async method lack of await.
                        Code = "CS1998"
                    }
                ]
            }
        };

        var results = await RunGetCodeActionsAsync(testLspServer, codeActionParams);
        Assert.Equal(3, results.Length);
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
        var inline = results.FirstOrDefault(r => r.Title.Contains($"Inline 'A()'"));
        var data = GetCodeActionResolveData(inline);
        Assert.NotNull(data);

        // Asserts that there are NestedActions on Inline
        Assert.NotNull(data!.NestedCodeActions);
        Assert.NotEmpty(data!.NestedCodeActions);

        // Asserts that the second NestedAction's path is correct
        var nestedActionData = GetCodeActionResolveData(data!.NestedCodeActions!.Value[1]);
        Assert.NotNull(nestedActionData);
        Assert.Equal("Inline 'A()'", nestedActionData!.CodeActionPath[0]);
        Assert.Equal("Inline and keep 'A()'", nestedActionData!.CodeActionPath[1]);

        // Asserts that there is a Command present on an action with nested actions
        Assert.NotNull(inline.Command);
    }

    [WpfTheory, CombinatorialData]
    public async Task TestStandardLspNestedFixAllCodeAction(bool mutatingLspWorkspace)
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
                Diagnostics =
                [
                    new LSP.Diagnostic
                    {
                        // async method lack of await.
                        Code = "CS1998"
                    }
                ]
            }
        };

        var results = await RunGetCodeActionsAsync(testLspServer, codeActionParams);
        Assert.Equal(3, results.Length);
        Assert.Equal("Suppress or configure issues", results[2].Title);
        var data = GetCodeActionResolveData(results[2]);
        Assert.NotNull(data);

        // Asserts that there are NestedActions present
        Assert.NotNull(data!.NestedCodeActions);

        //Asserts that a Nested Action could be a Fix All Action
        Assert.Equal("Fix All: in Source", data.NestedCodeActions!.Value[1].Title);
    }

    [WpfTheory, CombinatorialData]
    public async Task TestStandardLspNestedResolveTopLevelCodeAction(bool mutatingLspWorkspace)
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
        // Assert that nested code actions aren't enumerated.
        var inline = results.FirstOrDefault(r => r.Title.Contains($"Inline 'A()'"));
        var resolvedAction = await RunGetCodeActionResolveAsync(testLspServer, inline);
        Assert.Null(resolvedAction.Edit);
    }

    private static async Task<VSInternalCodeAction[]> RunGetCodeActionsAsync(
        TestLspServer testLspServer,
        CodeActionParams codeActionParams)
    {
        var result = await testLspServer.ExecuteRequestAsync<CodeActionParams, CodeAction[]>(
            LSP.Methods.TextDocumentCodeActionName, codeActionParams, CancellationToken.None);
        return result.Cast<VSInternalCodeAction>().ToArray();
    }

    private static async Task<VSInternalCodeAction> RunGetCodeActionResolveAsync(
        TestLspServer testLspServer,
        CodeAction codeAction)
    {
        var result = await testLspServer.ExecuteRequestAsync<CodeAction, CodeAction>(
            LSP.Methods.CodeActionResolveName, codeAction, CancellationToken.None);
        Assert.NotNull(result);
        return (VSInternalCodeAction)result!;
    }

    private static CodeActionResolveData? GetCodeActionResolveData(CodeAction codeAction)
    {
        return JsonSerializer.Deserialize<CodeActionResolveData>((JsonElement)codeAction.Data!, ProtocolConversions.LspJsonSerializerOptions);
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
            Data = JsonSerializer.SerializeToElement(data, ProtocolConversions.LspJsonSerializerOptions),
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
