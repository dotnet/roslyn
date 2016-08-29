﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SeparatedSyntaxListParsingTests : ParsingTests
    {
        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: options);
        }

        [Fact]
        public void TypeArguments()
        {
            UsingTree(@"
class C
{
    A<> a1;
    A<T> a2;
    A<,> a3;
    A<T U> a4;
    A<,,> a5;
    A<T,> a6;
    A<,T> a7;
    A<T U,,> a8;
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);

                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        N(SyntaxKind.GenericName);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeArgumentList);
                        N(SyntaxKind.LessThanToken);
                        {
                            N(SyntaxKind.OmittedTypeArgument); N(SyntaxKind.OmittedTypeArgumentToken);
                        }
                        N(SyntaxKind.GreaterThanToken);
                        N(SyntaxKind.VariableDeclarator);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.SemicolonToken);
                    }

                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        N(SyntaxKind.GenericName);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeArgumentList);
                        N(SyntaxKind.LessThanToken);
                        {
                            N(SyntaxKind.IdentifierName); N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.GreaterThanToken);
                        N(SyntaxKind.VariableDeclarator);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.SemicolonToken);
                    }

                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        N(SyntaxKind.GenericName);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeArgumentList);
                        N(SyntaxKind.LessThanToken);
                        {
                            N(SyntaxKind.OmittedTypeArgument); N(SyntaxKind.OmittedTypeArgumentToken);
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.OmittedTypeArgument); N(SyntaxKind.OmittedTypeArgumentToken);
                        }
                        N(SyntaxKind.GreaterThanToken);
                        N(SyntaxKind.VariableDeclarator);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.SemicolonToken);
                    }

                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        N(SyntaxKind.GenericName);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeArgumentList);
                        N(SyntaxKind.LessThanToken);
                        {
                            N(SyntaxKind.IdentifierName); N(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.IdentifierName); N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.GreaterThanToken);
                        N(SyntaxKind.VariableDeclarator);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.SemicolonToken);
                    }

                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        N(SyntaxKind.GenericName);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeArgumentList);
                        N(SyntaxKind.LessThanToken);
                        {
                            N(SyntaxKind.OmittedTypeArgument); N(SyntaxKind.OmittedTypeArgumentToken);
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.OmittedTypeArgument); N(SyntaxKind.OmittedTypeArgumentToken);
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.OmittedTypeArgument); N(SyntaxKind.OmittedTypeArgumentToken);
                        }
                        N(SyntaxKind.GreaterThanToken);
                        N(SyntaxKind.VariableDeclarator);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.SemicolonToken);
                    }

                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        N(SyntaxKind.GenericName);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeArgumentList);
                        N(SyntaxKind.LessThanToken);
                        {
                            N(SyntaxKind.IdentifierName); N(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.IdentifierName); N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.GreaterThanToken);
                        N(SyntaxKind.VariableDeclarator);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.SemicolonToken);
                    }

                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        N(SyntaxKind.GenericName);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeArgumentList);
                        N(SyntaxKind.LessThanToken);
                        {
                            N(SyntaxKind.IdentifierName); N(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.IdentifierName); N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.GreaterThanToken);
                        N(SyntaxKind.VariableDeclarator);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.SemicolonToken);
                    }

                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        N(SyntaxKind.GenericName);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeArgumentList);
                        N(SyntaxKind.LessThanToken);
                        {
                            N(SyntaxKind.IdentifierName); N(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.IdentifierName); N(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.IdentifierName); N(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.IdentifierName); N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.GreaterThanToken);
                        N(SyntaxKind.VariableDeclarator);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.SemicolonToken);
                    }

                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void TypeArguments2()
        {
            var tree = UsingTree(@"
class C
{
    new C<>();
    new C<, >();
    C<C<>> a1;
    C<A<>> a1;
    object a1 = typeof(C<C<, >, int>);
    object a2 = Swap<>(1, 1);
}

class M<,> { }
", options: TestOptions.Regular);

            CheckTypeArguments2();
        }

        void CheckTypeArguments2()
        {
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.NewKeyword);
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.OmittedTypeArgument);
                                {
                                    N(SyntaxKind.OmittedTypeArgumentToken);
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                    }
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.TupleType);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.NewKeyword);
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.OmittedTypeArgument);
                                {
                                    N(SyntaxKind.OmittedTypeArgumentToken);
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.OmittedTypeArgument);
                                {
                                    N(SyntaxKind.OmittedTypeArgumentToken);
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                    }
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.TupleType);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken);
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken);
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.OmittedTypeArgument);
                                            {
                                                N(SyntaxKind.OmittedTypeArgumentToken);
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken);
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
                                N(SyntaxKind.IdentifierToken);
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken);
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.OmittedTypeArgument);
                                            {
                                                N(SyntaxKind.OmittedTypeArgumentToken);
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.ObjectKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken);
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.TypeOfExpression);
                                    {
                                        N(SyntaxKind.TypeOfKeyword);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.GenericName);
                                            {
                                                N(SyntaxKind.IdentifierToken);
                                                N(SyntaxKind.TypeArgumentList);
                                                {
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.GenericName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken);
                                                        N(SyntaxKind.TypeArgumentList);
                                                        {
                                                            N(SyntaxKind.LessThanToken);
                                                            N(SyntaxKind.OmittedTypeArgument);
                                                            {
                                                                N(SyntaxKind.OmittedTypeArgumentToken);
                                                            }
                                                            N(SyntaxKind.CommaToken);
                                                            N(SyntaxKind.OmittedTypeArgument);
                                                            {
                                                                N(SyntaxKind.OmittedTypeArgumentToken);
                                                            }
                                                            N(SyntaxKind.GreaterThanToken);
                                                        }
                                                    }
                                                    N(SyntaxKind.CommaToken);
                                                    N(SyntaxKind.PredefinedType);
                                                    {
                                                        N(SyntaxKind.IntKeyword);
                                                    }
                                                    N(SyntaxKind.GreaterThanToken);
                                                }
                                            }
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.ObjectKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken);
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.InvocationExpression);
                                    {
                                        N(SyntaxKind.GenericName);
                                        {
                                            N(SyntaxKind.IdentifierToken);
                                            N(SyntaxKind.TypeArgumentList);
                                            {
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.OmittedTypeArgument);
                                                {
                                                    N(SyntaxKind.OmittedTypeArgumentToken);
                                                }
                                                N(SyntaxKind.GreaterThanToken);
                                            }
                                        }
                                        N(SyntaxKind.ArgumentList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.Argument);
                                            {
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken);
                                                }
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.Argument);
                                            {
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken);
                                                }
                                            }
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void TypeArguments2WithCSharp6()
        {
            var tree = UsingTree(@"
class C
{
    new C<>();
    new C<, >();
    C<C<>> a1;
    C<A<>> a1;
    object a1 = typeof(C<C<, >, int>);
    object a2 = Swap<>(1, 1);
}

class M<,> { }
", TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6));

            CheckTypeArguments2();
        }

        [Fact]
        public void ArrayRankSpecifiers()
        {
            UsingTree(@"
class C
{
    object a1 = new int[];
    object a1 = new int[1];
    object a1 = new int[,];
    object a1 = new int[1 2];
    object a1 = new int[,,];
    object a1 = new int[1,];
    object a1 = new int[,1];
    object a1 = new int[1 1 ,,];
}
");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);

                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.ObjectKeyword);
                        N(SyntaxKind.VariableDeclarator);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.EqualsValueClause);
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.ArrayCreationExpression);
                        N(SyntaxKind.NewKeyword);
                        N(SyntaxKind.ArrayType);
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.IntKeyword);
                        N(SyntaxKind.ArrayRankSpecifier);
                        N(SyntaxKind.OpenBracketToken);
                        {
                            N(SyntaxKind.OmittedArraySizeExpression); N(SyntaxKind.OmittedArraySizeExpressionToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                        N(SyntaxKind.SemicolonToken);
                    }

                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.ObjectKeyword);
                        N(SyntaxKind.VariableDeclarator);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.EqualsValueClause);
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.ArrayCreationExpression);
                        N(SyntaxKind.NewKeyword);
                        N(SyntaxKind.ArrayType);
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.IntKeyword);
                        N(SyntaxKind.ArrayRankSpecifier);
                        N(SyntaxKind.OpenBracketToken);
                        {
                            N(SyntaxKind.NumericLiteralExpression); N(SyntaxKind.NumericLiteralToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                        N(SyntaxKind.SemicolonToken);
                    }

                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.ObjectKeyword);
                        N(SyntaxKind.VariableDeclarator);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.EqualsValueClause);
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.ArrayCreationExpression);
                        N(SyntaxKind.NewKeyword);
                        N(SyntaxKind.ArrayType);
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.IntKeyword);
                        N(SyntaxKind.ArrayRankSpecifier);
                        N(SyntaxKind.OpenBracketToken);
                        {
                            N(SyntaxKind.OmittedArraySizeExpression); N(SyntaxKind.OmittedArraySizeExpressionToken);
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.OmittedArraySizeExpression); N(SyntaxKind.OmittedArraySizeExpressionToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                        N(SyntaxKind.SemicolonToken);
                    }

                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.ObjectKeyword);
                        N(SyntaxKind.VariableDeclarator);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.EqualsValueClause);
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.ArrayCreationExpression);
                        N(SyntaxKind.NewKeyword);
                        N(SyntaxKind.ArrayType);
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.IntKeyword);
                        N(SyntaxKind.ArrayRankSpecifier);
                        N(SyntaxKind.OpenBracketToken);
                        {
                            N(SyntaxKind.NumericLiteralExpression); N(SyntaxKind.NumericLiteralToken);
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.NumericLiteralExpression); N(SyntaxKind.NumericLiteralToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                        N(SyntaxKind.SemicolonToken);
                    }

                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.ObjectKeyword);
                        N(SyntaxKind.VariableDeclarator);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.EqualsValueClause);
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.ArrayCreationExpression);
                        N(SyntaxKind.NewKeyword);
                        N(SyntaxKind.ArrayType);
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.IntKeyword);
                        N(SyntaxKind.ArrayRankSpecifier);
                        N(SyntaxKind.OpenBracketToken);
                        {
                            N(SyntaxKind.OmittedArraySizeExpression); N(SyntaxKind.OmittedArraySizeExpressionToken);
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.OmittedArraySizeExpression); N(SyntaxKind.OmittedArraySizeExpressionToken);
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.OmittedArraySizeExpression); N(SyntaxKind.OmittedArraySizeExpressionToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                        N(SyntaxKind.SemicolonToken);
                    }

                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.ObjectKeyword);
                        N(SyntaxKind.VariableDeclarator);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.EqualsValueClause);
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.ArrayCreationExpression);
                        N(SyntaxKind.NewKeyword);
                        N(SyntaxKind.ArrayType);
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.IntKeyword);
                        N(SyntaxKind.ArrayRankSpecifier);
                        N(SyntaxKind.OpenBracketToken);
                        {
                            N(SyntaxKind.NumericLiteralExpression); N(SyntaxKind.NumericLiteralToken);
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.IdentifierName); N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                        N(SyntaxKind.SemicolonToken);
                    }

                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.ObjectKeyword);
                        N(SyntaxKind.VariableDeclarator);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.EqualsValueClause);
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.ArrayCreationExpression);
                        N(SyntaxKind.NewKeyword);
                        N(SyntaxKind.ArrayType);
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.IntKeyword);
                        N(SyntaxKind.ArrayRankSpecifier);
                        N(SyntaxKind.OpenBracketToken);
                        {
                            N(SyntaxKind.IdentifierName); N(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.NumericLiteralExpression); N(SyntaxKind.NumericLiteralToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                        N(SyntaxKind.SemicolonToken);
                    }

                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.ObjectKeyword);
                        N(SyntaxKind.VariableDeclarator);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.EqualsValueClause);
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.ArrayCreationExpression);
                        N(SyntaxKind.NewKeyword);
                        N(SyntaxKind.ArrayType);
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.IntKeyword);
                        N(SyntaxKind.ArrayRankSpecifier);
                        N(SyntaxKind.OpenBracketToken);
                        {
                            N(SyntaxKind.NumericLiteralExpression); N(SyntaxKind.NumericLiteralToken);
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.NumericLiteralExpression); N(SyntaxKind.NumericLiteralToken);
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.IdentifierName); N(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.IdentifierName); N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                        N(SyntaxKind.SemicolonToken);
                    }

                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }
    }
}
