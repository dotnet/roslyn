// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken);
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
                                            N(SyntaxKind.IdentifierToken);
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.GenericName);
                                        {
                                            N(SyntaxKind.IdentifierToken);
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
                                        N(SyntaxKind.IdentifierToken);
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
                                                        N(SyntaxKind.OutKeyword);
                                                        M(SyntaxKind.IdentifierName); // parameter type
                                                        {
                                                            M(SyntaxKind.IdentifierToken);
                                                        }
                                                        M(SyntaxKind.IdentifierToken); // parameter name
                                                    }
                                                    M(SyntaxKind.CloseParenToken);
                                                }
                                                M(SyntaxKind.EqualsGreaterThanToken);
                                                M(SyntaxKind.IdentifierName); // lambda body
                                                {
                                                    M(SyntaxKind.IdentifierToken);
                                                }
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
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken);
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
                                            N(SyntaxKind.IdentifierToken);
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.GenericName);
                                        {
                                            N(SyntaxKind.IdentifierToken);
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
                                        N(SyntaxKind.IdentifierToken);
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
                                                        N(SyntaxKind.OutKeyword);
                                                        N(SyntaxKind.IdentifierName); // parameter type
                                                        {
                                                            N(SyntaxKind.IdentifierToken);
                                                        }
                                                        M(SyntaxKind.IdentifierToken); // parameter name
                                                    }
                                                    M(SyntaxKind.CloseParenToken);
                                                }
                                                M(SyntaxKind.EqualsGreaterThanToken);
                                                M(SyntaxKind.IdentifierName); // lambda body
                                                {
                                                    M(SyntaxKind.IdentifierToken);
                                                }
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
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken);
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
                                            N(SyntaxKind.IdentifierToken);
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.GenericName);
                                        {
                                            N(SyntaxKind.IdentifierToken);
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
                                        N(SyntaxKind.IdentifierToken);
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
                                                        N(SyntaxKind.OutKeyword);
                                                        N(SyntaxKind.IdentifierName); // parameter type
                                                        {
                                                            N(SyntaxKind.IdentifierToken);
                                                        }
                                                        N(SyntaxKind.IdentifierToken); // parameter name
                                                    }
                                                    M(SyntaxKind.CloseParenToken);
                                                }
                                                M(SyntaxKind.EqualsGreaterThanToken);
                                                M(SyntaxKind.IdentifierName); // lambda body
                                                {
                                                    M(SyntaxKind.IdentifierToken);
                                                }
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
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken);
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
                                            N(SyntaxKind.IdentifierToken);
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.GenericName);
                                        {
                                            N(SyntaxKind.IdentifierToken);
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
                                        N(SyntaxKind.IdentifierToken);
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
                                                        N(SyntaxKind.OutKeyword);
                                                        N(SyntaxKind.IdentifierName); // parameter type
                                                        {
                                                            N(SyntaxKind.IdentifierToken);
                                                        }
                                                        N(SyntaxKind.IdentifierToken); // parameter name
                                                    }
                                                    M(SyntaxKind.CloseParenToken);
                                                }
                                                M(SyntaxKind.EqualsGreaterThanToken);
                                                M(SyntaxKind.IdentifierName); // lambda body
                                                {
                                                    M(SyntaxKind.IdentifierToken);
                                                }
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
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken);
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
                                            N(SyntaxKind.IdentifierToken);
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.GenericName);
                                        {
                                            N(SyntaxKind.IdentifierToken);
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
                                        N(SyntaxKind.IdentifierToken);
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
                                                        N(SyntaxKind.OutKeyword);
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken);
                                                        }
                                                        N(SyntaxKind.IdentifierToken);
                                                    }
                                                    N(SyntaxKind.CommaToken);
                                                    M(SyntaxKind.Parameter);
                                                    {
                                                        M(SyntaxKind.IdentifierToken);
                                                    }
                                                    M(SyntaxKind.CloseParenToken);
                                                }
                                                M(SyntaxKind.EqualsGreaterThanToken);
                                                M(SyntaxKind.IdentifierName);
                                                {
                                                    M(SyntaxKind.IdentifierToken);
                                                }
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
        public void TestLambdaWithNullValidation()
        {
            UsingDeclaration("Func<string, string> func1 = x! => x + \"1\";", options: TestOptions.RegularPreview);
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
                            N(SyntaxKind.ExclamationToken);
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
            UsingDeclaration("Func<int, int, bool> func1 = (x!, y) => x == y;", options: TestOptions.RegularPreview);
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
                                N(SyntaxKind.ExclamationToken);
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
            UsingDeclaration("Func<int, int> func1 = (x!) => x;", options: TestOptions.RegularPreview);
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
                                N(SyntaxKind.ExclamationToken);
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

        [Fact(Skip = "PROTOTYPE(BangBang)")]
        public void TestNullCheckedSingleParamNoSpaces()
        {
            UsingDeclaration("Func<int, int> func1 = x!=>x;", options: TestOptions.RegularPreview, expectedErrors: new DiagnosticDescription[]
            {
                    // (1,25): error CS8712: Space required between explanation-point and equals-sign here.
                    // Func<int, int> func1 = x!=>x;
                    Diagnostic(ErrorCode.ERR_NeedSpaceBetweenExclamationAndEquals, "!=").WithLocation(1, 25)
            });
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
                            N(SyntaxKind.ExclamationToken);
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
            UsingDeclaration("Func<int, int> func1 = (int x!) => x;", options: TestOptions.RegularPreview);
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
                                N(SyntaxKind.ExclamationToken);
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
            UsingDeclaration("Func<int, int, int> func1 = (int x!, int y) => x;", options: TestOptions.RegularPreview);
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
                                N(SyntaxKind.ExclamationToken);
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
            UsingDeclaration("Func<int, int, int> func1 = (int x!, int y!) => x;", options: TestOptions.RegularPreview);
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
                                N(SyntaxKind.ExclamationToken);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "y");
                                N(SyntaxKind.ExclamationToken);
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

        [Fact(Skip = "PROTOTYPE(BangBang)")]
        public void TestNullCheckedNoParams()
        {
            UsingDeclaration("Func<int> func1 = (!) => 42;", options: TestOptions.RegularPreview, expectedErrors: new DiagnosticDescription[]
            {
                    // (1,21): error CS1525: Invalid expression term ')'
                    // Func<int> func1 = (!) => 42;
                    Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(1, 21),
                    // (1,23): error CS1003: Syntax error, ',' expected
                    // Func<int> func1 = (!) => 42;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",", "=>").WithLocation(1, 23)
            });
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
                    N(SyntaxKind.ParenthesizedExpression);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.LogicalNotExpression);
                        {
                            N(SyntaxKind.ExclamationToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
        }

        [Fact]
        public void TestNullCheckedDiscard()
        {
            UsingDeclaration("Func<int, int> func1 = (_!) => 42;", options: TestOptions.RegularPreview);
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
                                N(SyntaxKind.ExclamationToken);
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
    }
}
