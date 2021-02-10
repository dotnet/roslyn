using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class PatternParsingTests
    {
        [Fact]
        public void ListPattern_01()
        {
            UsingStatement(@"_ = c is [];",
                TestOptions.RegularWithPatternCombinators);

            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.ListPattern);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void ListPattern_02()
        {
            UsingStatement(@"_ = c is [[]];",
                TestOptions.RegularWithPatternCombinators);

            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.ListPattern);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.ListPattern);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_01()
        {
            UsingStatement(@"_ = c is [..];",
                TestOptions.RegularWithPatternCombinators);

            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.ListPattern);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.SlicePattern);
                            {
                                N(SyntaxKind.DotDotToken);
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_02()
        {
            UsingStatement(@"_ = c is [.. var x];",
                TestOptions.RegularWithPatternCombinators);

            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.ListPattern);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.SlicePattern);
                            {
                                N(SyntaxKind.DotDotToken);
                                N(SyntaxKind.VarPattern);
                                {
                                    N(SyntaxKind.VarKeyword);
                                    N(SyntaxKind.SingleVariableDesignation);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_03()
        {
            UsingStatement(@"_ = c is ..;",
                TestOptions.RegularWithPatternCombinators);

            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.SlicePattern);
                        {
                            N(SyntaxKind.DotDotToken);
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();

        }

        [Fact]
        public void SlicePattern_04()
        {
            UsingStatement(@"_ = c is ....;",
                TestOptions.RegularWithPatternCombinators,
                // (1,10): error CS8635: Unexpected character sequence '...'
                // _ = c is ....;
                Diagnostic(ErrorCode.ERR_TripleDotNotAllowed, "").WithLocation(1, 10));

            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.SlicePattern);
                        {
                            N(SyntaxKind.DotDotToken);
                            N(SyntaxKind.SlicePattern);
                            {
                                N(SyntaxKind.DotDotToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_05()
        {
            UsingStatement(@"_ = c is [..[]];",
                TestOptions.RegularWithPatternCombinators);

            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.ListPattern);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.SlicePattern);
                            {
                                N(SyntaxKind.DotDotToken);
                                N(SyntaxKind.ListPattern);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_06()
        {
            UsingStatement(@"_ = c is [.. not p];",
                TestOptions.RegularWithPatternCombinators);

            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.ListPattern);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.SlicePattern);
                            {
                                N(SyntaxKind.DotDotToken);
                                N(SyntaxKind.NotPattern);
                                {
                                    N(SyntaxKind.NotKeyword);
                                    N(SyntaxKind.ConstantPattern);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "p");
                                        }
                                    }
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }
    }
}
