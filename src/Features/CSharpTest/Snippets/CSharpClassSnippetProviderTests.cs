// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Snippets;

[Trait(Traits.Feature, Traits.Features.Snippets)]
public sealed class CSharpClassSnippetProviderTests : AbstractCSharpSnippetProviderTests
{
    protected override string SnippetIdentifier => "class";

    [Fact]
    public Task InsertClassSnippetInBlockNamespaceTest()
        => VerifySnippetAsync("""
            namespace Namespace
            {
                $$
            }
            """, """
            namespace Namespace
            {
                class {|0:MyClass|}
                {
                    $$
                }
            }
            """);

    [Fact]
    public Task InsertClassSnippetInFileScopedNamespaceTest()
        => VerifySnippetAsync("""
            namespace Namespace;

            $$
            """, """
            namespace Namespace;

            class {|0:MyClass|}
            {
                $$
            }
            """);

    [Fact]
    public Task InsertClassSnippetTest()
        => VerifySnippetAsync("""
            $$
            """, """
            class {|0:MyClass|}
            {
                $$
            }
            """);

    [Fact]
    public Task InsertClassTopLevelSnippetTest()
        => VerifySnippetAsync("""
            System.Console.WriteLine();
            $$
            """, """
            System.Console.WriteLine();
            class {|0:MyClass|}
            {
                $$
            }
            """);

    [Fact]
    public Task InsertClassSnippetInClassTest()
        => VerifySnippetAsync("""
            class MyClass
            {
                $$
            }
            """, """
            class MyClass
            {
                class {|0:MyClass1|}
                {
                    $$
                }
            }
            """);

    [Fact]
    public Task InsertClassSnippetInRecordTest()
        => VerifySnippetAsync("""
            record MyRecord
            {
                $$
            }
            """, """
            record MyRecord
            {
                class {|0:MyClass|}
                {
                    $$
                }
            }
            """);

    [Fact]
    public Task InsertClassSnippetInStructTest()
        => VerifySnippetAsync("""
            struct MyStruct
            {
                $$
            }
            """, """
            struct MyStruct
            {
                class {|0:MyClass|}
                {
                    $$
                }
            }
            """);

    [Fact]
    public Task InsertClassSnippetInInterfaceTest()
        => VerifySnippetAsync("""
            interface MyInterface
            {
                $$
            }
            """, """
            interface MyInterface
            {
                class {|0:MyClass|}
                {
                    $$
                }
            }
            """);

    [Fact]
    public Task InsertClassSnippetWithModifiersTest()
        => VerifySnippetAsync("""
            $$
            """, """
            public class {|0:MyClass|}
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
    public Task NoClassSnippetInEnumTest()
        => VerifySnippetIsAbsentAsync("""
            enum MyEnum
            {
                $$
            }
            """);

    [Fact]
    public Task NoClassSnippetInMethodTest()
        => VerifySnippetIsAbsentAsync("""
            class Program
            {
                public void Method()
                {
                    $$
                }
            }
            """);

    [Fact]
    public Task NoClassSnippetInConstructorTest()
        => VerifySnippetIsAbsentAsync("""
            class Program
            {
                public Program()
                {
                    $$
                }
            }
            """);

    [Theory]
    [MemberData(nameof(CommonSnippetTestData.AllAccessibilityModifiers), MemberType = typeof(CommonSnippetTestData))]
    public Task InsertClassSnippetAfterAccessibilityModifier(string modifier)
        => VerifySnippetAsync($"""
            {modifier} $$
            """, $$"""
            {{modifier}} class {|0:MyClass|}
            {
                $$
            }
            """);

    [Theory]
    [MemberData(nameof(CommonSnippetTestData.AllAccessibilityModifiers), MemberType = typeof(CommonSnippetTestData))]
    public Task InsertClassSnippetAfterAccessibilityModifier_RequireAccessibilityModifiers(string modifier)
        => VerifySnippetAsync($"""
            {modifier} $$
            """, $$"""
            {{modifier}} class {|0:MyClass|}
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
    [InlineData("abstract")]
    [InlineData("sealed")]
    [InlineData("static")]
    [InlineData("unsafe")]
    public Task InsertClassSnippetAfterValidModifiersTest(string modifier)
        => VerifySnippetAsync($"""
            {modifier} $$
            """, $$"""
            {{modifier}} class {|0:MyClass|}
            {
                $$
            }
            """);

    [Theory]
    [InlineData("ref")]
    [InlineData("readonly")]
    public Task NoClassSnippetAfterInvalidModifiersTest(string modifier)
        => VerifySnippetIsAbsentAsync($"""
            {modifier} $$
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/69600")]
    [MemberData(nameof(CommonSnippetTestData.AllAccessibilityModifiers), MemberType = typeof(CommonSnippetTestData))]
    public Task NoAdditionalAccessibilityModifiersIfAfterPartialKeywordTest(string modifier)
        => VerifySnippetAsync($"""
            {modifier} partial $$
            """, $$"""
            {{modifier}} partial class {|0:MyClass|}
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
            public partial class {|0:MyClass|}
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
            public partial class {|0:MyClass|}
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
            sealed unsafe $$
            """, """
            public sealed unsafe class {|0:MyClass|}
            {
                $$
            }
            """,
            editorconfig: """
            root = true

            [*]
            dotnet_style_require_accessibility_modifiers = always
            
            csharp_preferred_modifier_order = public,sealed,unsafe
            """);

    [Fact]
    public Task EnsureCorrectModifierOrderFromOptionsTest_PublicModifierBeforeAllOthers_NotAllModifiersInTheList()
        => VerifySnippetAsync("""
            sealed unsafe $$
            """, """
            public sealed unsafe class {|0:MyClass|}
            {
                $$
            }
            """,
            editorconfig: """
            root = true

            [*]
            dotnet_style_require_accessibility_modifiers = always
            
            csharp_preferred_modifier_order = public,sealed
            """);

    [Fact]
    public Task EnsureCorrectModifierOrderFromOptionsTest_PublicModifierBetweenOthers()
        => VerifySnippetAsync("""
            sealed unsafe $$
            """, """
            sealed public unsafe class {|0:MyClass|}
            {
                $$
            }
            """,
            editorconfig: """
            root = true

            [*]
            dotnet_style_require_accessibility_modifiers = always
            
            csharp_preferred_modifier_order = sealed,public,unsafe
            """);

    [Fact]
    public Task EnsureCorrectModifierOrderFromOptionsTest_PublicModifierAfterAllOthers()
        => VerifySnippetAsync("""
            sealed unsafe $$
            """, """
            sealed unsafe public class {|0:MyClass|}
            {
                $$
            }
            """,
            editorconfig: """
            root = true

            [*]
            dotnet_style_require_accessibility_modifiers = always
            
            csharp_preferred_modifier_order = sealed,unsafe,public
            """);
}
