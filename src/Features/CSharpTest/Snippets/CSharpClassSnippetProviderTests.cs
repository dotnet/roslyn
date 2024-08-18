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
    public async Task InsertClassSnippetInBlockNamespaceTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertClassSnippetInFileScopedNamespaceTest()
    {
        await VerifySnippetAsync("""
            namespace Namespace;

            $$
            """, """
            namespace Namespace;

            class {|0:MyClass|}
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task InsertClassSnippetTest()
    {
        await VerifySnippetAsync("""
            $$
            """, """
            class {|0:MyClass|}
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task InsertClassTopLevelSnippetTest()
    {
        await VerifySnippetAsync("""
            System.Console.WriteLine();
            $$
            """, """
            System.Console.WriteLine();
            class {|0:MyClass|}
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task InsertClassSnippetInClassTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertClassSnippetInRecordTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertClassSnippetInStructTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertClassSnippetInInterfaceTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertClassSnippetWithModifiersTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task NoClassSnippetInEnumTest()
    {
        await VerifySnippetIsAbsentAsync("""
            enum MyEnum
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task NoClassSnippetInMethodTest()
    {
        await VerifySnippetIsAbsentAsync("""
            class Program
            {
                public void Method()
                {
                    $$
                }
            }
            """);
    }

    [Fact]
    public async Task NoClassSnippetInConstructorTest()
    {
        await VerifySnippetIsAbsentAsync("""
            class Program
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
    public async Task InsertClassSnippetAfterAccessibilityModifier(string modifier)
    {
        await VerifySnippetAsync($"""
            {modifier} $$
            """, $$"""
            {{modifier}} class {|0:MyClass|}
            {
                $$
            }
            """);
    }

    [Theory]
    [MemberData(nameof(CommonSnippetTestData.AllAccessibilityModifiers), MemberType = typeof(CommonSnippetTestData))]
    public async Task InsertClassSnippetAfterAccessibilityModifier_RequireAccessibilityModifiers(string modifier)
    {
        await VerifySnippetAsync($$"""
            {{modifier}} $$
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
    }

    [Theory]
    [InlineData("abstract")]
    [InlineData("sealed")]
    [InlineData("static")]
    [InlineData("unsafe")]
    public async Task InsertClassSnippetAfterValidModifiersTest(string modifier)
    {
        await VerifySnippetAsync($"""
            {modifier} $$
            """, $$"""
            {{modifier}} class {|0:MyClass|}
            {
                $$
            }
            """);
    }

    [Theory]
    [InlineData("ref")]
    [InlineData("readonly")]
    public async Task NoClassSnippetAfterInvalidModifiersTest(string modifier)
    {
        await VerifySnippetIsAbsentAsync($"""
            {modifier} $$
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/69600")]
    [MemberData(nameof(CommonSnippetTestData.AllAccessibilityModifiers), MemberType = typeof(CommonSnippetTestData))]
    public async Task NoAdditionalAccessibilityModifiersIfAfterPartialKeywordTest(string modifier)
    {
        await VerifySnippetAsync($$"""
            {{modifier}} partial $$
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69600")]
    public async Task EnsureCorrectModifierOrderAfterPartialKeywordTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69600")]
    public async Task EnsureCorrectModifierOrderAfterPartialKeywordTest_InvalidPreferredModifiersList()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task EnsureCorrectModifierOrderFromOptionsTest_PublicModifierBeforeAllOthers()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task EnsureCorrectModifierOrderFromOptionsTest_PublicModifierBeforeAllOthers_NotAllModifiersInTheList()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task EnsureCorrectModifierOrderFromOptionsTest_PublicModifierBetweenOthers()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task EnsureCorrectModifierOrderFromOptionsTest_PublicModifierAfterAllOthers()
    {
        await VerifySnippetAsync("""
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
}
