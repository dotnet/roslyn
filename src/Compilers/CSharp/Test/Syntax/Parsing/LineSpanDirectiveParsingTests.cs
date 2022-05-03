// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LineSpanDirectiveParsingTests : ParsingTests
    {
        public LineSpanDirectiveParsingTests(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions? options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: options);
        }

        protected override CSharpSyntaxNode ParseNode(string text, CSharpParseOptions? options)
        {
            return SyntaxFactory.ParseExpression(text, options: options);
        }

        private void UsingLineDirective(string text, CSharpParseOptions? options, params DiagnosticDescription[] expectedErrors)
        {
            var node = ParseTree(text, options).GetCompilationUnitRoot();
            Validate(text, node, expectedErrors);
            UsingNode(node.GetDirectives().Single(d => d.Kind() is SyntaxKind.LineDirectiveTrivia or SyntaxKind.LineSpanDirectiveTrivia));
        }

        [Fact]
        public void IsActive()
        {
            string source =
@"#if IsActive
#line (1, 2) - (3, 4) ""file.cs""
#endif";

            UsingLineDirective(source, TestOptions.Regular9);
            verify();

            UsingLineDirective(source, TestOptions.Regular9.WithPreprocessorSymbols("IsActive"),
                // (2,2): error CS8773: Feature 'line span directive' is not available in C# 9.0. Please use language version 10.0 or greater.
                // #line (1, 2) - (3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "line").WithArguments("line span directive", "10.0").WithLocation(2, 2));
            verify();

            void verify()
            {
                N(SyntaxKind.LineSpanDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    N(SyntaxKind.LineKeyword);
                    N(SyntaxKind.LineDirectivePosition);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.NumericLiteralToken, "1");
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.NumericLiteralToken, "2");
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.MinusToken);
                    N(SyntaxKind.LineDirectivePosition);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.NumericLiteralToken, "3");
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.NumericLiteralToken, "4");
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                    N(SyntaxKind.EndOfDirectiveToken);
                }
                EOF();
            }
        }

        [Fact]
        public void LineDirective_01()
        {
            string source = @"#line (1, 2) - (3, 4) ""file.cs""";

            UsingLineDirective(source, TestOptions.Regular9,
                // (1,2): error CS8773: Feature 'line span directive' is not available in C# 9.0. Please use language version 10.0 or greater.
                // #line (1, 2) - (3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "line").WithArguments("line span directive", "10.0").WithLocation(1, 2));
            verify();

            UsingLineDirective(source, TestOptions.Regular10);
            verify();

            void verify()
            {
                N(SyntaxKind.LineSpanDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    N(SyntaxKind.LineKeyword);
                    N(SyntaxKind.LineDirectivePosition);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.NumericLiteralToken, "1");
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.NumericLiteralToken, "2");
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.MinusToken);
                    N(SyntaxKind.LineDirectivePosition);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.NumericLiteralToken, "3");
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.NumericLiteralToken, "4");
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                    N(SyntaxKind.EndOfDirectiveToken);
                }
                EOF();
            }
        }

        [Fact]
        public void LineDirective_02()
        {
            string source = @"#line (1, 2) - (3, 4) 5 ""file.cs""";

            UsingLineDirective(source, TestOptions.Regular9,
                // (1,2): error CS8773: Feature 'line span directive' is not available in C# 9.0. Please use language version 10.0 or greater.
                // #line (1, 2) - (3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "line").WithArguments("line span directive", "10.0").WithLocation(1, 2));
            verify();

            UsingLineDirective(source, TestOptions.Regular10);
            verify();

            void verify()
            {
                N(SyntaxKind.LineSpanDirectiveTrivia);
                {
                    N(SyntaxKind.HashToken);
                    N(SyntaxKind.LineKeyword);
                    N(SyntaxKind.LineDirectivePosition);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.NumericLiteralToken, "1");
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.NumericLiteralToken, "2");
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.MinusToken);
                    N(SyntaxKind.LineDirectivePosition);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.NumericLiteralToken, "3");
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.NumericLiteralToken, "4");
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.NumericLiteralToken, "5");
                    N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                    N(SyntaxKind.EndOfDirectiveToken);
                }
                EOF();
            }
        }

        [Fact]
        public void LineDirective_03()
        {
            string source = @"#line (1, 2) - (3, 4) """"";

            UsingLineDirective(source, options: null);

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "4");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.StringLiteralToken, "\"\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void LineDirective_04()
        {
            string source = @"   #   line   (   1   ,   2   )   -   (   3   ,   4   )   5   ""   """;

            UsingLineDirective(source, options: null);

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "4");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.NumericLiteralToken, "5");
                N(SyntaxKind.StringLiteralToken, "\"   \"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void LineDirective_05()
        {
            string source = @"#line(1,2)-(3,4)""file.cs""";

            UsingLineDirective(source, options: null);

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "4");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void LineDirective_06()
        {
            string source = @"#line(1,2)-(3,4)5""file.cs""";

            UsingLineDirective(source, options: null);

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "4");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.NumericLiteralToken, "5");
                N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Incomplete_01()
        {
            string source = @"#line (";

            UsingLineDirective(source, options: null,
                // (1,8): error CS8938: The #line directive value is missing or out of range
                // #line (
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, "").WithLocation(1, 8));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.MinusToken);
                M(SyntaxKind.LineDirectivePosition);
                {
                    M(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Incomplete_02()
        {
            string source = @"#line (1";

            UsingLineDirective(source, options: null,
                // (1,9): error CS1003: Syntax error, ',' expected
                // #line (1
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(1, 9));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.MinusToken);
                M(SyntaxKind.LineDirectivePosition);
                {
                    M(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Incomplete_03()
        {
            string source = @"#line (1,";

            UsingLineDirective(source, options: null,
                // (1,10): error CS8938: The #line directive value is missing or out of range
                // #line (1,
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, "").WithLocation(1, 10));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.MinusToken);
                M(SyntaxKind.LineDirectivePosition);
                {
                    M(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Incomplete_04()
        {
            string source = @"#line (1, 2";

            UsingLineDirective(source, options: null,
                // (1,12): error CS1026: ) expected
                // #line (1, 2
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 12));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.MinusToken);
                M(SyntaxKind.LineDirectivePosition);
                {
                    M(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Incomplete_05()
        {
            string source = @"#line (1, 2)";

            UsingLineDirective(source, options: null,
                // (1,13): error CS1003: Syntax error, '-' expected
                // #line (1, 2)
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("-").WithLocation(1, 13));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.MinusToken);
                M(SyntaxKind.LineDirectivePosition);
                {
                    M(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Incomplete_06()
        {
            string source = @"#line (1, 2) -";

            UsingLineDirective(source, options: null,
                // (1,15): error CS1003: Syntax error, '(' expected
                // #line (1, 2) -
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(1, 15));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                M(SyntaxKind.LineDirectivePosition);
                {
                    M(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Incomplete_07()
        {
            string source = @"#line (1, 2) - (";

            UsingLineDirective(source, options: null,
                // (1,17): error CS8938: The #line directive value is missing or out of range
                // #line (1, 2) - (
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, "").WithLocation(1, 17));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Incomplete_08()
        {
            string source = @"#line (1, 2) - (3";

            UsingLineDirective(source, options: null,
                // (1,18): error CS1003: Syntax error, ',' expected
                // #line (1, 2) - (3
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(1, 18));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Incomplete_09()
        {
            string source = @"#line (1, 2) - (3,";

            UsingLineDirective(source, options: null,
                // (1,19): error CS8938: The #line directive value is missing or out of range
                // #line (1, 2) - (3,
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, "").WithLocation(1, 19));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    N(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Incomplete_10()
        {
            string source = @"#line (1, 2) - (3, 4";

            UsingLineDirective(source, options: null,
                // (1,21): error CS1026: ) expected
                // #line (1, 2) - (3, 4
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 21));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "4");
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Incomplete_11()
        {
            string source = @"#line (1, 2) - (3, 4)";

            UsingLineDirective(source, options: null,
                // (1,22): error CS1578: Quoted file name, single-line comment or end-of-line expected
                // #line (1, 2) - (3, 4)
                Diagnostic(ErrorCode.ERR_MissingPPFile, "").WithLocation(1, 22));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "4");
                    N(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Incomplete_12()
        {
            string source = @"#line (1, 2) - (3, 4) 5";

            UsingLineDirective(source, options: null,
                // (1,24): error CS1578: Quoted file name, single-line comment or end-of-line expected
                // #line (1, 2) - (3, 4) 5
                Diagnostic(ErrorCode.ERR_MissingPPFile, "").WithLocation(1, 24));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "4");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.NumericLiteralToken, "5");
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Incomplete_13()
        {
            ParseIncompleteSyntax(@"#line (1, 2) - (3, 4) 5 ""file.cs""");
        }

        [Fact]
        public void Missing_01()
        {
            string source = @"#line 1, 2) - (3, 4) ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,8): error CS1578: Quoted file name, single-line comment or end-of-line expected
                // #line 1, 2) - 3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_MissingPPFile, ",").WithLocation(1, 8));

            N(SyntaxKind.LineDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.NumericLiteralToken, "1");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Missing_02()
        {
            string source = @"#line (, 2) - (3, 4) ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,8): error CS8938: The #line directive value is missing or out of range
                // #line (, 2) - (3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, ",").WithLocation(1, 8));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.NumericLiteralToken);
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "4");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Missing_03()
        {
            string source = @"#line (1 2) - (3, 4) ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,10): error CS1003: Syntax error, ',' expected
                // #line (1 2) - (3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_SyntaxError, "2").WithArguments(",").WithLocation(1, 10));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "4");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Missing_04()
        {
            string source = @"#line (1, ) - (3, 4) ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,11): error CS8938: The #line directive value is missing or out of range
                // #line (1, ) - (3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, ")").WithLocation(1, 11));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "4");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Missing_05()
        {
            string source = @"#line (1, 2 - (3, 4) ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,13): error CS1026: ) expected
                // #line (1, 2 - (3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "-").WithLocation(1, 13));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    M(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "4");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Missing_06()
        {
            string source = @"#line (1, 2) (3, 4) ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,14): error CS1003: Syntax error, '-' expected
                // #line (1, 2) (3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("-").WithLocation(1, 14));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "4");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Missing_07()
        {
            string source = @"#line (1, 2) - 3, 4) ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,16): error CS1003: Syntax error, '(' expected
                // #line (1, 2) - 3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_SyntaxError, "3").WithArguments("(").WithLocation(1, 16));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    M(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "4");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Missing_08()
        {
            string source = @"#line (1, 2) - (, 4) ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,17): error CS8938: The #line directive value is missing or out of range
                // #line (1, 2) - (, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, ",").WithLocation(1, 17));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.NumericLiteralToken);
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "4");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Missing_09()
        {
            string source = @"#line (1, 2) - (3 4) ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,19): error CS1003: Syntax error, ',' expected
                // #line (1, 2) - (3 4) "file.cs"
                Diagnostic(ErrorCode.ERR_SyntaxError, "4").WithArguments(",").WithLocation(1, 19));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "4");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Missing_10()
        {
            string source = @"#line (1, 2) - (3, ) ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,20): error CS8938: The #line directive value is missing or out of range
                // #line (1, 2) - (3, ) "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, ")").WithLocation(1, 20));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    N(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void Missing_11()
        {
            string source = @"#line (1, 2) - (3, 4 ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,22): error CS1026: ) expected
                // #line (1, 2) - (3, 4 "file.cs"
                Diagnostic(ErrorCode.ERR_CloseParenExpected, @"""file.cs""").WithLocation(1, 22));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "4");
                    M(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void UnexpectedToken_01()
        {
            string source = @"#line ('1', 2) - (3, 4) ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,8): error CS8938: The #line directive value is missing or out of range
                // #line ('1', 2) - (3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, "'").WithLocation(1, 8));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.MinusToken);
                M(SyntaxKind.LineDirectivePosition);
                {
                    M(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void UnexpectedToken_02()
        {
            string source = @"#line (1, ""2"") - (3, 4) ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,11): error CS8938: The #line directive value is missing or out of range
                // #line (1, "2") - (3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, @"""2""").WithLocation(1, 11));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.MinusToken);
                M(SyntaxKind.LineDirectivePosition);
                {
                    M(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.StringLiteralToken, "\"2\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void UnexpectedToken_03()
        {
            string source = @"#line (1, 2) - (0b11, 4) ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,17): error CS8938: The #line directive value is missing or out of range
                // #line (1, 2) - (0b11, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, "0").WithLocation(1, 17));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "0");
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void UnexpectedToken_04()
        {
            string source = @"#line (1, 2) - (3, 0x04) ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,20): error CS8938: The #line directive value is missing or out of range
                // #line (1, 2) - (3, 0x04) "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, "0").WithLocation(1, 20));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "0");
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void UnexpectedToken_05()
        {
            string source = @"#line (null, 2) - (3, 4) ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,8): error CS8938: The #line directive value is missing or out of range
                // #line (null, 2) - (3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, "null").WithLocation(1, 8));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.MinusToken);
                M(SyntaxKind.LineDirectivePosition);
                {
                    M(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void UnexpectedToken_06()
        {
            string source = @"#line (1, true) - (3, 4) ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,11): error CS8938: The #line directive value is missing or out of range
                // #line (1, true) - (3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, "true").WithLocation(1, 11));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.MinusToken);
                M(SyntaxKind.LineDirectivePosition);
                {
                    M(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void UnexpectedToken_07()
        {
            string source = @"#line (1, 2) - (int, 4) ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,17): error CS8938: The #line directive value is missing or out of range
                // #line (1, 2) - (int, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, "int").WithLocation(1, 17));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void UnexpectedToken_08()
        {
            string source = @"#line (1u, 2) - (3, 4) ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,9): error CS1003: Syntax error, ',' expected
                // #line (1u, 2) - (3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_SyntaxError, "u").WithArguments(",").WithLocation(1, 9));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.MinusToken);
                M(SyntaxKind.LineDirectivePosition);
                {
                    M(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void UnexpectedToken_09()
        {
            string source = @"#line (1, 2f) - (3, 4) ""  """;

            UsingLineDirective(source, options: null,
                // (1,12): error CS1026: ) expected
                // #line (1, 2f) - (3, 4) "  "
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "f").WithLocation(1, 12));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.MinusToken);
                M(SyntaxKind.LineDirectivePosition);
                {
                    M(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void UnexpectedToken_10()
        {
            string source = @"#line (1, 2) - (3, 4) file.cs";

            UsingLineDirective(source, options: null,
                // (1,23): error CS1578: Quoted file name, single-line comment or end-of-line expected
                // #line (1, 2) - (3, 4) file.cs
                Diagnostic(ErrorCode.ERR_MissingPPFile, "file").WithLocation(1, 23));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "4");
                    N(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void VerifyValue_01()
        {
            string source = @"#line (-1, 2) - (3, 4) ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,8): error CS8938: The #line directive value is missing or out of range
                // #line (-1, 2) - (3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, "-").WithLocation(1, 8));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.NumericLiteralToken);
                    M(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    M(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void VerifyValue_02()
        {
            string source = @"#line (0, 0) - (0, 0) 0 ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,8): error CS8938: The #line directive value is missing or out of range
                // #line (0, 0) - (0, 0) 0 "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, "0").WithLocation(1, 8),
                // (1,11): error CS8938: The #line directive value is missing or out of range
                // #line (0, 0) - (0, 0) 0 "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, "0").WithLocation(1, 11),
                // (1,17): error CS8938: The #line directive value is missing or out of range
                // #line (0, 0) - (0, 0) 0 "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, "0").WithLocation(1, 17),
                // (1,20): error CS8938: The #line directive value is missing or out of range
                // #line (0, 0) - (0, 0) 0 "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, "0").WithLocation(1, 20),
                // (1,23): error CS8938: The #line directive value is missing or out of range
                // #line (0, 0) - (0, 0) 0 "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, "0").WithLocation(1, 23));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "0");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "0");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "0");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "0");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.NumericLiteralToken, "0");
                N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void VerifyValue_03()
        {
            string source = @"#line (16707565, 65536) - (16707565, 65536) 65536 ""file.cs""";

            UsingLineDirective(source, options: null);

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "16707565");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "65536");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "16707565");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "65536");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.NumericLiteralToken, "65536");
                N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void VerifyValue_04()
        {
            string source = @"#line (16707566, 65537) - (16707566, 65537) 65537 ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,8): error CS8938: The #line directive value is missing or out of range
                // #line (16707566, 65537) - (16707566, 65537) 65537 "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, "16707566").WithLocation(1, 8),
                // (1,18): error CS8938: The #line directive value is missing or out of range
                // #line (16707566, 65537) - (16707566, 65537) 65537 "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, "65537").WithLocation(1, 18),
                // (1,28): error CS8938: The #line directive value is missing or out of range
                // #line (16707566, 65537) - (16707566, 65537) 65537 "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, "16707566").WithLocation(1, 28),
                // (1,38): error CS8938: The #line directive value is missing or out of range
                // #line (16707566, 65537) - (16707566, 65537) 65537 "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, "65537").WithLocation(1, 38),
                // (1,45): error CS8938: The #line directive value is missing or out of range
                // #line (16707566, 65537) - (16707566, 65537) 65537 "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, "65537").WithLocation(1, 45));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "16707566");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "65537");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "16707566");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "65537");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.NumericLiteralToken, "65537");
                N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void VerifySpan_01()
        {
            string source = @"#line (10, 20) - (10, 20) ""file.cs""";

            UsingLineDirective(source, options: null);

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "10");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "20");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "10");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "20");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void VerifySpan_02()
        {
            string source = @"#line (10, 20) - (10, 19) ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,18): error CS8939: The #line directive end position must be greater than or equal to the start position
                // #line (10, 20) - (10, 19) "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveEndLessThanStart, "(10, 19)").WithLocation(1, 18));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "10");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "20");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "10");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "19");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void VerifySpan_03()
        {
            string source = @"#line (10, 20) - (9, 20) ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,18): error CS8939: The #line directive end position must be greater than or equal to the start position
                // #line (10, 20) - (9, 20) "file.cs"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveEndLessThanStart, "(9, 20)").WithLocation(1, 18));

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "10");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "20");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "9");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "20");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void VerifySpan_04()
        {
            string source = @"#line (10, 20) - (11, 1) ""file.cs""";

            UsingLineDirective(source, options: null);

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "10");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "20");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "11");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void NotUTF8StringLiteral_01()
        {
            string source = @"#line 1 ""file.cs""u8";

            UsingLineDirective(source, options: null,
                // (1,18): error CS1025: Single-line comment or end-of-line expected
                // #line 1 "file.cs"u8
                Diagnostic(ErrorCode.ERR_EndOfPPLineExpected, "u8").WithLocation(1, 18)
                );

            N(SyntaxKind.LineDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.NumericLiteralToken, "1");
                N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void NotUTF8StringLiteral_02()
        {
            string source = @"#line 1 @""file.cs""u8";

            UsingLineDirective(source, options: null,
                // (1,9): error CS1578: Quoted file name, single-line comment or end-of-line expected
                // #line 1 @"file.cs"u8
                Diagnostic(ErrorCode.ERR_MissingPPFile, "@").WithLocation(1, 9)
                );

            N(SyntaxKind.LineDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.NumericLiteralToken, "1");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void NotUTF8StringLiteral_03()
        {
            string source = @"#line 1 ""file.cs""U8";

            UsingLineDirective(source, options: null,
                // (1,18): error CS1025: Single-line comment or end-of-line expected
                // #line 1 "file.cs"U8
                Diagnostic(ErrorCode.ERR_EndOfPPLineExpected, "U8").WithLocation(1, 18)
                );

            N(SyntaxKind.LineDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.NumericLiteralToken, "1");
                N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void NotUTF8StringLiteral_04()
        {
            string source = @"#line 1 @""file.cs""U8";

            UsingLineDirective(source, options: null,
                // (1,9): error CS1578: Quoted file name, single-line comment or end-of-line expected
                // #line 1 @"file.cs"U8
                Diagnostic(ErrorCode.ERR_MissingPPFile, "@").WithLocation(1, 9)
                );

            N(SyntaxKind.LineDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.NumericLiteralToken, "1");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void NotUTF8StringLiteral_05()
        {
            string source = @"#line 1 """"""file.cs""""""u8";

            UsingLineDirective(source, options: null,
                // (1,9): error CS8996: Raw string literals are not allowed in preprocessor directives.
                // #line 1 """file.cs"""u8
                Diagnostic(ErrorCode.ERR_RawStringNotInDirectives, "").WithLocation(1, 9),
                // (1,22): error CS1025: Single-line comment or end-of-line expected
                // #line 1 """file.cs"""u8
                Diagnostic(ErrorCode.ERR_EndOfPPLineExpected, "u8").WithLocation(1, 22)
                );

            N(SyntaxKind.LineDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.NumericLiteralToken, "1");
                N(SyntaxKind.StringLiteralToken, "\"\"\"file.cs\"\"\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void NotUTF8StringLiteral_06()
        {
            string source = @"#line 1 @""""""file.cs""""""u8";

            UsingLineDirective(source, options: null,
                // (1,9): error CS1578: Quoted file name, single-line comment or end-of-line expected
                // #line 1 @"""file.cs"""u8
                Diagnostic(ErrorCode.ERR_MissingPPFile, "@").WithLocation(1, 9)
                );

            N(SyntaxKind.LineDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.NumericLiteralToken, "1");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void NotUTF8StringLiteral_07()
        {
            string source = @"#line 1 """"""file.cs""""""U8";

            UsingLineDirective(source, options: null,
                // (1,9): error CS8996: Raw string literals are not allowed in preprocessor directives.
                // #line 1 """file.cs"""U8
                Diagnostic(ErrorCode.ERR_RawStringNotInDirectives, "").WithLocation(1, 9),
                // (1,22): error CS1025: Single-line comment or end-of-line expected
                // #line 1 """file.cs"""U8
                Diagnostic(ErrorCode.ERR_EndOfPPLineExpected, "U8").WithLocation(1, 22)
                );

            N(SyntaxKind.LineDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.NumericLiteralToken, "1");
                N(SyntaxKind.StringLiteralToken, "\"\"\"file.cs\"\"\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void NotUTF8StringLiteral_08()
        {
            string source = @"#line 1 @""""""file.cs""""""U8";

            UsingLineDirective(source, options: null,
                // (1,9): error CS1578: Quoted file name, single-line comment or end-of-line expected
                // #line 1 @"""file.cs"""U8
                Diagnostic(ErrorCode.ERR_MissingPPFile, "@").WithLocation(1, 9)
                );

            N(SyntaxKind.LineDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.NumericLiteralToken, "1");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void NotUTF8StringLiteral_09()
        {
            string source = @"#line 1 """"""
file.cs
""""""u8";

            UsingLineDirective(source, options: null,
                // (1,9): error CS8996: Raw string literals are not allowed in preprocessor directives.
                // #line 1 """
                Diagnostic(ErrorCode.ERR_RawStringNotInDirectives, "").WithLocation(1, 9),
                // (2,4): error CS1025: Single-line comment or end-of-line expected
                // """u8
                Diagnostic(ErrorCode.ERR_EndOfPPLineExpected, "u8").WithLocation(2, 4)
                );

            N(SyntaxKind.LineDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.NumericLiteralToken, "1");
                N(SyntaxKind.StringLiteralToken, "\"\"\"" + @"
file.cs
" + "\"\"\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void NotUTF8StringLiteral_10()
        {
            string source = @"#line 1 @""""""
file.cs
""""""u8";

            UsingLineDirective(source, options: null,
                // (1,9): error CS1578: Quoted file name, single-line comment or end-of-line expected
                // #line 1 @"""
                Diagnostic(ErrorCode.ERR_MissingPPFile, "@").WithLocation(1, 9)
                );

            N(SyntaxKind.LineDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.NumericLiteralToken, "1");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void NotUTF8StringLiteral_11()
        {
            string source = @"#line 1 """"""
file.cs
""""""U8";

            UsingLineDirective(source, options: null,
                // (1,9): error CS8996: Raw string literals are not allowed in preprocessor directives.
                // #line 1 """
                Diagnostic(ErrorCode.ERR_RawStringNotInDirectives, "").WithLocation(1, 9),
                // (2,4): error CS1025: Single-line comment or end-of-line expected
                // """U8
                Diagnostic(ErrorCode.ERR_EndOfPPLineExpected, "U8").WithLocation(2, 4)
                );

            N(SyntaxKind.LineDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.NumericLiteralToken, "1");
                N(SyntaxKind.StringLiteralToken, "\"\"\"" + @"
file.cs
" + "\"\"\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void NotUTF8StringLiteral_12()
        {
            string source = @"#line 1 @""""""
file.cs
""""""U8";

            UsingLineDirective(source, options: null,
                // (1,9): error CS1578: Quoted file name, single-line comment or end-of-line expected
                // #line 1 @"""
                Diagnostic(ErrorCode.ERR_MissingPPFile, "@").WithLocation(1, 9)
                );

            N(SyntaxKind.LineDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.NumericLiteralToken, "1");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void NotUTF8StringLiteral_13()
        {
            string source = @"#line (1, 2)-(3, 4) ""file.cs""u8";

            UsingLineDirective(source, options: null,
                // (1,30): error CS1025: Single-line comment or end-of-line expected
                // #line (1, 2)-(3, 4) "file.cs"u8
                Diagnostic(ErrorCode.ERR_EndOfPPLineExpected, "u8").WithLocation(1, 30)
                );

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "4");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.StringLiteralToken, "\"file.cs\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void NotUTF8StringLiteral_14()
        {
            string source = @"#line (1, 2)-(3, 4) @""file.cs""u8";

            UsingLineDirective(source, options: null,
                // (1,21): error CS1578: Quoted file name, single-line comment or end-of-line expected
                // #line (1, 2)-(3, 4) @"file.cs"u8
                Diagnostic(ErrorCode.ERR_MissingPPFile, "@").WithLocation(1, 21)
                );

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "4");
                    N(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void NotUTF8StringLiteral_15()
        {
            string source = @"#line (1, 2)-(3, 4) """"""file.cs""""""u8";

            UsingLineDirective(source, options: null,
                // (1,21): error CS8996: Raw string literals are not allowed in preprocessor directives.
                // #line (1, 2)-(3, 4) """file.cs"""u8
                Diagnostic(ErrorCode.ERR_RawStringNotInDirectives, "").WithLocation(1, 21),
                // (1,34): error CS1025: Single-line comment or end-of-line expected
                // #line (1, 2)-(3, 4) """file.cs"""u8
                Diagnostic(ErrorCode.ERR_EndOfPPLineExpected, "u8").WithLocation(1, 34)
                );

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "4");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.StringLiteralToken, "\"\"\"file.cs\"\"\"");
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }

        [Fact]
        public void NotUTF8StringLiteral_16()
        {
            string source = @"#line (1, 2)-(3, 4) @""""""file.cs""""""u8";

            UsingLineDirective(source, options: null,
                // (1,21): error CS1578: Quoted file name, single-line comment or end-of-line expected
                // #line (1, 2)-(3, 4) @"""file.cs"""u8
                Diagnostic(ErrorCode.ERR_MissingPPFile, "@").WithLocation(1, 21)
                );

            N(SyntaxKind.LineSpanDirectiveTrivia);
            {
                N(SyntaxKind.HashToken);
                N(SyntaxKind.LineKeyword);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "1");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "2");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.LineDirectivePosition);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralToken, "3");
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralToken, "4");
                    N(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.StringLiteralToken);
                N(SyntaxKind.EndOfDirectiveToken);
            }
            EOF();
        }
    }
}
