// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    public Task TestEmptyMethod()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNotInClass()
        => new VerifyCS.Test
        {
            TestCode = """
            class S
            {
                void M() { }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotInReadOnlyStruct()
        => new VerifyCS.Test
        {
            TestCode = """
            readonly struct S
            {
                void M() { }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotInReadOnlyMember()
        => new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                readonly void M() { }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotWithAssignmentToThis()
        => new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                void M()
                {
                    this = default;
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotWithThisPassedByRef1()
        => new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                void M()
                {
                    G(ref this);
                }

                static void G(ref S s) { }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotWithThisPassedByRef2()
        => new VerifyCS.Test
        {
            TestCode = """
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
            """,
        }.RunAsync();

    [Fact]
    public Task TestWithThisPassedByIn1_A()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithThisPassedByIn1_B()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithThisPassedByIn2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithThisPassedByIn3()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNotWithWriteToField1()
        => new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int x;
                void M()
                {
                    x = 0;
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotWithWriteToField2()
        => new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int x;
                void M()
                {
                    x++;
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotWithWriteToField3()
        => new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int x;
                void M()
                {
                    G(ref x);
                }

                static void G(ref int x) { }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotWithWriteToField4()
        => new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int x;
                void M()
                {
                    G(out x);
                }

                static void G(out int x) { x = 0; }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotWithWriteToField5()
        => new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int x;
                void M()
                {
                    (x, x) = (0, 0);
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotWithWriteToField6()
        => new VerifyCS.Test
        {
            TestCode = """
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
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotWithWriteToField7()
        => new VerifyCS.Test
        {
            TestCode = """
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
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotWithWriteToField8()
        => new VerifyCS.Test
        {
            TestCode = """
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
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotInCSharp7()
        => new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                void M() { }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7,
        }.RunAsync();

    [Fact]
    public Task TestPropertyExpressionBody()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestPropertyAccessor1()
        => new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int P { get; }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestPropertyAccessor2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestIndexerAccessor2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestPropertyAccessor3()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestIndexerAccessor3()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWriteToFieldNotThroughThis()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestCallToStaticMethod()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestRecursiveCall()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMultipleAccessor()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMultipleIndexerAccessor()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMultipleAccessor_FixOne1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMultipleIndexerAccessor_FixOne1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMultipleAccessor2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMultipleIndexerAccessor2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestTakeRefReadOnlyToField()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNotWithAddressOfFieldTaken()
        => new VerifyCS.Test
        {
            TestCode = """
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
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotWithCallToNonReadOnlyMethod()
        => new VerifyCS.Test
        {
            TestCode = """
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
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotWithCallToNonReadOnlyIndexer()
        => new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                int x;
                int this[int y] { get { return x++; } set { x++; } }

                void M()
                {
                    var v = this[0];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotWithCaptureOfNonReadOnlyMethod1()
        => new VerifyCS.Test
        {
            TestCode = """
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
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotWithCaptureOfNonReadOnlyMethod2()
        => new VerifyCS.Test
        {
            TestCode = """
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
            """,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();

    [Fact]
    public Task TestCallToObjectMethod()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestCallToReadOnlyMethod()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestCallToReadOnlyIndexer1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestCallToReadOnlyIndexer2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestExplicitInterfaceImpl()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestEventMutation()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;
            struct S
            {
                event Action E;

                void M()
                {
                    this.E += () => { };
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotWithNonReadOnlyMethodCallOnField()
        => new VerifyCS.Test
        {
            TestCode = """
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
            """,
        }.RunAsync();

    [Fact]
    public Task TestWithReadOnlyMethodCallOnField()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNotWithNonReadOnlyMethodOnUnconstrainedField()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;
            struct T<X> where X : IComparable
            {
                X x;
                public void M() { x.CompareTo(null); }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotWithNonReadOnlyMethodOnStructConstrainedField()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;
            struct T<X> where X : struct, IComparable
            {
                X x;
                public void M() { x.CompareTo(null); }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestWithNonReadOnlyMethodOnClassConstrainedField()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNotWithMethodThatOnlyThrows1()
        => new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                void M() => throw new System.Exception();
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotWithMethodThatOnlyThrows2()
        => new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                void M()
                {
                    throw new System.Exception();
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotWithBadOperation()
        => new VerifyCS.Test
        {
            TestCode = """
            struct S
            {
                void M()
                {
                    {|CS0103:Goo|}();
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestWithLinqRewrite()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67531")]
    public Task TestWithFixedSizeBufferField1()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70116")]
    public Task TestInlineArraySpan1()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70116")]
    public Task TestInlineArraySpan2()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70116")]
    public Task TestInlineArraySpan2_A()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70116")]
    public Task TestInlineArraySpan3()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70116")]
    public Task TestInlineArraySpan3_A()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70116")]
    public Task TestInlineArraySpan4()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70116")]
    public Task TestInlineArraySpan4_A()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public Task TestPrimaryConstructorParameterReference0()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public Task TestPrimaryConstructorParameterReference1()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public Task TestPrimaryConstructorParameterReference2()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public Task TestPrimaryConstructorParameterReference3()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public Task TestPrimaryConstructorParameterReference4()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public Task TestPrimaryConstructorParameterReference5()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public Task TestPrimaryConstructorParameterReference6()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public Task TestPrimaryConstructorParameterReference7()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public Task TestPrimaryConstructorParameterReference8()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public Task TestPrimaryConstructorParameterReference9()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public Task TestPrimaryConstructorParameterReference10()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public Task TestPrimaryConstructorParameterReference11()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public Task TestPrimaryConstructorParameterReference12()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70780")]
    public Task TestPrimaryConstructorParameterReference13()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70864")]
    public Task TestInitAccessor1()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70864")]
    public Task TestInitAccessor2()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70864")]
    public Task TestInitAccessor3()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70864")]
    public Task TestInitAccessor4()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71500")]
    public Task TestOnInlineArray()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71500")]
    public Task TestOnInlineArrayCapturedIntoReadOnlySpan()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71500")]
    public Task TestOnInlineArrayRead()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71500")]
    public Task TestOnInlineArrayReadSafe()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71500")]
    public Task TestOnInlineArrayReadUnsafe()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71500")]
    public Task TestNotOnInlineArrayCapturedIntoSpan()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71500")]
    public Task TestNotOnInlineArrayWrittenInfo()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71500")]
    public Task TestMultipleAccessors()
        => new VerifyCS.Test
        {
            TestCode = """
                struct S
                {
                    public int M;

                    public int Z
                    {
                        [|get|] => M;
                        set => M = value;
                    }
                }
                """,
            FixedCode = """
                struct S
                {
                    public int M;

                    public int Z
                    {
                        readonly get => M;
                        set => M = value;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79384")]
    public Task TestSpanFromInlineArray()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Runtime.CompilerServices;

                public struct Example
                {
                    private Buffer _buffer;

                    public void Set(int index, int value)
                    {
                        var bufferSpan = _buffer[..0];

                        bufferSpan[0] = value;
                    }
                }

                [InlineArray(1)]
                public struct Buffer
                {
                    private int _element0;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        }.RunAsync();
}
