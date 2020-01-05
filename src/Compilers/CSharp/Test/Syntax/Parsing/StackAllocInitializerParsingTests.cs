// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing
{
    [CompilerTrait(CompilerFeature.StackAllocInitializer)]
    public class StackAllocInitializerParsingTests : ParsingTests
    {
        public StackAllocInitializerParsingTests(ITestOutputHelper output) : base(output) { }

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
                // (1,12): error CS8381: "Invalid rank specifier: expected ']'
                // stackalloc[1] { 42 }
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, "1").WithLocation(1, 12)
                );
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
        public void StackAllocInitializer_05()
        {
            var test = @"
class C {
    void Goo() {
        var x = stackalloc[3] { 1, 2, 3 };
    }
}
";

            ParseAndValidate(test,
                // (4,28): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, "3").WithLocation(4, 28)
                );
        }

        [Fact]
        public void StackAllocInitializer_06()
        {
            var test = @"
class C {
    void Goo() {
        var x = stackalloc[3,] { 1, 2, 3 };
    }
}
";

            ParseAndValidate(test,
                // (4,28): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[3,] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, "3").WithLocation(4, 28),
                // (4,29): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[3,] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, ",").WithLocation(4, 29)
                );
        }

        [Fact]
        public void StackAllocInitializer_07()
        {
            var test = @"
class C {
    void Goo() {
        var x = stackalloc[,3] { 1, 2, 3 };
    }
}
";

            ParseAndValidate(test,
                // (4,29): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[,3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, "3").WithLocation(4, 29),
                // (4,28): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[,3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, ",").WithLocation(4, 28)
                );
        }

        [Fact]
        public void StackAllocInitializer_08()
        {
            var test = @"
class C {
    void Goo() {
        var x = stackalloc[,3 { 1, 2, 3 };
    }
}
";

            ParseAndValidate(test,
                // (4,29): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[,3 { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, "3").WithLocation(4, 29),
                // (4,28): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[,3 { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, ",").WithLocation(4, 28),
                // (4,31): error CS1003: Syntax error, ']' expected
                //         var x = stackalloc[,3 { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments("]", "{").WithLocation(4, 31)
                );
        }

        [Fact]
        public void StackAllocInitializer_09()
        {
            var test = @"
class C {
    void Goo() {
        var x = stackalloc[3 { 1, 2, 3 };
    }
}
";

            ParseAndValidate(test,
                // (4,28): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[3 { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, "3").WithLocation(4, 28),
                // (4,30): error CS1003: Syntax error, ']' expected
                //         var x = stackalloc[3 { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments("]", "{").WithLocation(4, 30)
                );
        }

        [Fact]
        public void StackAllocInitializer_10()
        {
            var test = @"
class C {
    void Goo() {
        var x = stackalloc[3, { 1, 2, 3 };
    }
}
";

            ParseAndValidate(test,
                // (4,28): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[3, { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, "3").WithLocation(4, 28),
                // (4,29): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[3, { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, ",").WithLocation(4, 29),
                // (4,31): error CS1003: Syntax error, ']' expected
                //         var x = stackalloc[3, { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments("]", "{").WithLocation(4, 31)
                );
        }

        [Fact]
        public void StackAllocInitializer_11()
        {
            var test = @"
class C {
    void Goo() {
        var x = stackalloc[3,,] { 1, 2, 3 };
    }
}
";

            ParseAndValidate(test,
                // (4,28): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[3,,] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, "3").WithLocation(4, 28),
                // (4,29): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[3,,] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, ",").WithLocation(4, 29),
                // (4,30): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[3,,] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, ",").WithLocation(4, 30)
                );
        }

        [Fact]
        public void StackAllocInitializer_12()
        {
            var test = @"
class C {
    void Goo() {
        var x = stackalloc[,3,] { 1, 2, 3 };
    }
}
";

            ParseAndValidate(test,
                // (4,29): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[,3,] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, "3").WithLocation(4, 29),
                // (4,28): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[,3,] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, ",").WithLocation(4, 28),
                // (4,30): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[,3,] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, ",").WithLocation(4, 30)
                );
        }

        [Fact]
        public void StackAllocInitializer_13()
        {
            var test = @"
class C {
    void Goo() {
        var x = stackalloc[,,3] { 1, 2, 3 };
    }
}
";

            ParseAndValidate(test,
                // (4,30): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[,,3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, "3").WithLocation(4, 30),
                // (4,28): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[,,3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, ",").WithLocation(4, 28),
                // (4,29): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[,,3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, ",").WithLocation(4, 29)
                );
        }

        [Fact]
        public void StackAllocInitializer_14()
        {
            var test = @"
class C {
    void Goo() {
        var x = stackalloc[3,,3] { 1, 2, 3 };
    }
}
";

            ParseAndValidate(test,
                // (4,28): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[3,,3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, "3").WithLocation(4, 28),
                // (4,31): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[3,,3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, "3").WithLocation(4, 31),
                // (4,29): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[3,,3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, ",").WithLocation(4, 29),
                // (4,30): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = stackalloc[3,,3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, ",").WithLocation(4, 30)
                );
        }

        [Fact]
        public void StackAllocInitializer_15()
        {
            var test = @"
class C {
    void Goo() {
        var x = stackalloc[ { 1, 2, 3 };
    }
}
";

            ParseAndValidate(test,
                // (4,29): error CS1003: Syntax error, ']' expected
                //         var x = stackalloc[ { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments("]", "{").WithLocation(4, 29)
                );
        }
    }
}
