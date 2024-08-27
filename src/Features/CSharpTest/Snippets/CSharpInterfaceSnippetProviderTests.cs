// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Snippets;

[Trait(Traits.Feature, Traits.Features.Snippets)]
public sealed class CSharpInterfaceSnippetProviderTests : AbstractCSharpSnippetProviderTests
{
    protected override string SnippetIdentifier => "interface";

    [Fact]
    public async Task InsertInterfaceSnippetInBlockNamespaceTest()
    {
        await VerifySnippetAsync("""
            namespace Namespace
            {
                $$
            }
            """, """
            namespace Namespace
            {
                interface {|0:MyInterface|}
                {
                    $$
                }
            }
            """);
    }

    [Fact]
    public async Task InsertInterfaceSnippetInFileScopedNamespaceTest()
    {
        await VerifySnippetAsync("""
            namespace Namespace;

            $$
            """, """
            namespace Namespace;

            interface {|0:MyInterface|}
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task InsertInterfaceSnippetTest()
    {
        await VerifySnippetAsync("""
            $$
            """, """
            interface {|0:MyInterface|}
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task InsertInterfaceTopLevelSnippetTest()
    {
        await VerifySnippetAsync("""
            System.Console.WriteLine();
            $$
            """, """
            System.Console.WriteLine();
            interface {|0:MyInterface|}
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task InsertInterfaceSnippetInClassTest()
    {
        await VerifySnippetAsync("""
            class MyClass
            {
                $$
            }
            """, """
            class MyClass
            {
                interface {|0:MyInterface|}
                {
                    $$
                }
            }
            """);
    }

    [Fact]
    public async Task InsertInterfaceSnippetInRecordTest()
    {
        await VerifySnippetAsync("""
            record MyRecord
            {
                $$
            }
            """, """
            record MyRecord
            {
                interface {|0:MyInterface|}
                {
                    $$
                }
            }
            """);
    }

    [Fact]
    public async Task InsertInterfaceSnippetInStructTest()
    {
        await VerifySnippetAsync("""
            struct MyStruct
            {
                $$
            }
            """, """
            struct MyStruct
            {
                interface {|0:MyInterface|}
                {
                    $$
                }
            }
            """);
    }

    [Fact]
    public async Task InsertInterfaceSnippetInInterfaceTest()
    {
        await VerifySnippetAsync("""
            interface MyInterface
            {
                $$
            }
            """, """
            interface MyInterface
            {
                interface {|0:MyInterface1|}
                {
                    $$
                }
            }
            """);
    }

    [Fact]
    public async Task InsertInterfaceSnippetWithModifiersTest()
    {
        await VerifySnippetAsync("""
            $$
            """, """
            public interface {|0:MyInterface|}
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
    public async Task NoInterfaceSnippetInEnumTest()
    {
        await VerifySnippetIsAbsentAsync("""
            enum MyEnum
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task NoInterfaceSnippetInMethodTest()
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
    public async Task NoInterfaceSnippetInConstructorTest()
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
    public async Task InsertInterfaceSnippetAfterAccessibilityModifier(string modifier)
    {
        await VerifySnippetAsync($"""
            {modifier} $$
            """, $$"""
            {{modifier}} interface {|0:MyInterface|}
            {
                $$
            }
            """);
    }

    [Theory]
    [MemberData(nameof(CommonSnippetTestData.AllAccessibilityModifiers), MemberType = typeof(CommonSnippetTestData))]
    public async Task InsertInterfaceSnippetAfterAccessibilityModifier_RequireAccessibilityModifiers(string modifier)
    {
        await VerifySnippetAsync($"""
            {modifier} $$
            """, $$"""
            {{modifier}} interface {|0:MyInterface|}
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
    [InlineData("unsafe")]
    public async Task InsertInterfaceSnippetAfterValidModifiersTest(string modifier)
    {
        await VerifySnippetAsync($"""
            {modifier} $$
            """, $$"""
            {{modifier}} interface {|0:MyInterface|}
            {
                $$
            }
            """);
    }

    [Theory]
    [InlineData("abstract")]
    [InlineData("sealed")]
    [InlineData("static")]
    [InlineData("ref")]
    [InlineData("readonly")]
    public async Task NoInterfaceSnippetAfterInvalidModifiersTest(string modifier)
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
            {{modifier}} partial interface {|0:MyInterface|}
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
            public partial interface {|0:MyInterface|}
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
}
