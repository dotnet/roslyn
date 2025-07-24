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
    public Task InsertInterfaceSnippetInBlockNamespaceTest()
        => VerifySnippetAsync("""
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

    [Fact]
    public Task InsertInterfaceSnippetInFileScopedNamespaceTest()
        => VerifySnippetAsync("""
            namespace Namespace;

            $$
            """, """
            namespace Namespace;

            interface {|0:MyInterface|}
            {
                $$
            }
            """);

    [Fact]
    public Task InsertInterfaceSnippetTest()
        => VerifySnippetAsync("""
            $$
            """, """
            interface {|0:MyInterface|}
            {
                $$
            }
            """);

    [Fact]
    public Task InsertInterfaceTopLevelSnippetTest()
        => VerifySnippetAsync("""
            System.Console.WriteLine();
            $$
            """, """
            System.Console.WriteLine();
            interface {|0:MyInterface|}
            {
                $$
            }
            """);

    [Fact]
    public Task InsertInterfaceSnippetInClassTest()
        => VerifySnippetAsync("""
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

    [Fact]
    public Task InsertInterfaceSnippetInRecordTest()
        => VerifySnippetAsync("""
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

    [Fact]
    public Task InsertInterfaceSnippetInStructTest()
        => VerifySnippetAsync("""
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

    [Fact]
    public Task InsertInterfaceSnippetInInterfaceTest()
        => VerifySnippetAsync("""
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

    [Fact]
    public Task InsertInterfaceSnippetWithModifiersTest()
        => VerifySnippetAsync("""
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

    [Fact]
    public Task NoInterfaceSnippetInEnumTest()
        => VerifySnippetIsAbsentAsync("""
            enum MyEnum
            {
                $$
            }
            """);

    [Fact]
    public Task NoInterfaceSnippetInMethodTest()
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
    public Task NoInterfaceSnippetInConstructorTest()
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
    public Task InsertInterfaceSnippetAfterAccessibilityModifier(string modifier)
        => VerifySnippetAsync($"""
            {modifier} $$
            """, $$"""
            {{modifier}} interface {|0:MyInterface|}
            {
                $$
            }
            """);

    [Theory]
    [MemberData(nameof(CommonSnippetTestData.AllAccessibilityModifiers), MemberType = typeof(CommonSnippetTestData))]
    public Task InsertInterfaceSnippetAfterAccessibilityModifier_RequireAccessibilityModifiers(string modifier)
        => VerifySnippetAsync($"""
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

    [Theory]
    [InlineData("unsafe")]
    public Task InsertInterfaceSnippetAfterValidModifiersTest(string modifier)
        => VerifySnippetAsync($"""
            {modifier} $$
            """, $$"""
            {{modifier}} interface {|0:MyInterface|}
            {
                $$
            }
            """);

    [Theory]
    [InlineData("abstract")]
    [InlineData("sealed")]
    [InlineData("static")]
    [InlineData("ref")]
    [InlineData("readonly")]
    public Task NoInterfaceSnippetAfterInvalidModifiersTest(string modifier)
        => VerifySnippetIsAbsentAsync($"""
            {modifier} $$
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/69600")]
    [MemberData(nameof(CommonSnippetTestData.AllAccessibilityModifiers), MemberType = typeof(CommonSnippetTestData))]
    public Task NoAdditionalAccessibilityModifiersIfAfterPartialKeywordTest(string modifier)
        => VerifySnippetAsync($"""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69600")]
    public Task EnsureCorrectModifierOrderAfterPartialKeywordTest()
        => VerifySnippetAsync("""
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
