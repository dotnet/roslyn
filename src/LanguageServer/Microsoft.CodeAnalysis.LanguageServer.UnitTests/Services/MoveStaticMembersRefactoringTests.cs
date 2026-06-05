// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Services;

public sealed class MoveStaticMembersRefactoringTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerClientTests(testOutputHelper)
{
    [ConditionalTheory(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/83181")]
    [CombinatorialData]
    public async Task TestMoveStaticMembersActionIsSurfaced(bool includeDevKitComponents)
    {
        var markup =
            """
            class A
            {
                public static int {|caret:Foo|}() => 1;
            }
            """;
        await using var testLspServer = await CreateCSharpLanguageServerAsync(markup, includeDevKitComponents);
        var caretLocation = testLspServer.GetLocations("caret").Single();

        var codeActionResults = await testLspServer.RunGetCodeActionsAsync(CreateCodeActionParams(caretLocation));

        Assert.Contains(codeActionResults, action => action.Title == "Move static members to another type...");
    }

    [ConditionalTheory(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/83181")]
    [CombinatorialData]
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
        await using var testLspServer = await CreateCSharpLanguageServerAsync(markup, includeDevKitComponents, new ClientCapabilities
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
        var unresolvedCodeAction = Assert.Single(codeActionResults, action => action.Title == "Move static members to another type...");

        var resolvedCodeAction = await testLspServer.RunGetCodeActionResolveAsync(unresolvedCodeAction);
        testLspServer.ApplyWorkspaceEdit(resolvedCodeAction.Edit);

        AssertEx.Equal("""
            class A
            {
                public static int Baz() => 3;
            }
            """, testLspServer.GetDocumentText(selectionLocation.DocumentUri));

        var helperUri = ProtocolConversions.CreateAbsoluteDocumentUri(
            Path.Combine(Path.GetDirectoryName(selectionLocation.DocumentUri.GetRequiredParsedUri().LocalPath)!, "AHelpers.cs"));
        AssertEx.Equal("""
            internal static class AHelpers
            {

                public static int Bar() => 2;
                public static int Foo() => 1;
            }
            """, testLspServer.GetDocumentText(helperUri));
    }
}
