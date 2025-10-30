// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.NewLines.MultipleBlankLines;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.NewLines.MultipleBlankLines;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NewLines.MultipleBlankLines;

using Verify = CSharpCodeFixVerifier<
    CSharpMultipleBlankLinesDiagnosticAnalyzer,
    MultipleBlankLinesCodeFixProvider>;

public sealed class MultipleBlankLinesTests
{
    [Fact]
    public Task TestOneBlankLineAtTopOfFile()
        => new Verify.Test
        {
            TestCode = """

            // comment
            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestTwoBlankLineAtTopOfFile()
        => new Verify.Test
        {
            TestCode = """
            [||]

            // comment
            """,
            FixedCode = """

            // comment
            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestTwoBlankLineAtTopOfFile_NotWithOptionOff()
        => new Verify.Test
        {
            TestCode = """


            // comment
            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.TrueWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestThreeBlankLineAtTopOfFile()
        => new Verify.Test
        {
            TestCode = """
            [||]


            // comment
            """,
            FixedCode = """

            // comment
            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestFourBlankLineAtTopOfFile()
        => new Verify.Test
        {
            TestCode = """
            [||]



            // comment
            """,
            FixedCode = """

            // comment
            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestOneBlankLineAtTopOfEmptyFile()
        => new Verify.Test
        {
            TestCode = """


            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestTwoBlankLinesAtTopOfEmptyFile()
        => new Verify.Test
        {
            TestCode = """
            [||]


            """,
            FixedCode = """


            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestThreeBlankLinesAtTopOfEmptyFile()
        => new Verify.Test
        {
            TestCode = """
            [||]



            """,
            FixedCode = """


            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestFourBlankLinesAtTopOfEmptyFile()
        => new Verify.Test
        {
            TestCode = """
            [||]




            """,
            FixedCode = """


            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestNoBlankLineAtEndOfFile_1()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
            }
            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestNoBlankLineAtEndOfFile_2()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
            }

            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestOneBlankLineAtEndOfFile()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
            }


            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestTwoBlankLineAtEndOfFile()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
            }
            [||]


            """,
            FixedCode = """
            class C
            {
            }


            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestThreeBlankLineAtEndOfFile()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
            }
            [||]



            """,
            FixedCode = """
            class C
            {
            }


            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestFourBlankLineAtEndOfFile()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
            }
            [||]




            """,
            FixedCode = """
            class C
            {
            }


            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestNoBlankLineBetweenTokens()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
            }
            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestOneBlankLineBetweenTokens()
        => new Verify.Test
        {
            TestCode = """
            class C
            {

            }
            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestTwoBlankLineBetweenTokens()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
            [||]

            }
            """,
            FixedCode = """
            class C
            {

            }
            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestThreeBlankLineBetweenTokens()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
            [||]


            }
            """,
            FixedCode = """
            class C
            {

            }
            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestFourBlankLineBetweenTokens()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
            [||]



            }
            """,
            FixedCode = """
            class C
            {

            }
            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestNoBlankLineAfterComment()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
                // comment
            }
            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestOneBlankLineAfterComment()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
                // comment

            }
            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestTwoBlankLineAfterComment()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
                // comment
            [||]

            }
            """,
            FixedCode = """
            class C
            {
                // comment

            }
            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestThreeBlankLineAfterComment()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
                // comment
            [||]


            }
            """,
            FixedCode = """
            class C
            {
                // comment

            }
            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestFourBlankLineAfterComment()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
                // comment
            [||]


            }
            """,
            FixedCode = """
            class C
            {
                // comment

            }
            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestNoBlankLineAfterDirective()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
                #nullable enable
            }
            """,
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestOneBlankLineAfterDirective()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
                #nullable enable

            }
            """,
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestTwoBlankLineAfterDirective()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
                #nullable enable
            [||]

            }
            """,
            FixedCode = """
            class C
            {
                #nullable enable

            }
            """,
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestThreeBlankLineAfterDirective()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
                #nullable enable
            [||]


            }
            """,
            FixedCode = """
            class C
            {
                #nullable enable

            }
            """,
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestFourBlankLineAfterDirective()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
                #nullable enable
            [||]


            }
            """,
            FixedCode = """
            class C
            {
                #nullable enable

            }
            """,
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestNoBlankLineAfterDocComment()
        => new Verify.Test
        {
            TestCode = """

            /// <summary/>
            class C
            {
            }
            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestOneBlankLineAfterDocComment()
        => new Verify.Test
        {
            TestCode = """

            /// <summary/>

            class C
            {
            }
            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestTwoBlankLineAfterDocComment()
        => new Verify.Test
        {
            TestCode = """

            /// <summary/>
            [||]

            class C
            {
            }
            """,
            FixedCode = """

            /// <summary/>

            class C
            {
            }
            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestThreeBlankLineAfterDocComment()
        => new Verify.Test
        {
            TestCode = """

            /// <summary/>
            [||]


            class C
            {
            }
            """,
            FixedCode = """

            /// <summary/>

            class C
            {
            }
            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestFourBlankLineAfterDocComment()
        => new Verify.Test
        {
            TestCode = """

            /// <summary/>
            [||]



            class C
            {
            }
            """,
            FixedCode = """

            /// <summary/>

            class C
            {
            }
            """,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestNoBlankLineAllConstructs()
        => new Verify.Test
        {
            TestCode = """
            /// <summary/>
            //
            #nullable enable
            class C
            {
            }
            """,
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestOneBlankLineAllConstructs()
        => new Verify.Test
        {
            TestCode = """

            /// <summary/>

            //

            #nullable enable

            class C
            {
            }
            """,
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestTwoBlankLineAllConstructs()
        => new Verify.Test
        {
            TestCode = """
            [||]

            /// <summary/>


            //


            #nullable enable


            class C
            {
            }
            """,
            FixedCode = """

            /// <summary/>

            //

            #nullable enable

            class C
            {
            }
            """,
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestThreeBlankLineAllConstructs()
        => new Verify.Test
        {
            TestCode = """
            [||]


            /// <summary/>



            //



            #nullable enable



            class C
            {
            }
            """,
            FixedCode = """

            /// <summary/>

            //

            #nullable enable

            class C
            {
            }
            """,
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestFourBlankLineAllConstructs()
        => new Verify.Test
        {
            TestCode = """
            [||]



            /// <summary/>




            //




            #nullable enable




            class C
            {
            }
            """,
            FixedCode = """

            /// <summary/>

            //

            #nullable enable

            class C
            {
            }
            """,
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
}
