// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing;

public sealed class WithElementParsingTests(ITestOutputHelper output) : ParsingTests(output)
{
    public static readonly TheoryData<LanguageVersion> CollectionArgumentsLanguageVersions = new([LanguageVersion.CSharp14, LanguageVersion.Preview, LanguageVersionFacts.CSharpNext]);

    private void CollectionArgumentsOrInvocation(LanguageVersion languageVersion)
    {
        if (languageVersion > LanguageVersion.CSharp14)
        {
            N(SyntaxKind.WithElement);
            N(SyntaxKind.WithKeyword);
        }
        else
        {
            N(SyntaxKind.ExpressionElement);
            N(SyntaxKind.InvocationExpression);
            N(SyntaxKind.IdentifierName);
            N(SyntaxKind.IdentifierToken, "with");
        }
    }

    [Fact]
    public void TestSyntaxFacts()
    {
        Assert.Equal(SyntaxKind.WithKeyword, SyntaxFacts.GetContextualKeywordKind("with"));
        Assert.Equal(SyntaxKind.None, SyntaxFacts.GetKeywordKind("with"));
        Assert.True(SyntaxFacts.IsContextualKeyword(SyntaxKind.WithKeyword));
        Assert.Equal("with", SyntaxFacts.GetText(SyntaxKind.WithKeyword));
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement1(LanguageVersion languageVersion)
    {
        UsingExpression("[with]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "with");
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement2(LanguageVersion languageVersion)
    {
        UsingExpression("[with: with]",
            TestOptions.Regular.WithLanguageVersion(languageVersion),
            // (1,6): error CS1003: Syntax error, ',' expected
            // [with: with]
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 6),
            // (1,8): error CS1003: Syntax error, ',' expected
            // [with: with]
            Diagnostic(ErrorCode.ERR_SyntaxError, "with").WithArguments(",").WithLocation(1, 8));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "with");
                }
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "with");
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement3(LanguageVersion languageVersion)
    {
        UsingExpression("[.. with]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.SpreadElement);
            {
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "with");
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement4(LanguageVersion languageVersion)
    {
        UsingExpression("[with + with]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.AddExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "with");
                    }
                    N(SyntaxKind.PlusToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "with");
                    }
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement5(LanguageVersion languageVersion)
    {
        UsingExpression("[with.X]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.SimpleMemberAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "with");
                    }
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement6(LanguageVersion languageVersion)
    {
        UsingExpression("[with[X]]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.ElementAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "with");
                    }
                    N(SyntaxKind.BracketedArgumentList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Argument);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "X");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement7(LanguageVersion languageVersion)
    {
        UsingExpression("[with ? with : with]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.ConditionalExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "with");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "with");
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "with");
                    }
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement8(LanguageVersion languageVersion)
    {
        UsingExpression("[with?.with]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.ConditionalAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "with");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.MemberBindingExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "with");
                        }
                    }
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement9(LanguageVersion languageVersion)
    {
        UsingExpression("[with++]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.PostIncrementExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "with");
                    }
                    N(SyntaxKind.PlusPlusToken);
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement10(LanguageVersion languageVersion)
    {
        UsingExpression("[with)]",
            TestOptions.Regular.WithLanguageVersion(languageVersion),
            // (1,6): error CS1003: Syntax error, ',' expected
            // [with)]
            Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(1, 6));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "with");
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement11(LanguageVersion languageVersion)
    {
        UsingExpression("[with..with]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.RangeExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "with");
                    }
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "with");
                    }
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement12(LanguageVersion languageVersion)
    {
        UsingExpression("[with..with()]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.RangeExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "with");
                    }
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "with");
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement13(LanguageVersion languageVersion)
    {
        UsingExpression("[@with()]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.InvocationExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "@with");
                    }
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement14(LanguageVersion languageVersion)
    {
        UsingExpression("with()",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.InvocationExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "with");
            }
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement15(LanguageVersion languageVersion)
    {
        UsingExpression("a with()",
            TestOptions.Regular.WithLanguageVersion(languageVersion),
            // (1,1): error CS1073: Unexpected token 'with'
            // a with()
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "a").WithArguments("with").WithLocation(1, 1));

        N(SyntaxKind.IdentifierName);
        {
            N(SyntaxKind.IdentifierToken, "a");
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement16(LanguageVersion languageVersion)
    {
        UsingExpression("[with()] a => b",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.SimpleLambdaExpression);
        {
            N(SyntaxKind.AttributeList);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.Attribute);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "with");
                    }
                    N(SyntaxKind.AttributeArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            N(SyntaxKind.Parameter);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.EqualsGreaterThanToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "b");
            }
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement17(LanguageVersion languageVersion)
    {
        UsingExpression("[with()] async a => b",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.SimpleLambdaExpression);
        {
            N(SyntaxKind.AttributeList);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.Attribute);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "with");
                    }
                    N(SyntaxKind.AttributeArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.Parameter);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.EqualsGreaterThanToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "b");
            }
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement18(LanguageVersion languageVersion)
    {
        UsingExpression("[with()] (a) => b",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.ParenthesizedLambdaExpression);
        {
            N(SyntaxKind.AttributeList);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.Attribute);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "with");
                    }
                    N(SyntaxKind.AttributeArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Parameter);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.EqualsGreaterThanToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "b");
            }
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement19(LanguageVersion languageVersion)
    {
        UsingExpression("[a.with()]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.InvocationExpression);
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
                            N(SyntaxKind.IdentifierToken, "with");
                        }
                    }
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement20(LanguageVersion languageVersion)
    {
        UsingExpression("[(with)()]",
            TestOptions.Regular.WithLanguageVersion(languageVersion),
            // (1,9): error CS1525: Invalid expression term ')'
            // [(with)()]
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(1, 9));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.CastExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "with");
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.ParenthesizedExpression);
                    {
                        N(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement21(LanguageVersion languageVersion)
    {
        UsingExpression("[(with)(with)]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.CastExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "with");
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.ParenthesizedExpression);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "with");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement22(LanguageVersion languageVersion)
    {
        UsingExpression("[(with)]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.ParenthesizedExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "with");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void NotWithElement23(LanguageVersion languageVersion)
    {
        UsingExpression("[(with())]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.ParenthesizedExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "with");
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement1(LanguageVersion languageVersion)
    {
        UsingExpression("[with(]",
            TestOptions.Regular.WithLanguageVersion(languageVersion),
            // (1,7): error CS1026: ) expected
            // [with(]
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "]").WithLocation(1, 7),
            // (1,8): error CS1003: Syntax error, ']' expected
            // [with(]
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("]").WithLocation(1, 8));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                M(SyntaxKind.CloseParenToken);
            }
            M(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement2(LanguageVersion languageVersion)
    {
        UsingExpression("[with()]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement3(LanguageVersion languageVersion)
    {
        UsingExpression("[with(,)]",
            TestOptions.Regular.WithLanguageVersion(languageVersion),
            // (1,7): error CS0839: Argument missing
            // [with(,)]
            Diagnostic(ErrorCode.ERR_MissingArgument, ",").WithLocation(1, 7),
            // (1,8): error CS1525: Invalid expression term ')'
            // [with(,)]
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(1, 8));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                M(SyntaxKind.Argument);
                {
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.CommaToken);
                M(SyntaxKind.Argument);
                {
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement4(LanguageVersion languageVersion)
    {
        UsingExpression("[with(a)]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement5(LanguageVersion languageVersion)
    {
        UsingExpression("[with(ref a)]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement6(LanguageVersion languageVersion)
    {
        UsingExpression("[with(out a)]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.OutKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement7(LanguageVersion languageVersion)
    {
        UsingExpression("[with(out var a)]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.OutKeyword);
                    N(SyntaxKind.DeclarationExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "var");
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement8(LanguageVersion languageVersion)
    {
        UsingExpression("[with(name: value)]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.NameColon);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "name");
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "value");
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement9(LanguageVersion languageVersion)
    {
        UsingExpression("[with(a, b)]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement10(LanguageVersion languageVersion)
    {
        UsingExpression("[with(), with()]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CommaToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement11(LanguageVersion languageVersion)
    {
        UsingExpression("[a, with()]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
            }
            N(SyntaxKind.CommaToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement12(LanguageVersion languageVersion)
    {
        UsingExpression("[a:b, with()]",
            TestOptions.Regular.WithLanguageVersion(languageVersion),
            // (1,3): error CS1003: Syntax error, ',' expected
            // [a:b, with()]
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 3),
            // (1,4): error CS1003: Syntax error, ',' expected
            // [a:b, with()]
            Diagnostic(ErrorCode.ERR_SyntaxError, "b").WithArguments(",").WithLocation(1, 4));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
            }
            N(SyntaxKind.CommaToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement13(LanguageVersion languageVersion)
    {
        UsingExpression("[..a, with()]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.SpreadElement);
            {
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
            }
            N(SyntaxKind.CommaToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement14(LanguageVersion languageVersion)
    {
        UsingExpression("[with(), a]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement15(LanguageVersion languageVersion)
    {
        UsingExpression("[with(), a:b]",
            TestOptions.Regular.WithLanguageVersion(languageVersion),
            // (1,11): error CS1003: Syntax error, ',' expected
            // [with(), a:b]
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 11),
            // (1,12): error CS1003: Syntax error, ',' expected
            // [with(), a:b]
            Diagnostic(ErrorCode.ERR_SyntaxError, "b").WithArguments(",").WithLocation(1, 12));

        if (languageVersion == LanguageVersion.CSharp14)
        {
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "with");
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }
        else
        {
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.WithElement);
                {
                    N(SyntaxKind.WithKeyword);
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement16(LanguageVersion languageVersion)
    {
        UsingExpression("[with(), ..a]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.SpreadElement);
            {
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement17(LanguageVersion languageVersion)
    {
        UsingExpression("[with([])]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.CollectionExpression);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement18(LanguageVersion languageVersion)
    {
        UsingExpression("[with(() => {})]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.ParenthesizedLambdaExpression);
                    {
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement19(LanguageVersion languageVersion)
    {
        UsingExpression("[with(async () => {})]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.ParenthesizedLambdaExpression);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement20(LanguageVersion languageVersion)
    {
        UsingExpression("[with(from x in y select x)]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.QueryExpression);
                    {
                        N(SyntaxKind.FromClause);
                        {
                            N(SyntaxKind.FromKeyword);
                            N(SyntaxKind.IdentifierToken, "x");
                            N(SyntaxKind.InKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                        }
                        N(SyntaxKind.QueryBody);
                        {
                            N(SyntaxKind.SelectClause);
                            {
                                N(SyntaxKind.SelectKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                            }
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement21(LanguageVersion languageVersion)
    {
        UsingExpression("[with([with()])]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.CollectionExpression);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        CollectionArgumentsOrInvocation(languageVersion);
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement22(LanguageVersion languageVersion)
    {
        UsingExpression("[with(with: with)]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.NameColon);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "with");
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "with");
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement23(LanguageVersion languageVersion)
    {
        UsingExpression("[with(out _)]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.OutKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement24(LanguageVersion languageVersion)
    {
        UsingExpression("[with(in a)]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.InKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement25(LanguageVersion languageVersion)
    {
        UsingExpression("[with(name: ref a)]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.NameColon);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "name");
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement26(LanguageVersion languageVersion)
    {
        UsingExpression("[with(ref int () => { })]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.ParenthesizedLambdaExpression);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                        }
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement27(LanguageVersion languageVersion)
    {
        var expectedDiagnostics = (languageVersion == LanguageVersion.CSharp14) ?
            [] :
            new[]
            {
                // (1,8): error CS1003: Syntax error, ',' expected
                // [with()..x]
                Diagnostic(ErrorCode.ERR_SyntaxError, ".").WithArguments(",").WithLocation(1, 8)
            };
        UsingExpression("[with()..x]",
            TestOptions.Regular.WithLanguageVersion(languageVersion),
            expectedDiagnostics);

        if (languageVersion == LanguageVersion.CSharp14)
        {
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.RangeExpression);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "with");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.DotDotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }
        else
        {
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.WithElement);
                {
                    N(SyntaxKind.WithKeyword);
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.SpreadElement);
                {
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement28(LanguageVersion languageVersion)
    {
        var expectedDiagnostics = (languageVersion == LanguageVersion.CSharp14) ?
            [] :
            new[]
            {
                // (1,8): error CS1003: Syntax error, ',' expected
                // [with().x]
                Diagnostic(ErrorCode.ERR_SyntaxError, ".").WithArguments(",").WithLocation(1, 8),
                // (1,9): error CS1003: Syntax error, ',' expected
                // [with().x]
                Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments(",").WithLocation(1, 9)
            };
        UsingExpression("[with().x]",
            TestOptions.Regular.WithLanguageVersion(languageVersion),
            expectedDiagnostics);

        if (languageVersion == LanguageVersion.CSharp14)
        {
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.SimpleMemberAccessExpression);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "with");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }
        else
        {
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.WithElement);
                {
                    N(SyntaxKind.WithKeyword);
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement29(LanguageVersion languageVersion)
    {
        UsingExpression("[with()",
            TestOptions.Regular.WithLanguageVersion(languageVersion),
            // (1,8): error CS1003: Syntax error, ']' expected
            // [with()
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("]").WithLocation(1, 8));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            M(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement30(LanguageVersion languageVersion)
    {
        UsingExpression("[with(),",
            TestOptions.Regular.WithLanguageVersion(languageVersion),
            // (1,9): error CS1003: Syntax error, ']' expected
            // [with(),
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("]").WithLocation(1, 9));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CommaToken);
            M(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement31(LanguageVersion languageVersion)
    {
        UsingExpression("[with(_)]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement32(LanguageVersion languageVersion)
    {
        UsingExpression("[with(a,)]",
            TestOptions.Regular.WithLanguageVersion(languageVersion),
            // (1,9): error CS1525: Invalid expression term ')'
            // [with(a,)]
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(1, 9));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.CommaToken);
                    M(SyntaxKind.Argument);
                    {
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement33(LanguageVersion languageVersion)
    {
        UsingExpression("[with(,a)]",
            TestOptions.Regular.WithLanguageVersion(languageVersion),
            // (1,7): error CS0839: Argument missing
            // [with(,a)]
            Diagnostic(ErrorCode.ERR_MissingArgument, ",").WithLocation(1, 7));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            CollectionArgumentsOrInvocation(languageVersion);
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                M(SyntaxKind.Argument);
                {
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement34(LanguageVersion languageVersion)
    {
        UsingExpression("[with():y]",
            TestOptions.Regular.WithLanguageVersion(languageVersion),
            // (1,8): error CS1003: Syntax error, ',' expected
            // [with():y]
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 8),
            // (1,9): error CS1003: Syntax error, ',' expected
            // [with():y]
            Diagnostic(ErrorCode.ERR_SyntaxError, "y").WithArguments(",").WithLocation(1, 9));

        if (languageVersion == LanguageVersion.CSharp14)
        {
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "with");
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "y");
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }
        else
        {
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.WithElement);
                {
                    N(SyntaxKind.WithKeyword);
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "y");
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement35(LanguageVersion languageVersion)
    {
        UsingExpression("[x:with()]",
            TestOptions.Regular.WithLanguageVersion(languageVersion),
            // (1,3): error CS1003: Syntax error, ',' expected
            // [x:with()]
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 3),
            // (1,4): error CS1003: Syntax error, ',' expected
            // [x:with()]
            Diagnostic(ErrorCode.ERR_SyntaxError, "with").WithArguments(",").WithLocation(1, 4));

        if (languageVersion == LanguageVersion.CSharp14)
        {
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "with");
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }
        else
        {
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.WithElement);
                {
                    N(SyntaxKind.WithKeyword);
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement36(LanguageVersion languageVersion)
    {
        UsingExpression("[..with()]",
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.SpreadElement);
            {
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.InvocationExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "with");
                    }
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement37(LanguageVersion languageVersion)
    {
        var expectedDiagnostics = (languageVersion == LanguageVersion.CSharp14) ?
            [] :
            new[]
            {
                // (1,8): error CS1003: Syntax error, ',' expected
                // [with()++]
                Diagnostic(ErrorCode.ERR_SyntaxError, "++").WithArguments(",").WithLocation(1, 8),
                // (1,10): error CS1525: Invalid expression term ']'
                // [with()++]
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "]").WithArguments("]").WithLocation(1, 10),
            };
        UsingExpression("[with()++]",
            TestOptions.Regular.WithLanguageVersion(languageVersion),
            expectedDiagnostics);

        if (languageVersion == LanguageVersion.CSharp14)
        {
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.PostIncrementExpression);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "with");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.PlusPlusToken);
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }
        else
        {
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.WithElement);
                {
                    N(SyntaxKind.WithKeyword);
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.PreIncrementExpression);
                    {
                        N(SyntaxKind.PlusPlusToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement38(LanguageVersion languageVersion)
    {
        var expectedDiagnostics = (languageVersion == LanguageVersion.CSharp14) ?
            [] :
            new[]
            {
                // (1,8): error CS1003: Syntax error, ',' expected
                // [with()[0]]
                Diagnostic(ErrorCode.ERR_SyntaxError, "[").WithArguments(",").WithLocation(1, 8)
            };
        UsingExpression("[with()[0]]",
            TestOptions.Regular.WithLanguageVersion(languageVersion),
            expectedDiagnostics);

        if (languageVersion == LanguageVersion.CSharp14)
        {
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.ElementAccessExpression);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "with");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
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
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }
        else
        {
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.WithElement);
                {
                    N(SyntaxKind.WithKeyword);
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.CollectionExpression);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.ExpressionElement);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "0");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement39(LanguageVersion languageVersion)
    {
        UsingTree("""
            void M()
            {
                var v = [await with()];
            }
            """,
            TestOptions.Regular.WithLanguageVersion(languageVersion),
            // (3,20): error CS1003: Syntax error, ',' expected
            //     var v = [await with()];
            Diagnostic(ErrorCode.ERR_SyntaxError, "with").WithArguments(",").WithLocation(3, 20));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.LocalDeclarationStatement);
                        {
                            N(SyntaxKind.VariableDeclaration);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "var");
                                }
                                N(SyntaxKind.VariableDeclarator);
                                {
                                    N(SyntaxKind.IdentifierToken, "v");
                                    N(SyntaxKind.EqualsValueClause);
                                    {
                                        N(SyntaxKind.EqualsToken);
                                        N(SyntaxKind.CollectionExpression);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.ExpressionElement);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "await");
                                                }
                                            }
                                            M(SyntaxKind.CommaToken);
                                            CollectionArgumentsOrInvocation(languageVersion);
                                            N(SyntaxKind.ArgumentList);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                N(SyntaxKind.CloseParenToken);
                                            }
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                    }
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(CollectionArgumentsLanguageVersions))]
    public void WithElement40(LanguageVersion languageVersion)
    {
        UsingTree("""
            async void M()
            {
                var v = [await with()];
            }
            """,
            TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
                {
                    N(SyntaxKind.AsyncKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.LocalDeclarationStatement);
                        {
                            N(SyntaxKind.VariableDeclaration);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "var");
                                }
                                N(SyntaxKind.VariableDeclarator);
                                {
                                    N(SyntaxKind.IdentifierToken, "v");
                                    N(SyntaxKind.EqualsValueClause);
                                    {
                                        N(SyntaxKind.EqualsToken);
                                        N(SyntaxKind.CollectionExpression);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.ExpressionElement);
                                            {
                                                N(SyntaxKind.AwaitExpression);
                                                {
                                                    N(SyntaxKind.AwaitKeyword);
                                                    N(SyntaxKind.InvocationExpression);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "with");
                                                        }
                                                        N(SyntaxKind.ArgumentList);
                                                        {
                                                            N(SyntaxKind.OpenParenToken);
                                                            N(SyntaxKind.CloseParenToken);
                                                        }
                                                    }
                                                }
                                            }
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                    }
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }
}
