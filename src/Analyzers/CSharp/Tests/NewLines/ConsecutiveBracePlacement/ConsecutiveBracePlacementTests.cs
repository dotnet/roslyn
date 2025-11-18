// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.NewLines.ConsecutiveBracePlacement;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NewLines.ConsecutiveBracePlacement;

using VerifyCS = CSharpCodeFixVerifier<
    ConsecutiveBracePlacementDiagnosticAnalyzer,
    ConsecutiveBracePlacementCodeFixProvider>;

public sealed class ConsecutiveBracePlacementTests
{
    [Fact]
    public Task NotForBracesOnSameLineDirectlyTouching()
        => new VerifyCS.Test
        {
            TestCode = @"class C { void M() { }}",
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task NotForBracesOnSameLineWithSpace()
        => new VerifyCS.Test
        {
            TestCode = @"class C { void M() { } }",
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task NotForBracesOnSameLineWithComment()
        => new VerifyCS.Test
        {
            TestCode = @"class C { void M() { }/*goo*/}",
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task NotForBracesOnSameLineWithCommentAndSpaces()
        => new VerifyCS.Test
        {
            TestCode = @"class C { void M() { } /*goo*/ }",
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task NotForBracesOnSubsequentLines_TopLevel()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task NotForBracesOnSubsequentLinesWithComment1_TopLevel()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                } // comment
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task NotForBracesOnSubsequentLinesWithComment2_TopLevel()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                } /* comment */
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task NotForBracesOnSubsequentLinesIndented()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task NotForBracesOnSubsequentLinesIndentedWithComment1()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    } // comment
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task NotForBracesOnSubsequentLinesIndentedWithComment2()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    } /* comment */
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task NotForBracesWithBlankLinesIfCommentBetween1_TopLevel()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                }

                // comment

            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task NotForBracesWithBlankLinesIfCommentBetween2_TopLevel()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                }

                /* comment */

            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task NotForBracesWithBlankLinesIfDirectiveBetween1_TopLeve()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                }

                #nullable enable

            }
            """,
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task NotForBracesWithBlankLinesIfCommentBetween1_Nested()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }

                    // comment

                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task NotForBracesWithBlankLinesIfCommentBetween2_Nested()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }

                    /* comment */

                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task NotForBracesWithBlankLinesIfDirectiveBetween_Nested()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }

                    #nullable enable

                }
            }
            """,
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task OneBlankLineBetweenBraces_TopLevel()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                }

            [|}|]
            """,
            FixedCode = """
            class C
            {
                void M()
                {
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task OneBlankLineBetweenBraces_TopLevel_OptionDisabled()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                }

            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.TrueWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TwoBlankLinesBetweenBraces_TopLevel()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                }


            [|}|]
            """,
            FixedCode = """
            class C
            {
                void M()
                {
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task ThreeBlankLinesBetweenBraces_TopLevel()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                }



            [|}|]
            """,
            FixedCode = """
            class C
            {
                void M()
                {
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task BlankLinesBetweenBraces_LeadingComment_TopLevel()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                }



            /*comment*/[|}|]
            """,
            FixedCode = """
            class C
            {
                void M()
                {
                }
            /*comment*/}
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task BlankLinesBetweenBraces_TrailingComment_TopLevel()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                } /*comment*/



            [|}|]
            """,
            FixedCode = """
            class C
            {
                void M()
                {
                } /*comment*/
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task OneBlankLineBetweenBraces_Nested()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }

                [|}|]
            }
            """,
            FixedCode = """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TwoBlankLinesBetweenBraces_Nested()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }


                [|}|]
            }
            """,
            FixedCode = """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task ThreeBlankLinesBetweenBraces_Nested()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }



                [|}|]
            }
            """,
            FixedCode = """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task BlankLinesBetweenBraces_LeadingComment_Nested()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }



                /*comment*/[|}|]
            }
            """,
            FixedCode = """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }
                /*comment*/}
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task BlankLinesBetweenBraces_TrailingComment_Nested()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    } /*comment*/



                [|}|]
            }
            """,
            FixedCode = """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    } /*comment*/
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task FixAll1()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }

                [|}|]

            [|}|]
            """,
            FixedCode = """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task RealCode1()
        => new VerifyCS.Test
        {
            TestCode = """
            #nullable enable

            using System;

            #if CODE_STYLE
            using System.Collections.Generic;
            #endif

            namespace Microsoft.CodeAnalysis.Options
            {
                internal interface IOption { }

                internal interface IOption2
            #if !CODE_STYLE
                : IOption
            #endif
                {
                    string OptionDefinition { get; }

            #if CODE_STYLE
                    string Feature { get; }
                    string Name { get; }
                    Type Type { get; }
                    object? DefaultValue { get; }
                    bool IsPerLanguage { get; }

                    List<string> StorageLocations { get; }
            #endif
                }
            }
            """,
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task RealCode2()
        => new VerifyCS.Test
        {
            TestCode = """
            #define CODE_STYLE
            #nullable enable

            using System;

            #if CODE_STYLE
            using System.Collections.Generic;
            #endif

            namespace Microsoft.CodeAnalysis.Options
            {
                internal interface IOption { }

                internal interface IOption2
            #if !CODE_STYLE
                : IOption
            #endif
                {
                    string OptionDefinition { get; }

            #if CODE_STYLE
                    string Feature { get; }
                    string Name { get; }
                    Type Type { get; }
                    object? DefaultValue { get; }
                    bool IsPerLanguage { get; }

                    List<string> StorageLocations { get; }
            #endif
                }
            }
            """,
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
}
