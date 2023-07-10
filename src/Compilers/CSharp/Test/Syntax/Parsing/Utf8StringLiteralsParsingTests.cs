// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class Utf8StringLiteralsParsingTests : ParsingTests
    {
        public Utf8StringLiteralsParsingTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void RegularStringLiteral_01()
        {
            UsingExpression(@"""hello""");

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.StringLiteralToken, "\"hello\"");
            }
            EOF();
        }

        [Fact]
        public void RegularStringLiteral_02()
        {
            UsingExpression(@"@""hello""");

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.StringLiteralToken, "@\"hello\"");
            }
            EOF();
        }

        [Fact]
        public void RawStringLiteral_01()
        {
            UsingExpression(@"""""""hello""""""");

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.SingleLineRawStringLiteralToken, "\"\"\"hello\"\"\"");
            }
            EOF();
        }

        [Fact]
        public void RawStringLiteral_02()
        {
            UsingExpression(@"""""""
hello
""""""");

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.MultiLineRawStringLiteralToken, "\"\"\"" + @"
hello
" + "\"\"\"");
            }
            EOF();
        }

        [Fact]
        public void RawStringLiteral_03()
        {
            UsingExpression(@"@""""""hello""""""");

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.StringLiteralToken, "@\"\"\"hello\"\"\"");
            }
            EOF();
        }

        [Fact]
        public void RawStringLiteral_04()
        {
            UsingExpression(@"@""""""
hello
""""""");

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.StringLiteralToken, "@\"\"\"" + @"
hello
" + "\"\"\"");
            }
            EOF();
        }

        [Fact]
        public void Utf8StringLiteral_01()
        {
            UsingExpression(@"""hello""u8");

            N(SyntaxKind.Utf8StringLiteralExpression);
            {
                N(SyntaxKind.Utf8StringLiteralToken, "\"hello\"u8");
            }
            EOF();
        }

        [Fact]
        public void Utf8StringLiteral_02()
        {
            UsingExpression(@"""hello""u8", options: TestOptions.Regular11);

            N(SyntaxKind.Utf8StringLiteralExpression);
            {
                N(SyntaxKind.Utf8StringLiteralToken, "\"hello\"u8");
            }
            EOF();
        }

        [Fact]
        public void Utf8StringLiteral_03()
        {
            UsingExpression(@"""hello""u8", options: TestOptions.Regular10);

            N(SyntaxKind.Utf8StringLiteralExpression);
            {
                N(SyntaxKind.Utf8StringLiteralToken, "\"hello\"u8");
            }
            EOF();
        }

        [Fact]
        public void Utf8StringLiteral_04()
        {
            UsingExpression(@"@""hello""u8");

            N(SyntaxKind.Utf8StringLiteralExpression);
            {
                N(SyntaxKind.Utf8StringLiteralToken, "@\"hello\"u8");
            }
            EOF();
        }

        [Fact]
        public void Utf8StringLiteral_05()
        {
            UsingExpression(@"@""hello""u8", options: TestOptions.Regular11);

            N(SyntaxKind.Utf8StringLiteralExpression);
            {
                N(SyntaxKind.Utf8StringLiteralToken, "@\"hello\"u8");
            }
            EOF();
        }

        [Fact]
        public void Utf8StringLiteral_06()
        {
            UsingExpression(@"@""hello""u8", options: TestOptions.Regular10);

            N(SyntaxKind.Utf8StringLiteralExpression);
            {
                N(SyntaxKind.Utf8StringLiteralToken, "@\"hello\"u8");
            }
            EOF();
        }

        [Fact]
        public void Utf8StringLiteral_07()
        {
            UsingExpression(@"""hello""U8");

            N(SyntaxKind.Utf8StringLiteralExpression);
            {
                N(SyntaxKind.Utf8StringLiteralToken, "\"hello\"U8");
            }
            EOF();
        }

        [Fact]
        public void Utf8StringLiteral_08()
        {
            UsingExpression(@"""hello""U8", options: TestOptions.Regular11);

            N(SyntaxKind.Utf8StringLiteralExpression);
            {
                N(SyntaxKind.Utf8StringLiteralToken, "\"hello\"U8");
            }
            EOF();
        }

        [Fact]
        public void Utf8StringLiteral_09()
        {
            UsingExpression(@"""hello""U8", options: TestOptions.Regular10);

            N(SyntaxKind.Utf8StringLiteralExpression);
            {
                N(SyntaxKind.Utf8StringLiteralToken, "\"hello\"U8");
            }
            EOF();
        }

        [Fact]
        public void Utf8StringLiteral_10()
        {
            UsingExpression(@"@""hello""U8");

            N(SyntaxKind.Utf8StringLiteralExpression);
            {
                N(SyntaxKind.Utf8StringLiteralToken, "@\"hello\"U8");
            }
            EOF();
        }

        [Fact]
        public void Utf8StringLiteral_11()
        {
            UsingExpression(@"@""hello""U8", options: TestOptions.Regular11);

            N(SyntaxKind.Utf8StringLiteralExpression);
            {
                N(SyntaxKind.Utf8StringLiteralToken, "@\"hello\"U8");
            }
            EOF();
        }

        [Fact]
        public void Utf8StringLiteral_12()
        {
            UsingExpression(@"@""hello""U8", options: TestOptions.Regular10);

            N(SyntaxKind.Utf8StringLiteralExpression);
            {
                N(SyntaxKind.Utf8StringLiteralToken, "@\"hello\"U8");
            }
            EOF();
        }

        [Fact]
        public void Errors_01()
        {
            // The behavior is consistent with how type suffixes are handled on numeric literals, see Errors_06.
            UsingExpression(@"@""hello"" u8",
                // (1,1): error CS1073: Unexpected token 'u8'
                // @"hello" u8
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"@""hello""").WithArguments("u8").WithLocation(1, 1)
                );

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.StringLiteralToken, "@\"hello\"");
            }
            EOF();
        }

        [Fact]
        public void Errors_02()
        {
            UsingExpression(@"@""hello""u",
                // (1,1): error CS1073: Unexpected token 'u'
                // @"hello"u
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"@""hello""").WithArguments("u").WithLocation(1, 1)
                );

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.StringLiteralToken, "@\"hello\"");
            }
            EOF();
        }

        [Fact]
        public void Errors_03()
        {
            UsingExpression(@"@""hello""8",
                // (1,1): error CS1073: Unexpected token '8'
                // @"hello"8
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"@""hello""").WithArguments("8").WithLocation(1, 1)
                );

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.StringLiteralToken, "@\"hello\"");
            }
            EOF();
        }

        [Fact]
        public void Errors_04()
        {
            // The behavior is consistent with how type suffixes are handled on numeric literals, see Errors_05.
            UsingExpression(@"@""hello""u80",
                // (1,1): error CS1073: Unexpected token '0'
                // @"hello"u80
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"@""hello""u8").WithArguments("0").WithLocation(1, 1)
                );

            N(SyntaxKind.Utf8StringLiteralExpression);
            {
                N(SyntaxKind.Utf8StringLiteralToken, "@\"hello\"u8");
            }
            EOF();
        }

        [Fact]
        public void Errors_05()
        {
            UsingExpression(@"1L0",
                // (1,1): error CS1073: Unexpected token '0'
                // 1l0
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "1L").WithArguments("0").WithLocation(1, 1)
                );

            N(SyntaxKind.NumericLiteralExpression);
            {
                N(SyntaxKind.NumericLiteralToken, "1L");
            }
            EOF();
        }

        [Fact]
        public void Errors_06()
        {
            UsingExpression(@"1 L",
                // (1,1): error CS1073: Unexpected token 'L'
                // 1 L
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "1").WithArguments("L").WithLocation(1, 1)
                );

            N(SyntaxKind.NumericLiteralExpression);
            {
                N(SyntaxKind.NumericLiteralToken, "1");
            }
            EOF();
        }

        [Fact]
        public void Errors_07()
        {
            // The behavior is consistent with how type suffixes are handled on numeric literals, see Errors_06.
            UsingExpression(@"""hello"" u8",
                // (1,1): error CS1073: Unexpected token 'u8'
                // "hello" u8
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"""hello""").WithArguments("u8").WithLocation(1, 1)
                );

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.StringLiteralToken, "\"hello\"");
            }
            EOF();
        }

        [Fact]
        public void Errors_08()
        {
            UsingExpression(@"""hello""u",
                // (1,1): error CS1073: Unexpected token 'u'
                // "hello"u
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"""hello""").WithArguments("u").WithLocation(1, 1)
                );

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.StringLiteralToken, "\"hello\"");
            }
            EOF();
        }

        [Fact]
        public void Errors_09()
        {
            UsingExpression(@"""hello""8",
                // (1,1): error CS1073: Unexpected token '8'
                // "hello"8
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"""hello""").WithArguments("8").WithLocation(1, 1)
                );

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.StringLiteralToken, "\"hello\"");
            }
            EOF();
        }

        [Fact]
        public void Errors_10()
        {
            // The behavior is consistent with how type suffixes are handled on numeric literals, see Errors_05.
            UsingExpression(@"""hello""u80",
                // (1,1): error CS1073: Unexpected token '0'
                // "hello"u80
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"""hello""u8").WithArguments("0").WithLocation(1, 1)
                );

            N(SyntaxKind.Utf8StringLiteralExpression);
            {
                N(SyntaxKind.Utf8StringLiteralToken, "\"hello\"u8");
            }
            EOF();
        }

        [Fact]
        public void Errors_11()
        {
            // The behavior is consistent with how type suffixes are handled on numeric literals, see Errors_06.
            UsingExpression(@"@""hello"" U8",
                // (1,1): error CS1073: Unexpected token 'U8'
                // @"hello" U8
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"@""hello""").WithArguments("U8").WithLocation(1, 1)
                );

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.StringLiteralToken, "@\"hello\"");
            }
            EOF();
        }

        [Fact]
        public void Errors_12()
        {
            UsingExpression(@"@""hello""U",
                // (1,1): error CS1073: Unexpected token 'U'
                // @"hello"u
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"@""hello""").WithArguments("U").WithLocation(1, 1)
                );

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.StringLiteralToken, "@\"hello\"");
            }
            EOF();
        }

        [Fact]
        public void Errors_13()
        {
            // The behavior is consistent with how type suffixes are handled on numeric literals, see Errors_05.
            UsingExpression(@"@""hello""U80",
                // (1,1): error CS1073: Unexpected token '0'
                // @"hello"U80
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"@""hello""U8").WithArguments("0").WithLocation(1, 1)
                );

            N(SyntaxKind.Utf8StringLiteralExpression);
            {
                N(SyntaxKind.Utf8StringLiteralToken, "@\"hello\"U8");
            }
            EOF();
        }

        [Fact]
        public void Errors_14()
        {
            // The behavior is consistent with how type suffixes are handled on numeric literals, see Errors_06.
            UsingExpression(@"""hello"" U8",
                // (1,1): error CS1073: Unexpected token 'U8'
                // "hello" U8
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"""hello""").WithArguments("U8").WithLocation(1, 1)
                );

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.StringLiteralToken, "\"hello\"");
            }
            EOF();
        }

        [Fact]
        public void Errors_15()
        {
            UsingExpression(@"""hello""U",
                // (1,1): error CS1073: Unexpected token 'U'
                // "hello"u
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"""hello""").WithArguments("U").WithLocation(1, 1)
                );

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.StringLiteralToken, "\"hello\"");
            }
            EOF();
        }

        [Fact]
        public void Errors_16()
        {
            // The behavior is consistent with how type suffixes are handled on numeric literals, see Errors_05.
            UsingExpression(@"""hello""U80",
                // (1,1): error CS1073: Unexpected token '0'
                // "hello"U80
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"""hello""U8").WithArguments("0").WithLocation(1, 1)
                );

            N(SyntaxKind.Utf8StringLiteralExpression);
            {
                N(SyntaxKind.Utf8StringLiteralToken, "\"hello\"U8");
            }
            EOF();
        }

        [Fact]
        public void Interpolation_01()
        {
            UsingExpression(@"$""hello""u8",
                // (1,1): error CS1073: Unexpected token 'u8'
                // $"hello"u8
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"$""hello""").WithArguments("u8").WithLocation(1, 1)
                );

            N(SyntaxKind.InterpolatedStringExpression);
            {
                N(SyntaxKind.InterpolatedStringStartToken);
                N(SyntaxKind.InterpolatedStringText);
                {
                    N(SyntaxKind.InterpolatedStringTextToken);
                }
                N(SyntaxKind.InterpolatedStringEndToken);
            }
            EOF();
        }

        [Fact]
        public void Interpolation_02()
        {
            UsingExpression(@"$@""hello""u8",
                // (1,1): error CS1073: Unexpected token 'u8'
                // $@"hello"u8
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"$@""hello""").WithArguments("u8").WithLocation(1, 1)
                );

            N(SyntaxKind.InterpolatedStringExpression);
            {
                N(SyntaxKind.InterpolatedVerbatimStringStartToken);
                N(SyntaxKind.InterpolatedStringText);
                {
                    N(SyntaxKind.InterpolatedStringTextToken);
                }
                N(SyntaxKind.InterpolatedStringEndToken);
            }
            EOF();
        }

        [Fact]
        public void Interpolation_03()
        {
            UsingExpression(@"$""hello""U8",
                // (1,1): error CS1073: Unexpected token 'U8'
                // $"hello"U8
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"$""hello""").WithArguments("U8").WithLocation(1, 1)
                );

            N(SyntaxKind.InterpolatedStringExpression);
            {
                N(SyntaxKind.InterpolatedStringStartToken);
                N(SyntaxKind.InterpolatedStringText);
                {
                    N(SyntaxKind.InterpolatedStringTextToken);
                }
                N(SyntaxKind.InterpolatedStringEndToken);
            }
            EOF();
        }

        [Fact]
        public void Interpolation_04()
        {
            UsingExpression(@"$@""hello""U8",
                // (1,1): error CS1073: Unexpected token 'U8'
                // $@"hello"U8
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"$@""hello""").WithArguments("U8").WithLocation(1, 1)
                );

            N(SyntaxKind.InterpolatedStringExpression);
            {
                N(SyntaxKind.InterpolatedVerbatimStringStartToken);
                N(SyntaxKind.InterpolatedStringText);
                {
                    N(SyntaxKind.InterpolatedStringTextToken);
                }
                N(SyntaxKind.InterpolatedStringEndToken);
            }
            EOF();
        }

        [Fact]
        public void Utf8StringLiteral_13()
        {
            foreach (var options in new[] { TestOptions.RegularDefault, TestOptions.Regular11, TestOptions.Regular10 })
            {
                foreach (var suffix in new[] { "u8", "U8" })
                {
                    UsingExpression(@"""""""hello""""""" + suffix, options: options);

                    N(SyntaxKind.Utf8StringLiteralExpression);
                    {
                        N(SyntaxKind.Utf8SingleLineRawStringLiteralToken, "\"\"\"hello\"\"\"" + suffix);
                    }
                    EOF();
                }
            }
        }

        [Fact]
        public void Utf8StringLiteral_14()
        {
            foreach (var options in new[] { TestOptions.RegularDefault, TestOptions.Regular11, TestOptions.Regular10 })
            {
                foreach (var suffix in new[] { "u8", "U8" })
                {
                    UsingExpression(@"@""""""hello""""""" + suffix, options: options);

                    N(SyntaxKind.Utf8StringLiteralExpression);
                    {
                        N(SyntaxKind.Utf8StringLiteralToken, "@\"\"\"hello\"\"\"" + suffix);
                    }
                    EOF();
                }
            }
        }

        [Theory]
        [InlineData("u8")]
        [InlineData("U8")]
        public void Errors_17(string suffix)
        {
            // The behavior is consistent with how type suffixes are handled on numeric literals, see Errors_06.
            UsingExpression(@"""""""hello"""""" " + suffix,
                // (1,1): error CS1073: Unexpected token 'u8'
                // """hello""" u8
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"""""""hello""""""").WithArguments(suffix).WithLocation(1, 1)
                );

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.SingleLineRawStringLiteralToken, "\"\"\"hello\"\"\"");
            }
            EOF();
        }

        [Theory]
        [InlineData("u")]
        [InlineData("U")]
        public void Errors_18(string suffix)
        {
            UsingExpression(@"""""""hello""""""" + suffix,
                // (1,1): error CS1073: Unexpected token 'u'
                // """hello"""u
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"""""""hello""""""").WithArguments(suffix).WithLocation(1, 1)
                );

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.SingleLineRawStringLiteralToken, "\"\"\"hello\"\"\"");
            }
            EOF();
        }

        [Fact]
        public void Errors_19()
        {
            UsingExpression(@"""""""hello""""""8",
                // (1,1): error CS1073: Unexpected token '8'
                // """hello"""8
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"""""""hello""""""").WithArguments("8").WithLocation(1, 1)
                );

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.SingleLineRawStringLiteralToken, "\"\"\"hello\"\"\"");
            }
            EOF();
        }

        [Theory]
        [InlineData("u80")]
        [InlineData("U80")]
        public void Errors_20(string suffix)
        {
            // The behavior is consistent with how type suffixes are handled on numeric literals, see Errors_05.
            UsingExpression(@"""""""hello""""""" + suffix,
                // (1,1): error CS1073: Unexpected token '0'
                // """hello"""U80
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"""""""hello""""""" + suffix.Substring(0, 2)).WithArguments("0").WithLocation(1, 1)
                );

            N(SyntaxKind.Utf8StringLiteralExpression);
            {
                N(SyntaxKind.Utf8SingleLineRawStringLiteralToken, "\"\"\"hello\"\"\"" + suffix.Substring(0, 2));
            }
            EOF();
        }

        [Theory]
        [InlineData("u8")]
        [InlineData("U8")]
        public void Interpolation_05(string suffix)
        {
            UsingExpression(@"$""""""hello""""""" + suffix,
                // (1,1): error CS1073: Unexpected token 'u8'
                // $"""hello"""u8
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"$""""""hello""""""").WithArguments(suffix).WithLocation(1, 1)
                );

            N(SyntaxKind.InterpolatedStringExpression);
            {
                N(SyntaxKind.InterpolatedSingleLineRawStringStartToken);
                N(SyntaxKind.InterpolatedStringText);
                {
                    N(SyntaxKind.InterpolatedStringTextToken);
                }
                N(SyntaxKind.InterpolatedRawStringEndToken);
            }
            EOF();
        }

        [Theory]
        [InlineData("u8")]
        [InlineData("U8")]
        public void Interpolation_06(string suffix)
        {
            UsingExpression(@"$@""""""hello""""""" + suffix,
                // (1,1): error CS1073: Unexpected token 'u8'
                // $@"""hello"""u8
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"$@""""""hello""""""").WithArguments(suffix).WithLocation(1, 1)
                );

            N(SyntaxKind.InterpolatedStringExpression);
            {
                N(SyntaxKind.InterpolatedVerbatimStringStartToken);
                N(SyntaxKind.InterpolatedStringText);
                {
                    N(SyntaxKind.InterpolatedStringTextToken);
                }
                N(SyntaxKind.InterpolatedStringEndToken);
            }
            EOF();
        }

        [Fact]
        public void Utf8StringLiteral_15()
        {
            foreach (var options in new[] { TestOptions.RegularDefault, TestOptions.Regular11, TestOptions.Regular10 })
            {
                foreach (var suffix in new[] { "u8", "U8" })
                {
                    UsingExpression(@"""""""
hello
""""""" + suffix, options: options);

                    N(SyntaxKind.Utf8StringLiteralExpression);
                    {
                        N(SyntaxKind.Utf8MultiLineRawStringLiteralToken, "\"\"\"" + @"
hello
" + "\"\"\"" + suffix);
                    }
                    EOF();
                }
            }
        }

        [Fact]
        public void Utf8StringLiteral_16()
        {
            foreach (var options in new[] { TestOptions.RegularDefault, TestOptions.Regular11, TestOptions.Regular10 })
            {
                foreach (var suffix in new[] { "u8", "U8" })
                {
                    UsingExpression(@"@""""""
hello
""""""" + suffix, options: options);

                    N(SyntaxKind.Utf8StringLiteralExpression);
                    {
                        N(SyntaxKind.Utf8StringLiteralToken, "@\"\"\"" + @"
hello
" + "\"\"\"" + suffix);
                    }
                    EOF();
                }
            }
        }

        [Theory]
        [InlineData("u8")]
        [InlineData("U8")]
        public void Errors_21(string suffix)
        {
            // The behavior is consistent with how type suffixes are handled on numeric literals, see Errors_06.
            UsingExpression(@"""""""
hello
"""""" " + suffix,
                // (1,1): error CS1073: Unexpected token 'u8'
                // """hello""" u8
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"""""""
hello
""""""").WithArguments(suffix).WithLocation(1, 1)
                );

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.MultiLineRawStringLiteralToken, "\"\"\"" + @"
hello
" + "\"\"\"");
            }
            EOF();
        }

        [Theory]
        [InlineData("u")]
        [InlineData("U")]
        public void Errors_22(string suffix)
        {
            UsingExpression(@"""""""
hello
""""""" + suffix,
                // (1,1): error CS1073: Unexpected token 'u'
                // """hello"""u
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"""""""
hello
""""""").WithArguments(suffix).WithLocation(1, 1)
                );

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.MultiLineRawStringLiteralToken, "\"\"\"" + @"
hello
" + "\"\"\"");
            }
            EOF();
        }

        [Fact]
        public void Errors_23()
        {
            UsingExpression(@"""""""
hello
""""""8",
                // (1,1): error CS1073: Unexpected token '8'
                // """hello"""8
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"""""""
hello
""""""").WithArguments("8").WithLocation(1, 1)
                );

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.MultiLineRawStringLiteralToken, "\"\"\"" + @"
hello
" + "\"\"\"");
            }
            EOF();
        }

        [Theory]
        [InlineData("u80")]
        [InlineData("U80")]
        public void Errors_24(string suffix)
        {
            // The behavior is consistent with how type suffixes are handled on numeric literals, see Errors_05.
            UsingExpression(@"""""""
hello
""""""" + suffix,
                // (1,1): error CS1073: Unexpected token '0'
                // """hello"""U80
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"""""""
hello
""""""" + suffix.Substring(0, 2)).WithArguments("0").WithLocation(1, 1)
                );

            N(SyntaxKind.Utf8StringLiteralExpression);
            {
                N(SyntaxKind.Utf8MultiLineRawStringLiteralToken, "\"\"\"" + @"
hello
" + "\"\"\"" + suffix.Substring(0, 2));
            }
            EOF();
        }

        [Theory]
        [InlineData("u8")]
        [InlineData("U8")]
        public void Interpolation_07(string suffix)
        {
            UsingExpression(@"$""""""
hello
""""""" + suffix,
                // (1,1): error CS1073: Unexpected token 'u8'
                // $"""hello"""u8
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"$""""""
hello
""""""").WithArguments(suffix).WithLocation(1, 1)
                );

            N(SyntaxKind.InterpolatedStringExpression);
            {
                N(SyntaxKind.InterpolatedMultiLineRawStringStartToken);
                N(SyntaxKind.InterpolatedStringText);
                {
                    N(SyntaxKind.InterpolatedStringTextToken);
                }
                N(SyntaxKind.InterpolatedRawStringEndToken);
            }
            EOF();
        }

        [Theory]
        [InlineData("u8")]
        [InlineData("U8")]
        public void Interpolation_08(string suffix)
        {
            UsingExpression(@"$@""""""
hello
""""""" + suffix,
                // (1,1): error CS1073: Unexpected token 'u8'
                // $@"""hello"""u8
                Diagnostic(ErrorCode.ERR_UnexpectedToken, @"$@""""""
hello
""""""").WithArguments(suffix).WithLocation(1, 1)
                );

            N(SyntaxKind.InterpolatedStringExpression);
            {
                N(SyntaxKind.InterpolatedVerbatimStringStartToken);
                N(SyntaxKind.InterpolatedStringText);
                {
                    N(SyntaxKind.InterpolatedStringTextToken);
                }
                N(SyntaxKind.InterpolatedStringEndToken);
            }
            EOF();
        }

        [Fact]
        public void Utf8StringLiteral_Await_01()
        {
            UsingExpression(@"await ""hello""u8");

            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.Utf8StringLiteralExpression);
                {
                    N(SyntaxKind.Utf8StringLiteralToken, "\"hello\"u8");
                }
            }
            EOF();
        }

        [Fact]
        public void Utf8StringLiteral_Await_02()
        {
            UsingExpression(@"await @""hello""u8");

            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.Utf8StringLiteralExpression);
                {
                    N(SyntaxKind.Utf8StringLiteralToken, "@\"hello\"u8");
                }
            }
            EOF();
        }

        [Fact]
        public void Utf8StringLiteral_Await_03()
        {
            UsingExpression(@"await """"""hello""""""u8");

            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.Utf8StringLiteralExpression);
                {
                    N(SyntaxKind.Utf8SingleLineRawStringLiteralToken, "\"\"\"hello\"\"\"u8");
                }
            }
            EOF();
        }

        [Fact]
        public void Utf8StringLiteral_Await_04()
        {
            UsingExpression(@"await """"""
hello
""""""u8");

            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.Utf8StringLiteralExpression);
                {
                    N(SyntaxKind.Utf8MultiLineRawStringLiteralToken, "\"\"\"" + @"
hello
" + "\"\"\"u8");
                }
            }
            EOF();
        }
    }
}
