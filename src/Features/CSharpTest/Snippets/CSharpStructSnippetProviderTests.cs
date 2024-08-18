// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Snippets;

[Trait(Traits.Feature, Traits.Features.Snippets)]
public sealed class CSharpStructSnippetProviderTests : AbstractCSharpSnippetProviderTests
{
    protected override string SnippetIdentifier => "struct";

    [Fact]
    public async Task InsertStructSnippetInBlockNamespaceTest()
    {
        await VerifySnippetAsync("""
            namespace Namespace
            {
                $$
            }
            """, """
            namespace Namespace
            {
                struct {|0:MyStruct|}
                {
                    $$
                }
            }
            """);
    }

    [Fact]
    public async Task InsertStructSnippetInFileScopedNamespaceTest()
    {
        await VerifySnippetAsync("""
            namespace Namespace;

            $$
            """, """
            namespace Namespace;

            struct {|0:MyStruct|}
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task InsertStructSnippetTest()
    {
        await VerifySnippetAsync("""
            $$
            """, """
            struct {|0:MyStruct|}
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task InsertStructTopLevelSnippetTest()
    {
        await VerifySnippetAsync("""
            System.Console.WriteLine();
            $$
            """, """
            System.Console.WriteLine();
            struct {|0:MyStruct|}
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task InsertStructSnippetInClassTest()
    {
        await VerifySnippetAsync("""
            class MyClass
            {
                $$
            }
            """, """
            class MyClass
            {
                struct {|0:MyStruct|}
                {
                    $$
                }
            }
            """);
    }

    [Fact]
    public async Task InsertStructSnippetInRecordTest()
    {
        await VerifySnippetAsync("""
            record MyRecord
            {
                $$
            }
            """, """
            record MyRecord
            {
                struct {|0:MyStruct|}
                {
                    $$
                }
            }
            """);
    }

    [Fact]
    public async Task InsertStructSnippetInStructTest()
    {
        await VerifySnippetAsync("""
            struct MyStruct
            {
                $$
            }
            """, """
            struct MyStruct
            {
                struct {|0:MyStruct1|}
                {
                    $$
                }
            }
            """);
    }

    [Fact]
    public async Task InsertStructSnippetInInterfaceTest()
    {
        await VerifySnippetAsync("""
            interface MyInterface
            {
                $$
            }
            """, """
            interface MyInterface
            {
                struct {|0:MyStruct|}
                {
                    $$
                }
            }
            """);
    }

    [Fact]
    public async Task InsertStructSnippetWithModifiersTest()
    {
        await VerifySnippetAsync("""
            $$
            """, """
            public struct {|0:MyStruct|}
            {
                $$
            }
            """,
            editorconfig: """
            root = true
             
            [*]
            dotnet_style_require_accessibility_modifiers = always
            """);
    }

    [Fact]
    public async Task NoStructSnippetInEnumTest()
    {
        await VerifySnippetIsAbsentAsync("""
            enum MyEnum
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task NoStructSnippetInMethodTest()
    {
        await VerifySnippetIsAbsentAsync("""
            struct Program
            {
                public void Method()
                {
                    $$
                }
            }
            """);
    }

    [Fact]
    public async Task NoStructSnippetInConstructorTest()
    {
        await VerifySnippetIsAbsentAsync("""
            struct Program
            {
                public Program()
                {
                    $$
                }
            }
            """);
    }

    [Theory]
    [MemberData(nameof(CommonSnippetTestData.AllAccessibilityModifiers), MemberType = typeof(CommonSnippetTestData))]
    public async Task InsertStructSnippetAfterAccessibilityModifier(string modifier)
    {
        await VerifySnippetAsync($"""
            {modifier} $$
            """, $$"""
            {{modifier}} struct {|0:MyStruct|}
            {
                $$
            }
            """);
    }

    [Theory]
    [MemberData(nameof(CommonSnippetTestData.AllAccessibilityModifiers), MemberType = typeof(CommonSnippetTestData))]
    public async Task InsertStructSnippetAfterAccessibilityModifier_RequireAccessibilityModifiers(string modifier)
    {
        await VerifySnippetAsync($"""
            {modifier} $$
            """, $$"""
            {{modifier}} struct {|0:MyStruct|}
            {
                $$
            }
            """,
            editorconfig: """
            root = true
            
            [*]
            dotnet_style_require_accessibility_modifiers = always
            """);
    }

    [Theory]
    [InlineData("ref")]
    [InlineData("readonly")]
    [InlineData("unsafe")]
    public async Task InsertStructSnippetAfterValidModifiersTest(string modifier)
    {
        await VerifySnippetAsync($"""
            {modifier} $$
            """, $$"""
            {{modifier}} struct {|0:MyStruct|}
            {
                $$
            }
            """);
    }

    [Theory]
    [InlineData("abstract")]
    [InlineData("sealed")]
    [InlineData("static")]
    public async Task NoStructSnippetAfterInvalidModifiersTest(string modifier)
    {
        await VerifySnippetIsAbsentAsync($"""
            {modifier} $$
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/69600")]
    [MemberData(nameof(CommonSnippetTestData.AllAccessibilityModifiers), MemberType = typeof(CommonSnippetTestData))]
    public async Task NoAdditionalAccessibilityModifiersIfAfterPartialKeywordTest(string modifier)
    {
        await VerifySnippetAsync($"""
            {modifier} partial $$
            """, $$"""
            {{modifier}} partial struct {|0:MyStruct|}
            {
                $$
            }
            """,
            editorconfig: """
            root = true
            
            [*]
            dotnet_style_require_accessibility_modifiers = always
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69600")]
    public async Task EnsureCorrectModifierOrderAfterPartialKeywordTest()
    {
        await VerifySnippetAsync("""
            partial $$
            """, """
            public partial struct {|0:MyStruct|}
            {
                $$
            }
            """,
            editorconfig: """
            root = true
            
            [*]
            dotnet_style_require_accessibility_modifiers = always
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69600")]
    public async Task EnsureCorrectModifierOrderAfterPartialKeywordTest_InvalidPreferredModifiersList()
    {
        await VerifySnippetAsync("""
            partial $$
            """, """
            public partial struct {|0:MyStruct|}
            {
                $$
            }
            """,
            editorconfig: """
            root = true

            [*]
            dotnet_style_require_accessibility_modifiers = always
            
            csharp_preferred_modifier_order = invalid!
            """);
    }

    [Fact]
    public async Task EnsureCorrectModifierOrderFromOptionsTest_PublicModifierBeforeAllOthers()
    {
        await VerifySnippetAsync("""
            readonly ref $$
            """, """
            public readonly ref struct {|0:MyStruct|}
            {
                $$
            }
            """,
            editorconfig: """
            root = true

            [*]
            dotnet_style_require_accessibility_modifiers = always
            
            csharp_preferred_modifier_order = public,readonly,ref
            """);
    }

    [Fact]
    public async Task EnsureCorrectModifierOrderFromOptionsTest_PublicModifierBeforeAllOthers_NotAllModifiersInTheList()
    {
        await VerifySnippetAsync("""
            readonly ref $$
            """, """
            public readonly ref struct {|0:MyStruct|}
            {
                $$
            }
            """,
            editorconfig: """
            root = true

            [*]
            dotnet_style_require_accessibility_modifiers = always
            
            csharp_preferred_modifier_order = public,readonly
            """);
    }

    [Fact]
    public async Task EnsureCorrectModifierOrderFromOptionsTest_PublicModifierBetweenOthers()
    {
        await VerifySnippetAsync("""
            readonly ref $$
            """, """
            readonly public ref struct {|0:MyStruct|}
            {
                $$
            }
            """,
            editorconfig: """
            root = true

            [*]
            dotnet_style_require_accessibility_modifiers = always
            
            csharp_preferred_modifier_order = readonly,public,ref
            """);
    }

    [Fact]
    public async Task EnsureCorrectModifierOrderFromOptionsTest_PublicModifierAfterAllOthers()
    {
        await VerifySnippetAsync("""
            readonly ref $$
            """, """
            readonly ref public struct {|0:MyStruct|}
            {
                $$
            }
            """,
            editorconfig: """
            root = true

            [*]
            dotnet_style_require_accessibility_modifiers = always
            
            csharp_preferred_modifier_order = readonly,ref,public
            """);
    }
}
