// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Snippets;

[Trait(Traits.Feature, Traits.Features.Snippets)]
public sealed class CSharpIntMainSnippetProviderTests : AbstractCSharpSnippetProviderTests
{
    protected override string SnippetIdentifier => "sim";

    [Fact]
    public Task TestMissingInBlockNamespace()
        => VerifySnippetIsAbsentAsync("""
            namespace Test
            {
                $$
            }
            """);

    [Fact]
    public Task TestMissingInFileScopedNamespace()
        => VerifySnippetIsAbsentAsync("""
            namespace Test;
            
            $$
            """);

    [Fact]
    public Task TestMissingInTopLevelContext()
        => VerifySnippetIsAbsentAsync("""
            System.Console.WriteLine();
            $$
            """);

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("interface")]
    [InlineData("record")]
    [InlineData("record class")]
    [InlineData("record struct")]
    public Task TestInsertSnippetInType(string type)
        => VerifySnippetAsync($$"""
            {{type}} Program
            {
                $$
            }
            """, $$"""
            {{type}} Program
            {
                static int Main(string[] args)
                {
                    $$
                    return 0;
                }
            }
            """);

    [Fact]
    public Task TestMissingInEnum()
        => VerifySnippetIsAbsentAsync("""
            enum MyEnum
            {
                $$
            }
            """);

    [Fact]
    public Task TestMissingInMethod()
        => VerifySnippetIsAbsentAsync("""
            class Program
            {
                void M()
                {
                    $$
                }
            }
            """);

    [Fact]
    public Task TestMissingInConstructor()
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
    public Task TestInsertSnippetAfterAccessibilityModifier(string modifier)
        => VerifySnippetAsync($$"""
            class Program
            {
                {{modifier}} $$
            }
            """, $$"""
            class Program
            {
                {{modifier}} static int Main(string[] args)
                {
                    $$
                    return 0;
                }
            }
            """);

    [Theory]
    [InlineData("static")]
    [InlineData("virtual")]
    [InlineData("abstract")]
    [InlineData("override")]
    [InlineData("file")]
    public Task TestMissingAfterIncorrectModifiers(string modifier)
        => VerifySnippetIsAbsentAsync($$"""
            class Program
            {
                {{modifier}} $$
            }
            """);

    [Fact]
    public Task TestMissingIfAnotherMemberWithNameMainExists()
        => VerifySnippetIsAbsentAsync("""
            class Program
            {
                public int Main => 0;

                $$
            }
            """);

    [Fact]
    public Task TestMissingIfTopLevelStatementsArePresent()
        => VerifySnippetIsAbsentAsync("""
            System.Console.WriteLine();
            
            class Program
            {
                $$
            }
            """);
}
