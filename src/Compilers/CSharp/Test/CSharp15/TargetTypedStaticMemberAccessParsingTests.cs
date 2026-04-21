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
    [InlineData(LanguageVersion.CSharp13)]
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
