// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.CodeActions;

public sealed class CodeActionHelpersTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    [Theory]
    [InlineData("", "quickfix", true)]
    [InlineData("quickfix", "quickfix", true)]
    [InlineData("refactor", "refactor.extract", true)]
    [InlineData("refactor.extract", "refactor", false)]
    [InlineData("refactor", "refactoring", false)]
    public void TestCodeActionKindMatchesHierarchy(string requestedKind, string actionKind, bool expected)
        => Assert.Equal(expected, CodeActionHelpers.MatchesKind(new CodeActionKind(requestedKind), new CodeActionKind(actionKind)));

    [Theory, CombinatorialData]
    public async Task TestCodeActionHelperReturnsOnlyQuickFixes(bool hasVsLspCapability)
    {
        // Select statements offering both missing-using fixes and refactorings, but request only quick fixes.
        var markup = "class C { void M() { {|caret:int value = 1; Task.Delay(value);|} } }";
        await using var server = await CreateTestLspServerAsync(markup, mutatingLspWorkspace: false);
        var request = CodeActionsTests.CreateCodeActionParams(server.GetLocations("caret").Single());
        request.Context.Only = [CodeActionKind.QuickFix];

        // Discover with the real services and verify that the requested kind filters their output.
        var actions = await CodeActionHelpers.GetVSCodeActionsAsync(
            request, server.GetCurrentSolution().Projects.Single().Documents.Single(),
            server.TestWorkspace.ExportProvider.GetExportedValue<ICodeFixService>(),
            server.TestWorkspace.ExportProvider.GetExportedValue<ICodeRefactoringService>(), hasVsLspCapability, CancellationToken.None);

        // Require quick fixes and reject every other action kind.
        Assert.NotEmpty(actions);
        Assert.All(actions, action => Assert.Equal(CodeActionKind.QuickFix, action.Kind));
    }

    [Theory, CombinatorialData]
    public async Task TestCodeActionHelperIncludesChildRefactorings(bool hasVsLspCapability)
    {
        // Request the parent refactor kind for statements that also offer fixes.
        var markup = "class C { void M() { {|caret:int value = 1; Task.Delay(value);|} } }";
        await using var server = await CreateTestLspServerAsync(markup, mutatingLspWorkspace: false);
        var request = CodeActionsTests.CreateCodeActionParams(server.GetLocations("caret").Single());
        request.Context.Only = [CodeActionKind.Refactor];

        // Discover through the composed services, allowing only the requested refactoring kinds.
        var actions = await CodeActionHelpers.GetVSCodeActionsAsync(
            request, server.GetCurrentSolution().Projects.Single().Documents.Single(),
            server.TestWorkspace.ExportProvider.GetExportedValue<ICodeFixService>(),
            server.TestWorkspace.ExportProvider.GetExportedValue<ICodeRefactoringService>(),
            hasVsLspCapability, CancellationToken.None);

        // The parent filter must include extract children while excluding unrelated kinds.
        CodeActionKind?[] expectedKinds = [CodeActionKind.Refactor, CodeActionKind.RefactorExtract, CodeActionKind.RefactorInline];
        Assert.Contains(actions, action => action.Kind == CodeActionKind.RefactorExtract);
        Assert.All(actions, action => Assert.Contains(action.Kind, expectedKinds));
    }

    [Theory, CombinatorialData]
    public async Task TestCodeActionHelperReturnsOnlyExtractRefactorings(bool hasVsLspCapability)
    {
        // Request only extract refactorings from statements that also offer other actions.
        var markup = "class C { void M() { {|caret:int value = 1; Task.Delay(value);|} } }";
        await using var server = await CreateTestLspServerAsync(markup, mutatingLspWorkspace: false);
        var request = CodeActionsTests.CreateCodeActionParams(server.GetLocations("caret").Single());
        request.Context.Only = [CodeActionKind.RefactorExtract];

        // Discover with real services to exercise filtering of parent and sibling kinds.
        var actions = await CodeActionHelpers.GetVSCodeActionsAsync(
            request, server.GetCurrentSolution().Projects.Single().Documents.Single(),
            server.TestWorkspace.ExportProvider.GetExportedValue<ICodeFixService>(),
            server.TestWorkspace.ExportProvider.GetExportedValue<ICodeRefactoringService>(),
            hasVsLspCapability, CancellationToken.None);

        // Require extract results, but exclude parent and sibling refactoring kinds.
        Assert.NotEmpty(actions);
        Assert.All(actions, action => Assert.Equal(CodeActionKind.RefactorExtract, action.Kind));
    }

    [Theory, CombinatorialData]
    public async Task TestCodeActionHelperReturnsMixedRequestedKinds(bool hasVsLspCapability)
    {
        // Request both quick fixes and extract refactorings for the same selection.
        var markup = "class C { void M() { {|caret:int value = 1; Task.Delay(value);|} } }";
        await using var server = await CreateTestLspServerAsync(markup, mutatingLspWorkspace: false);
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
    public async Task TestCodeActionHelperReturnsNoOrdinaryActionsForUnsupportedKind(
        [CombinatorialValues("source.fixAll", "unknown")] string kind)
    {
        // Source actions are added by the handler; neither requested kind matches an ordinary action.
        var markup = "class C { void M() { {|caret:int value = 1; Task.Delay(value);|} } }";
        await using var server = await CreateTestLspServerAsync(markup, mutatingLspWorkspace: false);
        var request = CodeActionsTests.CreateCodeActionParams(server.GetLocations("caret").Single());
        request.Context.Only = [new CodeActionKind(kind)];

        // The real services offer actions here, but none belongs to the requested kind.
        var actions = await CodeActionHelpers.GetVSCodeActionsAsync(
            request, server.GetCurrentSolution().Projects.Single().Documents.Single(),
            server.TestWorkspace.ExportProvider.GetExportedValue<ICodeFixService>(),
            server.TestWorkspace.ExportProvider.GetExportedValue<ICodeRefactoringService>(),
            hasVsLspCapability: false, CancellationToken.None);

        Assert.Empty(actions);
    }

    [Theory, CombinatorialData]
    public async Task TestCodeActionHelperPreservesUnrestrictedFilters(
        bool hasVsLspCapability, bool useEmptyKind)
    {
        // Treat omitted, empty-list, and empty-parent filters as equivalent for either client format.
        var markup = "class C { void M() { {|caret:int value = 1; Task.Delay(value);|} } }";
        await using var server = await CreateTestLspServerAsync(markup, mutatingLspWorkspace: false);
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

}
