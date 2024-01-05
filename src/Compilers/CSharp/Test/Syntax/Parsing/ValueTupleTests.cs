// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Xunit;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing
{
    public class ValueTupleTests : ParsingTests
    {
        public ValueTupleTests(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: options);
        }

        [Fact]
        public void SimpleTuple()
        {
            var tree = UsingTree(@"
class C
{
    (int, string) Goo()
    {
        return (1, ""Alice"");
    }
}", options: TestOptions.Regular);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
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
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
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
                            N(SyntaxKind.ReturnStatement);
                            {
                                N(SyntaxKind.ReturnKeyword);
                                N(SyntaxKind.TupleExpression);
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
                                        N(SyntaxKind.StringLiteralExpression);
                                        {
                                            N(SyntaxKind.StringLiteralToken);
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
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
        public void LongTuple()
        {
            var tree = UsingTree(@"
class C
{
    (int, int, int, string, string, string, int, int, int) Goo()
    {
    }
}", options: TestOptions.Regular);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
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
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TuplesInLambda()
        {
            var tree = UsingTree(@"
class C
{
    var x = ((string, string) a, (int, int) b) => { };
}", options: TestOptions.Regular);
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
                                    N(SyntaxKind.ParenthesizedLambdaExpression);
                                    {
                                        N(SyntaxKind.ParameterList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.Parameter);
                                            {
                                                N(SyntaxKind.TupleType);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.TupleElement);
                                                    {
                                                        N(SyntaxKind.PredefinedType);
                                                        {
                                                            N(SyntaxKind.StringKeyword);
                                                        }
                                                    }
                                                    N(SyntaxKind.CommaToken);
                                                    N(SyntaxKind.TupleElement);
                                                    {
                                                        N(SyntaxKind.PredefinedType);
                                                        {
                                                            N(SyntaxKind.StringKeyword);
                                                        }
                                                    }
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                                N(SyntaxKind.IdentifierToken);
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
                                                    }
                                                    N(SyntaxKind.CommaToken);
                                                    N(SyntaxKind.TupleElement);
                                                    {
                                                        N(SyntaxKind.PredefinedType);
                                                        {
                                                            N(SyntaxKind.IntKeyword);
                                                        }
                                                    }
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                                N(SyntaxKind.IdentifierToken);
                                            }
                                            N(SyntaxKind.CloseParenToken);
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TuplesWithNamesInLambda()
        {
            var tree = UsingTree(@"
class C
{
    var x = ((string a, string) a, (int, int b) b) => { };
}", options: TestOptions.Regular);
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
                                    N(SyntaxKind.ParenthesizedLambdaExpression);
                                    {
                                        N(SyntaxKind.ParameterList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.Parameter);
                                            {
                                                N(SyntaxKind.TupleType);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.TupleElement);
                                                    {
                                                        N(SyntaxKind.PredefinedType);
                                                        {
                                                            N(SyntaxKind.StringKeyword);
                                                        }
                                                        N(SyntaxKind.IdentifierToken);
                                                    }
                                                    N(SyntaxKind.CommaToken);
                                                    N(SyntaxKind.TupleElement);
                                                    {
                                                        N(SyntaxKind.PredefinedType);
                                                        {
                                                            N(SyntaxKind.StringKeyword);
                                                        }
                                                    }
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                                N(SyntaxKind.IdentifierToken);
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
                                                    }
                                                    N(SyntaxKind.CommaToken);
                                                    N(SyntaxKind.TupleElement);
                                                    {
                                                        N(SyntaxKind.PredefinedType);
                                                        {
                                                            N(SyntaxKind.IntKeyword);
                                                        }
                                                        N(SyntaxKind.IdentifierToken);
                                                    }
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                                N(SyntaxKind.IdentifierToken);
                                            }
                                            N(SyntaxKind.CloseParenToken);
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TupleInParameters()
        {
            var tree = UsingTree(@"
class C
{
    void Goo((int, string) a)
    {
    }
}", options: TestOptions.Regular);
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
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.StringKeyword);
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.IdentifierToken);
                            }
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
        public void TupleTypeWithTooFewElements()
        {
            UsingTree(@"
class C
{
    void M(int x, () y, (int a) z) { }
}", options: TestOptions.Regular,
                // (4,20): error CS8124: Tuple must contain at least two elements.
                //     void M(int x, () y, (int a) z) { }
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(4, 20),
                // (4,31): error CS8124: Tuple must contain at least two elements.
                //     void M(int x, () y, (int a) z) { }
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(4, 31)
                );

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
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            N(SyntaxKind.CommaToken);
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
                                N(SyntaxKind.IdentifierToken, "y");
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
                                N(SyntaxKind.IdentifierToken, "z");
                            }
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
        public void TupleExpressionWithTooFewElements()
        {
            UsingTree(@"
class C
{
    object x = ((Alice: 1), ());
}", options: TestOptions.Regular,
                // (4,26): error CS8124: Tuple must contain at least two elements.
                //     object x = ((Alice: 1), ());
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(4, 26),
                // (4,30): error CS1525: Invalid expression term ')'
                //     object x = ((Alice: 1), ());
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(4, 30)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
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
                                N(SyntaxKind.IdentifierToken, "x");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.TupleExpression);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.TupleExpression);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                N(SyntaxKind.Argument);
                                                {
                                                    N(SyntaxKind.NameColon);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "Alice");
                                                        }
                                                        N(SyntaxKind.ColonToken);
                                                    }
                                                    N(SyntaxKind.NumericLiteralExpression);
                                                    {
                                                        N(SyntaxKind.NumericLiteralToken, "1");
                                                    }
                                                }
                                                M(SyntaxKind.CommaToken);
                                                M(SyntaxKind.Argument);
                                                {
                                                    M(SyntaxKind.IdentifierName);
                                                    {
                                                        M(SyntaxKind.IdentifierToken);
                                                    }
                                                }
                                                N(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.ParenthesizedExpression);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                M(SyntaxKind.IdentifierName);
                                                {
                                                    M(SyntaxKind.IdentifierToken);
                                                }
                                                N(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
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

        [Fact, WorkItem(13667, "https://github.com/dotnet/roslyn/issues/13667")]
        public void MissingShortTupleErrorWhenWarningPresent()
        {
            // Diff errors
            var test = @"
class Program
{
    object a = (x: 3l);
}
";
            ParseAndValidate(test,
                // (4,22): error CS8124: Tuple must contain at least two elements.
                //     object a = (x: 3l);
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(4, 22));

            CreateCompilation(test).VerifyDiagnostics(
                // (4,21): warning CS0078: The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
                //     object a = (x: 3l);
                Diagnostic(ErrorCode.WRN_LowercaseEllSuffix, "l").WithLocation(4, 21),
                // (4,22): error CS8124: Tuple must contain at least two elements.
                //     object a = (x: 3l);
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(4, 22));
        }

        [Fact, WorkItem(377111, "https://devdiv.visualstudio.com/0bdbc590-a062-4c3f-b0f6-9383f67865ee/_workitems?id=377111")]
        public void TernaryVersusDeclaration_01()
        {
            UsingStatement("return (i, isValid ? Errors.IsValid : Errors.HasErrors);");
            N(SyntaxKind.ReturnStatement);
            {
                N(SyntaxKind.ReturnKeyword);
                N(SyntaxKind.TupleExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "isValid");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Errors");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "IsValid");
                                }
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Errors");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "HasErrors");
                                }
                            }
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(377111, "https://devdiv.visualstudio.com/0bdbc590-a062-4c3f-b0f6-9383f67865ee/_workitems?id=377111")]
        public void TernaryVersusDeclaration_02()
        {
            UsingStatement("return (isValid ? Errors.IsValid : Errors.HasErrors, i);");
            N(SyntaxKind.ReturnStatement);
            {
                N(SyntaxKind.ReturnKeyword);
                N(SyntaxKind.TupleExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "isValid");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Errors");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "IsValid");
                                }
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Errors");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "HasErrors");
                                }
                            }
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(377111, "https://devdiv.visualstudio.com/0bdbc590-a062-4c3f-b0f6-9383f67865ee/_workitems?id=377111")]
        public void TernaryVersusDeclaration_03()
        {
            UsingStatement("return (i, a < b, c > d);");
            N(SyntaxKind.ReturnStatement);
            {
                N(SyntaxKind.ReturnKeyword);
                N(SyntaxKind.TupleExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.DeclarationExpression);
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
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "d");
                            }
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(377111, "https://devdiv.visualstudio.com/0bdbc590-a062-4c3f-b0f6-9383f67865ee/_workitems?id=377111")]
        public void TernaryVersusDeclaration_04()
        {
            UsingStatement("return (i, a < b, c > d.x);");
            N(SyntaxKind.ReturnStatement);
            {
                N(SyntaxKind.ReturnKeyword);
                N(SyntaxKind.TupleExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.CommaToken);
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
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "d");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                            }
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(377111, "https://devdiv.visualstudio.com/0bdbc590-a062-4c3f-b0f6-9383f67865ee/_workitems?id=377111")]
        public void TernaryVersusDeclaration_05()
        {
            UsingStatement("return (i, a < b, c > d && x);");
            N(SyntaxKind.ReturnStatement);
            {
                N(SyntaxKind.ReturnKeyword);
                N(SyntaxKind.TupleExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.CommaToken);
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
                        N(SyntaxKind.LogicalAndExpression);
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
                            N(SyntaxKind.AmpersandAmpersandToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }
    }
}
