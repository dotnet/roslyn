// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class PartialEventsAndConstructorsParsingTests(ITestOutputHelper output) : ParsingTests(output)
{
    [Fact]
    public void Event_Tree()
    {
        UsingTree("""
            partial class C
            {
                partial event Action E;
                partial event Action E { add { } remove { } }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.EventFieldDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.EventKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Action");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "E");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EventDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.EventKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Action");
                    }
                    N(SyntaxKind.IdentifierToken, "E");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.AddAccessorDeclaration);
                        {
                            N(SyntaxKind.AddKeyword);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.RemoveAccessorDeclaration);
                        {
                            N(SyntaxKind.RemoveKeyword);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
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
    public void Event_Definition()
    {
        UsingDeclaration("""
            partial event Action E;
            """);

        N(SyntaxKind.EventFieldDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.EventKeyword);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Action");
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "E");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Event_Definition_Multiple()
    {
        UsingDeclaration("""
            partial event Action E, F;
            """);

        N(SyntaxKind.EventFieldDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.EventKeyword);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Action");
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "E");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "F");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Event_Definition_Initializer()
    {
        UsingDeclaration("""
            partial event Action E = null;
            """);

        N(SyntaxKind.EventFieldDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.EventKeyword);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Action");
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "E");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NullLiteralExpression);
                        {
                            N(SyntaxKind.NullKeyword);
                        }
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Event_Definition_Multiple_Initializer()
    {
        UsingDeclaration("""
            partial event Action E, F = null;
            """);

        N(SyntaxKind.EventFieldDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.EventKeyword);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Action");
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "E");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "F");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NullLiteralExpression);
                        {
                            N(SyntaxKind.NullKeyword);
                        }
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Event_Definition_Multiple_Initializers()
    {
        UsingDeclaration("""
            partial event Action E = null, F = null;
            """);

        N(SyntaxKind.EventFieldDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.EventKeyword);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Action");
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "E");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NullLiteralExpression);
                        {
                            N(SyntaxKind.NullKeyword);
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "F");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NullLiteralExpression);
                        {
                            N(SyntaxKind.NullKeyword);
                        }
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Event_Definition_PartialAfterEvent()
    {
        UsingDeclaration("""
            event partial Action E;
            """,
            null,
            // (1,7): error CS1031: Type expected
            // event partial Action E;
            Diagnostic(ErrorCode.ERR_TypeExpected, "partial").WithLocation(1, 7),
            // (1,7): error CS1525: Invalid expression term 'partial'
            // event partial Action E;
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(1, 7),
            // (1,7): error CS1003: Syntax error, ',' expected
            // event partial Action E;
            Diagnostic(ErrorCode.ERR_SyntaxError, "partial").WithArguments(",").WithLocation(1, 7));

        N(SyntaxKind.EventFieldDeclaration);
        {
            N(SyntaxKind.EventKeyword);
            M(SyntaxKind.VariableDeclaration);
            {
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
                M(SyntaxKind.VariableDeclarator);
                {
                    M(SyntaxKind.IdentifierToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Event_Definition_PartialAfterType()
    {
        UsingDeclaration("""
            event Action partial E;
            """,
            null,
            // (1,22): error CS1003: Syntax error, ',' expected
            // event Action partial E;
            Diagnostic(ErrorCode.ERR_SyntaxError, "E").WithArguments(",").WithLocation(1, 22));

        N(SyntaxKind.EventFieldDeclaration);
        {
            N(SyntaxKind.EventKeyword);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Action");
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "partial");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Event_Definition_PartialAfterPublic()
    {
        UsingDeclaration("""
            public partial event Action E;
            """);

        N(SyntaxKind.EventFieldDeclaration);
        {
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.EventKeyword);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Action");
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "E");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Event_Definition_PartialBeforePublic()
    {
        UsingDeclaration("""
            partial public event Action E;
            """);

        N(SyntaxKind.EventFieldDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.EventKeyword);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Action");
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "E");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Event_Definition_DoublePartial()
    {
        UsingDeclaration("""
            partial partial event Action E;
            """,
            null,
            // (1,9): error CS1525: Invalid expression term 'partial'
            // partial partial event Action E;
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(1, 9),
            // (1,9): error CS1003: Syntax error, ',' expected
            // partial partial event Action E;
            Diagnostic(ErrorCode.ERR_SyntaxError, "partial").WithArguments(",").WithLocation(1, 9));

        N(SyntaxKind.FieldDeclaration);
        {
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "partial");
                }
                M(SyntaxKind.VariableDeclarator);
                {
                    M(SyntaxKind.IdentifierToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Event_Definition_MissingRest()
    {
        UsingDeclaration("""
            partial event
            """,
            null,
            // (1,14): error CS1031: Type expected
            // partial event
            Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(1, 14),
            // (1,14): error CS1514: { expected
            // partial event
            Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(1, 14),
            // (1,14): error CS1513: } expected
            // partial event
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 14));

        N(SyntaxKind.EventDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.EventKeyword);
            M(SyntaxKind.IdentifierName);
            {
                M(SyntaxKind.IdentifierToken);
            }
            M(SyntaxKind.IdentifierToken);
            M(SyntaxKind.AccessorList);
            {
                M(SyntaxKind.OpenBraceToken);
                M(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Event_Implementation()
    {
        UsingDeclaration("""
            partial event Action E { add { } remove { } }
            """);

        N(SyntaxKind.EventDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.EventKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Action");
            }
            N(SyntaxKind.IdentifierToken, "E");
            N(SyntaxKind.AccessorList);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.AddAccessorDeclaration);
                {
                    N(SyntaxKind.AddKeyword);
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.RemoveAccessorDeclaration);
                {
                    N(SyntaxKind.RemoveKeyword);
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Event_Implementation_Multiple()
    {
        UsingDeclaration("""
            partial event Action E, F { add { } remove { } }
            """,
            null,
            // (1,27): error CS1003: Syntax error, ',' expected
            // partial event Action E, F { add { } remove { } }
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(1, 27),
            // (1,49): error CS1002: ; expected
            // partial event Action E, F { add { } remove { } }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 49));

        N(SyntaxKind.EventFieldDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.EventKeyword);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Action");
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "E");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "F");
                }
            }
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Event_Implementation_PartialAfterEvent()
    {
        UsingDeclaration("""
            event partial Action E { add { } remove { } }
            """,
            null,
            // (1,7): error CS1031: Type expected
            // event partial Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_TypeExpected, "partial").WithLocation(1, 7),
            // (1,7): error CS1525: Invalid expression term 'partial'
            // event partial Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(1, 7),
            // (1,7): error CS1003: Syntax error, ',' expected
            // event partial Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_SyntaxError, "partial").WithArguments(",").WithLocation(1, 7),
            // (1,46): error CS1002: ; expected
            // event partial Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 46));

        N(SyntaxKind.EventFieldDeclaration);
        {
            N(SyntaxKind.EventKeyword);
            M(SyntaxKind.VariableDeclaration);
            {
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
                M(SyntaxKind.VariableDeclarator);
                {
                    M(SyntaxKind.IdentifierToken);
                }
            }
            M(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Event_Implementation_SemicolonAccessors()
    {
        UsingDeclaration("""
            partial event Action E { add; remove; }
            """);

        N(SyntaxKind.EventDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.EventKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Action");
            }
            N(SyntaxKind.IdentifierToken, "E");
            N(SyntaxKind.AccessorList);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.AddAccessorDeclaration);
                {
                    N(SyntaxKind.AddKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.RemoveAccessorDeclaration);
                {
                    N(SyntaxKind.RemoveKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Event_Implementation_PartialAccessors()
    {
        UsingDeclaration("""
            partial event Action E { partial add; partial remove; }
            """,
            null,
            // (1,26): error CS1055: An add or remove accessor expected
            // partial event Action E { partial add; partial remove; }
            Diagnostic(ErrorCode.ERR_AddOrRemoveExpected, "partial").WithLocation(1, 26),
            // (1,39): error CS1055: An add or remove accessor expected
            // partial event Action E { partial add; partial remove; }
            Diagnostic(ErrorCode.ERR_AddOrRemoveExpected, "partial").WithLocation(1, 39));

        N(SyntaxKind.EventDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.EventKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "Action");
            }
            N(SyntaxKind.IdentifierToken, "E");
            N(SyntaxKind.AccessorList);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.UnknownAccessorDeclaration);
                {
                    N(SyntaxKind.IdentifierToken, "partial");
                }
                N(SyntaxKind.AddAccessorDeclaration);
                {
                    N(SyntaxKind.AddKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.UnknownAccessorDeclaration);
                {
                    N(SyntaxKind.IdentifierToken, "partial");
                }
                N(SyntaxKind.RemoveAccessorDeclaration);
                {
                    N(SyntaxKind.RemoveKeyword);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void Event_InPlaceOfIdentifier()
    {
        UsingTree("""
            partial class C
            {
                [Attr(
                partial event Action E;
            }
            """,
            // (3,11): error CS1026: ) expected
            //     [Attr(
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(3, 11),
            // (3,11): error CS1003: Syntax error, ']' expected
            //     [Attr(
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("]").WithLocation(3, 11));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.EventFieldDeclaration);
                {
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Attr");
                            }
                            N(SyntaxKind.AttributeArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                M(SyntaxKind.CloseParenToken);
                            }
                        }
                        M(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.EventKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Action");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "E");
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
    public void Constructor_Tree()
    {
        UsingTree("""
            partial class C
            {
                partial C();
                partial C() { }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ConstructorDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ConstructorDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
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
    public void Constructor_ArrowBody()
    {
        UsingDeclaration("""
            partial C() => throw null;
            """);

        N(SyntaxKind.ConstructorDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.IdentifierToken, "C");
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
        EOF();
    }

    [Fact]
    public void Constructor_NoParens()
    {
        UsingDeclaration("""
            partial C;
            """);

        N(SyntaxKind.FieldDeclaration);
        {
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "partial");
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "C");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Constructor_NoName()
    {
        UsingDeclaration("""
            partial ();
            """);

        N(SyntaxKind.ConstructorDeclaration);
        {
            N(SyntaxKind.IdentifierToken, "partial");
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Constructor_PartialAfterName()
    {
        UsingDeclaration("""
            C partial();
            """);

        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "C");
            }
            N(SyntaxKind.IdentifierToken, "partial");
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Constructor_PartialAfterPublic()
    {
        UsingDeclaration("""
            public partial C();
            """);

        N(SyntaxKind.ConstructorDeclaration);
        {
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.IdentifierToken, "C");
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Constructor_PartialBeforePublic()
    {
        UsingDeclaration("""
            partial public C();
            """);

        N(SyntaxKind.ConstructorDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.IdentifierToken, "C");
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Constructor_TypeTwice()
    {
        UsingDeclaration("""
            partial C C();
            """);

        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "C");
            }
            N(SyntaxKind.IdentifierToken, "C");
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Constructor_PartialEscaped()
    {
        UsingDeclaration("""
            @partial C();
            """);

        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "@partial");
            }
            N(SyntaxKind.IdentifierToken, "C");
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void Constructor_KeywordName()
    {
        UsingDeclaration("""
            partial const();
            """,
            null,
            // (1,1): error CS1073: Unexpected token 'const'
            // partial const();
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "partial").WithArguments("const").WithLocation(1, 1),
            // (1,9): error CS1519: Invalid token 'const' in class, record, struct, or interface member declaration
            // partial const();
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "const").WithArguments("const").WithLocation(1, 9));

        N(SyntaxKind.IncompleteMember);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "partial");
            }
        }
        EOF();
    }

    [Fact]
    public void Constructor_InPlaceOfIdentifier()
    {
        UsingTree("""
            partial class C
            {
                [Attr(
                partial C();
            }
            """,
            // (3,11): error CS1026: ) expected
            //     [Attr(
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(3, 11),
            // (3,11): error CS1003: Syntax error, ']' expected
            //     [Attr(
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("]").WithLocation(3, 11));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ConstructorDeclaration);
                {
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Attr");
                            }
                            N(SyntaxKind.AttributeArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                M(SyntaxKind.CloseParenToken);
                            }
                        }
                        M(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }
}
