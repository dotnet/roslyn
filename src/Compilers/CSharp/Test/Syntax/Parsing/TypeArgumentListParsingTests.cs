// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public sealed class TypeArgumentListParsingTests : ParsingTests
    {
        public TypeArgumentListParsingTests(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions? options)
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
",
                // (6,76): error CS1002: ; expected
                //         var added = ImmutableDictionary<string, IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 76));
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
",
                // (6,73): error CS1002: ; expected
                //         var added = ImmutableDictionary<X[], IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 73));
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
",
                // (6,74): error CS1002: ; expected
                //         var added = ImmutableDictionary<int*, IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 74));
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
",
                // (6,43): error CS1525: Invalid expression term ','
                //         var added = ImmutableDictionary<X*, IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(6, 43),
                // (6,65): error CS1002: ; expected
                //         var added = ImmutableDictionary<X*, IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "<").WithLocation(6, 65),
                // (6,65): error CS1525: Invalid expression term '<'
                //         var added = ImmutableDictionary<X*, IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "<").WithArguments("<").WithLocation(6, 65),
                // (6,67): error CS1002: ; expected
                //         var added = ImmutableDictionary<X*, IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(6, 67),
                // (6,67): error CS1513: } expected
                //         var added = ImmutableDictionary<X*, IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_RbraceExpected, ",").WithLocation(6, 67));

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
",
                // (6,42): error CS1525: Invalid expression term 'int'
                //         var added = ImmutableDictionary<(int, string), IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(6, 42),
                // (6,47): error CS1525: Invalid expression term 'string'
                //         var added = ImmutableDictionary<(int, string), IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "string").WithArguments("string").WithLocation(6, 47),
                // (6,76): error CS1002: ; expected
                //         var added = ImmutableDictionary<(int, string), IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "<").WithLocation(6, 76),
                // (6,76): error CS1525: Invalid expression term '<'
                //         var added = ImmutableDictionary<(int, string), IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "<").WithArguments("<").WithLocation(6, 76),
                // (6,78): error CS1002: ; expected
                //         var added = ImmutableDictionary<(int, string), IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(6, 78),
                // (6,78): error CS1513: } expected
                //         var added = ImmutableDictionary<(int, string), IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_RbraceExpected, ",").WithLocation(6, 78));
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
",
                // (6,66): error CS1002: ; expected
                //         var added = ImmutableDictionary<(A), IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "<").WithLocation(6, 66),
                // (6,66): error CS1525: Invalid expression term '<'
                //         var added = ImmutableDictionary<(A), IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "<").WithArguments("<").WithLocation(6, 66),
                // (6,68): error CS1002: ; expected
                //         var added = ImmutableDictionary<(A), IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(6, 68),
                // (6,68): error CS1513: } expected
                //         var added = ImmutableDictionary<(A), IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_RbraceExpected, ",").WithLocation(6, 68));
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
",
                // (6,66): error CS1002: ; expected
                //         var added = ImmutableDictionary<A.B, IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "<").WithLocation(6, 66),
                // (6,66): error CS1525: Invalid expression term '<'
                //         var added = ImmutableDictionary<A.B, IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "<").WithArguments("<").WithLocation(6, 66),
                // (6,68): error CS1002: ; expected
                //         var added = ImmutableDictionary<A.B, IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(6, 68),
                // (6,68): error CS1513: } expected
                //         var added = ImmutableDictionary<A.B, IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_RbraceExpected, ",").WithLocation(6, 68));
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
",
                // (6,67): error CS1002: ; expected
                //         var added = ImmutableDictionary<A::B, IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "<").WithLocation(6, 67),
                // (6,67): error CS1525: Invalid expression term '<'
                //         var added = ImmutableDictionary<A::B, IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "<").WithArguments("<").WithLocation(6, 67),
                // (6,69): error CS1002: ; expected
                //         var added = ImmutableDictionary<A::B, IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(6, 69),
                // (6,69): error CS1513: } expected
                //         var added = ImmutableDictionary<A::B, IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_RbraceExpected, ",").WithLocation(6, 69));
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
",
                // (6,72): error CS1002: ; expected
                //         var added = ImmutableDictionary<A?, IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 72));
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
",
                // (6,44): error CS1002: ; expected
                //         var added = ImmutableDictionary<A?>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 44));
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
",
                // (8,38): error CS1003: Syntax error, ':' expected
                //         ProjectChange = projectChange;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(":").WithLocation(8, 38),
                // (8,38): error CS1525: Invalid expression term ';'
                //         ProjectChange = projectChange;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(8, 38));
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
        public void TestGenericArgWithComma_01()
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
",
                // (6,74): error CS1002: ; expected
                //         var added = ImmutableDictionary<T<S>, IImmutableDictionary<X, Y>>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 74));
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

        [Fact]
        public void TestGenericArgWithComma_02()
        {
            UsingTree(@"
class C
{
    void M()
    {
        var added = U<ImmutableDictionary<T<S>, IImmutableDictionary<X, Y>>>

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
                                                N(SyntaxKind.GreaterThanExpression);
                                                {
                                                    N(SyntaxKind.LessThanExpression);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "U");
                                                        }
                                                        N(SyntaxKind.LessThanToken);
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
                                                    N(SyntaxKind.GreaterThanToken);
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
        public void TestGenericArgWithComma_03()
        {
            UsingTree(@"
class C
{
    void M()
    {
        var added = ImmutableDictionary<T<S>, U<IImmutableDictionary<X, Y>>>

        ProjectChange = projectChange;
    }
}
",
                // (6,77): error CS1002: ; expected
                //         var added = ImmutableDictionary<T<S>, U<IImmutableDictionary<X, Y>>>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 77));
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
                                                        N(SyntaxKind.IdentifierToken, "U");
                                                        N(SyntaxKind.TypeArgumentList);
                                                        {
                                                            N(SyntaxKind.LessThanToken);
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
        public void TestGenericArgWithComma_04()
        {
            UsingTree(@"
class C
{
    void M()
    {
        var added = ImmutableDictionary<T<S>, IImmutableDictionary<X, U<Y>>>

        ProjectChange = projectChange;
    }
}
",
                // (6,77): error CS1002: ; expected
                //         var added = ImmutableDictionary<T<S>, IImmutableDictionary<X, U<Y>>>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 77));
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
                                                            N(SyntaxKind.GenericName);
                                                            {
                                                                N(SyntaxKind.IdentifierToken, "U");
                                                                N(SyntaxKind.TypeArgumentList);
                                                                {
                                                                    N(SyntaxKind.LessThanToken);
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
        public void TestGenericArgWithGreaterThan_01()
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

        [Fact]
        public void TestGenericArgWithGreaterThan_02()
        {
            UsingTree(@"
class C
{
    void M()
    {
        var added = ImmutableDictionary<U<T<S>>>

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
                                                        N(SyntaxKind.LessThanExpression);
                                                        {
                                                            N(SyntaxKind.IdentifierName);
                                                            {
                                                                N(SyntaxKind.IdentifierToken, "ImmutableDictionary");
                                                            }
                                                            N(SyntaxKind.LessThanToken);
                                                            N(SyntaxKind.IdentifierName);
                                                            {
                                                                N(SyntaxKind.IdentifierToken, "U");
                                                            }
                                                        }
                                                        N(SyntaxKind.LessThanToken);
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "T");
                                                        }
                                                    }
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.UnsignedRightShiftExpression);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "S");
                                                        }
                                                        N(SyntaxKind.GreaterThanGreaterThanGreaterThanToken);
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

        [Fact]
        public void TestGenericArgWithGreaterThan_03()
        {
            UsingTree(@"
class C
{
    void M()
    {
        var added = ImmutableDictionary<T<S>>>

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
                                                    N(SyntaxKind.UnsignedRightShiftExpression);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "S");
                                                        }
                                                        N(SyntaxKind.GreaterThanGreaterThanGreaterThanToken);
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

        [Fact]
        public void TestGenericArgWithGreaterThan_04()
        {
            UsingTree(@"
class C
{
    void M()
    {
        var added = ImmutableDictionary<T<(S, U)>>>

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
                                                    N(SyntaxKind.UnsignedRightShiftExpression);
                                                    {
                                                        N(SyntaxKind.TupleExpression);
                                                        {
                                                            N(SyntaxKind.OpenParenToken);
                                                            N(SyntaxKind.Argument);
                                                            {
                                                                N(SyntaxKind.IdentifierName);
                                                                {
                                                                    N(SyntaxKind.IdentifierToken, "S");
                                                                }
                                                            }
                                                            N(SyntaxKind.CommaToken);
                                                            N(SyntaxKind.Argument);
                                                            {
                                                                N(SyntaxKind.IdentifierName);
                                                                {
                                                                    N(SyntaxKind.IdentifierToken, "U");
                                                                }
                                                            }
                                                            N(SyntaxKind.CloseParenToken);
                                                        }
                                                        N(SyntaxKind.GreaterThanGreaterThanGreaterThanToken);
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

        [Fact]
        public void TestGenericArgWithGreaterThan_05()
        {
            UsingTree(@"
class C
{
    void M()
    {
        var added = ImmutableDictionary<T<(S a, U b)>>>

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
                                                    N(SyntaxKind.UnsignedRightShiftExpression);
                                                    {
                                                        N(SyntaxKind.TupleExpression);
                                                        {
                                                            N(SyntaxKind.OpenParenToken);
                                                            N(SyntaxKind.Argument);
                                                            {
                                                                N(SyntaxKind.DeclarationExpression);
                                                                {
                                                                    N(SyntaxKind.IdentifierName);
                                                                    {
                                                                        N(SyntaxKind.IdentifierToken, "S");
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
                                                                N(SyntaxKind.DeclarationExpression);
                                                                {
                                                                    N(SyntaxKind.IdentifierName);
                                                                    {
                                                                        N(SyntaxKind.IdentifierToken, "U");
                                                                    }
                                                                    N(SyntaxKind.SingleVariableDesignation);
                                                                    {
                                                                        N(SyntaxKind.IdentifierToken, "b");
                                                                    }
                                                                }
                                                            }
                                                            N(SyntaxKind.CloseParenToken);
                                                        }
                                                        N(SyntaxKind.GreaterThanGreaterThanGreaterThanToken);
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

        [Fact]
        public void TestGenericWithExtraCommasAndMissingTypes1()
        {
            UsingTree("""
                class C
                {
                    void M()
                    {
                        var added = Goo<string,,>.Instance;
                    }
                }
                """,
                // (5,32): error CS1031: Type expected
                //         var added = Goo<string,,>.Instance;
                Diagnostic(ErrorCode.ERR_TypeExpected, ",").WithLocation(5, 32),
                // (5,33): error CS1031: Type expected
                //         var added = Goo<string,,>.Instance;
                Diagnostic(ErrorCode.ERR_TypeExpected, ">").WithLocation(5, 33));

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
                                            N(SyntaxKind.SimpleMemberAccessExpression);
                                            {
                                                N(SyntaxKind.GenericName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Goo");
                                                    N(SyntaxKind.TypeArgumentList);
                                                    {
                                                        N(SyntaxKind.LessThanToken);
                                                        N(SyntaxKind.PredefinedType);
                                                        {
                                                            N(SyntaxKind.StringKeyword);
                                                        }
                                                        N(SyntaxKind.CommaToken);
                                                        M(SyntaxKind.IdentifierName);
                                                        {
                                                            M(SyntaxKind.IdentifierToken);
                                                        }
                                                        N(SyntaxKind.CommaToken);
                                                        M(SyntaxKind.IdentifierName);
                                                        {
                                                            M(SyntaxKind.IdentifierToken);
                                                        }
                                                        N(SyntaxKind.GreaterThanToken);
                                                    }
                                                }
                                                N(SyntaxKind.DotToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Instance");
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

        [Fact]
        public void TestGenericWithExtraCommasAndMissingTypes2()
        {
            UsingTree("""
                class C
                {
                    void M()
                    {
                        var added = Goo<Id,,>.Instance;
                    }
                }
                """,
                // (5,28): error CS1031: Type expected
                //         var added = Goo<Id,,>.Instance;
                Diagnostic(ErrorCode.ERR_TypeExpected, ",").WithLocation(5, 28),
                // (5,29): error CS1031: Type expected
                //         var added = Goo<Id,,>.Instance;
                Diagnostic(ErrorCode.ERR_TypeExpected, ">").WithLocation(5, 29));

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
                                            N(SyntaxKind.SimpleMemberAccessExpression);
                                            {
                                                N(SyntaxKind.GenericName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Goo");
                                                    N(SyntaxKind.TypeArgumentList);
                                                    {
                                                        N(SyntaxKind.LessThanToken);
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "Id");
                                                        }
                                                        N(SyntaxKind.CommaToken);
                                                        M(SyntaxKind.IdentifierName);
                                                        {
                                                            M(SyntaxKind.IdentifierToken);
                                                        }
                                                        N(SyntaxKind.CommaToken);
                                                        M(SyntaxKind.IdentifierName);
                                                        {
                                                            M(SyntaxKind.IdentifierToken);
                                                        }
                                                        N(SyntaxKind.GreaterThanToken);
                                                    }
                                                }
                                                N(SyntaxKind.DotToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Instance");
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

        [Fact]
        public void TestGenericWithExtraCommasAndMissingTypes3()
        {
            UsingTree("""
                class C
                {
                    void M()
                    {
                        var added = Goo<,Id,>.Instance;
                    }
                }
                """,
                // (5,25): error CS1031: Type expected
                //         var added = Goo<,Id,>.Instance;
                Diagnostic(ErrorCode.ERR_TypeExpected, ",").WithLocation(5, 25),
                // (5,29): error CS1031: Type expected
                //         var added = Goo<,Id,>.Instance;
                Diagnostic(ErrorCode.ERR_TypeExpected, ">").WithLocation(5, 29));

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
                                            N(SyntaxKind.SimpleMemberAccessExpression);
                                            {
                                                N(SyntaxKind.GenericName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Goo");
                                                    N(SyntaxKind.TypeArgumentList);
                                                    {
                                                        N(SyntaxKind.LessThanToken);
                                                        M(SyntaxKind.IdentifierName);
                                                        {
                                                            M(SyntaxKind.IdentifierToken);
                                                        }
                                                        N(SyntaxKind.CommaToken);
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "Id");
                                                        }
                                                        N(SyntaxKind.CommaToken);
                                                        M(SyntaxKind.IdentifierName);
                                                        {
                                                            M(SyntaxKind.IdentifierToken);
                                                        }
                                                        N(SyntaxKind.GreaterThanToken);
                                                    }
                                                }
                                                N(SyntaxKind.DotToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Instance");
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

        [Fact]
        public void TestGenericWithExtraCommasAndMissingTypes4()
        {
            UsingTree("""
                class C
                {
                    void M()
                    {
                        var added = Goo<,,Id>.Instance;
                    }
                }
                """,
                // (5,25): error CS1031: Type expected
                //         var added = Goo<,,Id>.Instance;
                Diagnostic(ErrorCode.ERR_TypeExpected, ",").WithLocation(5, 25),
                // (5,26): error CS1031: Type expected
                //         var added = Goo<,,Id>.Instance;
                Diagnostic(ErrorCode.ERR_TypeExpected, ",").WithLocation(5, 26));

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
                                            N(SyntaxKind.SimpleMemberAccessExpression);
                                            {
                                                N(SyntaxKind.GenericName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Goo");
                                                    N(SyntaxKind.TypeArgumentList);
                                                    {
                                                        N(SyntaxKind.LessThanToken);
                                                        M(SyntaxKind.IdentifierName);
                                                        {
                                                            M(SyntaxKind.IdentifierToken);
                                                        }
                                                        N(SyntaxKind.CommaToken);
                                                        M(SyntaxKind.IdentifierName);
                                                        {
                                                            M(SyntaxKind.IdentifierToken);
                                                        }
                                                        N(SyntaxKind.CommaToken);
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "Id");
                                                        }
                                                        N(SyntaxKind.GreaterThanToken);
                                                    }
                                                }
                                                N(SyntaxKind.DotToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Instance");
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

        [Fact]
        public void TestGenericWithExtraCommasAndMissingTypes5()
        {
            UsingTree("""
                class C
                {
                    void M()
                    {
                        var added = Goo<Id[],,>.Instance;
                    }
                }
                """,
                // (5,30): error CS1031: Type expected
                //         var added = Goo<Id[],,>.Instance;
                Diagnostic(ErrorCode.ERR_TypeExpected, ",").WithLocation(5, 30),
                // (5,31): error CS1031: Type expected
                //         var added = Goo<Id[],,>.Instance;
                Diagnostic(ErrorCode.ERR_TypeExpected, ">").WithLocation(5, 31));

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
                                            N(SyntaxKind.SimpleMemberAccessExpression);
                                            {
                                                N(SyntaxKind.GenericName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Goo");
                                                    N(SyntaxKind.TypeArgumentList);
                                                    {
                                                        N(SyntaxKind.LessThanToken);
                                                        N(SyntaxKind.ArrayType);
                                                        {
                                                            N(SyntaxKind.IdentifierName);
                                                            {
                                                                N(SyntaxKind.IdentifierToken, "Id");
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
                                                        M(SyntaxKind.IdentifierName);
                                                        {
                                                            M(SyntaxKind.IdentifierToken);
                                                        }
                                                        N(SyntaxKind.CommaToken);
                                                        M(SyntaxKind.IdentifierName);
                                                        {
                                                            M(SyntaxKind.IdentifierToken);
                                                        }
                                                        N(SyntaxKind.GreaterThanToken);
                                                    }
                                                }
                                                N(SyntaxKind.DotToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Instance");
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

        [Fact]
        public void TestGenericWithExtraCommasAndMissingTypes6()
        {
            UsingTree("""
                class C
                {
                    void M()
                    {
                        var added = Goo<(int i, int j),,>.Instance;
                    }
                }
                """,
                // (5,40): error CS1031: Type expected
                //         var added = Goo<(int i, int j),,>.Instance;
                Diagnostic(ErrorCode.ERR_TypeExpected, ",").WithLocation(5, 40),
                // (5,41): error CS1031: Type expected
                //         var added = Goo<(int i, int j),,>.Instance;
                Diagnostic(ErrorCode.ERR_TypeExpected, ">").WithLocation(5, 41));

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
                                            N(SyntaxKind.SimpleMemberAccessExpression);
                                            {
                                                N(SyntaxKind.GenericName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Goo");
                                                    N(SyntaxKind.TypeArgumentList);
                                                    {
                                                        N(SyntaxKind.LessThanToken);
                                                        N(SyntaxKind.TupleType);
                                                        {
                                                            N(SyntaxKind.OpenParenToken);
                                                            N(SyntaxKind.TupleElement);
                                                            {
                                                                N(SyntaxKind.PredefinedType);
                                                                {
                                                                    N(SyntaxKind.IntKeyword);
                                                                }
                                                                N(SyntaxKind.IdentifierToken, "i");
                                                            }
                                                            N(SyntaxKind.CommaToken);
                                                            N(SyntaxKind.TupleElement);
                                                            {
                                                                N(SyntaxKind.PredefinedType);
                                                                {
                                                                    N(SyntaxKind.IntKeyword);
                                                                }
                                                                N(SyntaxKind.IdentifierToken, "j");
                                                            }
                                                            N(SyntaxKind.CloseParenToken);
                                                        }
                                                        N(SyntaxKind.CommaToken);
                                                        M(SyntaxKind.IdentifierName);
                                                        {
                                                            M(SyntaxKind.IdentifierToken);
                                                        }
                                                        N(SyntaxKind.CommaToken);
                                                        M(SyntaxKind.IdentifierName);
                                                        {
                                                            M(SyntaxKind.IdentifierToken);
                                                        }
                                                        N(SyntaxKind.GreaterThanToken);
                                                    }
                                                }
                                                N(SyntaxKind.DotToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Instance");
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

        [Fact]
        public void TestGenericWithExtraCommasAndMissingTypes7()
        {
            UsingTree("""
                class C
                {
                    void M()
                    {
                        var added = Goo<K<int>,,>.Instance;
                    }
                }
                """,
                // (5,32): error CS1031: Type expected
                //         var added = Goo<K<int>,,>.Instance;
                Diagnostic(ErrorCode.ERR_TypeExpected, ",").WithLocation(5, 32),
                // (5,33): error CS1031: Type expected
                //         var added = Goo<K<int>,,>.Instance;
                Diagnostic(ErrorCode.ERR_TypeExpected, ">").WithLocation(5, 33));

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
                                            N(SyntaxKind.SimpleMemberAccessExpression);
                                            {
                                                N(SyntaxKind.GenericName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Goo");
                                                    N(SyntaxKind.TypeArgumentList);
                                                    {
                                                        N(SyntaxKind.LessThanToken);
                                                        N(SyntaxKind.GenericName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "K");
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
                                                        N(SyntaxKind.CommaToken);
                                                        M(SyntaxKind.IdentifierName);
                                                        {
                                                            M(SyntaxKind.IdentifierToken);
                                                        }
                                                        N(SyntaxKind.CommaToken);
                                                        M(SyntaxKind.IdentifierName);
                                                        {
                                                            M(SyntaxKind.IdentifierToken);
                                                        }
                                                        N(SyntaxKind.GreaterThanToken);
                                                    }
                                                }
                                                N(SyntaxKind.DotToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Instance");
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

        [Fact]
        public void TestGenericWithExtraCommasAndMissingTypes8()
        {
            UsingTree("""
                class C
                {
                    void M()
                    {
                        var added = Goo<K<int,,>,,>.Instance;
                    }
                }
                """,
                // (5,31): error CS1031: Type expected
                //         var added = Goo<K<int,,>,,>.Instance;
                Diagnostic(ErrorCode.ERR_TypeExpected, ",").WithLocation(5, 31),
                // (5,32): error CS1031: Type expected
                //         var added = Goo<K<int,,>,,>.Instance;
                Diagnostic(ErrorCode.ERR_TypeExpected, ">").WithLocation(5, 32),
                // (5,34): error CS1031: Type expected
                //         var added = Goo<K<int,,>,,>.Instance;
                Diagnostic(ErrorCode.ERR_TypeExpected, ",").WithLocation(5, 34),
                // (5,35): error CS1031: Type expected
                //         var added = Goo<K<int,,>,,>.Instance;
                Diagnostic(ErrorCode.ERR_TypeExpected, ">").WithLocation(5, 35));

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
                                            N(SyntaxKind.SimpleMemberAccessExpression);
                                            {
                                                N(SyntaxKind.GenericName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Goo");
                                                    N(SyntaxKind.TypeArgumentList);
                                                    {
                                                        N(SyntaxKind.LessThanToken);
                                                        N(SyntaxKind.GenericName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "K");
                                                            N(SyntaxKind.TypeArgumentList);
                                                            {
                                                                N(SyntaxKind.LessThanToken);
                                                                N(SyntaxKind.PredefinedType);
                                                                {
                                                                    N(SyntaxKind.IntKeyword);
                                                                }
                                                                N(SyntaxKind.CommaToken);
                                                                M(SyntaxKind.IdentifierName);
                                                                {
                                                                    M(SyntaxKind.IdentifierToken);
                                                                }
                                                                N(SyntaxKind.CommaToken);
                                                                M(SyntaxKind.IdentifierName);
                                                                {
                                                                    M(SyntaxKind.IdentifierToken);
                                                                }
                                                                N(SyntaxKind.GreaterThanToken);
                                                            }
                                                        }
                                                        N(SyntaxKind.CommaToken);
                                                        M(SyntaxKind.IdentifierName);
                                                        {
                                                            M(SyntaxKind.IdentifierToken);
                                                        }
                                                        N(SyntaxKind.CommaToken);
                                                        M(SyntaxKind.IdentifierName);
                                                        {
                                                            M(SyntaxKind.IdentifierToken);
                                                        }
                                                        N(SyntaxKind.GreaterThanToken);
                                                    }
                                                }
                                                N(SyntaxKind.DotToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Instance");
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
    }
}
