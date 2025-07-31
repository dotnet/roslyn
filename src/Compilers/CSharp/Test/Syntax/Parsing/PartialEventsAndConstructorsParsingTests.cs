// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class PartialEventsAndConstructorsParsingTests(ITestOutputHelper output) : ParsingTests(output)
{
    private sealed class CSharp14_Preview()
        : CombinatorialValuesAttribute(LanguageVersion.CSharp14, LanguageVersion.Preview);

    private sealed class CSharp13_CSharp14_Preview()
        : CombinatorialValuesAttribute(LanguageVersion.CSharp13, LanguageVersion.CSharp14, LanguageVersion.Preview);

    [Theory, CombinatorialData]
    public void Event_Tree([CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingTree("""
            partial class C
            {
                partial event Action E;
                partial event Action E { add { } remove { } }
            }
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion));

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

    [Theory, CombinatorialData]
    public void Event_Definition([CSharp13_CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            partial event Action E;
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion));

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

    [Theory, CombinatorialData]
    public void Event_Definition_Multiple([CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            partial event Action E, F;
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion));

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

    [Theory, CombinatorialData]
    public void Event_Definition_Initializer([CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            partial event Action E = null;
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion));

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

    [Theory, CombinatorialData]
    public void Event_Definition_Multiple_Initializer([CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            partial event Action E, F = null;
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion));

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

    [Theory, CombinatorialData]
    public void Event_Definition_Multiple_Initializers([CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            partial event Action E = null, F = null;
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion));

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

    [Theory, CombinatorialData]
    public void Event_Definition_PartialAfterEvent([CSharp13_CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            event partial Action E;
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion),
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

    [Theory, CombinatorialData]
    public void Event_Definition_PartialAfterType([CSharp13_CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            event Action partial E;
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion),
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

    [Theory, CombinatorialData]
    public void Event_Definition_PartialAfterPublic([CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            public partial event Action E;
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion));

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

    [Theory, CombinatorialData]
    public void Event_Definition_PartialBeforePublic([CSharp13_CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            partial public event Action E;
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion));

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

    [Theory, CombinatorialData]
    public void Event_Definition_DoublePartial([CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            partial partial event Action E;
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion),
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

    [Theory, CombinatorialData]
    public void Event_Definition_MissingRest([CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            partial event
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion),
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

    [Theory, CombinatorialData]
    public void Event_Implementation([CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            partial event Action E { add { } remove { } }
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion));

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

    [Theory, CombinatorialData]
    public void Event_Implementation_Multiple([CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            partial event Action E, F { add { } remove { } }
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion),
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

    [Theory, CombinatorialData]
    public void Event_Implementation_PartialAfterEvent([CSharp13_CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            event partial Action E { add { } remove { } }
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion),
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

    [Theory, CombinatorialData]
    public void Event_Implementation_SemicolonAccessors([CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            partial event Action E { add; remove; }
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion));

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

    [Theory, CombinatorialData]
    public void Event_Implementation_PartialAccessors([CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            partial event Action E { partial add; partial remove; }
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion),
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

    [Theory, CombinatorialData]
    public void Event_InPlaceOfIdentifier([CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingTree("""
            partial class C
            {
                [Attr(
                partial event Action E;
            }
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion),
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

    [Theory, CombinatorialData]
    public void Constructor_Tree([CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingTree("""
            partial class C
            {
                partial C();
                partial C() { }
            }
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion));

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

    [Theory, CombinatorialData]
    public void Constructor_Declaration([CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            partial C() { }
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion));

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
        EOF();
    }

    [Fact]
    public void Constructor_Declaration_CSharp13()
    {
        UsingDeclaration("""
            partial C() { }
            """,
            TestOptions.Regular13);

        N(SyntaxKind.MethodDeclaration);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "partial");
            }
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
        EOF();
    }

    [Theory, CombinatorialData]
    public void Constructor_ArrowBody([CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            partial C() => throw null;
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion));

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

    [Theory, CombinatorialData]
    public void Constructor_NoParens([CSharp13_CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            partial C;
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion));

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

    [Theory, CombinatorialData]
    public void Constructor_NoName([CSharp13_CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            partial ();
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion));

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

    [Theory, CombinatorialData]
    public void Constructor_PartialAsName([CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            partial partial();
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion));

        N(SyntaxKind.ConstructorDeclaration);
        {
            N(SyntaxKind.PartialKeyword);
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

    [Theory, CombinatorialData]
    public void Constructor_PartialAfterName([CSharp13_CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            C partial();
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion));

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

    [Theory, CombinatorialData]
    public void Constructor_PartialAfterPublic([CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            public partial C();
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion));

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

    [Theory, CombinatorialData]
    public void Constructor_PartialBeforePublic([CSharp13_CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            partial public C();
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion));

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

    [Theory, CombinatorialData]
    public void Constructor_TypeTwice([CSharp13_CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            partial C C();
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion));

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

    [Theory, CombinatorialData]
    public void Constructor_PartialEscaped([CSharp13_CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            @partial C();
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion));

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

    [Theory, CombinatorialData]
    public void Constructor_KeywordName([CSharp13_CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingDeclaration("""
            partial const();
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion),
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

    [Theory, CombinatorialData]
    public void Constructor_InPlaceOfIdentifier([CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingTree("""
            partial class C
            {
                [Attr(
                partial C();
            }
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion),
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

    [Theory, CombinatorialData]
    public void ReturningPartialType_LocalFunction_InMethod([CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingTree("""
            class C
            {
                void M()
                {
                    partial F() => null;
                }
            }
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion),
            // (4,6): error CS1513: } expected
            //     {
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 6),
            // (7,1): error CS1022: Type or namespace definition, or end-of-file expected
            // }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(7, 1));

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
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.ConstructorDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.IdentifierToken, "F");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.NullLiteralExpression);
                        {
                            N(SyntaxKind.NullKeyword);
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
    public void ReturningPartialType_LocalFunction_InMethod_CSharp13()
    {
        UsingTree("""
            class C
            {
                void M()
                {
                    partial F() => null;
                }
            }
            """,
            TestOptions.Regular13);

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
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.LocalFunctionStatement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "partial");
                            }
                            N(SyntaxKind.IdentifierToken, "F");
                            N(SyntaxKind.ParameterList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NullLiteralExpression);
                                {
                                    N(SyntaxKind.NullKeyword);
                                }
                            }
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

    [Theory, CombinatorialData]
    public void ReturningPartialType_LocalFunction_TopLevel([CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingTree("""
            partial F() => null;
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion),
            // (1,9): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
            // partial F() => null;
            Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "F").WithLocation(1, 9));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.IncompleteMember);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "F");
                }
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.ParenthesizedLambdaExpression);
                    {
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.NullLiteralExpression);
                        {
                            N(SyntaxKind.NullKeyword);
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
    public void ReturningPartialType_LocalFunction_TopLevel_CSharp13()
    {
        UsingTree("""
            partial F() => null;
            """,
            TestOptions.Regular13);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "partial");
                    }
                    N(SyntaxKind.IdentifierToken, "F");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.NullLiteralExpression);
                        {
                            N(SyntaxKind.NullKeyword);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void ReturningPartialType_Method([CSharp14_Preview] LanguageVersion langVersion)
    {
        UsingTree("""
            class C
            {
                partial M() => null;
                @partial M() => null;
            }
            """,
            TestOptions.Regular.WithLanguageVersion(langVersion));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ConstructorDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.NullLiteralExpression);
                        {
                            N(SyntaxKind.NullKeyword);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "@partial");
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
                        N(SyntaxKind.NullLiteralExpression);
                        {
                            N(SyntaxKind.NullKeyword);
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
    public void ReturningPartialType_Method_CSharp13()
    {
        UsingTree("""
            class C
            {
                partial M() => null;
                @partial M() => null;
            }
            """,
            TestOptions.Regular13);

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
                        N(SyntaxKind.IdentifierToken, "partial");
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
                        N(SyntaxKind.NullLiteralExpression);
                        {
                            N(SyntaxKind.NullKeyword);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "@partial");
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
                        N(SyntaxKind.NullLiteralExpression);
                        {
                            N(SyntaxKind.NullKeyword);
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
}
