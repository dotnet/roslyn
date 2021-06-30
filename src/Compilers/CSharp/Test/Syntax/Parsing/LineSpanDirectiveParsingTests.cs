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
                // (1,8): error CS1576: The line number specified for #line directive is missing or invalid
                // #line (
                Diagnostic(ErrorCode.ERR_InvalidLineNumber, "").WithLocation(1, 8));

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
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",", "").WithLocation(1, 9));

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
                // (1,10): error CS1576: The line number specified for #line directive is missing or invalid
                // #line (1,
                Diagnostic(ErrorCode.ERR_InvalidLineNumber, "").WithLocation(1, 10));

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
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("-", "").WithLocation(1, 13));

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
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(", "").WithLocation(1, 15));

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
                // (1,17): error CS1576: The line number specified for #line directive is missing or invalid
                // #line (1, 2) - (
                Diagnostic(ErrorCode.ERR_InvalidLineNumber, "").WithLocation(1, 17));

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
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",", "").WithLocation(1, 18));

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
                // (1,19): error CS1576: The line number specified for #line directive is missing or invalid
                // #line (1, 2) - (3,
                Diagnostic(ErrorCode.ERR_InvalidLineNumber, "").WithLocation(1, 19));

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
                // (1,8): error CS1576: The line number specified for #line directive is missing or invalid
                // #line (, 2) - (3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_InvalidLineNumber, ",").WithLocation(1, 8));

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
                Diagnostic(ErrorCode.ERR_SyntaxError, "2").WithArguments(",", "").WithLocation(1, 10));

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
                // (1,11): error CS1576: The line number specified for #line directive is missing or invalid
                // #line (1, ) - (3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_InvalidLineNumber, ")").WithLocation(1, 11));

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
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("-", "(").WithLocation(1, 14));

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
                Diagnostic(ErrorCode.ERR_SyntaxError, "3").WithArguments("(", "").WithLocation(1, 16));

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
                // (1,17): error CS1576: The line number specified for #line directive is missing or invalid
                // #line (1, 2) - (, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_InvalidLineNumber, ",").WithLocation(1, 17));

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
                Diagnostic(ErrorCode.ERR_SyntaxError, "4").WithArguments(",", "").WithLocation(1, 19));

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
                // (1,20): error CS1576: The line number specified for #line directive is missing or invalid
                // #line (1, 2) - (3, ) "file.cs"
                Diagnostic(ErrorCode.ERR_InvalidLineNumber, ")").WithLocation(1, 20));

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
                // (1,8): error CS1576: The line number specified for #line directive is missing or invalid
                // #line ('1', 2) - (3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_InvalidLineNumber, "'").WithLocation(1, 8));

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
                // (1,11): error CS1576: The line number specified for #line directive is missing or invalid
                // #line (1, "2") - (3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_InvalidLineNumber, @"""2""").WithLocation(1, 11));

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
                // (1,18): error CS1003: Syntax error, ',' expected
                // #line (1, 2) - (0b11, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_SyntaxError, "b11").WithArguments(",", "").WithLocation(1, 18));

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
                // (1,21): error CS1026: ) expected
                // #line (1, 2) - (3, 0x04) "file.cs"
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "x04").WithLocation(1, 21));

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
                // (1,8): error CS1576: The line number specified for #line directive is missing or invalid
                // #line (null, 2) - (3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_InvalidLineNumber, "null").WithLocation(1, 8));

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
                // (1,11): error CS1576: The line number specified for #line directive is missing or invalid
                // #line (1, true) - (3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_InvalidLineNumber, "true").WithLocation(1, 11));

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
                // (1,17): error CS1576: The line number specified for #line directive is missing or invalid
                // #line (1, 2) - (int, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_InvalidLineNumber, "int").WithLocation(1, 17));

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
                Diagnostic(ErrorCode.ERR_SyntaxError, "u").WithArguments(",", "").WithLocation(1, 9));

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
        public void UnexpectedToken_11()
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
        public void InvalidValue_01()
        {
            string source = @"#line (-1, 2) - (3, 4) ""file.cs""";

            UsingLineDirective(source, options: null,
                // (1,8): error CS1576: The line number specified for #line directive is missing or invalid
                // #line (-1, 2) - (3, 4) "file.cs"
                Diagnostic(ErrorCode.ERR_InvalidLineNumber, "-").WithLocation(1, 8));

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
        public void InvalidValue_02()
        {
            string source = @"#line (0, 0) - (0, 0) 0 ""file.cs""";

            UsingLineDirective(source, options: null);

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
    }
}
