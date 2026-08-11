// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Razor.AutoInsert;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.LanguageServer.AutoInsert;

public class CloseTextTagOnAutoInsertProviderTest(ITestOutputHelper testOutput) : RazorOnAutoInsertProviderTestBase(testOutput)
{
    private protected override IOnAutoInsertProvider CreateProvider() =>
        new CloseTextTagOnAutoInsertProvider();

    [Fact]
    public void OnTypeCloseAngle_ClosesTextTag()
    {
        RunAutoInsertTest(
            input: """
            @{
                <text>$$
            }
            """,
            expected: """
            @{
                <text>$0</text>
            }
            """);
    }

    [Fact]
    public void OnTypeCloseAngle_OutsideRazorBlock_DoesNotCloseTextTag()
    {
        RunAutoInsertTest(
            input: """
                <text>$$
                """,
            expected: """
                <text>
                """);
    }
}
