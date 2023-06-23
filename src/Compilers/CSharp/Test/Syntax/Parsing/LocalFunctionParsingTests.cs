// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public class LocalFunctionParsingTests : ParsingTests
    {
        public LocalFunctionParsingTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        [WorkItem(13480, "https://github.com/dotnet/roslyn/issues/13480")]
        public void IncompleteLocalFunc()
        {
            UsingTree(@"
class C
{
    void M1()
    {
        await L<
    }
    void M2()
    {
        int L<
    }
    void M3()
    {
        int? L<
    }
    void M4()
    {
        await L(
    }
    void M5()
    {
        int L(
    }
    void M6()
    {
        int? L(
    }
}",
                // (6,17): error CS1525: Invalid expression term '}'
                //         await L<
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(6, 17),
                // (6,17): error CS1002: ; expected
                //         await L<
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 17),
                // (10,15): error CS1001: Identifier expected
                //         int L<
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(10, 15),
                // (10,15): error CS1003: Syntax error, '>' expected
                //         int L<
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(">").WithLocation(10, 15),
                // (10,15): error CS1003: Syntax error, '(' expected
                //         int L<
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(10, 15),
                // (10,15): error CS1026: ) expected
                //         int L<
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(10, 15),
                // (10,15): error CS1002: ; expected
                //         int L<
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(10, 15),
                // (14,16): error CS1001: Identifier expected
                //         int? L<
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(14, 16),
                // (14,16): error CS1003: Syntax error, '>' expected
                //         int? L<
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(">").WithLocation(14, 16),
                // (14,16): error CS1003: Syntax error, '(' expected
                //         int? L<
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(14, 16),
                // (14,16): error CS1026: ) expected
                //         int? L<
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(14, 16),
                // (14,16): error CS1002: ; expected
                //         int? L<
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(14, 16),
                // (18,17): error CS1026: ) expected
                //         await L(
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(18, 17),
                // (18,17): error CS1002: ; expected
                //         await L(
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(18, 17),
                // (22,15): error CS1026: ) expected
                //         int L(
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(22, 15),
                // (22,15): error CS1002: ; expected
                //         int L(
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(22, 15),
                // (26,16): error CS1026: ) expected
                //         int? L(
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(26, 16),
                // (26,16): error CS1002: ; expected
                //         int? L(
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(26, 16));
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
                        N(SyntaxKind.IdentifierToken, "M1");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.LessThanExpression);
                                {
                                    N(SyntaxKind.AwaitExpression);
                                    {
                                        N(SyntaxKind.AwaitKeyword);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "L");
                                        }
                                    }
                                    N(SyntaxKind.LessThanToken);
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M2");
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
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "L");
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
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M3");
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
                                N(SyntaxKind.NullableType);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.QuestionToken);
                                }
                                N(SyntaxKind.IdentifierToken, "L");
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
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M4");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.AwaitExpression);
                                {
                                    N(SyntaxKind.AwaitKeyword);
                                    N(SyntaxKind.InvocationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "L");
                                        }
                                        N(SyntaxKind.ArgumentList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            M(SyntaxKind.CloseParenToken);
                                        }
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M5");
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
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "L");
                                N(SyntaxKind.ParameterList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    M(SyntaxKind.CloseParenToken);
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M6");
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
                                N(SyntaxKind.NullableType);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.QuestionToken);
                                }
                                N(SyntaxKind.IdentifierToken, "L");
                                N(SyntaxKind.ParameterList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    M(SyntaxKind.CloseParenToken);
                                }
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
        }

        [Fact]
        [WorkItem(38801, "https://github.com/dotnet/roslyn/issues/38801")]
        public void LocalFunctionAttribute()
        {
            var tree = UsingTree(@"
class C
{
    void M()
    {
        [A]
        void local() { }

        [return: A]
        void local() { }

        [A]
        int local() => 42;

        [A][B] void local() { }
    }
}", options: TestOptions.Regular9);

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
                                N(SyntaxKind.AttributeList);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.Attribute);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.VoidKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "local");
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
                            N(SyntaxKind.LocalFunctionStatement);
                            {
                                N(SyntaxKind.AttributeList);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.AttributeTargetSpecifier);
                                    {
                                        N(SyntaxKind.ReturnKeyword);
                                        N(SyntaxKind.ColonToken);
                                    }
                                    N(SyntaxKind.Attribute);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.VoidKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "local");
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
                            N(SyntaxKind.LocalFunctionStatement);
                            {
                                N(SyntaxKind.AttributeList);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.Attribute);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "local");
                                N(SyntaxKind.ParameterList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
                                }
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
                            N(SyntaxKind.LocalFunctionStatement);
                            {
                                N(SyntaxKind.AttributeList);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.Attribute);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.AttributeList);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.Attribute);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.VoidKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "local");
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
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            tree.GetDiagnostics().Verify();
        }

        [Fact]
        [WorkItem(38801, "https://github.com/dotnet/roslyn/issues/38801")]
        public void LocalFunctionModifier_Error_LocalVariable()
        {
            var tree = UsingTree(@"
class C
{
    void M()
    {
        public object local;
    }
}", TestOptions.Regular9,
                // (5,6): error CS1513: } expected
                //     {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 6),
                // (8,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(8, 1));

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
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.ObjectKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "local");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            tree.GetDiagnostics().Verify(
                // (5,6): error CS1513: } expected
                //     {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 6),
                // (8,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(8, 1));
        }

        [Fact]
        [WorkItem(38801, "https://github.com/dotnet/roslyn/issues/38801")]
        public void LocalFunction_NoBody()
        {
            UsingTree(@"
class C
{
    void M()
    {
        void local();
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
                            N(SyntaxKind.LocalFunctionStatement);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.VoidKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "local");
                                N(SyntaxKind.ParameterList);
                                {
                                    N(SyntaxKind.OpenParenToken);
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
        [WorkItem(38801, "https://github.com/dotnet/roslyn/issues/38801")]
        public void LocalFunction_Extern()
        {
            const string code = @"
class C
{
    void M()
    {
        extern void local();
    }
}";

            CreateCompilation(code, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (6,21): error CS8112: Local function 'local()' must declare a body because it is not marked 'static extern'.
                //         extern void local();
                Diagnostic(ErrorCode.ERR_LocalFunctionMissingBody, "local").WithArguments("local()").WithLocation(6, 21),
                // (6,21): warning CS0626: Method, operator, or accessor 'local()' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //         extern void local();
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "local").WithArguments("local()").WithLocation(6, 21),
                // (6,21): warning CS8321: The local function 'local' is declared but never used
                //         extern void local();
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(6, 21));
            CreateCompilation(code, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                // (6,9): error CS8400: Feature 'extern local functions' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         extern void local();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "extern").WithArguments("extern local functions", "9.0").WithLocation(6, 9),
                // (6,21): error CS8112: Local function 'local()' must declare a body because it is not marked 'static extern'.
                //         extern void local();
                Diagnostic(ErrorCode.ERR_LocalFunctionMissingBody, "local").WithArguments("local()").WithLocation(6, 21),
                // (6,21): warning CS0626: Method, operator, or accessor 'local()' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //         extern void local();
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "local").WithArguments("local()").WithLocation(6, 21),
                // (6,21): warning CS8321: The local function 'local' is declared but never used
                //         extern void local();
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(6, 21));

            UsingTree(code, TestOptions.Regular9).GetDiagnostics().Verify();
            verifyTree();

            UsingTree(code, TestOptions.Regular8);
            verifyTree();

            void verifyTree()
            {
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
                                    N(SyntaxKind.ExternKeyword);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.VoidKeyword);
                                    }
                                    N(SyntaxKind.IdentifierToken, "local");
                                    N(SyntaxKind.ParameterList);
                                    {
                                        N(SyntaxKind.OpenParenToken);
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
        }

        [Fact]
        [WorkItem(38801, "https://github.com/dotnet/roslyn/issues/38801")]
        public void LocalFunction_Extern_Body()
        {
            const string code = @"
class C
{
    void M()
    {
        extern void local() { }
    }
}";
            CreateCompilation(code, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (6,21): error CS0179: 'local()' cannot be extern and declare a body
                //         extern void local() { }
                Diagnostic(ErrorCode.ERR_ExternHasBody, "local").WithArguments("local()").WithLocation(6, 21),
                // (6,21): warning CS8321: The local function 'local' is declared but never used
                //         extern void local() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(6, 21));
            CreateCompilation(code, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                // (6,9): error CS8400: Feature 'extern local functions' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         extern void local() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "extern").WithArguments("extern local functions", "9.0").WithLocation(6, 9),
                // (6,21): error CS0179: 'local()' cannot be extern and declare a body
                //         extern void local() { }
                Diagnostic(ErrorCode.ERR_ExternHasBody, "local").WithArguments("local()").WithLocation(6, 21),
                // (6,21): warning CS8321: The local function 'local' is declared but never used
                //         extern void local() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(6, 21));

            UsingTree(code, TestOptions.Regular9).GetDiagnostics().Verify();
            verifyTree();

            UsingTree(code, TestOptions.Regular8);
            verifyTree();

            void verifyTree()
            {
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
                                    N(SyntaxKind.ExternKeyword);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.VoidKeyword);
                                    }
                                    N(SyntaxKind.IdentifierToken, "local");
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
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.EndOfFileToken);
                }
                EOF();
            }
        }

        [Fact]
        [WorkItem(38801, "https://github.com/dotnet/roslyn/issues/38801")]
        public void LocalVariable_Extern()
        {
            const string statement = "extern object obj;";
            UsingStatement(statement, TestOptions.Regular9,
                // (1,1): error CS0106: The modifier 'extern' is not valid for this item
                // extern object obj;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "extern").WithArguments("extern").WithLocation(1, 1));
            verifyTree();

            UsingStatement(statement,
                // (1,1): error CS0106: The modifier 'extern' is not valid for this item
                // extern object obj;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "extern").WithArguments("extern").WithLocation(1, 1));
            verifyTree();

            void verifyTree()
            {
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.ExternKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "obj");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        [WorkItem(38801, "https://github.com/dotnet/roslyn/issues/38801")]
        public void LocalFunctionAttribute_Error_LocalVariable()
        {
            var tree = UsingTree(@"
class C
{
    void M()
    {
        [A] object local;
    }
}", TestOptions.Regular9);

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
                                N(SyntaxKind.AttributeList);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.Attribute);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.ObjectKeyword);
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "local");
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

            CreateCompilation(tree).VerifyDiagnostics(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A] object local;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,20): warning CS0168: The variable 'local' is declared but never used
                //         [A] object local;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "local").WithArguments("local").WithLocation(6, 20));
        }

        [Fact]
        [WorkItem(38801, "https://github.com/dotnet/roslyn/issues/38801")]
        public void LocalFunctionAttribute_Error_LocalVariable_MultipleDeclarators()
        {
            var tree = UsingTree(@"
class C
{
    void M()
    {
        [A] object local1, local2;
    }
}", TestOptions.Regular9);

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
                                N(SyntaxKind.AttributeList);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.Attribute);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.ObjectKeyword);
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "local1");
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "local2");
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

            CreateCompilation(tree).VerifyDiagnostics(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A] object local1, local2;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,20): warning CS0168: The variable 'local1' is declared but never used
                //         [A] object local1, local2;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "local1").WithArguments("local1").WithLocation(6, 20),
                // (6,28): warning CS0168: The variable 'local2' is declared but never used
                //         [A] object local1, local2;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "local2").WithArguments("local2").WithLocation(6, 28));
        }

        [Fact]
        [WorkItem(12280, "https://github.com/dotnet/roslyn/issues/12280")]
        public void LocalFunctionAttribute_Error_IncompleteMember()
        {
            var tree = UsingTree(@"
class C
{
    void M()
    {
        [A]
    }
}",
                // (6,12): error CS1525: Invalid expression term '}'
                //         [A]
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(6, 12),
                // (6,12): error CS1002: ; expected
                //         [A]
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 12));

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
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.AttributeList);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.Attribute);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
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

            tree.GetDiagnostics().Verify(
                // (6,12): error CS1525: Invalid expression term '}'
                //         [A]
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(6, 12),
                // (6,12): error CS1002: ; expected
                //         [A]
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 12)
            );
        }

        [Fact]
        [WorkItem(12280, "https://github.com/dotnet/roslyn/issues/12280")]
        public void LocalFuncWithWhitespace()
        {
            var file = ParseFile(@"
class C
{
    void Main()
    {
        int
            goo() => 5;

        int
            goo() { return 5; }

        int
            goo<T>() => 5;

        int
            goo<T>() { return 5; }

        int
            goo<T>() where T : IFace => 5;

        int
            goo<T>() where T : IFace { return 5; }
    }
}");
            Assert.NotNull(file);
            file.SyntaxTree.GetDiagnostics().Verify();

            var errorText = @"
class C
{
    void M()
    {
        int
            goo() where T : IFace => 5;
        int
            goo() where T : IFace { return 5; }
        int
            goo<T>) { }
    }
}";
            file = ParseFile(errorText);

            CreateCompilation(errorText).VerifyDiagnostics(
                // (11,19): error CS1003: Syntax error, '(' expected
                //             goo<T>) { }
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("(").WithLocation(11, 19),
                // (7,19): error CS0080: Constraints are not allowed on non-generic declarations
                //             goo() where T : IFace => 5;
                Diagnostic(ErrorCode.ERR_ConstraintOnlyAllowedOnGenericDecl, "where").WithLocation(7, 19),
                // (9,13): error CS0128: A local variable or function named 'goo' is already defined in this scope
                //             goo() where T : IFace { return 5; }
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "goo").WithArguments("goo").WithLocation(9, 13),
                // (9,19): error CS0080: Constraints are not allowed on non-generic declarations
                //             goo() where T : IFace { return 5; }
                Diagnostic(ErrorCode.ERR_ConstraintOnlyAllowedOnGenericDecl, "where").WithLocation(9, 19),
                // (11,13): error CS0128: A local variable or function named 'goo' is already defined in this scope
                //             goo<T>) { }
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "goo").WithArguments("goo").WithLocation(11, 13),
                // (11,13): error CS0161: 'goo<T>()': not all code paths return a value
                //             goo<T>) { }
                Diagnostic(ErrorCode.ERR_ReturnExpected, "goo").WithArguments("goo<T>()").WithLocation(11, 13),
                // (7,13): warning CS8321: The local function 'goo' is declared but never used
                //             goo() where T : IFace => 5;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "goo").WithArguments("goo").WithLocation(7, 13),
                // (9,13): warning CS8321: The local function 'goo' is declared but never used
                //             goo() where T : IFace { return 5; }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "goo").WithArguments("goo").WithLocation(9, 13),
                // (11,13): warning CS8321: The local function 'goo' is declared but never used
                //             goo<T>) { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "goo").WithArguments("goo").WithLocation(11, 13));

            var m = Assert.IsType<MethodDeclarationSyntax>(file.DescendantNodes()
                .Where(n => n.Kind() == SyntaxKind.MethodDeclaration)
                .Single());
            Assert.All(m.Body.Statements,
                s => Assert.Equal(SyntaxKind.LocalFunctionStatement, s.Kind()));
        }

        [Fact]
        public void NeverEndingTest()
        {
            var file = ParseFile(@"public class C {
    public void M() {
        async public virtual M() {}
        unsafe public M() {}
        async override M() {}
        unsafe private async override M() {}
        async virtual override sealed M() {}
    }
}");
            file.SyntaxTree.GetDiagnostics().Verify(
                // (3,9): error CS0106: The modifier 'async' is not valid for this item
                //         async public virtual M() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "async").WithArguments("async").WithLocation(3, 9),
                // (3,15): error CS0106: The modifier 'public' is not valid for this item
                //         async public virtual M() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "public").WithArguments("public").WithLocation(3, 15),
                // (3,22): error CS1031: Type expected
                //         async public virtual M() {}
                Diagnostic(ErrorCode.ERR_TypeExpected, "virtual").WithLocation(3, 22),
                // (3,22): error CS1001: Identifier expected
                //         async public virtual M() {}
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "virtual").WithLocation(3, 22),
                // (3,22): error CS1002: ; expected
                //         async public virtual M() {}
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "virtual").WithLocation(3, 22),
                // (3,22): error CS1513: } expected
                //         async public virtual M() {}
                Diagnostic(ErrorCode.ERR_RbraceExpected, "virtual").WithLocation(3, 22),
                // (9,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(9, 1));
        }

        [Fact]
        public void DiagnosticsWithoutExperimental()
        {
            var text = @"
class c
{
    void m()
    {
        int local() => 0;
    }
    void m2()
    {
        int local() { return 0; }
    }
}";

            var file = ParseFile(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6));
            Assert.NotNull(file);
            Assert.True(file.DescendantNodes().Any(n => n.Kind() == SyntaxKind.LocalFunctionStatement && !n.ContainsDiagnostics));
            Assert.False(file.HasErrors);
            file.SyntaxTree.GetDiagnostics().Verify();

            Assert.Equal(0, file.SyntaxTree.Options.Features.Count);
            var c = Assert.IsType<ClassDeclarationSyntax>(file.Members.Single());
            Assert.Equal(2, c.Members.Count);
            var m = Assert.IsType<MethodDeclarationSyntax>(c.Members[0]);
            var s1 = Assert.IsType<LocalFunctionStatementSyntax>(m.Body.Statements[0]);
            Assert.False(s1.ContainsDiagnostics);

            var m2 = Assert.IsType<MethodDeclarationSyntax>(c.Members[1]);
            s1 = Assert.IsType<LocalFunctionStatementSyntax>(m.Body.Statements[0]);
            Assert.False(s1.ContainsDiagnostics);

            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (2,7): warning CS8981: The type name 'c' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // class c
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "c").WithArguments("c").WithLocation(2, 7),
                // (6,13): error CS8059: Feature 'local functions' is not available in C# 6. Please use language version 7.0 or greater.
                //         int local() => 0;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "local").WithArguments("local functions", "7.0").WithLocation(6, 13),
                // (6,13): warning CS8321: The local function 'local' is declared but never used
                //         int local() => 0;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(6, 13),
                // (10,13): error CS8059: Feature 'local functions' is not available in C# 6. Please use language version 7.0 or greater.
                //         int local() { return 0; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "local").WithArguments("local functions", "7.0").WithLocation(10, 13),
                // (10,13): warning CS8321: The local function 'local' is declared but never used
                //         int local() { return 0; }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(10, 13));
        }

        [Fact]
        public void NodesWithExperimental()
        {
            // Experimental nodes should only appear when experimental are
            // turned on in parse options
            var file = ParseFile(@"
class c
{
    void m()
    {
        int local() => 0;
    }
    void m2()
    {
        int local()
        {
            return 0;
        }
    }
}");

            Assert.NotNull(file);
            Assert.False(file.HasErrors);
            Assert.Equal(0, file.SyntaxTree.Options.Features.Count);
            var c = Assert.IsType<ClassDeclarationSyntax>(file.Members.Single());
            Assert.Equal(2, c.Members.Count);
            var m = Assert.IsType<MethodDeclarationSyntax>(c.Members[0]);
            var s1 = Assert.IsType<LocalFunctionStatementSyntax>(m.Body.Statements[0]);
            Assert.Equal(SyntaxKind.PredefinedType, s1.ReturnType.Kind());
            Assert.Equal("int", s1.ReturnType.ToString());
            Assert.Equal("local", s1.Identifier.ToString());
            Assert.NotNull(s1.ParameterList);
            Assert.Empty(s1.ParameterList.Parameters);
            Assert.NotNull(s1.ExpressionBody);
            Assert.Equal(SyntaxKind.NumericLiteralExpression, s1.ExpressionBody.Expression.Kind());

            var m2 = Assert.IsType<MethodDeclarationSyntax>(c.Members[1]);
            s1 = Assert.IsType<LocalFunctionStatementSyntax>(m2.Body.Statements[0]);
            Assert.Equal(SyntaxKind.PredefinedType, s1.ReturnType.Kind());
            Assert.Equal("int", s1.ReturnType.ToString());
            Assert.Equal("local", s1.Identifier.ToString());
            Assert.NotNull(s1.ParameterList);
            Assert.Empty(s1.ParameterList.Parameters);
            Assert.Null(s1.ExpressionBody);
            Assert.NotNull(s1.Body);
            var s2 = Assert.IsType<ReturnStatementSyntax>(s1.Body.Statements.Single());
            Assert.Equal(SyntaxKind.NumericLiteralExpression, s2.Expression.Kind());
        }

        [Fact]
        public void LocalFunctionsWithAwait()
        {
            UsingTree(@"
class c
{
    void m1() { await await() => new await(); }
    void m2() { await () => new await(); }
    async void m3() { await () => new await(); }
    void m4() { async await() => new await(); }
    async void m5() { await async () => new await(); }
}",
                // (6,30): error CS1525: Invalid expression term ')'
                //     async void m3() { await () => new await(); }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(6, 30),
                // (6,32): error CS1002: ; expected
                //     async void m3() { await () => new await(); }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "=>").WithLocation(6, 32),
                // (6,32): error CS1513: } expected
                //     async void m3() { await () => new await(); }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "=>").WithLocation(6, 32),
                // (6,39): error CS4003: 'await' cannot be used as an identifier within an async method or lambda expression
                //     async void m3() { await () => new await(); }
                Diagnostic(ErrorCode.ERR_BadAwaitAsIdentifier, "await").WithLocation(6, 39),
                // (8,38): error CS1002: ; expected
                //     async void m5() { await async () => new await(); }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "=>").WithLocation(8, 38),
                // (8,38): error CS1513: } expected
                //     async void m5() { await async () => new await(); }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "=>").WithLocation(8, 38),
                // (8,45): error CS4003: 'await' cannot be used as an identifier within an async method or lambda expression
                //     async void m5() { await async () => new await(); }
                Diagnostic(ErrorCode.ERR_BadAwaitAsIdentifier, "await").WithLocation(8, 45));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "c");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "m1");
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
                                    N(SyntaxKind.IdentifierToken, "await");
                                }
                                N(SyntaxKind.IdentifierToken, "await");
                                N(SyntaxKind.ParameterList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.ArrowExpressionClause);
                                {
                                    N(SyntaxKind.EqualsGreaterThanToken);
                                    N(SyntaxKind.ObjectCreationExpression);
                                    {
                                        N(SyntaxKind.NewKeyword);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "await");
                                        }
                                        N(SyntaxKind.ArgumentList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "m2");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.ParenthesizedLambdaExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "await");
                                    }
                                    N(SyntaxKind.ParameterList);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.EqualsGreaterThanToken);
                                    N(SyntaxKind.ObjectCreationExpression);
                                    {
                                        N(SyntaxKind.NewKeyword);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "await");
                                        }
                                        N(SyntaxKind.ArgumentList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "m3");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.AwaitExpression);
                                {
                                    N(SyntaxKind.AwaitKeyword);
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
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.ObjectCreationExpression);
                                {
                                    N(SyntaxKind.NewKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "await");
                                    }
                                    N(SyntaxKind.ArgumentList);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "m4");
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
                                    N(SyntaxKind.IdentifierToken, "async");
                                }
                                N(SyntaxKind.IdentifierToken, "await");
                                N(SyntaxKind.ParameterList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.ArrowExpressionClause);
                                {
                                    N(SyntaxKind.EqualsGreaterThanToken);
                                    N(SyntaxKind.ObjectCreationExpression);
                                    {
                                        N(SyntaxKind.NewKeyword);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "await");
                                        }
                                        N(SyntaxKind.ArgumentList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "m5");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.AwaitExpression);
                                {
                                    N(SyntaxKind.AwaitKeyword);
                                    N(SyntaxKind.InvocationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "async");
                                        }
                                        N(SyntaxKind.ArgumentList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.ObjectCreationExpression);
                                {
                                    N(SyntaxKind.NewKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "await");
                                    }
                                    N(SyntaxKind.ArgumentList);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.CloseParenToken);
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

        [WorkItem(13090, "https://github.com/dotnet/roslyn/issues/13090")]
        [Fact]
        public void AsyncVariable()
        {
            var file = ParseFile(
@"class C
{
    static void F(object async)
    {
        async.F();
        async->F();
        async = null;
        async += 1;
        async++;
        async[0] = null;
        async();
    }
    static void G()
    {
        async async;
        async.T t;
        async<object> u;
    }
    static void H()
    {
        async async() => 0;
        async F<T>() => 1;
        async async G<T>() { }
        async.T t() { }
        async<object> u(object o) => o;
    }
}");
            file.SyntaxTree.GetDiagnostics().Verify();
        }

        [Fact]
        public void StaticFunctions()
        {
            const string text =
@"class Program
{
    void M()
    {
        static void F() { }
    }
}";
            CreateCompilation(text, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (5,9): error CS8370: Feature 'static local functions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         static void F() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "static").WithArguments("static local functions", "8.0").WithLocation(5, 9),
                // (5,21): warning CS8321: The local function 'F' is declared but never used
                //         static void F() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "F").WithArguments("F").WithLocation(5, 21));
            CreateCompilation(text, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                // (5,21): warning CS8321: The local function 'F' is declared but never used
                //         static void F() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "F").WithArguments("F").WithLocation(5, 21));
            CreateCompilation(text, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (5,21): warning CS8321: The local function 'F' is declared but never used
                //         static void F() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "F").WithArguments("F").WithLocation(5, 21));

            UsingDeclaration(text, options: TestOptions.Regular7_3);
            checkNodes();

            UsingDeclaration(text, options: TestOptions.Regular8);
            checkNodes();

            UsingDeclaration(text, options: TestOptions.Regular9);
            checkNodes();

            void checkNodes()
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Program");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.VoidKeyword);
                        {
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
                                    N(SyntaxKind.StaticKeyword);
                                    N(SyntaxKind.PredefinedType);
                                    N(SyntaxKind.VoidKeyword);
                                    N(SyntaxKind.IdentifierToken, "F");
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
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                EOF();
            }
        }

        [Fact]
        public void AsyncStaticFunctions()
        {
            const string text =
@"class Program
{
    void M()
    {
        static async void F1() { }
        async static void F2() { }
    }
}";
            CreateCompilation(text, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (5,9): error CS8370: Feature 'static local functions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         static async void F1() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "static").WithArguments("static local functions", "8.0").WithLocation(5, 9),
                // (5,27): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         static async void F1() { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "F1").WithLocation(5, 27),
                // (5,27): warning CS8321: The local function 'F1' is declared but never used
                //         static async void F1() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "F1").WithArguments("F1").WithLocation(5, 27),
                // (6,15): error CS8370: Feature 'static local functions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         async static void F2() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "static").WithArguments("static local functions", "8.0").WithLocation(6, 15),
                // (6,27): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         async static void F2() { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "F2").WithLocation(6, 27),
                // (6,27): warning CS8321: The local function 'F2' is declared but never used
                //         async static void F2() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "F2").WithArguments("F2").WithLocation(6, 27));
            CreateCompilation(text, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                // (5,27): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         static async void F1() { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "F1").WithLocation(5, 27),
                // (5,27): warning CS8321: The local function 'F1' is declared but never used
                //         static async void F1() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "F1").WithArguments("F1").WithLocation(5, 27),
                // (6,27): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         async static void F2() { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "F2").WithLocation(6, 27),
                // (6,27): warning CS8321: The local function 'F2' is declared but never used
                //         async static void F2() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "F2").WithArguments("F2").WithLocation(6, 27));
            CreateCompilation(text, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (5,27): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         static async void F1() { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "F1").WithLocation(5, 27),
                // (5,27): warning CS8321: The local function 'F1' is declared but never used
                //         static async void F1() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "F1").WithArguments("F1").WithLocation(5, 27),
                // (6,27): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         async static void F2() { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "F2").WithLocation(6, 27),
                // (6,27): warning CS8321: The local function 'F2' is declared but never used
                //         async static void F2() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "F2").WithArguments("F2").WithLocation(6, 27));

            UsingDeclaration(text, options: TestOptions.Regular7_3);
            checkNodes();

            UsingDeclaration(text, options: TestOptions.Regular8);
            checkNodes();

            UsingDeclaration(text, options: TestOptions.Regular9);
            checkNodes();

            void checkNodes()
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Program");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.VoidKeyword);
                        {
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
                                    N(SyntaxKind.StaticKeyword);
                                    N(SyntaxKind.AsyncKeyword);
                                    N(SyntaxKind.PredefinedType);
                                    N(SyntaxKind.VoidKeyword);
                                    N(SyntaxKind.IdentifierToken, "F1");
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
                                N(SyntaxKind.LocalFunctionStatement);
                                {
                                    N(SyntaxKind.AsyncKeyword);
                                    N(SyntaxKind.StaticKeyword);
                                    N(SyntaxKind.PredefinedType);
                                    N(SyntaxKind.VoidKeyword);
                                    N(SyntaxKind.IdentifierToken, "F2");
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
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                EOF();
            }
        }

        [Fact]
        public void DuplicateStatic()
        {
            const string text =
@"class Program
{
    void M()
    {
        static static void F1() { }
        static async static void F2() { }
    }
}";
            CreateCompilation(text, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (5,9): error CS8370: Feature 'static local functions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         static static void F1() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "static").WithArguments("static local functions", "8.0").WithLocation(5, 9),
                // (5,16): error CS1031: Type expected
                //         static static void F1() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithLocation(5, 16),
                // (5,16): error CS1004: Duplicate 'static' modifier
                //         static static void F1() { }
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "static").WithArguments("static").WithLocation(5, 16),
                // (5,16): error CS8370: Feature 'static local functions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         static static void F1() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "static").WithArguments("static local functions", "8.0").WithLocation(5, 16),
                // (5,28): warning CS8321: The local function 'F1' is declared but never used
                //         static static void F1() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "F1").WithArguments("F1").WithLocation(5, 28),
                // (6,9): error CS8370: Feature 'static local functions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         static async static void F2() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "static").WithArguments("static local functions", "8.0").WithLocation(6, 9),
                // (6,22): error CS1031: Type expected
                //         static async static void F2() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithLocation(6, 22),
                // (6,22): error CS1004: Duplicate 'static' modifier
                //         static async static void F2() { }
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "static").WithArguments("static").WithLocation(6, 22),
                // (6,22): error CS8370: Feature 'static local functions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         static async static void F2() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "static").WithArguments("static local functions", "8.0").WithLocation(6, 22),
                // (6,34): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         static async static void F2() { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "F2").WithLocation(6, 34),
                // (6,34): warning CS8321: The local function 'F2' is declared but never used
                //         static async static void F2() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "F2").WithArguments("F2").WithLocation(6, 34));
            CreateCompilation(text, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                // (5,16): error CS1031: Type expected
                //         static static void F1() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithLocation(5, 16),
                // (5,16): error CS1004: Duplicate 'static' modifier
                //         static static void F1() { }
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "static").WithArguments("static").WithLocation(5, 16),
                // (5,28): warning CS8321: The local function 'F1' is declared but never used
                //         static static void F1() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "F1").WithArguments("F1").WithLocation(5, 28),
                // (6,22): error CS1031: Type expected
                //         static async static void F2() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithLocation(6, 22),
                // (6,22): error CS1004: Duplicate 'static' modifier
                //         static async static void F2() { }
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "static").WithArguments("static").WithLocation(6, 22),
                // (6,34): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         static async static void F2() { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "F2").WithLocation(6, 34),
                // (6,34): warning CS8321: The local function 'F2' is declared but never used
                //         static async static void F2() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "F2").WithArguments("F2").WithLocation(6, 34));
            CreateCompilation(text, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (5,16): error CS1031: Type expected
                //         static static void F1() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithLocation(5, 16),
                // (5,16): error CS1004: Duplicate 'static' modifier
                //         static static void F1() { }
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "static").WithArguments("static").WithLocation(5, 16),
                // (5,28): warning CS8321: The local function 'F1' is declared but never used
                //         static static void F1() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "F1").WithArguments("F1").WithLocation(5, 28),
                // (6,22): error CS1031: Type expected
                //         static async static void F2() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithLocation(6, 22),
                // (6,22): error CS1004: Duplicate 'static' modifier
                //         static async static void F2() { }
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "static").WithArguments("static").WithLocation(6, 22),
                // (6,34): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         static async static void F2() { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "F2").WithLocation(6, 34),
                // (6,34): warning CS8321: The local function 'F2' is declared but never used
                //         static async static void F2() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "F2").WithArguments("F2").WithLocation(6, 34));

            UsingDeclaration(text, options: TestOptions.Regular7_3,
                // (5,16): error CS1031: Type expected
                //         static static void F1() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithLocation(5, 16),
                // (6,22): error CS1031: Type expected
                //         static async static void F2() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithLocation(6, 22));
            checkNodes();

            UsingDeclaration(text, options: TestOptions.Regular8,
                // (5,16): error CS1031: Type expected
                //         static static void F1() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithLocation(5, 16),
                // (6,22): error CS1031: Type expected
                //         static async static void F2() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithLocation(6, 22));
            checkNodes();

            UsingDeclaration(text, options: TestOptions.Regular9,
                // (5,16): error CS1031: Type expected
                //         static static void F1() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithLocation(5, 16),
                // (6,22): error CS1031: Type expected
                //         static async static void F2() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithLocation(6, 22));
            checkNodes();

            void checkNodes()
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Program");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.VoidKeyword);
                        {
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
                                    N(SyntaxKind.StaticKeyword);
                                    N(SyntaxKind.StaticKeyword);
                                    N(SyntaxKind.PredefinedType);
                                    N(SyntaxKind.VoidKeyword);
                                    N(SyntaxKind.IdentifierToken, "F1");
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
                                N(SyntaxKind.LocalFunctionStatement);
                                {
                                    N(SyntaxKind.StaticKeyword);
                                    N(SyntaxKind.AsyncKeyword);
                                    N(SyntaxKind.StaticKeyword);
                                    N(SyntaxKind.PredefinedType);
                                    N(SyntaxKind.VoidKeyword);
                                    N(SyntaxKind.IdentifierToken, "F2");
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
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                EOF();
            }
        }

        [Fact, WorkItem(32106, "https://github.com/dotnet/roslyn/issues/32106")]
        public void DuplicateAsyncs1()
        {
            const string text = """
                class Program
                {
                    void M()
                    {
                        #pragma warning disable 1998, 8321
                        async async void F() { }
                    }
                }
                """;

            CreateCompilation(text).VerifyDiagnostics(
                // (6,15): error CS1031: Type expected
                //         async async void F() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "async").WithLocation(6, 15),
                // (6,15): error CS1004: Duplicate 'async' modifier
                //         async async void F() { }
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "async").WithArguments("async").WithLocation(6, 15));

            UsingDeclaration(text, options: TestOptions.Regular9,
                // (6,15): error CS1031: Type expected
                //         async async void F() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "async").WithLocation(6, 15));
            checkNodes();

            void checkNodes()
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Program");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.VoidKeyword);
                        {
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
                                    N(SyntaxKind.AsyncKeyword);
                                    N(SyntaxKind.AsyncKeyword);
                                    N(SyntaxKind.PredefinedType);
                                    N(SyntaxKind.VoidKeyword);
                                    N(SyntaxKind.IdentifierToken, "F");
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
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                EOF();
            }
        }

        [Fact, WorkItem(32106, "https://github.com/dotnet/roslyn/issues/32106")]
        public void DuplicateAsyncs2()
        {
            const string text = """
                class Program
                {
                    void M()
                    {
                        #pragma warning disable 1998, 8321
                        async async async void F() { }
                    }
                }
                """;

            CreateCompilation(text).VerifyDiagnostics(
                // (6,15): error CS1031: Type expected
                //         async async async void F() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "async").WithLocation(6, 15),
                // (6,15): error CS1004: Duplicate 'async' modifier
                //         async async async void F() { }
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "async").WithArguments("async").WithLocation(6, 15),
                // (6,21): error CS1031: Type expected
                //         async async async void F() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "async").WithLocation(6, 21));

            UsingDeclaration(text, options: TestOptions.Regular9,
                // (6,15): error CS1031: Type expected
                //         async async async void F() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "async").WithLocation(6, 15),
                // (6,21): error CS1031: Type expected
                //         async async async void F() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "async").WithLocation(6, 21));
            checkNodes();

            void checkNodes()
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Program");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.VoidKeyword);
                        {
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
                                    N(SyntaxKind.AsyncKeyword);
                                    N(SyntaxKind.AsyncKeyword);
                                    N(SyntaxKind.AsyncKeyword);
                                    N(SyntaxKind.PredefinedType);
                                    N(SyntaxKind.VoidKeyword);
                                    N(SyntaxKind.IdentifierToken, "F");
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
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                EOF();
            }
        }

        [Fact, WorkItem(32106, "https://github.com/dotnet/roslyn/issues/32106")]
        public void DuplicateAsyncs3()
        {
            const string text = """
                class Program
                {
                    void M()
                    {
                        #pragma warning disable 1998, 8321
                        async async async async void F() { }
                    }
                }
                """;

            CreateCompilation(text).VerifyDiagnostics(
                // (6,15): error CS1031: Type expected
                //         async async async async void F() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "async").WithLocation(6, 15),
                // (6,15): error CS1004: Duplicate 'async' modifier
                //         async async async async void F() { }
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "async").WithArguments("async").WithLocation(6, 15),
                // (6,21): error CS1031: Type expected
                //         async async async async void F() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "async").WithLocation(6, 21),
                // (6,27): error CS1031: Type expected
                //         async async async async void F() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "async").WithLocation(6, 27));

            UsingDeclaration(text, options: TestOptions.Regular9,
                // (6,15): error CS1031: Type expected
                //         async async async async void F() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "async").WithLocation(6, 15),
                // (6,21): error CS1031: Type expected
                //         async async async async void F() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "async").WithLocation(6, 21),
                // (6,27): error CS1031: Type expected
                //         async async async async void F() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "async").WithLocation(6, 27));
            checkNodes();

            void checkNodes()
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Program");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.VoidKeyword);
                        {
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
                                    N(SyntaxKind.AsyncKeyword);
                                    N(SyntaxKind.AsyncKeyword);
                                    N(SyntaxKind.AsyncKeyword);
                                    N(SyntaxKind.AsyncKeyword);
                                    N(SyntaxKind.PredefinedType);
                                    N(SyntaxKind.VoidKeyword);
                                    N(SyntaxKind.IdentifierToken, "F");
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
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                EOF();
            }
        }

        [Fact, WorkItem(32106, "https://github.com/dotnet/roslyn/issues/32106")]
        public void DuplicateAsyncs4()
        {
            const string text = """
                class Program
                {
                    void M()
                    {
                        #pragma warning disable 1998, 8321
                        async async async async async void F() { }
                    }
                }
                """;

            CreateCompilation(text).VerifyDiagnostics(
                // (6,15): error CS1031: Type expected
                //         async async async async async void F() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "async").WithLocation(6, 15),
                // (6,15): error CS1004: Duplicate 'async' modifier
                //         async async async async async void F() { }
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "async").WithArguments("async").WithLocation(6, 15),
                // (6,21): error CS1031: Type expected
                //         async async async async async void F() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "async").WithLocation(6, 21),
                // (6,27): error CS1031: Type expected
                //         async async async async async void F() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "async").WithLocation(6, 27),
                // (6,33): error CS1031: Type expected
                //         async async async async async void F() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "async").WithLocation(6, 33));

            UsingDeclaration(text, options: TestOptions.Regular9,
                // (6,15): error CS1031: Type expected
                //         async async async async async void F() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "async").WithLocation(6, 15),
                // (6,21): error CS1031: Type expected
                //         async async async async async void F() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "async").WithLocation(6, 21),
                // (6,27): error CS1031: Type expected
                //         async async async async async void F() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "async").WithLocation(6, 27),
                // (6,33): error CS1031: Type expected
                //         async async async async async void F() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "async").WithLocation(6, 33));
            checkNodes();

            void checkNodes()
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Program");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.VoidKeyword);
                        {
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
                                    N(SyntaxKind.AsyncKeyword);
                                    N(SyntaxKind.AsyncKeyword);
                                    N(SyntaxKind.AsyncKeyword);
                                    N(SyntaxKind.AsyncKeyword);
                                    N(SyntaxKind.AsyncKeyword);
                                    N(SyntaxKind.PredefinedType);
                                    N(SyntaxKind.VoidKeyword);
                                    N(SyntaxKind.IdentifierToken, "F");
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
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                EOF();
            }
        }

        [Fact]
        public void ReturnTypeBeforeStatic()
        {
            const string text =
@"class Program
{
    void M()
    {
        void static F() { }
    }
}";
            CreateCompilation(text, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (5,9): error CS1547: Keyword 'void' cannot be used in this context
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(5, 9),
                // (5,14): error CS1001: Identifier expected
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "static").WithLocation(5, 14),
                // (5,14): error CS1002: ; expected
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "static").WithLocation(5, 14),
                // (5,14): error CS8370: Feature 'static local functions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "static").WithArguments("static local functions", "8.0").WithLocation(5, 14),
                // (5,21): error CS0246: The type or namespace name 'F' could not be found (are you missing a using directive or an assembly reference?)
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "F").WithArguments("F").WithLocation(5, 21),
                // (5,22): error CS1001: Identifier expected
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(5, 22),
                // (5,22): error CS0161: '()': not all code paths return a value
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_ReturnExpected, "").WithArguments("()").WithLocation(5, 22));
            CreateCompilation(text, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                // (5,9): error CS1547: Keyword 'void' cannot be used in this context
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(5, 9),
                // (5,14): error CS1001: Identifier expected
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "static").WithLocation(5, 14),
                // (5,14): error CS1002: ; expected
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "static").WithLocation(5, 14),
                // (5,21): error CS0246: The type or namespace name 'F' could not be found (are you missing a using directive or an assembly reference?)
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "F").WithArguments("F").WithLocation(5, 21),
                // (5,22): error CS1001: Identifier expected
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(5, 22),
                // (5,22): error CS0161: '()': not all code paths return a value
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_ReturnExpected, "").WithArguments("()").WithLocation(5, 22));
            CreateCompilation(text, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (5,9): error CS1547: Keyword 'void' cannot be used in this context
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(5, 9),
                // (5,14): error CS1001: Identifier expected
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "static").WithLocation(5, 14),
                // (5,14): error CS1002: ; expected
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "static").WithLocation(5, 14),
                // (5,21): error CS0246: The type or namespace name 'F' could not be found (are you missing a using directive or an assembly reference?)
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "F").WithArguments("F").WithLocation(5, 21),
                // (5,22): error CS1001: Identifier expected
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(5, 22),
                // (5,22): error CS0161: '()': not all code paths return a value
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_ReturnExpected, "").WithArguments("()").WithLocation(5, 22));

            UsingDeclaration(text, options: TestOptions.Regular7_3,
                // (5,9): error CS1547: Keyword 'void' cannot be used in this context
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(5, 9),
                // (5,14): error CS1001: Identifier expected
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "static").WithLocation(5, 14),
                // (5,14): error CS1002: ; expected
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "static").WithLocation(5, 14),
                // (5,22): error CS1001: Identifier expected
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(5, 22));
            verify();

            UsingDeclaration(text, options: TestOptions.Regular8,
                // (5,9): error CS1547: Keyword 'void' cannot be used in this context
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(5, 9),
                // (5,14): error CS1001: Identifier expected
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "static").WithLocation(5, 14),
                // (5,14): error CS1002: ; expected
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "static").WithLocation(5, 14),
                // (5,22): error CS1001: Identifier expected
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(5, 22));
            verify();

            UsingDeclaration(text, options: TestOptions.Regular9,
                // (5,9): error CS1547: Keyword 'void' cannot be used in this context
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(5, 9),
                // (5,14): error CS1001: Identifier expected
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "static").WithLocation(5, 14),
                // (5,14): error CS1002: ; expected
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "static").WithLocation(5, 14),
                // (5,22): error CS1001: Identifier expected
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(5, 22));
            verify();

            void verify()
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Program");
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
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.VoidKeyword);
                                    }
                                    M(SyntaxKind.VariableDeclarator);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.LocalFunctionStatement);
                            {
                                N(SyntaxKind.StaticKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "F");
                                }
                                M(SyntaxKind.IdentifierToken);
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
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                EOF();
            }
        }

        [Fact]
        public void ParameterScope_InMethodAttributeNameOf_AnonymousFunctionWithImplicitParameters()
        {
            var source = @"
class C
{
    void M()
    {
        System.Func<int, int> x = [My(nameof(parameter))] delegate { return 1; }
    }
}

public class MyAttribute : System.Attribute
{
    public MyAttribute(string name1) { }
}
";
            UsingTree(source,
                // (6,59): error CS1002: ; expected
                //         System.Func<int, int> x = [My(nameof(parameter))] delegate { return 1; }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "delegate").WithLocation(6, 59),
                // (6,81): error CS1002: ; expected
                //         System.Func<int, int> x = [My(nameof(parameter))] delegate { return 1; }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 81));

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
                                        N(SyntaxKind.IdentifierToken, "x");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.CollectionExpression);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                N(SyntaxKind.ExpressionElement);
                                                {
                                                    N(SyntaxKind.InvocationExpression);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "My");
                                                        }
                                                        N(SyntaxKind.ArgumentList);
                                                        {
                                                            N(SyntaxKind.OpenParenToken);
                                                            N(SyntaxKind.Argument);
                                                            {
                                                                N(SyntaxKind.InvocationExpression);
                                                                {
                                                                    N(SyntaxKind.IdentifierName);
                                                                    {
                                                                        N(SyntaxKind.IdentifierToken, "nameof");
                                                                    }
                                                                    N(SyntaxKind.ArgumentList);
                                                                    {
                                                                        N(SyntaxKind.OpenParenToken);
                                                                        N(SyntaxKind.Argument);
                                                                        {
                                                                            N(SyntaxKind.IdentifierName);
                                                                            {
                                                                                N(SyntaxKind.IdentifierToken, "parameter");
                                                                            }
                                                                        }
                                                                        N(SyntaxKind.CloseParenToken);
                                                                    }
                                                                }
                                                            }
                                                            N(SyntaxKind.CloseParenToken);
                                                        }
                                                    }
                                                }
                                                N(SyntaxKind.CloseBracketToken);
                                            }
                                        }
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.AnonymousMethodExpression);
                                {
                                    N(SyntaxKind.DelegateKeyword);
                                    N(SyntaxKind.Block);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.ReturnStatement);
                                        {
                                            N(SyntaxKind.ReturnKeyword);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "1");
                                            }
                                            N(SyntaxKind.SemicolonToken);
                                        }
                                        N(SyntaxKind.CloseBraceToken);
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "MyAttribute");
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
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
                                    N(SyntaxKind.IdentifierToken, "Attribute");
                                }
                            }
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.ConstructorDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.IdentifierToken, "MyAttribute");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "name1");
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
    }
}
