// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class TypeArgumentListParsingTests : ParsingTests
    {
        public TypeArgumentListParsingTests(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: options);
        }

        [Fact, WorkItem(19456, "https://github.com/dotnet/roslyn/issues/19456")]
        public void TestPredefinedType()
        {
            UsingTree(@"
class C
{
    void M()
    {
        var added = ImmutableDictionary<string, IImmutableDictionary<X, Y>>

        ProjectChange = projectChange;
    }
}
");
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
                                        N(SyntaxKind.IdentifierToken, "added");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.GenericName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "ImmutableDictionary");
                                                N(SyntaxKind.TypeArgumentList);
                                                {
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.PredefinedType);
                                                    {
                                                        N(SyntaxKind.StringKeyword);
                                                    }
                                                    N(SyntaxKind.CommaToken);
                                                    N(SyntaxKind.GenericName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "IImmutableDictionary");
                                                        N(SyntaxKind.TypeArgumentList);
                                                        {
                                                            N(SyntaxKind.LessThanToken);
                                                            N(SyntaxKind.IdentifierName);
                                                            {
                                                                N(SyntaxKind.IdentifierToken, "X");
                                                            }
                                                            N(SyntaxKind.CommaToken);
                                                            N(SyntaxKind.IdentifierName);
                                                            {
                                                                N(SyntaxKind.IdentifierToken, "Y");
                                                            }
                                                            N(SyntaxKind.GreaterThanToken);
                                                        }
                                                    }
                                                    N(SyntaxKind.GreaterThanToken);
                                                }
                                            }
                                        }
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "ProjectChange");
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "projectChange");
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

        [Fact, WorkItem(19456, "https://github.com/dotnet/roslyn/issues/19456")]
        public void TestArrayType()
        {
            UsingTree(@"
class C
{
    void M()
    {
        var added = ImmutableDictionary<X[], IImmutableDictionary<X, Y>>

        ProjectChange = projectChange;
    }
}
");
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
                                        N(SyntaxKind.IdentifierToken, "added");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.GenericName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "ImmutableDictionary");
                                                N(SyntaxKind.TypeArgumentList);
                                                {
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.ArrayType);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "X");
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
                                                    N(SyntaxKind.CommaToken);
                                                    N(SyntaxKind.GenericName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "IImmutableDictionary");
                                                        N(SyntaxKind.TypeArgumentList);
                                                        {
                                                            N(SyntaxKind.LessThanToken);
                                                            N(SyntaxKind.IdentifierName);
                                                            {
                                                                N(SyntaxKind.IdentifierToken, "X");
                                                            }
                                                            N(SyntaxKind.CommaToken);
                                                            N(SyntaxKind.IdentifierName);
                                                            {
                                                                N(SyntaxKind.IdentifierToken, "Y");
                                                            }
                                                            N(SyntaxKind.GreaterThanToken);
                                                        }
                                                    }
                                                    N(SyntaxKind.GreaterThanToken);
                                                }
                                            }
                                        }
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "ProjectChange");
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "projectChange");
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

        [Fact, WorkItem(19456, "https://github.com/dotnet/roslyn/issues/19456")]
        public void TestPredefinedPointerType()
        {
            UsingTree(@"
class C
{
    void M()
    {
        var added = ImmutableDictionary<int*, IImmutableDictionary<X, Y>>

        ProjectChange = projectChange;
    }
}
");
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
                                        N(SyntaxKind.IdentifierToken, "added");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.GenericName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "ImmutableDictionary");
                                                N(SyntaxKind.TypeArgumentList);
                                                {
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.PointerType);
                                                    {
                                                        N(SyntaxKind.PredefinedType);
                                                        {
                                                            N(SyntaxKind.IntKeyword);
                                                        }
                                                        N(SyntaxKind.AsteriskToken);
                                                    }
                                                    N(SyntaxKind.CommaToken);
                                                    N(SyntaxKind.GenericName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "IImmutableDictionary");
                                                        N(SyntaxKind.TypeArgumentList);
                                                        {
                                                            N(SyntaxKind.LessThanToken);
                                                            N(SyntaxKind.IdentifierName);
                                                            {
                                                                N(SyntaxKind.IdentifierToken, "X");
                                                            }
                                                            N(SyntaxKind.CommaToken);
                                                            N(SyntaxKind.IdentifierName);
                                                            {
                                                                N(SyntaxKind.IdentifierToken, "Y");
                                                            }
                                                            N(SyntaxKind.GreaterThanToken);
                                                        }
                                                    }
                                                    N(SyntaxKind.GreaterThanToken);
                                                }
                                            }
                                        }
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "ProjectChange");
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "projectChange");
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

        [Fact, WorkItem(19456, "https://github.com/dotnet/roslyn/issues/19456")]
        public void TestNonPredefinedPointerType()
        {
            UsingTree(@"
class C
{
    void M()
    {
        var added = ImmutableDictionary<X*, IImmutableDictionary<X, Y>>

        ProjectChange = projectChange;
    }
}
");

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
                                        N(SyntaxKind.IdentifierToken, "added");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.LessThanExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "ImmutableDictionary");
                                                }
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.MultiplyExpression);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "X");
                                                    }
                                                    N(SyntaxKind.AsteriskToken);
                                                    M(SyntaxKind.IdentifierName);
                                                    {
                                                        M(SyntaxKind.IdentifierToken);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "IImmutableDictionary");
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.LessThanExpression);
                                {
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.RightShiftExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Y");
                                        }
                                        N(SyntaxKind.GreaterThanGreaterThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "ProjectChange");
                                        }
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "projectChange");
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

        [Fact, WorkItem(19456, "https://github.com/dotnet/roslyn/issues/19456")]
        public void TestTwoItemTupleType()
        {
            UsingTree(@"
class C
{
    void M()
    {
        var added = ImmutableDictionary<(int, string), IImmutableDictionary<X, Y>>

        ProjectChange = projectChange;
    }
}
");
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
                                        N(SyntaxKind.IdentifierToken, "added");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.LessThanExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "ImmutableDictionary");
                                                }
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.TupleExpression);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.Argument);
                                                    {
                                                        N(SyntaxKind.PredefinedType);
                                                        {
                                                            N(SyntaxKind.IntKeyword);
                                                        }
                                                    }
                                                    N(SyntaxKind.CommaToken);
                                                    N(SyntaxKind.Argument);
                                                    {
                                                        N(SyntaxKind.PredefinedType);
                                                        {
                                                            N(SyntaxKind.StringKeyword);
                                                        }
                                                    }
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "IImmutableDictionary");
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.LessThanExpression);
                                {
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.RightShiftExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Y");
                                        }
                                        N(SyntaxKind.GreaterThanGreaterThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "ProjectChange");
                                        }
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "projectChange");
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

        [Fact, WorkItem(19456, "https://github.com/dotnet/roslyn/issues/19456")]
        public void TestComparisonToTuple()
        {
            UsingTree(@"
public class C
{
    public static void Main()
    {
        XX X = new XX();
        int a = 1, b = 2;
        bool z = X < (a, b), w = false;
    }
}

struct XX
{
    public static bool operator <(XX x, (int a, int b) arg) => true;
    public static bool operator >(XX x, (int a, int b) arg) => false;
}");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Main");
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
                                        N(SyntaxKind.IdentifierToken, "XX");
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ObjectCreationExpression);
                                            {
                                                N(SyntaxKind.NewKeyword);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "XX");
                                                }
                                                N(SyntaxKind.ArgumentList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "1");
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "b");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "2");
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.BoolKeyword);
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "z");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.LessThanExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "X");
                                                }
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.TupleExpression);
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
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "w");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.FalseLiteralExpression);
                                            {
                                                N(SyntaxKind.FalseKeyword);
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "XX");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.BoolKeyword);
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "XX");
                                }
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.TupleType);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.IntKeyword);
                                        }
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.IntKeyword);
                                        }
                                        N(SyntaxKind.IdentifierToken, "b");
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.IdentifierToken, "arg");
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.TrueLiteralExpression);
                            {
                                N(SyntaxKind.TrueKeyword);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.BoolKeyword);
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.GreaterThanToken);
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "XX");
                                }
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.TupleType);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.IntKeyword);
                                        }
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.IntKeyword);
                                        }
                                        N(SyntaxKind.IdentifierToken, "b");
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.IdentifierToken, "arg");
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.FalseLiteralExpression);
                            {
                                N(SyntaxKind.FalseKeyword);
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

        [Fact, WorkItem(19456, "https://github.com/dotnet/roslyn/issues/19456")]
        public void TestOneItemTupleType()
        {
            UsingTree(@"
class C
{
    void M()
    {
        var added = ImmutableDictionary<(A), IImmutableDictionary<X, Y>>

        ProjectChange = projectChange;
    }
}
");
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
                                        N(SyntaxKind.IdentifierToken, "added");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.LessThanExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "ImmutableDictionary");
                                                }
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.ParenthesizedExpression);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "A");
                                                    }
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "IImmutableDictionary");
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.LessThanExpression);
                                {
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.RightShiftExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Y");
                                        }
                                        N(SyntaxKind.GreaterThanGreaterThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "ProjectChange");
                                        }
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "projectChange");
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

        [Fact, WorkItem(19456, "https://github.com/dotnet/roslyn/issues/19456")]
        public void TestQualifiedName()
        {
            UsingTree(@"
class C
{
    void M()
    {
        var added = ImmutableDictionary<A.B, IImmutableDictionary<X, Y>>

        ProjectChange = projectChange;
    }
}
");
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
                                        N(SyntaxKind.IdentifierToken, "added");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.LessThanExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "ImmutableDictionary");
                                                }
                                                N(SyntaxKind.LessThanToken);
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
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "IImmutableDictionary");
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.LessThanExpression);
                                {
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.RightShiftExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Y");
                                        }
                                        N(SyntaxKind.GreaterThanGreaterThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "ProjectChange");
                                        }
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "projectChange");
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

        [Fact, WorkItem(19456, "https://github.com/dotnet/roslyn/issues/19456")]
        public void TestAliasName()
        {
            UsingTree(@"
class C
{
    void M()
    {
        var added = ImmutableDictionary<A::B, IImmutableDictionary<X, Y>>

        ProjectChange = projectChange;
    }
}
");
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
                                        N(SyntaxKind.IdentifierToken, "added");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.LessThanExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "ImmutableDictionary");
                                                }
                                                N(SyntaxKind.LessThanToken);
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
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "IImmutableDictionary");
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.LessThanExpression);
                                {
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.RightShiftExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Y");
                                        }
                                        N(SyntaxKind.GreaterThanGreaterThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "ProjectChange");
                                        }
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "projectChange");
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

        [Fact, WorkItem(19456, "https://github.com/dotnet/roslyn/issues/19456")]
        public void TestNullableTypeWithComma()
        {
            UsingTree(@"
class C
{
    void M()
    {
        var added = ImmutableDictionary<A?, IImmutableDictionary<X, Y>>

        ProjectChange = projectChange;
    }
}
");
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
                                        N(SyntaxKind.IdentifierToken, "added");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.GenericName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "ImmutableDictionary");
                                                N(SyntaxKind.TypeArgumentList);
                                                {
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.NullableType);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "A");
                                                        }
                                                        N(SyntaxKind.QuestionToken);
                                                    }
                                                    N(SyntaxKind.CommaToken);
                                                    N(SyntaxKind.GenericName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "IImmutableDictionary");
                                                        N(SyntaxKind.TypeArgumentList);
                                                        {
                                                            N(SyntaxKind.LessThanToken);
                                                            N(SyntaxKind.IdentifierName);
                                                            {
                                                                N(SyntaxKind.IdentifierToken, "X");
                                                            }
                                                            N(SyntaxKind.CommaToken);
                                                            N(SyntaxKind.IdentifierName);
                                                            {
                                                                N(SyntaxKind.IdentifierToken, "Y");
                                                            }
                                                            N(SyntaxKind.GreaterThanToken);
                                                        }
                                                    }
                                                    N(SyntaxKind.GreaterThanToken);
                                                }
                                            }
                                        }
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "ProjectChange");
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "projectChange");
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

        [Fact, WorkItem(19456, "https://github.com/dotnet/roslyn/issues/19456")]
        public void TestNullableTypeWithGreaterThan()
        {
            UsingTree(@"
class C
{
    void M()
    {
        var added = ImmutableDictionary<A?>

        ProjectChange = projectChange;
    }
}
");
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
                                        N(SyntaxKind.IdentifierToken, "added");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.GenericName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "ImmutableDictionary");
                                                N(SyntaxKind.TypeArgumentList);
                                                {
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.NullableType);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "A");
                                                        }
                                                        N(SyntaxKind.QuestionToken);
                                                    }
                                                    N(SyntaxKind.GreaterThanToken);
                                                }
                                            }
                                        }
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "ProjectChange");
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "projectChange");
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

        [Fact, WorkItem(19456, "https://github.com/dotnet/roslyn/issues/19456")]
        public void TestNotNullableType()
        {
            UsingTree(@"
class C
{
    void M()
    {
        var added = ImmutableDictionary<A?

        ProjectChange = projectChange;
    }
}
");
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
                                        N(SyntaxKind.IdentifierToken, "added");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ConditionalExpression);
                                            {
                                                N(SyntaxKind.LessThanExpression);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "ImmutableDictionary");
                                                    }
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "A");
                                                    }
                                                }
                                                N(SyntaxKind.QuestionToken);
                                                N(SyntaxKind.SimpleAssignmentExpression);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "ProjectChange");
                                                    }
                                                    N(SyntaxKind.EqualsToken);
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "projectChange");
                                                    }
                                                }
                                                M(SyntaxKind.ColonToken);
                                                M(SyntaxKind.IdentifierName);
                                                {
                                                    M(SyntaxKind.IdentifierToken);
                                                }
                                            }
                                        }
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

        [Fact, WorkItem(19456, "https://github.com/dotnet/roslyn/issues/19456")]
        public void TestGenericArgWithComma()
        {
            UsingTree(@"
class C
{
    void M()
    {
        var added = ImmutableDictionary<T<S>, IImmutableDictionary<X, Y>>

        ProjectChange = projectChange;
    }
}
");
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
                                        N(SyntaxKind.IdentifierToken, "added");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.GenericName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "ImmutableDictionary");
                                                N(SyntaxKind.TypeArgumentList);
                                                {
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.GenericName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "T");
                                                        N(SyntaxKind.TypeArgumentList);
                                                        {
                                                            N(SyntaxKind.LessThanToken);
                                                            N(SyntaxKind.IdentifierName);
                                                            {
                                                                N(SyntaxKind.IdentifierToken, "S");
                                                            }
                                                            N(SyntaxKind.GreaterThanToken);
                                                        }
                                                    }
                                                    N(SyntaxKind.CommaToken);
                                                    N(SyntaxKind.GenericName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "IImmutableDictionary");
                                                        N(SyntaxKind.TypeArgumentList);
                                                        {
                                                            N(SyntaxKind.LessThanToken);
                                                            N(SyntaxKind.IdentifierName);
                                                            {
                                                                N(SyntaxKind.IdentifierToken, "X");
                                                            }
                                                            N(SyntaxKind.CommaToken);
                                                            N(SyntaxKind.IdentifierName);
                                                            {
                                                                N(SyntaxKind.IdentifierToken, "Y");
                                                            }
                                                            N(SyntaxKind.GreaterThanToken);
                                                        }
                                                    }
                                                    N(SyntaxKind.GreaterThanToken);
                                                }
                                            }
                                        }
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "ProjectChange");
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "projectChange");
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

        [Fact, WorkItem(19456, "https://github.com/dotnet/roslyn/issues/19456")]
        public void TestGenericArgWithGreaterThan()
        {
            UsingTree(@"
class C
{
    void M()
    {
        var added = ImmutableDictionary<T<S>>

        ProjectChange = projectChange;
    }
}
");
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
                                        N(SyntaxKind.IdentifierToken, "added");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.SimpleAssignmentExpression);
                                            {
                                                N(SyntaxKind.LessThanExpression);
                                                {
                                                    N(SyntaxKind.LessThanExpression);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "ImmutableDictionary");
                                                        }
                                                        N(SyntaxKind.LessThanToken);
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "T");
                                                        }
                                                    }
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.RightShiftExpression);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "S");
                                                        }
                                                        N(SyntaxKind.GreaterThanGreaterThanToken);
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "ProjectChange");
                                                        }
                                                    }
                                                }
                                                N(SyntaxKind.EqualsToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "projectChange");
                                                }
                                            }
                                        }
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

        [WorkItem(24642, "https://github.com/dotnet/roslyn/issues/24642")]
        [Fact]
        public void TestMissingCommaTokenInReturnType()
        {
            UsingTree(@"class Class
{
    Dictionary<int string> M(string a)
    {
        var x = 42;
    }
}");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Class");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "Dictionary");
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                M(SyntaxKind.CommaToken);
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "a");
                            }
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
                                        N(SyntaxKind.IdentifierToken, "x");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "42");
                                            }
                                        }
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

        [WorkItem(24642, "https://github.com/dotnet/roslyn/issues/24642")]
        [Fact]
        public void TestMissingGreaterThanTokenInReturnTypeOfMethod()
        {
            UsingTree(@"class Class
{
    List<int M()
    {
        var x = 42;
    }
}");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Class");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "List");
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                M(SyntaxKind.GreaterThanToken);
                            }
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
                                        N(SyntaxKind.IdentifierToken, "x");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "42");
                                            }
                                        }
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

        [WorkItem(24642, "https://github.com/dotnet/roslyn/issues/24642")]
        [Fact]
        public void TestMissingGreaterThanTokenInReturnTypeOfProperty()
        {
            UsingTree(@"class Class
{
    List<int M => 42;
    List<int M { get; set; }
}");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Class");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "List");
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                M(SyntaxKind.GreaterThanToken);
                            }
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "42");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "List");
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                M(SyntaxKind.GreaterThanToken);
                            }
                        }
                        N(SyntaxKind.IdentifierToken, "M");
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

        [WorkItem(24642, "https://github.com/dotnet/roslyn/issues/24642")]
        [Fact]
        public void TestMissingGreaterThanTokenInTypeOfField()
        {
            UsingTree(@"class Class
{
    List<int M;
    List<int M = 42;
}");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Class");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    M(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    M(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "42");
                                    }
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

        [WorkItem(24642, "https://github.com/dotnet/roslyn/issues/24642")]
        [Fact]
        public void TestMissingGreaterThanTokenAfterMissingCommaTokenInReturnType()
        {
            UsingTree(@"class Class
{
    List<int string M()
    {
        var x = 42;
    }
}");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Class");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "List");
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                M(SyntaxKind.CommaToken);
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                                M(SyntaxKind.GreaterThanToken);
                            }
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
                                        N(SyntaxKind.IdentifierToken, "x");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "42");
                                            }
                                        }
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

        [WorkItem(24642, "https://github.com/dotnet/roslyn/issues/24642")]
        [Fact]
        public void TestMissingGreaterThanTokenInBothParametersInParameterList()
        {
            UsingTree(@"class Class
{
    void M(List<int parameter1, List<int parameter2)
    {
        var x = 42;
    }
}");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Class");
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
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.IntKeyword);
                                        }
                                        M(SyntaxKind.CommaToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "parameter1");
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.GenericName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "List");
                                            N(SyntaxKind.TypeArgumentList);
                                            {
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                                M(SyntaxKind.CommaToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "parameter2");
                                                }
                                                M(SyntaxKind.GreaterThanToken);
                                            }
                                        }
                                        M(SyntaxKind.GreaterThanToken);
                                    }
                                }
                                M(SyntaxKind.IdentifierToken);
                            }
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
                                        N(SyntaxKind.IdentifierToken, "x");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "42");
                                            }
                                        }
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

        [WorkItem(24642, "https://github.com/dotnet/roslyn/issues/24642")]
        [Fact]
        public void TestMissingGreaterThanTokenInNew()
        {
            UsingTree(@"class Class
{
    void M()
    {
        var a = new List<int();
        var b  = new List<int string();
        var c = 42;
    }
}");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Class");
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
                                        N(SyntaxKind.IdentifierToken, "a");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ObjectCreationExpression);
                                            {
                                                N(SyntaxKind.NewKeyword);
                                                N(SyntaxKind.GenericName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "List");
                                                    N(SyntaxKind.TypeArgumentList);
                                                    {
                                                        N(SyntaxKind.LessThanToken);
                                                        N(SyntaxKind.PredefinedType);
                                                        {
                                                            N(SyntaxKind.IntKeyword);
                                                        }
                                                        M(SyntaxKind.GreaterThanToken);
                                                    }
                                                }
                                                N(SyntaxKind.ArgumentList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
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
                                        N(SyntaxKind.IdentifierToken, "b");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ObjectCreationExpression);
                                            {
                                                N(SyntaxKind.NewKeyword);
                                                N(SyntaxKind.GenericName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "List");
                                                    N(SyntaxKind.TypeArgumentList);
                                                    {
                                                        N(SyntaxKind.LessThanToken);
                                                        N(SyntaxKind.PredefinedType);
                                                        {
                                                            N(SyntaxKind.IntKeyword);
                                                        }
                                                        M(SyntaxKind.CommaToken);
                                                        N(SyntaxKind.PredefinedType);
                                                        {
                                                            N(SyntaxKind.StringKeyword);
                                                        }
                                                        M(SyntaxKind.GreaterThanToken);
                                                    }
                                                }
                                                N(SyntaxKind.ArgumentList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
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
                                        N(SyntaxKind.IdentifierToken, "c");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "42");
                                            }
                                        }
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

        [WorkItem(24642, "https://github.com/dotnet/roslyn/issues/24642")]
        [Fact]
        public void TestMissingGreaterThanTokenInBaseList()
        {
            UsingTree(@"class Class : Parent<int
{
    public void M(){}
}");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Class");
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "Parent");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    M(SyntaxKind.GreaterThanToken);
                                }
                            }
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
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
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

        }

        [WorkItem(24642, "https://github.com/dotnet/roslyn/issues/24642")]
        [Fact]
        public void TestMissingGreaterThanTokenInConstraint()
        {
            UsingTree(@"class Class
{
    void M<T>() where T : List<int
    {
        var x = 42;
    }");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Class");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.TypeParameterList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.TypeParameter);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
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
                                    N(SyntaxKind.IdentifierToken, "List");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.IntKeyword);
                                        }
                                        M(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
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
                                        N(SyntaxKind.IdentifierToken, "x");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "42");
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }
    }
}
