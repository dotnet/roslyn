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
    public class UTF8StringLiteralsParsingTests : ParsingTests
    {
        public UTF8StringLiteralsParsingTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void RegularStringLiteral()
        {
            UsingExpression(@"""hello""");

            N(SyntaxKind.StringLiteralExpression);
            {
                N(SyntaxKind.StringLiteralToken, "\"hello\"");
            }
            EOF();
        }

        [Fact]
        public void UTF8StringLiteral_01()
        {
            UsingExpression(@"""hello""u8");

            N(SyntaxKind.UTF8StringLiteralExpression);
            {
                N(SyntaxKind.UTF8StringLiteralToken, "\"hello\"u8");
            }
            EOF();
        }

        [Fact]
        public void UTF8StringLiteral_02()
        {
            UsingExpression(@"""hello""u8", options: TestOptions.RegularNext);

            N(SyntaxKind.UTF8StringLiteralExpression);
            {
                N(SyntaxKind.UTF8StringLiteralToken, "\"hello\"u8");
            }
            EOF();
        }

        [Fact]
        public void UTF8StringLiteral_03()
        {
            UsingExpression(@"""hello""u8", options: TestOptions.Regular10);

            N(SyntaxKind.UTF8StringLiteralExpression);
            {
                N(SyntaxKind.UTF8StringLiteralToken, "\"hello\"u8");
            }
            EOF();
        }

        [Fact]
        public void UTF8StringLiteral_04()
        {
            UsingExpression(@"@""hello""u8");

            N(SyntaxKind.UTF8StringLiteralExpression);
            {
                N(SyntaxKind.UTF8StringLiteralToken, "@\"hello\"u8");
            }
            EOF();
        }

        [Fact]
        public void UTF8StringLiteral_05()
        {
            UsingExpression(@"@""hello""u8", options: TestOptions.RegularNext);

            N(SyntaxKind.UTF8StringLiteralExpression);
            {
                N(SyntaxKind.UTF8StringLiteralToken, "@\"hello\"u8");
            }
            EOF();
        }

        [Fact]
        public void UTF8StringLiteral_06()
        {
            UsingExpression(@"@""hello""u8", options: TestOptions.Regular10);

            N(SyntaxKind.UTF8StringLiteralExpression);
            {
                N(SyntaxKind.UTF8StringLiteralToken, "@\"hello\"u8");
            }
            EOF();
        }

        [Fact]
        public void UTF8StringLiteral_07()
        {
            UsingExpression(@"""hello""U8");

            N(SyntaxKind.UTF8StringLiteralExpression);
            {
                N(SyntaxKind.UTF8StringLiteralToken, "\"hello\"U8");
            }
            EOF();
        }

        [Fact]
        public void UTF8StringLiteral_08()
        {
            UsingExpression(@"""hello""U8", options: TestOptions.RegularNext);

            N(SyntaxKind.UTF8StringLiteralExpression);
            {
                N(SyntaxKind.UTF8StringLiteralToken, "\"hello\"U8");
            }
            EOF();
        }

        [Fact]
        public void UTF8StringLiteral_09()
        {
            UsingExpression(@"""hello""U8", options: TestOptions.Regular10);

            N(SyntaxKind.UTF8StringLiteralExpression);
            {
                N(SyntaxKind.UTF8StringLiteralToken, "\"hello\"U8");
            }
            EOF();
        }

        [Fact]
        public void UTF8StringLiteral_10()
        {
            UsingExpression(@"@""hello""U8");

            N(SyntaxKind.UTF8StringLiteralExpression);
            {
                N(SyntaxKind.UTF8StringLiteralToken, "@\"hello\"U8");
            }
            EOF();
        }

        [Fact]
        public void UTF8StringLiteral_11()
        {
            UsingExpression(@"@""hello""U8", options: TestOptions.RegularNext);

            N(SyntaxKind.UTF8StringLiteralExpression);
            {
                N(SyntaxKind.UTF8StringLiteralToken, "@\"hello\"U8");
            }
            EOF();
        }

        [Fact]
        public void UTF8StringLiteral_12()
        {
            UsingExpression(@"@""hello""U8", options: TestOptions.Regular10);

            N(SyntaxKind.UTF8StringLiteralExpression);
            {
                N(SyntaxKind.UTF8StringLiteralToken, "@\"hello\"U8");
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

            N(SyntaxKind.UTF8StringLiteralExpression);
            {
                N(SyntaxKind.UTF8StringLiteralToken, "@\"hello\"u8");
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

            N(SyntaxKind.UTF8StringLiteralExpression);
            {
                N(SyntaxKind.UTF8StringLiteralToken, "\"hello\"u8");
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

            N(SyntaxKind.UTF8StringLiteralExpression);
            {
                N(SyntaxKind.UTF8StringLiteralToken, "@\"hello\"U8");
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

            N(SyntaxKind.UTF8StringLiteralExpression);
            {
                N(SyntaxKind.UTF8StringLiteralToken, "\"hello\"U8");
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
    }
}
