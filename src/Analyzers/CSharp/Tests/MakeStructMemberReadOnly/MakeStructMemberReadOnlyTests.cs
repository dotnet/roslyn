// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.MakeStructMemberReadOnly;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeStructMemberReadOnly;

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
    public async Task TestWithThisPassedByIn1_A()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                void [|M|]()
                {
                    G(in this);
                }

                static void G(in S s) { }
            }
            """,
            FixedCode = """
            struct S
            {
                readonly void M()
                {
                    G(in this);
                }

                static void G(in S s) { }
            }
            """,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithThisPassedByIn1_B()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                void [|M|]()
                {
                    G(this);
                }

                static void G(in S s) { }
            }
            """,
            FixedCode = """
            struct S
            {
                readonly void M()
                {
                    G(this);
                }

                static void G(in S s) { }
            }
            """,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithThisPassedByIn2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                void [|M|]()
                {
                    this.G();
                }
            }

            static class X
            {
                public static void G(in this S s) { }
            }
            """,
            FixedCode = """
            struct S
            {
                readonly void M()
                {
                    this.G();
                }
            }

            static class X
            {
                public static void G(in this S s) { }
            }
            """,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithThisPassedByIn3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                void [|M|]()
                {
                    var v = this + this;
                }

                public static S operator+(in S s1, in S s2) => default;
            }
            """,
            FixedCode = """
            struct S
            {
                readonly void M()
                {
                    var v = this + this;
                }

                public static S operator+(in S s1, in S s2) => default;
            }
            """,
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
    public async Task TestNotWithWriteToField5()
    {
        var test = """
            struct S
            {
                int x;
                void M()
                {
                    (x, x) = (0, 0);
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
    public async Task TestNotWithWriteToField6()
    {
        var test = """
            struct D
            {
                public int i;
            }
            struct S
            {
                D d;
                void M()
                {
                    d.i = 0;
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
    public async Task TestNotWithWriteToField7()
    {
        var test = """
            struct BitVector
            {
                int x;
                public bool this[int index] { get => x++ > 0; set => x++; }
            }
            struct S
            {
                BitVector bits;
                void M()
                {
                    bits[0] = true;
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
    public async Task TestNotWithWriteToField8()
    {
        var test = """
            struct BitVector
            {
                int x;
                public bool this[int index] { get => x++ > 0; set => x++; }
            }
            struct S
            {
                BitVector bits;
                void M()
                {
                    (bits[0], bits[1]) = (true, false);
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
                int P [|=>|] 0;
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

    [Fact]
    public async Task TestNotWithNonReadOnlyMethodCallOnField()
    {
        var testCode = """
            struct T
            {
                int i;
                public void Dispose() { i++; }
            }

            struct S
            {
                T t;

                void Dispose()
                {
                    t.Dispose();
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithReadOnlyMethodCallOnField()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct T
            {
                public readonly void Dispose() { }
            }

            struct S
            {
                T t;

                void [|Dispose|]()
                {
                    t.Dispose();
                }
            }
            """,
            FixedCode = """
            struct T
            {
                public readonly void Dispose() { }
            }

            struct S
            {
                T t;

                readonly void Dispose()
                {
                    t.Dispose();
                }
            }
            """,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithNonReadOnlyMethodOnUnconstrainedField()
    {
        var testCode = """
            using System;
            struct T<X> where X : IComparable
            {
                X x;
                public void M() { x.CompareTo(null); }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithNonReadOnlyMethodOnStructConstrainedField()
    {
        var testCode = """
            using System;
            struct T<X> where X : struct, IComparable
            {
                X x;
                public void M() { x.CompareTo(null); }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithNonReadOnlyMethodOnClassConstrainedField()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;
            struct T<X> where X : class, IComparable
            {
                X x;
                public void [|M|]() { x.CompareTo(null); }
            }
            """,
            FixedCode = """
            using System;
            struct T<X> where X : class, IComparable
            {
                X x;
                public readonly void M() { x.CompareTo(null); }
            }
            """,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithMethodThatOnlyThrows1()
    {
        var test = """
            struct S
            {
                void M() => throw new System.Exception();
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = test,
            FixedCode = test,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithMethodThatOnlyThrows2()
    {
        var test = """
            struct S
            {
                void M()
                {
                    throw new System.Exception();
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
    public async Task TestNotWithBadOperation()
    {
        var test = """
            struct S
            {
                void M()
                {
                    {|CS0103:Goo|}();
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
    public async Task TestWithLinqRewrite()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System.Collections.Generic;
            using System.Linq;
            struct S
            {
                void [|M|](IEnumerable<int> x)
                {
                    var v = from y in x
                            select y;
                }
            }
            """,
            FixedCode = """
            using System.Collections.Generic;
            using System.Linq;
            struct S
            {
                readonly void M(IEnumerable<int> x)
                {
                    var v = from y in x
                            select y;
                }
            }
            """,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67531")]
    public async Task TestWithFixedSizeBufferField1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            struct Repro
            {
                private unsafe fixed byte bytes[16];

                public unsafe void AsSpan()
                {
                    M(ref bytes[0]);
                }

                private readonly void M(ref byte b) { }
            }
            """,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70116")]
    public async Task TestInlineArraySpan1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Security.Cryptography;

            public struct Repro
            {
            	public ByteArray20 Data;
            	public ByteArray20 Hash;

                public void RecalculateHash()
            	{
            		SHA1.HashData(Data, Hash);
            	}

                [InlineArray(20)]
            	public struct ByteArray20
            	{
            		private byte _byte;
            	}
            }
            """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70116")]
    public async Task TestInlineArraySpan2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Security.Cryptography;

            public struct Repro
            {
            	public ByteArray20 Data;
            	public ByteArray20 Hash;

                public void RecalculateHash()
            	{
                    TakesSpan(Data);
            	}

                readonly void TakesSpan(Span<byte> bytes) { }

                [InlineArray(20)]
            	public struct ByteArray20
            	{
            		private byte _byte;
            	}
            }
            """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70116")]
    public async Task TestInlineArraySpan2_A()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Security.Cryptography;

            public struct Repro
            {
            	public ByteArray20 Data;
            	public ByteArray20 Hash;

                public void RecalculateHash()
            	{
                    TakesSpan(this.Data);
            	}

                readonly void TakesSpan(Span<byte> bytes) { }

                [InlineArray(20)]
            	public struct ByteArray20
            	{
            		private byte _byte;
            	}
            }
            """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70116")]
    public async Task TestInlineArraySpan3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Security.Cryptography;

            public struct Repro
            {
            	public ByteArray20 Data;
            	public ByteArray20 Hash;

                public void [|RecalculateHash|]()
            	{
                    TakesReadOnlySpan(Data);
            	}

                readonly void TakesReadOnlySpan(ReadOnlySpan<byte> bytes) { }

                [InlineArray(20)]
            	public struct ByteArray20
            	{
            		private byte _byte;
            	}
            }
            """,
            FixedCode = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Security.Cryptography;

            public struct Repro
            {
            	public ByteArray20 Data;
            	public ByteArray20 Hash;

                public readonly void RecalculateHash()
            	{
                    TakesReadOnlySpan(Data);
            	}

                readonly void TakesReadOnlySpan(ReadOnlySpan<byte> bytes) { }

                [InlineArray(20)]
            	public struct ByteArray20
            	{
            		private byte _byte;
            	}
            }
            """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70116")]
    public async Task TestInlineArraySpan3_A()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Security.Cryptography;

            public struct Repro
            {
            	public ByteArray20 Data;
            	public ByteArray20 Hash;

                public void [|RecalculateHash|]()
            	{
                    TakesReadOnlySpan(this.Data);
            	}

                readonly void TakesReadOnlySpan(ReadOnlySpan<byte> bytes) { }

                [InlineArray(20)]
            	public struct ByteArray20
            	{
            		private byte _byte;
            	}
            }
            """,
            FixedCode = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Security.Cryptography;

            public struct Repro
            {
            	public ByteArray20 Data;
            	public ByteArray20 Hash;

                public readonly void RecalculateHash()
            	{
                    TakesReadOnlySpan(this.Data);
            	}

                readonly void TakesReadOnlySpan(ReadOnlySpan<byte> bytes) { }

                [InlineArray(20)]
            	public struct ByteArray20
            	{
            		private byte _byte;
            	}
            }
            """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70116")]
    public async Task TestInlineArraySpan4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Security.Cryptography;

            public struct Repro
            {
            	public ByteArray20 Data;
            	public ByteArray20 Hash;

                public void [|RecalculateHash|]()
            	{
                    TakesByteArray(Data);
            	}

                readonly void TakesByteArray(ByteArray20 bytes) { }

                [InlineArray(20)]
            	public struct ByteArray20
            	{
            		private byte _byte;
            	}
            }
            """,
            FixedCode = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Security.Cryptography;

            public struct Repro
            {
            	public ByteArray20 Data;
            	public ByteArray20 Hash;

                public readonly void RecalculateHash()
            	{
                    TakesByteArray(Data);
            	}

                readonly void TakesByteArray(ByteArray20 bytes) { }

                [InlineArray(20)]
            	public struct ByteArray20
            	{
            		private byte _byte;
            	}
            }
            """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70116")]
    public async Task TestInlineArraySpan4_A()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Security.Cryptography;

            public struct Repro
            {
            	public ByteArray20 Data;
            	public ByteArray20 Hash;

                public void [|RecalculateHash|]()
            	{
                    TakesByteArray(this.Data);
            	}

                readonly void TakesByteArray(ByteArray20 bytes) { }

                [InlineArray(20)]
            	public struct ByteArray20
            	{
            		private byte _byte;
            	}
            }
            """,
            FixedCode = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Security.Cryptography;

            public struct Repro
            {
            	public ByteArray20 Data;
            	public ByteArray20 Hash;

                public readonly void RecalculateHash()
            	{
                    TakesByteArray(this.Data);
            	}

                readonly void TakesByteArray(ByteArray20 bytes) { }

                [InlineArray(20)]
            	public struct ByteArray20
            	{
            		private byte _byte;
            	}
            }
            """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public async Task TestPrimaryConstructorParameterReference0()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                struct Cell(short value)
                {
                    public readonly short Value => value;

                    public void RemoveBit(int candidate)
                    {
                        value = 0;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public async Task TestPrimaryConstructorParameterReference1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                struct Cell(short value)
                {
                    public readonly short Value => value;

                    public void RemoveBit(int candidate)
                    {
                        value = (short)(value & ~(1 << candidate));
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public async Task TestPrimaryConstructorParameterReference2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                struct Cell(short value)
                {
                    public readonly short Value => value;

                    public void RemoveBit(int candidate)
                    {
                        value++;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public async Task TestPrimaryConstructorParameterReference3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                struct Point
                {
                    public int X;
                }
                
                struct Cell(Point value)
                {
                    public void RemoveBit(int candidate)
                    {
                        value.X++;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public async Task TestPrimaryConstructorParameterReference4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                struct Point
                {
                    public int X;
                }
                
                struct Cell(Point value)
                {
                    public void RemoveBit(int candidate)
                    {
                        value.X = 1;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public async Task TestPrimaryConstructorParameterReference5()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                struct Point
                {
                    public int X;

                    public void MutatingMethod() => X++;
                }
                
                struct Cell(Point value)
                {
                    public void RemoveBit(int candidate)
                    {
                        value.MutatingMethod();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public async Task TestPrimaryConstructorParameterReference6()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                struct Point
                {
                    public int X;

                    public readonly void NonMutatingMethod() { }
                }
                
                struct Cell(Point value)
                {
                    public void [|RemoveBit|](int candidate)
                    {
                        value.NonMutatingMethod();
                    }
                }
                """,
            FixedCode = """
                struct Point
                {
                    public int X;

                    public readonly void NonMutatingMethod() { }
                }
                
                struct Cell(Point value)
                {
                    public readonly void RemoveBit(int candidate)
                    {
                        value.NonMutatingMethod();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public async Task TestPrimaryConstructorParameterReference7()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                struct Cell(int value)
                {
                    public void [|RemoveBit|](int candidate)
                    {
                        var x = value;
                    }
                }
                """,
            FixedCode = """
                struct Cell(int value)
                {
                    public readonly void RemoveBit(int candidate)
                    {
                        var x = value;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public async Task TestPrimaryConstructorParameterReference8()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                struct Cell(string value)
                {
                    public void RemoveBit(int candidate)
                    {
                        value = "";
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public async Task TestPrimaryConstructorParameterReference9()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class Point
                {
                    public int X;

                    public void MutatingMethod() => X++;
                }

                struct Cell(Point point)
                {
                    public void [|RemoveBit|](int candidate)
                    {
                        point.X = 1;
                    }
                }
                """,
            FixedCode = """
                class Point
                {
                    public int X;

                    public void MutatingMethod() => X++;
                }

                struct Cell(Point point)
                {
                    public readonly void RemoveBit(int candidate)
                    {
                        point.X = 1;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public async Task TestPrimaryConstructorParameterReference10()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            class Point
            {
                public int X;

                public void MutatingMethod() => X++;
            }

            struct Cell(Point point)
            {
                public void [|RemoveBit|](int candidate)
                {
                    point.MutatingMethod();
                }
            }
            """,
            FixedCode = """
            class Point
            {
                public int X;

                public void MutatingMethod() => X++;
            }

            struct Cell(Point point)
            {
                public readonly void RemoveBit(int candidate)
                {
                    point.MutatingMethod();
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public async Task TestPrimaryConstructorParameterReference11()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            struct Cell<T>(T t) where T : class, IDisposable
            {
                public void [|RemoveBit|](int candidate)
                {
                    t.Dispose();
                }
            }
            """,
            FixedCode = """
            using System;
            
            struct Cell<T>(T t) where T : class, IDisposable
            {
                public readonly void RemoveBit(int candidate)
                {
                    t.Dispose();
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public async Task TestPrimaryConstructorParameterReference12()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            struct Cell<T>(T t) where T : struct, IDisposable
            {
                public void RemoveBit(int candidate)
                {
                    t.Dispose();
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public async Task TestPrimaryConstructorParameterReference13()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            struct Cell<T>(T t) where T : IDisposable
            {
                public void RemoveBit(int candidate)
                {
                    t.Dispose();
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70864")]
    public async Task TestInitAccessor1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                public record struct TypeMapCapacity
                {
                    private readonly int _MaxSize;

                    public readonly int? MaxSize
                    {
                        get => _MaxSize;

                        // missing, since the property is already readonly
                        init
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70864")]
    public async Task TestInitAccessor2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                public record struct TypeMapCapacity
                {
                    private readonly int _MaxSize;

                    public int? MaxSize
                    {
                        readonly get => _MaxSize;

                        [|init|]
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                using System;

                public record struct TypeMapCapacity
                {
                    private readonly int _MaxSize;

                    public readonly int? MaxSize
                    {
                        get => _MaxSize;

                        init
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70864")]
    public async Task TestInitAccessor3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                public record struct TypeMapCapacity
                {
                    private int _MaxSize;

                    public int? MaxSize
                    {
                        get => _MaxSize++;

                        init
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70864")]
    public async Task TestInitAccessor4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                public record struct TypeMapCapacity
                {
                    private readonly int _MaxSize;

                    public int? MaxSize
                    {
                        [|init|]
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                using System;

                public record struct TypeMapCapacity
                {
                    private readonly int _MaxSize;

                    public readonly int? MaxSize
                    {
                        init
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71500")]
    public async Task TestOnInlineArray()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Runtime.CompilerServices;

                internal struct Repro
                {
                    private Values _values;

                    public void [|Populate|]()
                    {
                    }
                }

                [InlineArray(4)]
                internal struct Values
                {
                    private int _field;
                }
                """,
            FixedCode = """
                using System;
                using System.Runtime.CompilerServices;
                
                internal struct Repro
                {
                    private Values _values;
                
                    public readonly void Populate()
                    {
                    }
                }
                
                [InlineArray(4)]
                internal struct Values
                {
                    private int _field;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71500")]
    public async Task TestOnInlineArrayCapturedIntoReadOnlySpan()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Runtime.CompilerServices;

                internal struct Repro
                {
                    private Values _values;

                    public void [|Populate|]()
                    {
                        ReadOnlySpan<int> values = _values;
                    }
                }

                [InlineArray(4)]
                internal struct Values
                {
                    private int _field;
                }
                """,
            FixedCode = """
                using System;
                using System.Runtime.CompilerServices;
                
                internal struct Repro
                {
                    private Values _values;
                
                    public readonly void Populate()
                    {
                        ReadOnlySpan<int> values = _values;
                    }
                }
                
                [InlineArray(4)]
                internal struct Values
                {
                    private int _field;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71500")]
    public async Task TestOnInlineArrayRead()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Runtime.CompilerServices;

                internal struct Repro
                {
                    private Values _values;

                    public void [|Populate|]()
                    {
                        var v = _values[0];
                    }
                }

                [InlineArray(4)]
                internal struct Values
                {
                    private int _field;
                }
                """,
            FixedCode = """
                using System;
                using System.Runtime.CompilerServices;
                
                internal struct Repro
                {
                    private Values _values;
                
                    public readonly void Populate()
                    {
                        var v = _values[0];
                    }
                }
                
                [InlineArray(4)]
                internal struct Values
                {
                    private int _field;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71500")]
    public async Task TestOnInlineArrayReadSafe()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Runtime.CompilerServices;

                internal struct Repro
                {
                    private Values _values;

                    public void [|Populate|]()
                    {
                        _values[0].Safe();
                    }
                }

                [InlineArray(4)]
                internal struct Values
                {
                    private S _field;
                }

                internal struct S
                {
                    public readonly void Safe() { }
                }
                """,
            FixedCode = """
                using System;
                using System.Runtime.CompilerServices;
                
                internal struct Repro
                {
                    private Values _values;
                
                    public readonly void Populate()
                    {
                        _values[0].Safe();
                    }
                }
                
                [InlineArray(4)]
                internal struct Values
                {
                    private S _field;
                }
                
                internal struct S
                {
                    public readonly void Safe() { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71500")]
    public async Task TestOnInlineArrayReadUnsafe()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Runtime.CompilerServices;

                internal struct Repro
                {
                    private Values _values;

                    public void Populate()
                    {
                        _values[0].Unsafe();
                    }
                }

                [InlineArray(4)]
                internal struct Values
                {
                    private S _field;
                }

                internal struct S
                {
                    private int i;
                    public void Unsafe() { i++; }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71500")]
    public async Task TestNotOnInlineArrayCapturedIntoSpan()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Runtime.CompilerServices;

                internal struct Repro
                {
                    private Values _values;

                    public void Populate()
                    {
                        Span<int> values = _values;
                    }
                }

                [InlineArray(4)]
                internal struct Values
                {
                    private int _field;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71500")]
    public async Task TestNotOnInlineArrayWrittenInfo()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Runtime.CompilerServices;

                internal struct Repro
                {
                    private Values _values;

                    public void Populate()
                    {
                        _values[0] = 1;
                    }
                }

                [InlineArray(4)]
                internal struct Values
                {
                    private int _field;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }
}
