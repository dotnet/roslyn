// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Services;

public sealed class ExtractRefactoringTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerClientTests(testOutputHelper)
{
    [Theory]
    [CombinatorialData]
    public async Task TestExtractBaseClass(bool includeDevKitComponents)
    {
        var markup =
            """
            class {|caret:A|}
            {
                public void M()
                {
                }
            }
            """;
        await using var testLspServer = await CreateCSharpLanguageServerAsync(markup, includeDevKitComponents);
        var caretLocation = testLspServer.GetLocations("caret").Single();

        await TestCodeActionAsync(testLspServer, caretLocation, "Extract base class...", """
            internal class NewBaseType
            {
                public void M()
                {
                }
            }

            class A : NewBaseType
            {
            }
            """);
    }

    [Theory]
    [CombinatorialData]
    public async Task TestExtractInterface(bool includeDevKitComponents)
    {
        var markup =
            """
            class {|caret:A|}
            {
                public void M()
                {
                }
            }
            """;
        await using var testLspServer = await CreateCSharpLanguageServerAsync(markup, includeDevKitComponents);
        var caretLocation = testLspServer.GetLocations("caret").Single();

        await TestCodeActionAsync(testLspServer, caretLocation, "Extract interface...", """
            interface IA
            {
                void M();
            }

            class A : IA
            {
                public void M()
                {
                }
            }
            """);
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

        var updatedCode = testLspClient.GetDocumentText(caretLocation.DocumentUri);

        AssertEx.Equal(expected, updatedCode);
    }
}
