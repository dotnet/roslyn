// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.AddImport;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using CodeActionRequestPriority = Microsoft.CodeAnalysis.CodeActions.CodeActionRequestPriority;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.CodeActions;

public sealed class CodeActionsTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    [Theory, CombinatorialData]
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

    [Theory, CombinatorialData]
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

        var topLevelAction = Assert.Single(results, action => action.Title == titlePath[0]);
        var introduceConstant = topLevelAction.Children!.FirstOrDefault(
            r => JsonSerializer.Deserialize<CodeActionResolveData>((JsonElement)r.Data!, ProtocolConversions.LspJsonSerializerOptions)!.UniqueIdentifier == titlePath[1]);

        AssertJsonEquals(expected, introduceConstant);
    }

    [Theory, CombinatorialData]
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
            TextDocument = CreateTextDocumentIdentifier(caret.DocumentUri),
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
        Assert.Equal(1, addImport!.Diagnostics!.Length);
        Assert.Equal(AddImportDiagnosticIds.CS0103, addImport.Diagnostics.Single().Code!.Value);
    }

    [Theory, CombinatorialData]
    public async Task TestCodeActionHelperFiltersRequestedKindsAndSkipsUnrequestedServices(
        bool mutatingLspWorkspace,
        bool hasVsLspCapability,
        [CombinatorialValues("quickfix", "refactor", "refactor.extract", "refactor.inline", "quickfix,refactor.extract")] string requestedKinds)
    {
        // Context.Only should restrict both computation and returned action kinds for standard and VS clients.
        // Select statements offering missing-using fixes and extraction refactorings, or a local variable to inline.
        var markup = requestedKinds == "refactor.inline"
            ? "class C { void M() { int {|caret:value|} = 1; System.Console.WriteLine(value); } }"
            : "class C { void M() { {|caret:int value = 1; Task.Delay(value);|} } }";
        await using var server = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var request = CreateCodeActionParams(server.GetLocations("caret").Single());
        request.Context.Only = requestedKinds.Split(',').Select(kind => new CodeActionKind(kind)).ToArray();

        // Only requested categories get real services. Computing an excluded category fails immediately.
        var codeFixService = requestedKinds.Contains("quickfix")
            ? server.TestWorkspace.ExportProvider.GetExportedValue<ICodeFixService>()
            : new ThrowingCodeFixService();
        var codeRefactoringService = requestedKinds.Contains("refactor")
            ? server.TestWorkspace.ExportProvider.GetExportedValue<ICodeRefactoringService>()
            : new ThrowingCodeRefactoringService();

        // Call discovery directly so the helper, rather than the request handler, must honor Context.Only.
        var actions = await CodeActionHelpers.GetVSCodeActionsAsync(
            request, server.GetCurrentSolution().Projects.Single().Documents.Single(),
            codeFixService, codeRefactoringService, hasVsLspCapability, CancellationToken.None);

        // Require results for every requested category and reject unrelated kinds.
        // A parent refactor filter also permits its extract and inline children.
        CodeActionKind?[] expectedKinds = requestedKinds switch
        {
            "quickfix" => [CodeActionKind.QuickFix],
            "refactor" => [CodeActionKind.Refactor, CodeActionKind.RefactorExtract, CodeActionKind.RefactorInline],
            "refactor.extract" => [CodeActionKind.RefactorExtract],
            "refactor.inline" => [CodeActionKind.RefactorInline],
            _ => [CodeActionKind.QuickFix, CodeActionKind.RefactorExtract],
        };
        foreach (var requestedKind in request.Context.Only)
            Assert.Contains(actions, action => action.Kind == requestedKind ||
                requestedKind == CodeActionKind.Refactor && action.Kind == CodeActionKind.RefactorExtract);
        Assert.All(actions, action => Assert.Contains(action.Kind, expectedKinds));
    }

    [Theory, CombinatorialData]
    public async Task TestCodeActionHelperSkipsUnsupportedKinds(
        bool mutatingLspWorkspace,
        bool hasVsLspCapability,
        [CombinatorialValues("source.fixAll", "unknown", "refactoring", "quickfix.other")] string requestedKind)
    {
        // Source actions and unsupported kinds must not trigger ordinary fix/refactoring computation.
        // Include a lookalike prefix and an unsupported child kind to verify hierarchical matching boundaries.
        await using var server = await CreateTestLspServerAsync("class C { {|caret:|} }", mutatingLspWorkspace);
        var request = CreateCodeActionParams(server.GetLocations("caret").Single());
        request.Context.Only = [new CodeActionKind(requestedKind)];

        // Both services throw if called, proving these filters can be rejected without computing any actions.
        var actions = await CodeActionHelpers.GetVSCodeActionsAsync(
            request, server.GetCurrentSolution().Projects.Single().Documents.Single(),
            new ThrowingCodeFixService(),
            new ThrowingCodeRefactoringService(),
            hasVsLspCapability, CancellationToken.None);

        // No ordinary action matches; source.fixAll is added separately by the request handler.
        Assert.Empty(actions);
    }

    [Theory, CombinatorialData]
    public async Task TestCodeActionHelperPreservesUnrestrictedFilters(
        bool mutatingLspWorkspace, bool hasVsLspCapability, bool useEmptyKind)
    {
        // Treat omitted, empty-list, and empty-parent filters as equivalent for either client format.
        var markup = "class C { void M() { {|caret:int value = 1; Task.Delay(value);|} } }";
        await using var server = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var request = CreateCodeActionParams(server.GetLocations("caret").Single());
        var document = server.GetCurrentSolution().Projects.Single().Documents.Single();
        var codeFixService = server.TestWorkspace.ExportProvider.GetExportedValue<ICodeFixService>();
        var codeRefactoringService = server.TestWorkspace.ExportProvider.GetExportedValue<ICodeRefactoringService>();

        // Capture the complete action list with Context.Only omitted as the baseline.
        var unfiltered = await CodeActionHelpers.GetVSCodeActionsAsync(
            request, document, codeFixService, codeRefactoringService, hasVsLspCapability, CancellationToken.None);

        // Repeat discovery with either an empty list or the empty parent kind, both meaning "all kinds".
        request.Context.Only = useEmptyKind ? [CodeActionKind.Empty] : [];
        var filtered = await CodeActionHelpers.GetVSCodeActionsAsync(
            request, document, codeFixService, codeRefactoringService, hasVsLspCapability, CancellationToken.None);

        // Require both categories in the baseline, then compare full payloads to catch filtering or metadata changes.
        Assert.Contains(unfiltered, action => action.Kind == CodeActionKind.QuickFix);
        Assert.Contains(unfiltered, action => action.Kind == CodeActionKind.RefactorExtract);
        AssertJsonEquals(unfiltered, filtered);
    }

    [ConditionalTheory(typeof(IsEnglishLocal)), CombinatorialData]
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
            TextDocument = CreateTextDocumentIdentifier(caret.DocumentUri),
            Range = caret.Range,
            Context = new CodeActionContext
            {
            }
        };

        var results = await RunGetCodeActionsAsync(testLspServer, codeActionParams);
        var inline = results.FirstOrDefault(r => r.Title.Contains($"Inline 'A()'"));
        var data = GetCodeActionResolveData(inline!);
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
        Assert.NotNull(inline?.Command);
    }

    [ConditionalTheory(typeof(IsEnglishLocal)), CombinatorialData]
    public async Task TestStandardLspNestedFixAllCodeAction(bool mutatingLspWorkspace)
    {
        var markup = """
            class ABC
            {
                private static async void XYZ()
                {
                    {|caret:System.Threading.Tasks.Task.Yield()|};
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var caret = testLspServer.GetLocations("caret").Single();
        var codeActionParams = new CodeActionParams
        {
            TextDocument = CreateTextDocumentIdentifier(caret.DocumentUri),
            Range = caret.Range,
            Context = new CodeActionContext
            {
                Diagnostics =
                [
                    new LSP.Diagnostic
                    {
                        // Task-returning method not awaited
                        Code = "CS4014"
                    }
                ]
            }
        };

        var results = await RunGetCodeActionsAsync(testLspServer, codeActionParams);
        Assert.Equal(10, results.Length);
        Assert.Equal("Suppress or configure issues", results[9].Title);
        var data = GetCodeActionResolveData(results[9]);
        Assert.NotNull(data);

        // Asserts that there are NestedActions present
        Assert.NotNull(data!.NestedCodeActions);

        //Asserts that a Nested Action could be a Fix All Action
        Assert.Equal("Fix All: in Source", data.NestedCodeActions!.Value[1].Title);
    }

    [ConditionalTheory(typeof(IsEnglishLocal)), CombinatorialData]
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
            TextDocument = CreateTextDocumentIdentifier(caret.DocumentUri),
            Range = caret.Range,
            Context = new CodeActionContext
            {
            }
        };

        var results = await RunGetCodeActionsAsync(testLspServer, codeActionParams);
        // Assert that nested code actions aren't enumerated.
        var inline = results.FirstOrDefault(r => r.Title.Contains($"Inline 'A()'"));
        var resolvedAction = await RunGetCodeActionResolveAsync(testLspServer, inline!);
        Assert.Null(resolvedAction.Edit);
    }

    private static async Task<VSInternalCodeAction[]> RunGetCodeActionsAsync(
        TestLspServer testLspServer,
        CodeActionParams codeActionParams)
    {
        var result = await testLspServer.ExecuteRequestAsync<CodeActionParams, CodeAction[]>(
            LSP.Methods.TextDocumentCodeActionName, codeActionParams, CancellationToken.None);
        return [.. result!.Cast<VSInternalCodeAction>()];
    }

    private static async Task<VSInternalCodeAction> RunGetCodeActionResolveAsync(
        TestLspServer testLspServer,
        CodeAction codeAction)
    {
        var result = await testLspServer.ExecuteRequestAsync<CodeAction, CodeAction>(
            LSP.Methods.CodeActionResolveName, codeAction, CancellationToken.None);
        Assert.NotNull(result);
        return (VSInternalCodeAction)result;
    }

    private static CodeActionResolveData? GetCodeActionResolveData(CodeAction codeAction)
    {
        return JsonSerializer.Deserialize<CodeActionResolveData>((JsonElement)codeAction.Data!, ProtocolConversions.LspJsonSerializerOptions);
    }

    internal static CodeActionParams CreateCodeActionParams(LSP.Location caret)
        => new()
        {
            TextDocument = CreateTextDocumentIdentifier(caret.DocumentUri),
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

    private sealed class ThrowingCodeFixService : ICodeFixService
    {
        public IAsyncEnumerable<CodeFixCollection> StreamFixesAsync(
            TextDocument document, TextSpan textSpan, CodeActionRequestPriority? priority, CancellationToken cancellationToken)
            => throw ExceptionUtilities.Unreachable();

        public Task<CodeFixCollection?> GetMostSevereFixAsync(
            TextDocument document, TextSpan range, CodeActionRequestPriority? priority, CancellationToken cancellationToken)
            => throw ExceptionUtilities.Unreachable();

        public Task<CodeFixCollection?> GetDocumentFixAllForIdInSpanAsync(
            Document document, TextSpan? textSpan, string diagnosticId, DiagnosticSeverity severity, CancellationToken cancellationToken)
            => throw ExceptionUtilities.Unreachable();

        public Task<Document> ApplyCodeFixesForSpecificDiagnosticIdAsync(
            Document document, TextSpan? textSpan, string diagnosticId, DiagnosticSeverity severity,
            IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
            => throw ExceptionUtilities.Unreachable();

        public CodeFixProvider? GetSuppressionFixer(string language, IEnumerable<string> diagnosticIds)
            => throw ExceptionUtilities.Unreachable();
    }

    private sealed class ThrowingCodeRefactoringService : ICodeRefactoringService
    {
        public Task<bool> HasRefactoringsAsync(TextDocument document, TextSpan textSpan, CancellationToken cancellationToken)
            => throw ExceptionUtilities.Unreachable();

        public Task<ImmutableArray<CodeRefactoring>> GetRefactoringsAsync(
            TextDocument document, TextSpan textSpan, CodeActionRequestPriority? priority, CancellationToken cancellationToken)
            => throw ExceptionUtilities.Unreachable();
    }
}
