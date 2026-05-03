// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Snippets;

[Trait(Traits.Feature, Traits.Features.Snippets)]
public sealed class CSharpConstructorSnippetProviderTests : AbstractCSharpSnippetProviderTests
{
    protected override string SnippetIdentifier => "ctor";

    [Fact]
    public Task ConstructorSnippetMissingInNamespace()
        => VerifySnippetIsAbsentAsync("""
            namespace Namespace
            {
                $$
            }
            """);

    [Fact]
    public Task ConstructorSnippetMissingInFileScopedNamespace()
        => VerifySnippetIsAbsentAsync("""
            namespace Namespace;

            $$
            """);

    [Fact]
    public Task ConstructorSnippetMissingInTopLevelContext()
        => VerifySnippetIsAbsentAsync("""
            System.Console.WriteLine();
            $$
            """);

    [Fact]
    public Task InsertConstructorSnippetInClassTest()
        => VerifySnippetAsync("""
            class MyClass
            {
                $$
            }
            """, """
            class MyClass
            {
                public MyClass()
                {
                    $$
                }
            }
            """);

    [Fact]
    public Task InsertConstructorSnippetInAbstractClassTest()
        => VerifySnippetAsync("""
            abstract class MyClass
            {
                $$
            }
            """, """
            abstract class MyClass
            {
                protected MyClass()
                {
                    $$
                }
            }
            """);

    [Fact]
    public Task InsertConstructorSnippetInAbstractClassTest_AbstractModifierInOtherPartialDeclaration()
        => VerifySnippetAsync("""
            partial class MyClass
            {
                $$
            }

            abstract partial class MyClass
            {
            }
            """, """
            partial class MyClass
            {
                protected MyClass()
                {
                    $$
                }
            }
            
            abstract partial class MyClass
            {
            }
            """);

    [Fact]
    public Task InsertConstructorSnippetInNestedAbstractClassTest()
        => VerifySnippetAsync("""
            class MyClass
            {
                abstract class NestedClass
                {
                    $$
                }
            }
            """, """
            class MyClass
            {
                abstract class NestedClass
                {
                    protected NestedClass()
                    {
                        $$
                    }
                }
            }
            """);

    [Fact]
    public Task InsertConstructorSnippetInStructTest()
        => VerifySnippetAsync("""
            struct MyStruct
            {
                $$
            }
            """, """
            struct MyStruct
            {
                public MyStruct()
                {
                    $$
                }
            }
            """);

    [Fact]
    public Task InsertConstructorSnippetInRecordTest()
        => VerifySnippetAsync("""
            record MyRecord
            {
                $$
            }
            """, """
            record MyRecord
            {
                public MyRecord()
                {
                    $$
                }
            }
            """);

    [Fact]
    public Task ConstructorSnippetMissingInInterface()
        => VerifySnippetIsAbsentAsync("""
            interface MyInterface
            {
                $$
            }
            """);

    [Fact]
    public Task InsertConstructorSnippetInNestedClassTest()
        => VerifySnippetAsync("""
            class MyClass
            {
                class MyClass1
                {
                    $$
                }
            }
            """, """
            class MyClass
            {
                class MyClass1
                {
                    public MyClass1()
                    {
                        $$
                    }
                }
            }
            """);

    [Theory]
    [InlineData("static")]
    [MemberData(nameof(CommonSnippetTestData.AllAccessibilityModifiers), MemberType = typeof(CommonSnippetTestData))]
    public Task InsertConstructorSnippetAfterValidModifiersTest(string modifiers)
        => VerifySnippetAsync($$"""
            class MyClass
            {
                {{modifiers}} $$
            }
            """, $$"""
            class MyClass
            {
                {{modifiers}} MyClass()
                {
                    $$
                }
            }
            """);

    [Theory]
    [InlineData("abstract")]
    [InlineData("sealed")]
    [InlineData("virtual")]
    [InlineData("override")]
    [InlineData("readonly")]
    [InlineData("new")]
    [InlineData("file")]
    public Task ConstructorSnippetMissingAfterInvalidModifierTest(string modifier)
        => VerifySnippetIsAbsentAsync($$"""
            class MyClass
            {
                {{modifier}} $$
            }
            """);

    [Theory]
    [MemberData(nameof(CommonSnippetTestData.AllAccessibilityModifiers), MemberType = typeof(CommonSnippetTestData))]
    public Task ConstructorSnippetMissingAfterBothAccessibilityModifierAndStaticKeywordTest(string accessibilityModifier)
        => VerifySnippetIsAbsentAsync($$"""
            class MyClass
            {
                {{accessibilityModifier}} static $$
            }
            """);

    [Fact]
    public Task InsertConstructorSnippetAfterAccessibilityModifierBeforeOtherMemberTest()
        => VerifySnippetAsync("""
            class C
            {
                private $$
                readonly int Value = 3;
            }
            """, """
            class C
            {
                private C()
                {
                    $$
                }
                readonly int Value = 3;
            }
            """);

    [Fact]
    public Task InsertConstructorSnippetBetweenAccessibilityModifiersBeforeOtherMemberTest()
        => VerifySnippetAsync("""
            class C
            {
                protected $$
                internal int Value = 3;
            }
            """, """
            class C
            {
                protected C()
                {
                    $$
                }
                internal int Value = 3;
            }
            """);

    [Fact]
    public Task InsertConstructorSnippetAfterAccessibilityModifierBeforeOtherStaticMemberTest()
        => VerifySnippetAsync("""
            class C
            {
                internal $$
                static int Value = 3;
            }
            """, """
            class C
            {
                internal C()
                {
                    $$
                }
                static int Value = 3;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68176")]
    public Task InsertCorrectConstructorSnippetInNestedTypeTest_CtorBeforeNestedType()
        => VerifySnippetAsync("""
            class Outer
            {
                $$
                class Inner
                {
                }
            }
            """, """
            class Outer
            {
                public Outer()
                {
                    $$
                }
                class Inner
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68176")]
    public Task InsertCorrectConstructorSnippetInNestedTypeTest_CtorAfterNestedType()
        => VerifySnippetAsync("""
            class Outer
            {
                class Inner
                {
                }
                $$
            }
            """, """
            class Outer
            {
                class Inner
                {
                }
                public Outer()
                {
                    $$
                }
            }
            """);
}
