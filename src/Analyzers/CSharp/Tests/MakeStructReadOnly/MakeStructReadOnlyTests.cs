// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.MakeStructReadOnly;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeStructReadOnly;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpMakeStructReadOnlyDiagnosticAnalyzer,
    CSharpMakeStructReadOnlyCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructReadOnly)]
public class MakeStructReadOnlyTests
{
    private static Task TestMissingAsync(string testCode, LanguageVersion version = LanguageVersion.CSharp12)
        => TestAsync(testCode, testCode, version);

    private static async Task TestAsync(string testCode, string fixedCode, LanguageVersion version = LanguageVersion.CSharp12)
    {
        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            LanguageVersion = version,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
        }.RunAsync();
    }

    [Fact]
    public async Task ShouldNotTriggerForCSharp7_1()
    {
        await TestMissingAsync(
            """
            struct S
            {
                readonly int i;
            }
            """, LanguageVersion.CSharp7_1);
    }

    [Fact]
    public async Task ShouldTriggerFor7_2()
    {
        await TestAsync(
            """
            struct [|S|]
            {
                readonly int i;
            }
            """,
            """
            readonly struct S
            {
                readonly int i;
            }
            """,
LanguageVersion.CSharp7_2);
    }

    [Fact]
    public async Task TestMissingWithAlreadyReadOnlyStruct()
    {
        await TestMissingAsync(
            """
            readonly struct S
            {
                readonly int i;
            }
            """);
    }

    [Fact]
    public async Task TestMissingWithAlreadyReadOnlyRecordStruct()
    {
        await TestMissingAsync(
            """
            readonly record struct S
            {
                readonly int i;
            }
            """);
    }

    [Fact]
    public async Task TestMissingWithMutableField()
    {
        await TestMissingAsync(
            """
            struct S
            {
                int i;
            }
            """);
    }

    [Fact]
    public async Task TestMissingWithMutableFieldRecordStruct()
    {
        await TestMissingAsync(
            """
            record struct S
            {
                int i;
            }
            """);
    }

    [Fact]
    public async Task TestMissingWithMutableAndReadOnlyField()
    {
        await TestMissingAsync(
            """
            struct S
            {
                int i;
                readonly int j;
            }
            """);
    }

    [Fact]
    public async Task TestMissingWithMutableAndReadOnlyFieldRecordStruct1()
    {
        await TestMissingAsync(
            """
            record struct S
            {
                int i;
                readonly int j;
            }
            """);
    }

    [Fact]
    public async Task TestMissingWithMutableAndReadOnlyFieldRecordStruct2()
    {
        await TestMissingAsync(
            """
            record struct S(int j)
            {
                int i;
            }
            """);
    }

    [Fact]
    public async Task TestMissingWithMutableAndReadOnlyFieldStruct2()
    {
        await TestMissingAsync(
            """
            struct S(int j)
            {
                int i;
            }
            """);
    }

    [Fact]
    public async Task TestMissingWithMutableProperty()
    {
        await TestMissingAsync(
            """
            struct S
            {
                int P { get; set; }
            }
            """);
    }

    [Fact]
    public async Task TestMissingWithMutablePropertyRecordStruct1()
    {
        await TestMissingAsync(
            """
            record struct S
            {
                int P { get; set; }
            }
            """);
    }

    [Fact]
    public async Task TestMissingWithMutablePropertyRecordStruct2()
    {
        await TestMissingAsync(
            """
            record struct S(int q)
            {
                int P { get; set; }
            }
            """);
    }

    [Fact]
    public async Task TestMissingWithMutablePropertyStruct2()
    {
        await TestMissingAsync(
            """
            struct S(int q)
            {
                int P { get; set; }
            }
            """);
    }

    [Fact]
    public async Task TestMissingWithEmptyStruct()
    {
        await TestMissingAsync(
            """
            struct S
            {
            }
            """);
    }

    [Fact]
    public async Task TestMissingWithEmptyRecordStruct()
    {
        await TestMissingAsync(
            """
            record struct S
            {
            }
            """);
    }

    [Fact]
    public async Task TestMissingWithEmptyRecordStructPrimaryConstructor()
    {
        await TestMissingAsync(
            """
            record struct S()
            {
            }
            """);
    }

    [Fact]
    public async Task TestMissingWithEmptyStructPrimaryConstructor()
    {
        await TestMissingAsync(
            """
            struct S()
            {
            }
            """);
    }

    [Fact]
    public async Task TestMissingWithOtherReadonlyPartialPart()
    {
        await TestMissingAsync(
            """
            partial struct S
            {
                readonly int i;
            }

            readonly partial struct S
            {
            }
            """);
    }

    [Fact]
    public async Task TestOnStructWithReadOnlyField()
    {
        await TestAsync(
            """
            struct [|S|]
            {
                readonly int i;
            }
            """,
            """
            readonly struct S
            {
                readonly int i;
            }
            """);
    }

    [Fact]
    public async Task TestOnRecordStructWithReadOnlyField()
    {
        await TestAsync(
            """
            record struct [|S|]
            {
                readonly int i;
            }
            """,
            """
            readonly record struct S
            {
                readonly int i;
            }
            """);
    }

    [Fact]
    public async Task TestOnStructWithGetOnlyProperty()
    {
        await TestAsync(
            """
            struct [|S|]
            {
                int P { get; }
            }
            """,
            """
            readonly struct S
            {
                int P { get; }
            }
            """);
    }

    [Fact]
    public async Task TestOnRecordStructWithGetOnlyProperty()
    {
        await TestAsync(
            """
            record struct [|S|]
            {
                int P { get; }
            }
            """,
            """
            readonly record struct S
            {
                int P { get; }
            }
            """);
    }

    [Fact]
    public async Task TestOnStructWithInitOnlyProperty()
    {
        await TestAsync(
            """
            struct [|S|]
            {
                int P { get; init; }
            }
            """,
            """
            readonly struct S
            {
                int P { get; init; }
            }
            """);
    }

    [Fact]
    public async Task TestOnRecordStructWithInitOnlyProperty()
    {
        await TestAsync(
            """
            record struct [|S|]
            {
                int P { get; init; }
            }
            """,
            """
            readonly record struct S
            {
                int P { get; init; }
            }
            """);
    }

    [Fact]
    public async Task TestOnRecordStructWithReadOnlyField2()
    {
        await TestAsync(
            """
            record struct [|S|]
            {
                readonly int i;
            }
            """,
            """
            readonly record struct S
            {
                readonly int i;
            }
            """);
    }

    [Fact]
    public async Task TestMissingRecordStructWithPrimaryConstructorField()
    {
        await TestMissingAsync(
            """
            record struct S(int i)
            {
            }
            """);
    }

    [Fact]
    public async Task TestMissingStructWithPrimaryConstructor()
    {
        await TestMissingAsync(
            """
            struct S(int i)
            {
            }
            """);
    }

    [Fact]
    public async Task TestMissingOnRecordStructWithPrimaryConstructorFieldAndNormalField()
    {
        await TestMissingAsync(
            """
            record struct S(int i)
            {
                readonly int j;
            }
            """);
    }

    [Fact]
    public async Task TestOnStructWithPrimaryConstructorAndReadonlyField()
    {
        await TestAsync(
            """
            struct [|S|](int i)
            {
                readonly int i;
            }
            """,
            """
            readonly struct S(int i)
            {
                readonly int i;
            }
            """,
LanguageVersion.CSharp12);
    }

    [Fact]
    public async Task TestNestedStructs1()
    {
        await TestAsync(
            """
            struct [|S|]
            {
                readonly int i;

                struct [|T|]
                {
                    readonly int j;
                }
            }
            """,
            """
            readonly struct S
            {
                readonly int i;

                readonly struct T
                {
                    readonly int j;
                }
            }
            """);
    }

    [Fact]
    public async Task TestNestedStructs2()
    {
        await TestAsync(
            """
            struct [|S|]
            {
                readonly int i;

                struct T
                {
                    int j;
                }
            }
            """,
            """
            readonly struct S
            {
                readonly int i;

                struct T
                {
                    int j;
                }
            }
            """);
    }

    [Fact]
    public async Task TestNestedStructs3()
    {
        await TestAsync(
            """
            struct S
            {
                int i;

                struct [|T|]
                {
                    readonly int j;
                }
            }
            """,
            """
            struct S
            {
                int i;

                readonly struct T
                {
                    readonly int j;
                }
            }
            """);
    }

    [Fact]
    public async Task TestNestedStructs4()
    {
        await TestAsync(
            """
            struct [|S|]
            {
                readonly int i;

                struct T
                {
                    readonly int j;

                    void M()
                    {
                        this = default;
                    }
                }
            }
            """,
            """
            readonly struct S
            {
                readonly int i;

                struct T
                {
                    readonly int j;

                    void M()
                    {
                        this = default;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestDocComments1()
    {
        await TestAsync(
            """
            /// <summary>docs</summary>
            record struct [|S|]
            {
                readonly int j;
            }
            """,
            """
            /// <summary>docs</summary>
            readonly record struct S
            {
                readonly int j;
            }
            """);
    }

    [Fact]
    public async Task TestDocComments2()
    {
        await TestAsync(
            """
            namespace N
            {
                /// <summary>docs</summary>
                record struct [|S|]
                {
                    readonly int j;
                }
            }
            """,
            """
            namespace N
            {
                /// <summary>docs</summary>
                readonly record struct S
                {
                    readonly int j;
                }
            }
            """);
    }

    [Fact]
    public async Task TestExistingModifier1()
    {
        await TestAsync(
            """
            public record struct [|S|]
            {
                readonly int j;
            }
            """,
            """
            public readonly record struct S
            {
                readonly int j;
            }
            """);
    }

    [Fact]
    public async Task TestExistingModifier2()
    {
        await TestAsync(
            """
            namespace N
            {
                public record struct [|S|]
                {
                    readonly int j;
                }
            }
            """,
            """
            namespace N
            {
                public readonly record struct S
                {
                    readonly int j;
                }
            }
            """);
    }

    [Fact]
    public async Task TestOnStructWithReadOnlyFieldAndMutableNormalProp()
    {
        await TestAsync(
            """
            struct [|S|]
            {
                readonly int i;

                int P { set { } }
            }
            """,
            """
            readonly struct S
            {
                readonly int i;

                int P { set { } }
            }
            """);
    }

    [Fact]
    public async Task TestOnStructWithReadOnlyFieldAndMutableAutoProp()
    {
        await TestMissingAsync(
            """
            struct S
            {
                readonly int i;

                int P { get; set; }
            }
            """);
    }

    [Fact]
    public async Task TestMissingOnStructThatWritesToThis1()
    {
        await TestMissingAsync(
            """
            struct S
            {
                readonly int i;

                void M()
                {
                    this = default;
                }
            }
            """);
    }

    [Fact]
    public async Task TestMissingOnStructThatWritesToThis2()
    {
        await TestMissingAsync(
            """
            struct S
            {
                readonly int i;

                void M()
                {
                    this.ByRef();
                }
            }

            static class Extensions
            {
                public static void ByRef(ref this S s) { }
            }
            """);
    }

    [Fact]
    public async Task TestMissingOnStructThatWritesToThis3()
    {
        await TestMissingAsync(
            """
            struct S
            {
                readonly int i;

                void M()
                {
                    Goo(ref this);
                }

                void Goo(ref S s) { }
            }
            """);
    }

    [Fact]
    public async Task TestMissingOnStructThatWritesToThis4()
    {
        await TestMissingAsync(
            """
            struct S
            {
                readonly int i;

                void M()
                {
                    Goo(out this);
                }

                void Goo(out S s) { s = default; }
            }
            """);
    }

    [Fact]
    public async Task TestMissingOnStructThatWritesToThis5()
    {
        await TestMissingAsync(
            """
            struct S
            {
                readonly int i;

                void M()
                {
                    ref S s = ref this;
                }
            }
            """);
    }

    [Fact]
    public async Task TestMissingOnStructThatWritesToThis6()
    {
        await TestMissingAsync(
            """
            struct S
            {
                readonly int i;

                void M()
                {
                    this++;
                }

                public static S operator++(S s) => default;
            }
            """);
    }

    [Fact]
    public async Task TestOnStructThatReadsFromThis1()
    {
        await TestAsync(
            """
            struct [|S|]
            {
                readonly int i;

                void M()
                {
                    Goo(in this);
                }

                void Goo(in S s) { }
            }
            """,
            """
            readonly struct S
            {
                readonly int i;

                void M()
                {
                    Goo(in this);
                }

                void Goo(in S s) { }
            }
            """);
    }

    [Fact]
    public async Task TestOnStructThatReadsFromThis2()
    {
        await TestAsync(
            """
            struct [|S|]
            {
                readonly int i;

                void M()
                {
                    this.Goo();
                }

                void Goo() { }
            }
            """,
            """
            readonly struct S
            {
                readonly int i;

                void M()
                {
                    this.Goo();
                }

                void Goo() { }
            }
            """);
    }

    [Fact]
    public async Task TestOnStructThatReadsFromThis3()
    {
        await TestAsync(
            """
            struct [|S|]
            {
                readonly int i;

                void M()
                {
                    this.Goo();
                }
            }

            static class Extensions
            {
                public static void Goo(this S s) { }
            }
            """,
            """
            readonly struct S
            {
                readonly int i;

                void M()
                {
                    this.Goo();
                }
            }

            static class Extensions
            {
                public static void Goo(this S s) { }
            }
            """);
    }

    [Fact]
    public async Task TestOnStructThatReadsFromThis4()
    {
        await TestAsync(
            """
            struct [|S|]
            {
                readonly int i;

                void M()
                {
                    ref readonly S s = ref this;
                }
            }
            """,
            """
            readonly struct S
            {
                readonly int i;

                void M()
                {
                    ref readonly S s = ref this;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69994")]
    public async Task NotWithFieldLikeEvent()
    {
        await TestMissingAsync(
            """
            using System;

            public struct MyStruct
            {
                public event Action MyEvent;

                public readonly int MyInt;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69994")]
    public async Task WithPropertyLikeEvent1()
    {
        await TestAsync(
            """
            using System;

            public struct [|MyStruct|]
            {
                public event Action MyEvent { add { } remove { } }

                public readonly int MyInt;
            }
            """,
            """
            using System;

            public readonly struct MyStruct
            {
                public event Action MyEvent { add { } remove { } }

                public readonly int MyInt;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69994")]
    public async Task WithPropertyLikeEvent2()
    {
        await TestAsync(
            """
            using System;
            using System.Collections.Generic;

            public struct [|MyStruct|]
            {
                private readonly List<Action> actions = new();

                public event Action MyEvent { add => actions.Add(value); remove => actions.Remove(value); }

                public MyStruct() { }
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            public readonly struct MyStruct
            {
                private readonly List<Action> actions = new();
            
                public event Action MyEvent { add => actions.Add(value); remove => actions.Remove(value); }
            
                public MyStruct() { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69994")]
    public async Task NotWithPropertyLikeEvent1()
    {
        await TestMissingAsync(
            """
            using System;
            using System.Collections.Generic;

            public struct MyStruct
            {
                private List<Action> actions = new();

                public event Action MyEvent { add => actions.Add(value); remove => actions.Remove(value); }
            
                public MyStruct() { }
            }
            """);
    }
}
