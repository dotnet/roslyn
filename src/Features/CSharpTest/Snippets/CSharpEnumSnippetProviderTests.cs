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
    public Task InsertEnumSnippetInBlockNamespaceTest()
        => VerifySnippetAsync("""
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

    [Fact]
    public Task InsertEnumSnippetInFileScopedNamespaceTest()
        => VerifySnippetAsync("""
            namespace Namespace;
            
            $$
            """, """
            namespace Namespace;
            
            enum {|0:MyEnum|}
            {
                $$
            }
            """);

    [Fact]
    public Task InsertEnumSnippetTest()
        => VerifySnippetAsync("""
            $$
            """, """
            enum {|0:MyEnum|}
            {
                $$
            }
            """);

    [Fact]
    public Task InsertEnumTopLevelSnippetTest()
        => VerifySnippetAsync("""
            System.Console.WriteLine();
            $$
            """, """
            System.Console.WriteLine();
            enum {|0:MyEnum|}
            {
                $$
            }
            """);

    [Fact]
    public Task InsertEnumSnippetInClassTest()
        => VerifySnippetAsync("""
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

    [Fact]
    public Task InsertEnumSnippetInRecordTest()
        => VerifySnippetAsync("""
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

    [Fact]
    public Task InsertEnumSnippetInStructTest()
        => VerifySnippetAsync("""
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

    [Fact]
    public Task InsertEnumSnippetInInterfaceTest()
        => VerifySnippetAsync("""
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

    [Fact]
    public Task InsertEnumSnippetWithModifiersTest()
        => VerifySnippetAsync("""
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

    [Fact]
    public Task NoEnumSnippetInEnumTest()
        => VerifySnippetIsAbsentAsync("""
            enum MyEnum
            {
                $$
            }
            """);

    [Fact]
    public Task NoEnumSnippetInMethodTest()
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
    public Task NoEnumSnippetInConstructorTest()
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
    public Task InsertEnumSnippetAfterAccessibilityModifier(string modifier)
        => VerifySnippetAsync($"""
            {modifier} $$
            """, $$"""
            {{modifier}} enum {|0:MyEnum|}
            {
                $$
            }
            """);

    [Theory]
    [MemberData(nameof(CommonSnippetTestData.AllAccessibilityModifiers), MemberType = typeof(CommonSnippetTestData))]
    public Task InsertEnumSnippetAfterAccessibilityModifier_RequireAccessibilityModifiers(string modifier)
        => VerifySnippetAsync($"""
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

    [Theory]
    [InlineData("abstract")]
    [InlineData("partial")]
    [InlineData("sealed")]
    [InlineData("static")]
    [InlineData("ref")]
    [InlineData("readonly")]
    [InlineData("unsafe")]
    public Task NoEnumSnippetAfterInvalidModifiersTest(string modifier)
        => VerifySnippetIsAbsentAsync($"""
            {modifier} $$
            """);
}
