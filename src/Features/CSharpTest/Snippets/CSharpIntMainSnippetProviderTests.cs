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
    public async Task TestMissingInBlockNamespace()
    {
        await VerifySnippetIsAbsentAsync("""
            namespace Test
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task TestMissingInFileScopedNamespace()
    {
        await VerifySnippetIsAbsentAsync("""
            namespace Test;
            
            $$
            """);
    }

    [Fact]
    public async Task TestMissingInTopLevelContext()
    {
        await VerifySnippetIsAbsentAsync("""
            System.Console.WriteLine();
            $$
            """);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("interface")]
    [InlineData("record")]
    [InlineData("record class")]
    [InlineData("record struct")]
    public async Task TestInsertSnippetInType(string type)
    {
        await VerifySnippetAsync($$"""
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
    }

    [Fact]
    public async Task TestMissingInEnum()
    {
        await VerifySnippetIsAbsentAsync("""
            enum MyEnum
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task TestMissingInMethod()
    {
        await VerifySnippetIsAbsentAsync("""
            class Program
            {
                void M()
                {
                    $$
                }
            }
            """);
    }

    [Fact]
    public async Task TestMissingInConstructor()
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
    public async Task TestInsertSnippetAfterAccessibilityModifier(string modifier)
    {
        await VerifySnippetAsync($$"""
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
    }

    [Theory]
    [InlineData("static")]
    [InlineData("virtual")]
    [InlineData("abstract")]
    [InlineData("override")]
    [InlineData("file")]
    public async Task TestMissingAfterIncorrectModifiers(string modifier)
    {
        await VerifySnippetIsAbsentAsync($$"""
            class Program
            {
                {{modifier}} $$
            }
            """);
    }

    [Fact]
    public async Task TestMissingIfAnotherMemberWithNameMainExists()
    {
        await VerifySnippetIsAbsentAsync("""
            class Program
            {
                public int Main => 0;

                $$
            }
            """);
    }

    [Fact]
    public async Task TestMissingIfTopLevelStatementsArePresent()
    {
        await VerifySnippetIsAbsentAsync("""
            System.Console.WriteLine();
            
            class Program
            {
                $$
            }
            """);
    }
}
