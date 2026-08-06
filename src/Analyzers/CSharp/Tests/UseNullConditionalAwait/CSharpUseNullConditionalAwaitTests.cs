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
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string testCode,
        LanguageVersion languageVersion = LanguageVersion.Preview)
        => new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = testCode,
            LanguageVersion = languageVersion,
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

    [Fact]
    public Task IfStatement_IsNotNullPattern()
        => TestAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                async Task M(Task t)
                {
                    {|IDE0420:if|} (t is not null)
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
    public Task IfStatement_NullOnLeft()
        => TestAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                async Task M(Task t)
                {
                    {|IDE0420:if|} (null != t)
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
    public Task Ternary_NotEquals()
        => TestAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                async Task<int?> M(Task<int> t)
                {
                    return {|IDE0420:t|} != null ? await t : null;
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                async Task<int?> M(Task<int> t)
                {
                    return await? t;
                }
            }
            """);

    [Fact]
    public Task Ternary_Equals_Reversed()
        => TestAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                async Task<int?> M(Task<int> t)
                {
                    return {|IDE0420:t|} == null ? null : await t;
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                async Task<int?> M(Task<int> t)
                {
                    return await? t;
                }
            }
            """);

    [Fact]
    public Task Ternary_ConfigureAwait()
        => TestAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                async Task<int?> M(Task<int> t)
                {
                    return {|IDE0420:t|} != null ? await t.ConfigureAwait(false) : null;
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                async Task<int?> M(Task<int> t)
                {
                    return await? t?.ConfigureAwait(false);
                }
            }
            """);

    [Fact]
    public Task Ternary_NotWhenNullBranchIsNotNull()
        => TestMissingAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                async Task<int> M(Task<int> t, int fallback)
                {
                    return t != null ? await t : fallback;
                }
            }
            """);

    [Fact]
    public Task IfStatement_LogicalNot()
        => TestAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                async Task M(Task t)
                {
                    {|IDE0420:if|} (!(t == null))
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
    public Task IfStatement_ParenthesizedReceiver()
        => TestAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                async Task M(Task t)
                {
                    {|IDE0420:if|} (((t)) != (null))
                        await (t);
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                async Task M(Task t)
                {
                    await? (t);
                }
            }
            """);

    [Fact]
    public Task IfStatement_MemberAccess()
        => TestAsync(
            """
            using System.Threading.Tasks;
            class Holder
            {
                public Task Task { get; }
            }

            class C
            {
                async Task M(Holder holder)
                {
                    {|IDE0420:if|} (holder != null)
                        await holder.Task;
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class Holder
            {
                public Task Task { get; }
            }

            class C
            {
                async Task M(Holder holder)
                {
                    await? holder?.Task;
                }
            }
            """);

    [Fact]
    public Task IfStatement_ElementAccess()
        => TestAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                async Task M(Task[] tasks)
                {
                    {|IDE0420:if|} (tasks != null)
                        await tasks[0];
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                async Task M(Task[] tasks)
                {
                    await? tasks?[0];
                }
            }
            """);

    [Fact]
    public Task IfStatement_PreservesLeadingComments()
        => TestAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                async Task M(Task t)
                {
                    // Before if
                    {|IDE0420:if|} (t != null)
                        // Before await
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
                    // Before if
                    // Before await
                    await? t;
                }
            }
            """);

    [Fact]
    public Task IfStatement_PreservesTrailingComment()
        => TestAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                async Task M(Task t)
                {
                    {|IDE0420:if|} (t != null)
                    {
                        await t; // After await
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
                    await? t; // After await
                }
            }
            """);

    [Fact]
    public Task IfStatement_NotWithMultipleStatements()
        => TestMissingAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                async Task M(Task t)
                {
                    if (t != null)
                    {
                        await t;
                        await t;
                    }
                }
            }
            """);

    [Fact]
    public Task IfStatement_NotWithDirective()
        => TestMissingAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                async Task M(Task t)
                {
                    if (t != null)
                    {
            #if DEBUG
                        await t;
            #endif
                    }
                }
            }
            """);

    [Fact]
    public Task NotBeforeCSharp15()
        => TestMissingAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                async Task M(Task t)
                {
                    if (t != null)
                        await t;
                }
            }
            """,
            LanguageVersion.CSharp14);
}
