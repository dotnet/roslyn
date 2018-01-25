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
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "stackalloc").WithArguments("stackalloc initilizer", "7.3").WithLocation(1, 1));
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
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "stackalloc").WithArguments("stackalloc initilizer", "7.3").WithLocation(1, 1));
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
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "stackalloc").WithArguments("stackalloc initilizer", "7.3").WithLocation(1, 1));
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
    }
}
