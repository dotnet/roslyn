// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.StatementAttributes)]
    public class StatementAttributeParsingTests : ParsingTests
    {
        public StatementAttributeParsingTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void AttributeOnBlock()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]{}
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.Block);
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
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnEmptyStatement()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A];
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.EmptyStatement);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnLabeledStatement()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]
        bar:
            Goo();
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.LabeledStatement);
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
                                N(SyntaxKind.IdentifierToken, "bar");
                                N(SyntaxKind.ColonToken);
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
                                    N(SyntaxKind.SemicolonToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (7,9): warning CS0164: This label has not been referenced
                //         bar:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "bar").WithLocation(7, 9));
        }

        [Fact]
        public void AttributeOnGotoStatement()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]
        goto bar;
        bar:
            Goo();
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.GotoStatement);
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
                                N(SyntaxKind.GotoKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "bar");
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.LabeledStatement);
                            {
                                N(SyntaxKind.IdentifierToken, "bar");
                                N(SyntaxKind.ColonToken);
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
                                    N(SyntaxKind.SemicolonToken);
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


            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnBreakStatement()
        {
            var test = @"
class C
{
    void Goo()
    {
        while (true)
        {
            [A]
            break;
        }
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.WhileStatement);
                            {
                                N(SyntaxKind.WhileKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TrueLiteralExpression);
                                {
                                    N(SyntaxKind.TrueKeyword);
                                }
                                N(SyntaxKind.CloseParenToken);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.BreakStatement);
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
                                        N(SyntaxKind.BreakKeyword);
                                        N(SyntaxKind.SemicolonToken);
                                    }
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (8,13): error CS7014: Attributes are not valid in this context.
                //             [A]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(8, 13));
        }

        [Fact]
        public void AttributeOnContinueStatement()
        {
            var test = @"
class C
{
    void Goo()
    {
        while (true)
        {
            [A]
            continue;
        }
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.WhileStatement);
                            {
                                N(SyntaxKind.WhileKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TrueLiteralExpression);
                                {
                                    N(SyntaxKind.TrueKeyword);
                                }
                                N(SyntaxKind.CloseParenToken);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.ContinueStatement);
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
                                        N(SyntaxKind.ContinueKeyword);
                                        N(SyntaxKind.SemicolonToken);
                                    }
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (8,13): error CS7014: Attributes are not valid in this context.
                //             [A]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(8, 13));
        }

        [Fact]
        public void AttributeOnReturn()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]return;
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.ReturnStatement);
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
                                N(SyntaxKind.ReturnKeyword);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnThrow()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]throw;
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.ThrowStatement);
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
                                N(SyntaxKind.ThrowKeyword);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]throw;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                //         [A]throw;
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnYieldReturn()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]yield return 0;
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.YieldReturnStatement);
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
                                N(SyntaxKind.YieldKeyword);
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(test).GetDiagnostics().Verify(
                // (4,10): error CS1624: The body of 'C.Goo()' cannot be an iterator block because 'void' is not an iterator interface type
                //     void Goo()
                Diagnostic(ErrorCode.ERR_BadIteratorReturn, "Goo").WithArguments("C.Goo()", "void").WithLocation(4, 10),
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnYieldBreak()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]yield return 0;
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.YieldReturnStatement);
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
                                N(SyntaxKind.YieldKeyword);
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(test).GetDiagnostics().Verify(
                // (4,10): error CS1624: The body of 'C.Goo()' cannot be an iterator block because 'void' is not an iterator interface type
                //     void Goo()
                Diagnostic(ErrorCode.ERR_BadIteratorReturn, "Goo").WithArguments("C.Goo()", "void").WithLocation(4, 10),
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnNakedYield()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]yield
    }
}";
            UsingTree(test);

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
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "yield");
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]yield
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0103: The name 'yield' does not exist in the current context
                //         [A]yield
                Diagnostic(ErrorCode.ERR_NameNotInContext, "yield").WithArguments("yield").WithLocation(6, 12),
                // (6,17): error CS1002: ; expected
                //         [A]yield
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 17));
        }

        [Fact]
        public void AttributeOnWhileStatement()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]while (true);
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.WhileStatement);
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
                                N(SyntaxKind.WhileKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TrueLiteralExpression);
                                {
                                    N(SyntaxKind.TrueKeyword);
                                }
                                N(SyntaxKind.CloseParenToken);
                                N(SyntaxKind.EmptyStatement);
                                {
                                    N(SyntaxKind.SemicolonToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnDoStatement()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]do { } while (true);
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.DoStatement);
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
                                N(SyntaxKind.DoKeyword);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                                N(SyntaxKind.WhileKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TrueLiteralExpression);
                                {
                                    N(SyntaxKind.TrueKeyword);
                                }
                                N(SyntaxKind.CloseParenToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnForStatement()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]for (;;) { }
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.ForStatement);
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
                                N(SyntaxKind.ForKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.SemicolonToken);
                                N(SyntaxKind.SemicolonToken);
                                N(SyntaxKind.CloseParenToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnNormalForEachStatement()
        {
            var test = @"
class C
{
    void Goo(string[] vals)
    {
        [A]foreach (var v in vals) { }
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.Parameter);
                            {
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
                                N(SyntaxKind.IdentifierToken, "vals");
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ForEachStatement);
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
                                N(SyntaxKind.ForEachKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "var");
                                }
                                N(SyntaxKind.IdentifierToken, "v");
                                N(SyntaxKind.InKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "vals");
                                }
                                N(SyntaxKind.CloseParenToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnForEachVariableStatement()
        {
            var test = @"
class C
{
    void Goo((int, string)[] vals)
    {
        [A]foreach (var (i, s) in vals) { }
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.ArrayType);
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
                                N(SyntaxKind.IdentifierToken, "vals");
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ForEachVariableStatement);
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
                                N(SyntaxKind.ForEachKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.DeclarationExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "var");
                                    }
                                    N(SyntaxKind.ParenthesizedVariableDesignation);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "i");
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "s");
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                                N(SyntaxKind.InKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "vals");
                                }
                                N(SyntaxKind.CloseParenToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnUsingStatement()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]using (null) { }
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.UsingStatement);
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
                                N(SyntaxKind.UsingKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.NullLiteralExpression);
                                {
                                    N(SyntaxKind.NullKeyword);
                                }
                                N(SyntaxKind.CloseParenToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnAwaitUsingStatement1()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]await using (null) { }
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.UsingStatement);
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
                                N(SyntaxKind.AwaitKeyword);
                                N(SyntaxKind.UsingKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.NullLiteralExpression);
                                {
                                    N(SyntaxKind.NullKeyword);
                                }
                                N(SyntaxKind.CloseParenToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]await using (null) { }
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0518: Predefined type 'System.IAsyncDisposable' is not defined or imported
                //         [A]await using (null) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await").WithArguments("System.IAsyncDisposable").WithLocation(6, 12),
                // (6,12): error CS0518: Predefined type 'System.Threading.Tasks.ValueTask' is not defined or imported
                //         [A]await using (null) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await").WithArguments("System.Threading.Tasks.ValueTask").WithLocation(6, 12),
                // (6,12): error CS4033: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
                //         [A]await using (null) { }
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod, "await").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnAwaitUsingStatement2()
        {
            var test = @"
class C
{
    async void Goo()
    {
        [A]await using (null) { }
    }
}";
            UsingTree(test);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
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
                            N(SyntaxKind.UsingStatement);
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
                                N(SyntaxKind.AwaitKeyword);
                                N(SyntaxKind.UsingKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.NullLiteralExpression);
                                {
                                    N(SyntaxKind.NullKeyword);
                                }
                                N(SyntaxKind.CloseParenToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]await using (null) { }
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0518: Predefined type 'System.IAsyncDisposable' is not defined or imported
                //         [A]await using (null) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await").WithArguments("System.IAsyncDisposable").WithLocation(6, 12),
                // (6,12): error CS0518: Predefined type 'System.Threading.Tasks.ValueTask' is not defined or imported
                //         [A]await using (null) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await").WithArguments("System.Threading.Tasks.ValueTask").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnFixedStatement()
        {
            var test = @"
class C
{
    unsafe void Goo(int[] vals)
    {
        [A]fixed (int* p = vals) { }
    }
}";
            UsingTree(test);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.UnsafeKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Goo");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
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
                                N(SyntaxKind.IdentifierToken, "vals");
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.FixedStatement);
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
                                        N(SyntaxKind.IdentifierToken, "p");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "vals");
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
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(test).GetDiagnostics().Verify(
                // (4,17): error CS0227: Unsafe code may only appear if compiling with /unsafe
                //     unsafe void Goo(int[] vals)
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "Goo").WithLocation(4, 17),
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]fixed (int* p = vals) { }
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnCheckedStatement()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]checked { }
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.CheckedStatement);
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
                                N(SyntaxKind.CheckedKeyword);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnCheckedBlock()
        {
            var test = @"
class C
{
    void Goo()
    {
        checked [A]{ }
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.CheckedStatement);
                            {
                                N(SyntaxKind.CheckedKeyword);
                                N(SyntaxKind.Block);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,17): error CS7014: Attributes are not valid in this context.
                //         checked [A]{ }
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 17));
        }

        [Fact]
        public void AttributeOnUncheckedStatement()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]unchecked { }
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.UncheckedStatement);
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
                                N(SyntaxKind.UncheckedKeyword);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnUnsafeStatement()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]unsafe { }
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.UnsafeStatement);
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
                                N(SyntaxKind.UnsafeKeyword);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]unsafe { }
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0227: Unsafe code may only appear if compiling with /unsafe
                //         [A]unsafe { }
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnUnsafeBlock()
        {
            var test = @"
class C
{
    void Goo()
    {
        unsafe [A]{ }
    }
}";
            UsingTree(test);

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
                                N(SyntaxKind.UnsafeKeyword);
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
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "A");
                                            }
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                    }
                                    M(SyntaxKind.VariableDeclarator);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS0106: The modifier 'unsafe' is not valid for this item
                //         unsafe [A]{ }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "unsafe").WithArguments("unsafe").WithLocation(6, 9),
                // (6,16): error CS1031: Type expected
                //         unsafe [A]{ }
                Diagnostic(ErrorCode.ERR_TypeExpected, "[").WithLocation(6, 16),
                // (6,16): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         unsafe [A]{ }
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[A]").WithLocation(6, 16),
                // (6,17): error CS0103: The name 'A' does not exist in the current context
                //         unsafe [A]{ }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "A").WithArguments("A").WithLocation(6, 17),
                // (6,19): error CS1001: Identifier expected
                //         unsafe [A]{ }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(6, 19),
                // (6,19): error CS1002: ; expected
                //         unsafe [A]{ }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(6, 19));
        }

        [Fact]
        public void AttributeOnLockStatement()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]lock (null) { }
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.LockStatement);
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
                                N(SyntaxKind.LockKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.NullLiteralExpression);
                                {
                                    N(SyntaxKind.NullKeyword);
                                }
                                N(SyntaxKind.CloseParenToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnIfStatement()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]if (true) { }
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.IfStatement);
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
                                N(SyntaxKind.IfKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TrueLiteralExpression);
                                {
                                    N(SyntaxKind.TrueKeyword);
                                }
                                N(SyntaxKind.CloseParenToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnSwitchStatement()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]switch (0) { }
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.SwitchStatement);
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
                                N(SyntaxKind.SwitchKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                                N(SyntaxKind.CloseParenToken);
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]switch (0) { }
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,23): warning CS1522: Empty switch block
                //         [A]switch (0) { }
                Diagnostic(ErrorCode.WRN_EmptySwitch, "{").WithLocation(6, 23));
        }

        [Fact]
        public void AttributeOnStatementInSwitchSection()
        {
            var test = @"
class C
{
    void Goo()
    {
        switch (0)
        {
            default:
                [A]return;
        }
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.SwitchStatement);
                            {
                                N(SyntaxKind.SwitchKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                                N(SyntaxKind.CloseParenToken);
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.SwitchSection);
                                {
                                    N(SyntaxKind.DefaultSwitchLabel);
                                    {
                                        N(SyntaxKind.DefaultKeyword);
                                        N(SyntaxKind.ColonToken);
                                    }
                                    N(SyntaxKind.ReturnStatement);
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
                                        N(SyntaxKind.ReturnKeyword);
                                        N(SyntaxKind.SemicolonToken);
                                    }
                                }
                                N(SyntaxKind.CloseBraceToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(test).GetDiagnostics().Verify(
                // (9,17): error CS7014: Attributes are not valid in this context.
                //                 [A]return;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(9, 17));
        }

        [Fact]
        public void AttributeOnStatementAboveCase()
        {
            var test = @"
class C
{
    void Goo()
    {
        switch (0)
        {
            [A]
            case 0:
                return;
        }
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.SwitchStatement);
                            {
                                N(SyntaxKind.SwitchKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                                N(SyntaxKind.CloseParenToken);
                                N(SyntaxKind.OpenBraceToken);
                                M(SyntaxKind.CloseBraceToken);
                            }
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
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ReturnStatement);
                            {
                                N(SyntaxKind.ReturnKeyword);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (7,9): warning CS1522: Empty switch block
                //         {
                Diagnostic(ErrorCode.WRN_EmptySwitch, "{").WithLocation(7, 9),
                // (7,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(7, 10),
                // (8,13): error CS7014: Attributes are not valid in this context.
                //             [A]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(8, 13),
                // (8,16): error CS1525: Invalid expression term 'case'
                //             [A]
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("case").WithLocation(8, 16),
                // (8,16): error CS1002: ; expected
                //             [A]
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(8, 16),
                // (8,16): error CS1513: } expected
                //             [A]
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(8, 16),
                // (9,19): error CS1002: ; expected
                //             case 0:
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(9, 19),
                // (9,19): error CS1513: } expected
                //             case 0:
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(9, 19),
                // (13,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(13, 1));
        }

        [Fact]
        public void AttributeOnStatementAboveDefaultCase()
        {
            var test = @"
class C
{
    void Goo()
    {
        switch (0)
        {
            [A]
            default:
                return;
        }
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.SwitchStatement);
                            {
                                N(SyntaxKind.SwitchKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                                N(SyntaxKind.CloseParenToken);
                                N(SyntaxKind.OpenBraceToken);
                                M(SyntaxKind.CloseBraceToken);
                            }
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
                                N(SyntaxKind.DefaultLiteralExpression);
                                {
                                    N(SyntaxKind.DefaultKeyword);
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ReturnStatement);
                            {
                                N(SyntaxKind.ReturnKeyword);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (7,9): warning CS1522: Empty switch block
                //         {
                Diagnostic(ErrorCode.WRN_EmptySwitch, "{").WithLocation(7, 9),
                // (7,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(7, 10),
                // (8,13): error CS7014: Attributes are not valid in this context.
                //             [A]
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(8, 13),
                // (9,13): error CS8716: There is no target type for the default literal.
                //             default:
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(9, 13),
                // (9,20): error CS1002: ; expected
                //             default:
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(9, 20),
                // (9,20): error CS1513: } expected
                //             default:
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(9, 20),
                // (13,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(13, 1));
        }

        [Fact]
        public void AttributeOnTryStatement()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]try { } finally { }
    }
}";
            UsingTree(test);


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
                            N(SyntaxKind.TryStatement);
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
                                N(SyntaxKind.TryKeyword);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                                N(SyntaxKind.FinallyClause);
                                {
                                    N(SyntaxKind.FinallyKeyword);
                                    N(SyntaxKind.Block);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.CloseBraceToken);
                                    }
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]try { } finally { }
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnTryBlock()
        {
            var test = @"
class C
{
    void Goo()
    {
        try [A] { } finally { }
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.TryStatement);
                            {
                                N(SyntaxKind.TryKeyword);
                                N(SyntaxKind.Block);
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
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                                N(SyntaxKind.FinallyClause);
                                {
                                    N(SyntaxKind.FinallyKeyword);
                                    N(SyntaxKind.Block);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.CloseBraceToken);
                                    }
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,13): error CS7014: Attributes are not valid in this context.
                //         try [A] { } finally { }
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 13));
        }

        [Fact]
        public void AttributeOnFinally()
        {
            var test = @"
class C
{
    void Goo()
    {
        try { } [A] finally { }
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.TryStatement);
                            {
                                N(SyntaxKind.TryKeyword);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                                M(SyntaxKind.FinallyClause);
                                {
                                    M(SyntaxKind.FinallyKeyword);
                                    M(SyntaxKind.Block);
                                    {
                                        M(SyntaxKind.OpenBraceToken);
                                        M(SyntaxKind.CloseBraceToken);
                                    }
                                }
                            }
                            N(SyntaxKind.TryStatement);
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
                                M(SyntaxKind.TryKeyword);
                                M(SyntaxKind.Block);
                                {
                                    M(SyntaxKind.OpenBraceToken);
                                    M(SyntaxKind.CloseBraceToken);
                                }
                                N(SyntaxKind.FinallyClause);
                                {
                                    N(SyntaxKind.FinallyKeyword);
                                    N(SyntaxKind.Block);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.CloseBraceToken);
                                    }
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,15): error CS1524: Expected catch or finally
                //         try { } [A] finally { }
                Diagnostic(ErrorCode.ERR_ExpectedEndTry, "}").WithLocation(6, 15),
                // (6,17): error CS7014: Attributes are not valid in this context.
                //         try { } [A] finally { }
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 17),
                // (6,21): error CS1003: Syntax error, 'try' expected
                //         try { } [A] finally { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "finally").WithArguments("try", "finally").WithLocation(6, 21),
                // (6,21): error CS1514: { expected
                //         try { } [A] finally { }
                Diagnostic(ErrorCode.ERR_LbraceExpected, "finally").WithLocation(6, 21),
                // (6,21): error CS1513: } expected
                //         try { } [A] finally { }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "finally").WithLocation(6, 21));
        }

        [Fact]
        public void AttributeOnFinallyBlock()
        {
            var test = @"
class C
{
    void Goo()
    {
        try { } finally [A] { }
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.TryStatement);
                            {
                                N(SyntaxKind.TryKeyword);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                                N(SyntaxKind.FinallyClause);
                                {
                                    N(SyntaxKind.FinallyKeyword);
                                    N(SyntaxKind.Block);
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
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.CloseBraceToken);
                                    }
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,25): error CS7014: Attributes are not valid in this context.
                //         try { } finally [A] { }
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 25));
        }

        [Fact]
        public void AttributeOnCatch()
        {
            var test = @"
class C
{
    void Goo()
    {
        try { } [A] catch { }
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.TryStatement);
                            {
                                N(SyntaxKind.TryKeyword);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                                M(SyntaxKind.FinallyClause);
                                {
                                    M(SyntaxKind.FinallyKeyword);
                                    M(SyntaxKind.Block);
                                    {
                                        M(SyntaxKind.OpenBraceToken);
                                        M(SyntaxKind.CloseBraceToken);
                                    }
                                }
                            }
                            N(SyntaxKind.TryStatement);
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
                                M(SyntaxKind.TryKeyword);
                                M(SyntaxKind.Block);
                                {
                                    M(SyntaxKind.OpenBraceToken);
                                    M(SyntaxKind.CloseBraceToken);
                                }
                                N(SyntaxKind.CatchClause);
                                {
                                    N(SyntaxKind.CatchKeyword);
                                    N(SyntaxKind.Block);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.CloseBraceToken);
                                    }
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,15): error CS1524: Expected catch or finally
                //         try { } [A] catch { }
                Diagnostic(ErrorCode.ERR_ExpectedEndTry, "}").WithLocation(6, 15),
                // (6,17): error CS7014: Attributes are not valid in this context.
                //         try { } [A] catch { }
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 17),
                // (6,21): error CS1003: Syntax error, 'try' expected
                //         try { } [A] catch { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "catch").WithArguments("try", "catch").WithLocation(6, 21),
                // (6,21): error CS1514: { expected
                //         try { } [A] catch { }
                Diagnostic(ErrorCode.ERR_LbraceExpected, "catch").WithLocation(6, 21),
                // (6,21): error CS1513: } expected
                //         try { } [A] catch { }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "catch").WithLocation(6, 21));
        }

        [Fact]
        public void AttributeOnCatchBlock()
        {
            var test = @"
class C
{
    void Goo()
    {
        try { } catch [A] { }
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.TryStatement);
                            {
                                N(SyntaxKind.TryKeyword);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                                N(SyntaxKind.CatchClause);
                                {
                                    N(SyntaxKind.CatchKeyword);
                                    N(SyntaxKind.Block);
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
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.CloseBraceToken);
                                    }
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,23): error CS7014: Attributes are not valid in this context.
                //         try { } catch [A] { }
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 23));
        }

        [Fact]
        public void AttributeOnEmbeddedStatement()
        {
            var test = @"
class C
{
    void Goo()
    {
        if (true) [A]return;
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.IfStatement);
                            {
                                N(SyntaxKind.IfKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TrueLiteralExpression);
                                {
                                    N(SyntaxKind.TrueKeyword);
                                }
                                N(SyntaxKind.CloseParenToken);
                                N(SyntaxKind.ReturnStatement);
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
                                    N(SyntaxKind.ReturnKeyword);
                                    N(SyntaxKind.SemicolonToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,19): error CS7014: Attributes are not valid in this context.
                //         if (true) [A]return;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 19));
        }

        [Fact]
        public void AttributeOnExpressionStatement_AnonymousMethod_NoParameters()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]delegate { }
    }
}";
            UsingTree(test);

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
                                N(SyntaxKind.AnonymousMethodExpression);
                                {
                                    N(SyntaxKind.DelegateKeyword);
                                    N(SyntaxKind.Block);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]delegate { }
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,24): error CS1002: ; expected
                //         [A]delegate { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 24));
        }

        [Fact]
        public void AttributeOnExpressionStatement_AnonymousMethod_NoBody()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]delegate
    }
}";
            UsingTree(test);

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
                                N(SyntaxKind.AnonymousMethodExpression);
                                {
                                    N(SyntaxKind.DelegateKeyword);
                                    M(SyntaxKind.Block);
                                    {
                                        M(SyntaxKind.OpenBraceToken);
                                        M(SyntaxKind.CloseBraceToken);
                                    }
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]delegate
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,20): error CS1514: { expected
                //         [A]delegate
                Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(6, 20),
                // (6,20): error CS1002: ; expected
                //         [A]delegate
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 20));
        }

        [Fact]
        public void AttributeOnExpressionStatement_AnonymousMethod_Parameters()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]delegate () { };
    }
}";
            UsingTree(test);

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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]delegate () { };
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A]delegate () { };
                Diagnostic(ErrorCode.ERR_IllegalStatement, "delegate () { }").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnExpressionStatement_Lambda_NoParameters()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]() => { };
    }
}";
            UsingTree(test);

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
                                N(SyntaxKind.ParenthesizedLambdaExpression);
                                {
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]() => { };
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A]() => { };
                Diagnostic(ErrorCode.ERR_IllegalStatement, "() => { }").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnExpressionStatement_Lambda_Parameters1()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A](int i) => { };
    }
}";
            UsingTree(test);

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
                                            N(SyntaxKind.IdentifierToken, "i");
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A](int i) => { };
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A](int i) => { };
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(int i) => { }").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnExpressionStatement_Lambda_Parameters2()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]i => { };
    }
}";
            UsingTree(test);

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
                                N(SyntaxKind.SimpleLambdaExpression);
                                {
                                    N(SyntaxKind.Parameter);
                                    {
                                        N(SyntaxKind.IdentifierToken, "i");
                                    }
                                    N(SyntaxKind.EqualsGreaterThanToken);
                                    N(SyntaxKind.Block);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.CloseBraceToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]i => { };
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A]i => { };
                Diagnostic(ErrorCode.ERR_IllegalStatement, "i => { }").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnExpressionStatement_AnonymousObject()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]new { };
    }
}";
            UsingTree(test);

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
                                N(SyntaxKind.AnonymousObjectCreationExpression);
                                {
                                    N(SyntaxKind.NewKeyword);
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]new { };
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A]new { };
                Diagnostic(ErrorCode.ERR_IllegalStatement, "new { }").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnExpressionStatement_ArrayCreation()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]new int[] { };
    }
}";
            UsingTree(test);

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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]new int[] { };
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A]new int[] { };
                Diagnostic(ErrorCode.ERR_IllegalStatement, "new int[] { }").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnExpressionStatement_AnonymousArrayCreation()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]new [] { 0 };
    }
}";
            UsingTree(test);

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
                                            N(SyntaxKind.NumericLiteralToken, "0");
                                        }
                                        N(SyntaxKind.CloseBraceToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]new [] { 0 };
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A]new [] { 0 };
                Diagnostic(ErrorCode.ERR_IllegalStatement, "new [] { 0 }").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnExpressionStatement_Assignment()
        {
            var test = @"
class C
{
    void Goo(int a)
    {
        [A]a = 0;
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "a");
                            }
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
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                    N(SyntaxKind.EqualsToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]a = 0;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnExpressionStatement_CompoundAssignment()
        {
            var test = @"
class C
{
    void Goo(int a)
    {
        [A]a += 0;
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "a");
                            }
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
                                N(SyntaxKind.AddAssignmentExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                    N(SyntaxKind.PlusEqualsToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]a += 0;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnExpressionStatement_AwaitExpresion_NonAsyncContext()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]await a;
    }
}";
            UsingTree(test);

            CreateCompilation(test).GetDiagnostics().Verify(
                // (5,6): error CS1513: } expected
                //     {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 6),
                // (6,10): error CS0246: The type or namespace name 'AAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //         [A]await a;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("AAttribute").WithLocation(6, 10),
                // (6,10): error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)
                //         [A]await a;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("A").WithLocation(6, 10),
                // (6,12): error CS0246: The type or namespace name 'await' could not be found (are you missing a using directive or an assembly reference?)
                //         [A]await a;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "await").WithArguments("await").WithLocation(6, 12),
                // (6,18): warning CS0169: The field 'C.a' is never used
                //         [A]await a;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "a").WithArguments("C.a").WithLocation(6, 18),
                // (8,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(8, 1));
        }

        [Fact]
        public void AttributeOnExpressionStatement_AwaitExpresion_AsyncContext()
        {
            var test = @"
class C
{
    async void Goo()
    {
        [A]await a;
    }
}";
            UsingTree(test);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
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
                                N(SyntaxKind.AwaitExpression);
                                {
                                    N(SyntaxKind.AwaitKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]await a;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,18): error CS0103: The name 'a' does not exist in the current context
                //         [A]await a;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 18));
        }

        [Fact]
        public void AttributeOnExpressionStatement_BinaryExpression()
        {
            var test = @"
class C
{
    void Goo(int a)
    {
        [A]a + a;
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "a");
                            }
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
                                N(SyntaxKind.AddExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                    N(SyntaxKind.PlusToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]a + a;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A]a + a;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "a + a").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnExpressionStatement_CastExpression()
        {
            var test = @"
class C
{
    void Goo(int a)
    {
        [A](object)a;
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "a");
                            }
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
                                N(SyntaxKind.CastExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.ObjectKeyword);
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A](object)a;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A](object)a;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(object)a").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnExpressionStatement_ConditionalAccess()
        {
            var test = @"
class C
{
    void Goo(string a)
    {
        [A]a?.ToString();
    }
}";
            UsingTree(test);

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
                                N(SyntaxKind.ConditionalAccessExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                    N(SyntaxKind.QuestionToken);
                                    N(SyntaxKind.InvocationExpression);
                                    {
                                        N(SyntaxKind.MemberBindingExpression);
                                        {
                                            N(SyntaxKind.DotToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "ToString");
                                            }
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]a?.ToString();
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnExpressionStatement_DefaultExpression()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]default(int);
    }
}";
            UsingTree(test);

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
                                N(SyntaxKind.DefaultExpression);
                                {
                                    N(SyntaxKind.DefaultKeyword);
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]default(int);
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A]default(int);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "default(int)").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnExpressionStatement_DefaultLiteral()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]default;
    }
}";
            UsingTree(test);

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
                                N(SyntaxKind.DefaultLiteralExpression);
                                {
                                    N(SyntaxKind.DefaultKeyword);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]default;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS8716: There is no target type for the default literal.
                //         [A]default;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 12),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A]default;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "default").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnExpressionStatement_ElementAccess()
        {
            var test = @"
class C
{
    void Goo(string s)
    {
        [A]s[0];
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "s");
                            }
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
                                N(SyntaxKind.ElementAccessExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "s");
                                    }
                                    N(SyntaxKind.BracketedArgumentList);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "0");
                                            }
                                        }
                                        N(SyntaxKind.CloseBracketToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]s[0];
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A]s[0];
                Diagnostic(ErrorCode.ERR_IllegalStatement, "s[0]").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnExpressionStatement_ElementBinding()
        {
            var test = @"
class C
{
    void Goo(string s)
    {
        [A]s?[0];
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "s");
                            }
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
                                N(SyntaxKind.ConditionalAccessExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "s");
                                    }
                                    N(SyntaxKind.QuestionToken);
                                    N(SyntaxKind.ElementBindingExpression);
                                    {
                                        N(SyntaxKind.BracketedArgumentList);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.Argument);
                                            {
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "0");
                                                }
                                            }
                                            N(SyntaxKind.CloseBracketToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]s?[0];
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A]s?[0];
                Diagnostic(ErrorCode.ERR_IllegalStatement, "s?[0]").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnExpressionStatement_Invocation()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]Goo();
    }
}";
            UsingTree(test);

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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]Goo();
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnExpressionStatement_Literal()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]0;
    }
}";
            UsingTree(test);

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
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]0;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A]0;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "0").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnExpressionStatement_MemberAccess()
        {
            var test = @"
class C
{
    void Goo(int i)
    {
        [A]i.ToString;
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "i");
                            }
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
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "i");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "ToString");
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]i.ToString;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A]i.ToString;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "i.ToString").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnExpressionStatement_ObjectCreation_Builtin()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]new int();
    }
}";
            UsingTree(test);

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
                                N(SyntaxKind.ObjectCreationExpression);
                                {
                                    N(SyntaxKind.NewKeyword);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]new int();
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnExpressionStatement_ObjectCreation_TypeName()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]new System.Int32();
    }
}";
            UsingTree(test);

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
                                N(SyntaxKind.ObjectCreationExpression);
                                {
                                    N(SyntaxKind.NewKeyword);
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "System");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Int32");
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
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]new System.Int32();
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnExpressionStatement_Parenthesized()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A](1);
    }
}";
            UsingTree(test);

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
                                N(SyntaxKind.ParenthesizedExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "1");
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A](1);
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A](1);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(1)").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnExpressionStatement_PostfixUnary()
        {
            var test = @"
class C
{
    void Goo(int i)
    {
        [A]i++;
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "i");
                            }
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
                                N(SyntaxKind.PostIncrementExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "i");
                                    }
                                    N(SyntaxKind.PlusPlusToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]i++;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnExpressionStatement_PrefixUnary()
        {
            var test = @"
class C
{
    void Goo(int i)
    {
        [A]++i;
    }
}";
            UsingTree(test);


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
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "i");
                            }
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
                                N(SyntaxKind.PreIncrementExpression);
                                {
                                    N(SyntaxKind.PlusPlusToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "i");
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]++i;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnExpressionStatement_Query()
        {
            var test = @"
using System.Linq;
class C
{
    void Goo(string s)
    {
        [A]from c in s select c;
    }
}";
            UsingTree(test);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "System");
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Linq");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
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
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "s");
                            }
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
                                N(SyntaxKind.QueryExpression);
                                {
                                    N(SyntaxKind.FromClause);
                                    {
                                        N(SyntaxKind.FromKeyword);
                                        N(SyntaxKind.IdentifierToken, "c");
                                        N(SyntaxKind.InKeyword);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "s");
                                        }
                                    }
                                    N(SyntaxKind.QueryBody);
                                    {
                                        N(SyntaxKind.SelectClause);
                                        {
                                            N(SyntaxKind.SelectKeyword);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "c");
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (7,9): error CS7014: Attributes are not valid in this context.
                //         [A]from c in s select c;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(7, 9),
                // (7,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A]from c in s select c;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "from c in s select c").WithLocation(7, 12));
        }

        [Fact]
        public void AttributeOnExpressionStatement_Range1()
        {
            var test = @"
class C
{
    void Goo(int a, int b)
    {
        [A]a..b;
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "b");
                            }
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
                                N(SyntaxKind.RangeExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                    N(SyntaxKind.DotDotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "b");
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]a..b;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0518: Predefined type 'System.Range' is not defined or imported
                //         [A]a..b;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "a..b").WithArguments("System.Range").WithLocation(6, 12),
                // (6,12): error CS0518: Predefined type 'System.Index' is not defined or imported
                //         [A]a..b;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "a").WithArguments("System.Index").WithLocation(6, 12),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A]a..b;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "a..b").WithLocation(6, 12),
                // (6,15): error CS0518: Predefined type 'System.Index' is not defined or imported
                //         [A]a..b;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "b").WithArguments("System.Index").WithLocation(6, 15));
        }

        [Fact]
        public void AttributeOnExpressionStatement_Range2()
        {
            var test = @"
class C
{
    void Goo(int a, int b)
    {
        [A]a..;
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "b");
                            }
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
                                N(SyntaxKind.RangeExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                    N(SyntaxKind.DotDotToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]a..;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0518: Predefined type 'System.Range' is not defined or imported
                //         [A]a..;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "a..").WithArguments("System.Range").WithLocation(6, 12),
                // (6,12): error CS0518: Predefined type 'System.Index' is not defined or imported
                //         [A]a..;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "a").WithArguments("System.Index").WithLocation(6, 12),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A]a..;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "a..").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnExpressionStatement_Range3()
        {
            var test = @"
class C
{
    void Goo(int a, int b)
    {
        [A]..b;
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "b");
                            }
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
                                N(SyntaxKind.RangeExpression);
                                {
                                    N(SyntaxKind.DotDotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "b");
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]..b;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0518: Predefined type 'System.Range' is not defined or imported
                //         [A]..b;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "..b").WithArguments("System.Range").WithLocation(6, 12),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A]..b;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "..b").WithLocation(6, 12),
                // (6,14): error CS0518: Predefined type 'System.Index' is not defined or imported
                //         [A]..b;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "b").WithArguments("System.Index").WithLocation(6, 14));
        }

        [Fact]
        public void AttributeOnExpressionStatement_Range4()
        {
            var test = @"
class C
{
    void Goo(int a, int b)
    {
        [A]..;
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "b");
                            }
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
                                N(SyntaxKind.RangeExpression);
                                {
                                    N(SyntaxKind.DotDotToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]..;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0518: Predefined type 'System.Range' is not defined or imported
                //         [A]..;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "..").WithArguments("System.Range").WithLocation(6, 12),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A]..;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "..").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnExpressionStatement_Sizeof()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]sizeof(int);
    }
}";
            UsingTree(test);

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
                                N(SyntaxKind.SizeOfExpression);
                                {
                                    N(SyntaxKind.SizeOfKeyword);
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]sizeof(int);
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A]sizeof(int);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "sizeof(int)").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnExpressionStatement_SwitchExpression()
        {
            var test = @"
class C
{
    void Goo(int a)
    {
        [A]a switch { };
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "a");
                            }
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
                                N(SyntaxKind.SwitchExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                    N(SyntaxKind.SwitchKeyword);
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]a switch { };
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A]a switch { };
                Diagnostic(ErrorCode.ERR_IllegalStatement, "a switch { }").WithLocation(6, 12),
                // (6,14): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive).
                //         [A]a switch { };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithLocation(6, 14),
                // (6,14): error CS8506: No best type was found for the switch expression.
                //         [A]a switch { };
                Diagnostic(ErrorCode.ERR_SwitchExpressionNoBestType, "switch").WithLocation(6, 14));
        }

        [Fact]
        public void AttributeOnExpressionStatement_TypeOf()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]typeof(int);
    }
}";
            UsingTree(test);

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
                                N(SyntaxKind.TypeOfExpression);
                                {
                                    N(SyntaxKind.TypeOfKeyword);
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]typeof(int);
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         [A]typeof(int);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "typeof(int)").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnLocalDeclOrMember1()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]int i;
    }
}";
            UsingTree(test);

            CreateCompilation(test).GetDiagnostics().Verify(
                // (5,6): error CS1513: } expected
                //     {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 6),
                // (6,10): error CS0246: The type or namespace name 'AAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //         [A]int i;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("AAttribute").WithLocation(6, 10),
                // (6,10): error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)
                //         [A]int i;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("A").WithLocation(6, 10),
                // (6,16): warning CS0169: The field 'C.i' is never used
                //         [A]int i;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "i").WithArguments("C.i").WithLocation(6, 16),
                // (8,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(8, 1));
        }

        [Fact]
        public void AttributeOnLocalDeclOrMember2()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]int i, j;
    }
}";
            UsingTree(test);

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
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "i");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "j");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(test).GetDiagnostics().Verify(
                // (5,6): error CS1513: } expected
                //     {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 6),
                // (6,10): error CS0246: The type or namespace name 'AAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //         [A]int i, j;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("AAttribute").WithLocation(6, 10),
                // (6,10): error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)
                //         [A]int i, j;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("A").WithLocation(6, 10),
                // (6,10): error CS0246: The type or namespace name 'AAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //         [A]int i, j;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("AAttribute").WithLocation(6, 10),
                // (6,10): error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)
                //         [A]int i, j;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("A").WithLocation(6, 10),
                // (6,16): warning CS0169: The field 'C.i' is never used
                //         [A]int i, j;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "i").WithArguments("C.i").WithLocation(6, 16),
                // (6,19): warning CS0169: The field 'C.j' is never used
                //         [A]int i, j;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "j").WithArguments("C.j").WithLocation(6, 19),
                // (8,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(8, 1));
        }

        [Fact]
        public void AttributeOnLocalDeclOrMember3()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]int i = 0;
    }
}";
            UsingTree(test);

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
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "i");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "0");
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (5,6): error CS1513: } expected
                //     {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 6),
                // (6,10): error CS0246: The type or namespace name 'AAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //         [A]int i = 0;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("AAttribute").WithLocation(6, 10),
                // (6,10): error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)
                //         [A]int i = 0;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("A").WithLocation(6, 10),
                // (6,16): warning CS0414: The field 'C.i' is assigned but its value is never used
                //         [A]int i = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "i").WithArguments("C.i").WithLocation(6, 16),
                // (8,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(8, 1));
        }

        [Fact]
        public void AttributeOnLocalDeclOrMember4()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]int this[int i] => 0;
    }
}";
            UsingTree(test);

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
                            M(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.IndexerDeclaration);
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(test).GetDiagnostics().Verify(
                // (5,6): error CS1513: } expected
                //     {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 6),
                // (6,10): error CS0246: The type or namespace name 'AAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //         [A]int this[int i] => 0;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("AAttribute").WithLocation(6, 10),
                // (6,10): error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)
                //         [A]int this[int i] => 0;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("A").WithLocation(6, 10),
                // (8,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(8, 1));
        }

        [Fact]
        public void AttributeOnLocalDeclOrMember5()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]const int i = 0;
    }
}";
            UsingTree(test);

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
                        N(SyntaxKind.ConstKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "i");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "0");
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (5,6): error CS1513: } expected
                //     {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 6),
                // (6,10): error CS0246: The type or namespace name 'AAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //         [A]const int i = 0;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("AAttribute").WithLocation(6, 10),
                // (6,10): error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)
                //         [A]const int i = 0;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("A").WithLocation(6, 10),
                // (8,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(8, 1));
        }

        [Fact]
        public void AttributeOnLocalDeclOrMember6()
        {
            var test = @"
class C
{
    void Goo()
    {
        [A]public int i = 0;
    }
}";
            UsingTree(test);
















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
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "i");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "0");
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (5,6): error CS1513: } expected
                //     {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 6),
                // (6,10): error CS0246: The type or namespace name 'AAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //         [A]public int i = 0;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("AAttribute").WithLocation(6, 10),
                // (6,10): error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)
                //         [A]public int i = 0;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("A").WithLocation(6, 10),
                // (8,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(8, 1));
        }

        [Fact]
        public void AttributeOnLocalDeclOrMember7()
        {
            var test = @"
class C
{
    void Goo(System.IDisposable d)
    {
        [A]using var i = d;
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.Parameter);
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
                                        N(SyntaxKind.IdentifierToken, "IDisposable");
                                    }
                                }
                                N(SyntaxKind.IdentifierToken, "d");
                            }
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
                                N(SyntaxKind.UsingKeyword);
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "var");
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "i");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "d");
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]using var i = d;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9));
        }

        [Fact]
        public void AttributeOnLocalDeclOrMember8()
        {
            var test = @"
class C
{
    void Goo(System.IAsyncDisposable d)
    {
        [A]await using var i = d;
    }
}";
            UsingTree(test);

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
                            N(SyntaxKind.Parameter);
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
                                        N(SyntaxKind.IdentifierToken, "IAsyncDisposable");
                                    }
                                }
                                N(SyntaxKind.IdentifierToken, "d");
                            }
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
                                N(SyntaxKind.AwaitKeyword);
                                N(SyntaxKind.UsingKeyword);
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "var");
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "i");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "d");
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (4,21): error CS0234: The type or namespace name 'IAsyncDisposable' does not exist in the namespace 'System' (are you missing an assembly reference?)
                //     void Goo(System.IAsyncDisposable d)
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "IAsyncDisposable").WithArguments("IAsyncDisposable", "System").WithLocation(4, 21),
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]await using var i = d;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0518: Predefined type 'System.IAsyncDisposable' is not defined or imported
                //         [A]await using var i = d;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await").WithArguments("System.IAsyncDisposable").WithLocation(6, 12),
                // (6,12): error CS4033: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
                //         [A]await using var i = d;
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod, "await").WithLocation(6, 12));
        }

        [Fact]
        public void AttributeOnLocalDeclOrMember9()
        {
            var test = @"
class C
{
    async void Goo(System.IAsyncDisposable d)
    {
        [A]await using var i = d;
    }
}";
            UsingTree(test);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Goo");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
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
                                        N(SyntaxKind.IdentifierToken, "IAsyncDisposable");
                                    }
                                }
                                N(SyntaxKind.IdentifierToken, "d");
                            }
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
                                N(SyntaxKind.AwaitKeyword);
                                N(SyntaxKind.UsingKeyword);
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "var");
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "i");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "d");
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

            CreateCompilation(test).GetDiagnostics().Verify(
                // (4,27): error CS0234: The type or namespace name 'IAsyncDisposable' does not exist in the namespace 'System' (are you missing an assembly reference?)
                //     async void Goo(System.IAsyncDisposable d)
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "IAsyncDisposable").WithArguments("IAsyncDisposable", "System").WithLocation(4, 27),
                // (6,9): error CS7014: Attributes are not valid in this context.
                //         [A]await using var i = d;
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(6, 9),
                // (6,12): error CS0518: Predefined type 'System.IAsyncDisposable' is not defined or imported
                //         [A]await using var i = d;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "await").WithArguments("System.IAsyncDisposable").WithLocation(6, 12));
        }
    }
}
