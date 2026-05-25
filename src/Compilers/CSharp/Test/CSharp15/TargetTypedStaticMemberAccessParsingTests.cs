// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class TargetTypedStaticMemberAccessParsingTests : ParsingTests
{
    public TargetTypedStaticMemberAccessParsingTests(ITestOutputHelper output) : base(output) { }

    #region Bare forms

    [Theory]
    [InlineData(LanguageVersion.CSharp14)]
    [InlineData(LanguageVersion.Preview)]
    public void BareMemberAccess_NotLangVersionGated(LanguageVersion languageVersion)
    {
        UsingExpression(".X", TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.TargetTypedMemberAccessExpression);
        {
            N(SyntaxKind.DotToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "X");
            }
        }
        EOF();
    }

    [Fact]
    public void BareMemberAccess_Generic()
    {
        UsingExpression(".X<T>");

        N(SyntaxKind.TargetTypedMemberAccessExpression);
        {
            N(SyntaxKind.DotToken);
            N(SyntaxKind.GenericName);
            {
                N(SyntaxKind.IdentifierToken, "X");
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
        }
        EOF();
    }

    [Fact]
    public void BareMemberAccess_GenericMultipleArgs()
    {
        UsingExpression(".X<T, U>");

        N(SyntaxKind.TargetTypedMemberAccessExpression);
        {
            N(SyntaxKind.DotToken);
            N(SyntaxKind.GenericName);
            {
                N(SyntaxKind.IdentifierToken, "X");
                N(SyntaxKind.TypeArgumentList);
                {
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "U");
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
            }
        }
        EOF();
    }

    #endregion

    #region Postfix chaining

    [Fact]
    public void Invocation()
    {
        UsingExpression(".X()");

        N(SyntaxKind.InvocationExpression);
        {
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "X");
                }
            }
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
        }
        EOF();
    }

    [Fact]
    public void InvocationWithArgs()
    {
        UsingExpression(".Some(42)");

        N(SyntaxKind.InvocationExpression);
        {
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Some");
                }
            }
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "42");
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
        }
        EOF();
    }

    [Fact]
    public void GenericInvocation()
    {
        UsingExpression(".Create<T>(x)");

        N(SyntaxKind.InvocationExpression);
        {
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.GenericName);
                {
                    N(SyntaxKind.IdentifierToken, "Create");
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
            }
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
        }
        EOF();
    }

    [Fact]
    public void DottedChain_Expression()
    {
        // Parser-resilient: `.A.B` builds a tree even though the binder will reject it.
        UsingExpression(".A.B");

        N(SyntaxKind.SimpleMemberAccessExpression);
        {
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
            }
            N(SyntaxKind.DotToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "B");
            }
        }
        EOF();
    }

    [Fact]
    public void DottedChain_WithInvocationInMiddle()
    {
        UsingExpression(".X()[y].Z()");

        N(SyntaxKind.InvocationExpression);
        {
            N(SyntaxKind.SimpleMemberAccessExpression);
            {
                N(SyntaxKind.ElementAccessExpression);
                {
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.TargetTypedMemberAccessExpression);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "X");
                            }
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
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Z");
                }
            }
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
        }
        EOF();
    }

    [Fact]
    public void NullSuppression()
    {
        UsingExpression(".X!");

        N(SyntaxKind.SuppressNullableWarningExpression);
        {
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "X");
                }
            }
            N(SyntaxKind.ExclamationToken);
        }
        EOF();
    }

    [Fact]
    public void PostfixIncrement()
    {
        UsingExpression(".X++");

        N(SyntaxKind.PostIncrementExpression);
        {
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "X");
                }
            }
            N(SyntaxKind.PlusPlusToken);
        }
        EOF();
    }

    [Fact]
    public void PrefixIncrement()
    {
        UsingExpression("++.X");

        N(SyntaxKind.PreIncrementExpression);
        {
            N(SyntaxKind.PlusPlusToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "X");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void PrefixUnaryPlus()
    {
        UsingExpression("+.X");

        N(SyntaxKind.UnaryPlusExpression);
        {
            N(SyntaxKind.PlusToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "X");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void PrefixAddressOf()
    {
        // `&.X` — address-of is syntactically valid on any expression; the binder enforces the unsafe/pointer rules.
        UsingExpression("&.X");

        N(SyntaxKind.AddressOfExpression);
        {
            N(SyntaxKind.AmpersandToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "X");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void PrefixPointerIndirection()
    {
        // `*.X` — pointer indirection is syntactically valid on any expression; binder enforces pointer rules.
        UsingExpression("*.X");

        N(SyntaxKind.PointerIndirectionExpression);
        {
            N(SyntaxKind.AsteriskToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "X");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalAccessOnTargetTyped()
    {
        // Parser-resilient: `.A?.B` has the target-typed form as the receiver of a null-conditional.
        // Binder will reject the leading `.A` in this context.
        UsingExpression(".A?.B");

        N(SyntaxKind.ConditionalAccessExpression);
        {
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.MemberBindingExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "B");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void Parenthesized()
    {
        UsingExpression("(.X)");

        N(SyntaxKind.ParenthesizedExpression);
        {
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "X");
                }
            }
            N(SyntaxKind.CloseParenToken);
        }
        EOF();
    }

    #endregion

    #region Expression positions

    [Fact]
    public void AssignmentRhs()
    {
        UsingStatement("SomeTarget t = .Red;");

        N(SyntaxKind.LocalDeclarationStatement);
        {
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "SomeTarget");
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "t");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.TargetTypedMemberAccessExpression);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Red");
                            }
                        }
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void ReturnStatement()
    {
        UsingStatement("return .None;");

        N(SyntaxKind.ReturnStatement);
        {
            N(SyntaxKind.ReturnKeyword);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "None");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Argument()
    {
        UsingExpression("Goo(.Red)");

        N(SyntaxKind.InvocationExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Goo");
            }
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Red");
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
        }
        EOF();
    }

    [Fact]
    public void NamedArgument()
    {
        UsingExpression("Goo(p: .Red)");

        N(SyntaxKind.InvocationExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Goo");
            }
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.NameColon);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "p");
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Red");
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
        }
        EOF();
    }

    [Fact]
    public void RefArgument()
    {
        // `Goo(ref .Red)` — parser builds the argument; the binder decides whether a target-typed
        // member access can actually be a ref-argument.
        UsingExpression("Goo(ref .Red)");

        N(SyntaxKind.InvocationExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Goo");
            }
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Red");
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
        }
        EOF();
    }

    [Fact]
    public void OutArgument()
    {
        UsingExpression("Goo(out .Red)");

        N(SyntaxKind.InvocationExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Goo");
            }
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.OutKeyword);
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Red");
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
        }
        EOF();
    }

    [Fact]
    public void InArgument()
    {
        UsingExpression("Goo(in .Red)");

        N(SyntaxKind.InvocationExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Goo");
            }
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.InKeyword);
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Red");
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
        }
        EOF();
    }

    [Fact]
    public void InCollectionExpression()
    {
        UsingExpression("[.A, .B]");

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.TargetTypedMemberAccessExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.TargetTypedMemberAccessExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "B");
                    }
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Fact]
    public void InSpreadElement()
    {
        UsingExpression("[.. .X]");

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.SpreadElement);
            {
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.TargetTypedMemberAccessExpression);
                {
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

    [Fact]
    public void InSpreadElement_NoSpaceBeforeDot()
    {
        // Pins lexer + parser behavior for three adjacent dots.  The lexer emits
        // `ERR_TripleDotNotAllowed` and consumes all three dots into a single `..` (DotDotToken),
        // so the spread operand is the plain identifier `X` — NOT a target-typed member access `.X`.
        // To actually spread a target-typed member access, authors must insert whitespace: `[.. .X]`.
        UsingExpression("[...X]",
            // (1,2): error CS8635: Unexpected character sequence '...'
            // [...X]
            Diagnostic(ErrorCode.ERR_TripleDotNotAllowed, "").WithLocation(1, 2));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.SpreadElement);
            {
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "X");
                }
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Fact]
    public void ObjectInitializer()
    {
        UsingExpression("new C { P = .Red }");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "C");
            }
            N(SyntaxKind.ObjectInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "P");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Red");
                        }
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void LambdaBody()
    {
        UsingExpression("() => .X");

        N(SyntaxKind.ParenthesizedLambdaExpression);
        {
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.EqualsGreaterThanToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "X");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void ThrowExpression()
    {
        UsingExpression("x ?? throw .None");

        N(SyntaxKind.CoalesceExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.QuestionQuestionToken);
            N(SyntaxKind.ThrowExpression);
            {
                N(SyntaxKind.ThrowKeyword);
                N(SyntaxKind.TargetTypedMemberAccessExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "None");
                    }
                }
            }
        }
        EOF();
    }

    #endregion

    #region Grammar positions
    // These tests pin that `.X` is accepted in every distinct parser call site where an expression can appear.
    // The cases below are derived from the set of `ParseExpression`, `ParseExpressionCore`, `ParseSubExpression`,
    // `ParsePossibleRefExpression`, `ParseExpressionForParenthesizedConstruct`, `ParseVariableInitializer`, and
    // `ParseArgumentExpression` callers in the parser.

    [Fact]
    public void Grammar_LocalDeclarationInitializer()
    {
        UsingStatement("object x = .Y;");

        N(SyntaxKind.LocalDeclarationStatement);
        {
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.ObjectKeyword);
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.TargetTypedMemberAccessExpression);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Y");
                            }
                        }
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Grammar_TupleLiteral()
    {
        UsingExpression("(.A, .B)");

        N(SyntaxKind.TupleExpression);
        {
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.Argument);
            {
                N(SyntaxKind.TargetTypedMemberAccessExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.Argument);
            {
                N(SyntaxKind.TargetTypedMemberAccessExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "B");
                    }
                }
            }
            N(SyntaxKind.CloseParenToken);
        }
        EOF();
    }

    [Fact]
    public void Grammar_CompoundAssignment()
    {
        UsingExpression("x += .Y");

        N(SyntaxKind.AddAssignmentExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.PlusEqualsToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Y");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void Grammar_CheckedExpression()
    {
        UsingExpression("checked(.X)");

        N(SyntaxKind.CheckedExpression);
        {
            N(SyntaxKind.CheckedKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "X");
                }
            }
            N(SyntaxKind.CloseParenToken);
        }
        EOF();
    }

    [Fact]
    public void Grammar_InterpolatedStringHole()
    {
        UsingExpression(""" $"{.X}" """.Trim());

        N(SyntaxKind.InterpolatedStringExpression);
        {
            N(SyntaxKind.InterpolatedStringStartToken);
            N(SyntaxKind.Interpolation);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.TargetTypedMemberAccessExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.InterpolatedStringEndToken);
        }
        EOF();
    }

    [Fact]
    public void Grammar_ArrayRankSize()
    {
        // `new int[.X]` — `.X` is the size expression; `ParseExpressionCore` is invoked for each rank.
        UsingExpression("new int[.X]");

        N(SyntaxKind.ArrayCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.ArrayType);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.ArrayRankSpecifier);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "X");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
        }
        EOF();
    }

    [Fact]
    public void Grammar_IfCondition()
    {
        UsingStatement("if (.X) { }");

        N(SyntaxKind.IfStatement);
        {
            N(SyntaxKind.IfKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "X");
                }
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Grammar_WhileCondition()
    {
        UsingStatement("while (.X) { }");

        N(SyntaxKind.WhileStatement);
        {
            N(SyntaxKind.WhileKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "X");
                }
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Grammar_DoWhileCondition()
    {
        UsingStatement("do { } while (.X);");

        N(SyntaxKind.DoStatement);
        {
            N(SyntaxKind.DoKeyword);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.WhileKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "X");
                }
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Grammar_SwitchGoverningExpression()
    {
        UsingStatement("switch (.X) { }");

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "X");
                }
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Grammar_LockTarget()
    {
        UsingStatement("lock (.X) { }");

        N(SyntaxKind.LockStatement);
        {
            N(SyntaxKind.LockKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "X");
                }
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Grammar_ForeachIterExpression()
    {
        UsingStatement("foreach (var x in .Y) { }");

        N(SyntaxKind.ForEachStatement);
        {
            N(SyntaxKind.ForEachKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "var");
            }
            N(SyntaxKind.IdentifierToken, "x");
            N(SyntaxKind.InKeyword);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Y");
                }
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Grammar_ForCondition()
    {
        UsingStatement("for (; .X;) { }");

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "X");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Grammar_UsingStatementInitializer()
    {
        UsingStatement("using (var x = .Y) { }");

        N(SyntaxKind.UsingStatement);
        {
            N(SyntaxKind.UsingKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "var");
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.TargetTypedMemberAccessExpression);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Y");
                            }
                        }
                    }
                }
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Grammar_FixedStatementInitializer()
    {
        UsingStatement("fixed (int* p = .X) { }");

        N(SyntaxKind.FixedStatement);
        {
            N(SyntaxKind.FixedKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.PointerType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.AsteriskToken);
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "p");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.TargetTypedMemberAccessExpression);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "X");
                            }
                        }
                    }
                }
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Grammar_CatchFilter()
    {
        UsingStatement("try { } catch when (.X) { }");

        N(SyntaxKind.TryStatement);
        {
            N(SyntaxKind.TryKeyword);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.CatchClause);
            {
                N(SyntaxKind.CatchKeyword);
                N(SyntaxKind.CatchFilterClause);
                {
                    N(SyntaxKind.WhenKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "X");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
        }
        EOF();
    }

    [Fact]
    public void Grammar_YieldReturn()
    {
        UsingStatement("yield return .X;");

        N(SyntaxKind.YieldReturnStatement);
        {
            N(SyntaxKind.YieldKeyword);
            N(SyntaxKind.ReturnKeyword);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "X");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Grammar_ThrowStatement()
    {
        UsingStatement("throw .X;");

        N(SyntaxKind.ThrowStatement);
        {
            N(SyntaxKind.ThrowKeyword);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "X");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Grammar_AwaitOperand_TopLevel()
    {
        // At an expression level, `await .X` is a regular member access on an identifier named `await`.
        // In an async or top-level (implicitly async) context, `await` becomes a keyword and `.X` is its operand.
        // This test uses a top-level statement to force the async context (same shape as the existing
        // `TestAwaitParsedAsElementAccessTopLevel` test in `CollectionExpressionParsingTests`).
        UsingTree("await .X;");

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.AwaitExpression);
                    {
                        N(SyntaxKind.AwaitKeyword);
                        N(SyntaxKind.TargetTypedMemberAccessExpression);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "X");
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void Grammar_AwaitOperand_NotInAsyncContext_IsMemberAccess()
    {
        // Outside an async/top-level context, `await` is a regular identifier, and `await .X` is member access.
        UsingExpression("await .X");

        N(SyntaxKind.SimpleMemberAccessExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "await");
            }
            N(SyntaxKind.DotToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "X");
            }
        }
        EOF();
    }

    [Fact]
    public void Grammar_DefaultParameterValue()
    {
        UsingTree("""
            class C
            {
                void M(object o = .X) { }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.ObjectKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "o");
                            N(SyntaxKind.EqualsValueClause);
                            {
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.TargetTypedMemberAccessExpression);
                                {
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void Grammar_AttributeArgument()
    {
        UsingTree("""
            [My(.X)]
            class C { }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.AttributeList);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.Attribute);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "My");
                        }
                        N(SyntaxKind.AttributeArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.AttributeArgument);
                            {
                                N(SyntaxKind.TargetTypedMemberAccessExpression);
                                {
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void Grammar_QuerySelect()
    {
        UsingExpression("from x in y select .Z");

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
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Z");
                        }
                    }
                }
            }
        }
        EOF();
    }

    [Fact]
    public void Grammar_QueryWhere()
    {
        UsingExpression("from x in y where .Z select x");

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
                N(SyntaxKind.WhereClause);
                {
                    N(SyntaxKind.WhereKeyword);
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Z");
                        }
                    }
                }
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
        EOF();
    }

    [Fact]
    public void Grammar_ConstructorBaseInitializer()
    {
        UsingTree("""
            class C : B
            {
                public C() : base(.X) { }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.BaseList);
                {
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.SimpleBaseType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ConstructorDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.BaseConstructorInitializer);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.BaseKeyword);
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.TargetTypedMemberAccessExpression);
                                {
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    #endregion

    #region Operator positions

    [Fact]
    public void PrefixBitwiseNot()
    {
        UsingExpression("~.Mask");

        N(SyntaxKind.BitwiseNotExpression);
        {
            N(SyntaxKind.TildeToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Mask");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void PrefixLogicalNot()
    {
        UsingExpression("!.X");

        N(SyntaxKind.LogicalNotExpression);
        {
            N(SyntaxKind.ExclamationToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "X");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void PrefixUnaryMinus()
    {
        UsingExpression("-.X");

        N(SyntaxKind.UnaryMinusExpression);
        {
            N(SyntaxKind.MinusToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "X");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void BitwiseOr()
    {
        UsingExpression(".A | .B");

        N(SyntaxKind.BitwiseOrExpression);
        {
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
            }
            N(SyntaxKind.BarToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "B");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void CrossOperandMix()
    {
        UsingExpression("BindingFlags.Public | .Static | .DeclaredOnly");

        N(SyntaxKind.BitwiseOrExpression);
        {
            N(SyntaxKind.BitwiseOrExpression);
            {
                N(SyntaxKind.SimpleMemberAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "BindingFlags");
                    }
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Public");
                    }
                }
                N(SyntaxKind.BarToken);
                N(SyntaxKind.TargetTypedMemberAccessExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Static");
                    }
                }
            }
            N(SyntaxKind.BarToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "DeclaredOnly");
                }
            }
        }
        EOF();
    }

    #endregion

    #region Type positions

    [Fact]
    public void NewWithTargetTypedName()
    {
        // Parser accepts `new .Foo()`; binder will reject this form in the current feature scope.
        UsingExpression("new .Foo()");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.TargetTypedQualifiedName);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Foo");
                }
            }
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
        }
        EOF();
    }

    [Fact]
    public void NewWithTargetTypedGenericName()
    {
        UsingExpression("new .Foo<T>()");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.TargetTypedQualifiedName);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.GenericName);
                {
                    N(SyntaxKind.IdentifierToken, "Foo");
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
            }
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
        }
        EOF();
    }

    [Fact]
    public void NewWithTargetTypedNameAndInitializer()
    {
        UsingExpression("new .Foo() { P = 1 }");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.TargetTypedQualifiedName);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Foo");
                }
            }
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.ObjectInitializerExpression);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "P");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void TypeOfWithTargetTypedName()
    {
        // Parser accepts; binder will reject.
        UsingExpression("typeof(.Foo)");

        N(SyntaxKind.TypeOfExpression);
        {
            N(SyntaxKind.TypeOfKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.TargetTypedQualifiedName);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Foo");
                }
            }
            N(SyntaxKind.CloseParenToken);
        }
        EOF();
    }

    [Fact]
    public void DefaultOfTargetTypedName()
    {
        UsingExpression("default(.Foo)");

        N(SyntaxKind.DefaultExpression);
        {
            N(SyntaxKind.DefaultKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.TargetTypedQualifiedName);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Foo");
                }
            }
            N(SyntaxKind.CloseParenToken);
        }
        EOF();
    }

    [Fact]
    public void DottedTypeChain()
    {
        // Parser-resilient: `.A.B` in a type position builds a QualifiedName over the target-typed head.
        UsingExpression("new .A.B()");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.QualifiedName);
            {
                N(SyntaxKind.TargetTypedQualifiedName);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "B");
                }
            }
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
        }
        EOF();
    }

    [Fact]
    public void NewTargetTypedArray()
    {
        // Parser accepts `new .Foo[10]` as an array creation; binder will reject.
        UsingExpression("new .Foo[10]");

        N(SyntaxKind.ArrayCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.ArrayType);
            {
                N(SyntaxKind.TargetTypedQualifiedName);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Foo");
                    }
                }
                N(SyntaxKind.ArrayRankSpecifier);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "10");
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
        }
        EOF();
    }

    // The following tests pin the cast-vs-member-access disambiguation for `(X).Y`.  The disambiguation mirrors the
    // existing `(X)[y]` rule: when `X` is syntactically ambiguous between a type and an expression (plain identifier,
    // generic name, dotted name, tuple type), `(X).Y` is parsed as member access on a parenthesized expression for
    // back-compat; only when `X` must be a type (predefined, pointer, nullable, alias-qualified) is `(X).Y` parsed as
    // a cast.  Per LDM, `(A).B` should continue to fail when `A` binds to a type; here we only pin the parser shape.

    [Fact]
    public void CastAmbiguity_Ambiguous_ParenthesizedMemberAccess()
    {
        UsingExpression("(A).Y");

        N(SyntaxKind.SimpleMemberAccessExpression);
        {
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.DotToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Y");
            }
        }
        EOF();
    }

    [Fact]
    public void CastAmbiguity_DottedAmbiguous_ParenthesizedMemberAccess()
    {
        UsingExpression("(A.B).C");

        N(SyntaxKind.SimpleMemberAccessExpression);
        {
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.SimpleMemberAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "B");
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.DotToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "C");
            }
        }
        EOF();
    }

    [Fact]
    public void CastAmbiguity_Generic_ParenthesizedMemberAccess()
    {
        UsingExpression("(A<T>).Y");

        N(SyntaxKind.SimpleMemberAccessExpression);
        {
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.GenericName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
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
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.DotToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Y");
            }
        }
        EOF();
    }

    [Fact]
    public void CastAmbiguity_PredefinedType_Cast()
    {
        // `(int).Y` — `int` is unambiguously a type, so this is a cast of the target-typed member access `.Y`.
        UsingExpression("(int).Y");

        N(SyntaxKind.CastExpression);
        {
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Y");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void CastAmbiguity_Nullable_Cast()
    {
        // `(A?).Y` — nullable type is unambiguous, so cast of target-typed member access.
        UsingExpression("(A?).Y");

        N(SyntaxKind.CastExpression);
        {
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.NullableType);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.QuestionToken);
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Y");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void CastAmbiguity_Pointer_Cast()
    {
        // `(A*).Y` — pointer type is unambiguous, so cast of target-typed member access.
        UsingExpression("(A*).Y");

        N(SyntaxKind.CastExpression);
        {
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.PointerType);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.AsteriskToken);
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Y");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void CastAmbiguity_Array_Cast()
    {
        // `(A[]).Y` — array type is unambiguous, so cast of target-typed member access.
        UsingExpression("(A[]).Y");

        N(SyntaxKind.CastExpression);
        {
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.ArrayType);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
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
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Y");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void CastAmbiguity_AliasQualified_Cast()
    {
        // `(A::B).Y` — alias-qualified name is unambiguously a type.
        UsingExpression("(A::B).Y");

        N(SyntaxKind.CastExpression);
        {
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.AliasQualifiedName);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.ColonColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "B");
                }
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Y");
                }
            }
        }
        EOF();
    }

    #endregion

    #region Grammar type positions
    // These tests pin behavior at every distinct parser call site where a type or name can appear
    // (derived from the set of `ParseType`, `ParseReturnType`, `ParseTypeArgument`,
    // `ParseUnderlyingType`, `ParseQualifiedName`, and `ParseAliasQualifiedName` callers).
    //
    // Summary of what the parser currently does with a leading `.`:
    //
    // * Positions that flow through `ParseUnderlyingType` (type operators + a handful of declaration contexts
    //   that aren't dispatched on the first token) accept `.X` as a `TargetTypedQualifiedName`:
    //     `new .Foo()`, `typeof(.Foo)`, `default(.Foo)`, `x as .Foo`, `x is .Foo`, `sizeof(.Foo)`,
    //     `stackalloc .Foo[N]`, `catch (.Ex e)`, `using A = .Foo`, `where T : .I`, base list `: .B`,
    //     `ref .T M()`.
    // * Declaration contexts whose first-token dispatcher rejects `.` (class members) produce a correct
    //   member-declaration tree plus an `ERR_InvalidMemberDecl` diagnostic at the `.`.
    // * Argument-list / parameter-list parsers reject `.` at the start of a parameter with
    //   `ERR_IdentifierExpected`.
    // * Statement positions (`.T x;`, `.T Local() { }`, `.T* p;`, `.T[] x;`, `.T? x = null;`,
    //   `foreach (.T x in y)`) are NOT supported: `.T` is parsed as an expression, and the tokens that
    //   would have named the variable become follow-on errors.  These positions have no target type by
    //   construction, so target-typed static member access does not apply; the tests below pin the
    //   actual expression-parse behavior.
    //
    // Ambiguities the parser can't resolve:
    // * `Foo<.Bar>(x)` is parsed as a less-than / greater-than comparison chain, not as a generic
    //   invocation with `.Bar` as a type argument.

    [Fact]
    public void GrammarType_MethodReturnType_NotSupported()
    {
        // At class-body scope, the first-token dispatcher rejects `.` with `ERR_InvalidMemberDecl` and
        // SKIPS the `.` as invalid syntax.  The following `R M() { }` parses as a normal method
        // declaration with `R` (not `.R`) as the return type.  No `TargetTypedQualifiedName` is produced.
        UsingTree("""
            class C
            {
                .R M() { }
            }
            """,
            // (3,5): error CS1519: Invalid token '.' in a member declaration
            //     .R M() { }
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ".").WithArguments(".").WithLocation(3, 5));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "R");
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
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void GrammarType_MethodParameterType_NotSupported()
    {
        // Parameter-list parsing rejects `.` with `ERR_IdentifierExpected` and skips it.  The parameter
        // type ends up as a plain `IdentifierName T` — no `TargetTypedQualifiedName` is produced.
        UsingTree("""
            class C
            {
                void M(.T p) { }
            }
            """,
            // (3,12): error CS1001: Identifier expected
            //     void M(.T p) { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ".").WithLocation(3, 12));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.IdentifierToken, "p");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void GrammarType_FieldDeclarationType_NotSupported()
    {
        // Like method-return-type at class scope, `.` is rejected and skipped.  The field parses as
        // `T F;` with no `TargetTypedQualifiedName`.
        UsingTree("""
            class C
            {
                .T F;
            }
            """,
            // (3,5): error CS1519: Invalid token '.' in a member declaration
            //     .T F;
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ".").WithArguments(".").WithLocation(3, 5));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "F");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void GrammarType_PropertyDeclarationType_NotSupported()
    {
        UsingTree("""
            class C
            {
                .T P { get; set; }
            }
            """,
            // (3,5): error CS1519: Invalid token '.' in a member declaration
            //     .T P { get; set; }
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ".").WithArguments(".").WithLocation(3, 5));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                    N(SyntaxKind.IdentifierToken, "P");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void GrammarType_IndexerType_NotSupported()
    {
        UsingTree("""
            class C
            {
                .T this[int i] => default;
            }
            """,
            // (3,5): error CS1519: Invalid token '.' in a member declaration
            //     .T this[int i] => default;
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ".").WithArguments(".").WithLocation(3, 5));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.IndexerDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                    N(SyntaxKind.ThisKeyword);
                    N(SyntaxKind.BracketedParameterList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.DefaultLiteralExpression);
                        {
                            N(SyntaxKind.DefaultKeyword);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void GrammarType_GenericConstraint()
    {
        UsingTree("""
            class C<T> where T : .I { }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.TypeParameterList);
                {
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.TypeParameter);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
                N(SyntaxKind.TypeParameterConstraintClause);
                {
                    N(SyntaxKind.WhereKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.TypeConstraint);
                    {
                        N(SyntaxKind.TargetTypedQualifiedName);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                            }
                        }
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void GrammarType_BaseList()
    {
        UsingTree("class C : .B { }");

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.BaseList);
                {
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.SimpleBaseType);
                    {
                        N(SyntaxKind.TargetTypedQualifiedName);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                        }
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void GrammarType_TypeArgument_NotSupported()
    {
        // `Foo<.Bar>(x)` — the `<` / `>` generic-argument-list disambiguation heuristic does not recognize
        // a target-typed name as a valid type argument, so the expression is parsed as a comparison chain
        // `(Foo < .Bar) > (x)` rather than `Foo<.Bar>(x)`.
        UsingExpression("Foo<.Bar>(x)");

        N(SyntaxKind.GreaterThanExpression);
        {
            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Foo");
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.TargetTypedMemberAccessExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Bar");
                    }
                }
            }
            N(SyntaxKind.GreaterThanToken);
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.CloseParenToken);
            }
        }
        EOF();
    }

    [Fact]
    public void GrammarType_AsExpression()
    {
        UsingExpression("x as .Foo");

        N(SyntaxKind.AsExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.AsKeyword);
            N(SyntaxKind.TargetTypedQualifiedName);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Foo");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void GrammarType_SizeOf()
    {
        UsingExpression("sizeof(.Foo)");

        N(SyntaxKind.SizeOfExpression);
        {
            N(SyntaxKind.SizeOfKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.TargetTypedQualifiedName);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Foo");
                }
            }
            N(SyntaxKind.CloseParenToken);
        }
        EOF();
    }

    [Fact]
    public void GrammarType_CatchExceptionType()
    {
        UsingStatement("try { } catch (.Ex e) { }");

        N(SyntaxKind.TryStatement);
        {
            N(SyntaxKind.TryKeyword);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.CatchClause);
            {
                N(SyntaxKind.CatchKeyword);
                N(SyntaxKind.CatchDeclaration);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.TargetTypedQualifiedName);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Ex");
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "e");
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
        }
        EOF();
    }

    [Fact]
    public void GrammarType_UsingAlias()
    {
        UsingTree("using A = .Foo;");

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.TargetTypedQualifiedName);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Foo");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void GrammarType_StackAllocElementType()
    {
        UsingExpression("stackalloc .Foo[10]");

        N(SyntaxKind.StackAllocArrayCreationExpression);
        {
            N(SyntaxKind.StackAllocKeyword);
            N(SyntaxKind.ArrayType);
            {
                N(SyntaxKind.TargetTypedQualifiedName);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Foo");
                    }
                }
                N(SyntaxKind.ArrayRankSpecifier);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "10");
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
        }
        EOF();
    }

    [Fact]
    public void GrammarType_DelegateDeclaration()
    {
        // `delegate .R D(.T p);` — the return-type position accepts `.R` via `ParseReturnType` and produces
        // a `TargetTypedQualifiedName`, but the parameter-list parser rejects `.` with
        // `ERR_IdentifierExpected` and skips it, leaving the parameter type as a plain `IdentifierName T`.
        UsingTree("delegate .R D(.T p);",
            // (1,15): error CS1001: Identifier expected
            // delegate .R D(.T p);
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ".").WithLocation(1, 15));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.DelegateDeclaration);
            {
                N(SyntaxKind.DelegateKeyword);
                N(SyntaxKind.TargetTypedQualifiedName);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "R");
                    }
                }
                N(SyntaxKind.IdentifierToken, "D");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.IdentifierToken, "p");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void GrammarType_LocalDeclarationType_ParsesAsExpressionStatement()
    {
        // Statement-scope: no target-type source exists for `.T`, so `.T` parses as a primary expression
        // `TargetTypedMemberAccessExpression`; the following `x` then fails to continue the statement.
        UsingStatement(".T x;",
            // (1,1): error CS1073: Unexpected token 'x'
            // .T x;
            Diagnostic(ErrorCode.ERR_UnexpectedToken, ".T ").WithArguments("x").WithLocation(1, 1),
            // (1,4): error CS1002: ; expected
            // .T x;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "x").WithLocation(1, 4));

        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "T");
                }
            }
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void GrammarType_LocalFunctionReturnType_ParsesAsExpressionStatement()
    {
        // Same as above: `.T Local() { }` parses `.T` as an expression statement; the following tokens
        // fall into error recovery.
        UsingStatement(".T Local() { }",
            // (1,1): error CS1073: Unexpected token 'Local'
            // .T Local() { }
            Diagnostic(ErrorCode.ERR_UnexpectedToken, ".T ").WithArguments("Local").WithLocation(1, 1),
            // (1,4): error CS1002: ; expected
            // .T Local() { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "Local").WithLocation(1, 4));

        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "T");
                }
            }
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void GrammarType_PointerTypeStandalone_ParsesAsMultiplyExpression()
    {
        // `.T* p;` — parsed as the multiplication expression `.T * p;`, not as a pointer-type declaration.
        UsingStatement(".T* p;");

        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.MultiplyExpression);
            {
                N(SyntaxKind.TargetTypedMemberAccessExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                }
                N(SyntaxKind.AsteriskToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "p");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void GrammarType_ArrayTypeStandalone_ParsesAsElementAccess()
    {
        // `.T[] x;` — `.T[]` is parsed as an element-access expression with an empty index list; the
        // following `x` then triggers error recovery.
        UsingStatement(".T[] x;",
            // (1,1): error CS1073: Unexpected token 'x'
            // .T[] x;
            Diagnostic(ErrorCode.ERR_UnexpectedToken, ".T[] ").WithArguments("x").WithLocation(1, 1),
            // (1,4): error CS0443: Syntax error; value expected
            // .T[] x;
            Diagnostic(ErrorCode.ERR_ValueExpected, "]").WithLocation(1, 4),
            // (1,6): error CS1002: ; expected
            // .T[] x;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "x").WithLocation(1, 6));

        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.ElementAccessExpression);
            {
                N(SyntaxKind.TargetTypedMemberAccessExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                }
                N(SyntaxKind.BracketedArgumentList);
                {
                    N(SyntaxKind.OpenBracketToken);
                    M(SyntaxKind.Argument);
                    {
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void GrammarType_NullableTypeStandalone_ParsesAsConditionalExpression()
    {
        // `.T? x = null;` — parsed as a conditional expression `.T ? x = null : ???`, which errors with
        // `:` expected.  The `?` disambiguation lookahead goes looking for a `:` and comes back empty.
        UsingStatement(".T? x = null;",
            // (1,13): error CS1003: Syntax error, ':' expected
            // .T? x = null;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(":").WithLocation(1, 13),
            // (1,13): error CS1525: Invalid expression term ';'
            // .T? x = null;
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(1, 13));

        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.TargetTypedMemberAccessExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NullLiteralExpression);
                    {
                        N(SyntaxKind.NullKeyword);
                    }
                }
                M(SyntaxKind.ColonToken);
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void GrammarType_ForeachVariableType_ProducesErrorRecovery()
    {
        // `foreach (.T x in y)` — the foreach parser does not call the `ParseUnderlyingType` path that
        // accepts leading `.`.  Instead, `.T` is parsed as the variable expression of a deconstructing
        // `ForEachVariableStatement`; `x` then becomes the collection (with a missing `in` keyword); the
        // following `in y)` tokens drop into error recovery.
        UsingStatement("foreach (.T x in y) { }",
            // (1,1): error CS1073: Unexpected token 'in'
            // foreach (.T x in y) { }
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "foreach (.T x ").WithArguments("in").WithLocation(1, 1),
            // (1,13): error CS1515: 'in' expected
            // foreach (.T x in y) { }
            Diagnostic(ErrorCode.ERR_InExpected, "x").WithLocation(1, 13),
            // (1,13): error CS0230: Type and identifier are both required in a foreach statement
            // foreach (.T x in y) { }
            Diagnostic(ErrorCode.ERR_BadForeachDecl, "x").WithLocation(1, 13),
            // (1,15): error CS1026: ) expected
            // foreach (.T x in y) { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "in").WithLocation(1, 15),
            // (1,15): error CS1525: Invalid expression term 'in'
            // foreach (.T x in y) { }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "in").WithArguments("in").WithLocation(1, 15),
            // (1,15): error CS1002: ; expected
            // foreach (.T x in y) { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "in").WithLocation(1, 15));

        N(SyntaxKind.ForEachVariableStatement);
        {
            N(SyntaxKind.ForEachKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "T");
                }
            }
            M(SyntaxKind.InKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            M(SyntaxKind.CloseParenToken);
            M(SyntaxKind.ExpressionStatement);
            {
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
                M(SyntaxKind.SemicolonToken);
            }
        }
        EOF();
    }

    [Fact]
    public void GrammarName_NamespaceDeclaration_NotSupported()
    {
        // Namespace names are parsed via `ParseQualifiedName`, which does not accept a leading `.`.
        // The parser emits `ERR_IdentifierExpected`, synthesizes a missing identifier for the left side,
        // and builds a `QualifiedName(<missing>, ., X)` — NOT a `TargetTypedQualifiedName`.
        UsingTree("namespace .X { }",
            // (1,11): error CS1001: Identifier expected
            // namespace .X { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ".").WithLocation(1, 11));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.NamespaceDeclaration);
            {
                N(SyntaxKind.NamespaceKeyword);
                N(SyntaxKind.QualifiedName);
                {
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void GrammarName_UsingDirective_NotSupported()
    {
        // Non-alias using directives parse the name via `ParseQualifiedName`, which does not accept a
        // leading `.`.  (Alias using directives — `using A = .Foo;` — take a different path through
        // `ParseType` and DO accept it.  See `GrammarType_UsingAlias` above.)
        UsingTree("using .X;",
            // (1,7): error CS1001: Identifier expected
            // using .X;
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ".").WithLocation(1, 7));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.QualifiedName);
                {
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void GrammarName_Attribute_NotSupported()
    {
        // Attribute names are parsed via `ParseQualifiedName`; leading `.` produces an identifier-expected
        // diagnostic and a `QualifiedName(<missing>, ., MyAttr)`.
        UsingTree("""
            [.MyAttr] class C { }
            """,
            // (1,2): error CS1001: Identifier expected
            // [.MyAttr] class C { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ".").WithLocation(1, 2));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.AttributeList);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.Attribute);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "MyAttr");
                            }
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void GrammarType_BaseList_WithTargetTypedGenericArgument()
    {
        // `class C : X<.Y> { }` — the base-type position goes through `ParseType`, which routes generic
        // type arguments through `ParseTypeArgument` → `ParseType` → `ParseUnderlyingType`, so the
        // inner `.Y` is accepted as a `TargetTypedQualifiedName`.  Contrast with the expression-scope
        // case `Foo<.Bar>(x)` (see `GrammarType_TypeArgument_NotSupported`) where `<` / `>` ambiguity
        // prevents the same parse.
        UsingTree("class C : X<.Y> { }");

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.BaseList);
                {
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.SimpleBaseType);
                    {
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "X");
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.TargetTypedQualifiedName);
                                {
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Y");
                                    }
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void GrammarType_GenericConstraint_WithTargetTypedGenericArgument()
    {
        // `where T : I<.Y>` — generic constraint with a target-typed type argument.  Same route as
        // `GrammarType_BaseList_WithTargetTypedGenericArgument`.
        UsingTree("class C<T> where T : I<.Y> { }");

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.TypeParameterList);
                {
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.TypeParameter);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
                N(SyntaxKind.TypeParameterConstraintClause);
                {
                    N(SyntaxKind.WhereKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.TypeConstraint);
                    {
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "I");
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.TargetTypedQualifiedName);
                                {
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Y");
                                    }
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void GrammarType_NewGenericType_WithTargetTypedGenericArgument()
    {
        // `new X<.Y>()` — generic object creation with a target-typed type argument.
        UsingExpression("new X<.Y>()");

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.GenericName);
            {
                N(SyntaxKind.IdentifierToken, "X");
                N(SyntaxKind.TypeArgumentList);
                {
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.TargetTypedQualifiedName);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Y");
                        }
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
            }
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
        }
        EOF();
    }

    [Fact]
    public void GrammarType_RefMethodReturnType()
    {
        UsingTree("""
            class C
            {
                ref .T M() => throw null;
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.TargetTypedQualifiedName);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.ThrowExpression);
                        {
                            N(SyntaxKind.ThrowKeyword);
                            N(SyntaxKind.NullLiteralExpression);
                            {
                                N(SyntaxKind.NullKeyword);
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    #endregion

    #region Pattern positions

    [Fact]
    public void IsExpression_TypePattern()
    {
        // Mirrors how `x is Foo` already parses as the old `IsExpression` binary form (when `Foo` looks like a type)
        // rather than an `IsPatternExpression`. The binder decides from there.
        UsingExpression("x is .Error");

        N(SyntaxKind.IsExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.IsKeyword);
            N(SyntaxKind.TargetTypedQualifiedName);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Error");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void IsExpression_PositionalPattern()
    {
        UsingExpression("x is .Success(var y)");

        N(SyntaxKind.IsPatternExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.IsKeyword);
            N(SyntaxKind.RecursivePattern);
            {
                N(SyntaxKind.TargetTypedQualifiedName);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Success");
                    }
                }
                N(SyntaxKind.PositionalPatternClause);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Subpattern);
                    {
                        N(SyntaxKind.VarPattern);
                        {
                            N(SyntaxKind.VarKeyword);
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
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
    public void IsExpression_PropertyPattern()
    {
        UsingExpression("x is .Foo { P: 1 }");

        N(SyntaxKind.IsPatternExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.IsKeyword);
            N(SyntaxKind.RecursivePattern);
            {
                N(SyntaxKind.TargetTypedQualifiedName);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Foo");
                    }
                }
                N(SyntaxKind.PropertyPatternClause);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.Subpattern);
                    {
                        N(SyntaxKind.NameColon);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "P");
                            }
                            N(SyntaxKind.ColonToken);
                        }
                        N(SyntaxKind.ConstantPattern);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "1");
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
    public void IsExpression_DeclarationPattern()
    {
        UsingExpression("x is .Foo y");

        N(SyntaxKind.IsPatternExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.IsKeyword);
            N(SyntaxKind.DeclarationPattern);
            {
                N(SyntaxKind.TargetTypedQualifiedName);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Foo");
                    }
                }
                N(SyntaxKind.SingleVariableDesignation);
                {
                    N(SyntaxKind.IdentifierToken, "y");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void IsExpression_OrPattern()
    {
        UsingExpression("x is .A or .B");

        N(SyntaxKind.IsPatternExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.IsKeyword);
            N(SyntaxKind.OrPattern);
            {
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                    }
                }
                N(SyntaxKind.OrKeyword);
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                    }
                }
            }
        }
        EOF();
    }

    [Fact]
    public void IsExpression_NotPattern()
    {
        UsingExpression("x is not .None");

        N(SyntaxKind.IsPatternExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.IsKeyword);
            N(SyntaxKind.NotPattern);
            {
                N(SyntaxKind.NotKeyword);
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "None");
                        }
                    }
                }
            }
        }
        EOF();
    }

    [Fact]
    public void IsExpression_ParenthesizedOrPattern()
    {
        UsingExpression("x is (.A or .B)");

        N(SyntaxKind.IsPatternExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.IsKeyword);
            N(SyntaxKind.ParenthesizedPattern);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.OrPattern);
                {
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.TargetTypedMemberAccessExpression);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                            }
                        }
                    }
                    N(SyntaxKind.OrKeyword);
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.TargetTypedMemberAccessExpression);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
        }
        EOF();
    }

    [Fact]
    public void SwitchExpression_Arms()
    {
        UsingExpression("x switch { .A => 1, .B(var v) => v }");

        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                    }
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.TargetTypedQualifiedName);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                    }
                    N(SyntaxKind.PositionalPatternClause);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.VarPattern);
                            {
                                N(SyntaxKind.VarKeyword);
                                N(SyntaxKind.SingleVariableDesignation);
                                {
                                    N(SyntaxKind.IdentifierToken, "v");
                                }
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "v");
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void SwitchStatement_CaseLabel()
    {
        UsingStatement("""
            switch (x)
            {
                case .Red: break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CaseSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Red");
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void SwitchStatement_CaseLabel_PositionalPattern()
    {
        // `case .Success(var value):` — a case label whose pattern is a positional recursive pattern
        // on a target-typed qualified name, with a variable designation via `var value` inside.
        UsingStatement("""
            switch (x)
            {
                case .Success(var value): break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.TargetTypedQualifiedName);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Success");
                            }
                        }
                        N(SyntaxKind.PositionalPatternClause);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Subpattern);
                            {
                                N(SyntaxKind.VarPattern);
                                {
                                    N(SyntaxKind.VarKeyword);
                                    N(SyntaxKind.SingleVariableDesignation);
                                    {
                                        N(SyntaxKind.IdentifierToken, "value");
                                    }
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void NestedPattern_PropertyWithOrPattern()
    {
        UsingExpression("x is { A: .Some(0) or .None }");

        N(SyntaxKind.IsPatternExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
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
                                N(SyntaxKind.IdentifierToken, "A");
                            }
                            N(SyntaxKind.ColonToken);
                        }
                        N(SyntaxKind.OrPattern);
                        {
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.TargetTypedQualifiedName);
                                {
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Some");
                                    }
                                }
                                N(SyntaxKind.PositionalPatternClause);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Subpattern);
                                    {
                                        N(SyntaxKind.ConstantPattern);
                                        {
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "0");
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                            N(SyntaxKind.OrKeyword);
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.TargetTypedMemberAccessExpression);
                                {
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "None");
                                    }
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

    [Fact]
    public void ListPattern_WithTargetTypedElements()
    {
        UsingExpression("x is [.A, .B]");

        N(SyntaxKind.IsPatternExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.IsKeyword);
            N(SyntaxKind.ListPattern);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
        }
        EOF();
    }

    [Fact]
    public void ListPattern_SliceThenTargetTyped()
    {
        UsingExpression("x is [.., .A]");

        N(SyntaxKind.IsPatternExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.IsKeyword);
            N(SyntaxKind.ListPattern);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.SlicePattern);
                {
                    N(SyntaxKind.DotDotToken);
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
        }
        EOF();
    }

    #endregion

    #region Conditional-expression disambiguation

    [Fact]
    public void Conditional_BothBranchesTargetTyped()
    {
        UsingExpression("x ? .A : .B");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "B");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void NullConditionalMemberAccess_BaselineUnchanged()
    {
        UsingExpression("x?.Y");

        N(SyntaxKind.ConditionalAccessExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.MemberBindingExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Y");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void Conditional_SameShapeAsNullConditional_WithColon()
    {
        // `x ? .Y : z` ambiguous between `(x?.Y) : z` and `x ? .Y : z`. Ternary wins
        // because of the following `:`.
        UsingExpression("x ? .Y : z");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Y");
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "z");
            }
        }
        EOF();
    }

    [Fact]
    public void Conditional_DottedChain_InTrueBranch()
    {
        UsingExpression("x ? .A.B : c");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.SimpleMemberAccessExpression);
            {
                N(SyntaxKind.TargetTypedMemberAccessExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "B");
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "c");
            }
        }
        EOF();
    }

    [Fact]
    public void Conditional_InvocationInTrueBranch()
    {
        UsingExpression("x ? .A() : .B");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.TargetTypedMemberAccessExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "B");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void Conditional_GenericInvocationInTrueBranch()
    {
        UsingExpression("x ? .A<T>() : .B");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.TargetTypedMemberAccessExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
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
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "B");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void Conditional_Reinterpret_InnerConditionalAccess()
    {
        // `a ? b?.X : c` must still parse the inner `b?.X` as a null-conditional access
        // for back-compat (the retry-with-force mechanism kicks in).
        UsingExpression("a ? b?.X : c");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.MemberBindingExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "c");
            }
        }
        EOF();
    }

    [Fact]
    public void Conditional_LegitimateNestedTernary_WithTargetTypedWhenTrue()
    {
        // `a ? b ? .X : c : d` is a legitimate nested ternary; the inner ternary's `:` is found,
        // so no reinterpretation is triggered.
        UsingExpression("a ? b ? .X : c : d");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.TargetTypedMemberAccessExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "d");
            }
        }
        EOF();
    }

    [Fact]
    public void Conditional_NestedAmbiguity_SpecTodoCase()
    {
        // `A ? .B ? .C : D : E` — nested ternaries where both when-true branches are target-typed.
        UsingExpression("A ? .B ? .C : D : E");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "A");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.TargetTypedMemberAccessExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "B");
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.TargetTypedMemberAccessExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "C");
                    }
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "D");
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "E");
            }
        }
        EOF();
    }

    [Fact]
    public void Conditional_NullConditional_WithPostfixSuppression_NotReinterpreted()
    {
        // `a ? .A! : .B` — the `!` is a postfix suppression on the target-typed member access
        // in the when-true; no reinterpretation needed.
        UsingExpression("a ? .A! : .B");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.SuppressNullableWarningExpression);
            {
                N(SyntaxKind.TargetTypedMemberAccessExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                }
                N(SyntaxKind.ExclamationToken);
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "B");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void Conditional_CaseLabelWhenClauseWithNestedTernary_IsStillTernary()
    {
        // Counter-case to `Conditional_CaseLabelWhenClauseWithNullConditionalAccess_NotReinterpreted`:
        // when the ternary `?` sits INSIDE a nested delimited construct (here `Foo(...)`) in a
        // case-label when-clause, the inner `:` is the ternary's separator and the outer `:` terminates
        // the case label.  The `ParseWhenClause` reparse-with-ForceConditionalAccessExpression logic
        // must NOT fire, because the naive parse's top-level result is an `InvocationExpression`, not a
        // `ConditionalExpression`.
        UsingStatement("""
            switch (v)
            {
                case int i when Foo(x > 0 ? .Y : .Z): break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "v");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.WhenClause);
                    {
                        N(SyntaxKind.WhenKeyword);
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Foo");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.ConditionalExpression);
                                    {
                                        N(SyntaxKind.GreaterThanExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "x");
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "0");
                                            }
                                        }
                                        N(SyntaxKind.QuestionToken);
                                        N(SyntaxKind.TargetTypedMemberAccessExpression);
                                        {
                                            N(SyntaxKind.DotToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Y");
                                            }
                                        }
                                        N(SyntaxKind.ColonToken);
                                        N(SyntaxKind.TargetTypedMemberAccessExpression);
                                        {
                                            N(SyntaxKind.DotToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Z");
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Conditional_CaseLabelWhenClauseWithNullConditionalAccess_NotReinterpreted()
    {
        // Regression: the `:` at the end of the case label must NOT be interpreted as a ternary
        // separator by the `?.` speculation.  `s?.Length` is a legitimate null-conditional access inside
        // the `when` guard; the trailing `:` terminates the case label.
        UsingExpression("s?.Length == 0");

        N(SyntaxKind.EqualsExpression);
        {
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "s");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.MemberBindingExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Length");
                    }
                }
            }
            N(SyntaxKind.EqualsEqualsToken);
            N(SyntaxKind.NumericLiteralExpression);
            {
                N(SyntaxKind.NumericLiteralToken, "0");
            }
        }
        EOF();

        // End-to-end: in a case-label's `when` guard, `s?.Length` parses as a null-conditional access
        // and the trailing `:` terminates the case label (rather than being interpreted as a ternary
        // separator, which was a regression from the initial target-typed static member access parser).
        UsingStatement("""
            switch (x)
            {
                case string s when s?.Length == 0: break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.StringKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "s");
                        }
                    }
                    N(SyntaxKind.WhenClause);
                    {
                        N(SyntaxKind.WhenKeyword);
                        N(SyntaxKind.EqualsExpression);
                        {
                            N(SyntaxKind.ConditionalAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "s");
                                }
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.MemberBindingExpression);
                                {
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Length");
                                    }
                                }
                            }
                            N(SyntaxKind.EqualsEqualsToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "0");
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Conditional_CaseLabelWhenClauseWithNullConditionalIndex_NotReinterpreted()
    {
        // Same regression for `?[`: `arr?[0]` inside a case label's `when` guard must not be
        // reinterpreted as a ternary because of the case-label `:`.
        UsingStatement("""
            switch (x)
            {
                case int i when arr?[0] == i: break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.WhenClause);
                    {
                        N(SyntaxKind.WhenKeyword);
                        N(SyntaxKind.EqualsExpression);
                        {
                            N(SyntaxKind.ConditionalAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "arr");
                                }
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.ElementBindingExpression);
                                {
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
                            N(SyntaxKind.EqualsEqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "i");
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Conditional_CaseLabelWhenClauseWithOuterTernaryAndNestedNullConditional_Reinterpreted()
    {
        // Nested variant: the outer `?` (`a ? X : c`) is a genuine ternary whose when-true `b?.Length == 0`
        // contains a null-conditional access.  The case-label `:` sits AFTER the ternary's whenFalse.
        //
        // Naive parse misinterprets the INNER `?` as another ternary (because the `:` speculation sees
        // the case-label `:`), producing `a ? (b ? .Length == 0 : c) : <missing>` and leaving tokens
        // beyond the case label.  The ContainsTernaryToReinterpret walker finds the inner ternary whose
        // when-true starts with `.`, and the reparse-with-ForceConditionalAccessExpression produces the
        // correct tree: outer ternary with `b?.Length == 0` as when-true and `c` as when-false.
        UsingStatement("""
            switch (x)
            {
                case int i when a ? b?.Length == 0 : c != null: break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.WhenClause);
                    {
                        N(SyntaxKind.WhenKeyword);
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.EqualsExpression);
                            {
                                N(SyntaxKind.ConditionalAccessExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "b");
                                    }
                                    N(SyntaxKind.QuestionToken);
                                    N(SyntaxKind.MemberBindingExpression);
                                    {
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Length");
                                        }
                                    }
                                }
                                N(SyntaxKind.EqualsEqualsToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.NotEqualsExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "c");
                                }
                                N(SyntaxKind.ExclamationEqualsToken);
                                N(SyntaxKind.NullLiteralExpression);
                                {
                                    N(SyntaxKind.NullKeyword);
                                }
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Conditional_WhenClause_LegitTernaryWithTargetTypedBothBranches_NoReparse()
    {
        // Critical negative case for the `ParseWhenClause` reparse logic: `when a ? .Y : .Z:` is a
        // legitimate ternary whose when-true/when-false are both target-typed member accesses.  The
        // ternary's `:` is consumed naturally; the case-label `:` follows.  Even though the tree
        // contains a `ConditionalExpression` whose when-true starts with `.`, the reparse must NOT fire
        // because `this.CurrentToken.Kind == SyntaxKind.ColonToken` (the case-label `:`) after the
        // naive parse.  Analogous to `ConditionalAmbiguity3`/`5` in the original collection-expression
        // ambiguity PR (#68756).
        UsingStatement("""
            switch (v)
            {
                case int i when a ? .Y : .Z: break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "v");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.WhenClause);
                    {
                        N(SyntaxKind.WhenKeyword);
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.TargetTypedMemberAccessExpression);
                            {
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Y");
                                }
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.TargetTypedMemberAccessExpression);
                            {
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Z");
                                }
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Conditional_WhenClause_NullConditionalWithPostfixChain_Reparsed()
    {
        // Null-conditional with a postfix chain: `when s?.Length.ToString() == null:`.  Reparse fires
        // (naive parse misinterprets `?.Length.ToString() == null` as a ternary when-true); the
        // ForceConditionalAccessExpression retry produces a `ConditionalAccess` whose whenNotNull is
        // the full `.Length.ToString()` chain.  Analogous to `ConditionalAmbiguity4A` in #68756.
        UsingStatement("""
            switch (v)
            {
                case int i when s?.Length.ToString() == null: break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "v");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.WhenClause);
                    {
                        N(SyntaxKind.WhenKeyword);
                        N(SyntaxKind.EqualsExpression);
                        {
                            N(SyntaxKind.ConditionalAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "s");
                                }
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.InvocationExpression);
                                {
                                    N(SyntaxKind.SimpleMemberAccessExpression);
                                    {
                                        N(SyntaxKind.MemberBindingExpression);
                                        {
                                            N(SyntaxKind.DotToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Length");
                                            }
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "ToString");
                                        }
                                    }
                                    N(SyntaxKind.ArgumentList);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                            }
                            N(SyntaxKind.EqualsEqualsToken);
                            N(SyntaxKind.NullLiteralExpression);
                            {
                                N(SyntaxKind.NullKeyword);
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Conditional_WhenClause_NullConditionalInTernaryWhenFalse_Reparsed()
    {
        // Null-conditional buried in the WhenFalse of a well-formed outer ternary:
        // `when a ? b : c?.Length == 0:`.  The outer `?` is a legitimate ternary (its next token
        // is `b`, not `.` or `[`, so no speculation).  The inner `c?.Length == 0` gets misparsed
        // as a nested ternary whose when-true starts with `.`.  ContainsTernaryToReinterpret walks
        // into the WhenFalse and finds the inner misparse, driving the reparse.  Verifies that the
        // walker recurses beyond the top-level when-true.
        UsingStatement("""
            switch (v)
            {
                case int i when a ? b : c?.Length == 0: break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "v");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.WhenClause);
                    {
                        N(SyntaxKind.WhenKeyword);
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.EqualsExpression);
                            {
                                N(SyntaxKind.ConditionalAccessExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "c");
                                    }
                                    N(SyntaxKind.QuestionToken);
                                    N(SyntaxKind.MemberBindingExpression);
                                    {
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Length");
                                        }
                                    }
                                }
                                N(SyntaxKind.EqualsEqualsToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Conditional_WhenClause_NullConditionalIndexWithPostfixChain_Reparsed()
    {
        // Indexer analogue of `Conditional_WhenClause_NullConditionalWithPostfixChain_Reparsed`:
        // `when arr?[0].ToString() == null:`.  Reparse must preserve the `.ToString()` postfix on
        // the null-conditional element binding.  Analogous to `ConditionalAmbiguity4A` in #68756 but
        // under a case-label `:`.
        UsingStatement("""
            switch (v)
            {
                case int i when arr?[0].ToString() == null: break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "v");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.WhenClause);
                    {
                        N(SyntaxKind.WhenKeyword);
                        N(SyntaxKind.EqualsExpression);
                        {
                            N(SyntaxKind.ConditionalAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "arr");
                                }
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.InvocationExpression);
                                {
                                    N(SyntaxKind.SimpleMemberAccessExpression);
                                    {
                                        N(SyntaxKind.ElementBindingExpression);
                                        {
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
                                            N(SyntaxKind.IdentifierToken, "ToString");
                                        }
                                    }
                                    N(SyntaxKind.ArgumentList);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                            }
                            N(SyntaxKind.EqualsEqualsToken);
                            N(SyntaxKind.NullLiteralExpression);
                            {
                                N(SyntaxKind.NullKeyword);
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Conditional_WhenClause_Ambiguity3_LegitTernaryWithCollectionBranches_NoReparse()
    {
        // Analogue of `ConditionalAmbiguity3` (`a ? [b] : c`) in #68756, under a case-label.
        // Legit ternary whose when-true is a collection expression.  Current token after naive parse is
        // already the case-label `:` — reparse must NOT fire.
        UsingStatement("""
            switch (v)
            {
                case int i when a ? [b] : [c]: break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "v");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.WhenClause);
                    {
                        N(SyntaxKind.WhenKeyword);
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.CollectionExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.ExpressionElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "b");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.CollectionExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.ExpressionElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "c");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Conditional_WhenClause_Ambiguity4_CollectionVariant_Reparsed()
    {
        // Analogue of `ConditionalAmbiguity4` (`a ? b?[c] : d`) in #68756.  The inner `b?[c]` is a
        // null-conditional-index buried inside the outer ternary's when-true.  Reparse fires because
        // the naive parse interprets the inner `?[` as a nested ternary, leaving us beyond the
        // case-label `:`.
        UsingStatement("""
            switch (v)
            {
                case int i when a ? b?[c] : d: break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "v");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.WhenClause);
                    {
                        N(SyntaxKind.WhenKeyword);
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.ConditionalAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.ElementBindingExpression);
                                {
                                    N(SyntaxKind.BracketedArgumentList);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "c");
                                            }
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                }
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "d");
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Conditional_WhenClause_Ambiguity5_NestedTernaryWithTargetTyped_NoReparse()
    {
        // Analogue of `ConditionalAmbiguity5` (`a ? b ? [c] : d : e`) but with target-typed `.X`:
        // `when a ? b ? .X : d : e:`.  Even though ContainsTernaryToReinterpret would find the inner
        // ternary whose when-true starts with `.`, the reparse does NOT fire because after the naive
        // parse we're already at the case-label `:` (the outer ternary completed with `e` as
        // when-false).  Verifies that the `CurrentToken != ColonToken` guard correctly suppresses
        // reparse for well-formed 3-level nested ternaries.
        UsingStatement("""
            switch (v)
            {
                case int i when a ? b ? .X : d : e: break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "v");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.WhenClause);
                    {
                        N(SyntaxKind.WhenKeyword);
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.ConditionalExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.TargetTypedMemberAccessExpression);
                                {
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                }
                                N(SyntaxKind.ColonToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "d");
                                }
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "e");
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Conditional_WhenClause_Ambiguity6_NullConditionalAccessAsTernaryCondition_NoReparse()
    {
        // Analogue of `ConditionalAmbiguity6` (`a?[c] ? b : d`) in #68756 but with `.X`:
        // `when a?.X ? b : c:`.  The null-conditional access `a?.X` is the TERNARY'S CONDITION, not
        // buried in a when-true.  The speculation at the first `?` correctly returns true
        // (null-conditional) because ParsePossibleRefExpression stops at `?`, not `:`.  Reparse does
        // NOT fire.
        UsingStatement("""
            switch (v)
            {
                case int i when a?.X ? b : c: break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "v");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.WhenClause);
                    {
                        N(SyntaxKind.WhenKeyword);
                        N(SyntaxKind.ConditionalExpression);
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
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                }
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Conditional_WhenClause_Ambiguity6_NullConditionalIndexAsTernaryCondition_NoReparse()
    {
        // Analogue of `ConditionalAmbiguity6` (`a?[c] ? b : d`) in #68756.  Collection-indexer variant
        // of the previous test: the null-conditional INDEXER is the ternary's condition.
        UsingStatement("""
            switch (v)
            {
                case int i when a?[c] ? b : d: break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "v");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.WhenClause);
                    {
                        N(SyntaxKind.WhenKeyword);
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.ConditionalAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.ElementBindingExpression);
                                {
                                    N(SyntaxKind.BracketedArgumentList);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "c");
                                            }
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                }
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "d");
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Conditional_WhenClause_Ambiguity6A_NullConditionalIndexWithPostfixAsTernaryCondition_NoReparse()
    {
        // Analogue of `ConditionalAmbiguity6A` (`a?[c].M() ? b : d`).  The condition is a
        // null-conditional indexer followed by a postfix member-call.  No reparse.
        UsingStatement("""
            switch (v)
            {
                case int i when a?[c].M() ? b : d: break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "v");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.WhenClause);
                    {
                        N(SyntaxKind.WhenKeyword);
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.ConditionalAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.InvocationExpression);
                                {
                                    N(SyntaxKind.SimpleMemberAccessExpression);
                                    {
                                        N(SyntaxKind.ElementBindingExpression);
                                        {
                                            N(SyntaxKind.BracketedArgumentList);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                N(SyntaxKind.Argument);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "c");
                                                    }
                                                }
                                                N(SyntaxKind.CloseBracketToken);
                                            }
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "M");
                                        }
                                    }
                                    N(SyntaxKind.ArgumentList);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "d");
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Conditional_WhenClause_Ambiguity7_NullConditionalIndexWithExtraColon_ParsesAsNestedTernary()
    {
        // Analogue of `ConditionalAmbiguity7` (`a?[c] ? b : d : e`) in #68756.  With TWO `:`s and a
        // trailing case-label `:`, the parser finds enough `:` tokens to fully consume the when-clause
        // expression as a nested ternary: `a ? ([c] ? b : d) : e`.  The `?[` in `a?[c]` does NOT
        // commit to null-conditional because the `?` speculation's ParsePossibleRefExpression greedily
        // continues past `[c]` and parses `[c] ? b : d` as a complete ternary, then sees `:` (for the
        // outer) and returns `false` (reinterpret as ternary).  `ContainsTernaryToReinterpret` doesn't
        // fire because the inner ternary's when-true is `b`, not `.`/`[`, and the outer's when-true is
        // the inner ternary (whose first token is `[c]`'s `[`).  After the naive parse we're already
        // at the case-label `:`, so no reparse.  This test pins the surprising (but legal) result.
        UsingStatement("""
            switch (v)
            {
                case int i when a?[c] ? b : d : e: break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "v");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.WhenClause);
                    {
                        N(SyntaxKind.WhenKeyword);
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.ConditionalExpression);
                            {
                                N(SyntaxKind.CollectionExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "c");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                                N(SyntaxKind.ColonToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "d");
                                }
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "e");
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Conditional_WhenClause_Ambiguity10_NullConditionalIndexContainsLambdaWithTernary_Reparsed()
    {
        // Analogue of `ConditionalAmbiguity10` (`a ? b?[() => x ? [y] : z] : d`) in #68756.  The
        // complex case: a null-conditional indexer contains a lambda whose body is itself a ternary
        // with a collection when-true.  This exercises `ForceConditionalAccessExpression` being reset
        // when entering the lambda body (so the inner `x ? [y] : z` remains a ternary even during the
        // outer force-reparse), while still allowing `b?[...]` to commit to null-conditional on the
        // retry.
        UsingStatement("""
            switch (v)
            {
                case int i when a ? b?[() => x ? [y] : z] : d: break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "v");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.WhenClause);
                    {
                        N(SyntaxKind.WhenKeyword);
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.ConditionalAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.ElementBindingExpression);
                                {
                                    N(SyntaxKind.BracketedArgumentList);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
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
                                                N(SyntaxKind.ConditionalExpression);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "x");
                                                    }
                                                    N(SyntaxKind.QuestionToken);
                                                    N(SyntaxKind.CollectionExpression);
                                                    {
                                                        N(SyntaxKind.OpenBracketToken);
                                                        N(SyntaxKind.ExpressionElement);
                                                        {
                                                            N(SyntaxKind.IdentifierName);
                                                            {
                                                                N(SyntaxKind.IdentifierToken, "y");
                                                            }
                                                        }
                                                        N(SyntaxKind.CloseBracketToken);
                                                    }
                                                    N(SyntaxKind.ColonToken);
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "z");
                                                    }
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                }
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "d");
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Conditional_WhenClause_Ambiguity4_DotVariant_Reparsed()
    {
        // Dot counterpart of `Conditional_WhenClause_Ambiguity4_CollectionVariant_Reparsed`:
        // `when a ? b?.X : d:` — simple null-conditional-access in when-true.  Reparse fires.
        UsingStatement("""
            switch (v)
            {
                case int i when a ? b?.X : d: break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "v");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.WhenClause);
                    {
                        N(SyntaxKind.WhenKeyword);
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.ConditionalAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.MemberBindingExpression);
                                {
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                }
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "d");
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Conditional_WhenClause_Ambiguity4_DotInvocationVariant_Reparsed()
    {
        // Invocation variant of the above: `when a ? b?.X() : d:`.  Reparse fires; the null-conditional
        // access's whenNotNull is an `InvocationExpression` wrapping the `MemberBinding(.X)`.
        UsingStatement("""
            switch (v)
            {
                case int i when a ? b?.X() : d: break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "v");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.WhenClause);
                    {
                        N(SyntaxKind.WhenKeyword);
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.ConditionalAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.InvocationExpression);
                                {
                                    N(SyntaxKind.MemberBindingExpression);
                                    {
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "X");
                                        }
                                    }
                                    N(SyntaxKind.ArgumentList);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "d");
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Conditional_WhenClause_Ambiguity6A_NullConditionalAccessWithPostfixAsTernaryCondition_NoReparse()
    {
        // Dot counterpart of
        // `Conditional_WhenClause_Ambiguity6A_NullConditionalIndexWithPostfixAsTernaryCondition_NoReparse`:
        // `when a?.X.M() ? b : c:`.  The condition is a null-conditional access followed by a postfix
        // member-call.  No reparse.
        UsingStatement("""
            switch (v)
            {
                case int i when a?.X.M() ? b : c: break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "v");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.WhenClause);
                    {
                        N(SyntaxKind.WhenKeyword);
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.ConditionalAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.InvocationExpression);
                                {
                                    N(SyntaxKind.SimpleMemberAccessExpression);
                                    {
                                        N(SyntaxKind.MemberBindingExpression);
                                        {
                                            N(SyntaxKind.DotToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "X");
                                            }
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "M");
                                        }
                                    }
                                    N(SyntaxKind.ArgumentList);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Conditional_WhenClause_Ambiguity6_NullConditionalInvocationAsTernaryCondition_NoReparse()
    {
        // Invocation variant of the null-conditional-as-ternary-condition tests:
        // `when a?.X() ? b : c:`.  No reparse.
        UsingStatement("""
            switch (v)
            {
                case int i when a?.X() ? b : c: break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "v");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.WhenClause);
                    {
                        N(SyntaxKind.WhenKeyword);
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.ConditionalAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.InvocationExpression);
                                {
                                    N(SyntaxKind.MemberBindingExpression);
                                    {
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "X");
                                        }
                                    }
                                    N(SyntaxKind.ArgumentList);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Conditional_WhenClause_Ambiguity7_NullConditionalAccessWithExtraColon_ParsesAsNestedTernary()
    {
        // Dot counterpart of
        // `Conditional_WhenClause_Ambiguity7_NullConditionalIndexWithExtraColon_ParsesAsNestedTernary`:
        // `when a?.X ? b : c : d:`.  Parallel to the collection version, this parses as a nested
        // ternary `a ? (.X ? b : c) : d` — the target-typed `.X` is promoted to an inner-ternary
        // condition because ParsePossibleRefExpression greedily consumes `.X ? b : c` during the
        // speculation for `a?.X`, sees `:` (for the outer ternary's separator), and returns `false`
        // (reinterpret as ternary).
        UsingStatement("""
            switch (v)
            {
                case int i when a?.X ? b : c : d: break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "v");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.WhenClause);
                    {
                        N(SyntaxKind.WhenKeyword);
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.ConditionalExpression);
                            {
                                N(SyntaxKind.TargetTypedMemberAccessExpression);
                                {
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                }
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                                N(SyntaxKind.ColonToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "c");
                                }
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "d");
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Conditional_WhenClause_Ambiguity10_NullConditionalMethodCallContainsLambdaWithTernary_Reparsed()
    {
        // Dot/invocation counterpart of
        // `Conditional_WhenClause_Ambiguity10_NullConditionalIndexContainsLambdaWithTernary_Reparsed`:
        // `when a ? b?.M(() => x ? .Y : .Z) : d:`.  A null-conditional method call whose argument is a
        // lambda whose body is a ternary with target-typed branches.  Exercises the
        // `ForceConditionalAccessExpression` reset on lambda entry.
        UsingStatement("""
            switch (v)
            {
                case int i when a ? b?.M(() => x ? .Y : .Z) : d: break;
            }
            """);

        N(SyntaxKind.SwitchStatement);
        {
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "v");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchSection);
            {
                N(SyntaxKind.CasePatternSwitchLabel);
                {
                    N(SyntaxKind.CaseKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.WhenClause);
                    {
                        N(SyntaxKind.WhenKeyword);
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.ConditionalAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.InvocationExpression);
                                {
                                    N(SyntaxKind.MemberBindingExpression);
                                    {
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "M");
                                        }
                                    }
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
                                                N(SyntaxKind.ConditionalExpression);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "x");
                                                    }
                                                    N(SyntaxKind.QuestionToken);
                                                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                                                    {
                                                        N(SyntaxKind.DotToken);
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "Y");
                                                        }
                                                    }
                                                    N(SyntaxKind.ColonToken);
                                                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                                                    {
                                                        N(SyntaxKind.DotToken);
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "Z");
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "d");
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                }
                N(SyntaxKind.BreakStatement);
                {
                    N(SyntaxKind.BreakKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void Conditional_RangePrefix_NotConfusedWithTargetTyped()
    {
        // `?..` is unchanged: a ternary with a prefix range expression as its when-true.
        UsingExpression("x ? ..5 : y");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "5");
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "y");
            }
        }
        EOF();
    }

    #endregion

    #region Mixing with collection expressions

    [Fact]
    public void Conditional_WithCollectionExpressions_BothBranches()
    {
        UsingExpression("x ? [.A, .B] : [.C, .D]");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "D");
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Conditional_CollectionThenTargetTyped()
    {
        UsingExpression("x ? [.A] : .C");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "C");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void Conditional_TargetTypedThenCollection()
    {
        UsingExpression("x ? .A : [.B]");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.TargetTypedMemberAccessExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalAccess_ElementBinding_BaselineUnchanged()
    {
        UsingExpression("x?[.Y]");

        N(SyntaxKind.ConditionalAccessExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ElementBindingExpression);
            {
                N(SyntaxKind.BracketedArgumentList);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.TargetTypedMemberAccessExpression);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Y");
                            }
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
        }
        EOF();
    }

    [Fact]
    public void Conditional_WithCollectionInTrueBranch_AndColon()
    {
        UsingExpression("x ? [.Y] : z");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.TargetTypedMemberAccessExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Y");
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "z");
            }
        }
        EOF();
    }

    #endregion

    #region Mixing with conditional access

    [Fact]
    public void NullConditional_Chained()
    {
        UsingExpression("x?.Y?.Z");

        N(SyntaxKind.ConditionalAccessExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.MemberBindingExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Y");
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.MemberBindingExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Z");
                    }
                }
            }
        }
        EOF();
    }

    [Fact]
    public void ParenthesizedTargetTyped_WithConditionalAccess()
    {
        // `(.A)?.B` — parser-resilient; `(.A)` is a parenthesized target-typed member access,
        // followed by a null-conditional access.
        UsingExpression("(.A)?.B");

        N(SyntaxKind.ConditionalAccessExpression);
        {
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.TargetTypedMemberAccessExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.MemberBindingExpression);
            {
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "B");
                }
            }
        }
        EOF();
    }

    #endregion

    #region Error recovery

    [Fact]
    public void RecoveryDotOnly()
    {
        // A lone `.` (not followed by an identifier) does NOT enter the target-typed member access path; it falls
        // through to the default `ERR_InvalidExprTerm` recovery, matching pre-feature behavior.
        UsingExpression(".",
            // (1,1): error CS1525: Invalid expression term '.'
            // .
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ".").WithArguments(".").WithLocation(1, 1),
            // (1,2): error CS1001: Identifier expected
            // .
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 2));

        N(SyntaxKind.SimpleMemberAccessExpression);
        {
            M(SyntaxKind.IdentifierName);
            {
                M(SyntaxKind.IdentifierToken);
            }
            N(SyntaxKind.DotToken);
            M(SyntaxKind.IdentifierName);
            {
                M(SyntaxKind.IdentifierToken);
            }
        }
        EOF();
    }

    [Fact]
    public void RecoveryDotOpenParen()
    {
        // `.(` does NOT enter the target-typed member access path (no identifier after `.`); the `.` is treated as
        // a post-primary member access continuation on a missing left-hand expression.
        UsingExpression(".()",
            // (1,1): error CS1525: Invalid expression term '.'
            // .()
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ".").WithArguments(".").WithLocation(1, 1),
            // (1,2): error CS1001: Identifier expected
            // .()
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 2));

        N(SyntaxKind.InvocationExpression);
        {
            N(SyntaxKind.SimpleMemberAccessExpression);
            {
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.DotToken);
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
            }
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
        }
        EOF();
    }

    [Fact]
    public void RecoveryDotColonColonIdentifier()
    {
        // `.Goo::Bar` — the `.Goo` parses as TargetTypedQualifiedName; the `::Bar` continuation is invalid (alias
        // qualification requires a plain identifier on the left), and the parser recovers by treating `::` as a
        // missing `.` with ERR_UnexpectedAliasedName.
        UsingExpression("new .Goo::Bar()",
            // (1,9): error CS7000: Unexpected use of an aliased name
            // new .Goo::Bar()
            Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "::").WithLocation(1, 9));

        N(SyntaxKind.ObjectCreationExpression);
        {
            N(SyntaxKind.NewKeyword);
            N(SyntaxKind.QualifiedName);
            {
                N(SyntaxKind.TargetTypedQualifiedName);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Goo");
                    }
                }
                M(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Bar");
                }
            }
            N(SyntaxKind.ArgumentList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
        }
        EOF();
    }

    // The following theories guard against the target-typed dispatch ever being reached when the `.` is not
    // followed by an identifier.  Dots are extremely common in malformed / partial code, so the parser must not
    // accidentally produce a TargetTypedMemberAccessExpression or TargetTypedQualifiedName in any of these
    // degenerate positions.  We assert shape only indirectly: the tree must contain no target-typed nodes.

    [Theory]
    [InlineData(".")]
    [InlineData(". ")]
    [InlineData(".(")]
    [InlineData(".()")]
    [InlineData(".[")]
    [InlineData(".[0]")]
    [InlineData(".<T>")]
    [InlineData(".5")]
    [InlineData(".+")]
    [InlineData(".-")]
    [InlineData(".*")]
    [InlineData(".,")]
    [InlineData(".;")]
    [InlineData(".)")]
    [InlineData(".}")]
    [InlineData(".=")]
    [InlineData(".==")]
    [InlineData(".\"hi\"")]
    [InlineData(".@")]
    [InlineData(".if")]                  // reserved keyword — not a true identifier
    [InlineData("x + .")]
    [InlineData("Goo(.)")]
    [InlineData("[.]")]
    [InlineData("x ? . : y")]
    public void DotNotFollowedByIdentifier_InExpression_DoesNotProduceTargetTypedNodes(string source)
    {
        var tree = SyntaxFactory.ParseExpression(source);

        Assert.Empty(tree.DescendantNodesAndSelf().OfType<TargetTypedMemberAccessExpressionSyntax>());
        Assert.Empty(tree.DescendantNodesAndSelf().OfType<TargetTypedQualifiedNameSyntax>());
    }

    [Theory]
    [InlineData("class C { void M() { new .; } }")]
    [InlineData("class C { void M() { new .(); } }")]
    [InlineData("class C { void M() { new .[10]; } }")]
    [InlineData("class C { void M() { new .<T>; } }")]
    [InlineData("class C { void M() { new .5; } }")]
    [InlineData("class C { void M() { new .+; } }")]
    [InlineData("class C { void M() { new . ; } }")]
    [InlineData("class C { void M() { _ = typeof(.); } }")]
    [InlineData("class C { void M() { _ = typeof(.<T>); } }")]
    [InlineData("class C { void M() { _ = default(.); } }")]
    [InlineData("class C { void M() { _ = sizeof(.); } }")]
    [InlineData("class C { void M() { _ = (.)x; } }")]
    [InlineData("class C { void M(object o) { _ = o is .; } }")]
    [InlineData("class C { void M(object o) { _ = o is .(var x); } }")]
    [InlineData("class C { void M(object o) { _ = o is .{ P: 1 }; } }")]
    [InlineData("class C { void M(object o) { _ = o is .<T>; } }")]
    [InlineData("class C { void M(object o) { _ = o is .5; } }")]
    [InlineData("class C { void M(object o) { switch (o) { case .: break; } } }")]
    [InlineData("class C { void M(object o) { switch (o) { case .<T>: break; } } }")]
    public void DotNotFollowedByIdentifier_InTypeOrPattern_DoesNotProduceTargetTypedNodes(string source)
    {
        var tree = SyntaxFactory.ParseSyntaxTree(source);

        Assert.Empty(tree.GetRoot().DescendantNodesAndSelf().OfType<TargetTypedMemberAccessExpressionSyntax>());
        Assert.Empty(tree.GetRoot().DescendantNodesAndSelf().OfType<TargetTypedQualifiedNameSyntax>());
    }

    #endregion
}
