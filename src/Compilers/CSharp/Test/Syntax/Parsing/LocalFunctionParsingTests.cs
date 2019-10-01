// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Linq;
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
}");
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
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "await");
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "L");
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
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "await");
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "L");
                                        M(SyntaxKind.BracketedArgumentList);
                                        {
                                            M(SyntaxKind.OpenBracketToken);
                                            M(SyntaxKind.CloseBracketToken);
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
}", options: TestOptions.RegularPreview);

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
}", TestOptions.RegularPreview);

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
        public void LocalFunctionAttribute_Error_LocalVariable()
        {
            var tree = UsingTree(@"
class C
{
    void M()
    {
        [A] object local;
    }
}", TestOptions.RegularPreview);

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
        public void LocalFunctionAttribute_Error_LocalVariable_MultipleDeclarators()
        {
            var tree = UsingTree(@"
class C
{
    void M()
    {
        [A] object local1, local2;
    }
}", TestOptions.RegularPreview);

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
}");

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
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("(", ")").WithLocation(11, 19),
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
        int local() { return 0; }
    }
}", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6));
            Assert.NotNull(file);
            Assert.False(file.DescendantNodes().Any(n => n.Kind() == SyntaxKind.LocalFunctionStatement && !n.ContainsDiagnostics));
            Assert.True(file.HasErrors);
            file.SyntaxTree.GetDiagnostics().Verify(
                // (6,13): error CS8059: Feature 'local functions' is not available in C# 6. Please use language version 7.0 or greater.
                //         int local() => 0;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "local").WithArguments("local functions", "7.0").WithLocation(6, 13),
                // (10,13): error CS8059: Feature 'local functions' is not available in C# 6. Please use language version 7.0 or greater.
                //         int local() { return 0; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "local").WithArguments("local functions", "7.0").WithLocation(10, 13)
                );

            Assert.Equal(0, file.SyntaxTree.Options.Features.Count);
            var c = Assert.IsType<ClassDeclarationSyntax>(file.Members.Single());
            Assert.Equal(2, c.Members.Count);
            var m = Assert.IsType<MethodDeclarationSyntax>(c.Members[0]);
            var s1 = Assert.IsType<LocalFunctionStatementSyntax>(m.Body.Statements[0]);
            Assert.True(s1.ContainsDiagnostics);

            var m2 = Assert.IsType<MethodDeclarationSyntax>(c.Members[1]);
            s1 = Assert.IsType<LocalFunctionStatementSyntax>(m.Body.Statements[0]);
            Assert.True(s1.ContainsDiagnostics);
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

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/10388")]
        public void LocalFunctionsWithAwait()
        {
            var file = ParseFile(@"class c
{
    void m1() { await await() => new await(); }
    void m2() { await () => new await(); }
    async void m3() { await () => new await(); }
    void m4() { async await() => new await(); }
}");

            Assert.NotNull(file);
            var c = (ClassDeclarationSyntax)file.Members.Single();
            Assert.Equal(4, c.Members.Count);

            {
                Assert.Equal(SyntaxKind.MethodDeclaration, c.Members[0].Kind());
                var m1 = (MethodDeclarationSyntax)c.Members[0];
                Assert.Equal(0, m1.Modifiers.Count);
                Assert.Equal(1, m1.Body.Statements.Count);
                Assert.Equal(SyntaxKind.LocalFunctionStatement, m1.Body.Statements[0].Kind());
                var s1 = (LocalFunctionStatementSyntax)m1.Body.Statements[0];
                Assert.False(s1.HasErrors);
                Assert.Equal(0, s1.Modifiers.Count);
                Assert.Equal("await", s1.ReturnType.ToString());
                Assert.Equal("await", s1.Identifier.ToString());
                Assert.Null(s1.TypeParameterList);
                Assert.Equal(0, s1.ParameterList.ParameterCount);
                Assert.Null(s1.Body);
                Assert.NotNull(s1.ExpressionBody);
            }

            {
                Assert.Equal(SyntaxKind.MethodDeclaration, c.Members[1].Kind());
                var m2 = (MethodDeclarationSyntax)c.Members[1];
                Assert.Equal(0, m2.Modifiers.Count);
                Assert.Equal(2, m2.Body.Statements.Count);
                Assert.Equal(SyntaxKind.ExpressionStatement, m2.Body.Statements[0].Kind());
                var s1 = (ExpressionStatementSyntax)m2.Body.Statements[0];
                Assert.Equal(SyntaxKind.InvocationExpression, s1.Expression.Kind());
                var e1 = (InvocationExpressionSyntax)s1.Expression;
                Assert.Equal("await", e1.Expression.ToString());
                Assert.Equal(0, e1.ArgumentList.Arguments.Count);
                Assert.True(s1.SemicolonToken.IsMissing);
                Assert.Equal("=> ", s1.GetTrailingTrivia().ToFullString());
            }

            {
                Assert.Equal(SyntaxKind.MethodDeclaration, c.Members[2].Kind());
                var m3 = (MethodDeclarationSyntax)c.Members[2];
                Assert.Equal(1, m3.Modifiers.Count);
                Assert.Equal("async", m3.Modifiers.Single().ToString());
                Assert.Equal(2, m3.Body.Statements.Count);
                Assert.Equal(SyntaxKind.ExpressionStatement, m3.Body.Statements[0].Kind());
                var s1 = (ExpressionStatementSyntax)m3.Body.Statements[0];
                Assert.Equal(SyntaxKind.AwaitExpression, s1.Expression.Kind());
                var e1 = (AwaitExpressionSyntax)s1.Expression;
                Assert.Equal(SyntaxKind.SimpleLambdaExpression, e1.Expression.Kind());
                Assert.True(s1.SemicolonToken.IsMissing);
                Assert.Equal("=> ", s1.GetTrailingTrivia().ToFullString());
            }
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

            var expected = new[]
            {
                // (5,9): error CS8652: The feature 'static local functions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         static void F() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "static").WithArguments("static local functions", "8.0").WithLocation(5, 9)
            };

            UsingDeclaration(text, options: TestOptions.Regular7_3, expected);
            checkNodes();

            UsingDeclaration(text, options: TestOptions.Regular8);
            checkNodes();

            UsingDeclaration(text, options: TestOptions.RegularPreview);
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

            var expected = new[]
            {
                // (5,9): error CS8652: The feature 'static local functions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         static async void F1() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "static").WithArguments("static local functions", "8.0").WithLocation(5, 9),
                // (6,15): error CS8652: The feature 'static local functions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         async static void F2() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "static").WithArguments("static local functions", "8.0").WithLocation(6, 15)
            };
            UsingDeclaration(text, options: TestOptions.Regular7_3, expected);
            checkNodes();

            UsingDeclaration(text, options: TestOptions.Regular8);
            checkNodes();

            UsingDeclaration(text, options: TestOptions.RegularPreview);
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

            var expected = new[]
            {
                // (5,9): error CS8652: The feature 'static local functions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         static static void F1() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "static").WithArguments("static local functions", "8.0").WithLocation(5, 9),
                // (5,16): error CS1031: Type expected
                //         static static void F1() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithArguments("static").WithLocation(5, 16),
                // (5,16): error CS8652: The feature 'static local functions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         static static void F1() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "static").WithArguments("static local functions", "8.0").WithLocation(5, 16),
                // (6,9): error CS8652: The feature 'static local functions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         static async static void F2() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "static").WithArguments("static local functions", "8.0").WithLocation(6, 9),
                // (6,22): error CS1031: Type expected
                //         static async static void F2() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithArguments("static").WithLocation(6, 22),
                // (6,22): error CS8652: The feature 'static local functions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         static async static void F2() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "static").WithArguments("static local functions", "8.0").WithLocation(6, 22)
            };

            UsingDeclaration(text, options: TestOptions.Regular7_3, expected);
            checkNodes();

            UsingDeclaration(text, options: TestOptions.Regular8,
                // (5,16): error CS1031: Type expected
                //         static static void F1() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithArguments("static").WithLocation(5, 16),
                // (6,22): error CS1031: Type expected
                //         static async static void F2() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithArguments("static").WithLocation(6, 22));
            checkNodes();

            UsingDeclaration(text, options: TestOptions.RegularPreview,
                // (5,16): error CS1031: Type expected
                //         static static void F1() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithArguments("static").WithLocation(5, 16),
                // (6,22): error CS1031: Type expected
                //         static async static void F2() { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithArguments("static").WithLocation(6, 22));
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
            var expected = new[]
            {
                // (5,9): error CS1547: Keyword 'void' cannot be used in this context
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(5, 9),
                // (5,14): error CS1001: Identifier expected
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "static").WithLocation(5, 14),
                // (5,14): error CS1002: ; expected
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "static").WithLocation(5, 14),
                // (5,14): error CS8652: The feature 'static local functions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "static").WithArguments("static local functions", "8.0").WithLocation(5, 14),
                // (5,22): error CS1001: Identifier expected
                //         void static F() { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(5, 22)
            };

            UsingDeclaration(text, options: TestOptions.Regular7_3, expected);

            expected = new[]
            {
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
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(5, 22)
            };

            UsingDeclaration(text, options: TestOptions.Regular8, expected);

            UsingDeclaration(text, options: TestOptions.RegularPreview, expected);
        }
    }
}
