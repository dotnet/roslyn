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
public class TupleNameCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType() => typeof(TupleNameCompletionProvider);

    [Fact]
    public async Task AfterOpenParen()
    {
        await VerifyItemExistsAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    (int word, int zword) t = ($$
                }
            }
            """, "word", displayTextSuffix: ":");
    }

    [Fact]
    public async Task AfterOpenParenWithBraceCompletion()
    {
        await VerifyItemExistsAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    (int word, int zword) t = ($$)
                }
            }
            """, "word", displayTextSuffix: ":");
    }

    [Fact]
    public async Task AfterOpenParenInTupleExpression()
    {
        await VerifyItemExistsAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    (int word, int zword) t = ($$, zword: 2
                }
            }
            """, "word", displayTextSuffix: ":");
    }

    [Fact]
    public async Task AfterOpenParenInTupleExpressionWithBraceCompletion()
    {
        await VerifyItemExistsAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    (int word, int zword) t = ($$, zword: 2
                }
            }
            """, "word", displayTextSuffix: ":");
    }

    [Fact]
    public async Task AfterComma()
    {
        await VerifyItemExistsAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    (int word, int zword) t = (1, $$
                }
            }
            """, "zword", displayTextSuffix: ":");
    }

    [Fact]
    public async Task AfterCommaWithBraceCompletion()
    {
        await VerifyItemExistsAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    (int word, int zword) t = (1, $$)
                }
            }
            """, "zword", displayTextSuffix: ":");
    }

    [Fact]
    public async Task InTupleAsArgument()
    {
        await VerifyItemExistsAsync("""
            class Program
            {
                static void Main((int word, int zword) args)
                {
                     Main(($$))
                }
            }
            """, "word", displayTextSuffix: ":");
    }

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
    public async Task AtIndexGreaterThanNumberOfTupleElements()
    {
        var markup = """
            class Program
            {
                static void Main(string[] args)
                {
                    (int word, int zword) t = (1, 2, 3, 4, $$ 
                }
            }
            """;
        await VerifyNoItemsExistAsync(markup);
    }

    [Fact]
    public async Task ConvertCastToTupleExpression()
    {
        var markup = """
            class C
            {
                void goo()
                {
                    (int goat, int moat) x = (g$$)1;
                }
            }
            """;
        await VerifyItemExistsAsync(markup, "goat", displayTextSuffix: ":");
    }
}
