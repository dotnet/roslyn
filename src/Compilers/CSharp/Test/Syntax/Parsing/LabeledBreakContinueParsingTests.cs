// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class LabeledBreakContinueParsingTests : ParsingTests
{
    public LabeledBreakContinueParsingTests(ITestOutputHelper output) : base(output) { }

    #region Language version agnostic

    [Theory, CombinatorialData]
    public void LabeledBreak_AllLanguageVersions(LanguageVersion version)
    {
        UsingStatement("break myLabel;", TestOptions.Regular.WithLanguageVersion(version));
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "myLabel");
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void LabeledContinue_AllLanguageVersions(LanguageVersion version)
    {
        UsingStatement("continue myLabel;", TestOptions.Regular.WithLanguageVersion(version));
        N(SyntaxKind.ContinueStatement);
        {
            N(SyntaxKind.ContinueKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "myLabel");
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    #endregion

    #region Valid labeled forms

    [Fact]
    public void LabeledBreak()
    {
        UsingStatement("break myLabel;");
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "myLabel");
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void LabeledContinue()
    {
        UsingStatement("continue myLabel;");
        N(SyntaxKind.ContinueStatement);
        {
            N(SyntaxKind.ContinueKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "myLabel");
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void UnlabeledBreak()
    {
        UsingStatement("break;");
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void UnlabeledContinue()
    {
        UsingStatement("continue;");
        N(SyntaxKind.ContinueStatement);
        {
            N(SyntaxKind.ContinueKeyword);
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void LabeledBreak_EscapedKeyword()
    {
        UsingStatement("break @class;");
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "@class");
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void LabeledContinue_EscapedKeyword()
    {
        UsingStatement("continue @while;");
        N(SyntaxKind.ContinueStatement);
        {
            N(SyntaxKind.ContinueKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "@while");
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    #endregion

    #region Contextual keywords as labels

    public static IEnumerable<object[]> ContextualKeywords
        => SyntaxFacts.GetContextualKeywordKinds().Select(k => new object[] { SyntaxFacts.GetText(k) });

    [Fact]
    public void Break_AwaitAsLabel_InsideAsyncMethod()
    {
        UsingDeclaration("""
            async void M()
            {
                while (true)
                {
                    break await;
                }
            }
            """,
            options: null,
            // (5,15): error CS4003: 'await' cannot be used as an identifier within an async method or lambda expression
            //             break await;
            Diagnostic(ErrorCode.ERR_BadAwaitAsIdentifier, "await").WithLocation(5, 15));
        N(SyntaxKind.MethodDeclaration);
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
                N(SyntaxKind.WhileStatement);
                {
                    N(SyntaxKind.WhileKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.TrueLiteralExpression);
                    {
                        N(SyntaxKind.TrueKeyword);
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.BreakStatement);
                        {
                            N(SyntaxKind.BreakKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "await");
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Break_FieldAsLabel_InsidePropertyAccessor()
    {
        UsingDeclaration("""
            int P
            {
                get
                {
                    while (true)
                    {
                        break field;
                    }
                }
            }
            """,
            options: null);
        N(SyntaxKind.PropertyDeclaration);
        {
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
            }
            N(SyntaxKind.IdentifierToken, "P");
            N(SyntaxKind.AccessorList);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.GetAccessorDeclaration);
                {
                    N(SyntaxKind.GetKeyword);
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.WhileStatement);
                        {
                            N(SyntaxKind.WhileKeyword);
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.TrueLiteralExpression);
                            {
                                N(SyntaxKind.TrueKeyword);
                            }
                            N(SyntaxKind.CloseParenToken);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.BreakStatement);
                                {
                                    N(SyntaxKind.BreakKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "field");
                                    }
                                    N(SyntaxKind.SemicolonToken);
                                }
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Break_ValueAsLabel_InsidePropertySetter()
    {
        UsingDeclaration("""
            int P
            {
                set
                {
                    while (true)
                    {
                        break value;
                    }
                }
            }
            """,
            options: null);
        N(SyntaxKind.PropertyDeclaration);
        {
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
            }
            N(SyntaxKind.IdentifierToken, "P");
            N(SyntaxKind.AccessorList);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SetAccessorDeclaration);
                {
                    N(SyntaxKind.SetKeyword);
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.WhileStatement);
                        {
                            N(SyntaxKind.WhileKeyword);
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.TrueLiteralExpression);
                            {
                                N(SyntaxKind.TrueKeyword);
                            }
                            N(SyntaxKind.CloseParenToken);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.BreakStatement);
                                {
                                    N(SyntaxKind.BreakKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "value");
                                    }
                                    N(SyntaxKind.SemicolonToken);
                                }
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Theory]
    [MemberData(nameof(ContextualKeywords))]
    public void Break_ContextualKeywordAsLabel(string keyword)
    {
        UsingStatement($"break {keyword};");
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, keyword);
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Theory]
    [MemberData(nameof(ContextualKeywords))]
    public void Continue_ContextualKeywordAsLabel(string keyword)
    {
        UsingStatement($"continue {keyword};");
        N(SyntaxKind.ContinueStatement);
        {
            N(SyntaxKind.ContinueKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, keyword);
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    #endregion

    #region Non-identifier tokens after break/continue

    [Fact]
    public void Break_NumericLiteral()
    {
        UsingStatement("break 0;",
            // (1,1): error CS1073: Unexpected token '0'
            // break 0;
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "break ").WithArguments("0").WithLocation(1, 1),
            // (1,7): error CS1002: ; expected
            // break 0;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "0").WithLocation(1, 7));
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Continue_NumericLiteral()
    {
        UsingStatement("continue 0;",
            // (1,1): error CS1073: Unexpected token '0'
            // continue 0;
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "continue ").WithArguments("0").WithLocation(1, 1),
            // (1,10): error CS1002: ; expected
            // continue 0;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "0").WithLocation(1, 10));
        N(SyntaxKind.ContinueStatement);
        {
            N(SyntaxKind.ContinueKeyword);
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Break_CaseKeyword()
    {
        UsingStatement("break case;",
            // (1,1): error CS1073: Unexpected token 'case'
            // break case;
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "break ").WithArguments("case").WithLocation(1, 1),
            // (1,7): error CS1002: ; expected
            // break case;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "case").WithLocation(1, 7));
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Break_ReturnKeyword()
    {
        UsingStatement("break return;",
            // (1,1): error CS1073: Unexpected token 'return'
            // break return;
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "break ").WithArguments("return").WithLocation(1, 1),
            // (1,7): error CS1002: ; expected
            // break return;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "return").WithLocation(1, 7));
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Break_IntKeyword()
    {
        UsingStatement("break int;",
            // (1,1): error CS1073: Unexpected token 'int'
            // break int;
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "break ").WithArguments("int").WithLocation(1, 1),
            // (1,7): error CS1002: ; expected
            // break int;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "int").WithLocation(1, 7));
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Break_BreakKeyword()
    {
        UsingStatement("break break a;",
            // (1,1): error CS1073: Unexpected token 'break'
            // break break a;
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "break ").WithArguments("break").WithLocation(1, 1),
            // (1,7): error CS1002: ; expected
            // break break a;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "break").WithLocation(1, 7));
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Break_ContinueKeyword()
    {
        UsingStatement("break continue a;",
            // (1,1): error CS1073: Unexpected token 'continue'
            // break continue a;
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "break ").WithArguments("continue").WithLocation(1, 1),
            // (1,7): error CS1002: ; expected
            // break continue a;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "continue").WithLocation(1, 7));
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Continue_ContinueKeyword()
    {
        UsingStatement("continue continue a;",
            // (1,1): error CS1073: Unexpected token 'continue'
            // continue continue a;
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "continue ").WithArguments("continue").WithLocation(1, 1),
            // (1,10): error CS1002: ; expected
            // continue continue a;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "continue").WithLocation(1, 10));
        N(SyntaxKind.ContinueStatement);
        {
            N(SyntaxKind.ContinueKeyword);
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Continue_BreakKeyword()
    {
        UsingStatement("continue break a;",
            // (1,1): error CS1073: Unexpected token 'break'
            // continue break a;
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "continue ").WithArguments("break").WithLocation(1, 1),
            // (1,10): error CS1002: ; expected
            // continue break a;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "break").WithLocation(1, 10));
        N(SyntaxKind.ContinueStatement);
        {
            N(SyntaxKind.ContinueKeyword);
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Continue_CaseKeyword()
    {
        UsingStatement("continue case;",
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "continue ").WithArguments("case").WithLocation(1, 1),
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "case").WithLocation(1, 10));
        N(SyntaxKind.ContinueStatement);
        {
            N(SyntaxKind.ContinueKeyword);
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Continue_ReturnKeyword()
    {
        UsingStatement("continue return;",
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "continue ").WithArguments("return").WithLocation(1, 1),
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "return").WithLocation(1, 10));
        N(SyntaxKind.ContinueStatement);
        {
            N(SyntaxKind.ContinueKeyword);
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Continue_IntKeyword()
    {
        UsingStatement("continue int;",
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "continue ").WithArguments("int").WithLocation(1, 1),
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "int").WithLocation(1, 10));
        N(SyntaxKind.ContinueStatement);
        {
            N(SyntaxKind.ContinueKeyword);
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Break_DefaultKeyword()
    {
        // Unlike `goto default;`, there is no `break default;` form. `default` is a reserved
        // keyword so it is not accepted as a label identifier.
        UsingStatement("break default;",
            // (1,1): error CS1073: Unexpected token 'default'
            // break default;
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "break ").WithArguments("default").WithLocation(1, 1),
            // (1,7): error CS1002: ; expected
            // break default;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "default").WithLocation(1, 7));
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Continue_DefaultKeyword()
    {
        UsingStatement("continue default;",
            // (1,1): error CS1073: Unexpected token 'default'
            // continue default;
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "continue ").WithArguments("default").WithLocation(1, 1),
            // (1,10): error CS1002: ; expected
            // continue default;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "default").WithLocation(1, 10));
        N(SyntaxKind.ContinueStatement);
        {
            N(SyntaxKind.ContinueKeyword);
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Break_ThisKeyword()
    {
        UsingStatement("break this;",
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "break ").WithArguments("this").WithLocation(1, 1),
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "this").WithLocation(1, 7));
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Break_NullKeyword()
    {
        UsingStatement("break null;",
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "break ").WithArguments("null").WithLocation(1, 1),
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "null").WithLocation(1, 7));
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Break_TrueKeyword()
    {
        UsingStatement("break true;",
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "break ").WithArguments("true").WithLocation(1, 1),
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "true").WithLocation(1, 7));
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Break_FalseKeyword()
    {
        UsingStatement("break false;",
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "break ").WithArguments("false").WithLocation(1, 1),
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "false").WithLocation(1, 7));
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Break_ParenthesizedExpression()
    {
        UsingStatement("break (a);",
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "break ").WithArguments("(").WithLocation(1, 1),
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "(").WithLocation(1, 7));
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Break_StringLiteral()
    {
        UsingStatement("""break "label";""",
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "break ").WithArguments(@"""label""").WithLocation(1, 1),
            Diagnostic(ErrorCode.ERR_SemicolonExpected, @"""label""").WithLocation(1, 7));
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    #endregion

    #region Expressions after break/continue (only simple identifier consumed)

    [Fact]
    public void Break_ExpressionNotConsumed()
    {
        UsingStatement("break a + b;",
            // (1,1): error CS1073: Unexpected token '+'
            // break a + b;
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "break a ").WithArguments("+").WithLocation(1, 1),
            // (1,9): error CS1002: ; expected
            // break a + b;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "+").WithLocation(1, 9));
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Continue_ExpressionNotConsumed()
    {
        UsingStatement("continue a + b;",
            // (1,1): error CS1073: Unexpected token '+'
            // continue a + b;
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "continue a ").WithArguments("+").WithLocation(1, 1),
            // (1,12): error CS1002: ; expected
            // continue a + b;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "+").WithLocation(1, 12));
        N(SyntaxKind.ContinueStatement);
        {
            N(SyntaxKind.ContinueKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Break_DottedNameNotConsumed()
    {
        UsingStatement("break a.b;",
            // (1,1): error CS1073: Unexpected token '.'
            // break a.b;
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "break a").WithArguments(".").WithLocation(1, 1),
            // (1,8): error CS1002: ; expected
            // break a.b;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ".").WithLocation(1, 8));
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Continue_DottedNameNotConsumed()
    {
        UsingStatement("continue a.b;",
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "continue a").WithArguments(".").WithLocation(1, 1),
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ".").WithLocation(1, 11));
        N(SyntaxKind.ContinueStatement);
        {
            N(SyntaxKind.ContinueKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Continue_DottedNameMultipleMembersNotConsumed()
    {
        UsingStatement("continue a.b.c;",
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "continue a").WithArguments(".").WithLocation(1, 1),
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ".").WithLocation(1, 11));
        N(SyntaxKind.ContinueStatement);
        {
            N(SyntaxKind.ContinueKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Break_ExtraIdentifierAfterLabel()
    {
        UsingStatement("break a b;",
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "break a ").WithArguments("b").WithLocation(1, 1),
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "b").WithLocation(1, 9));
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Continue_ExtraIdentifierAfterLabel()
    {
        UsingStatement("continue a b;",
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "continue a ").WithArguments("b").WithLocation(1, 1),
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "b").WithLocation(1, 12));
        N(SyntaxKind.ContinueStatement);
        {
            N(SyntaxKind.ContinueKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Break_LabelFollowedByOpenBrace()
    {
        UsingStatement("break a {",
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "break a ").WithArguments("{").WithLocation(1, 1),
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(1, 9));
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    #endregion

    #region Missing semicolon

    [Fact]
    public void LabeledBreak_MissingSemicolon()
    {
        UsingStatement("break myLabel",
            // (1,14): error CS1002: ; expected
            // break myLabel
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 14));
        N(SyntaxKind.BreakStatement);
        {
            N(SyntaxKind.BreakKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "myLabel");
            }
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void LabeledContinue_MissingSemicolon()
    {
        UsingStatement("continue myLabel",
            // (1,17): error CS1002: ; expected
            // continue myLabel
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 17));
        N(SyntaxKind.ContinueStatement);
        {
            N(SyntaxKind.ContinueKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "myLabel");
            }
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    #endregion

    #region yield break not affected

    [Fact]
    public void YieldBreak_NotAffected()
    {
        UsingStatement("yield break;");
        N(SyntaxKind.YieldBreakStatement);
        {
            N(SyntaxKind.YieldKeyword);
            N(SyntaxKind.BreakKeyword);
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void YieldBreak_WithIdentifier()
    {
        // `yield break a;` still parses as YieldBreakStatement. The `a` is unconsumed.
        UsingStatement("yield break a;",
            // (1,1): error CS1073: Unexpected token 'a'
            // yield break a;
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "yield break ").WithArguments("a").WithLocation(1, 1),
            // (1,13): error CS1002: ; expected
            // yield break a;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "a").WithLocation(1, 13));
        N(SyntaxKind.YieldBreakStatement);
        {
            N(SyntaxKind.YieldKeyword);
            N(SyntaxKind.BreakKeyword);
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    #endregion

}
