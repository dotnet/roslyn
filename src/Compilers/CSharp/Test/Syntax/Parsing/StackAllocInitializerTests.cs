// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing
{
    [CompilerTrait(CompilerFeature.StackAllocInitializer)]
    public class StackAllocInitializerTests : ParsingTests
    {
        public StackAllocInitializerTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void StackAllocInitializer_01()
        {
            UsingExpression("stackalloc int[] { 42 }", options: TestOptions.Regular7,
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "stackalloc").WithArguments("stackalloc initializer", "7.3").WithLocation(1, 1));
            N(SyntaxKind.StackAllocArrayCreationExpression);
            {
                N(SyntaxKind.StackAllocKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.ArrayRankSpecifier);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.OmittedArraySizeExpression);
                        {
                            N(SyntaxKind.OmittedArraySizeExpressionToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
                N(SyntaxKind.ArrayInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "42");
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void StackAllocInitializer_02()
        {
            UsingExpression("stackalloc int[1] { 42 }", options: TestOptions.Regular7,
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "stackalloc").WithArguments("stackalloc initializer", "7.3").WithLocation(1, 1));
            N(SyntaxKind.StackAllocArrayCreationExpression);
            {
                N(SyntaxKind.StackAllocKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.ArrayRankSpecifier);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
                N(SyntaxKind.ArrayInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "42");
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void StackAllocInitializer_03()
        {
            UsingExpression("stackalloc[] { 42 }", options: TestOptions.Regular7,
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "stackalloc").WithArguments("stackalloc initializer", "7.3").WithLocation(1, 1));
            N(SyntaxKind.ImplicitStackAllocArrayCreationExpression);
            {
                N(SyntaxKind.StackAllocKeyword);
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.CloseBracketToken);
                N(SyntaxKind.ArrayInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "42");
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void StackAllocInitializer_04()
        {
            UsingExpression("stackalloc[1] { 42 }", options: TestOptions.Regular7,
                // (1,1): error CS8107: Feature 'stackalloc initializer' is not available in C# 7.0. Please use language version 7.3 or greater.
                // stackalloc[1] { 42 }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "stackalloc").WithArguments("stackalloc initializer", "7.3").WithLocation(1, 1),
                // (1,12): error CS1003: Syntax error, ']' expected
                // stackalloc[1] { 42 }
                Diagnostic(ErrorCode.ERR_SyntaxError, "1").WithArguments("]", "").WithLocation(1, 12),
                // (1,12): error CS1514: { expected
                // stackalloc[1] { 42 }
                Diagnostic(ErrorCode.ERR_LbraceExpected, "1").WithLocation(1, 12),
                // (1,13): error CS1003: Syntax error, ',' expected
                // stackalloc[1] { 42 }
                Diagnostic(ErrorCode.ERR_SyntaxError, "]").WithArguments(",", "]").WithLocation(1, 13),
                // (1,15): error CS1003: Syntax error, ',' expected
                // stackalloc[1] { 42 }
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",", "{").WithLocation(1, 15),
                // (1,21): error CS1513: } expected
                // stackalloc[1] { 42 }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 21));
            N(SyntaxKind.ImplicitStackAllocArrayCreationExpression);
            {
                N(SyntaxKind.StackAllocKeyword);
                N(SyntaxKind.OpenBracketToken);
                M(SyntaxKind.CloseBracketToken);
                N(SyntaxKind.ArrayInitializerExpression);
                {
                    M(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.ArrayInitializerExpression);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "42");
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }
    }
}
