// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.MakeStructMemberReadOnly;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeStructReadOnly;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpMakeStructMemberReadOnlyDiagnosticAnalyzer,
    CSharpMakeStructMemberReadOnlyCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructMemberReadOnly)]
public class MakeStructMemberReadOnlyTests
{
    [Fact]
    public async Task TestEmptyMethod()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                void [|M|]() { }
            }
            """,
            FixedCode = """
            struct S
            {
                readonly void M() { }
            }
            """
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotInClass()
    {
        var test = """
            class S
            {
                void M() { }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = test,
            FixedCode = test,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotInReadOnlyStruct()
    {
        var test = """
            readonly struct S
            {
                void M() { }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = test,
            FixedCode = test,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotInReadOnlyMember()
    {
        var test = """
            struct S
            {
                readonly void M() { }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = test,
            FixedCode = test,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithAssignmentToThis()
    {
        var test = """
            struct S
            {
                void M()
                {
                    this = default;
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = test,
            FixedCode = test,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithThisPassedByRef1()
    {
        var test = """
            struct S
            {
                void M()
                {
                    G(ref this);
                }

                static void G(ref S s) { }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = test,
            FixedCode = test,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithThisPassedByRef2()
    {
        var test = """
            struct S
            {
                void M()
                {
                    this.G();
                }
            }

            static class X
            {
                public static void G(ref this S s) { }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = test,
            FixedCode = test,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithWriteToField()
    {
        var test = """
            struct S
            {
                int x;
                void M()
                {
                    x = 0;
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = test,
            FixedCode = test,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotInCSharp7()
    {
        var test = """
            struct S
            {
                void M() { }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = test,
            FixedCode = test,
            LanguageVersion = LanguageVersion.CSharp7,
        }.RunAsync();
    }

    [Fact]
    public async Task TestPropertyExpressionBody()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int [|P|] => 0;
            }
            """,
            FixedCode = """
            struct S
            {
                readonly int P => 0;
            }
            """
        }.RunAsync();
    }

    [Fact]
    public async Task TestAutoPropertyAccessor1()
    {
        var test = """
            struct S
            {
                int P { get; }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = test,
            FixedCode = test,
        }.RunAsync();
    }

    [Fact]
    public async Task TestAutoPropertyAccessor2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int P { [|get|] => 0; }
            }
            """,
            FixedCode = """
            struct S
            {
                readonly int P { get => 0; }
            }
            """
        }.RunAsync();
    }

    [Fact]
    public async Task TestAutoPropertyAccessor3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int P { [|get|] { return 0; } }
            }
            """,
            FixedCode = """
            struct S
            {
                readonly int P { get { return 0; } }
            }
            """
        }.RunAsync();
    }
}
