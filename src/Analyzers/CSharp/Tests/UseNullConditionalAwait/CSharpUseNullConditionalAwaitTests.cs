// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseNullConditionalAwait;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseNullConditionalAwait;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseNullConditionalAwaitDiagnosticAnalyzer,
    CSharpUseNullConditionalAwaitCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)]
public sealed class CSharpUseNullConditionalAwaitTests
{
    private static Task TestAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string testCode,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string fixedCode)
        => new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();

    private static Task TestMissingAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string testCode)
        => new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = testCode,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();

    [Fact]
    public Task IfStatement_BareReceiver()
        => TestAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                async Task M(Task t)
                {
                    {|IDE0420:if|} (t != null)
                        await t;
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                async Task M(Task t)
                {
                    await? t;
                }
            }
            """);

    [Fact]
    public Task IfStatement_Block()
        => TestAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                async Task M(Task t)
                {
                    {|IDE0420:if|} (t != null)
                    {
                        await t;
                    }
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                async Task M(Task t)
                {
                    await? t;
                }
            }
            """);

    [Fact]
    public Task IfStatement_ConfigureAwait()
        => TestAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                async Task M(Task t)
                {
                    {|IDE0420:if|} (t != null)
                        await t.ConfigureAwait(false);
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                async Task M(Task t)
                {
                    await? t?.ConfigureAwait(false);
                }
            }
            """);

    [Fact]
    public Task NotWhenElsePresent()
        => TestMissingAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                async Task M(Task t)
                {
                    if (t != null)
                        await t;
                    else
                        await Task.CompletedTask;
                }
            }
            """);

    [Fact]
    public Task NotWhenReceiverDiffers()
        => TestMissingAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                async Task M(Task t, Task other)
                {
                    if (t != null)
                        await other;
                }
            }
            """);
}
