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
public sealed class MakeStructReadOnlyTests
{
    private static Task TestMissingAsync(string testCode, LanguageVersion version = LanguageVersion.CSharp12)
        => TestAsync(testCode, testCode, version);

    private static Task TestAsync(string testCode, string fixedCode, LanguageVersion version = LanguageVersion.CSharp12)
        => new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            LanguageVersion = version,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
        }.RunAsync();

    [Fact]
    public Task ShouldNotTriggerForCSharp7_1()
        => TestMissingAsync(
            """
            struct S
            {
                readonly int i;
            }
            """, LanguageVersion.CSharp7_1);

    [Fact]
    public Task ShouldTriggerFor7_2()
        => TestAsync(
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

    [Fact]
    public Task TestMissingWithAlreadyReadOnlyStruct()
        => TestMissingAsync(
            """
            readonly struct S
            {
                readonly int i;
            }
            """);

    [Fact]
    public Task TestMissingWithAlreadyReadOnlyRecordStruct()
        => TestMissingAsync(
            """
            readonly record struct S
            {
                readonly int i;
            }
            """);

    [Fact]
    public Task TestMissingWithMutableField()
        => TestMissingAsync(
            """
            struct S
            {
                int i;
            }
            """);

    [Fact]
    public Task TestMissingWithMutableFieldRecordStruct()
        => TestMissingAsync(
            """
            record struct S
            {
                int i;
            }
            """);

    [Fact]
    public Task TestMissingWithMutableAndReadOnlyField()
        => TestMissingAsync(
            """
            struct S
            {
                int i;
                readonly int j;
            }
            """);

    [Fact]
    public Task TestMissingWithMutableAndReadOnlyFieldRecordStruct1()
        => TestMissingAsync(
            """
            record struct S
            {
                int i;
                readonly int j;
            }
            """);

    [Fact]
    public Task TestMissingWithMutableAndReadOnlyFieldRecordStruct2()
        => TestMissingAsync(
            """
            record struct S(int j)
            {
                int i;
            }
            """);

    [Fact]
    public Task TestMissingWithMutableAndReadOnlyFieldStruct2()
        => TestMissingAsync(
            """
            struct S(int j)
            {
                int i;
            }
            """);

    [Fact]
    public Task TestMissingWithMutableProperty()
        => TestMissingAsync(
            """
            struct S
            {
                int P { get; set; }
            }
            """);

    [Fact]
    public Task TestMissingWithMutablePropertyRecordStruct1()
        => TestMissingAsync(
            """
            record struct S
            {
                int P { get; set; }
            }
            """);

    [Fact]
    public Task TestMissingWithMutablePropertyRecordStruct2()
        => TestMissingAsync(
            """
            record struct S(int q)
            {
                int P { get; set; }
            }
            """);

    [Fact]
    public Task TestMissingWithMutablePropertyStruct2()
        => TestMissingAsync(
            """
            struct S(int q)
            {
                int P { get; set; }
            }
            """);

    [Fact]
    public Task TestMissingWithEmptyStruct()
        => TestMissingAsync(
            """
            struct S
            {
            }
            """);

    [Fact]
    public Task TestMissingWithEmptyRecordStruct()
        => TestMissingAsync(
            """
            record struct S
            {
            }
            """);

    [Fact]
    public Task TestMissingWithEmptyRecordStructPrimaryConstructor()
        => TestMissingAsync(
            """
            record struct S()
            {
            }
            """);

    [Fact]
    public Task TestMissingWithEmptyStructPrimaryConstructor()
        => TestMissingAsync(
            """
            struct S()
            {
            }
            """);

    [Fact]
    public Task TestMissingWithOtherReadonlyPartialPart()
        => TestMissingAsync(
            """
            partial struct S
            {
                readonly int i;
            }

            readonly partial struct S
            {
            }
            """);

    [Fact]
    public Task TestOnStructWithReadOnlyField()
        => TestAsync(
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

    [Fact]
    public Task TestOnRecordStructWithReadOnlyField()
        => TestAsync(
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

    [Fact]
    public Task TestOnStructWithGetOnlyProperty()
        => TestAsync(
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

    [Fact]
    public Task TestOnRecordStructWithGetOnlyProperty()
        => TestAsync(
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

    [Fact]
    public Task TestOnStructWithInitOnlyProperty()
        => TestAsync(
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

    [Fact]
    public Task TestOnRecordStructWithInitOnlyProperty()
        => TestAsync(
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

    [Fact]
    public Task TestOnRecordStructWithReadOnlyField2()
        => TestAsync(
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

    [Fact]
    public Task TestMissingRecordStructWithPrimaryConstructorField()
        => TestMissingAsync(
            """
            record struct S(int i)
            {
            }
            """);

    [Fact]
    public Task TestMissingStructWithPrimaryConstructor()
        => TestMissingAsync(
            """
            struct S(int i)
            {
            }
            """);

    [Fact]
    public Task TestMissingOnRecordStructWithPrimaryConstructorFieldAndNormalField()
        => TestMissingAsync(
            """
            record struct S(int i)
            {
                readonly int j;
            }
            """);

    [Fact]
    public Task TestOnStructWithPrimaryConstructorAndReadonlyField()
        => TestAsync(
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

    [Fact]
    public Task TestNestedStructs1()
        => TestAsync(
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

    [Fact]
    public Task TestNestedStructs2()
        => TestAsync(
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

    [Fact]
    public Task TestNestedStructs3()
        => TestAsync(
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

    [Fact]
    public Task TestNestedStructs4()
        => TestAsync(
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

    [Fact]
    public Task TestDocComments1()
        => TestAsync(
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

    [Fact]
    public Task TestDocComments2()
        => TestAsync(
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

    [Fact]
    public Task TestExistingModifier1()
        => TestAsync(
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

    [Fact]
    public Task TestExistingModifier2()
        => TestAsync(
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

    [Fact]
    public Task TestOnStructWithReadOnlyFieldAndMutableNormalProp()
        => TestAsync(
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

    [Fact]
    public Task TestOnStructWithReadOnlyFieldAndMutableAutoProp()
        => TestMissingAsync(
            """
            struct S
            {
                readonly int i;

                int P { get; set; }
            }
            """);

    [Fact]
    public Task TestMissingOnStructThatWritesToThis1()
        => TestMissingAsync(
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

    [Fact]
    public Task TestMissingOnStructThatWritesToThis2()
        => TestMissingAsync(
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

    [Fact]
    public Task TestMissingOnStructThatWritesToThis3()
        => TestMissingAsync(
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

    [Fact]
    public Task TestMissingOnStructThatWritesToThis4()
        => TestMissingAsync(
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

    [Fact]
    public Task TestMissingOnStructThatWritesToThis5()
        => TestMissingAsync(
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

    [Fact]
    public Task TestMissingOnStructThatWritesToThis6()
        => TestMissingAsync(
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

    [Fact]
    public Task TestOnStructThatReadsFromThis1()
        => TestAsync(
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

    [Fact]
    public Task TestOnStructThatReadsFromThis2()
        => TestAsync(
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

    [Fact]
    public Task TestOnStructThatReadsFromThis3()
        => TestAsync(
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

    [Fact]
    public Task TestOnStructThatReadsFromThis4()
        => TestAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69994")]
    public Task NotWithFieldLikeEvent()
        => TestMissingAsync(
            """
            using System;

            public struct MyStruct
            {
                public event Action MyEvent;

                public readonly int MyInt;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69994")]
    public Task WithPropertyLikeEvent1()
        => TestAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69994")]
    public Task WithPropertyLikeEvent2()
        => TestAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69994")]
    public Task NotWithPropertyLikeEvent1()
        => TestMissingAsync(
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
