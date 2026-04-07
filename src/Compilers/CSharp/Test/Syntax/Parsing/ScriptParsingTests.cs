// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ScriptParsingTests : ParsingTests
    {
        public ScriptParsingTests(ITestOutputHelper output) : base(output) { }

        #region Helpers

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: options ?? TestOptions.Script);
        }

        private void ParseAndValidate(string text, params ErrorDescription[] errors)
        {
            ParseAndValidate(text, null, errors);
        }

        private SyntaxTree ParseAndValidate(string text, CSharpParseOptions options, params ErrorDescription[] errors)
        {
            var parsedTree = ParseTree(text, options);
            var parsedText = parsedTree.GetCompilationUnitRoot();

            // we validate the text roundtrips
            Assert.Equal(text, parsedText.ToFullString());

            // get all errors
            var actualErrors = parsedTree.GetDiagnostics(parsedText);
            if (errors == null || errors.Length == 0)
            {
                Assert.Empty(actualErrors);
            }
            else
            {
                DiagnosticsUtils.VerifyErrorCodes(actualErrors, errors);
            }

            return parsedTree;
        }

        #endregion

        [Fact]
        public void Error_StaticPartial()
        {
            var test = @"
int

static partial class C { }
";
            ParseAndValidate(test, new ErrorDescription { Code = 1585, Line = 4, Column = 1 } //static must precede type
                           );
        }

        [WorkItem(529472, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529472")]
        [Fact(Skip = "529472")]
        public void CS1002ERR_SemicolonExpected()
        {
            var test = @"
int a  
Console.Goo
";
            ParseAndValidate(test, TestOptions.Script,
                new ErrorDescription[] {
                    new ErrorDescription { Code = (int)ErrorCode.ERR_SemicolonExpected, Line = 2, Column = 6 },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_SemicolonExpected, Line = 3, Column = 12 }});
        }

        [Fact]
        public void Error_NewKeywordUsedAsOperator()
        {
            var test = @"
new in
";

            UsingTree(test,
                // (2,5): error CS1526: A new expression requires an argument list or (), [], or {} after type
                // new in
                Diagnostic(ErrorCode.ERR_BadNewExpr, "in").WithLocation(2, 5),
                // (2,5): error CS1002: ; expected
                // new in
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "in").WithLocation(2, 5),
                // (2,5): error CS7017: Member definition, statement, or end-of-file expected
                // new in
                Diagnostic(ErrorCode.ERR_GlobalDefinitionOrStatementExpected, "in").WithLocation(2, 5));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ObjectCreationExpression);
                        {
                            N(SyntaxKind.NewKeyword);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            M(SyntaxKind.ArgumentList);
                            {
                                M(SyntaxKind.OpenParenToken);
                                M(SyntaxKind.CloseParenToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        #region Method Declarations

        [Fact]
        public void MethodDeclarationAndMethodCall()
        {
            UsingTree(@"
void bar() { }
bar();
");
            N(SyntaxKind.CompilationUnit);
            {
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
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        #endregion

        #region Field Declarations

        [Fact]
        public void FieldDeclarationError1()
        {
            var tree = UsingTree("int x y;",
                // (1,7): error CS1002: ; expected
                // int x y;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "y").WithLocation(1, 7));
            Assert.True(tree.GetCompilationUnitRoot().ContainsDiagnostics);

            N(SyntaxKind.CompilationUnit);
            {
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
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    M(SyntaxKind.SemicolonToken).IsMissing.ShouldBe(true);
                }

                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void FieldDeclarationError2()
        {
            var tree = UsingTree("int x y z;",
                // (1,7): error CS1002: ; expected
                // int x y z;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "y").WithLocation(1, 7));
            Assert.True(tree.GetCompilationUnitRoot().ContainsDiagnostics);

            N(SyntaxKind.CompilationUnit);
            {
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
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    M(SyntaxKind.SemicolonToken).IsMissing.ShouldBe(true);
                }

                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }

                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        #endregion

        #region Constructor and Finalizer

        [Fact]
        public void Constructor()
        {
            var test = @"
Script() { }
";
            ParseAndValidate(test, new ErrorDescription { Code = (int)ErrorCode.ERR_SemicolonExpected, Line = 2, Column = 10 });
        }

        [Fact]
        public void StaticConstructor()
        {
            var test = @"
static Script() { }
";
            ParseAndValidate(test, new ErrorDescription { Code = 1520, Line = 2, Column = 8 });
        }

        [Fact]
        public void Finalizer()
        {
            var test = @"
~Script() { }
";
            ParseAndValidate(test, new ErrorDescription { Code = (int)ErrorCode.ERR_SemicolonExpected, Line = 2, Column = 11 });
        }

        #endregion

        #region New

        [Fact]
        public void NewExpression()
        {
            UsingTree(@"new[] { 1 };");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ImplicitArrayCreationExpression);
                        {
                            N(SyntaxKind.NewKeyword);
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.CloseBracketToken);
                            N(SyntaxKind.ArrayInitializerExpression);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken);
                                }
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void NewAnonymousTypeExpressionStatement()
        {
            UsingTree(@"new { a = 1 };");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.AnonymousObjectCreationExpression);
                        {
                            N(SyntaxKind.NewKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.AnonymousObjectMemberDeclarator);
                            {
                                N(SyntaxKind.NameEquals);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken);
                                    }
                                    N(SyntaxKind.EqualsToken);
                                }
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken);
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void NewArrayExpressionStatement()
        {
            UsingTree(@"new T[5];");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ArrayCreationExpression);
                        {
                            N(SyntaxKind.NewKeyword);
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken);
                                }
                                N(SyntaxKind.ArrayRankSpecifier);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken);
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void NewArrayExpressionWithInitializerAndPostFixExpressionStatement()
        {
            UsingTree(@"new int[] { }.Clone();");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
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
                                            N(SyntaxKind.OmittedArraySizeExpression);
                                            {
                                                N(SyntaxKind.OmittedArraySizeExpressionToken);
                                            }
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                    }
                                    N(SyntaxKind.ArrayInitializerExpression);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.CloseBraceToken);
                                    }
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void NewModifier_Method_WithBody()
        {
            UsingTree("new void Goo() { }");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
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
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void NewModifier_Method_ReturnsIdentifier()
        {
            var tree = UsingTree(@"
new T Goo();
");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void NewModifier_Method_ReturnsArray()
        {
            UsingTree("new int[] Goo();");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
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
                            N(SyntaxKind.OmittedArraySizeExpression);
                            {
                                N(SyntaxKind.OmittedArraySizeExpressionToken);
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void NewModifier_Method_ReturnsPartial()
        {
            var src = """
new partial Goo();
""";
            var tree = UsingTree(src, options: TestOptions.Regular13);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }

            tree = UsingTree(src,
                // (1,13): error CS1520: Method must have a return type
                // new partial Goo();
                Diagnostic(ErrorCode.ERR_MemberNeedsType, "Goo").WithLocation(1, 13));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.PartialKeyword);
                    M(SyntaxKind.PredefinedType);
                    {
                        M(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Goo");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void NewModifier_Method_ReturnsPartialArray()
        {
            var tree = UsingTree(@"
new partial[] Goo();
");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
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
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void NewModifier_Method_ReturnsPartialQualified()
        {
            var src = """
new partial.partial Goo();
""";
            var tree = UsingTree(src, options: TestOptions.Regular13);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }

            tree = UsingTree(src,
                // (1,13): error CS1525: Invalid expression term 'partial'
                // new partial.partial Goo();
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(1, 13),
                // (1,13): error CS1002: ; expected
                // new partial.partial Goo();
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "partial").WithLocation(1, 13),
                // (1,21): error CS1520: Method must have a return type
                // new partial.partial Goo();
                Diagnostic(ErrorCode.ERR_MemberNeedsType, "Goo").WithLocation(1, 21));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ObjectCreationExpression);
                        {
                            N(SyntaxKind.NewKeyword);
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "partial");
                                }
                                N(SyntaxKind.DotToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.ArgumentList);
                            {
                                M(SyntaxKind.OpenParenToken);
                                M(SyntaxKind.CloseParenToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    M(SyntaxKind.PredefinedType);
                    {
                        M(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Goo");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void NewModifier_PartialMethod_ReturnsPredefined1()
        {
            NewModifier_PartialMethod_ReturnsPredefined("void", SyntaxKind.VoidKeyword);
            NewModifier_PartialMethod_ReturnsPredefined("int", SyntaxKind.IntKeyword);
            NewModifier_PartialMethod_ReturnsPredefined("bool", SyntaxKind.BoolKeyword);
        }

        private void NewModifier_PartialMethod_ReturnsPredefined(string typeName, SyntaxKind keyword)
        {
            var tree = UsingTree("new partial " + typeName + " Goo();");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(keyword);
                    }
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void NewModifier_PartialMethod_ReturnsPartial()
        {
            var src = """
new partial partial Goo();
""";
            var tree = UsingTree(src, options: TestOptions.Regular13);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }

            tree = UsingTree(src,
                // (1,13): error CS1525: Invalid expression term 'partial'
                // new partial partial Goo();
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(1, 13),
                // (1,13): error CS1003: Syntax error, ',' expected
                // new partial partial Goo();
                Diagnostic(ErrorCode.ERR_SyntaxError, "partial").WithArguments(",").WithLocation(1, 13));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void NewModifier_PartialMethod_ReturnsPartialQualified()
        {
            var src = """
new partial partial.partial partial();
""";
            var tree = UsingTree(src, options: TestOptions.Regular13);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }

            tree = UsingTree(src,
                // (1,21): error CS1525: Invalid expression term 'partial'
                // new partial partial.partial partial();
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(1, 21),
                // (1,21): error CS1003: Syntax error, '(' expected
                // new partial partial.partial partial();
                Diagnostic(ErrorCode.ERR_SyntaxError, "partial").WithArguments("(").WithLocation(1, 21),
                // (1,36): error CS1001: Identifier expected
                // new partial partial.partial partial();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 36),
                // (1,36): error CS1003: Syntax error, ',' expected
                // new partial partial.partial partial();
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(",").WithLocation(1, 36),
                // (1,37): error CS8124: Tuple must contain at least two elements.
                // new partial partial.partial partial();
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(1, 37),
                // (1,38): error CS1001: Identifier expected
                // new partial partial.partial partial();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 38),
                // (1,38): error CS1026: ) expected
                // new partial partial.partial partial();
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(1, 38));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "partial");
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
                        }
                        N(SyntaxKind.DotToken);
                    }
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.ParameterList);
                    {
                        M(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "partial");
                            }
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.TupleType);
                            {
                                N(SyntaxKind.OpenParenToken);
                                M(SyntaxKind.TupleElement);
                                {
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                M(SyntaxKind.CommaToken);
                                M(SyntaxKind.TupleElement);
                                {
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void NewModifier_Method_ReturnsPrimitive()
        {
            UsingTree("new int Goo();");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void NewModifier_Indexer_ReturnsIdentifier()
        {
            var tree = UsingTree(@"
new T this[int a] { get; }
");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IndexerDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
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
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void NewModifier_Indexer_ReturnsArray()
        {
            var tree = UsingTree(@"
new T[] this[int a] { get; }
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IndexerDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
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
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void NewModifier_PartialIndexer()
        {
            // partial indexers are not allowed, but we should still parse it and report a semantic error
            // "Only methods, classes, structs, or interfaces may be partial"

            var tree = UsingTree(@"
new partial partial this[int i] { get; }
");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IndexerDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
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
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void NewModifier_WithOtherModifier1()
        {
            NewModifier_WithOtherModifier("public", SyntaxKind.PublicKeyword);
            NewModifier_WithOtherModifier("internal", SyntaxKind.InternalKeyword);
            NewModifier_WithOtherModifier("protected", SyntaxKind.ProtectedKeyword);
            NewModifier_WithOtherModifier("private", SyntaxKind.PrivateKeyword);
            NewModifier_WithOtherModifier("sealed", SyntaxKind.SealedKeyword);
            NewModifier_WithOtherModifier("abstract", SyntaxKind.AbstractKeyword);
            NewModifier_WithOtherModifier("static", SyntaxKind.StaticKeyword);
            NewModifier_WithOtherModifier("virtual", SyntaxKind.VirtualKeyword);
            NewModifier_WithOtherModifier("extern", SyntaxKind.ExternKeyword);
            NewModifier_WithOtherModifier("new", SyntaxKind.NewKeyword);
            NewModifier_WithOtherModifier("override", SyntaxKind.OverrideKeyword);
            NewModifier_WithOtherModifier("readonly", SyntaxKind.ReadOnlyKeyword);
            NewModifier_WithOtherModifier("volatile", SyntaxKind.VolatileKeyword);
            NewModifier_WithOtherModifier("unsafe", SyntaxKind.UnsafeKeyword);
        }

        private void NewModifier_WithOtherModifier(string modifier, SyntaxKind keyword)
        {
            var tree = UsingTree("new " + modifier + @" T Goo;");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(keyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void NewModifier_Class()
        {
            var tree = UsingTree(@"
new class C { }
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void NewModifier_PartialClass()
        {
            var tree = UsingTree(@"
new partial class C { }
");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void NewModifier_ClassWithMisplacedModifiers1()
        {
            var source = "new partial public class C { }";
            CreateCompilation(source).VerifyDiagnostics(
                    // (1,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                    // new partial public class C { }
                    Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(1, 5),
                    // (1,26): error CS0106: The modifier 'new' is not valid for this item
                    // new partial public class C { }
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("new").WithLocation(1, 26)
                );
            var tree = UsingTree(source);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void NewModifier_ClassWithMisplacedModifiers2()
        {
            var source = "new static partial public class C { }";
            CreateCompilation(source).VerifyDiagnostics(
                // (1,12): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                // new static partial public class C { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(1, 12),
                // (1,33): error CS0106: The modifier 'new' is not valid for this item
                // new static partial public class C { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("new").WithLocation(1, 33));
            var tree = UsingTree(source);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        #endregion

        #region Using

        [Fact]
        public void Using()
        {
            var tree = UsingTree(@"
using Goo;
using Goo.Bar;
using Goo = Bar;
using (var x = bar) { }
");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.NameEquals);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.EqualsToken);
                    }
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.UsingStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken);
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken);
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        #endregion

        #region Unsafe

        [Fact]
        public void Unsafe_Block()
        {
            var tree = UsingTree(@"
unsafe { }
");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.UnsafeStatement);
                    {
                        N(SyntaxKind.UnsafeKeyword);
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void Unsafe_Field()
        {
            var tree = UsingTree(@"
unsafe int Goo;
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.UnsafeKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void Unsafe_Method()
        {
            var tree = UsingTree(@"
unsafe void Goo() { }
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.UnsafeKeyword);
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
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void Unsafe_Property()
        {
            var tree = UsingTree(@"
unsafe int Goo { get; }
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.UnsafeKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        /// bug="3784" project = "Roslyn"
        [Fact]
        public void PointerDeclaration()
        {
            var test = @"
unsafe Idf * Idf;
";
            ParseAndValidate(test);
        }

        #endregion

        #region Fixed

        [Fact]
        public void Fixed()
        {
            var tree = UsingTree(@"
fixed (int* a = b) { }
fixed int x[5];
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
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
                                N(SyntaxKind.IdentifierToken);
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken);
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
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.FixedKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.BracketedArgumentList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken);
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        #endregion

        #region Delegate

        [Fact]
        public void Delegate1()
        {
            var tree = UsingTree(@"
delegate { }();
");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.AnonymousMethodExpression);
                            {
                                N(SyntaxKind.DelegateKeyword);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void Delegate2()
        {
            var tree = UsingTree(@"
delegate(){ }();
");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.AnonymousMethodExpression);
                            {
                                N(SyntaxKind.DelegateKeyword);
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
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void Delegate3()
        {
            var tree = UsingTree(@"
delegate void Goo();
");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.DelegateDeclaration);
                {
                    N(SyntaxKind.DelegateKeyword);
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
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        #endregion

        #region Indexer

        [Fact]
        public void Indexer1()
        {
            var tree = UsingTree(@"
bool this[int index]{} 
");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IndexerDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.BoolKeyword);
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
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void Indexer2()
        {
            var tree = UsingTree(@"
public partial bool this[int index] {}
");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IndexerDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.BoolKeyword);
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
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void Indexer4()
        {
            var tree = UsingTree(@"
new public bool this[int index] { get; }
");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IndexerDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.BoolKeyword);
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
                            N(SyntaxKind.IdentifierToken, "index");
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Indexer5()
        {
            var tree = UsingTree(@"
new public bool this[int index] { get; }
");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IndexerDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.BoolKeyword);
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
                            N(SyntaxKind.IdentifierToken, "index");
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Error_IndexerDefinition()
        {
            var test = @"string this ="""";";
            ParseAndValidate(test,
                new ErrorDescription { Code = 1001, Line = 1, Column = 13 },
                new ErrorDescription { Code = 1003, Line = 1, Column = 13 },
                new ErrorDescription { Code = 1003, Line = 1, Column = 16 },
                new ErrorDescription { Code = 1514, Line = 1, Column = 16 },
                new ErrorDescription { Code = 1014, Line = 1, Column = 16 },
                new ErrorDescription { Code = 1513, Line = 1, Column = 17 });

            CreateCompilation(test).VerifyDiagnostics(
                // (1,13): error CS1003: Syntax error, '[' expected
                // string this ="";
                Diagnostic(ErrorCode.ERR_SyntaxError, "=").WithArguments("[").WithLocation(1, 13),
                // (1,13): error CS1001: Identifier expected
                // string this ="";
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "=").WithLocation(1, 13),
                // (1,16): error CS1003: Syntax error, ']' expected
                // string this ="";
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]").WithLocation(1, 16),
                // (1,16): error CS1514: { expected
                // string this ="";
                Diagnostic(ErrorCode.ERR_LbraceExpected, ";").WithLocation(1, 16),
                // (1,16): error CS1014: A get or set accessor expected
                // string this ="";
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, ";").WithLocation(1, 16),
                // (1,17): error CS1513: } expected
                // string this ="";
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 17),
                // (1,8): error CS0548: '<invalid-global-code>.this': property or indexer must have at least one accessor
                // string this ="";
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "this").WithArguments("<invalid-global-code>.this").WithLocation(1, 8),
                // error CS1551: Indexers must have at least one parameter
                Diagnostic(ErrorCode.ERR_IndexerNeedsParam).WithLocation(1, 1));
        }

        #endregion

        #region Extern

        [Fact]
        public void ExternAlias()
        {
            var tree = UsingTree(@"
extern alias Goo;
extern alias Goo();
extern alias Goo { get; }
extern alias Goo<T> { get; }
",
                // (5,14): error CS7002: Unexpected use of a generic name
                // extern alias Goo<T> { get; }
                Diagnostic(ErrorCode.ERR_UnexpectedGenericName, "Goo").WithLocation(5, 14));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ExternAliasDirective);
                {
                    N(SyntaxKind.ExternKeyword);
                    N(SyntaxKind.AliasKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.ExternKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.ExternKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
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
                    N(SyntaxKind.ExternKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        #endregion

        #region Ordering

        [Fact]
        public void Delegate()
        {
            var test = @"
delegate { }
delegate() { }
delegate void Goo();
delegate void MyDel(int i);
";
            ParseAndValidate(test,
                new ErrorDescription { Code = (int)ErrorCode.ERR_SemicolonExpected, Line = 2, Column = 13 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_SemicolonExpected, Line = 3, Column = 15 });
        }

        [Fact]
        public void ExternAliasAmbiguity()
        {
            var test = @"
extern alias Goo;
extern alias Goo();
extern alias Goo { get; }
extern alias Goo<T> { get; }
";
            ParseAndValidate(test, new ErrorDescription { Code = 7002, Line = 5, Column = 14 });
        }

        [Fact]
        public void ExternOrdering_Statement()
        {
            var test = @"
using(var x = 1) { }
extern alias Goo;
";
            ParseAndValidate(test, new ErrorDescription { Code = 439, Line = 3, Column = 1 });
        }

        [Fact]
        public void ExternOrdering_Method()
        {
            var test = @"
extern void goo();
extern alias Goo;
";
            ParseAndValidate(test, new ErrorDescription { Code = 439, Line = 3, Column = 1 });
        }

        [Fact]
        public void ExternOrdering_Field()
        {
            var test = @"
int a = 1;
extern alias Goo;
";
            ParseAndValidate(test, new ErrorDescription { Code = 439, Line = 3, Column = 1 });
        }

        [Fact]
        public void ExternOrdering_Property()
        {
            var test = @"
extern alias Goo { get; }
extern alias Goo;
";

            ParseAndValidate(test, new ErrorDescription { Code = 439, Line = 3, Column = 1 });
        }

        [Fact]
        public void UsingOrdering_Statement()
        {
            var test = @"
using(var x = 1) { }
using Goo;
";
            ParseAndValidate(test, new ErrorDescription { Code = 1529, Line = 3, Column = 1 });
        }

        [Fact]
        public void UsingOrdering_Member()
        {
            var test = @"
void goo() { }
using Goo;
";
            ParseAndValidate(test, new ErrorDescription { Code = 1529, Line = 3, Column = 1 });
        }

        #endregion

        #region Partial

        [Fact]
        public void PartialMethod()
        {
            var tree = UsingTree(@"
partial void Goo();
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
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
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            var test = @"
new public bool this[int index] 
 {
     get { return true; }
 }
";
            ParseAndValidate(test);
        }

        /// bug="3778" project = "Roslyn"
        [Fact]
        public void PartialMethodDefinition()
        {
            var test = @"
 partial void Goo();
";
            ParseAndValidate(test);
        }

        /// bug="3780" project = "Roslyn"
        [Fact]
        public void UsingNewModifierWithPartialMethodDefinition()
        {
            var test = @"
new partial void Goo();
";
            ParseAndValidate(test);
        }

        [Fact]
        public void ImplementingDeclarationOfPartialMethod()
        {
            var test = @"
partial void Goo(){};
";
            ParseAndValidate(test, new ErrorDescription { Code = 1597, Line = 2, Column = 21 });
        }

        [Fact]
        public void EnumDeclaration()
        {
            var test = @"
partial enum @en {};
";
            CreateCompilation(test).VerifyDiagnostics(
                // (2,14): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                // partial enum @en {};
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "@en").WithLocation(2, 14));
        }

        [Fact]
        public void UsingPartial()
        {
            var src = """
partial = partial;

partial partial;
partial partial = partial;

partial Goo { get; }
partial partial Goo { get; } 
partial partial[] Goo { get; } 
partial partial<int> Goo { get; } 

partial Goo() { } 
partial partial Goo() { } 
partial partial[] Goo() { } 
partial partial<int> Goo() { }
""";
            var tree = UsingTree(src, options: TestOptions.Regular13);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "partial");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "partial");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "partial");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "partial");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "partial");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "partial");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "partial");
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "partial");
                    }
                    N(SyntaxKind.IdentifierToken, "Goo");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
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
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "partial");
                    }
                    N(SyntaxKind.IdentifierToken, "Goo");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
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
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
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
                    N(SyntaxKind.IdentifierToken, "Goo");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
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
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "partial");
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
                    N(SyntaxKind.IdentifierToken, "Goo");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
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
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "partial");
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
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
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
                    N(SyntaxKind.IdentifierToken, "Goo");
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
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "partial");
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
                    N(SyntaxKind.IdentifierToken, "Goo");
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            tree = UsingTree(src,
                // (11,9): error CS1520: Method must have a return type
                // partial Goo() { } 
                Diagnostic(ErrorCode.ERR_MemberNeedsType, "Goo").WithLocation(11, 9),
                // (12,9): error CS1525: Invalid expression term 'partial'
                // partial partial Goo() { } 
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(12, 9),
                // (12,9): error CS1003: Syntax error, ',' expected
                // partial partial Goo() { } 
                Diagnostic(ErrorCode.ERR_SyntaxError, "partial").WithArguments(",").WithLocation(12, 9),
                // (12,17): error CS1002: ; expected
                // partial partial Goo() { } 
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "Goo").WithLocation(12, 17),
                // (12,23): error CS1002: ; expected
                // partial partial Goo() { } 
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(12, 23));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "partial");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "partial");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
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
                            N(SyntaxKind.IdentifierToken, "partial");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
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
                            N(SyntaxKind.IdentifierToken, "partial");
                            N(SyntaxKind.EqualsValueClause);
                            {
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "partial");
                                }
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "partial");
                    }
                    N(SyntaxKind.IdentifierToken, "Goo");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
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
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "partial");
                    }
                    N(SyntaxKind.IdentifierToken, "Goo");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
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
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
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
                    N(SyntaxKind.IdentifierToken, "Goo");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
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
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "partial");
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
                    N(SyntaxKind.IdentifierToken, "Goo");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    M(SyntaxKind.PredefinedType);
                    {
                        M(SyntaxKind.VoidKeyword);
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
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
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
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Goo");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
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
                    N(SyntaxKind.IdentifierToken, "Goo");
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
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "partial");
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
                    N(SyntaxKind.IdentifierToken, "Goo");
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        #endregion

        #region Attributes

        [Fact]
        public void Attributes()
        {
            var tree = UsingTree(@"
[assembly: Goo]
[module: Bar]
[Goo]
void goo() { }
[Bar]
int x;
[Baz]
class C { }
[Baz]
struct C { }
[Baz]
enum C { }
[Baz]
delegate D();
",
                // (15,11): error CS1001: Identifier expected
                // delegate D();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(15, 11));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.AttributeList);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.AttributeTargetSpecifier);
                    {
                        N(SyntaxKind.AssemblyKeyword);
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.Attribute);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
                N(SyntaxKind.AttributeList);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.AttributeTargetSpecifier);
                    {
                        N(SyntaxKind.ModuleKeyword);
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.Attribute);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
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
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EnumDeclaration);
                {
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.EnumKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.DelegateDeclaration);
                {
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        #endregion

        #region Fields

        [Fact]
        public void Fields()
        {
            var tree = UsingTree(@"
int x;
volatile int x;
readonly int x;
static int x;
fixed int x[10];
");

            N(SyntaxKind.CompilationUnit);
            {
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
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VolatileKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
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
                    N(SyntaxKind.ReadOnlyKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
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
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
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
                    N(SyntaxKind.FixedKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.BracketedArgumentList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken);
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        #endregion

        #region Multiplication

        [Fact]
        public void Multiplication()
        {
            // pointer decl
            string test = @"a.b * c;";
            ParseAndValidate(test, TestOptions.Regular9);

            // pointer decl
            test = @"a.b * c";
            ParseAndValidate(test, TestOptions.Regular9, new[] { new ErrorDescription { Code = (int)ErrorCode.ERR_SemicolonExpected, Line = 1, Column = 8 } }); // expected ';'

            // multiplication
            test = @"a.b * c;";
            ParseAndValidate(test, TestOptions.Script);

            // multiplication
            test = @"a.b * c;";
            ParseAndValidate(test, TestOptions.Script);

            // multiplication
            test = @"a.b * c";
            ParseAndValidate(test, TestOptions.Script);
        }

        [Fact]
        public void Multiplication_Interactive_Semicolon()
        {
            var tree = UsingTree(@"a * b;", TestOptions.Script);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.MultiplyExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void Multiplication_Interactive_NoSemicolon()
        {
            var tree = UsingTree(@"a * b", TestOptions.Script);

            Assert.False(tree.GetCompilationUnitRoot().ContainsDiagnostics);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.MultiplyExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Multiplication_Complex()
        {
            var tree = UsingTree(@"a<t>.n * f(x)", TestOptions.Script);
            Assert.False(tree.GetCompilationUnitRoot().ContainsDiagnostics);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.MultiplyExpression);
                        {
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "t");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "n");
                                }
                            }
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "f");
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
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        #endregion

        #region Ternary Operator

        // T is a type name, including:
        // a<b>
        // a<b>.c
        // a[]
        // a[,,]
        // a<b>.b[]
        // a<b,c>.d[]
        // etc.
        //
        // Ts is a comma separated list of type names

        // field decls:
        // T ? idf;
        // T ? idf, ... 
        // T ? idf = <expr>, ...
        // T ? idf = <expr>;

        // property decls:
        // T ? idf { ...
        // T ? idf<Ts> { ...
        // T ? idf<Ts>. ... { ...

        // method decls:
        // T ? idf() where ...
        // T ? idf() { ...
        // T ? idf(T idf ...            
        // T ? idf.idf(T idf ...            
        // T ? idf<Ts>(T idf ...
        // T ? idf<Ts>.idf<Ts>. ...(T idf ...
        // T ? idf([Attr]T idf ...
        // T ? idf([Attr]T ? idf ...
        // T ? idf(out T ? idf ...
        // T ? idf(T ? idf, ...
        // T ? idf(this idf ...
        // T ? idf(params ...
        // T ? idf(__arglist ...

        // expressions:
        // T ? non-idf
        // T ? idf
        // T ? idf. ...
        // T ? idf[ ...
        // T ? idf<
        // T ? idf<T
        // T ? idf<Ts>
        // T ? idf<Ts>.
        // T ? idf<Ts>. ... (
        // T ? idf(                
        // T ? idf(a               
        // T ? idf(a)
        // T ? idf(this
        // T ? idf(this = ...
        // T ? idf(this[ ... 
        // T ? idf(this. ... 
        // T ? idf(this< ... 
        // T ? idf(this( ... 
        // T ? idf(ref a)
        // T ? idf()
        // T ? idf();              // method without body must be abstract, which is probably not what user intended to write in interactive
        // T ? idf(T ? idf
        // T ? idf(x: 1, y: 2) : c(z: 3)
        // T ? idf => { } : c => { }
        // T ? idf => (d ? e => 1 : f => 2)(3) : c => 2
        // T ? idf = <expr>
        // T ? b = x ? y : z : w

        // fields //

        [Fact]
        public void Ternary_FieldDecl_Semicolon1()
        {
            var tree = UsingTree(@"T ? a;", TestOptions.Script);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.NullableType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.QuestionToken);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void Ternary_FieldDecl_Semicolon2()
        {
            var tree = UsingTree(@"T ? b, c = 1;", TestOptions.Script);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.NullableType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.QuestionToken);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.EqualsValueClause);
                            {
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken);
                                }
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void Ternary_FieldDecl_Semicolon3()
        {
            var tree = UsingTree(@"T ? b = d => { };", TestOptions.Script);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.NullableType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.QuestionToken);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.EqualsValueClause);
                            {
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.SimpleLambdaExpression);
                                {
                                    N(SyntaxKind.Parameter);
                                    {
                                        N(SyntaxKind.IdentifierToken);
                                    }
                                    N(SyntaxKind.EqualsGreaterThanToken);
                                    N(SyntaxKind.Block);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.CloseBraceToken);
                                    }
                                }
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void Ternary_FieldDecl_Semicolon4()
        {
            var tree = UsingTree(@"T ? b = x ? y : z;", TestOptions.Script);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.NullableType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.QuestionToken);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.EqualsValueClause);
                            {
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.ConditionalExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken);
                                    }
                                    N(SyntaxKind.QuestionToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken);
                                    }
                                    N(SyntaxKind.ColonToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken);
                                    }
                                }
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void Ternary_FieldDecl_Comma1()
        {
            var tree = UsingTree(@"T ? a,", TestOptions.Script,
                // (1,7): error CS1001: Identifier expected
                // T ? a,
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 7),
                // (1,7): error CS1002: ; expected
                // T ? a,
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 7));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.NullableType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        N(SyntaxKind.CommaToken);
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_FieldDecl_Comma2()
        {
            var tree = UsingTree(@"T ? a = 1,", TestOptions.Script,
                // (1,11): error CS1001: Identifier expected
                // T ? a = 1,
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 11),
                // (1,11): error CS1002: ; expected
                // T ? a = 1,
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 11));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.NullableType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
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
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        // properties //

        [Fact]
        public void Ternary_PropertyDecl1()
        {
            var tree = UsingTree(@"T ? a {", TestOptions.Script,
                // (1,8): error CS1513: } expected
                // T ? a {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 8));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_PropertyDecl2()
        {
            var tree = UsingTree(@"T ? a.b {", TestOptions.Script,
                // (1,10): error CS1513: } expected
                // T ? a.b {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 10));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.IdentifierToken, "b");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_PropertyDecl3()
        {
            var tree = UsingTree(@"T ? a<T>.b {", TestOptions.Script,
                // (1,13): error CS1513: } expected
                // T ? a<T>.b {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 13));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
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
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.IdentifierToken, "b");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_PropertyDecl4()
        {
            var tree = UsingTree(@"T ? a<T?>.b<S>.c {", TestOptions.Script,
                // (1,19): error CS1513: } expected
                // T ? a<T?>.b<S>.c {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 19));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.NullableType);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "T");
                                        }
                                        N(SyntaxKind.QuestionToken);
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
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
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.IdentifierToken, "c");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        // methods //

        [Fact]
        public void Ternary_MethodDecl1()
        {
            var tree = UsingTree(@"T ? a() {", TestOptions.Script,
                // (1,10): error CS1513: } expected
                // T ? a() {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 10));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl1_Where()
        {
            var tree = UsingTree(@"T ? a() where", TestOptions.Script,
                // (1,14): error CS1001: Identifier expected
                // T ? a() where
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 14),
                // (1,14): error CS1003: Syntax error, ':' expected
                // T ? a() where
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 14),
                // (1,14): error CS1031: Type expected
                // T ? a() where
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(1, 14),
                // (1,14): error CS1002: ; expected
                // T ? a() where
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 14));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.ColonToken);
                        M(SyntaxKind.TypeConstraint);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl2()
        {
            var tree = UsingTree(@"T ? a(T b", TestOptions.Script,
                // (1,10): error CS1026: ) expected
                // T ? a(T b
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 10),
                // (1,10): error CS1002: ; expected
                // T ? a(T b
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 10));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl3()
        {
            var tree = UsingTree(@"T ? a.b(T c", TestOptions.Script,
                // (1,12): error CS1026: ) expected
                // T ? a.b(T c
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 12),
                // (1,12): error CS1002: ; expected
                // T ? a.b(T c
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 12));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.IdentifierToken, "b");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl4()
        {
            var tree = UsingTree(@"T ? a<A>.b<B>(C c", TestOptions.Script,
                // (1,18): error CS1026: ) expected
                // T ? a<A>.b<B>(C c
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 18),
                // (1,18): error CS1002: ; expected
                // T ? a<A>.b<B>(C c
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 18));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "A");
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.IdentifierToken, "b");
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
                            }
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl5()
        {
            var tree = UsingTree(@"T ? a([Attr]C c", TestOptions.Script,
                // (1,16): error CS1026: ) expected
                // T ? a([Attr]C c
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 16),
                // (1,16): error CS1002: ; expected
                // T ? a([Attr]C c
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 16));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
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
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
                            }
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl6()
        {
            var tree = UsingTree(@"T ? a([Attr(a = b)]c", TestOptions.Script,
                // (1,21): error CS1001: Identifier expected
                // T ? a([Attr(a = b)]c
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 21),
                // (1,21): error CS1026: ) expected
                // T ? a([Attr(a = b)]c
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 21),
                // (1,21): error CS1002: ; expected
                // T ? a([Attr(a = b)]c
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 21));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
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
                                        N(SyntaxKind.AttributeArgument);
                                        {
                                            N(SyntaxKind.NameEquals);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "a");
                                                }
                                                N(SyntaxKind.EqualsToken);
                                            }
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "b");
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl7()
        {
            var tree = UsingTree(@"T ? a(out C c", TestOptions.Script,
                // (1,14): error CS1026: ) expected
                // T ? a(out C c
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 14),
                // (1,14): error CS1002: ; expected
                // T ? a(out C c
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 14));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.OutKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
                            }
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl8()
        {
            var tree = UsingTree(@"T ? a(C[] a", TestOptions.Script,
                // (1,12): error CS1026: ) expected
                // T ? a(C[] a
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 12),
                // (1,12): error CS1002: ; expected
                // T ? a(C[] a
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 12));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
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
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl9()
        {
            var tree = UsingTree(@"T ? a(params", TestOptions.Script,
                // (1,13): error CS1031: Type expected
                // T ? a(params
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(1, 13),
                // (1,13): error CS1001: Identifier expected
                // T ? a(params
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 13),
                // (1,13): error CS1026: ) expected
                // T ? a(params
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 13),
                // (1,13): error CS1002: ; expected
                // T ? a(params
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 13));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.ParamsKeyword);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl10()
        {
            var tree = UsingTree(@"T ? a(out T ? b", TestOptions.Script,
                // (1,16): error CS1026: ) expected
                // T ? a(out T ? b
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 16),
                // (1,16): error CS1002: ; expected
                // T ? a(out T ? b
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 16));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.OutKeyword);
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "T");
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl11()
        {
            var tree = UsingTree(@"T ? a(ref T ? b", TestOptions.Script,
                // (1,16): error CS1026: ) expected
                // T ? a(ref T ? b
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 16),
                // (1,16): error CS1002: ; expected
                // T ? a(ref T ? b
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 16));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "T");
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl12()
        {
            var tree = UsingTree(@"T ? a(params T ? b", TestOptions.Script,
                // (1,19): error CS1026: ) expected
                // T ? a(params T ? b
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 19),
                // (1,19): error CS1002: ; expected
                // T ? a(params T ? b
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 19));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.ParamsKeyword);
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "T");
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl13()
        {
            var tree = UsingTree(@"T ? a([Attr]T ? b", TestOptions.Script,
                // (1,18): error CS1026: ) expected
                // T ? a([Attr]T ? b
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 18),
                // (1,18): error CS1002: ; expected
                // T ? a([Attr]T ? b
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 18));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
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
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "T");
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl14A()
        {
            var tree = UsingTree(@"T ? a(T ? b,", TestOptions.Script,
                // (1,13): error CS1031: Type expected
                // T ? a(T ? b,
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(1, 13),
                // (1,13): error CS1001: Identifier expected
                // T ? a(T ? b,
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 13),
                // (1,13): error CS1026: ) expected
                // T ? a(T ? b,
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 13),
                // (1,13): error CS1002: ; expected
                // T ? a(T ? b,
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 13));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "T");
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                        N(SyntaxKind.CommaToken);
                        M(SyntaxKind.Parameter);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl14B()
        {
            var tree = UsingTree(@"T ? a(T ? b)", TestOptions.Script,
                // (1,13): error CS1002: ; expected
                // T ? a(T ? b)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 13));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "T");
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl15()
        {
            var tree = UsingTree(@"T ? a(T c)", TestOptions.Script,
                // (1,11): error CS1002: ; expected
                // T ? a(T c)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 11));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl16()
        {
            var tree = UsingTree(@"T ? a(this c d", TestOptions.Script,
                // (1,15): error CS1026: ) expected
                // T ? a(this c d
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 15),
                // (1,15): error CS1002: ; expected
                // T ? a(this c d
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 15));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.ThisKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                            N(SyntaxKind.IdentifierToken, "d");
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl17()
        {
            var tree = UsingTree(@"T ? a(ref out T a", TestOptions.Script,
                // (1,18): error CS1026: ) expected
                // T ? a(ref out T a
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 18),
                // (1,18): error CS1002: ; expected
                // T ? a(ref out T a
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 18));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.OutKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl18()
        {
            var tree = UsingTree(@"T ? a(int a", TestOptions.Script,
                // (1,12): error CS1026: ) expected
                // T ? a(int a
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 12),
                // (1,12): error CS1002: ; expected
                // T ? a(int a
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 12));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl19()
        {
            var tree = UsingTree(@"T ? a(ref int a", TestOptions.Script,
                // (1,16): error CS1026: ) expected
                // T ? a(ref int a
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 16),
                // (1,16): error CS1002: ; expected
                // T ? a(ref int a
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 16));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl20()
        {
            var tree = UsingTree(@"T ? a(T a =", TestOptions.Script,
                // (1,12): error CS1733: Expected expression
                // T ? a(T a =
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 12),
                // (1,12): error CS1026: ) expected
                // T ? a(T a =
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 12),
                // (1,12): error CS1002: ; expected
                // T ? a(T a =
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 12));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.IdentifierToken, "a");
                            N(SyntaxKind.EqualsValueClause);
                            {
                                N(SyntaxKind.EqualsToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl21()
        {
            var tree = UsingTree(@"T ? a(T[,] a", TestOptions.Script,
                // (1,13): error CS1026: ) expected
                // T ? a(T[,] a
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 13),
                // (1,13): error CS1002: ; expected
                // T ? a(T[,] a
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 13));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "T");
                                }
                                N(SyntaxKind.ArrayRankSpecifier);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.OmittedArraySizeExpression);
                                    {
                                        N(SyntaxKind.OmittedArraySizeExpressionToken);
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.OmittedArraySizeExpression);
                                    {
                                        N(SyntaxKind.OmittedArraySizeExpressionToken);
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl22()
        {
            var tree = UsingTree(@"T ? a(T?[10] a)",
                // (1,16): error CS1002: ; expected
                // T ? a(T?[10] a)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 16));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.NullableType);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "T");
                                    }
                                    N(SyntaxKind.QuestionToken);
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
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        /// <summary>
        /// Prefer method declaration over an expression.
        /// </summary>
        [Fact]
        public void Ternary_MethodDecl_GenericAmbiguity1()
        {
            var tree = UsingTree(@"T ? m(a < b, c > d)", TestOptions.Script,
                // (1,20): error CS1002: ; expected
                // T ? m(a < b, c > d)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 20));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "m");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "b");
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "c");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.IdentifierToken, "d");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        // expressions //

        [Fact]
        public void Ternary_Expression1()
        {
            var tree = UsingTree(@"T ? 1", TestOptions.Script,
                // (1,6): error CS1003: Syntax error, ':' expected
                // T ? 1
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 6),
                // (1,6): error CS1733: Expected expression
                // T ? 1
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 6));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "1");
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression2()
        {
            var tree = UsingTree(@"T ? a", TestOptions.Script,
                // (1,6): error CS1003: Syntax error, ':' expected
                // T ? a
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 6),
                // (1,6): error CS1733: Expected expression
                // T ? a
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 6));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression3()
        {
            var tree = UsingTree(@"T ? a.", TestOptions.Script,
                // (1,7): error CS1001: Identifier expected
                // T ? a.
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 7),
                // (1,7): error CS1003: Syntax error, ':' expected
                // T ? a.
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 7),
                // (1,7): error CS1733: Expected expression
                // T ? a.
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 7));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.DotToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression4()
        {
            var tree = UsingTree(@"T ? a[", TestOptions.Script,
                // (1,7): error CS1003: Syntax error, ']' expected
                // T ? a[
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("]").WithLocation(1, 7),
                // (1,7): error CS1003: Syntax error, ':' expected
                // T ? a[
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 7),
                // (1,7): error CS1733: Expected expression
                // T ? a[
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 7));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.ElementAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.BracketedArgumentList);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    M(SyntaxKind.CloseBracketToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression5()
        {
            var tree = UsingTree(@"T ? a<", TestOptions.Script,
                // (1,7): error CS1733: Expected expression
                // T ? a<
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 7),
                // (1,7): error CS1003: Syntax error, ':' expected
                // T ? a<
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 7),
                // (1,7): error CS1733: Expected expression
                // T ? a<
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 7));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.LessThanExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.LessThanToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression6()
        {
            var tree = UsingTree(@"T ? a<b", TestOptions.Script,
                // (1,8): error CS1003: Syntax error, ':' expected
                // T ? a<b
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 8),
                // (1,8): error CS1733: Expected expression
                // T ? a<b
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 8));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.LessThanExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression7()
        {
            var tree = UsingTree(@"T ? a<b>", TestOptions.Script,
                // (1,9): error CS1003: Syntax error, ':' expected
                // T ? a<b>
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 9),
                // (1,9): error CS1733: Expected expression
                // T ? a<b>
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 9));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "b");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression8()
        {
            var tree = UsingTree(@"T ? a<b,c>", TestOptions.Script,
                // (1,11): error CS1003: Syntax error, ':' expected
                // T ? a<b,c>
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 11),
                // (1,11): error CS1733: Expected expression
                // T ? a<b,c>
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 11));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "b");
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "c");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression9()
        {
            var tree = UsingTree(@"T ? a<b>.", TestOptions.Script,
                // (1,10): error CS1001: Identifier expected
                // T ? a<b>.
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 10),
                // (1,10): error CS1003: Syntax error, ':' expected
                // T ? a<b>.
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 10),
                // (1,10): error CS1733: Expected expression
                // T ? a<b>.
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 10));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                                N(SyntaxKind.DotToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression10()
        {
            var tree = UsingTree(@"T ? a<b>.c", TestOptions.Script,
                // (1,11): error CS1003: Syntax error, ':' expected
                // T ? a<b>.c
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 11),
                // (1,11): error CS1733: Expected expression
                // T ? a<b>.c
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 11));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "c");
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression11()
        {
            var tree = UsingTree(@"T ? a<b>.c(", TestOptions.Script,
                // (1,12): error CS1026: ) expected
                // T ? a<b>.c(
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 12),
                // (1,12): error CS1003: Syntax error, ':' expected
                // T ? a<b>.c(
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 12),
                // (1,12): error CS1733: Expected expression
                // T ? a<b>.c(
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 12));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "b");
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "c");
                                    }
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression12()
        {
            var tree = UsingTree(@"T ? a(", TestOptions.Script,
                // (1,7): error CS1026: ) expected
                // T ? a(
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 7),
                // (1,7): error CS1003: Syntax error, ':' expected
                // T ? a(
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 7),
                // (1,7): error CS1733: Expected expression
                // T ? a(
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 7));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression13()
        {
            var tree = UsingTree(@"T ? a.b(", TestOptions.Script,
                // (1,9): error CS1026: ) expected
                // T ? a.b(
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 9),
                // (1,9): error CS1003: Syntax error, ':' expected
                // T ? a.b(
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 9),
                // (1,9): error CS1733: Expected expression
                // T ? a.b(
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 9));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "b");
                                    }
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression14()
        {
            var tree = UsingTree(@"T ? m(c", TestOptions.Script,
                // (1,8): error CS1026: ) expected
                // T ? m(c
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 8),
                // (1,8): error CS1003: Syntax error, ':' expected
                // T ? m(c
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 8),
                // (1,8): error CS1733: Expected expression
                // T ? m(c
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 8));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "c");
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression15()
        {
            var tree = UsingTree(@"T ? m(c,", TestOptions.Script,
                // (1,9): error CS1733: Expected expression
                // T ? m(c,
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 9),
                // (1,9): error CS1026: ) expected
                // T ? m(c,
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 9),
                // (1,9): error CS1003: Syntax error, ':' expected
                // T ? m(c,
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 9),
                // (1,9): error CS1733: Expected expression
                // T ? m(c,
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 9));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "c");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    M(SyntaxKind.Argument);
                                    {
                                        M(SyntaxKind.IdentifierName);
                                        {
                                            M(SyntaxKind.IdentifierToken);
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression16()
        {
            var tree = UsingTree(@"T ? m(c:", TestOptions.Script,
                // (1,9): error CS1733: Expected expression
                // T ? m(c:
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 9),
                // (1,9): error CS1026: ) expected
                // T ? m(c:
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 9),
                // (1,9): error CS1003: Syntax error, ':' expected
                // T ? m(c:
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 9),
                // (1,9): error CS1733: Expected expression
                // T ? m(c:
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 9));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
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
                                                N(SyntaxKind.IdentifierToken, "c");
                                            }
                                            N(SyntaxKind.ColonToken);
                                        }
                                        M(SyntaxKind.IdentifierName);
                                        {
                                            M(SyntaxKind.IdentifierToken);
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression17()
        {
            var tree = UsingTree(@"T ? m(c?", TestOptions.Script,
                // (1,9): error CS1733: Expected expression
                // T ? m(c?
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 9),
                // (1,9): error CS1003: Syntax error, ':' expected
                // T ? m(c?
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 9),
                // (1,9): error CS1733: Expected expression
                // T ? m(c?
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 9),
                // (1,9): error CS1026: ) expected
                // T ? m(c?
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 9),
                // (1,9): error CS1003: Syntax error, ':' expected
                // T ? m(c?
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 9),
                // (1,9): error CS1733: Expected expression
                // T ? m(c?
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 9));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.ConditionalExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "c");
                                            }
                                            N(SyntaxKind.QuestionToken);
                                            M(SyntaxKind.IdentifierName);
                                            {
                                                M(SyntaxKind.IdentifierToken);
                                            }
                                            M(SyntaxKind.ColonToken);
                                            M(SyntaxKind.IdentifierName);
                                            {
                                                M(SyntaxKind.IdentifierToken);
                                            }
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression18()
        {
            var tree = UsingTree(@"T ? m(c? a", TestOptions.Script,
                // (1,11): error CS1003: Syntax error, ':' expected
                // T ? m(c? a
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 11),
                // (1,11): error CS1733: Expected expression
                // T ? m(c? a
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 11),
                // (1,11): error CS1026: ) expected
                // T ? m(c? a
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 11),
                // (1,11): error CS1003: Syntax error, ':' expected
                // T ? m(c? a
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 11),
                // (1,11): error CS1733: Expected expression
                // T ? m(c? a
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 11));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.ConditionalExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "c");
                                            }
                                            N(SyntaxKind.QuestionToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "a");
                                            }
                                            M(SyntaxKind.ColonToken);
                                            M(SyntaxKind.IdentifierName);
                                            {
                                                M(SyntaxKind.IdentifierToken);
                                            }
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression19()
        {
            var tree = UsingTree(@"T ? m(c? a =", TestOptions.Script,
                // (1,13): error CS1733: Expected expression
                // T ? m(c? a =
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 13),
                // (1,13): error CS1003: Syntax error, ':' expected
                // T ? m(c? a =
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 13),
                // (1,13): error CS1733: Expected expression
                // T ? m(c? a =
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 13),
                // (1,13): error CS1026: ) expected
                // T ? m(c? a =
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 13),
                // (1,13): error CS1003: Syntax error, ':' expected
                // T ? m(c? a =
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 13),
                // (1,13): error CS1733: Expected expression
                // T ? m(c? a =
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 13));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.ConditionalExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "c");
                                            }
                                            N(SyntaxKind.QuestionToken);
                                            N(SyntaxKind.SimpleAssignmentExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "a");
                                                }
                                                N(SyntaxKind.EqualsToken);
                                                M(SyntaxKind.IdentifierName);
                                                {
                                                    M(SyntaxKind.IdentifierToken);
                                                }
                                            }
                                            M(SyntaxKind.ColonToken);
                                            M(SyntaxKind.IdentifierName);
                                            {
                                                M(SyntaxKind.IdentifierToken);
                                            }
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression20()
        {
            var tree = UsingTree(@"T ? m(c? a = b ?", TestOptions.Script,
                // (1,17): error CS1733: Expected expression
                // T ? m(c? a = b ?
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 17),
                // (1,17): error CS1003: Syntax error, ':' expected
                // T ? m(c? a = b ?
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 17),
                // (1,17): error CS1733: Expected expression
                // T ? m(c? a = b ?
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 17),
                // (1,17): error CS1003: Syntax error, ':' expected
                // T ? m(c? a = b ?
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 17),
                // (1,17): error CS1733: Expected expression
                // T ? m(c? a = b ?
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 17),
                // (1,17): error CS1026: ) expected
                // T ? m(c? a = b ?
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 17),
                // (1,17): error CS1003: Syntax error, ':' expected
                // T ? m(c? a = b ?
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 17),
                // (1,17): error CS1733: Expected expression
                // T ? m(c? a = b ?
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 17));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.ConditionalExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "c");
                                            }
                                            N(SyntaxKind.QuestionToken);
                                            N(SyntaxKind.SimpleAssignmentExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "a");
                                                }
                                                N(SyntaxKind.EqualsToken);
                                                N(SyntaxKind.ConditionalExpression);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "b");
                                                    }
                                                    N(SyntaxKind.QuestionToken);
                                                    M(SyntaxKind.IdentifierName);
                                                    {
                                                        M(SyntaxKind.IdentifierToken);
                                                    }
                                                    M(SyntaxKind.ColonToken);
                                                    M(SyntaxKind.IdentifierName);
                                                    {
                                                        M(SyntaxKind.IdentifierToken);
                                                    }
                                                }
                                            }
                                            M(SyntaxKind.ColonToken);
                                            M(SyntaxKind.IdentifierName);
                                            {
                                                M(SyntaxKind.IdentifierToken);
                                            }
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression21()
        {
            var tree = UsingTree(@"T ? m()", TestOptions.Script,
                // (1,8): error CS1003: Syntax error, ':' expected
                // T ? m()
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 8),
                // (1,8): error CS1733: Expected expression
                // T ? m()
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 8));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression22()
        {
            var tree = UsingTree(@"T ? m(a)", TestOptions.Script,
                // (1,9): error CS1003: Syntax error, ':' expected
                // T ? m(a)
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 9),
                // (1,9): error CS1733: Expected expression
                // T ? m(a)
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 9));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression23()
        {
            var tree = UsingTree(@"T ? m();", TestOptions.Script,
                // (1,8): error CS1003: Syntax error, ':' expected
                // T ? m();
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(":").WithLocation(1, 8),
                // (1,8): error CS1525: Invalid expression term ';'
                // T ? m();
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(1, 8));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression24()
        {
            var tree = UsingTree(@"T ? m(a);", TestOptions.Script,
                // (1,9): error CS1003: Syntax error, ':' expected
                // T ? m(a);
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(":").WithLocation(1, 9),
                // (1,9): error CS1525: Invalid expression term ';'
                // T ? m(a);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(1, 9));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression25()
        {
            var tree = UsingTree(@"T ? m(x: 1", TestOptions.Script,
                // (1,11): error CS1026: ) expected
                // T ? m(x: 1
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 11),
                // (1,11): error CS1003: Syntax error, ':' expected
                // T ? m(x: 1
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 11),
                // (1,11): error CS1733: Expected expression
                // T ? m(x: 1
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 11));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
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
                                                N(SyntaxKind.IdentifierToken, "x");
                                            }
                                            N(SyntaxKind.ColonToken);
                                        }
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "1");
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression26()
        {
            var tree = UsingTree(@"T ? m(x: 1, y: a ? b : c)", TestOptions.Script,
                // (1,26): error CS1003: Syntax error, ':' expected
                // T ? m(x: 1, y: a ? b : c)
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 26),
                // (1,26): error CS1733: Expected expression
                // T ? m(x: 1, y: a ? b : c)
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 26));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
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
                                                N(SyntaxKind.IdentifierToken, "x");
                                            }
                                            N(SyntaxKind.ColonToken);
                                        }
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "1");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.NameColon);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "y");
                                            }
                                            N(SyntaxKind.ColonToken);
                                        }
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
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "c");
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression27()
        {
            var tree = UsingTree(@"T ? u => { } : v => { }", TestOptions.Script);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.SimpleLambdaExpression);
                            {
                                N(SyntaxKind.Parameter);
                                {
                                    N(SyntaxKind.IdentifierToken, "u");
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleLambdaExpression);
                            {
                                N(SyntaxKind.Parameter);
                                {
                                    N(SyntaxKind.IdentifierToken, "v");
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression28()
        {
            var tree = UsingTree(@"T ? u => (d ? e => 1 : f => 2)(3) : c => 2", TestOptions.Script);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.SimpleLambdaExpression);
                            {
                                N(SyntaxKind.Parameter);
                                {
                                    N(SyntaxKind.IdentifierToken, "u");
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.InvocationExpression);
                                {
                                    N(SyntaxKind.ParenthesizedExpression);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.ConditionalExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "d");
                                            }
                                            N(SyntaxKind.QuestionToken);
                                            N(SyntaxKind.SimpleLambdaExpression);
                                            {
                                                N(SyntaxKind.Parameter);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "e");
                                                }
                                                N(SyntaxKind.EqualsGreaterThanToken);
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "1");
                                                }
                                            }
                                            N(SyntaxKind.ColonToken);
                                            N(SyntaxKind.SimpleLambdaExpression);
                                            {
                                                N(SyntaxKind.Parameter);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "f");
                                                }
                                                N(SyntaxKind.EqualsGreaterThanToken);
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "2");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.ArgumentList);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "3");
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleLambdaExpression);
                            {
                                N(SyntaxKind.Parameter);
                                {
                                    N(SyntaxKind.IdentifierToken, "c");
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "2");
                                }
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression30()
        {
            var tree = UsingTree(@"T ? a ?", TestOptions.Script,
                // (1,8): error CS1733: Expected expression
                // T ? a ?
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 8),
                // (1,8): error CS1003: Syntax error, ':' expected
                // T ? a ?
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 8),
                // (1,8): error CS1733: Expected expression
                // T ? a ?
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 8),
                // (1,8): error CS1003: Syntax error, ':' expected
                // T ? a ?
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 8),
                // (1,8): error CS1733: Expected expression
                // T ? a ?
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 8));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.ConditionalExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.QuestionToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                                M(SyntaxKind.ColonToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression31()
        {
            var tree = UsingTree(@"T ? a =", TestOptions.Script,
                // (1,8): error CS1733: Expected expression
                // T ? a =
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 8),
                // (1,8): error CS1003: Syntax error, ':' expected
                // T ? a =
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 8),
                // (1,8): error CS1733: Expected expression
                // T ? a =
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 8));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.SimpleAssignmentExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.EqualsToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression32()
        {
            var tree = UsingTree(@"T ? a = b", TestOptions.Script,
                // (1,10): error CS1003: Syntax error, ':' expected
                // T ? a = b
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 10),
                // (1,10): error CS1733: Expected expression
                // T ? a = b
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 10));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.SimpleAssignmentExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression33()
        {
            var tree = UsingTree(@"T ? a = b : ", TestOptions.Script,
                // (1,13): error CS1733: Expected expression
                // T ? a = b : 
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 13));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.SimpleAssignmentExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                            }
                            N(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression34()
        {
            var tree = UsingTree(@"T ? m(out c", TestOptions.Script,
                // (1,12): error CS1026: ) expected
                // T ? m(out c
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 12),
                // (1,12): error CS1003: Syntax error, ':' expected
                // T ? m(out c
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 12),
                // (1,12): error CS1733: Expected expression
                // T ? m(out c
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 12));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.OutKeyword);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "c");
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression35()
        {
            var tree = UsingTree(@"T ? m(ref c", TestOptions.Script,
                // (1,12): error CS1026: ) expected
                // T ? m(ref c
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 12),
                // (1,12): error CS1003: Syntax error, ':' expected
                // T ? m(ref c
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 12),
                // (1,12): error CS1733: Expected expression
                // T ? m(ref c
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 12));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.RefKeyword);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "c");
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression36()
        {
            var tree = UsingTree(@"T ? m(ref out", TestOptions.Script,
                // (1,11): error CS1525: Invalid expression term 'out'
                // T ? m(ref out
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "out").WithArguments("out").WithLocation(1, 11),
                // (1,11): error CS1003: Syntax error, ',' expected
                // T ? m(ref out
                Diagnostic(ErrorCode.ERR_SyntaxError, "out").WithArguments(",").WithLocation(1, 11),
                // (1,14): error CS1733: Expected expression
                // T ? m(ref out
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 14),
                // (1,14): error CS1026: ) expected
                // T ? m(ref out
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 14),
                // (1,14): error CS1003: Syntax error, ':' expected
                // T ? m(ref out
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 14),
                // (1,14): error CS1733: Expected expression
                // T ? m(ref out
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 14));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.RefKeyword);
                                        M(SyntaxKind.IdentifierName);
                                        {
                                            M(SyntaxKind.IdentifierToken);
                                        }
                                    }
                                    M(SyntaxKind.CommaToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.OutKeyword);
                                        M(SyntaxKind.IdentifierName);
                                        {
                                            M(SyntaxKind.IdentifierToken);
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression37()
        {
            var tree = UsingTree(@"T ? m(ref out c", TestOptions.Script,
                // (1,11): error CS1525: Invalid expression term 'out'
                // T ? m(ref out c
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "out").WithArguments("out").WithLocation(1, 11),
                // (1,11): error CS1003: Syntax error, ',' expected
                // T ? m(ref out c
                Diagnostic(ErrorCode.ERR_SyntaxError, "out").WithArguments(",").WithLocation(1, 11),
                // (1,16): error CS1026: ) expected
                // T ? m(ref out c
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 16),
                // (1,16): error CS1003: Syntax error, ':' expected
                // T ? m(ref out c
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 16),
                // (1,16): error CS1733: Expected expression
                // T ? m(ref out c
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 16));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.RefKeyword);
                                        M(SyntaxKind.IdentifierName);
                                        {
                                            M(SyntaxKind.IdentifierToken);
                                        }
                                    }
                                    M(SyntaxKind.CommaToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.OutKeyword);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "c");
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression38()
        {
            var tree = UsingTree(@"T ? m(this", TestOptions.Script,
                // (1,11): error CS1026: ) expected
                // T ? m(this
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 11),
                // (1,11): error CS1003: Syntax error, ':' expected
                // T ? m(this
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 11),
                // (1,11): error CS1733: Expected expression
                // T ? m(this
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 11));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.ThisExpression);
                                        {
                                            N(SyntaxKind.ThisKeyword);
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression39()
        {
            var tree = UsingTree(@"T ? m(this.", TestOptions.Script,
                // (1,12): error CS1001: Identifier expected
                // T ? m(this.
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 12),
                // (1,12): error CS1026: ) expected
                // T ? m(this.
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 12),
                // (1,12): error CS1003: Syntax error, ':' expected
                // T ? m(this.
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 12),
                // (1,12): error CS1733: Expected expression
                // T ? m(this.
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 12));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.SimpleMemberAccessExpression);
                                        {
                                            N(SyntaxKind.ThisExpression);
                                            {
                                                N(SyntaxKind.ThisKeyword);
                                            }
                                            N(SyntaxKind.DotToken);
                                            M(SyntaxKind.IdentifierName);
                                            {
                                                M(SyntaxKind.IdentifierToken);
                                            }
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression40()
        {
            var tree = UsingTree(@"T ? m(this<", TestOptions.Script,
                // (1,12): error CS1733: Expected expression
                // T ? m(this<
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 12),
                // (1,12): error CS1026: ) expected
                // T ? m(this<
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 12),
                // (1,12): error CS1003: Syntax error, ':' expected
                // T ? m(this<
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 12),
                // (1,12): error CS1733: Expected expression
                // T ? m(this<
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 12));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.LessThanExpression);
                                        {
                                            N(SyntaxKind.ThisExpression);
                                            {
                                                N(SyntaxKind.ThisKeyword);
                                            }
                                            N(SyntaxKind.LessThanToken);
                                            M(SyntaxKind.IdentifierName);
                                            {
                                                M(SyntaxKind.IdentifierToken);
                                            }
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression41()
        {
            var tree = UsingTree(@"T ? m(this[", TestOptions.Script,
                // (1,12): error CS1003: Syntax error, ']' expected
                // T ? m(this[
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("]").WithLocation(1, 12),
                // (1,12): error CS1026: ) expected
                // T ? m(this[
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 12),
                // (1,12): error CS1003: Syntax error, ':' expected
                // T ? m(this[
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 12),
                // (1,12): error CS1733: Expected expression
                // T ? m(this[
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 12));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.ElementAccessExpression);
                                        {
                                            N(SyntaxKind.ThisExpression);
                                            {
                                                N(SyntaxKind.ThisKeyword);
                                            }
                                            N(SyntaxKind.BracketedArgumentList);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                M(SyntaxKind.CloseBracketToken);
                                            }
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression41A()
        {
            var tree = UsingTree(@"T ? m(this a", TestOptions.Script,
                // (1,12): error CS1003: Syntax error, ',' expected
                // T ? m(this a
                Diagnostic(ErrorCode.ERR_SyntaxError, "a").WithArguments(",").WithLocation(1, 12),
                // (1,13): error CS1026: ) expected
                // T ? m(this a
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 13),
                // (1,13): error CS1003: Syntax error, ':' expected
                // T ? m(this a
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 13),
                // (1,13): error CS1733: Expected expression
                // T ? m(this a
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 13));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.ThisExpression);
                                        {
                                            N(SyntaxKind.ThisKeyword);
                                        }
                                    }
                                    M(SyntaxKind.CommaToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression42()
        {
            var tree = UsingTree(@"T ? m(this(", TestOptions.Script,
                // (1,12): error CS1026: ) expected
                // T ? m(this(
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 12),
                // (1,12): error CS1026: ) expected
                // T ? m(this(
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 12),
                // (1,12): error CS1003: Syntax error, ':' expected
                // T ? m(this(
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 12),
                // (1,12): error CS1733: Expected expression
                // T ? m(this(
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 12));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.InvocationExpression);
                                        {
                                            N(SyntaxKind.ThisExpression);
                                            {
                                                N(SyntaxKind.ThisKeyword);
                                            }
                                            N(SyntaxKind.ArgumentList);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                M(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression43()
        {
            var tree = UsingTree(@"T ? m(T[", TestOptions.Script,
                // (1,9): error CS1003: Syntax error, ']' expected
                // T ? m(T[
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("]").WithLocation(1, 9),
                // (1,9): error CS1026: ) expected
                // T ? m(T[
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 9),
                // (1,9): error CS1003: Syntax error, ':' expected
                // T ? m(T[
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 9),
                // (1,9): error CS1733: Expected expression
                // T ? m(T[
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 9));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.ElementAccessExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "T");
                                            }
                                            N(SyntaxKind.BracketedArgumentList);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                M(SyntaxKind.CloseBracketToken);
                                            }
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression44()
        {
            var tree = UsingTree(@"T ? m(T[1", TestOptions.Script,
                // (1,10): error CS1003: Syntax error, ']' expected
                // T ? m(T[1
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("]").WithLocation(1, 10),
                // (1,10): error CS1026: ) expected
                // T ? m(T[1
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 10),
                // (1,10): error CS1003: Syntax error, ':' expected
                // T ? m(T[1
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 10),
                // (1,10): error CS1733: Expected expression
                // T ? m(T[1
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 10));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.ElementAccessExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "T");
                                            }
                                            N(SyntaxKind.BracketedArgumentList);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                N(SyntaxKind.Argument);
                                                {
                                                    N(SyntaxKind.NumericLiteralExpression);
                                                    {
                                                        N(SyntaxKind.NumericLiteralToken, "1");
                                                    }
                                                }
                                                M(SyntaxKind.CloseBracketToken);
                                            }
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression45()
        {
            var tree = UsingTree(@"T ? m(T[1]", TestOptions.Script,
                // (1,11): error CS1026: ) expected
                // T ? m(T[1]
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 11),
                // (1,11): error CS1003: Syntax error, ':' expected
                // T ? m(T[1]
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 11),
                // (1,11): error CS1733: Expected expression
                // T ? m(T[1]
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 11));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.ElementAccessExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "T");
                                            }
                                            N(SyntaxKind.BracketedArgumentList);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                N(SyntaxKind.Argument);
                                                {
                                                    N(SyntaxKind.NumericLiteralExpression);
                                                    {
                                                        N(SyntaxKind.NumericLiteralToken, "1");
                                                    }
                                                }
                                                N(SyntaxKind.CloseBracketToken);
                                            }
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_MethodDecl46()
        {
            var tree = UsingTree(@"T ? a(T ? a =", TestOptions.Script,
                // (1,14): error CS1733: Expected expression
                // T ? a(T ? a =
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 14),
                // (1,14): error CS1003: Syntax error, ':' expected
                // T ? a(T ? a =
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 14),
                // (1,14): error CS1733: Expected expression
                // T ? a(T ? a =
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 14),
                // (1,14): error CS1026: ) expected
                // T ? a(T ? a =
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 14),
                // (1,14): error CS1003: Syntax error, ':' expected
                // T ? a(T ? a =
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 14),
                // (1,14): error CS1733: Expected expression
                // T ? a(T ? a =
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 14));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.ConditionalExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "T");
                                            }
                                            N(SyntaxKind.QuestionToken);
                                            N(SyntaxKind.SimpleAssignmentExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "a");
                                                }
                                                N(SyntaxKind.EqualsToken);
                                                M(SyntaxKind.IdentifierName);
                                                {
                                                    M(SyntaxKind.IdentifierToken);
                                                }
                                            }
                                            M(SyntaxKind.ColonToken);
                                            M(SyntaxKind.IdentifierName);
                                            {
                                                M(SyntaxKind.IdentifierToken);
                                            }
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression47()
        {
            var tree = UsingTree(@"T ? a(T)", TestOptions.Script,
                // (1,9): error CS1003: Syntax error, ':' expected
                // T ? a(T)
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 9),
                // (1,9): error CS1733: Expected expression
                // T ? a(T)
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 9));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "T");
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression48()
        {
            var tree = UsingTree(@"T ? a(ref int.MaxValue)", TestOptions.Script,
                // (1,24): error CS1003: Syntax error, ':' expected
                // T ? a(ref int.MaxValue)
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 24),
                // (1,24): error CS1733: Expected expression
                // T ? a(ref int.MaxValue)
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 24));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.RefKeyword);
                                        N(SyntaxKind.SimpleMemberAccessExpression);
                                        {
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.DotToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "MaxValue");
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression49()
        {
            var tree = UsingTree(@"T ? a(ref a,", TestOptions.Script,
                // (1,13): error CS1733: Expected expression
                // T ? a(ref a,
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 13),
                // (1,13): error CS1026: ) expected
                // T ? a(ref a,
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 13),
                // (1,13): error CS1003: Syntax error, ':' expected
                // T ? a(ref a,
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 13),
                // (1,13): error CS1733: Expected expression
                // T ? a(ref a,
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 13));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.RefKeyword);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    M(SyntaxKind.Argument);
                                    {
                                        M(SyntaxKind.IdentifierName);
                                        {
                                            M(SyntaxKind.IdentifierToken);
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression50()
        {
            var tree = UsingTree(@"T ? a(,", TestOptions.Script,
                // (1,7): error CS0839: Argument missing
                // T ? a(,
                Diagnostic(ErrorCode.ERR_MissingArgument, ",").WithLocation(1, 7),
                // (1,8): error CS1733: Expected expression
                // T ? a(,
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 8),
                // (1,8): error CS1026: ) expected
                // T ? a(,
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 8),
                // (1,8): error CS1003: Syntax error, ':' expected
                // T ? a(,
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 8),
                // (1,8): error CS1733: Expected expression
                // T ? a(,
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 8));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    M(SyntaxKind.Argument);
                                    {
                                        M(SyntaxKind.IdentifierName);
                                        {
                                            M(SyntaxKind.IdentifierToken);
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    M(SyntaxKind.Argument);
                                    {
                                        M(SyntaxKind.IdentifierName);
                                        {
                                            M(SyntaxKind.IdentifierToken);
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression51()
        {
            var tree = UsingTree(@"T ? a(T ? b[1] : b[2])", TestOptions.Script,
                // (1,23): error CS1003: Syntax error, ':' expected
                // T ? a(T ? b[1] : b[2])
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 23),
                // (1,23): error CS1733: Expected expression
                // T ? a(T ? b[1] : b[2])
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 23));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.ConditionalExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "T");
                                            }
                                            N(SyntaxKind.QuestionToken);
                                            N(SyntaxKind.ElementAccessExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "b");
                                                }
                                                N(SyntaxKind.BracketedArgumentList);
                                                {
                                                    N(SyntaxKind.OpenBracketToken);
                                                    N(SyntaxKind.Argument);
                                                    {
                                                        N(SyntaxKind.NumericLiteralExpression);
                                                        {
                                                            N(SyntaxKind.NumericLiteralToken, "1");
                                                        }
                                                    }
                                                    N(SyntaxKind.CloseBracketToken);
                                                }
                                            }
                                            N(SyntaxKind.ColonToken);
                                            N(SyntaxKind.ElementAccessExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "b");
                                                }
                                                N(SyntaxKind.BracketedArgumentList);
                                                {
                                                    N(SyntaxKind.OpenBracketToken);
                                                    N(SyntaxKind.Argument);
                                                    {
                                                        N(SyntaxKind.NumericLiteralExpression);
                                                        {
                                                            N(SyntaxKind.NumericLiteralToken, "2");
                                                        }
                                                    }
                                                    N(SyntaxKind.CloseBracketToken);
                                                }
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_Expression52()
        {
            var tree = UsingTree(@"
T ? f(a ? b : c)
",
                // (2,17): error CS1003: Syntax error, ':' expected
                // T ? f(a ? b : c)
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(2, 17),
                // (2,17): error CS1733: Expected expression
                // T ? f(a ? b : c)
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(2, 17));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "f");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
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
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "c");
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        /// <summary>
        /// Trailing colon turns a method declaration into an expression.
        /// </summary>
        [Fact]
        public void Ternary_Expression_GenericAmbiguity1()
        {
            var tree = UsingTree(@"T ? m(a < b, c > d) :", TestOptions.Script,
                // (1,22): error CS1733: Expected expression
                // T ? m(a < b, c > d) :
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 22));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "m");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.LessThanExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "a");
                                            }
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "b");
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.GreaterThanExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "c");
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "d");
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                            N(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_WithQuery_FieldDecl1()
        {
            var tree = UsingTree(@"
T? from;
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.NullableType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.QuestionToken);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void Ternary_WithQuery_Expression1()
        {
            var tree = UsingTree(@"
T ? from
",
                // (2,9): error CS1003: Syntax error, ':' expected
                // T ? from
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(2, 9),
                // (2,9): error CS1733: Expected expression
                // T ? from
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(2, 9));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "from");
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_WithQuery_Expression2()
        {
            var tree = UsingTree(@"
T ? from x
",
                // (2,11): error CS1001: Identifier expected
                // T ? from x
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(2, 11),
                // (2,11): error CS1003: Syntax error, 'in' expected
                // T ? from x
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("in").WithLocation(2, 11),
                // (2,11): error CS1733: Expected expression
                // T ? from x
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(2, 11),
                // (2,11): error CS0742: A query body must end with a select clause or a group clause
                // T ? from x
                Diagnostic(ErrorCode.ERR_ExpectedSelectOrGroup, "").WithLocation(2, 11),
                // (2,11): error CS1003: Syntax error, ':' expected
                // T ? from x
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(2, 11),
                // (2,11): error CS1733: Expected expression
                // T ? from x
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(2, 11));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.QueryExpression);
                            {
                                N(SyntaxKind.FromClause);
                                {
                                    N(SyntaxKind.FromKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                    M(SyntaxKind.IdentifierToken);
                                    M(SyntaxKind.InKeyword);
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                M(SyntaxKind.QueryBody);
                                {
                                    M(SyntaxKind.SelectClause);
                                    {
                                        M(SyntaxKind.SelectKeyword);
                                        M(SyntaxKind.IdentifierName);
                                        {
                                            M(SyntaxKind.IdentifierToken);
                                        }
                                    }
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Ternary_WithQuery_Expression3()
        {
            var tree = UsingTree(@"
T ? f(from
",
                // (2,11): error CS1026: ) expected
                // T ? f(from
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(2, 11),
                // (2,11): error CS1003: Syntax error, ':' expected
                // T ? f(from
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(2, 11),
                // (2,11): error CS1733: Expected expression
                // T ? f(from
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(2, 11));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "f");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "from");
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        /// <summary>
        /// Assume that "from" usually doesn't bind to a type and is rather a start of a query.
        /// </summary>
        [Fact]
        public void Ternary_WithQuery_Expression4()
        {
            var tree = UsingTree(@"
T ? f(from x
",
                // (2,13): error CS1001: Identifier expected
                // T ? f(from x
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(2, 13),
                // (2,13): error CS1003: Syntax error, 'in' expected
                // T ? f(from x
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("in").WithLocation(2, 13),
                // (2,13): error CS1733: Expected expression
                // T ? f(from x
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(2, 13),
                // (2,13): error CS0742: A query body must end with a select clause or a group clause
                // T ? f(from x
                Diagnostic(ErrorCode.ERR_ExpectedSelectOrGroup, "").WithLocation(2, 13),
                // (2,13): error CS1026: ) expected
                // T ? f(from x
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(2, 13),
                // (2,13): error CS1003: Syntax error, ':' expected
                // T ? f(from x
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(2, 13),
                // (2,13): error CS1733: Expected expression
                // T ? f(from x
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(2, 13));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "f");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.QueryExpression);
                                        {
                                            N(SyntaxKind.FromClause);
                                            {
                                                N(SyntaxKind.FromKeyword);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "x");
                                                }
                                                M(SyntaxKind.IdentifierToken);
                                                M(SyntaxKind.InKeyword);
                                                M(SyntaxKind.IdentifierName);
                                                {
                                                    M(SyntaxKind.IdentifierToken);
                                                }
                                            }
                                            M(SyntaxKind.QueryBody);
                                            {
                                                M(SyntaxKind.SelectClause);
                                                {
                                                    M(SyntaxKind.SelectKeyword);
                                                    M(SyntaxKind.IdentifierName);
                                                    {
                                                        M(SyntaxKind.IdentifierToken);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                            M(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        #endregion

        #region Queries

        [Fact]
        public void From_Identifier()
        {
            var tree = UsingTree(@"from", TestOptions.Script);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "from");
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void From_FieldDecl()
        {
            var tree = UsingTree(@"from c", TestOptions.Script,
                // (1,7): error CS1002: ; expected
                // from c
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 7));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void From_FieldDecl2()
        {
            var tree = UsingTree(@"from x,",
                // (1,8): error CS1001: Identifier expected
                // from x,
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 8),
                // (1,8): error CS1002: ; expected
                // from x,
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 8));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "from");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.CommaToken);
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void From_FieldDecl3()
        {
            var tree = UsingTree(@"from x;");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void From_FieldDecl4()
        {
            var tree = UsingTree(@"from x =",
                // (1,9): error CS1733: Expected expression
                // from x =
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 9),
                // (1,9): error CS1002: ; expected
                // from x =
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 9));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "from");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                            N(SyntaxKind.EqualsValueClause);
                            {
                                N(SyntaxKind.EqualsToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void From_FieldDecl5()
        {
            var tree = UsingTree(@"from x[",
                // (1,7): error CS0650: Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.
                // from x[
                Diagnostic(ErrorCode.ERR_CStyleArray, "[").WithLocation(1, 7),
                // (1,8): error CS1003: Syntax error, ']' expected
                // from x[
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("]").WithLocation(1, 8),
                // (1,8): error CS1002: ; expected
                // from x[
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 8));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "from");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                            N(SyntaxKind.BracketedArgumentList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.OmittedArraySizeExpression);
                                    {
                                        N(SyntaxKind.OmittedArraySizeExpressionToken);
                                    }
                                }
                                M(SyntaxKind.CloseBracketToken);
                            }
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void From_MethodDecl1()
        {
            var tree = UsingTree(@"from c(",
                // (1,8): error CS1026: ) expected
                // from c(
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 8),
                // (1,8): error CS1002: ; expected
                // from c(
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 8));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "from");
                    }
                    N(SyntaxKind.IdentifierToken, "c");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void From_MethodDecl2()
        {
            var tree = UsingTree(@"from a<",
                // (1,8): error CS1001: Identifier expected
                // from a<
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 8),
                // (1,8): error CS1003: Syntax error, '>' expected
                // from a<
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(">").WithLocation(1, 8),
                // (1,8): error CS1003: Syntax error, '(' expected
                // from a<
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(1, 8),
                // (1,8): error CS1026: ) expected
                // from a<
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 8),
                // (1,8): error CS1002: ; expected
                // from a<
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 8));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "from");
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        M(SyntaxKind.TypeParameter);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.GreaterThanToken);
                    }
                    M(SyntaxKind.ParameterList);
                    {
                        M(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void From_MethodDecl3()
        {
            var tree = UsingTree(@"from a.",
                // (1,8): error CS1001: Identifier expected
                // from a.
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 8),
                // (1,8): error CS1003: Syntax error, '(' expected
                // from a.
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(1, 8),
                // (1,8): error CS1026: ) expected
                // from a.
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 8),
                // (1,8): error CS1002: ; expected
                // from a.
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 8));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "from");
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        N(SyntaxKind.DotToken);
                    }
                    M(SyntaxKind.IdentifierToken);
                    M(SyntaxKind.ParameterList);
                    {
                        M(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void From_MethodDecl4()
        {
            var tree = UsingTree(@"from a::",
                // (1,7): error CS0687: The namespace alias qualifier '::' always resolves to a type or namespace so is illegal here. Consider using '.' instead.
                // from a::
                Diagnostic(ErrorCode.ERR_AliasQualAsExpression, "::").WithLocation(1, 7),
                // (1,9): error CS1001: Identifier expected
                // from a::
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 9),
                // (1,9): error CS1003: Syntax error, '(' expected
                // from a::
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(1, 9),
                // (1,9): error CS1026: ) expected
                // from a::
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 9),
                // (1,9): error CS1002: ; expected
                // from a::
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 9));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "from");
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        M(SyntaxKind.DotToken);
                    }
                    M(SyntaxKind.IdentifierToken);
                    M(SyntaxKind.ParameterList);
                    {
                        M(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void From_MethodDecl5()
        {
            var tree = UsingTree(@"from global::",
                // (1,12): error CS0687: The namespace alias qualifier '::' always resolves to a type or namespace so is illegal here. Consider using '.' instead.
                // from global::
                Diagnostic(ErrorCode.ERR_AliasQualAsExpression, "::").WithLocation(1, 12),
                // (1,14): error CS1001: Identifier expected
                // from global::
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 14),
                // (1,14): error CS1003: Syntax error, '(' expected
                // from global::
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(1, 14),
                // (1,14): error CS1026: ) expected
                // from global::
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 14),
                // (1,14): error CS1002: ; expected
                // from global::
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 14));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "from");
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "global");
                        }
                        M(SyntaxKind.DotToken);
                    }
                    M(SyntaxKind.IdentifierToken);
                    M(SyntaxKind.ParameterList);
                    {
                        M(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void From_PropertyDecl1()
        {
            var tree = UsingTree(@"from c {",
                // (1,9): error CS1513: } expected
                // from c {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 9));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "from");
                    }
                    N(SyntaxKind.IdentifierToken, "c");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void From_Query1()
        {
            var tree = UsingTree(@"from c d",
                // (1,9): error CS1003: Syntax error, 'in' expected
                // from c d
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("in").WithLocation(1, 9),
                // (1,9): error CS1733: Expected expression
                // from c d
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 9),
                // (1,9): error CS0742: A query body must end with a select clause or a group clause
                // from c d
                Diagnostic(ErrorCode.ERR_ExpectedSelectOrGroup, "").WithLocation(1, 9));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.QueryExpression);
                        {
                            N(SyntaxKind.FromClause);
                            {
                                N(SyntaxKind.FromKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "c");
                                }
                                N(SyntaxKind.IdentifierToken, "d");
                                M(SyntaxKind.InKeyword);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.QueryBody);
                            {
                                M(SyntaxKind.SelectClause);
                                {
                                    M(SyntaxKind.SelectKeyword);
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void From_Query2()
        {
            var tree = UsingTree(@"from x* a",
                // (1,10): error CS1003: Syntax error, 'in' expected
                // from x* a
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("in").WithLocation(1, 10),
                // (1,10): error CS1733: Expected expression
                // from x* a
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 10),
                // (1,10): error CS0742: A query body must end with a select clause or a group clause
                // from x* a
                Diagnostic(ErrorCode.ERR_ExpectedSelectOrGroup, "").WithLocation(1, 10));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.QueryExpression);
                        {
                            N(SyntaxKind.FromClause);
                            {
                                N(SyntaxKind.FromKeyword);
                                N(SyntaxKind.PointerType);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                    N(SyntaxKind.AsteriskToken);
                                }
                                N(SyntaxKind.IdentifierToken, "a");
                                M(SyntaxKind.InKeyword);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.QueryBody);
                            {
                                M(SyntaxKind.SelectClause);
                                {
                                    M(SyntaxKind.SelectKeyword);
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void From_Query3()
        {
            var tree = UsingTree(@"from x? a",
                // (1,10): error CS1003: Syntax error, 'in' expected
                // from x? a
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("in").WithLocation(1, 10),
                // (1,10): error CS1733: Expected expression
                // from x? a
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 10),
                // (1,10): error CS0742: A query body must end with a select clause or a group clause
                // from x? a
                Diagnostic(ErrorCode.ERR_ExpectedSelectOrGroup, "").WithLocation(1, 10));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.QueryExpression);
                        {
                            N(SyntaxKind.FromClause);
                            {
                                N(SyntaxKind.FromKeyword);
                                N(SyntaxKind.NullableType);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                    N(SyntaxKind.QuestionToken);
                                }
                                N(SyntaxKind.IdentifierToken, "a");
                                M(SyntaxKind.InKeyword);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.QueryBody);
                            {
                                M(SyntaxKind.SelectClause);
                                {
                                    M(SyntaxKind.SelectKeyword);
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void From_Query4()
        {
            var tree = UsingTree(@"from x[] a",
                // (1,11): error CS1003: Syntax error, 'in' expected
                // from x[] a
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("in").WithLocation(1, 11),
                // (1,11): error CS1733: Expected expression
                // from x[] a
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 11),
                // (1,11): error CS0742: A query body must end with a select clause or a group clause
                // from x[] a
                Diagnostic(ErrorCode.ERR_ExpectedSelectOrGroup, "").WithLocation(1, 11));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.QueryExpression);
                        {
                            N(SyntaxKind.FromClause);
                            {
                                N(SyntaxKind.FromKeyword);
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
                                N(SyntaxKind.IdentifierToken, "a");
                                M(SyntaxKind.InKeyword);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.QueryBody);
                            {
                                M(SyntaxKind.SelectClause);
                                {
                                    M(SyntaxKind.SelectKeyword);
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void From_Query5()
        {
            var tree = UsingTree(@"from goo in",
                // (1,12): error CS1733: Expected expression
                // from goo in
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 12),
                // (1,12): error CS0742: A query body must end with a select clause or a group clause
                // from goo in
                Diagnostic(ErrorCode.ERR_ExpectedSelectOrGroup, "").WithLocation(1, 12));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.QueryExpression);
                        {
                            N(SyntaxKind.FromClause);
                            {
                                N(SyntaxKind.FromKeyword);
                                N(SyntaxKind.IdentifierToken, "goo");
                                N(SyntaxKind.InKeyword);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.QueryBody);
                            {
                                M(SyntaxKind.SelectClause);
                                {
                                    M(SyntaxKind.SelectKeyword);
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void From_Query6()
        {
            var tree = UsingTree(@"from goo.bar in",
                // (1,14): error CS1001: Identifier expected
                // from goo.bar in
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "in").WithLocation(1, 14),
                // (1,16): error CS1733: Expected expression
                // from goo.bar in
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 16),
                // (1,16): error CS0742: A query body must end with a select clause or a group clause
                // from goo.bar in
                Diagnostic(ErrorCode.ERR_ExpectedSelectOrGroup, "").WithLocation(1, 16));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.QueryExpression);
                        {
                            N(SyntaxKind.FromClause);
                            {
                                N(SyntaxKind.FromKeyword);
                                N(SyntaxKind.QualifiedName);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "goo");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "bar");
                                    }
                                }
                                M(SyntaxKind.IdentifierToken);
                                N(SyntaxKind.InKeyword);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.QueryBody);
                            {
                                M(SyntaxKind.SelectClause);
                                {
                                    M(SyntaxKind.SelectKeyword);
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        #endregion

        #region Global statement separators

        /// <summary>
        /// Comma after a global statement is ignored and a new global statement is parsed.
        /// </summary>
        [Fact]
        public void GlobalStatementSeparators_Comma1()
        {
            var tree = UsingTree("a < b,c.", TestOptions.Script,
                // (1,6): error CS1002: ; expected
                // a < b,c.
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(1, 6),
                // (1,6): error CS7017: Member definition, statement, or end-of-file expected
                // a < b,c.
                Diagnostic(ErrorCode.ERR_GlobalDefinitionOrStatementExpected, ",").WithLocation(1, 6),
                // (1,9): error CS1001: Identifier expected
                // a < b,c.
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 9));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.LessThanExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleMemberAccessExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                            N(SyntaxKind.DotToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalStatementSeparators_Comma2()
        {
            var tree = UsingTree(@"
a < b,
void goo() { }
",
                // (3,1): error CS1547: Keyword 'void' cannot be used in this context
                // void goo() { }
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(3, 1),
                // (3,6): error CS1003: Syntax error, '>' expected
                // void goo() { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "goo").WithArguments(">").WithLocation(3, 6));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.VoidKeyword);
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "goo");
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalStatementSeparators_ClosingParen()
        {
            var tree = UsingTree(@"
a < b)
void goo() { }
",
                // (2,6): error CS1002: ; expected
                // a < b)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(2, 6),
                // (2,6): error CS7017: Member definition, statement, or end-of-file expected
                // a < b)
                Diagnostic(ErrorCode.ERR_GlobalDefinitionOrStatementExpected, ")").WithLocation(2, 6));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.LessThanExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "goo");
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalStatementSeparators_ClosingBracket()
        {
            var tree = UsingTree(@"
a < b]
void goo() { }
",
                // (2,6): error CS1002: ; expected
                // a < b]
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "]").WithLocation(2, 6),
                // (2,6): error CS7017: Member definition, statement, or end-of-file expected
                // a < b]
                Diagnostic(ErrorCode.ERR_GlobalDefinitionOrStatementExpected, "]").WithLocation(2, 6));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.LessThanExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "goo");
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalStatementSeparators_ClosingBrace()
        {
            var tree = UsingTree(@"
a < b}
void goo() { }
",
                // (2,6): error CS1002: ; expected
                // a < b}
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(2, 6),
                // (2,6): error CS7017: Member definition, statement, or end-of-file expected
                // a < b}
                Diagnostic(ErrorCode.ERR_GlobalDefinitionOrStatementExpected, "}").WithLocation(2, 6));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.LessThanExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "goo");
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalStatementSeparators_NonAsciiCharacter()
        {
            var test = @"
H �oz
";
            ParseAndValidate(test,
                new ErrorDescription { Code = (int)ErrorCode.ERR_SemicolonExpected, Line = 2, Column = 3 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnexpectedCharacter, Line = 2, Column = 3 });
        }

        [Fact]
        public void GlobalStatementSeparators_UnicodeCharacter()
        {
            var test = @"
int नुसौप्रख्यातनिदेशकपुरानी 
";
            ParseAndValidate(test,
                new ErrorDescription { Code = (int)ErrorCode.ERR_SemicolonExpected, Line = 2, Column = 29 });
        }

        [Fact]
        public void GlobalStatementSeparators_Missing()
        {
            var test = @"
using System;
int a
Console.Goo()
";

            UsingTree(test,
                // (3,6): error CS1003: Syntax error, '=' expected
                // int a
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("=").WithLocation(3, 6),
                // (4,14): error CS1002: ; expected
                // Console.Goo()
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 14));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "System");
                    }
                    N(SyntaxKind.SemicolonToken);
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
                            N(SyntaxKind.IdentifierToken, "a");
                            N(SyntaxKind.EqualsValueClause);
                            {
                                M(SyntaxKind.EqualsToken);
                                N(SyntaxKind.InvocationExpression);
                                {
                                    N(SyntaxKind.SimpleMemberAccessExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Console");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Goo");
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
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        #endregion

        #region Invalid Keywords

        [Fact]
        public void OperatorError()
        {
            var test = @"operator";
            ParseAndValidate(test,
                new ErrorDescription { Code = 1003, Line = 1, Column = 9 },
                new ErrorDescription { Code = 1031, Line = 1, Column = 9 },
                new ErrorDescription { Code = 1003, Line = 1, Column = 1 },
                new ErrorDescription { Code = 1026, Line = 1, Column = 9 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_SemicolonExpected, Line = 1, Column = 9 });
        }

        [Fact]
        public void OperatorImplicitError()
        {
            var test = @"implicit";
            ParseAndValidate(test,
                new ErrorDescription { Code = 1003, Line = 1, Column = 9 },
                new ErrorDescription { Code = 1031, Line = 1, Column = 9 },
                new ErrorDescription { Code = 1003, Line = 1, Column = 9 },
                new ErrorDescription { Code = 1026, Line = 1, Column = 9 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_SemicolonExpected, Line = 1, Column = 9 });
        }

        [Fact]
        public void OperatorExplicitError()
        {
            var test = @"explicit";
            ParseAndValidate(test,
                new ErrorDescription { Code = 1003, Line = 1, Column = 9 },
                new ErrorDescription { Code = 1031, Line = 1, Column = 9 },
                new ErrorDescription { Code = 1003, Line = 1, Column = 9 },
                new ErrorDescription { Code = 1026, Line = 1, Column = 9 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_SemicolonExpected, Line = 1, Column = 9 });
        }

        #endregion

        [Fact]
        public void FieldDeclaration()
        {
            var test = @"
volatile int x;
const int w;
readonly int y;
static int z;
";
            ParseAndValidate(test, new ErrorDescription { Code = 145, Line = 3, Column = 11 });
        }

        /// bug="3782" project = "Roslyn"
        [Fact]
        public void ClassDeclaration()
        {
            var test = @"
class C { }
static class C2 { }
partial class C3 { }
";
            ParseAndValidate(test);
        }

        /// bug="3783" project = "Roslyn"
        [Fact]
        public void InterfaceDeclaration()
        {
            var test = @"
interface IC { }
";
            ParseAndValidate(test);
        }

        [Fact]
        public void TopLevelXML()
        {
            var test = @"
<Expects Status=success></Expects>
";
            ParseAndValidate(test,
                new ErrorDescription { Code = 1525, Line = 2, Column = 1 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_SemicolonExpected, Line = 2, Column = 10 },
                new ErrorDescription { Code = 1525, Line = 2, Column = 25 },
                new ErrorDescription { Code = 1525, Line = 2, Column = 26 },
                new ErrorDescription { Code = 1733, Line = 2, Column = 35 });
        }

        [Fact]
        public void NotIncorrectKeyword()
        {
            var test = @"
parial class Test
{
}
";
            ParseAndValidate(test,
                new ErrorDescription { Code = (int)ErrorCode.ERR_SemicolonExpected, Line = 2, Column = 8 });
        }

        [Fact]
        public void Keyword()
        {
            var test = @"
p class A
 {
 }
";
            ParseAndValidate(test,
                new ErrorDescription { Code = (int)ErrorCode.ERR_SemicolonExpected, Line = 2, Column = 3 });
        }

        [WorkItem(528532, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528532")]
        [Fact]
        public void ParseForwardSlash()
        {
            var test = @"/";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            Assert.Equal(1, tree.GetCompilationUnitRoot().ChildNodes().Count());
            Assert.Equal(SyntaxKind.GlobalStatement, tree.GetCompilationUnitRoot().ChildNodes().ToList()[0].Kind());
        }

        [WorkItem(541164, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541164")]
        [Fact]
        public void CS1733ERR_ExpressionExpected()
        {
            var test = @"Console.WriteLine(""Hello"")?";

            ParseAndValidate(test,
                new ErrorDescription { Code = 1733, Line = 1, Column = 28 },
                new ErrorDescription { Code = 1003, Line = 1, Column = 28 },
                new ErrorDescription { Code = 1733, Line = 1, Column = 28 });
        }

        #region Shebang

        [Fact]
        public void Shebang()
        {
            var command = "/usr/bin/env csi";
            var tree = ParseAndValidate($"#!{command}", TestOptions.Script);
            var root = tree.GetCompilationUnitRoot();

            Assert.Empty(root.ChildNodes());
            var eof = root.EndOfFileToken;
            Assert.Equal(SyntaxKind.EndOfFileToken, eof.Kind());
            var trivia = eof.GetLeadingTrivia().Single();
            TestShebang(trivia, command);
            Assert.True(root.ContainsDirectives);
            TestShebang(root.GetDirectives().Single(), command);

            tree = ParseAndValidate($"#! {command}\r\n ", TestOptions.Script);
            root = tree.GetCompilationUnitRoot();

            Assert.Empty(root.ChildNodes());
            eof = root.EndOfFileToken;
            Assert.Equal(SyntaxKind.EndOfFileToken, eof.Kind());
            var leading = eof.GetLeadingTrivia().ToArray();
            Assert.Equal(2, leading.Length);
            Assert.Equal(SyntaxKind.ShebangDirectiveTrivia, leading[0].Kind());
            Assert.Equal(SyntaxKind.WhitespaceTrivia, leading[1].Kind());
            TestShebang(leading[0], command);
            Assert.True(root.ContainsDirectives);
            TestShebang(root.GetDirectives().Single(), command);

            tree = ParseAndValidate(
$@"#!{command}
Console.WriteLine(""Hi!"");", TestOptions.Script);
            root = tree.GetCompilationUnitRoot();

            var statement = root.ChildNodes().Single();
            Assert.Equal(SyntaxKind.GlobalStatement, statement.Kind());
            trivia = statement.GetLeadingTrivia().Single();
            TestShebang(trivia, command);
            Assert.True(root.ContainsDirectives);
            TestShebang(root.GetDirectives().Single(), command);
        }

        [Fact]
        public void ShebangNotFirstCharacter()
        {
            ParseAndValidate(" #!/usr/bin/env csi", TestOptions.Script,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadDirectivePlacement, Line = 1, Column = 2 });

            ParseAndValidate("\n#!/usr/bin/env csi", TestOptions.Script,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadDirectivePlacement, Line = 2, Column = 1 });

            ParseAndValidate("\r\n#!/usr/bin/env csi", TestOptions.Script,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadDirectivePlacement, Line = 2, Column = 1 });

            ParseAndValidate("#!/bin/sh\r\n#!/usr/bin/env csi", TestOptions.Script,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadDirectivePlacement, Line = 2, Column = 1 });

            ParseAndValidate("a #!/usr/bin/env csi", TestOptions.Script,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadDirectivePlacement, Line = 1, Column = 3 });
        }

        [Fact]
        public void ShebangNoBang()
        {
            ParseAndValidate("#/usr/bin/env csi", TestOptions.Script,
                new ErrorDescription { Code = (int)ErrorCode.ERR_PPDirectiveExpected, Line = 1, Column = 1 });
        }

        [Fact]
        public void ShebangSpaceBang()
        {
            ParseAndValidate("# !/usr/bin/env csi", TestOptions.Script,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadDirectivePlacement, Line = 1, Column = 1 });
        }

        [Fact]
        public void ShebangInComment()
        {
            var tree = ParseAndValidate("//#!/usr/bin/env csi", TestOptions.Script);
            var root = tree.GetCompilationUnitRoot();

            Assert.Empty(root.ChildNodes());
            var eof = root.EndOfFileToken;
            Assert.Equal(SyntaxKind.EndOfFileToken, eof.Kind());
            Assert.Equal(SyntaxKind.SingleLineCommentTrivia, eof.GetLeadingTrivia().Single().Kind());
        }

        [Fact]
        public void ShebangNotInScript()
        {
            ParseAndValidate("#!/usr/bin/env csi", TestOptions.Regular,
                // (1,2): error CS9314: '#!' directives can be only used in scripts or file-based programs
                new ErrorDescription { Code = (int)ErrorCode.ERR_PPShebangInProjectBasedProgram, Line = 1, Column = 2 });
        }

        private void TestShebang(SyntaxTrivia trivia, string expectedSkippedText)
        {
            Assert.True(trivia.IsDirective);
            Assert.Equal(SyntaxKind.ShebangDirectiveTrivia, trivia.Kind());
            Assert.True(trivia.HasStructure);
            TestShebang((ShebangDirectiveTriviaSyntax)trivia.GetStructure(), expectedSkippedText);
        }

        private void TestShebang(DirectiveTriviaSyntax directive, string expectedSkippedText)
        {
            var shebang = (ShebangDirectiveTriviaSyntax)directive;
            Assert.False(shebang.HasStructuredTrivia);
            Assert.Equal(SyntaxKind.HashToken, shebang.HashToken.Kind());
            Assert.Equal(SyntaxKind.ExclamationToken, shebang.ExclamationToken.Kind());
            var endOfDirective = shebang.EndOfDirectiveToken;
            Assert.Equal(SyntaxKind.EndOfDirectiveToken, endOfDirective.Kind());
            Assert.Equal(0, endOfDirective.Span.Length);
            var skippedText = endOfDirective.LeadingTrivia.Single();
            Assert.Equal(SyntaxKind.PreprocessingMessageTrivia, skippedText.Kind());
            Assert.Equal(expectedSkippedText, skippedText.ToString());
            var content = shebang.Content;
            Assert.False(content.HasLeadingTrivia || content.HasTrailingTrivia);
            Assert.Equal(SyntaxKind.StringLiteralToken, content.Kind());
            Assert.Equal(expectedSkippedText, content.ToString());
        }

        #endregion
    }
}
