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
    public Task InsertStructSnippetInBlockNamespaceTest()
        => VerifySnippetAsync("""
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

    [Fact]
    public Task InsertStructSnippetInFileScopedNamespaceTest()
        => VerifySnippetAsync("""
            namespace Namespace;

            $$
            """, """
            namespace Namespace;

            struct {|0:MyStruct|}
            {
                $$
            }
            """);

    [Fact]
    public Task InsertStructSnippetTest()
        => VerifySnippetAsync("""
            $$
            """, """
            struct {|0:MyStruct|}
            {
                $$
            }
            """);

    [Fact]
    public Task InsertStructTopLevelSnippetTest()
        => VerifySnippetAsync("""
            System.Console.WriteLine();
            $$
            """, """
            System.Console.WriteLine();
            struct {|0:MyStruct|}
            {
                $$
            }
            """);

    [Fact]
    public Task InsertStructSnippetInClassTest()
        => VerifySnippetAsync("""
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

    [Fact]
    public Task InsertStructSnippetInRecordTest()
        => VerifySnippetAsync("""
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

    [Fact]
    public Task InsertStructSnippetInStructTest()
        => VerifySnippetAsync("""
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

    [Fact]
    public Task InsertStructSnippetInInterfaceTest()
        => VerifySnippetAsync("""
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

    [Fact]
    public Task InsertStructSnippetWithModifiersTest()
        => VerifySnippetAsync("""
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

    [Fact]
    public Task NoStructSnippetInEnumTest()
        => VerifySnippetIsAbsentAsync("""
            enum MyEnum
            {
                $$
            }
            """);

    [Fact]
    public Task NoStructSnippetInMethodTest()
        => VerifySnippetIsAbsentAsync("""
            struct Program
            {
                public void Method()
                {
                    $$
                }
            }
            """);

    [Fact]
    public Task NoStructSnippetInConstructorTest()
        => VerifySnippetIsAbsentAsync("""
            struct Program
            {
                public Program()
                {
                    $$
                }
            }
            """);

    [Theory]
    [MemberData(nameof(CommonSnippetTestData.AllAccessibilityModifiers), MemberType = typeof(CommonSnippetTestData))]
    public Task InsertStructSnippetAfterAccessibilityModifier(string modifier)
        => VerifySnippetAsync($"""
            {modifier} $$
            """, $$"""
            {{modifier}} struct {|0:MyStruct|}
            {
                $$
            }
            """);

    [Theory]
    [MemberData(nameof(CommonSnippetTestData.AllAccessibilityModifiers), MemberType = typeof(CommonSnippetTestData))]
    public Task InsertStructSnippetAfterAccessibilityModifier_RequireAccessibilityModifiers(string modifier)
        => VerifySnippetAsync($"""
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

    [Theory]
    [InlineData("ref")]
    [InlineData("readonly")]
    [InlineData("unsafe")]
    public Task InsertStructSnippetAfterValidModifiersTest(string modifier)
        => VerifySnippetAsync($"""
            {modifier} $$
            """, $$"""
            {{modifier}} struct {|0:MyStruct|}
            {
                $$
            }
            """);

    [Theory]
    [InlineData("abstract")]
    [InlineData("sealed")]
    [InlineData("static")]
    public Task NoStructSnippetAfterInvalidModifiersTest(string modifier)
        => VerifySnippetIsAbsentAsync($"""
            {modifier} $$
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/69600")]
    [MemberData(nameof(CommonSnippetTestData.AllAccessibilityModifiers), MemberType = typeof(CommonSnippetTestData))]
    public Task NoAdditionalAccessibilityModifiersIfAfterPartialKeywordTest(string modifier)
        => VerifySnippetAsync($"""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69600")]
    public Task EnsureCorrectModifierOrderAfterPartialKeywordTest()
        => VerifySnippetAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69600")]
    public Task EnsureCorrectModifierOrderAfterPartialKeywordTest_InvalidPreferredModifiersList()
        => VerifySnippetAsync("""
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

    [Fact]
    public Task EnsureCorrectModifierOrderFromOptionsTest_PublicModifierBeforeAllOthers()
        => VerifySnippetAsync("""
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

    [Fact]
    public Task EnsureCorrectModifierOrderFromOptionsTest_PublicModifierBeforeAllOthers_NotAllModifiersInTheList()
        => VerifySnippetAsync("""
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

    [Fact]
    public Task EnsureCorrectModifierOrderFromOptionsTest_PublicModifierBetweenOthers()
        => VerifySnippetAsync("""
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

    [Fact]
    public Task EnsureCorrectModifierOrderFromOptionsTest_PublicModifierAfterAllOthers()
        => VerifySnippetAsync("""
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
