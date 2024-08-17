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
    public async Task ConstructorSnippetMissingInNamespace()
    {
        await VerifySnippetIsAbsentAsync("""
            namespace Namespace
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task ConstructorSnippetMissingInFileScopedNamespace()
    {
        await VerifySnippetIsAbsentAsync("""
            namespace Namespace;

            $$
            """);
    }

    [Fact]
    public async Task ConstructorSnippetMissingInTopLevelContext()
    {
        await VerifySnippetIsAbsentAsync("""
            System.Console.WriteLine();
            $$
            """);
    }

    [Fact]
    public async Task InsertConstructorSnippetInClassTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertConstructorSnippetInAbstractClassTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertConstructorSnippetInAbstractClassTest_AbstractModifierInOtherPartialDeclaration()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertConstructorSnippetInNestedAbstractClassTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertConstructorSnippetInStructTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertConstructorSnippetInRecordTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task ConstructorSnippetMissingInInterface()
    {
        await VerifySnippetIsAbsentAsync("""
            interface MyInterface
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task InsertConstructorSnippetInNestedClassTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Theory]
    [InlineData("public")]
    [InlineData("private")]
    [InlineData("protected")]
    [InlineData("internal")]
    [InlineData("private protected")]
    [InlineData("protected internal")]
    [InlineData("static")]
    public async Task InsertConstructorSnippetAfterValidModifiersTest(string modifiers)
    {
        await VerifySnippetAsync($$"""
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
    }

    [Theory]
    [InlineData("abstract")]
    [InlineData("sealed")]
    [InlineData("virtual")]
    [InlineData("override")]
    [InlineData("readonly")]
    [InlineData("new")]
    [InlineData("file")]
    public async Task ConstructorSnippetMissingAfterInvalidModifierTest(string modifier)
    {
        await VerifySnippetIsAbsentAsync($$"""
            class MyClass
            {
                {{modifier}} $$
            }
            """);
    }

    [Theory]
    [InlineData("public")]
    [InlineData("private")]
    [InlineData("protected")]
    [InlineData("internal")]
    [InlineData("private protected")]
    [InlineData("protected internal")]
    public async Task ConstructorSnippetMissingAfterBothAccessibilityModifierAndStaticKeywordTest(string accessibilityModifier)
    {
        await VerifySnippetIsAbsentAsync($$"""
            class MyClass
            {
                {{accessibilityModifier}} static $$
            }
            """);
    }

    [Fact]
    public async Task InsertConstructorSnippetAfterAccessibilityModifierBeforeOtherMemberTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertConstructorSnippetBetweenAccessibilityModifiersBeforeOtherMemberTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertConstructorSnippetAfterAccessibilityModifierBeforeOtherStaticMemberTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68176")]
    public async Task InsertCorrectConstructorSnippetInNestedTypeTest_CtorBeforeNestedType()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68176")]
    public async Task InsertCorrectConstructorSnippetInNestedTypeTest_CtorAfterNestedType()
    {
        await VerifySnippetAsync("""
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
}
