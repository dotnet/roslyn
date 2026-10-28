// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using CodeActionRequestPriority = Microsoft.CodeAnalysis.CodeActions.CodeActionRequestPriority;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.CodeActions;

public sealed class CodeActionHelpersTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    [Theory, CombinatorialData]
    public async Task TestCodeActionHelperReturnsOnlyQuickFixes(bool mutatingLspWorkspace, bool hasVsLspCapability)
    {
        // Select statements offering both missing-using fixes and refactorings, but request only quick fixes.
        var markup = "class C { void M() { {|caret:int value = 1; Task.Delay(value);|} } }";
        await using var server = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var request = CodeActionsTests.CreateCodeActionParams(server.GetLocations("caret").Single());
        request.Context.Only = [CodeActionKind.QuickFix];

        // Discover through the helper; computing the excluded refactorings fails immediately.
        var actions = await CodeActionHelpers.GetVSCodeActionsAsync(
            request, server.GetCurrentSolution().Projects.Single().Documents.Single(),
            server.TestWorkspace.ExportProvider.GetExportedValue<ICodeFixService>(),
            new ThrowingCodeRefactoringService(), hasVsLspCapability, CancellationToken.None);

        // Require quick fixes and reject every other action kind.
        Assert.NotEmpty(actions);
        Assert.All(actions, action => Assert.Equal(CodeActionKind.QuickFix, action.Kind));
    }

    [Theory, CombinatorialData]
    public async Task TestCodeActionHelperIncludesChildRefactorings(bool mutatingLspWorkspace, bool hasVsLspCapability)
    {
        // Request the parent refactor kind for statements that also offer fixes.
        var markup = "class C { void M() { {|caret:int value = 1; Task.Delay(value);|} } }";
        await using var server = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var request = CodeActionsTests.CreateCodeActionParams(server.GetLocations("caret").Single());
        request.Context.Only = [CodeActionKind.Refactor];

        // Discover refactorings without computing the excluded quick fixes.
        var actions = await CodeActionHelpers.GetVSCodeActionsAsync(
            request, server.GetCurrentSolution().Projects.Single().Documents.Single(),
            new ThrowingCodeFixService(),
            server.TestWorkspace.ExportProvider.GetExportedValue<ICodeRefactoringService>(),
            hasVsLspCapability, CancellationToken.None);

        // The parent filter must include extract children while excluding unrelated kinds.
        CodeActionKind?[] expectedKinds = [CodeActionKind.Refactor, CodeActionKind.RefactorExtract, CodeActionKind.RefactorInline];
        Assert.Contains(actions, action => action.Kind == CodeActionKind.RefactorExtract);
        Assert.All(actions, action => Assert.Contains(action.Kind, expectedKinds));
    }

    [Theory, CombinatorialData]
    public async Task TestCodeActionHelperReturnsOnlyExtractRefactorings(bool mutatingLspWorkspace, bool hasVsLspCapability)
    {
        // Request only extract refactorings from statements that also offer other actions.
        var markup = "class C { void M() { {|caret:int value = 1; Task.Delay(value);|} } }";
        await using var server = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var request = CodeActionsTests.CreateCodeActionParams(server.GetLocations("caret").Single());
        request.Context.Only = [CodeActionKind.RefactorExtract];

        // Compute refactorings for the child kind without requesting quick fixes.
        var actions = await CodeActionHelpers.GetVSCodeActionsAsync(
            request, server.GetCurrentSolution().Projects.Single().Documents.Single(),
            new ThrowingCodeFixService(),
            server.TestWorkspace.ExportProvider.GetExportedValue<ICodeRefactoringService>(),
            hasVsLspCapability, CancellationToken.None);

        // Require extract results, but exclude parent and sibling refactoring kinds.
        Assert.NotEmpty(actions);
        Assert.All(actions, action => Assert.Equal(CodeActionKind.RefactorExtract, action.Kind));
    }

    [Theory, CombinatorialData]
    public async Task TestCodeActionHelperReturnsMixedRequestedKinds(bool mutatingLspWorkspace, bool hasVsLspCapability)
    {
        // Request both quick fixes and extract refactorings for the same selection.
        var markup = "class C { void M() { {|caret:int value = 1; Task.Delay(value);|} } }";
        await using var server = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var request = CodeActionsTests.CreateCodeActionParams(server.GetLocations("caret").Single());
        request.Context.Only = [CodeActionKind.QuickFix, CodeActionKind.RefactorExtract];

        // Both requested categories need their real services during discovery.
        var actions = await CodeActionHelpers.GetVSCodeActionsAsync(
            request, server.GetCurrentSolution().Projects.Single().Documents.Single(),
            server.TestWorkspace.ExportProvider.GetExportedValue<ICodeFixService>(),
            server.TestWorkspace.ExportProvider.GetExportedValue<ICodeRefactoringService>(),
            hasVsLspCapability, CancellationToken.None);

        // Include each requested kind without allowing unrelated refactorings.
        CodeActionKind?[] expectedKinds = [CodeActionKind.QuickFix, CodeActionKind.RefactorExtract];
        Assert.Contains(actions, action => action.Kind == CodeActionKind.QuickFix);
        Assert.Contains(actions, action => action.Kind == CodeActionKind.RefactorExtract);
        Assert.All(actions, action => Assert.Contains(action.Kind, expectedKinds));
    }

    [Theory, CombinatorialData]
    public async Task TestCodeActionHelperSkipsComputationForSourceFixAll(bool mutatingLspWorkspace, bool hasVsLspCapability)
    {
        // Request the source action, which the handler adds separately from quick fixes and refactorings.
        var markup = "class C { {|caret:|} }";
        await using var server = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var request = CodeActionsTests.CreateCodeActionParams(server.GetLocations("caret").Single());
        request.Context.Only = [CodeActionKind.SourceFixAll];

        // Both services throw if called, proving source-only requests skip ordinary action computation.
        var actions = await CodeActionHelpers.GetVSCodeActionsAsync(
            request, server.GetCurrentSolution().Projects.Single().Documents.Single(),
            new ThrowingCodeFixService(),
            new ThrowingCodeRefactoringService(),
            hasVsLspCapability, CancellationToken.None);

        // No ordinary action matches; source.fixAll is added separately by the request handler.
        Assert.Empty(actions);
    }

    [Theory, CombinatorialData]
    public async Task TestCodeActionHelperSkipsComputationForUnknownKind(bool mutatingLspWorkspace, bool hasVsLspCapability)
    {
        // Request a kind that neither code fixes nor refactorings support.
        var markup = "class C { {|caret:|} }";
        await using var server = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var request = CodeActionsTests.CreateCodeActionParams(server.GetLocations("caret").Single());
        request.Context.Only = [new CodeActionKind("unknown")];

        // Reject the unsupported request without invoking either action service.
        var actions = await CodeActionHelpers.GetVSCodeActionsAsync(
            request, server.GetCurrentSolution().Projects.Single().Documents.Single(),
            new ThrowingCodeFixService(),
            new ThrowingCodeRefactoringService(),
            hasVsLspCapability, CancellationToken.None);

        // No action can match the unknown kind.
        Assert.Empty(actions);
    }

    [Theory, CombinatorialData]
    public async Task TestCodeActionHelperPreservesUnrestrictedFilters(
        bool mutatingLspWorkspace, bool hasVsLspCapability, bool useEmptyKind)
    {
        // Treat omitted, empty-list, and empty-parent filters as equivalent for either client format.
        var markup = "class C { void M() { {|caret:int value = 1; Task.Delay(value);|} } }";
        await using var server = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var request = CodeActionsTests.CreateCodeActionParams(server.GetLocations("caret").Single());
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
