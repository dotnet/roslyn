// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.ProcessHost.UnitTests.CodeActions;

public sealed class MoveStaticMembersRefactoringTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerClientTests(testOutputHelper)
{
    [Theory, CombinatorialData]
    public async Task TestMoveStaticMembersActionIsSurfaced(bool includeDevKitComponents)
    {
        var markup =
            """
            class A
            {
                public static int {|caret:Foo|}() => 1;
            }
            """;
        var workspaceContent = LspTestWorkspaces.SimpleProject.WithCSharp(markup);
        await using var testLspServer = await CreateLanguageServerAsync(workspaceContent, new() { IncludeDevKitComponents = includeDevKitComponents });
        var caretLocation = testLspServer.GetLocations("caret").Single();

        var codeActionResults = await testLspServer.RunGetCodeActionsAsync(CreateCodeActionParams(caretLocation));

        Assert.Contains(codeActionResults, action => action.Title == FeaturesResources.Move_static_members_to_another_type);
    }

    [Theory, CombinatorialData]
    public async Task TestMoveStaticMembersActionMovesSelectedMembersToHelperClass(bool includeDevKitComponents)
    {
        var markup =
            """
            class A
            {
                {|selection:public static int Foo() => 1;

                public static int Bar() => 2;|}
                public static int Baz() => 3;
            }
            """;
        var workspaceContent = LspTestWorkspaces.SimpleProject.WithCSharp(markup);
        await using var testLspServer = await CreateLanguageServerAsync(workspaceContent, new() { IncludeDevKitComponents = includeDevKitComponents }, new ClientCapabilities
        {
            Workspace = new WorkspaceClientCapabilities
            {
                WorkspaceEdit = new WorkspaceEditSetting
                {
                    ResourceOperations = [ResourceOperationKind.Create]
                }
            }
        });
        var selectionLocation = testLspServer.GetLocations("selection").Single();

        var codeActionResults = await testLspServer.RunGetCodeActionsAsync(CreateCodeActionParams(selectionLocation));
        var unresolvedCodeAction = Assert.Single(codeActionResults, action => action.Title == FeaturesResources.Move_static_members_to_another_type);

        var resolvedCodeAction = await testLspServer.RunGetCodeActionResolveAsync(unresolvedCodeAction);
        testLspServer.ApplyWorkspaceEdit(resolvedCodeAction.Edit);

        AssertEx.Equal("""
            class A
            {
                public static int Baz() => 3;
            }
            """, testLspServer.GetFileText(selectionLocation.DocumentUri));

        AssertEx.Equal("""
            internal static class AHelpers
            {

                public static int Bar() => 2;
                public static int Foo() => 1;
            }
            """, testLspServer.GetFileText("AHelpers.cs"));
    }
}
