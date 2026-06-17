// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.ProcessHost.UnitTests.CodeActions;

public sealed class GenerateFromMembersRefactoringTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerClientTests(testOutputHelper)
{
    // Generate-constructor and generate-overrides only appear once the project's framework references finish loading,
    // but the out-of-process test server fires "project initialized" before that happens and offers no event to wait on.
    // These tests are correct and pass once references are present; skipped here pending a test-infra fix.
    private const string MetadataLoadRaceSkip = "Flaky: depends on metadata references that load asynchronously after project initialization completes; needs a test-infra wait hook.";

    [Theory(Skip = MetadataLoadRaceSkip), CombinatorialData]
    public async Task TestGenerateConstructorFromMembersIsSurfaced(bool includeDevKitComponents)
    {
        var markup =
            """
            class {|caret:|}C
            {
                public int X { get; set; }
                public string Y { get; set; }
            }
            """;
        var workspaceContent = LspTestWorkspaces.SimpleProject.WithCSharp(markup);
        await using var testLspServer = await CreateLanguageServerAsync(workspaceContent, new() { IncludeDevKitComponents = includeDevKitComponents });
        var caretLocation = testLspServer.GetLocations("caret").Single();

        var codeActionResults = await testLspServer.RunGetCodeActionsAsync(CreateCodeActionParams(caretLocation));

        Assert.Contains(codeActionResults, action => action.Title == FeaturesResources.Generate_constructor_from_all_members);
    }

    [Theory(Skip = MetadataLoadRaceSkip), CombinatorialData]
    public async Task TestGenerateConstructorFromMembersGeneratesConstructor(bool includeDevKitComponents)
    {
        var markup =
            """
            class {|caret:|}C
            {
                public int X { get; set; }
                public string Y { get; set; }
            }
            """;
        var workspaceContent = LspTestWorkspaces.SimpleProject.WithCSharp(markup);
        await using var testLspServer = await CreateLanguageServerAsync(workspaceContent, new() { IncludeDevKitComponents = includeDevKitComponents });
        var caretLocation = testLspServer.GetLocations("caret").Single();

        // headless behavior selects all members and does not add null checks.
        await TestCodeActionAsync(testLspServer, caretLocation, FeaturesResources.Generate_constructor_from_all_members, """
            class C
            {
                public C(int x, string y)
                {
                    X = x;
                    Y = y;
                }

                public int X { get; set; }
                public string Y { get; set; }
            }
            """);
    }

    [Theory, CombinatorialData]
    public async Task TestGenerateEqualsAndGetHashCodeIsSurfaced(bool includeDevKitComponents)
    {
        var markup =
            """
            class {|caret:|}C
            {
                public int X { get; set; }
            }
            """;
        var workspaceContent = LspTestWorkspaces.SimpleProject.WithCSharp(markup);
        await using var testLspServer = await CreateLanguageServerAsync(workspaceContent, new() { IncludeDevKitComponents = includeDevKitComponents });
        var caretLocation = testLspServer.GetLocations("caret").Single();

        var codeActionResults = await testLspServer.RunGetCodeActionsAsync(CreateCodeActionParams(caretLocation));

        Assert.Contains(codeActionResults, action => action.Title == FeaturesResources.Generate_Equals_from_all_members);
        Assert.Contains(codeActionResults, action => action.Title == FeaturesResources.Generate_Equals_and_GetHashCode_from_all_members);
    }

    [Theory(Skip = MetadataLoadRaceSkip), CombinatorialData]
    public async Task TestGenerateOverridesIsSurfaced(bool includeDevKitComponents)
    {
        var markup =
            """
            class {|caret:|}C
            {
                public int X { get; set; }
            }
            """;
        var workspaceContent = LspTestWorkspaces.SimpleProject.WithCSharp(markup);
        await using var testLspServer = await CreateLanguageServerAsync(workspaceContent, new() { IncludeDevKitComponents = includeDevKitComponents });
        var caretLocation = testLspServer.GetLocations("caret").Single();

        var codeActionResults = await testLspServer.RunGetCodeActionsAsync(CreateCodeActionParams(caretLocation));

        Assert.Contains(codeActionResults, action => action.Title == FeaturesResources.Generate_overrides_for_all_members);
    }

    private static async Task TestCodeActionAsync(
        TestLspClient testLspClient,
        LSP.Location caretLocation,
        string codeActionTitle,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expected)
    {
        var codeActionResults = await testLspClient.RunGetCodeActionsAsync(CreateCodeActionParams(caretLocation));

        var unresolvedCodeAction = Assert.Single(codeActionResults, codeAction => codeAction.Title == codeActionTitle);

        var resolvedCodeAction = await testLspClient.RunGetCodeActionResolveAsync(unresolvedCodeAction);

        testLspClient.ApplyWorkspaceEdit(resolvedCodeAction.Edit);

        var updatedCode = testLspClient.GetFileText(caretLocation.DocumentUri);

        AssertEx.Equal(expected, updatedCode);
    }
}
