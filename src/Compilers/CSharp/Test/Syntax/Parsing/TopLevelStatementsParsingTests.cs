// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.TopLevelStatements)]
    public sealed class TopLevelStatementsParsingTests : ParsingTests
    {
        public TopLevelStatementsParsingTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void InsertOpenBraceBeforeCodes()
        {
            UsingTree(
@"{
        this.I = i;
    };
}",
                // (4,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(4, 1)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.SimpleAssignmentExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.ThisExpression);
                                    {
                                        N(SyntaxKind.ThisKeyword);
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "I");
                                    }
                                }
                                N(SyntaxKind.EqualsToken);
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
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.EmptyStatement);
                    {
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TestIncompleteGlobalMembers()
        {
            var text = @"
asas]
extern alias A;
asas
using System;
sadasdasd]

[assembly: goo]

class C
{
}


[a]fod;
[b";
            UsingTree(text,
                // (2,5): error CS1001: Identifier expected
                // asas]
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "]").WithLocation(2, 5),
                // (2,5): error CS1003: Syntax error, ',' expected
                // asas]
                Diagnostic(ErrorCode.ERR_SyntaxError, "]").WithArguments(",").WithLocation(2, 5),
                // (2,6): error CS1002: ; expected
                // asas]
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(2, 6),
                // (3,1): error CS0439: An extern alias declaration must precede all other elements defined in the namespace
                // extern alias A;
                Diagnostic(ErrorCode.ERR_ExternAfterElements, "extern").WithLocation(3, 1),
                // (4,1): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // asas
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "asas").WithLocation(4, 1),
                // (5,1): error CS1529: A using clause must precede all other elements defined in the namespace except extern alias declarations
                // using System;
                Diagnostic(ErrorCode.ERR_UsingAfterElements, "using System;").WithLocation(5, 1),
                // (6,10): error CS1001: Identifier expected
                // sadasdasd]
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "]").WithLocation(6, 10),
                // (6,10): error CS1003: Syntax error, ',' expected
                // sadasdasd]
                Diagnostic(ErrorCode.ERR_SyntaxError, "]").WithArguments(",").WithLocation(6, 10),
                // (6,11): error CS1002: ; expected
                // sadasdasd]
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 11),
                // (8,2): error CS1730: Assembly and module attributes must precede all other elements defined in a file except using clauses and extern alias declarations
                // [assembly: goo]
                Diagnostic(ErrorCode.ERR_GlobalAttributesNotFirst, "assembly").WithLocation(8, 2),
                // (15,1): error CS8803: Top-level statements must precede namespace and type declarations.
                // [a]fod;
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "[a]fod;").WithLocation(15, 1),
                // (16,3): error CS1003: Syntax error, ']' expected
                // [b
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("]").WithLocation(16, 3)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "asas");
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "asas");
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
                                N(SyntaxKind.IdentifierToken, "sadasdasd");
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.AttributeList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Attribute);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "fod");
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        M(SyntaxKind.CloseBracketToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void IncompleteTopLevelOperator()
        {
            var text = @"
fg implicit//
class C { }
";
            UsingTree(text,
                // (2,1): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // fg implicit//
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "fg").WithArguments("+").WithLocation(2, 1),
                // (2,4): error CS1003: Syntax error, 'operator' expected
                // fg implicit//
                Diagnostic(ErrorCode.ERR_SyntaxError, "implicit").WithArguments("operator").WithLocation(2, 4),
                // (2,4): error CS1037: Overloadable operator expected
                // fg implicit//
                Diagnostic(ErrorCode.ERR_OvlOperatorExpected, "implicit").WithLocation(2, 4),
                // (2,12): error CS1003: Syntax error, '(' expected
                // fg implicit//
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(2, 12),
                // (2,12): error CS1026: ) expected
                // fg implicit//
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(2, 12),
                // (2,12): error CS1002: ; expected
                // fg implicit//
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(2, 12)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.OperatorDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "fg");
                    }
                    M(SyntaxKind.OperatorKeyword);
                    M(SyntaxKind.PlusToken);
                    M(SyntaxKind.ParameterList);
                    {
                        M(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TestGlobalNamespaceWithOpenBraceBeforeNamespace()
        {
            var text = "{ namespace n { }";
            UsingTree(text,
                // (1,3): error CS1513: } expected
                // { namespace n { }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "namespace").WithLocation(1, 3)
                );
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.NamespaceDeclaration);
                {
                    N(SyntaxKind.NamespaceKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "n");
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void CS1056ERR_UnexpectedCharacter_EscapedBackslash()
        {
            var test = @"using S\u005Cu0065 = System;
class A
{
int x = 0;
}
";
            UsingTree(test,
                // (1,8): error CS1002: ; expected
                // using S\u005Cu0065 = System;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, @"\u005C").WithLocation(1, 8),
                // (1,8): error CS1056: Unexpected character '\u005C'
                // using S\u005Cu0065 = System;
                Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments(@"\u005C").WithLocation(1, 8)
                );
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "S");
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "u0065");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "System");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "A");
                    N(SyntaxKind.OpenBraceToken);
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
                                N(SyntaxKind.IdentifierToken, "x");
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
        }

        [Fact]
        public void TestNegInvalidExternAlias01()
        {
            UsingTree(Resources.InvalidExternAlias01,
                // (1,1): error CS0106: The modifier 'extern' is not valid for this item
                // extern alias libAlias=other_library.dll;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "extern").WithArguments("extern").WithLocation(1, 1)
                );
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.ExternKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "alias");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "libAlias");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.SimpleMemberAccessExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "other_library");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "dll");
                                        }
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "myClass");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void SeparatorsOfSeparatedSyntaxLists()
        {
            var test = "int goo(int a, int b, int c) {}";
            UsingTree(test);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "goo");
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
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GetDiagnosticsOnMissingToken()
        {
            var test = @"c1<t";
            UsingTree(test,
                // (1,5): error CS1002: ; expected
                // c1<t
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 5)
                );

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
                                N(SyntaxKind.IdentifierToken, "c1");
                            }
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "t");
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
        public void TestGetNextTokenExcludingSkippedTokens()
        {
            var test =
@"garbage
using goo.bar;
";
            UsingTree(test,
                // (1,1): error CS0116: A namespace cannot directly contain members such as fields or methods
                // garbage
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "garbage").WithLocation(1, 1)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
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
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [WorkItem(541587, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541587")]
        [Fact]
        public void GetDiagnosticsOnMissingToken3()
        {
            var test = @"class c2 4";
            UsingTree(test,
                // (1,10): error CS1514: { expected
                // class c2 4
                Diagnostic(ErrorCode.ERR_LbraceExpected, "4").WithLocation(1, 10),
                // (1,10): error CS1513: } expected
                // class c2 4
                Diagnostic(ErrorCode.ERR_RbraceExpected, "4").WithLocation(1, 10),
                // (1,10): error CS8803: Top-level statements must precede namespace and type declarations.
                // class c2 4
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "4").WithLocation(1, 10),
                // (1,11): error CS1002: ; expected
                // class c2 4
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 11)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "c2");
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "4");
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void CS0071ERR_ExplicitEventFieldImpl()
        {
            var test = @"
class Test : Itest
{
   event D ITest.E()   // CS0071
   {
   }
   public static int Main()
   {
       return 1;
   }
}
";
            UsingTree(test,
                // (4,17): error CS0071: An explicit interface implementation of an event must use event accessor syntax
                //    event D ITest.E()   // CS0071
                Diagnostic(ErrorCode.ERR_ExplicitEventFieldImpl, ".").WithLocation(4, 17),
                // (4,20): error CS8124: Tuple must contain at least two elements.
                //    event D ITest.E()   // CS0071
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(4, 20),
                // (5,4): error CS1519: Invalid token '{' in class, record, struct, or interface member declaration
                //    {
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(5, 4),
                // (7,4): error CS8803: Top-level statements must precede namespace and type declarations.
                //    public static int Main()
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, @"public static int Main()
   {
       return 1;
   }").WithLocation(7, 4),
                // (7,4): error CS0106: The modifier 'public' is not valid for this item
                //    public static int Main()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "public").WithArguments("public").WithLocation(7, 4),
                // (11,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(11, 1)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Test");
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Itest");
                            }
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.EventDeclaration);
                    {
                        N(SyntaxKind.EventKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "D");
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "ITest");
                            }
                            N(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.IdentifierToken, "E");
                        M(SyntaxKind.AccessorList);
                        {
                            M(SyntaxKind.OpenBraceToken);
                            M(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.IncompleteMember);
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
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void CS1514ERR_LbraceExpected02()
        {
            var test = @"public class S.D 
{
    public string P.P { get; set; }
}
";
            UsingTree(test,
                // (1,15): error CS1514: { expected
                // public class S.D 
                Diagnostic(ErrorCode.ERR_LbraceExpected, ".").WithLocation(1, 15),
                // (1,15): error CS1513: } expected
                // public class S.D 
                Diagnostic(ErrorCode.ERR_RbraceExpected, ".").WithLocation(1, 15),
                // (1,15): error CS1022: Type or namespace definition, or end-of-file expected
                // public class S.D 
                Diagnostic(ErrorCode.ERR_EOFExpected, ".").WithLocation(1, 15),
                // (1,16): error CS8803: Top-level statements must precede namespace and type declarations.
                // public class S.D 
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, @"D 
").WithLocation(1, 16),
                // (1,17): error CS1001: Identifier expected
                // public class S.D 
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 17),
                // (1,17): error CS1002: ; expected
                // public class S.D 
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 17),
                // (2,2): error CS1513: } expected
                // {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(2, 2),
                // (4,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(4, 1));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "S");
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "D");
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
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
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.StringKeyword);
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "P");
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.IdentifierToken, "P");
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void CS1022ERR_EOFExpected02()
        {
            var test = @" > Roslyn.Utilities.dll!  Basic";
            UsingTree(test,
                // (1,2): error CS1525: Invalid expression term '>'
                //  > Roslyn.Utilities.dll!  Basic
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ">").WithArguments(">").WithLocation(1, 2),
                // (1,27): error CS1002: ; expected
                //  > Roslyn.Utilities.dll!  Basic
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "Basic").WithLocation(1, 27),
                // (1,32): error CS1001: Identifier expected
                //  > Roslyn.Utilities.dll!  Basic
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 32),
                // (1,32): error CS1002: ; expected
                //  > Roslyn.Utilities.dll!  Basic
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 32)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.GreaterThanExpression);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.GreaterThanToken);
                            N(SyntaxKind.SuppressNullableWarningExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.SimpleMemberAccessExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Roslyn");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Utilities");
                                        }
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "dll");
                                    }
                                }
                                N(SyntaxKind.ExclamationToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
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
                                N(SyntaxKind.IdentifierToken, "Basic");
                            }
                            M(SyntaxKind.VariableDeclarator);
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
        public void CS0267ERR_PartialMisplaced_Delegate1()
        {
            var test = @"
partial delegate E { }
";
            CreateCompilation(test).VerifyDiagnostics(
                    // (2,18): error CS0246: The type or namespace name 'E' could not be found (are you missing a using directive or an assembly reference?)
                    // partial delegate E { }
                    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "E").WithArguments("E").WithLocation(2, 18),
                    // (2,20): error CS1001: Identifier expected
                    // partial delegate E { }
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(2, 20),
                    // (2,20): error CS1003: Syntax error, '(' expected
                    // partial delegate E { }
                    Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments("(").WithLocation(2, 20),
                    // (2,20): error CS1026: ) expected
                    // partial delegate E { }
                    Diagnostic(ErrorCode.ERR_CloseParenExpected, "{").WithLocation(2, 20),
                    // (2,20): error CS1002: ; expected
                    // partial delegate E { }
                    Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(2, 20),
                    // (2,20): error CS8803: Top-level statements must precede namespace and type declarations.
                    // partial delegate E { }
                    Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "{ }").WithLocation(2, 20),
                    // (2,20): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                    // partial delegate E { }
                    Diagnostic(ErrorCode.ERR_PartialMisplaced, "").WithLocation(2, 20)
                );
            UsingTree(test,
                // (2,20): error CS1001: Identifier expected
                // partial delegate E { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(2, 20),
                // (2,20): error CS1003: Syntax error, '(' expected
                // partial delegate E { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments("(").WithLocation(2, 20),
                // (2,20): error CS1026: ) expected
                // partial delegate E { }
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "{").WithLocation(2, 20),
                // (2,20): error CS1002: ; expected
                // partial delegate E { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(2, 20),
                // (2,20): error CS8803: Top-level statements must precede namespace and type declarations.
                // partial delegate E { }
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "{ }").WithLocation(2, 20)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.DelegateDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "E");
                    }
                    M(SyntaxKind.IdentifierToken);
                    M(SyntaxKind.ParameterList);
                    {
                        M(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.GlobalStatement);
                {
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

        [Fact, WorkItem(543622, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543622")]
        public void CS0116ERR__NamespaceUnexpected()
        {
            var test = @"{
    get
    {
        ParseDefaultDir();
    }
}";
            UsingTree(test,
                // (2,8): error CS1002: ; expected
                //     get
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(2, 8)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "get");
                            }
                            M(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.InvocationExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "ParseDefaultDir");
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
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Multiplication()
        {
            // pointer decl
            string test = @"a.b * c;";
            UsingTree(test);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PointerType);
                            {
                                N(SyntaxKind.QualifiedName);
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
                                N(SyntaxKind.AsteriskToken);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "c");
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
        [Trait("Feature", "Directives")]
        public void TestNegIfEndifDirectivesWithBadCode()
        {
            var test =
@"#if true
#else
#endif
aeu";
            UsingTree(test,
                // (4,4): error CS1001: Identifier expected
                // aeu
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(4, 4),
                // (4,4): error CS1002: ; expected
                // aeu
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 4)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "aeu");
                            }
                            M(SyntaxKind.VariableDeclarator);
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
        public void TestExternWithoutAlias()
        {
            var test = "extern a;";
            UsingTree(test,
                // (1,8): error CS0116: A namespace cannot directly contain members such as fields or methods
                // extern a;
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "a").WithLocation(1, 8)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.ExternKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.EmptyStatement);
                    {
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(528655, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528655")]
        public void ErrorSymbolForInvalidCode()
        {
            var test = @"
public class A 
{
	int goo	{	void goo() {}	} // Error
	static int Main() {	return 1;    }
}
";
            UsingTree(test,
                // (4,12): error CS1513: } expected
                // 	int goo	{	void goo() {}	} // Error
                Diagnostic(ErrorCode.ERR_RbraceExpected, "void").WithLocation(4, 12),
                // (5,2): error CS8803: Top-level statements must precede namespace and type declarations.
                // 	static int Main() {	return 1;    }
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "static int Main() {	return 1;    }").WithLocation(5, 2),
                // (6,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(6, 1)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "A");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "goo");
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            M(SyntaxKind.CloseBraceToken);
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
                    {
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void InvalidAlias()
        {
            string test =
"extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}";

            UsingTree(test,
                // (1,20): error CS1026: ) expected
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "*").WithLocation(1, 20),
                // (1,20): error CS1002: ; expected
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "*").WithLocation(1, 20),
                // (1,21): error CS1040: Preprocessor directives must appear as the first non-whitespace character on a line
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_BadDirectivePlacement, "#").WithLocation(1, 21),
                // (1,61): error CS1733: Expected expression
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 61),
                // (1,61): error CS1002: ; expected
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 61));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
                    {
                        N(SyntaxKind.ExternKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "alias");
                        }
                        N(SyntaxKind.IdentifierToken, "Alias");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            M(SyntaxKind.CloseParenToken);
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.PointerIndirectionExpression);
                        {
                            N(SyntaxKind.AsteriskToken);
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
        public void TopLevelIndexer()
        {
            var test = @"
this[double E] { get { return /*<bind>*/E/*</bind>*/; } }
";
            UsingTree(test,
                // (2,6): error CS1525: Invalid expression term 'double'
                // this[double E] { get { return /*<bind>*/E/*</bind>*/; } }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "double").WithArguments("double").WithLocation(2, 6),
                // (2,13): error CS1003: Syntax error, ',' expected
                // this[double E] { get { return /*<bind>*/E/*</bind>*/; } }
                Diagnostic(ErrorCode.ERR_SyntaxError, "E").WithArguments(",").WithLocation(2, 13),
                // (2,16): error CS1002: ; expected
                // this[double E] { get { return /*<bind>*/E/*</bind>*/; } }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(2, 16),
                // (2,22): error CS1002: ; expected
                // this[double E] { get { return /*<bind>*/E/*</bind>*/; } }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(2, 22)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
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
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.DoubleKeyword);
                                    }
                                }
                                M(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "E");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
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
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "get");
                            }
                            M(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ReturnStatement);
                            {
                                N(SyntaxKind.ReturnKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "E");
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void UnrecognizedGenericTypeReference()
        {
            string test = "/*<bind>*/C<object, string/*</bind>*/";
            UsingTree(test,
                // (1,13): error CS1525: Invalid expression term 'object'
                // /*<bind>*/C<object, string/*</bind>*/
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "object").WithArguments("object").WithLocation(1, 13),
                // (1,19): error CS1002: ; expected
                // /*<bind>*/C<object, string/*</bind>*/
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(1, 19),
                // (1,19): error CS1022: Type or namespace definition, or end-of-file expected
                // /*<bind>*/C<object, string/*</bind>*/
                Diagnostic(ErrorCode.ERR_EOFExpected, ",").WithLocation(1, 19),
                // (1,38): error CS1001: Identifier expected
                // /*<bind>*/C<object, string/*</bind>*/
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 38),
                // (1,38): error CS1002: ; expected
                // /*<bind>*/C<object, string/*</bind>*/
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 38)
                );

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
                                N(SyntaxKind.IdentifierToken, "C");
                            }
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.ObjectKeyword);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            M(SyntaxKind.VariableDeclarator);
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
        public void IncompleteOperator()
        {
            UsingTree(@"C operator +(C lhs, C rhs) {",
                // (1,29): error CS1513: } expected
                // C operator +(C lhs, C rhs) {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 29)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.OperatorDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "C");
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PlusToken);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
                            }
                            N(SyntaxKind.IdentifierToken, "lhs");
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
                            }
                            N(SyntaxKind.IdentifierToken, "rhs");
                        }
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
        public void NewKeyword()
        {
            UsingTree(@"new ",
                // (1,5): error CS1526: A new expression requires (), [], or {} after type
                // new 
                Diagnostic(ErrorCode.ERR_BadNewExpr, "").WithLocation(1, 5),
                // (1,5): error CS1002: ; expected
                // new 
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 5)
                );

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

        [Theory]
        [InlineData(LanguageVersion.CSharp9)]
        [InlineData(LanguageVersion.Preview)]
        public void TupleUnsupportedInUsingStatement(LanguageVersion version)
        {
            var test = @"
using VT2 = (int, int);
";

            var tree = UsingTree(test, TestOptions.RegularPreview.WithLanguageVersion(version));

            // No parse errors here regardless of version.  Errors are just semantic.
            tree.GetDiagnostics().Verify();

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.NameEquals);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "VT2");
                        }
                        N(SyntaxKind.EqualsToken);
                    }
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
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(530131, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530131")]
        public void MetadataReferenceWithInvalidAlias()
        {
            var test = "extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}";

            UsingTree(test,
                // (1,20): error CS1026: ) expected
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "*").WithLocation(1, 20),
                // (1,20): error CS1002: ; expected
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "*").WithLocation(1, 20),
                // (1,21): error CS1040: Preprocessor directives must appear as the first non-whitespace character on a line
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_BadDirectivePlacement, "#").WithLocation(1, 21),
                // (1,61): error CS1733: Expected expression
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 61),
                // (1,61): error CS1002: ; expected
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 61));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
                    {
                        N(SyntaxKind.ExternKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "alias");
                        }
                        N(SyntaxKind.IdentifierToken, "Alias");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            M(SyntaxKind.CloseParenToken);
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.PointerIndirectionExpression);
                        {
                            N(SyntaxKind.AsteriskToken);
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
        public void Identifier_01()
        {
            var test = "e";

            UsingTree(test,
                // (1,2): error CS1001: Identifier expected
                // e
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 2),
                // (1,2): error CS1002: ; expected
                // e
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 2)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "e");
                            }
                            M(SyntaxKind.VariableDeclarator);
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
        public void Identifier_02()
        {
            var test =
@"
[Flags]
e
";

            UsingTree(test,
                // (3,1): error CS0116: A namespace cannot directly contain members such as fields or methods
                // e
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "e").WithLocation(3, 1)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Flags");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TestSkippedText()
        {
            var test = "abc using";

            UsingTree(test,
                // (1,1): error CS0116: A namespace cannot directly contain members such as fields or methods
                // abc using
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "abc").WithLocation(1, 1),
                // (1,10): error CS1001: Identifier expected
                // abc using
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 10),
                // (1,10): error CS1002: ; expected
                // abc using
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 10)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void UsingLocalDeclaration_01()
        {
            var test = "using var a = new MyDisposable();";

            UsingTree(test);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
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
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "MyDisposable");
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void UsingLocalDeclaration_02()
        {
            var test = "using static type name;";

            UsingTree(test,
                // (1,7): error CS9229: Modifiers cannot be placed on using declarations
                // using static type name;
                Diagnostic(ErrorCode.ERR_NoModifiersOnUsing, "static").WithLocation(1, 7)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "type");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "name");
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
        public void UsingLocalDeclaration_03()
        {
            var test = "using volatile;";

            UsingTree(test,
                // (1,7): error CS9229: Modifiers cannot be placed on using declarations
                // using volatile;
                Diagnostic(ErrorCode.ERR_NoModifiersOnUsing, "volatile").WithLocation(1, 7),
                // (1,15): error CS1031: Type expected
                // using volatile;
                Diagnostic(ErrorCode.ERR_TypeExpected, ";").WithLocation(1, 15),
                // (1,15): error CS1001: Identifier expected
                // using volatile;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 15)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.VolatileKeyword);
                        M(SyntaxKind.VariableDeclaration);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            M(SyntaxKind.VariableDeclarator);
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
        public void UsingLocalDeclaration_04()
        {
            var test = "using const;";

            UsingTree(test,
                // (1,7): error CS9229: Modifiers cannot be placed on using declarations
                // using const;
                Diagnostic(ErrorCode.ERR_NoModifiersOnUsing, "const").WithLocation(1, 7),
                // (1,12): error CS1031: Type expected
                // using const;
                Diagnostic(ErrorCode.ERR_TypeExpected, ";").WithLocation(1, 12),
                // (1,12): error CS1001: Identifier expected
                // using const;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 12),
                // (1,12): error CS0145: A const field requires a value to be provided
                // using const;
                Diagnostic(ErrorCode.ERR_ConstValueRequired, ";").WithLocation(1, 12)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.ConstKeyword);
                        M(SyntaxKind.VariableDeclaration);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            M(SyntaxKind.VariableDeclarator);
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
        public void UsingLocalDeclaration_05()
        {
            var test = "using ref;";

            UsingTree(test,
                // (1,10): error CS1031: Type expected
                // using ref;
                Diagnostic(ErrorCode.ERR_TypeExpected, ";").WithLocation(1, 10),
                // (1,10): error CS1001: Identifier expected
                // using ref;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 10)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.RefType);
                            {
                                N(SyntaxKind.RefKeyword);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.VariableDeclarator);
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
        public void UsingLocalDeclaration_06()
        {
            var test = "using readonly;";

            UsingTree(test,
                // (1,7): error CS9229: Modifiers cannot be placed on using declarations
                // using readonly;
                Diagnostic(ErrorCode.ERR_NoModifiersOnUsing, "readonly").WithLocation(1, 7),
                // (1,15): error CS1031: Type expected
                // using readonly;
                Diagnostic(ErrorCode.ERR_TypeExpected, ";").WithLocation(1, 15),
                // (1,15): error CS1001: Identifier expected
                // using readonly;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 15)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        M(SyntaxKind.VariableDeclaration);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            M(SyntaxKind.VariableDeclarator);
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
        public void UsingDirective_01()
        {
            var test = "using static type;";

            UsingTree(test);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "type");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void UsingDirective_02()
        {
            var test = "using type;";

            UsingTree(test);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "type");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void UsingDirective_03()
        {
            var test = "using alias = type;";

            UsingTree(test);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.NameEquals);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "alias");
                        }
                        N(SyntaxKind.EqualsToken);
                    }
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "type");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void UsingDirective_04()
        {
            var test = "using ns.type;";

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
                            N(SyntaxKind.IdentifierToken, "ns");
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "type");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void UsingDirective_05()
        {
            var test = "using static alias = type;";

            UsingTree(test);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.NameEquals);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "alias");
                        }
                        N(SyntaxKind.EqualsToken);
                    }
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "type");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void UsingDirective_06()
        {
            var test = "using int.Parse name = value;";

            UsingTree(test,
                // (1,10): error CS1001: Identifier expected
                // using int.Parse name = value;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ".").WithLocation(1, 10),
                // (1,10): error CS1003: Syntax error, ',' expected
                // using int.Parse name = value;
                Diagnostic(ErrorCode.ERR_SyntaxError, ".").WithArguments(",").WithLocation(1, 10),
                // (1,11): error CS1002: ; expected
                // using int.Parse name = value;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "Parse").WithLocation(1, 11)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
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
                                N(SyntaxKind.IdentifierToken, "Parse");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "name");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "value");
                                    }
                                }
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
        public void UsingDirective_07()
        {
            var test = "using int (x, y)";

            UsingTree(test,
                // (1,11): error CS1001: Identifier expected
                // using int (x, y)
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 11),
                // (1,11): error CS1528: Expected ; or = (cannot specify constructor arguments in declaration)
                // using int (x, y)
                Diagnostic(ErrorCode.ERR_BadVarDecl, "(x, y").WithLocation(1, 11),
                // (1,11): error CS1003: Syntax error, '[' expected
                // using int (x, y)
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("[").WithLocation(1, 11),
                // (1,16): error CS1003: Syntax error, ']' expected
                // using int (x, y)
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("]").WithLocation(1, 16),
                // (1,17): error CS1002: ; expected
                // using int (x, y)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 17)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                                N(SyntaxKind.BracketedArgumentList);
                                {
                                    M(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "x");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "y");
                                        }
                                    }
                                    M(SyntaxKind.CloseBracketToken);
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
        public void UsingDirective_08()
        {
            var test = "using int";

            UsingTree(test,
                // (1,10): error CS1001: Identifier expected
                // using int
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 10),
                // (1,10): error CS1002: ; expected
                // using int
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 10)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            M(SyntaxKind.VariableDeclarator);
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

        [WorkItem(611177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611177")]
        [Fact]
        public void Repro611177()
        {
            var test = @"[_<_[delegate using'";

            UsingTree(test,
                // (1,15): error CS1514: { expected
                // [_<_[delegate using'
                Diagnostic(ErrorCode.ERR_LbraceExpected, "using").WithLocation(1, 15),
                // (1,15): error CS1003: Syntax error, ',' expected
                // [_<_[delegate using'
                Diagnostic(ErrorCode.ERR_SyntaxError, "using").WithArguments(",").WithLocation(1, 15),
                // (1,15): error CS0443: Syntax error; value expected
                // [_<_[delegate using'
                Diagnostic(ErrorCode.ERR_ValueExpected, "").WithLocation(1, 15),
                // (1,15): error CS1003: Syntax error, ']' expected
                // [_<_[delegate using'
                Diagnostic(ErrorCode.ERR_SyntaxError, "using").WithArguments("]").WithLocation(1, 15),
                // (1,15): error CS1003: Syntax error, '>' expected
                // [_<_[delegate using'
                Diagnostic(ErrorCode.ERR_SyntaxError, "using").WithArguments(">").WithLocation(1, 15),
                // (1,15): error CS1003: Syntax error, ']' expected
                // [_<_[delegate using'
                Diagnostic(ErrorCode.ERR_SyntaxError, "using").WithArguments("]").WithLocation(1, 15),
                // (1,20): error CS1031: Type expected
                // [_<_[delegate using'
                Diagnostic(ErrorCode.ERR_TypeExpected, "'").WithLocation(1, 20),
                // (1,20): error CS1001: Identifier expected
                // [_<_[delegate using'
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "'").WithLocation(1, 20),
                // (1,20): error CS1002: ; expected
                // [_<_[delegate using'
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "'").WithLocation(1, 20),
                // (1,20): error CS1010: Newline in constant
                // [_<_[delegate using'
                Diagnostic(ErrorCode.ERR_NewlineInConst, "").WithLocation(1, 20),
                // (1,20): error CS1011: Empty character literal
                // [_<_[delegate using'
                Diagnostic(ErrorCode.ERR_EmptyCharConst, "").WithLocation(1, 20),
                // (1,21): error CS1002: ; expected
                // [_<_[delegate using'
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 21)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.AttributeList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Attribute);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "_");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.ArrayType);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "_");
                                            }
                                            N(SyntaxKind.ArrayRankSpecifier);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                N(SyntaxKind.AnonymousMethodExpression);
                                                {
                                                    N(SyntaxKind.DelegateKeyword);
                                                    M(SyntaxKind.Block);
                                                    {
                                                        M(SyntaxKind.OpenBraceToken);
                                                        M(SyntaxKind.CloseBraceToken);
                                                    }
                                                }
                                                M(SyntaxKind.CommaToken);
                                                M(SyntaxKind.IdentifierName);
                                                {
                                                    M(SyntaxKind.IdentifierToken);
                                                }
                                                M(SyntaxKind.CloseBracketToken);
                                            }
                                        }
                                        M(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                            M(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.UsingKeyword);
                        M(SyntaxKind.VariableDeclaration);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.CharacterLiteralExpression);
                        {
                            N(SyntaxKind.CharacterLiteralToken);
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ConstructorLike_01()
        {
            var test = @"local() {}";

            UsingTree(test,
                // (1,9): error CS1002: ; expected
                // local() {}
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(1, 9)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "local");
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ConstructorLike_02()
        {
            var test = @"static local() {}";

            UsingTree(test,
                // (1,13): error CS1001: Identifier expected
                // static local() {}
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 13)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
                    {
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "local");
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ConstructorLike_03()
        {
            var test = @"[attribute] local() {}";

            UsingTree(test,
                // (1,18): error CS1001: Identifier expected
                // [attribute] local() {}
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 18)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
                    {
                        N(SyntaxKind.AttributeList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Attribute);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "attribute");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "local");
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void UsingAliasTest()
        {
            var test = @"using s = delegate*<void>;";

            UsingTree(test);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.NameEquals);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "s");
                        }
                        N(SyntaxKind.EqualsToken);
                    }
                    N(SyntaxKind.FunctionPointerType);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.FunctionPointerParameterList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.FunctionPointerParameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.VoidKeyword);
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ErrorRecovery_01()
        {
            var test = @"ar";

            UsingTree(test,
                // (1,3): error CS1001: Identifier expected
                // ar
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 3),
                // (1,3): error CS1002: ; expected
                // ar
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 3)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "ar");
                            }
                            M(SyntaxKind.VariableDeclarator);
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
        public void ErrorRecovery_02()
        {
            var test = @"
Console.WriteLine();
ar";

            UsingTree(test,
                // (3,3): error CS1001: Identifier expected
                // ar
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 3),
                // (3,3): error CS1002: ; expected
                // ar
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 3)
                );

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
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Console");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "WriteLine");
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
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "ar");
                            }
                            M(SyntaxKind.VariableDeclarator);
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
        public void ErrorRecovery_03()
        {
            var test = @"
ar
Console.WriteLine();
";

            UsingTree(test,
                // (2,3): error CS1001: Identifier expected
                // ar
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(2, 3),
                // (2,3): error CS1002: ; expected
                // ar
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(2, 3)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "ar");
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
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
                                    N(SyntaxKind.IdentifierToken, "WriteLine");
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
            EOF();
        }

        [Fact]
        public void ErrorRecovery_04()
        {
            var test = @"extern alias ";

            UsingTree(test,
                // (1,8): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // extern alias 
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "alias").WithLocation(1, 8)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.ExternKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "alias");
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ErrorRecovery_05()
        {
            var test = @"using aliasY = X.Y;";

            UsingTree(test);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.NameEquals);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "aliasY");
                        }
                        N(SyntaxKind.EqualsToken);
                    }
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "X");
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Y");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ErrorRecovery_06()
        {
            var test = @"
using X;
using aliasY = X.Y;
";

            UsingTree(test);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
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
                            N(SyntaxKind.IdentifierToken, "aliasY");
                        }
                        N(SyntaxKind.EqualsToken);
                    }
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "X");
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Y");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ErrorRecovery_07()
        {
            var test = @"
System.String[]
using aliasY = X.Y;
";

            UsingTree(test,
                // (2,15): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // System.String[]
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "]").WithLocation(2, 15)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.NameEquals);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "aliasY");
                        }
                        N(SyntaxKind.EqualsToken);
                    }
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "X");
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Y");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ErrorRecovery_08()
        {
            var test = @"
scoped struct A { }
scoped ref struct B { }
scoped readonly ref struct C { }
";

            UsingTree(test,
                // (2,8): error CS1001: Identifier expected
                // scoped struct A { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "struct").WithLocation(2, 8),
                // (2,8): error CS1002: ; expected
                // scoped struct A { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(2, 8),
                // (3,12): error CS1031: Type expected
                // scoped ref struct B { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "struct").WithLocation(3, 12),
                // (4,1): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // scoped readonly ref struct C { }
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "scoped").WithLocation(4, 1));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "A");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "B");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.ScopedKeyword);
                }
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.ReadOnlyKeyword);
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ErrorRecovery_09()
        {
            var test = @"
record class Point(int x, int y);
";

            UsingTree(test);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Point");
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
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ErrorRecovery_10()
        {
            var test = @"
record class Point(int x, int y);
";

            CreateCompilation(test, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                // (2,1): error CS8400: Feature 'top-level statements' is not available in C# 8.0. Please use language version 9.0 or greater.
                // record class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "record ").WithArguments("top-level statements", "9.0").WithLocation(2, 1),
                // (2,1): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                // record class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(2, 1),
                // (2,8): error CS1001: Identifier expected
                // record class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "class").WithLocation(2, 8),
                // (2,8): error CS1002: ; expected
                // record class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "class").WithLocation(2, 8),
                // (2,19): error CS8400: Feature 'primary constructors' is not available in C# 8.0. Please use language version 12.0 or greater.
                // record class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "(int x, int y)").WithArguments("primary constructors", "12.0").WithLocation(2, 19),
                // (2,24): warning CS9113: Parameter 'x' is unread.
                // record class Point(int x, int y);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "x").WithArguments("x").WithLocation(2, 24),
                // (2,31): warning CS9113: Parameter 'y' is unread.
                // record class Point(int x, int y);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "y").WithArguments("y").WithLocation(2, 31)
                );

            UsingTree(test, TestOptions.Regular8,
                // (2,8): error CS1001: Identifier expected
                // record class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "class").WithLocation(2, 8),
                // (2,8): error CS1002: ; expected
                // record class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "class").WithLocation(2, 8)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "record");
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Point");
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
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ErrorRecovery_11()
        {
            var test = @"
global using Goo;
p
global using Bar;
";

            UsingTree(test,
                // (3,1): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // p
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "p").WithLocation(3, 1)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.GlobalKeyword);
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Goo");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.GlobalKeyword);
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Bar");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ErrorRecovery_12()
        {
            var test = @"
using Goo;
p
using Bar;
";

            UsingTree(test,
                // (3,1): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // p
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "p").WithLocation(3, 1)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Goo");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Bar");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ErrorRecovery_13()
        {
            var test = @"
using Goo;
p
using Bar x;
";

            UsingTree(test,
                // (3,2): error CS1001: Identifier expected
                // p
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 2),
                // (3,2): error CS1002: ; expected
                // p
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 2)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Goo");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "p");
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Bar");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
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
        public void ErrorRecovery_14()
        {
            var test = @"
global using Goo;
p
global using Bar x;
";

            UsingTree(test,
                // (4,8): error CS1002: ; expected
                // global using Bar x;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "using").WithLocation(4, 8)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.GlobalKeyword);
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Goo");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "p");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "global");
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Bar");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
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
        public void ErrorRecovery_15()
        {
            var test = @"
			       W   )b
";

            UsingTree(test,
                // (2,15): error CS1001: Identifier expected
                // 			       W   )b
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(2, 15),
                // (2,15): error CS1003: Syntax error, ',' expected
                // 			       W   )b
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(2, 15),
                // (2,16): error CS1002: ; expected
                // 			       W   )b
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "b").WithLocation(2, 16),
                // (2,17): error CS1001: Identifier expected
                // 			       W   )b
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(2, 17),
                // (2,17): error CS1002: ; expected
                // 			       W   )b
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(2, 17));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "W");
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
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
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                            M(SyntaxKind.VariableDeclarator);
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
        [WorkItem("https://github.com/dotnet/roslyn/issues/67050")]
        public void EmptyLocalDeclaration()
        {
            var text = """ 
struct S { }
partial ext X
""";
            UsingTree(text,
                // (1,13): error CS1031: Type expected
                // struct S { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(1, 13),
                // (1,13): error CS1525: Invalid expression term 'partial'
                // struct S { }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("partial").WithLocation(1, 13),
                // (1,13): error CS1003: Syntax error, ',' expected
                // struct S { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(1, 13),
                // (2,1): error CS8803: Top-level statements must precede namespace and type declarations.
                // partial ext X
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "partial").WithLocation(2, 1),
                // (2,9): error CS1002: ; expected
                // partial ext X
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "ext").WithLocation(2, 9),
                // (2,14): error CS1002: ; expected
                // partial ext X
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(2, 14));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "S");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                M(SyntaxKind.GlobalStatement);
                {
                    M(SyntaxKind.LocalDeclarationStatement);
                    {
                        M(SyntaxKind.VariableDeclaration);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
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
                                N(SyntaxKind.IdentifierToken, "ext");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "X");
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }
    }
}
