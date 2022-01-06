// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LambdaParameterParsingTests : ParsingTests
    {
        public LambdaParameterParsingTests(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: options);
        }

        protected override CSharpSyntaxNode ParseNode(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseExpression(text, options: options);
        }

        [Fact]
        public void EndOfFileAfterOut()
        {
            UsingTree(@"
class C {
     void Goo() {
          System.Func<int, int> f = (out 
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
                        N(SyntaxKind.IdentifierToken, "Goo");
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
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "System");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.GenericName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Func");
                                            N(SyntaxKind.TypeArgumentList);
                                            {
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                                N(SyntaxKind.CommaToken);
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                                N(SyntaxKind.GreaterThanToken);
                                            }
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "f");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ParenthesizedExpression);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                M(SyntaxKind.IdentifierName);
                                                {
                                                    M(SyntaxKind.IdentifierToken);
                                                }
                                                M(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
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
        public void EndOfFileAfterOutType()
        {
            UsingTree(@"
class C {
     void Goo() {
          System.Func<int, int> f = (out C
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
                        N(SyntaxKind.IdentifierToken, "Goo");
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
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "System");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.GenericName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Func");
                                            N(SyntaxKind.TypeArgumentList);
                                            {
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                                N(SyntaxKind.CommaToken);
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                                N(SyntaxKind.GreaterThanToken);
                                            }
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "f");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ParenthesizedExpression);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                M(SyntaxKind.IdentifierName);
                                                {
                                                    M(SyntaxKind.IdentifierToken);
                                                }
                                                M(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
                                }
                                M(SyntaxKind.SemicolonToken);
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
        public void EndOfFileAfterOutTypeIdentifier()
        {
            UsingTree(@"
class C {
     void Goo() {
          System.Func<int, int> f = (out C c
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
                        N(SyntaxKind.IdentifierToken, "Goo");
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
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "System");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.GenericName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Func");
                                            N(SyntaxKind.TypeArgumentList);
                                            {
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                                N(SyntaxKind.CommaToken);
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                                N(SyntaxKind.GreaterThanToken);
                                            }
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "f");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ParenthesizedExpression);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                M(SyntaxKind.IdentifierName);
                                                {
                                                    M(SyntaxKind.IdentifierToken);
                                                }
                                                M(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "C");
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "c");
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
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
        public void EndOfFileAfterOutTypeIdentifierParen()
        {
            UsingTree(@"
class C {
     void Goo() {
          System.Func<int, int> f = (out C c
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
                        N(SyntaxKind.IdentifierToken, "Goo");
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
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "System");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.GenericName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Func");
                                            N(SyntaxKind.TypeArgumentList);
                                            {
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                                N(SyntaxKind.CommaToken);
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                                N(SyntaxKind.GreaterThanToken);
                                            }
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "f");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ParenthesizedExpression);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                M(SyntaxKind.IdentifierName);
                                                {
                                                    M(SyntaxKind.IdentifierToken);
                                                }
                                                M(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "C");
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "c");
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
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
        public void EndOfFileAfterOutTypeIdentifierComma()
        {
            UsingTree(@"
class C {
     void Goo() {
          System.Func<int, int> f = (out C c,
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
                        N(SyntaxKind.IdentifierToken, "Goo");
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
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "System");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.GenericName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Func");
                                            N(SyntaxKind.TypeArgumentList);
                                            {
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                                N(SyntaxKind.CommaToken);
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                                N(SyntaxKind.GreaterThanToken);
                                            }
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "f");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ParenthesizedExpression);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                M(SyntaxKind.IdentifierName);
                                                {
                                                    M(SyntaxKind.IdentifierToken);
                                                }
                                                M(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "C");
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "c");
                                    }
                                    N(SyntaxKind.CommaToken);
                                    M(SyntaxKind.VariableDeclarator);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
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

        [Fact, WorkItem(14167, "https://github.com/dotnet/roslyn/issues/14167")]
        public void HangingLambdaParsing_Bug14167()
        {
            var tree = UsingNode(@"(int a, int b Main();");
            tree.GetDiagnostics().Verify(
                // (1,1): error CS1073: Unexpected token 'b'
                // (int a, int b Main();
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "(int a, int ").WithArguments("b").WithLocation(1, 1),
                // (1,9): error CS1525: Invalid expression term 'int'
                // (int a, int b Main();
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(1, 9),
                // (1,13): error CS1026: ) expected
                // (int a, int b Main();
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "b").WithLocation(1, 13)
                );
            N(SyntaxKind.TupleExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.DeclarationExpression);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                }
                M(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [Fact]
        public void Arglist_01()
        {
            string source = "(__arglist) => { }";
            UsingExpression(source,
                // (1,1): error CS1073: Unexpected token '=>'
                // (__arglist) => { }
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "(__arglist)").WithArguments("=>").WithLocation(1, 1));

            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.ArgListExpression);
                {
                    N(SyntaxKind.ArgListKeyword);
                }
                N(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [Fact]
        public void Arglist_02()
        {
            string source = "(int x, __arglist) => { }";
            UsingExpression(source,
                // (1,1): error CS1073: Unexpected token '=>'
                // (int x, __arglist) => { }
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "(int x, __arglist)").WithArguments("=>").WithLocation(1, 1));

            N(SyntaxKind.TupleExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.DeclarationExpression);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.ArgListExpression);
                    {
                        N(SyntaxKind.ArgListKeyword);
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [Fact]
        public void Arglist_03()
        {
            string source = "static (__arglist) => { }";
            UsingExpression(source,
                // (1,9): error CS1041: Identifier expected; '__arglist' is a keyword
                // static (__arglist) => { }
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "__arglist").WithArguments("", "__arglist").WithLocation(1, 9));

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.StaticKeyword);
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
            EOF();
        }

        [Fact]
        public void TestLambdaWithNullValidation()
        {
            UsingDeclaration("Func<string, string> func1 = x!! => x + \"1\";", options: TestOptions.RegularPreview);
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    N(SyntaxKind.IdentifierToken, "Func");
                    N(SyntaxKind.TypeArgumentList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.StringKeyword);
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.StringKeyword);
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "func1");
                }
                N(SyntaxKind.EqualsValueClause);
                {
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.SimpleLambdaExpression);
                    {
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.ExclamationExclamationToken);
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.AddExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.PlusToken);
                            N(SyntaxKind.StringLiteralExpression);
                            {
                                N(SyntaxKind.StringLiteralToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
        }

        [Fact]
        public void TestLambdaWithNullValidationParams()
        {
            UsingDeclaration("Func<int, int, bool> func1 = (x!!, y) => x == y;", options: TestOptions.RegularPreview);
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    N(SyntaxKind.IdentifierToken, "Func");
                    N(SyntaxKind.TypeArgumentList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.BoolKeyword);
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "func1");
                }
                N(SyntaxKind.EqualsValueClause);
                {
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.ParenthesizedLambdaExpression);
                    {
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                                N(SyntaxKind.ExclamationExclamationToken);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.EqualsExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.EqualsEqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
        }

        [Fact]
        public void TestNullCheckedSingleParamInParens()
        {
            UsingDeclaration("Func<int, int> func1 = (x!!) => x;", options: TestOptions.RegularPreview);
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    N(SyntaxKind.IdentifierToken, "Func");
                    N(SyntaxKind.TypeArgumentList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "func1");
                }
                N(SyntaxKind.EqualsValueClause);
                {
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.ParenthesizedLambdaExpression);
                    {
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                                N(SyntaxKind.ExclamationExclamationToken);
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
        }

        [Fact]
        public void TestNullCheckedSingleParamNoSpaces()
        {
            UsingDeclaration("Func<int, int> func1 = x!!=>x;", options: TestOptions.RegularPreview, expectedErrors: new DiagnosticDescription[0]);
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    N(SyntaxKind.IdentifierToken, "Func");
                    N(SyntaxKind.TypeArgumentList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "func1");
                }
                N(SyntaxKind.EqualsValueClause);
                {
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.SimpleLambdaExpression);
                    {
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                            N(SyntaxKind.ExclamationExclamationToken);
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
        }

        [Fact]
        public void TestNullCheckedTypedSingleParamInParen()
        {
            UsingDeclaration("Func<int, int> func1 = (int x!!) => x;", options: TestOptions.RegularPreview);
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    N(SyntaxKind.IdentifierToken, "Func");
                    N(SyntaxKind.TypeArgumentList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "func1");
                }
                N(SyntaxKind.EqualsValueClause);
                {
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.ParenthesizedLambdaExpression);
                    {
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "x");
                                N(SyntaxKind.ExclamationExclamationToken);
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
        }

        [Fact]
        public void TestNullCheckedTypedManyParams()
        {
            UsingDeclaration("Func<int, int, int> func1 = (int x!!, int y) => x;", options: TestOptions.RegularPreview);
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    N(SyntaxKind.IdentifierToken, "Func");
                    N(SyntaxKind.TypeArgumentList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "func1");
                }
                N(SyntaxKind.EqualsValueClause);
                {
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.ParenthesizedLambdaExpression);
                    {
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "x");
                                N(SyntaxKind.ExclamationExclamationToken);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
        }

        [Fact]
        public void TestManyNullCheckedTypedParams()
        {
            UsingDeclaration("Func<int, int, int> func1 = (int x!!, int y!!) => x;", options: TestOptions.RegularPreview);
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    N(SyntaxKind.IdentifierToken, "Func");
                    N(SyntaxKind.TypeArgumentList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "func1");
                }
                N(SyntaxKind.EqualsValueClause);
                {
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.ParenthesizedLambdaExpression);
                    {
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "x");
                                N(SyntaxKind.ExclamationExclamationToken);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "y");
                                N(SyntaxKind.ExclamationExclamationToken);
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
        }

        [Fact]
        public void TestNullCheckedNoParams()
        {
            UsingDeclaration("Func<int> func1 = (!!) => 42;", options: TestOptions.RegularPreview, expectedErrors: new DiagnosticDescription[]
            {
                    // (1,22): error CS1525: Invalid expression term ')'
                    // Func<int> func1 = (!!) => 42;
                    Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(1, 22),
                    // (1,24): error CS1003: Syntax error, ',' expected
                    // Func<int> func1 = (!!) => 42;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",", "=>").WithLocation(1, 24)
            });
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func1");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.ParenthesizedExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.LogicalNotExpression);
                                {
                                    N(SyntaxKind.ExclamationToken);
                                    N(SyntaxKind.LogicalNotExpression);
                                    {
                                        N(SyntaxKind.ExclamationToken);
                                        M(SyntaxKind.IdentifierName);
                                        {
                                            M(SyntaxKind.IdentifierToken);
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
        }

        [Fact]
        public void TestNullCheckedDiscard()
        {
            UsingDeclaration("Func<int, int> func1 = (_!!) => 42;", options: TestOptions.RegularPreview);
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    N(SyntaxKind.IdentifierToken, "Func");
                    N(SyntaxKind.TypeArgumentList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "func1");
                }
                N(SyntaxKind.EqualsValueClause);
                {
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.ParenthesizedLambdaExpression);
                    {
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                                N(SyntaxKind.ExclamationExclamationToken);
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken);
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
        }

        [Fact]
        public void TestNullCheckedSyntaxCorrection0()
        {
            UsingDeclaration("Func<string, string> func0 = x!=> x;", options: TestOptions.RegularPreview,
                    // (1,31): error CS1003: Syntax error, '!!' expected
                    // Func<string, string> func0 = x!=> x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments("!!", "!").WithLocation(1, 31));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func0");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.SimpleLambdaExpression);
                            {
                                N(SyntaxKind.Parameter);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                    N(SyntaxKind.ExclamationExclamationToken);
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
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
        public void TestNullCheckedSyntaxCorrection1()
        {
            UsingDeclaration("Func<string, string> func1 = x !=> x;", options: TestOptions.RegularPreview,
                    // (1,32): error CS1003: Syntax error, '!!' expected
                    // Func<string, string> func1 = x !=> x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments("!!", "!").WithLocation(1, 32));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func1");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.SimpleLambdaExpression);
                            {
                                N(SyntaxKind.Parameter);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                    N(SyntaxKind.ExclamationExclamationToken);
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
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
        public void TestNullCheckedSyntaxCorrection2()
        {
            UsingDeclaration("Func<string, string> func2 = x != > x;", options: TestOptions.RegularPreview,
                    // (1,32): error CS1003: Syntax error, '!!' expected
                    // Func<string, string> func2 = x != > x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments("!!", "!").WithLocation(1, 32),
                    // (1,33): error CS1003: Syntax error, '=>' expected
                    // Func<string, string> func2 = x != > x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "=").WithArguments("=>", "=").WithLocation(1, 33));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func2");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.SimpleLambdaExpression);
                            {
                                N(SyntaxKind.Parameter);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                    N(SyntaxKind.ExclamationExclamationToken);
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
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
        public void TestNullCheckedSyntaxCorrection3()
        {
            UsingDeclaration("Func<string, string> func3 = x! => x;", options: TestOptions.RegularPreview,
                    // (1,33): error CS1003: Syntax error, ',' expected
                    // Func<string, string> func3 = x! => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",", "=>").WithLocation(1, 33));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func3");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.SuppressNullableWarningExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                                N(SyntaxKind.ExclamationToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
        }

        [Fact]
        public void TestNullCheckedSyntaxCorrection4()
        {
            UsingDeclaration("Func<string, string> func4 = x ! => x;", options: TestOptions.RegularPreview,
                    // (1,34): error CS1003: Syntax error, ',' expected
                    // Func<string, string> func4 = x ! => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",", "=>").WithLocation(1, 34));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func4");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.SuppressNullableWarningExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                                N(SyntaxKind.ExclamationToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
        }

        [Fact]
        public void TestNullCheckedSyntaxCorrection5()
        {
            UsingDeclaration("Func<string, string> func5 = x !!=> x;", options: TestOptions.RegularPreview);
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func5");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.SimpleLambdaExpression);
                            {
                                N(SyntaxKind.Parameter);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                    N(SyntaxKind.ExclamationExclamationToken);
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
        }

        [Fact]
        public void TestNullCheckedSyntaxCorrection6()
        {
            UsingDeclaration("Func<string, string> func6 = x !!= > x;", options: TestOptions.RegularPreview,
                    // (1,34): error CS1003: Syntax error, '=>' expected
                    // Func<string, string> func6 = x !!= > x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "=").WithArguments("=>", "=").WithLocation(1, 34));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func6");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.SimpleLambdaExpression);
                            {
                                N(SyntaxKind.Parameter);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                    N(SyntaxKind.ExclamationExclamationToken);
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
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
        public void TestNullCheckedSyntaxCorrection7()
        {
            UsingDeclaration("Func<string, string> func7 = x!! => x;", options: TestOptions.RegularPreview);
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func7");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.SimpleLambdaExpression);
                            {
                                N(SyntaxKind.Parameter);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                    N(SyntaxKind.ExclamationExclamationToken);
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
        }

        [Fact]
        public void TestNullCheckedSyntaxCorrection8()
        {
            UsingDeclaration("Func<string, string> func8 = x! !=> x;", options: TestOptions.RegularPreview,
                    // (1,31): error CS1003: Syntax error, '!!' expected
                    // Func<string, string> func8 = x! !=> x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments("!!", "!").WithLocation(1, 31));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func8");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.SimpleLambdaExpression);
                            {
                                N(SyntaxKind.Parameter);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                    N(SyntaxKind.ExclamationExclamationToken);
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
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
        public void TestNullCheckedSyntaxCorrection9()
        {
            UsingDeclaration("Func<string, string> func9 = x! ! => x;", options: TestOptions.RegularPreview,
                    // (1,31): error CS1003: Syntax error, '!!' expected
                    // Func<string, string> func9 = x! ! => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "! ").WithArguments("!!", "!").WithLocation(1, 31));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func9");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.SimpleLambdaExpression);
                            {
                                N(SyntaxKind.Parameter);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                    N(SyntaxKind.ExclamationExclamationToken);
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
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
        public void TestBracesAfterSimpleLambdaName()
        {
            UsingDeclaration("Func<string[], string> func0 = x[] => x;", options: TestOptions.RegularPreview,
                    // (1,34): error CS0443: Syntax error; value expected
                    // Func<string[], string> func0 = x[] => x;
                    Diagnostic(ErrorCode.ERR_ValueExpected, "]").WithLocation(1, 34),
                    // (1,36): error CS1003: Syntax error, ',' expected
                    // Func<string[], string> func0 = x[] => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",", "=>").WithLocation(1, 36));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
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
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func0");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.ElementAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
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
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void TestBracesAfterParenthesizedLambdaName()
        {
            UsingDeclaration("Func<string[], string> func0 = (x[]) => x;", options: TestOptions.RegularPreview,
                    // (1,36): error CS1001: Identifier expected
                    // Func<string[], string> func0 = (x[]) => x;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(1, 36));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
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
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func0");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.ParenthesizedLambdaExpression);
                            {
                                N(SyntaxKind.ParameterList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Parameter);
                                    {
                                        N(SyntaxKind.ArrayType);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "x");
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
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
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
        public void TestBracesAfterParenthesizedLambdaTypeAndName()
        {
            UsingDeclaration("Func<string[], string> func0 = (string x[]) => x;", options: TestOptions.RegularPreview,
                    // (1,33): error CS1525: Invalid expression term 'string'
                    // Func<string[], string> func0 = (string x[]) => x;
                    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "string").WithArguments("string").WithLocation(1, 33),
                    // (1,40): error CS1026: ) expected
                    // Func<string[], string> func0 = (string x[]) => x;
                    Diagnostic(ErrorCode.ERR_CloseParenExpected, "x").WithLocation(1, 40),
                    // (1,40): error CS1003: Syntax error, ',' expected
                    // Func<string[], string> func0 = (string x[]) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments(",", "").WithLocation(1, 40));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
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
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func0");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.ParenthesizedExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                                M(SyntaxKind.CloseParenToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void TestDefaultValueSimpleLambda()
        {
            UsingDeclaration("Func<string, string> func0 = x = null => x;", options: TestOptions.RegularPreview,
                    // (1,39): error CS1003: Syntax error, ',' expected
                    // Func<string, string> func0 = x = null => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",", "=>").WithLocation(1, 39));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func0");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
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
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void TestDefaultValueParenthesizedLamda1()
        {
            UsingDeclaration("Func<string, string> func0 = (x = null) => x;", options: TestOptions.RegularPreview,
                    // (1,41): error CS1003: Syntax error, ',' expected
                    // Func<string, string> func0 = (x = null) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",", "=>").WithLocation(1, 41));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func0");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.ParenthesizedExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
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
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void TestDefaultValueParenthesizedLamda2()
        {
            UsingDeclaration("Func<string, string> func0 = (y, x = null) => x;", options: TestOptions.RegularPreview,
                    // (1,44): error CS1003: Syntax error, ',' expected
                    // Func<string, string> func0 = (y, x = null) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",", "=>").WithLocation(1, 44));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func0");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "y");
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
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
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void TestDefaultValueParenthesizedLamdaWithType1()
        {
            UsingDeclaration("Func<string, string> func0 = (string x = null) => x;", options: TestOptions.RegularPreview,
                    // (1,31): error CS1525: Invalid expression term 'string'
                    // Func<string, string> func0 = (string x = null) => x;
                    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "string").WithArguments("string").WithLocation(1, 31),
                    // (1,38): error CS1026: ) expected
                    // Func<string, string> func0 = (string x = null) => x;
                    Diagnostic(ErrorCode.ERR_CloseParenExpected, "x").WithLocation(1, 38),
                    // (1,38): error CS1003: Syntax error, ',' expected
                    // Func<string, string> func0 = (string x = null) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments(",", "").WithLocation(1, 38));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func0");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.ParenthesizedExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                                M(SyntaxKind.CloseParenToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void TestDefaultValueParenthesizedLamdaWithType2()
        {
            UsingDeclaration("Func<string, string> func0 = (string y, string x = null) => x;", options: TestOptions.RegularPreview,
                    // (1,41): error CS1525: Invalid expression term 'string'
                    // Func<string, string> func0 = (string y, string x = null) => x;
                    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "string").WithArguments("string").WithLocation(1, 41),
                    // (1,48): error CS1026: ) expected
                    // Func<string, string> func0 = (string y, string x = null) => x;
                    Diagnostic(ErrorCode.ERR_CloseParenExpected, "x").WithLocation(1, 48),
                    // (1,48): error CS1003: Syntax error, ',' expected
                    // Func<string, string> func0 = (string y, string x = null) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments(",", "").WithLocation(1, 48));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func0");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.StringKeyword);
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "y");
                                        }
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
                                M(SyntaxKind.CloseParenToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void TestNullCheckedDefaultValueSimpleLambda()
        {
            UsingDeclaration("Func<string, string> func0 = x!! = null => x;", options: TestOptions.RegularPreview,
                    // (1,41): error CS1003: Syntax error, ',' expected
                    // Func<string, string> func0 = x!! = null => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",", "=>").WithLocation(1, 41));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func0");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.SimpleAssignmentExpression);
                            {
                                N(SyntaxKind.SuppressNullableWarningExpression);
                                {
                                    N(SyntaxKind.SuppressNullableWarningExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "x");
                                        }
                                        N(SyntaxKind.ExclamationToken);
                                    }
                                    N(SyntaxKind.ExclamationToken);
                                }
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.NullLiteralExpression);
                                {
                                    N(SyntaxKind.NullKeyword);
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
        public void TestNullCheckedDefaultValueParenthesizedLamda1()
        {
            UsingDeclaration("Func<string, string> func0 = (x!! = null) => x;", options: TestOptions.RegularPreview,
                    // (1,35): error CS1065: Default values are not valid in this context.
                    // Func<string, string> func0 = (x!! = null) => x;
                    Diagnostic(ErrorCode.ERR_DefaultValueNotAllowed, "=").WithLocation(1, 35));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func0");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.ParenthesizedLambdaExpression);
                            {
                                N(SyntaxKind.ParameterList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Parameter);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                        N(SyntaxKind.ExclamationExclamationToken);
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
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
        public void TestNullCheckedDefaultValueParenthesizedLamda2()
        {
            UsingDeclaration("Func<string, string> func0 = (y, x!! = null) => x;", options: TestOptions.RegularPreview,
                    // (1,38): error CS1065: Default values are not valid in this context.
                    // Func<string, string> func0 = (y, x!! = null) => x;
                    Diagnostic(ErrorCode.ERR_DefaultValueNotAllowed, "=").WithLocation(1, 38));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func0");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.ParenthesizedLambdaExpression);
                            {
                                N(SyntaxKind.ParameterList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Parameter);
                                    {
                                        N(SyntaxKind.IdentifierToken, "y");
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.Parameter);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                        N(SyntaxKind.ExclamationExclamationToken);
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
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
        public void TestNullCheckedDefaultValueParenthesizedLamdaWithType1()
        {
            UsingDeclaration("Func<string, string> func0 = (string x!! = null) => x;", options: TestOptions.RegularPreview,
                    // (1,42): error CS1065: Default values are not valid in this context.
                    // Func<string, string> func0 = (string x!! = null) => x;
                    Diagnostic(ErrorCode.ERR_DefaultValueNotAllowed, "=").WithLocation(1, 42));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func0");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.ParenthesizedLambdaExpression);
                            {
                                N(SyntaxKind.ParameterList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Parameter);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.StringKeyword);
                                        }
                                        N(SyntaxKind.IdentifierToken, "x");
                                        N(SyntaxKind.ExclamationExclamationToken);
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
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
        public void TestNullCheckedDefaultValueParenthesizedLamdaWithType2()
        {
            UsingDeclaration("Func<string, string> func0 = (string y, string x!! = null) => x;", options: TestOptions.RegularPreview,
                    // (1,52): error CS1065: Default values are not valid in this context.
                    // Func<string, string> func0 = (string y, string x!! = null) => x;
                    Diagnostic(ErrorCode.ERR_DefaultValueNotAllowed, "=").WithLocation(1, 52));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func0");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.ParenthesizedLambdaExpression);
                            {
                                N(SyntaxKind.ParameterList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Parameter);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.StringKeyword);
                                        }
                                        N(SyntaxKind.IdentifierToken, "y");
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.Parameter);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.StringKeyword);
                                        }
                                        N(SyntaxKind.IdentifierToken, "x");
                                        N(SyntaxKind.ExclamationExclamationToken);
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
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
        public void TestNullCheckedSpaceBetweenSimpleLambda()
        {
            UsingDeclaration("Func<string, string> func0 = x! ! => x;", options: TestOptions.RegularPreview,
                    // (1,31): error CS1003: Syntax error, '!!' expected
                    // Func<string, string> func0 = x! ! => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "! ").WithArguments("!!", "!").WithLocation(1, 31));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func0");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.SimpleLambdaExpression);
                            {
                                N(SyntaxKind.Parameter);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                    N(SyntaxKind.ExclamationExclamationToken);
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
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
        public void TestNullCheckedSpaceBetweenParenthesizedLamda1()
        {
            UsingDeclaration("Func<string, string> func0 = (x! !) => x;", options: TestOptions.RegularPreview,
                    // (1,32): error CS1003: Syntax error, '!!' expected
                    // Func<string, string> func0 = (x! !) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments("!!", "!").WithLocation(1, 32));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func0");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.ParenthesizedLambdaExpression);
                            {
                                N(SyntaxKind.ParameterList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Parameter);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                        N(SyntaxKind.ExclamationExclamationToken);
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
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
        public void TestNullCheckedSpaceBetweenParenthesizedLamda2()
        {
            UsingDeclaration("Func<string, string> func0 = (y, x! !) => x;", options: TestOptions.RegularPreview,
                    // (1,35): error CS1003: Syntax error, '!!' expected
                    // Func<string, string> func0 = (y, x! !) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments("!!", "!").WithLocation(1, 35));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func0");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.ParenthesizedLambdaExpression);
                            {
                                N(SyntaxKind.ParameterList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Parameter);
                                    {
                                        N(SyntaxKind.IdentifierToken, "y");
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.Parameter);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                        N(SyntaxKind.ExclamationExclamationToken);
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
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
        public void TestNullCheckedSpaceBetweenLamdaWithType1()
        {
            UsingDeclaration("Func<string, string> func0 = (string x! !) => x;", options: TestOptions.RegularPreview,
                    // (1,39): error CS1003: Syntax error, '!!' expected
                    // Func<string, string> func0 = (string x! !) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments("!!", "!").WithLocation(1, 39));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func0");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.ParenthesizedLambdaExpression);
                            {
                                N(SyntaxKind.ParameterList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Parameter);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.StringKeyword);
                                        }
                                        N(SyntaxKind.IdentifierToken, "x");
                                        N(SyntaxKind.ExclamationExclamationToken);
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
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
        public void TestNullCheckedSpaceBetweenLamdaWithType2()
        {
            UsingDeclaration("Func<string, string> func0 = (string y, string x! !) => x;", options: TestOptions.RegularPreview,
                    // (1,49): error CS1003: Syntax error, '!!' expected
                    // Func<string, string> func0 = (string y, string x! !) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments("!!", "!").WithLocation(1, 49));
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Func");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "func0");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.ParenthesizedLambdaExpression);
                            {
                                N(SyntaxKind.ParameterList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Parameter);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.StringKeyword);
                                        }
                                        N(SyntaxKind.IdentifierToken, "y");
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.Parameter);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.StringKeyword);
                                        }
                                        N(SyntaxKind.IdentifierToken, "x");
                                        N(SyntaxKind.ExclamationExclamationToken);
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
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
        public void AsyncAwaitInLambda()
        {
            UsingStatement(@"F(async () => await Task.FromResult(4));");
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.InvocationExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "F");
                    }
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
                                N(SyntaxKind.AwaitExpression);
                                {
                                    N(SyntaxKind.AwaitKeyword);
                                    N(SyntaxKind.InvocationExpression);
                                    {
                                        N(SyntaxKind.SimpleMemberAccessExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Task");
                                            }
                                            N(SyntaxKind.DotToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "FromResult");
                                            }
                                        }
                                        N(SyntaxKind.ArgumentList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.Argument);
                                            {
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "4");
                                                }
                                            }
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }
    }
}
