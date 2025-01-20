// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.Patterns)]
    public class PatternParsingTests2 : ParsingTests
    {
        private new void UsingExpression(string text, params DiagnosticDescription[] expectedErrors)
        {
            UsingExpression(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), expectedErrors);
        }

        public PatternParsingTests2(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void ExtendedPropertySubpattern_01()
        {
            var test = @"e is { a.b.c: p }";
            var testWithStatement = @$"class C {{ void M() {{ var v = {test}; }} }}";

            CreateCompilation(testWithStatement, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (1,30): error CS0103: The name 'e' does not exist in the current context
                // class C { void M() { var v = e is { a.b.c: p }; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(1, 30),
                // (1,44): error CS0103: The name 'p' does not exist in the current context
                // class C { void M() { var v = e is { a.b.c: p }; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "p").WithArguments("p").WithLocation(1, 44));
            CreateCompilation(testWithStatement, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,30): error CS0103: The name 'e' does not exist in the current context
                // class C { void M() { var v = e is { a.b.c: p }; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(1, 30),
                // (1,42): error CS8773: Feature 'extended property patterns' is not available in C# 9.0. Please use language version 10.0 or greater.
                // class C { void M() { var v = e is { a.b.c: p }; } }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, ":").WithArguments("extended property patterns", "10.0").WithLocation(1, 42),
                // (1,44): error CS0103: The name 'p' does not exist in the current context
                // class C { void M() { var v = e is { a.b.c: p }; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "p").WithArguments("p").WithLocation(1, 44));

            UsingExpression(test, TestOptions.Regular10);
            verify();

            UsingExpression(test, TestOptions.Regular9);
            verify();

            void verify()
            {

                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.PropertyPatternClause);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.Subpattern);
                            {
                                N(SyntaxKind.ExpressionColon);
                                {
                                    N(SyntaxKind.SimpleMemberAccessExpression);
                                    {
                                        N(SyntaxKind.SimpleMemberAccessExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "a");
                                            }
                                            N(SyntaxKind.DotToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "b");
                                            }
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "c");
                                        }
                                    }
                                    N(SyntaxKind.ColonToken);
                                }
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "p");
                                    }
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                EOF();
            }
        }

        [Fact]
        public void ExtendedPropertySubpattern_02()
        {
            UsingExpression(@"e is { {}: p }",
                // (1,10): error CS1003: Syntax error, ',' expected
                // e is { {}: p }
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 10),
                // (1,12): error CS1003: Syntax error, ',' expected
                // e is { {}: p }
                Diagnostic(ErrorCode.ERR_SyntaxError, "p").WithArguments(",").WithLocation(1, 12));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.PropertyPatternClause);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "p");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ExtendedPropertySubpattern_03()
        {
            UsingExpression(@"e is { name<T>: p }");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.ExpressionColon);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "name");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "T");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "p");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ExtendedPropertySubpattern_04()
        {
            UsingExpression(@"e is { name[0]: p }",
                    // (1,15): error CS1003: Syntax error, ',' expected
                    // e is { name[0]: p }
                    Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 15),
                    // (1,17): error CS1003: Syntax error, ',' expected
                    // e is { name[0]: p }
                    Diagnostic(ErrorCode.ERR_SyntaxError, "p").WithArguments(",").WithLocation(1, 17));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.TypePattern);
                            {
                                N(SyntaxKind.ArrayType);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "name");
                                    }
                                    N(SyntaxKind.ArrayRankSpecifier);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "0");
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                }
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "p");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ExtendedPropertySubpattern_05()
        {
            UsingExpression(@"e is { a?.b: p }");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.ExpressionColon);
                            {
                                N(SyntaxKind.ConditionalAccessExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                    N(SyntaxKind.QuestionToken);
                                    N(SyntaxKind.MemberBindingExpression);
                                    {
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
                                    }
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "p");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ExtendedPropertySubpattern_06()
        {
            UsingExpression(@"e is { a->c: p }");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.ExpressionColon);
                            {
                                N(SyntaxKind.PointerMemberAccessExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                    N(SyntaxKind.MinusGreaterThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "c");
                                    }
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "p");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ExtendedPropertySubpattern_07()
        {
            UsingExpression(@"e is { [0]: p }",
                // (1,11): error CS1003: Syntax error, ',' expected
                // e is { [0]: p }
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 11),
                // (1,13): error CS1003: Syntax error, ',' expected
                // e is { [0]: p }
                Diagnostic(ErrorCode.ERR_SyntaxError, "p").WithArguments(",").WithLocation(1, 13));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.ListPattern);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "0");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "p");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ExtendedPropertySubpattern_08()
        {
            UsingExpression(@"e is { not a: p }",
                    // (1,13): error CS1003: Syntax error, ',' expected
                    // e is { not a: p }
                    Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 13),
                    // (1,15): error CS1003: Syntax error, ',' expected
                    // e is { not a: p }
                    Diagnostic(ErrorCode.ERR_SyntaxError, "p").WithArguments(",").WithLocation(1, 15));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NotPattern);
                            {
                                N(SyntaxKind.NotKeyword);
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                }
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "p");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ExtendedPropertySubpattern_09()
        {
            UsingExpression(@"e is { x or y: p }",
                    // (1,14): error CS1003: Syntax error, ',' expected
                    // e is { x or y: p }
                    Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 14),
                    // (1,16): error CS1003: Syntax error, ',' expected
                    // e is { x or y: p }
                    Diagnostic(ErrorCode.ERR_SyntaxError, "p").WithArguments(",").WithLocation(1, 16));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.OrPattern);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                }
                                N(SyntaxKind.OrKeyword);
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "y");
                                    }
                                }
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "p");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ExtendedPropertySubpattern_10()
        {
            UsingExpression(@"e is { 1: p }");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.ExpressionColon);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "1");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "p");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ExtendedPropertySubpattern_11()
        {
            UsingExpression(@"e is { >1: p }",
                    // (1,10): error CS1003: Syntax error, ',' expected
                    // e is { >1: p }
                    Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 10),
                    // (1,12): error CS1003: Syntax error, ',' expected
                    // e is { >1: p }
                    Diagnostic(ErrorCode.ERR_SyntaxError, "p").WithArguments(",").WithLocation(1, 12));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.RelationalPattern);
                            {
                                N(SyntaxKind.GreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "1");
                                }
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "p");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ExtendedPropertySubpattern_12()
        {
            UsingExpression(@"e is { a!.b: p }");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.ExpressionColon);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.SuppressNullableWarningExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                        N(SyntaxKind.ExclamationToken);
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "b");
                                    }
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "p");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ExtendedPropertySubpattern_13()
        {
            UsingExpression(@"e is { a[0].b: p }");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.ExpressionColon);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.ElementAccessExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                        N(SyntaxKind.BracketedArgumentList);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.Argument);
                                            {
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "0");
                                                }
                                            }
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "b");
                                    }
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "p");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ExtendedPropertySubpattern_14()
        {
            UsingExpression(@"e is { [0].b: p }",
                // (1,11): error CS1003: Syntax error, ',' expected
                // e is { [0].b: p }
                Diagnostic(ErrorCode.ERR_SyntaxError, ".").WithArguments(",").WithLocation(1, 11),
                // (1,12): error CS1003: Syntax error, ',' expected
                // e is { [0].b: p }
                Diagnostic(ErrorCode.ERR_SyntaxError, "b").WithArguments(",").WithLocation(1, 12));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.ListPattern);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "0");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "p");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ExtendedPropertySubpattern_15()
        {
            UsingExpression(@"e is { (c?a:b): p }");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.ExpressionColon);
                            {
                                N(SyntaxKind.ParenthesizedExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.ConditionalExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "c");
                                        }
                                        N(SyntaxKind.QuestionToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                        N(SyntaxKind.ColonToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "p");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ExtendedPropertySubpattern_InPositionalPattern()
        {
            UsingExpression(@"e is ( a.b.c: p )");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PositionalPatternClause);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.ExpressionColon);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.SimpleMemberAccessExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "c");
                                    }
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "p");
                                }
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
            EOF();
        }

        #region Missing > in type parameter list

        [Fact]
        public void MissingClosingAngleBracket01()
        {
            UsingExpression(@"e is List<int",
                // (1,11): error CS1525: Invalid expression term 'int'
                // e is List<int
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(1, 11));

            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "List");
                    }
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void MissingClosingAngleBracket02()
        {
            UsingExpression(@"e is List<int or IEnumerable<int",
                // (1,15): error CS1003: Syntax error, '>' expected
                // e is List<int or IEnumerable<int
                Diagnostic(ErrorCode.ERR_SyntaxError, "or").WithArguments(">").WithLocation(1, 15),
                // (1,30): error CS1525: Invalid expression term 'int'
                // e is List<int or IEnumerable<int
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(1, 30));

            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.OrPattern);
                    {
                        N(SyntaxKind.TypePattern);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    M(SyntaxKind.GreaterThanToken);
                                }
                            }
                        }
                        N(SyntaxKind.OrKeyword);
                        N(SyntaxKind.ConstantPattern);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "IEnumerable");
                            }
                        }
                    }
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void MissingClosingAngleBracket03()
        {
            UsingExpression(@"e is List<int { Count: 4 }",
                // (1,1): error CS1073: Unexpected token '{'
                // e is List<int { Count: 4 }
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "e is List<int").WithArguments("{").WithLocation(1, 1),
                // (1,11): error CS1525: Invalid expression term 'int'
                // e is List<int { Count: 4 }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(1, 11));

            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "List");
                    }
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void MissingClosingAngleBracket04()
        {
            UsingExpression(@"e is not List<int and not IEnumerable<int",
                // (1,19): error CS1003: Syntax error, '>' expected
                // e is not List<int and not IEnumerable<int
                Diagnostic(ErrorCode.ERR_SyntaxError, "and").WithArguments(">").WithLocation(1, 19),
                // (1,39): error CS1525: Invalid expression term 'int'
                // e is not List<int and not IEnumerable<int
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(1, 39));

            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.AndPattern);
                    {
                        N(SyntaxKind.NotPattern);
                        {
                            N(SyntaxKind.NotKeyword);
                            N(SyntaxKind.TypePattern);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.IntKeyword);
                                        }
                                        M(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.AndKeyword);
                        N(SyntaxKind.NotPattern);
                        {
                            N(SyntaxKind.NotKeyword);
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "IEnumerable");
                                }
                            }
                        }
                    }
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void MissingClosingAngleBracket05()
        {
            UsingExpression(@"e is (not List<int and not IEnumerable<int) or List<int or (not IEnumerable<int)",
                // (1,20): error CS1003: Syntax error, '>' expected
                // e is (not List<int and not IEnumerable<int) or List<int or (not IEnumerable<int)
                Diagnostic(ErrorCode.ERR_SyntaxError, "and").WithArguments(">").WithLocation(1, 20),
                // (1,40): error CS1525: Invalid expression term 'int'
                // e is (not List<int and not IEnumerable<int) or List<int or (not IEnumerable<int)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(1, 40),
                // (1,57): error CS1003: Syntax error, '>' expected
                // e is (not List<int and not IEnumerable<int) or List<int or (not IEnumerable<int)
                Diagnostic(ErrorCode.ERR_SyntaxError, "or").WithArguments(">").WithLocation(1, 57),
                // (1,77): error CS1525: Invalid expression term 'int'
                // e is (not List<int and not IEnumerable<int) or List<int or (not IEnumerable<int)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(1, 77));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.OrPattern);
                {
                    N(SyntaxKind.OrPattern);
                    {
                        N(SyntaxKind.ParenthesizedPattern);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.AndPattern);
                            {
                                N(SyntaxKind.NotPattern);
                                {
                                    N(SyntaxKind.NotKeyword);
                                    N(SyntaxKind.TypePattern);
                                    {
                                        N(SyntaxKind.GenericName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "List");
                                            N(SyntaxKind.TypeArgumentList);
                                            {
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                                M(SyntaxKind.GreaterThanToken);
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.AndKeyword);
                                N(SyntaxKind.NotPattern);
                                {
                                    N(SyntaxKind.NotKeyword);
                                    N(SyntaxKind.ConstantPattern);
                                    {
                                        N(SyntaxKind.LessThanExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "IEnumerable");
                                            }
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                        }
                                    }
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.OrKeyword);
                        N(SyntaxKind.TypePattern);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    M(SyntaxKind.GreaterThanToken);
                                }
                            }
                        }
                    }
                    N(SyntaxKind.OrKeyword);
                    N(SyntaxKind.ParenthesizedPattern);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.NotPattern);
                        {
                            N(SyntaxKind.NotKeyword);
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.LessThanExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "IEnumerable");
                                    }
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void MissingClosingAngleBracket06()
        {
            UsingExpression(@"e is X<Y { Property: A<B a }",
                // (1,1): error CS1073: Unexpected token '{'
                // e is X<Y { Property: A<B a }
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "e is X<Y").WithArguments("{").WithLocation(1, 1));

            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Y");
                }
            }
            EOF();
        }

        [Fact]
        public void MissingClosingAngleBracket07()
        {
            UsingExpression(@"e is A.B<X or C.D<Y",
                // (1,12): error CS1003: Syntax error, '>' expected
                // e is A.B<X or C.D<Y
                Diagnostic(ErrorCode.ERR_SyntaxError, "or").WithArguments(">").WithLocation(1, 12));

            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.OrPattern);
                    {
                        N(SyntaxKind.TypePattern);
                        {
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "A");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "B");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "X");
                                        }
                                        M(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.OrKeyword);
                        N(SyntaxKind.ConstantPattern);
                        {
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "D");
                                }
                            }
                        }
                    }
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Y");
                }
            }
            EOF();
        }

        #endregion

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
        public void ExtendedPropertySubpattern_NullableType1()
        {
            UsingExpression("e is { Prop: Type? }");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Prop");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.TypePattern);
                            {
                                N(SyntaxKind.NullableType);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.QuestionToken);
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
        public void ExtendedPropertySubpattern_NullableType2()
        {
            UsingExpression("e is { Prop: Type? propVal }");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Prop");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.DeclarationPattern);
                            {
                                N(SyntaxKind.NullableType);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.QuestionToken);
                                }
                                N(SyntaxKind.SingleVariableDesignation);
                                {
                                    N(SyntaxKind.IdentifierToken, "propVal");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
        public void ExtendedPropertySubpattern_NullableType3()
        {
            UsingExpression("e is { Prop: Type? propVal, Prop2: int? val2 }");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Prop");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.DeclarationPattern);
                            {
                                N(SyntaxKind.NullableType);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.QuestionToken);
                                }
                                N(SyntaxKind.SingleVariableDesignation);
                                {
                                    N(SyntaxKind.IdentifierToken, "propVal");
                                }
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Prop2");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.DeclarationPattern);
                            {
                                N(SyntaxKind.NullableType);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.QuestionToken);
                                }
                                N(SyntaxKind.SingleVariableDesignation);
                                {
                                    N(SyntaxKind.IdentifierToken, "val2");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
        public void ExtendedPropertySubpattern_NullableType4()
        {
            UsingExpression("e is { Prop: Type? propVal Prop2: int? val2 }",
                // (1,28): error CS1003: Syntax error, ',' expected
                // e is { Prop: Type? propVal Prop2: int? val2 }
                Diagnostic(ErrorCode.ERR_SyntaxError, "Prop2").WithArguments(",").WithLocation(1, 28));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Prop");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.DeclarationPattern);
                            {
                                N(SyntaxKind.NullableType);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.QuestionToken);
                                }
                                N(SyntaxKind.SingleVariableDesignation);
                                {
                                    N(SyntaxKind.IdentifierToken, "propVal");
                                }
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Prop2");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.DeclarationPattern);
                            {
                                N(SyntaxKind.NullableType);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.QuestionToken);
                                }
                                N(SyntaxKind.SingleVariableDesignation);
                                {
                                    N(SyntaxKind.IdentifierToken, "val2");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
        public void ExtendedPropertySubpattern_NullableType5()
        {
            UsingExpression("e is { Prop: Type? or AnotherType? }");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Prop");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.OrPattern);
                            {
                                N(SyntaxKind.TypePattern);
                                {
                                    N(SyntaxKind.NullableType);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Type");
                                        }
                                        N(SyntaxKind.QuestionToken);
                                    }
                                }
                                N(SyntaxKind.OrKeyword);
                                N(SyntaxKind.TypePattern);
                                {
                                    N(SyntaxKind.NullableType);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "AnotherType");
                                        }
                                        N(SyntaxKind.QuestionToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
        public void ExtendedPropertySubpattern_NullableType6()
        {
            UsingExpression("e is { Prop: Type? t or AnotherType? a }");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Prop");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.OrPattern);
                            {
                                N(SyntaxKind.DeclarationPattern);
                                {
                                    N(SyntaxKind.NullableType);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Type");
                                        }
                                        N(SyntaxKind.QuestionToken);
                                    }
                                    N(SyntaxKind.SingleVariableDesignation);
                                    {
                                        N(SyntaxKind.IdentifierToken, "t");
                                    }
                                }
                                N(SyntaxKind.OrKeyword);
                                N(SyntaxKind.DeclarationPattern);
                                {
                                    N(SyntaxKind.NullableType);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "AnotherType");
                                        }
                                        N(SyntaxKind.QuestionToken);
                                    }
                                    N(SyntaxKind.SingleVariableDesignation);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }
    }
}
