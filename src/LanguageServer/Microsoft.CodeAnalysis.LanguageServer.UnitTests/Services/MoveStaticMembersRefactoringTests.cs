// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
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
}
