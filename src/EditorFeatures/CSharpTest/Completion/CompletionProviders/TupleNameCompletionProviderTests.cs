// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionSetSources;

[Trait(Traits.Feature, Traits.Features.Completion)]
public sealed class TupleNameCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType() => typeof(TupleNameCompletionProvider);

    [Fact]
    public Task AfterOpenParen()
        => VerifyItemExistsAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    (int word, int zword) t = ($$
                }
            }
            """, "word", displayTextSuffix: ":");

    [Fact]
    public Task AfterOpenParenWithBraceCompletion()
        => VerifyItemExistsAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    (int word, int zword) t = ($$)
                }
            }
            """, "word", displayTextSuffix: ":");

    [Fact]
    public Task AfterOpenParenInTupleExpression()
        => VerifyItemExistsAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    (int word, int zword) t = ($$, zword: 2
                }
            }
            """, "word", displayTextSuffix: ":");

    [Fact]
    public Task AfterOpenParenInTupleExpressionWithBraceCompletion()
        => VerifyItemExistsAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    (int word, int zword) t = ($$, zword: 2
                }
            }
            """, "word", displayTextSuffix: ":");

    [Fact]
    public Task AfterComma()
        => VerifyItemExistsAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    (int word, int zword) t = (1, $$
                }
            }
            """, "zword", displayTextSuffix: ":");

    [Fact]
    public Task AfterCommaWithBraceCompletion()
        => VerifyItemExistsAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    (int word, int zword) t = (1, $$)
                }
            }
            """, "zword", displayTextSuffix: ":");

    [Fact]
    public Task InTupleAsArgument()
        => VerifyItemExistsAsync("""
            class Program
            {
                static void Main((int word, int zword) args)
                {
                     Main(($$))
                }
            }
            """, "word", displayTextSuffix: ":");

    [Fact]
    public async Task MultiplePossibleTuples()
    {
        var markup = """
            class Program
            {
                static void Main((int number, int znumber) args) { }
                static void Main((string word, int zword) args) {
                    Main(($$
                }
            }
            """;
        await VerifyItemExistsAsync(markup, "word", displayTextSuffix: ":");
        await VerifyItemExistsAsync(markup, "number", displayTextSuffix: ":");
    }

    [Fact]
    public async Task MultiplePossibleTuplesAfterComma()
    {
        var markup = """
            class Program
            {
                static void Main((int number, int znumber) args) { }
                static void Main((string word, int zword) args) {
                    Main((1, $$
                }
            }
            """;
        await VerifyItemExistsAsync(markup, "zword", displayTextSuffix: ":");
        await VerifyItemExistsAsync(markup, "znumber", displayTextSuffix: ":");
    }

    [Fact]
    public Task AtIndexGreaterThanNumberOfTupleElements()
        => VerifyNoItemsExistAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    (int word, int zword) t = (1, 2, 3, 4, $$ 
                }
            }
            """);

    [Fact]
    public Task ConvertCastToTupleExpression()
        => VerifyItemExistsAsync("""
            class C
            {
                void goo()
                {
                    (int goat, int moat) x = (g$$)1;
                }
            }
            """, "goat", displayTextSuffix: ":");
}
