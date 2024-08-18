// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Snippets;

[Trait(Traits.Feature, Traits.Features.Snippets)]
public sealed class CSharpEnumSnippetProviderTests : AbstractCSharpSnippetProviderTests
{
    protected override string SnippetIdentifier => "enum";

    [Fact]
    public async Task InsertEnumSnippetInBlockNamespaceTest()
    {
        await VerifySnippetAsync("""
            namespace Namespace
            {
                $$
            }
            """, """
            namespace Namespace
            {
                enum {|0:MyEnum|}
                {
                    $$
                }
            }
            """);
    }

    [Fact]
    public async Task InsertEnumSnippetInFileScopedNamespaceTest()
    {
        await VerifySnippetAsync("""
            namespace Namespace;
            
            $$
            """, """
            namespace Namespace;
            
            enum {|0:MyEnum|}
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task InsertEnumSnippetTest()
    {
        await VerifySnippetAsync("""
            $$
            """, """
            enum {|0:MyEnum|}
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task InsertEnumTopLevelSnippetTest()
    {
        await VerifySnippetAsync("""
            System.Console.WriteLine();
            $$
            """, """
            System.Console.WriteLine();
            enum {|0:MyEnum|}
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task InsertEnumSnippetInClassTest()
    {
        await VerifySnippetAsync("""
            class MyClass
            {
                $$
            }
            """, """
            class MyClass
            {
                enum {|0:MyEnum|}
                {
                    $$
                }
            }
            """);
    }

    [Fact]
    public async Task InsertEnumSnippetInRecordTest()
    {
        await VerifySnippetAsync("""
            record MyRecord
            {
                $$
            }
            """, """
            record MyRecord
            {
                enum {|0:MyEnum|}
                {
                    $$
                }
            }
            """);
    }

    [Fact]
    public async Task InsertEnumSnippetInStructTest()
    {
        await VerifySnippetAsync("""
            struct MyStruct
            {
                $$
            }
            """, """
            struct MyStruct
            {
                enum {|0:MyEnum|}
                {
                    $$
                }
            }
            """);
    }

    [Fact]
    public async Task InsertEnumSnippetInInterfaceTest()
    {
        await VerifySnippetAsync("""
            interface MyInterface
            {
                $$
            }
            """, """
            interface MyInterface
            {
                enum {|0:MyEnum|}
                {
                    $$
                }
            }
            """);
    }

    [Fact]
    public async Task InsertEnumSnippetWithModifiersTest()
    {
        await VerifySnippetAsync("""
            $$
            """, """
            public enum {|0:MyEnum|}
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
    public async Task NoEnumSnippetInEnumTest()
    {
        await VerifySnippetIsAbsentAsync("""
            enum MyEnum
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task NoEnumSnippetInMethodTest()
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
    public async Task NoEnumSnippetInConstructorTest()
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
    public async Task InsertEnumSnippetAfterAccessibilityModifier(string modifier)
    {
        await VerifySnippetAsync($"""
            {modifier} $$
            """, $$"""
            {{modifier}} enum {|0:MyEnum|}
            {
                $$
            }
            """);
    }

    [Theory]
    [MemberData(nameof(CommonSnippetTestData.AllAccessibilityModifiers), MemberType = typeof(CommonSnippetTestData))]
    public async Task InsertEnumSnippetAfterAccessibilityModifier_RequireAccessibilityModifiers(string modifier)
    {
        await VerifySnippetAsync($"""
            {modifier} $$
            """, $$"""
            {{modifier}} enum {|0:MyEnum|}
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
    //[InlineData("partial")]
    [InlineData("sealed")]
    [InlineData("static")]
    [InlineData("ref")]
    [InlineData("readonly")]
    [InlineData("unsafe")]
    public async Task NoEnumSnippetAfterInvalidModifiersTest(string modifier)
    {
        await VerifySnippetIsAbsentAsync($"""
            {modifier} $$
            """);
    }
}
