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

public class ConsecutiveBracePlacementTests
{
    [Fact]
    public async Task NotForBracesOnSameLineDirectlyTouching()
    {
        var code =
@"class C { void M() { }}";

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task NotForBracesOnSameLineWithSpace()
    {
        var code =
@"class C { void M() { } }";

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task NotForBracesOnSameLineWithComment()
    {
        var code =
@"class C { void M() { }/*goo*/}";

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task NotForBracesOnSameLineWithCommentAndSpaces()
    {
        var code =
@"class C { void M() { } /*goo*/ }";

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task NotForBracesOnSubsequentLines_TopLevel()
    {
        var code =
            """
            class C
            {
                void M()
                {
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task NotForBracesOnSubsequentLinesWithComment1_TopLevel()
    {
        var code =
            """
            class C
            {
                void M()
                {
                } // comment
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task NotForBracesOnSubsequentLinesWithComment2_TopLevel()
    {
        var code =
            """
            class C
            {
                void M()
                {
                } /* comment */
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task NotForBracesOnSubsequentLinesIndented()
    {
        var code =
            """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task NotForBracesOnSubsequentLinesIndentedWithComment1()
    {
        var code =
            """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    } // comment
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task NotForBracesOnSubsequentLinesIndentedWithComment2()
    {
        var code =
            """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    } /* comment */
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task NotForBracesWithBlankLinesIfCommentBetween1_TopLevel()
    {
        var code =
            """
            class C
            {
                void M()
                {
                }

                // comment

            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task NotForBracesWithBlankLinesIfCommentBetween2_TopLevel()
    {
        var code =
            """
            class C
            {
                void M()
                {
                }

                /* comment */

            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task NotForBracesWithBlankLinesIfDirectiveBetween1_TopLeve()
    {
        var code =
            """
            class C
            {
                void M()
                {
                }

                #nullable enable

            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task NotForBracesWithBlankLinesIfCommentBetween1_Nested()
    {
        var code =
            """
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
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task NotForBracesWithBlankLinesIfCommentBetween2_Nested()
    {
        var code =
            """
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
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task NotForBracesWithBlankLinesIfDirectiveBetween_Nested()
    {
        var code =
            """
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
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task OneBlankLineBetweenBraces_TopLevel()
    {
        var code =
            """
            class C
            {
                void M()
                {
                }

            [|}|]
            """;
        var fixedCode =
            """
            class C
            {
                void M()
                {
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task OneBlankLineBetweenBraces_TopLevel_OptionDisabled()
    {
        var code =
            """
            class C
            {
                void M()
                {
                }

            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.TrueWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TwoBlankLinesBetweenBraces_TopLevel()
    {
        var code =
            """
            class C
            {
                void M()
                {
                }


            [|}|]
            """;
        var fixedCode =
            """
            class C
            {
                void M()
                {
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task ThreeBlankLinesBetweenBraces_TopLevel()
    {
        var code =
            """
            class C
            {
                void M()
                {
                }



            [|}|]
            """;
        var fixedCode =
            """
            class C
            {
                void M()
                {
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task BlankLinesBetweenBraces_LeadingComment_TopLevel()
    {
        var code =
            """
            class C
            {
                void M()
                {
                }



            /*comment*/[|}|]
            """;
        var fixedCode =
            """
            class C
            {
                void M()
                {
                }
            /*comment*/}
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task BlankLinesBetweenBraces_TrailingComment_TopLevel()
    {
        var code =
            """
            class C
            {
                void M()
                {
                } /*comment*/



            [|}|]
            """;
        var fixedCode =
            """
            class C
            {
                void M()
                {
                } /*comment*/
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task OneBlankLineBetweenBraces_Nested()
    {
        var code =
            """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }

                [|}|]
            }
            """;
        var fixedCode =
            """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TwoBlankLinesBetweenBraces_Nested()
    {
        var code =
            """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }


                [|}|]
            }
            """;
        var fixedCode =
            """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task ThreeBlankLinesBetweenBraces_Nested()
    {
        var code =
            """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }



                [|}|]
            }
            """;
        var fixedCode =
            """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task BlankLinesBetweenBraces_LeadingComment_Nested()
    {
        var code =
            """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }



                /*comment*/[|}|]
            }
            """;
        var fixedCode =
            """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }
                /*comment*/}
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task BlankLinesBetweenBraces_TrailingComment_Nested()
    {
        var code =
            """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    } /*comment*/



                [|}|]
            }
            """;
        var fixedCode =
            """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    } /*comment*/
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task FixAll1()
    {
        var code =
            """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }

                [|}|]

            [|}|]
            """;
        var fixedCode =
            """
            namespace N
            {
                class C
                {
                    void M()
                    {
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task RealCode1()
    {
        var code =
            """
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
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task RealCode2()
    {
        var code =
            """
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
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }
}
