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
public sealed class MakeStructMemberReadOnlyTests
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
    public async Task TestNotWithWriteToField1()
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
    public async Task TestNotWithWriteToField2()
    {
        var test = """
            struct S
            {
                int x;
                void M()
                {
                    x++;
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
    public async Task TestNotWithWriteToField3()
    {
        var test = """
            struct S
            {
                int x;
                void M()
                {
                    G(ref x);
                }

                static void G(ref int x) { }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = test,
            FixedCode = test,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithWriteToField4()
    {
        var test = """
            struct S
            {
                int x;
                void M()
                {
                    G(out x);
                }

                static void G(out int x) { x = 0; }
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
    public async Task TestPropertyAccessor1()
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
    public async Task TestPropertyAccessor2()
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
    public async Task TestIndexerAccessor2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int this[int i] { [|get|] => 0; }
            }
            """,
            FixedCode = """
            struct S
            {
                readonly int this[int i] { get => 0; }
            }
            """
        }.RunAsync();
    }

    [Fact]
    public async Task TestPropertyAccessor3()
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

    [Fact]
    public async Task TestIndexerAccessor3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int this[int i] { [|get|] { return 0; } }
            }
            """,
            FixedCode = """
            struct S
            {
                readonly int this[int i] { get { return 0; } }
            }
            """
        }.RunAsync();
    }

    [Fact]
    public async Task TestWriteToFieldNotThroughThis()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int i;

                void [|M|]()
                {
                    S s;
                    s.i = 1;
                }
            }
            """,
            FixedCode = """
            struct S
            {
                int i;

                readonly void M()
                {
                    S s;
                    s.i = 1;
                }
            }
            """
        }.RunAsync();
    }

    [Fact]
    public async Task TestCallToStaticMethod()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int i;

                void [|M|]()
                {
                    G();
                }

                static void G() { }
            }
            """,
            FixedCode = """
            struct S
            {
                int i;

                readonly void M()
                {
                    G();
                }
            
                static void G() { }
            }
            """
        }.RunAsync();
    }

    [Fact]
    public async Task TestRecursiveCall()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int i;

                void [|M|]()
                {
                    M();
                }
            }
            """,
            FixedCode = """
            struct S
            {
                int i;

                readonly void M()
                {
                    M();
                }
            }
            """
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleAccessor()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int i;

                int X { [|get|] => 0; [|set|] { } }
            }
            """,
            FixedCode = """
            struct S
            {
                int i;

                readonly int X { get => 0; set { } }
            }
            """,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleIndexerAccessor()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int i;

                int this[int x] { [|get|] => 0; [|set|] { } }
            }
            """,
            FixedCode = """
            struct S
            {
                int i;

                readonly int this[int x] { get => 0; set { } }
            }
            """,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleAccessor_FixOne1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int i;

                int X { [|get|] => 0; [|set|] { } }
            }
            """,
            FixedState =
            {
                Sources =
                {
                    """
                    struct S
                    {
                        int i;

                        int X { readonly get => 0; set { } }
                    }
                    """,
                },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(5,32): info IDE0251: 
                    VerifyCS.Diagnostic("IDE0251").WithSeverity(DiagnosticSeverity.Info).WithSpan(5, 32, 5, 35).WithOptions(DiagnosticOptions.IgnoreAdditionalLocations),
                },
            },
            BatchFixedCode = """
            struct S
            {
                int i;

                readonly int X { get => 0; set { } }
            }
            """,
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleIndexerAccessor_FixOne1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int i;

                int this[int x] { [|get|] => 0; [|set|] { } }
            }
            """,
            FixedState =
            {
                Sources =
                {
                    """
                    struct S
                    {
                        int i;

                        int this[int x] { readonly get => 0; set { } }
                    }
                    """,
                },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(5,32): info IDE0251: 
                    VerifyCS.Diagnostic("IDE0251").WithSeverity(DiagnosticSeverity.Info).WithSpan(5, 42, 5, 45).WithOptions(DiagnosticOptions.IgnoreAdditionalLocations),
                },
            },
            BatchFixedCode = """
            struct S
            {
                int i;

                readonly int this[int x] { get => 0; set { } }
            }
            """,
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleAccessor2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int i;

                int X { [|get|] => 0; readonly set { } }
            }
            """,
            FixedCode = """
            struct S
            {
                int i;

                readonly int X { get => 0; set { } }
            }
            """
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleIndexerAccessor2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int i;

                int this[int x] { [|get|] => 0; readonly set { } }
            }
            """,
            FixedCode = """
            struct S
            {
                int i;

                readonly int this[int x] { get => 0; set { } }
            }
            """
        }.RunAsync();
    }

    [Fact]
    public async Task TestTakeRefReadOnlyToField()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int i;

                void [|M|]()
                {
                    ref readonly int x = ref i;
                }
            }
            """,
            FixedCode = """
            struct S
            {
                int i;
            
                readonly void M()
                {
                    ref readonly int x = ref i;
                }
            }
            """
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithAddressOfFieldTaken()
    {
        var test = """
            struct S
            {
                int x;
                unsafe void M()
                {
                    fixed (int* y = &x)
                    {
                    }
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
    public async Task TestNotWithCallToNonReadOnlyMethod()
    {
        var test = """
            struct S
            {
                int x;
                void M()
                {
                    this.X();
                }

                void X()
                {
                    x = 1;
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
    public async Task TestNotWithCallToNonReadOnlyIndexer()
    {
        var test = """
            struct S
            {
                int x;
                int this[int y] { get { return x++; } set { x++; } }

                void M()
                {
                    var v = this[0];
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
    public async Task TestNotWithCaptureOfNonReadOnlyMethod1()
    {
        var test = """
            struct S
            {
                int x;
                void M()
                {
                    System.Action v = this.X;
                }

                void X()
                {
                    x = 1;
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
    public async Task TestNotWithCaptureOfNonReadOnlyMethod2()
    {
        var test = """
            struct S
            {
                int x;
                void M()
                {
                    var v = this.X;
                }

                void X()
                {
                    x = 1;
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = test,
            FixedCode = test,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact]
    public async Task TestCallToObjectMethod()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                void [|M|]() { this.ToString(); }
            }
            """,
            FixedCode = """
            struct S
            {
                readonly void M() { this.ToString(); }
            }
            """
        }.RunAsync();
    }

    [Fact]
    public async Task TestCallToReadOnlyMethod()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                void [|M|]() { this.X(); }
                readonly void X() { }
            }
            """,
            FixedCode = """
            struct S
            {
                readonly void M() { this.X(); }
                readonly void X() { }
            }
            """
        }.RunAsync();
    }

    [Fact]
    public async Task TestCallToReadOnlyIndexer1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int i;
                int this[int x] { readonly get => 0; set { i++; } }

                void [|M|]()
                {
                    var v = this[0];
                }
            }
            """,
            FixedCode = """
            struct S
            {
                int i;
                int this[int x] { readonly get => 0; set { i++; } }

                readonly void M()
                {
                    var v = this[0];
                }
            }
            """
        }.RunAsync();
    }

    [Fact]
    public async Task TestCallToReadOnlyIndexer2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int i;
                readonly int this[int x] { get => 0; }

                void [|M|]()
                {
                    var v = this[0];
                }
            }
            """,
            FixedCode = """
            struct S
            {
                int i;
                readonly int this[int x] { get => 0; }
            
                readonly void M()
                {
                    var v = this[0];
                }
            }
            """
        }.RunAsync();
    }

    [Fact]
    public async Task TestExplicitInterfaceImpl()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;
            struct S : IEquatable<S>
            {
                bool IEquatable<S>.[|Equals|](S s) => true;
            }
            """,
            FixedCode = """
            using System;
            struct S : IEquatable<S>
            {
                readonly bool IEquatable<S>.Equals(S s) => true;
            }
            """
        }.RunAsync();
    }

    [Fact]
    public async Task TestEventMutation()
    {
        var testCode = """
            using System;
            struct S
            {
                event Action E;

                void M()
                {
                    this.E += () => { };
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = testCode,
        }.RunAsync();
    }
}
