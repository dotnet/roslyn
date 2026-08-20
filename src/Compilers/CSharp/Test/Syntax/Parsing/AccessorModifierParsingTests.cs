// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class AccessorModifierParsingTests(ITestOutputHelper output) : ParsingTests(output)
{
    [Fact]
    public void ContextualAndKeywordAccessorModifierOrderings()
    {
        const string source = """
            class C
            {
                int P { partial ref get; }
                int Q { async partial get; }
                int R { ref scoped get; }
            }
            """;

        UsingTree(
            source,
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "partial").WithLocation(3, 13),
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "async").WithLocation(4, 13),
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "partial").WithLocation(4, 19),
            Diagnostic(ErrorCode.ERR_RbraceExpected, "ref").WithLocation(5, 13),
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(6, 1));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
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
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
                        }
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Q");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "async");
                        }
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
                        }
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "get");
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
    public void RefAccessorModifierBodyForms()
    {
        const string source = "class C { int P { ref get { } ref get; ref get => 0; } }";

        UsingTree(source);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
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
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
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

    [Fact]
    public void ContextualKeywordWithoutAccessorNameBeforeBodyForms()
    {
        const string source = "class C { int P { partial { } partial; } }";

        UsingTree(
            source,
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "partial").WithLocation(1, 19),
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "partial").WithLocation(1, 31));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
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
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
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
    public void EscapedContextualAndArbitraryAccessorNames()
    {
        const string source = "class C { int P { @partial; @async; @scoped; unknown; } }";

        UsingTree(
            source,
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "@partial").WithLocation(1, 19),
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "@async").WithLocation(1, 29),
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "@scoped").WithLocation(1, 37),
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "unknown").WithLocation(1, 46));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
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
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "@partial");
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "@async");
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "@scoped");
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "unknown");
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
    public void ContextualKeywordBeforeNonAccessorTokens()
    {
        const string source = "class C { int P { partial => 0; partial unknown; } }";

        UsingTree(
            source,
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "partial").WithLocation(1, 19),
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "partial").WithLocation(1, 33),
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "unknown").WithLocation(1, 41));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
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
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
                        }
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "unknown");
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
    public void AccessorModifierBeforeAttributeRecovery()
    {
        const string source = "class C { int P { private [System.Obsolete] get; } }";

        UsingTree(
            source,
            Diagnostic(ErrorCode.ERR_RbraceExpected, "private").WithLocation(1, 19),
            Diagnostic(ErrorCode.ERR_TypeExpected, "[").WithLocation(1, 27),
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(1, 52));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
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
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.PrivateKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.ArrayRankSpecifier);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "System");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Obsolete");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "get");
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
    public void ValidPropertyAndIndexerAccessors()
    {
        const string source = """
            class C
            {
                public int P { get; private set; }
                public int Q { get; private init; }
                public int this[int i] { private get => 0; set { } }
            }
            """;

        UsingTree(source);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
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
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.PrivateKeyword);
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Q");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.InitAccessorDeclaration);
                        {
                            N(SyntaxKind.PrivateKeyword);
                            N(SyntaxKind.InitKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.IndexerDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
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
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.PrivateKeyword);
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.SetKeyword);
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

        CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
    }

    [Fact]
    public void RefModifiersRemainOnPropertyAndEventAccessors()
    {
        const string source = """
            class C
            {
                int P { ref get => 0; abstract ref set { } }
                event System.Action E { ref add { } abstract ref remove { } }
            }
            """;

        UsingTree(source);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
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
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.AbstractKeyword);
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EventDeclaration);
                {
                    N(SyntaxKind.EventKeyword);
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "System");
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Action");
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "E");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.AddAccessorDeclaration);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.AddKeyword);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.RemoveAccessorDeclaration);
                        {
                            N(SyntaxKind.AbstractKeyword);
                            N(SyntaxKind.RefKeyword);
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

        CreateCompilation(source).VerifyDiagnostics(
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("ref").WithLocation(3, 17),
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("abstract").WithLocation(3, 40),
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("ref").WithLocation(3, 40),
            Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "ref").WithLocation(4, 29),
            Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "abstract").WithLocation(4, 41));
    }

    [Fact]
    public void RefReturningMembersWithAccessorKeywordType()
    {
        const string source = """
            #pragma warning disable CS8981
            class get { }
            class C
            {
                get _value;
                ref get A => ref _value;
                ref get B { }
                ref get C;
                ref get D() { }
                ref get E() => ref _value;
            }
            """;

        UsingTree(source);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "get");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
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
                            N(SyntaxKind.IdentifierToken, "get");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "_value");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "get");
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "A");
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.RefExpression);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_value");
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "get");
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "B");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "get");
                            }
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "get");
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "D");
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
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "get");
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "E");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.RefExpression);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_value");
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

    [Fact]
    public void IncompleteRefAccessorBeforeCloseBrace()
    {
        const string source = """
            class C
            {
                int P { ref get }
            }
            """;

        UsingTree(
            source,
            Diagnostic(ErrorCode.ERR_SemiOrLBraceOrArrowExpected, "}").WithLocation(3, 21));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
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
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.GetKeyword);
                            M(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(source).VerifyDiagnostics(
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("ref").WithLocation(3, 17),
            Diagnostic(ErrorCode.ERR_SemiOrLBraceOrArrowExpected, "}").WithLocation(3, 21));
    }

    [Fact]
    public void ScopedRefModifiersRemainOnAccessor()
    {
        const string source = """
            class C
            {
                public int P { scoped ref get; set; }
            }
            """;

        UsingTree(
            source,
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "scoped").WithLocation(3, 20));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "P");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.RefKeyword);
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

        CreateCompilation(source).VerifyDiagnostics(
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "scoped").WithLocation(3, 20),
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("ref").WithLocation(3, 31));
    }

    [Fact]
    public void SafeModifierOrderingAndAccessorName()
    {
        const string source = """
            class C
            {
                public int P { private safe get => 0; set { } }
                public int Q { get => 0; safe private set { } }
                int R { safe; get => 0; set { } }
            }
            """;

        UsingTree(
            source,
            TestOptions.RegularPreview,
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "safe").WithLocation(5, 13));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
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
                            N(SyntaxKind.PrivateKeyword);
                            N(SyntaxKind.SafeKeyword);
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Q");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.SafeKeyword);
                            N(SyntaxKind.PrivateKeyword);
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "safe");
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.SetKeyword);
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

        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "safe").WithLocation(5, 13));
    }

    [Fact]
    public void ContextualKeywordsAreRecoveredBeforeAccessors()
    {
        const string source = """
            class C
            {
                int P { scoped get; set; }
                int Q { partial get; set; }
                int R { async get; set; }
                public int S { private async get; set; }
                public int T { private partial get; set; }
            }
            """;

        UsingTree(
            source,
            TestOptions.RegularPreview,
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "scoped").WithLocation(3, 13),
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "partial").WithLocation(4, 13),
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "async").WithLocation(5, 13),
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "async").WithLocation(6, 28),
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "partial").WithLocation(7, 28));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
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
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
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
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Q");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
                        }
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
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "async");
                        }
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
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "S");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.PrivateKeyword);
                            N(SyntaxKind.IdentifierToken, "async");
                        }
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
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "T");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.PrivateKeyword);
                            N(SyntaxKind.IdentifierToken, "partial");
                        }
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
    public void InvalidModifiersRemainOnAccessors()
    {
        const string source = """
            class C
            {
                int P { required get; file set; }
                int Q { closed get; static set; }
                public int R { private private get; set; }
            }
            """;

        UsingTree(source, TestOptions.RegularPreview);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
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
                            N(SyntaxKind.RequiredKeyword);
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.FileKeyword);
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Q");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.ClosedKeyword);
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.StaticKeyword);
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.PrivateKeyword);
                            N(SyntaxKind.PrivateKeyword);
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

        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("required").WithLocation(3, 22),
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("file").WithLocation(3, 32),
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("closed").WithLocation(4, 20),
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("static").WithLocation(4, 32),
            Diagnostic(ErrorCode.ERR_DuplicateModifier, "private").WithArguments("private").WithLocation(5, 28));
    }

    [Fact]
    public void AttributeBeforeReadonlyAccessorModifier()
    {
        const string source = """
            struct S
            {
                public int P { [System.Obsolete] readonly get => 0; set { } }
            }
            """;

        UsingTree(source);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
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
                            N(SyntaxKind.AttributeList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Attribute);
                                {
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "System");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Obsolete");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.SetKeyword);
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

        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void AccessorRecoveryDoesNotConsumeFollowingMember()
    {
        const string source = """
            class C
            {
                int P
                {
                    get { return 0; }
                private int F;
            }
            """;

        UsingTree(
            source,
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 26));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
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
                                N(SyntaxKind.ReturnStatement);
                                {
                                    N(SyntaxKind.ReturnKeyword);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "0");
                                    }
                                    N(SyntaxKind.SemicolonToken);
                                }
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.PrivateKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
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

        CreateCompilation(source).VerifyDiagnostics(
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 26),
            Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("C.F").WithLocation(6, 17));
    }

    [Fact]
    public void AccessorModifierBeforeFeature()
    {
        const string source = """
            class C
            {
                public int P { get; private set; }
            }
            """;
        var options = TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp1);

        UsingTree(source, options);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
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
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.PrivateKeyword);
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

        CreateCompilation(source, parseOptions: options).VerifyDiagnostics(
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion1, "P").WithArguments("automatically implemented properties", "3").WithLocation(3, 16),
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion1, "private").WithArguments("access modifiers on properties", "2").WithLocation(3, 25));
    }

    [Fact]
    public void SafeAccessorModifierBeforeFeature()
    {
        const string source = """
            class C
            {
                public int P { private safe get => 0; set { } }
            }
            """;

        UsingTree(source, TestOptions.Regular14);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
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
                            N(SyntaxKind.PrivateKeyword);
                            N(SyntaxKind.SafeKeyword);
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.SetKeyword);
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

        CreateCompilation(source, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "safe").WithArguments("updated memory safety rules").WithLocation(3, 28));
    }

    [Fact]
    public void OlderLanguageVersionDoesNotRecognizeNewAccessorModifiers()
    {
        const string source = """
            class C
            {
                int P { required get; file set; closed init; }
            }
            """;

        UsingTree(
            source,
            TestOptions.Regular10,
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "required").WithLocation(3, 13),
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "file").WithLocation(3, 27),
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "closed").WithLocation(3, 37));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
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
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "required");
                        }
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "file");
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "closed");
                        }
                        N(SyntaxKind.InitAccessorDeclaration);
                        {
                            N(SyntaxKind.InitKeyword);
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
    public void PartialAccessorModifierBeforeCloseBrace()
    {
        const string source = """
            class C
            {
                int P { partial }
            }
            """;

        UsingTree(
            source,
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "partial").WithLocation(3, 13));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
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
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
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
    public void PartialAccessorModifierBeforeEndOfFile()
    {
        const string source = "class C { int P { partial";

        UsingTree(
            source,
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "partial").WithLocation(1, 19),
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 26),
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 26));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
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
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
                        }
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                M(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AttributeAndPartialAccessorModifierBeforeCloseBrace()
    {
        const string source = """
            class C
            {
                int P { [System.Obsolete] partial }
            }
            """;

        UsingTree(
            source,
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "partial").WithLocation(3, 31));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
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
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.AttributeList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Attribute);
                                {
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "System");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Obsolete");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.IdentifierToken, "partial");
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
    public void PartialFieldAfterIncompleteAccessorList()
    {
        const string source = """
            class C
            {
                int P
                {
                    get;
                partial int F;
            }
            """;

        UsingTree(
            source,
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "int").WithLocation(6, 13),
            Diagnostic(ErrorCode.ERR_RbraceExpected, "int").WithLocation(6, 13));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
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
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.PartialKeyword);
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
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
    public void AttributeBeforeScopedAccessorModifier()
    {
        const string source = """
            class C
            {
                int P { [System.Obsolete] scoped get; }
            }
            """;

        UsingTree(
            source,
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "scoped").WithLocation(3, 31));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
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
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.AttributeList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Attribute);
                                {
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "System");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Obsolete");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
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
    public void ContextualModifiersOnOtherAccessorKinds()
    {
        const string source = """
            class C
            {
                int P { partial init; }
                int this[int i] { scoped get; }
                event System.Action E { partial add { } scoped remove { } }
            }
            """;

        UsingTree(
            source,
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "partial").WithLocation(3, 13),
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "scoped").WithLocation(4, 23),
            Diagnostic(ErrorCode.ERR_AddOrRemoveExpected, "partial").WithLocation(5, 29),
            Diagnostic(ErrorCode.ERR_AddOrRemoveExpected, "scoped").WithLocation(5, 45));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
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
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
                        }
                        N(SyntaxKind.InitAccessorDeclaration);
                        {
                            N(SyntaxKind.InitKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.IndexerDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
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
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EventDeclaration);
                {
                    N(SyntaxKind.EventKeyword);
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "System");
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Action");
                        }
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
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
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
}
