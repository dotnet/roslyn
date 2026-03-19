// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class DeclarationParsingTests : ParsingTests
    {
        [Fact]
        public void TypeMissingIdentifier_Class01()
        {
            const string source =
                """
                public static class
                {
                    public static int StaticProperty { get; set; }

                    public int Property { get; set; }

                    public void Method() { }

                    public class { }

                    public class Nested { }
                }
                """;

            UsingTree(source,
                // (1,20): error CS1001: Identifier expected
                // public static class
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 20),
                // (9,18): error CS1001: Identifier expected
                //     public class { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(9, 18)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.ClassKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "StaticProperty");
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
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Property");
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
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method");
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
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.ClassKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.ClassKeyword);
                        N(SyntaxKind.IdentifierToken, "Nested");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TypeMissingIdentifier_Class02()
        {
            const string source =
                """
                public class : 
                {
                    public int Property { get; set; }
                    public void Method1() { }

                    public class : object { }
                    public class : List<int>;

                    public void Method2() { }
                }
                """;

            UsingTree(source,
                // (1,14): error CS1001: Identifier expected
                // public class : 
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(1, 14),
                // (1,15): error CS1031: Type expected
                // public class : 
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(1, 15),
                // (6,18): error CS1001: Identifier expected
                //     public class : object { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(6, 18),
                // (7,18): error CS1001: Identifier expected
                //     public class : List<int>;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(7, 18)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ClassKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        M(SyntaxKind.SimpleBaseType);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Property");
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
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method1");
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
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.ClassKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.BaseList);
                        {
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleBaseType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.ObjectKeyword);
                                }
                            }
                        }
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.ClassKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.BaseList);
                        {
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleBaseType);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
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
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method2");
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
        public void TypeMissingIdentifier_Class03()
        {
            const string source =
                """
                public class : Base<T>
                    where T : List<T>
                {
                    public int Property { get; set; }
                    public void Method1() { }

                    public class <U> : Base<U>
                        where U : List<U>;

                    public void Method2() { }
                }
                """;

            UsingTree(source,
                // (1,14): error CS1001: Identifier expected
                // public class : Base<T>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(1, 14),
                // (7,18): error CS1001: Identifier expected
                //     public class <U> : Base<U>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<").WithLocation(7, 18)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ClassKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "Base");
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
                        }
                    }
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.TypeConstraint);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
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
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Property");
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
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method1");
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
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.ClassKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeParameterList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.TypeParameter);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.BaseList);
                        {
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleBaseType);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Base");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.TypeParameterConstraintClause);
                        {
                            N(SyntaxKind.WhereKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.TypeConstraint);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method2");
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
        public void TypeMissingIdentifier_Class04()
        {
            const string source =
                """
                public partial class : Base<T>
                    where T : List<T>
                {
                    public int Property { get; set; }
                    public void Method1() { }

                    public class <U> : Base<U>
                        where U : List<U>;

                    public void Method2() { }
                }
                partial class : Base<T>
                {
                    partial class;
                }
                """;

            UsingTree(source,
                // (1,22): error CS1001: Identifier expected
                // public partial class : Base<T>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(1, 22),
                // (7,18): error CS1001: Identifier expected
                //     public class <U> : Base<U>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<").WithLocation(7, 18),
                // (12,15): error CS1001: Identifier expected
                // partial class : Base<T>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(12, 15),
                // (14,18): error CS1001: Identifier expected
                //     partial class;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(14, 18)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.ClassKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "Base");
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
                        }
                    }
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.TypeConstraint);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
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
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Property");
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
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method1");
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
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.ClassKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeParameterList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.TypeParameter);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.BaseList);
                        {
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleBaseType);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Base");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.TypeParameterConstraintClause);
                        {
                            N(SyntaxKind.WhereKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.TypeConstraint);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method2");
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
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.ClassKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "Base");
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
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.PartialKeyword);
                        N(SyntaxKind.ClassKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TypeMissingIdentifier_Class05()
        {
            const string source =
                """
                public partial class (string name)
                    : Base<T>(name)
                    where T : List<T>
                {
                    public int Property { get; set; }
                    public void Method1() { }

                    public class <U> : Base<U>
                        where U : List<U>;

                    public void Method2() { }
                }
                public partial class <T>(int length)
                    : Base<T>(length)
                    where T : List<T>
                {
                    public int Property { get; set; }
                    public void Method1() { }
                
                    public class <U> : Base<U>
                        where U : List<U>;
                
                    public void Method2() { }
                }
                """;

            UsingTree(source,
                // (1,22): error CS1001: Identifier expected
                // public partial class (string name)
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 22),
                // (8,18): error CS1001: Identifier expected
                //     public class <U> : Base<U>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<").WithLocation(8, 18),
                // (13,22): error CS1001: Identifier expected
                // public partial class <T>(int length)
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<").WithLocation(13, 22),
                // (20,18): error CS1001: Identifier expected
                //     public class <U> : Base<U>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<").WithLocation(20, 18)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.ClassKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "name");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.PrimaryConstructorBaseType);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "Base");
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
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "name");
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                    }
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.TypeConstraint);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
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
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Property");
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
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method1");
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
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.ClassKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeParameterList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.TypeParameter);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.BaseList);
                        {
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleBaseType);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Base");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.TypeParameterConstraintClause);
                        {
                            N(SyntaxKind.WhereKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.TypeConstraint);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method2");
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
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.ClassKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "length");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.PrimaryConstructorBaseType);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "Base");
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
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "length");
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                    }
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.TypeConstraint);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
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
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Property");
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
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method1");
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
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.ClassKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeParameterList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.TypeParameter);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.BaseList);
                        {
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleBaseType);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Base");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.TypeParameterConstraintClause);
                        {
                            N(SyntaxKind.WhereKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.TypeConstraint);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method2");
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
        public void TypeMissingIdentifier_Interface01()
        {
            const string source =
                """
                public interface
                {
                    public abstract int X { get; }
                
                    public interface : { }
                    public interface { }

                    public void F();
                    abstract int Y();
                }
                """;

            UsingTree(source,
                // (1,17): error CS1001: Identifier expected
                // public interface
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 17),
                // (5,22): error CS1001: Identifier expected
                //     public interface : { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(5, 22),
                // (5,24): error CS1031: Type expected
                //     public interface : { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "{").WithLocation(5, 24),
                // (6,22): error CS1001: Identifier expected
                //     public interface { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(6, 22)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.InterfaceKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.AbstractKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "X");
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
                    N(SyntaxKind.InterfaceDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.InterfaceKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.BaseList);
                        {
                            N(SyntaxKind.ColonToken);
                            M(SyntaxKind.SimpleBaseType);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                        }
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.InterfaceDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.InterfaceKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "F");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AbstractKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Y");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
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
        public void TypeMissingIdentifier_Interface02()
        {
            const string source =
                """
                public interface : 
                {
                    public int Property { get; set; }
                    public void Method1() { }

                    public interface : IEnumerable<int> { }
                    public interface : IList<int>;

                    public void Method2() { }
                }
                """;

            UsingTree(source,
                // (1,18): error CS1001: Identifier expected
                // public interface : 
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(1, 18),
                // (1,19): error CS1031: Type expected
                // public interface : 
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(1, 19),
                // (6,22): error CS1001: Identifier expected
                //     public interface : IEnumerable<int> { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(6, 22),
                // (7,22): error CS1001: Identifier expected
                //     public interface : IList<int>;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(7, 22)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.InterfaceKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        M(SyntaxKind.SimpleBaseType);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Property");
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
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method1");
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
                    N(SyntaxKind.InterfaceDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.InterfaceKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.BaseList);
                        {
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleBaseType);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "IEnumerable");
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
                            }
                        }
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.InterfaceDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.InterfaceKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.BaseList);
                        {
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleBaseType);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "IList");
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
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method2");
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
        public void TypeMissingIdentifier_Interface03()
        {
            const string source =
                """
                public interface : IBase<T>
                    where T : List<T>
                {
                    public int Property { get; set; }
                    public void Method1() { }

                    public interface <U> : IBase<U>
                        where T : List<U>;

                    public void Method2() { }
                }
                """;

            UsingTree(source,
                // (1,18): error CS1001: Identifier expected
                // public interface : IBase<T>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(1, 18),
                // (7,22): error CS1001: Identifier expected
                //     public interface <U> : IBase<U>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<").WithLocation(7, 22)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.InterfaceKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "IBase");
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
                        }
                    }
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.TypeConstraint);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
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
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Property");
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
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method1");
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
                    N(SyntaxKind.InterfaceDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.InterfaceKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeParameterList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.TypeParameter);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.BaseList);
                        {
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleBaseType);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "IBase");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.TypeParameterConstraintClause);
                        {
                            N(SyntaxKind.WhereKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.TypeConstraint);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method2");
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
        public void TypeMissingIdentifier_Enum01()
        {
            const string source =
                """
                public enum
                {
                    A,
                    B,
                    C = 1,
                    D = C + 2,
                    E = 0,
                }
                """;

            UsingTree(source,
                // (1,12): error CS1001: Identifier expected
                // public enum
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 12)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.EnumDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.EnumKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.EnumMemberDeclaration);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.EnumMemberDeclaration);
                    {
                        N(SyntaxKind.IdentifierToken, "B");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.EnumMemberDeclaration);
                    {
                        N(SyntaxKind.IdentifierToken, "C");
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
                    N(SyntaxKind.EnumMemberDeclaration);
                    {
                        N(SyntaxKind.IdentifierToken, "D");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.AddExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
                                }
                                N(SyntaxKind.PlusToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "2");
                                }
                            }
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.EnumMemberDeclaration);
                    {
                        N(SyntaxKind.IdentifierToken, "E");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "0");
                            }
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TypeMissingIdentifier_Enum02()
        {
            const string source =
                """
                public enum : uint
                {
                    A,
                    B,
                    C = 1,
                    D = C + 2,
                    E = 0,
                }
                """;

            UsingTree(source,
                // (1,13): error CS1001: Identifier expected
                // public enum : uint
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(1, 13)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.EnumDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.EnumKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.UIntKeyword);
                            }
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.EnumMemberDeclaration);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.EnumMemberDeclaration);
                    {
                        N(SyntaxKind.IdentifierToken, "B");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.EnumMemberDeclaration);
                    {
                        N(SyntaxKind.IdentifierToken, "C");
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
                    N(SyntaxKind.EnumMemberDeclaration);
                    {
                        N(SyntaxKind.IdentifierToken, "D");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.AddExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
                                }
                                N(SyntaxKind.PlusToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "2");
                                }
                            }
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.EnumMemberDeclaration);
                    {
                        N(SyntaxKind.IdentifierToken, "E");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "0");
                            }
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TypeMissingIdentifier_Struct01()
        {
            const string source =
                """
                public static struct
                {
                    public static int StaticProperty { get; set; }

                    public int Property { get; set; }

                    public void Method() { }

                    public struct { }

                    public struct Nested { }
                }
                """;

            UsingTree(source,
                // (1,21): error CS1001: Identifier expected
                // public static struct
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 21),
                // (9,19): error CS1001: Identifier expected
                //     public struct { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(9, 19)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.StructKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "StaticProperty");
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
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Property");
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
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method");
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
                    N(SyntaxKind.StructDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StructKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.StructDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StructKeyword);
                        N(SyntaxKind.IdentifierToken, "Nested");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TypeMissingIdentifier_Struct02()
        {
            const string source =
                """
                public struct : 
                {
                    public int Property { get; set; }
                    public void Method1() { }

                    public struct : IStruct { }
                    public struct : IStruct<int>;

                    public void Method2() { }
                }
                """;

            UsingTree(source,
                // (1,15): error CS1001: Identifier expected
                // public struct : 
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(1, 15),
                // (1,16): error CS1031: Type expected
                // public struct : 
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(1, 16),
                // (6,19): error CS1001: Identifier expected
                //     public struct : IStruct { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(6, 19),
                // (7,19): error CS1001: Identifier expected
                //     public struct : IStruct<int>;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(7, 19)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StructKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        M(SyntaxKind.SimpleBaseType);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Property");
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
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method1");
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
                    N(SyntaxKind.StructDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StructKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.BaseList);
                        {
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleBaseType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "IStruct");
                                }
                            }
                        }
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.StructDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StructKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.BaseList);
                        {
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleBaseType);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "IStruct");
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
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method2");
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
        public void TypeMissingIdentifier_Struct03()
        {
            const string source =
                """
                public struct : IStruct<T>
                    where T : List<T>
                {
                    public int Property { get; set; }
                    public void Method1() { }

                    public struct <U> : IStruct<U>
                        where T : List<U>;

                    public void Method2() { }
                }
                """;

            UsingTree(source,
                // (1,15): error CS1001: Identifier expected
                // public struct : IStruct<T>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(1, 15),
                // (7,19): error CS1001: Identifier expected
                //     public struct <U> : IStruct<U>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<").WithLocation(7, 19)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StructKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "IStruct");
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
                        }
                    }
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.TypeConstraint);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
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
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Property");
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
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method1");
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
                    N(SyntaxKind.StructDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StructKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeParameterList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.TypeParameter);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.BaseList);
                        {
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleBaseType);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "IStruct");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.TypeParameterConstraintClause);
                        {
                            N(SyntaxKind.WhereKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.TypeConstraint);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method2");
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
        public void TypeMissingIdentifier_Struct04()
        {
            const string source =
                """
                public struct (string length)
                    : IStruct<T>
                    where T : List<T>
                {
                    public int Property { get; set; }
                    public void Method1() { }
                
                    public struct <U> : IStruct<U>
                        where T : List<U>;
                
                    public void Method2() { }
                }
                """;

            UsingTree(source,
                // (1,15): error CS1001: Identifier expected
                // public struct (string length)
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 15),
                // (8,19): error CS1001: Identifier expected
                //     public struct <U> : IStruct<U>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<").WithLocation(8, 19)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StructKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "length");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "IStruct");
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
                        }
                    }
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.TypeConstraint);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
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
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Property");
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
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method1");
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
                    N(SyntaxKind.StructDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StructKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeParameterList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.TypeParameter);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.BaseList);
                        {
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleBaseType);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "IStruct");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.TypeParameterConstraintClause);
                        {
                            N(SyntaxKind.WhereKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.TypeConstraint);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method2");
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
        public void TypeMissingIdentifier_Struct05()
        {
            const string source =
                """
                public struct <T>(string length)
                    : IStruct<T>
                    where T : List<T>
                {
                    public int Property { get; set; }
                    public void Method1() { }
                
                    public struct <U> : IStruct<U>
                        where T : List<U>;
                
                    public void Method2() { }
                }
                """;

            UsingTree(source,
                // (1,15): error CS1001: Identifier expected
                // public struct <T>(string length)
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<").WithLocation(1, 15),
                // (8,19): error CS1001: Identifier expected
                //     public struct <U> : IStruct<U>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<").WithLocation(8, 19)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StructKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "length");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "IStruct");
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
                        }
                    }
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.TypeConstraint);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
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
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Property");
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
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method1");
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
                    N(SyntaxKind.StructDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StructKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeParameterList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.TypeParameter);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.BaseList);
                        {
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleBaseType);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "IStruct");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.TypeParameterConstraintClause);
                        {
                            N(SyntaxKind.WhereKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.TypeConstraint);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method2");
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
        public void TypeMissingIdentifier_RecordClass01()
        {
            const string source =
                """
                public partial record class ()
                    where T : List<T>
                {
                    public int Property { get; set; }
                    public void Method1() { }

                    public partial record class ;
                    public class <U> : Base<U>
                        where U : List<U>;

                    public void Method2() { }
                }
                public record class : Base<T>
                {
                    partial class;
                }
                """;

            UsingTree(source,
                // (1,29): error CS1001: Identifier expected
                // public partial record class ()
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 29),
                // (7,33): error CS1001: Identifier expected
                //     public partial record class ;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(7, 33),
                // (8,18): error CS1001: Identifier expected
                //     public class <U> : Base<U>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<").WithLocation(8, 18),
                // (13,21): error CS1001: Identifier expected
                // public record class : Base<T>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(13, 21),
                // (15,18): error CS1001: Identifier expected
                //     partial class;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(15, 18)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.ClassKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.TypeConstraint);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
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
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Property");
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
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method1");
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
                    N(SyntaxKind.RecordDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PartialKeyword);
                        N(SyntaxKind.RecordKeyword);
                        N(SyntaxKind.ClassKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.ClassKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeParameterList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.TypeParameter);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.BaseList);
                        {
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleBaseType);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Base");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.TypeParameterConstraintClause);
                        {
                            N(SyntaxKind.WhereKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.TypeConstraint);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method2");
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
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.ClassKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "Base");
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
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.PartialKeyword);
                        N(SyntaxKind.ClassKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TypeMissingIdentifier_RecordClass02()
        {
            const string source =
                """
                public record class (ImmutableArray<T> Array)
                    : BaseRecord<T>(Array)
                    where T : List<T>
                {
                    public int Property { get; set; }
                    public void Method1() { }

                    public partial record class ;
                    public class <U> : Base<U>
                        where U : List<U>;

                    public void Method2() { }
                }
                file record class <T> : Base<T>
                {
                    file record class <U>(IEnumerable<U> Enumerable);
                    file class <U> { }
                    file class : Base;
                    file class;
                }
                """;

            UsingTree(source,
                // (1,21): error CS1001: Identifier expected
                // public record class (ImmutableArray<T> Array)
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 21),
                // (8,33): error CS1001: Identifier expected
                //     public partial record class ;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(8, 33),
                // (9,18): error CS1001: Identifier expected
                //     public class <U> : Base<U>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<").WithLocation(9, 18),
                // (14,19): error CS1001: Identifier expected
                // file record class <T> : Base<T>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<").WithLocation(14, 19),
                // (16,23): error CS1001: Identifier expected
                //     file record class <U>(IEnumerable<U> Enumerable);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<").WithLocation(16, 23),
                // (17,16): error CS1001: Identifier expected
                //     file class <U> { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<").WithLocation(17, 16),
                // (18,16): error CS1001: Identifier expected
                //     file class : Base;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(18, 16),
                // (19,15): error CS1001: Identifier expected
                //     file class;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(19, 15)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.ClassKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "ImmutableArray");
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
                            N(SyntaxKind.IdentifierToken, "Array");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.PrimaryConstructorBaseType);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "BaseRecord");
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
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Array");
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                    }
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.TypeConstraint);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
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
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Property");
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
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method1");
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
                    N(SyntaxKind.RecordDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PartialKeyword);
                        N(SyntaxKind.RecordKeyword);
                        N(SyntaxKind.ClassKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.ClassKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeParameterList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.TypeParameter);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.BaseList);
                        {
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleBaseType);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Base");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.TypeParameterConstraintClause);
                        {
                            N(SyntaxKind.WhereKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.TypeConstraint);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method2");
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
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.FileKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.ClassKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "Base");
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
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.RecordDeclaration);
                    {
                        N(SyntaxKind.FileKeyword);
                        N(SyntaxKind.RecordKeyword);
                        N(SyntaxKind.ClassKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeParameterList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.TypeParameter);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "IEnumerable");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                                N(SyntaxKind.IdentifierToken, "Enumerable");
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.FileKeyword);
                        N(SyntaxKind.ClassKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeParameterList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.TypeParameter);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.FileKeyword);
                        N(SyntaxKind.ClassKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.BaseList);
                        {
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleBaseType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Base");
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.FileKeyword);
                        N(SyntaxKind.ClassKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TypeMissingIdentifier_RecordStruct01()
        {
            const string source =
                """
                public partial record struct ()
                    where T : List<T>
                {
                    public int Property { get; set; }
                    public void Method1() { }

                    public partial record struct ;
                    public struct <U> : IStruct<U>
                        where U : List<U>;

                    public void Method2() { }
                }
                public record struct : IStruct<T>
                {
                    partial struct;
                }
                """;

            UsingTree(source,
                // (1,30): error CS1001: Identifier expected
                // public partial record struct ()
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 30),
                // (7,34): error CS1001: Identifier expected
                //     public partial record struct ;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(7, 34),
                // (8,19): error CS1001: Identifier expected
                //     public struct <U> : IStruct<U>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<").WithLocation(8, 19),
                // (13,22): error CS1001: Identifier expected
                // public record struct : IStruct<T>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(13, 22),
                // (15,19): error CS1001: Identifier expected
                //     partial struct;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(15, 19)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordStructDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.StructKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.TypeConstraint);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
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
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Property");
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
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method1");
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
                    N(SyntaxKind.RecordStructDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PartialKeyword);
                        N(SyntaxKind.RecordKeyword);
                        N(SyntaxKind.StructKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.StructDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StructKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeParameterList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.TypeParameter);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.BaseList);
                        {
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleBaseType);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "IStruct");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.TypeParameterConstraintClause);
                        {
                            N(SyntaxKind.WhereKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.TypeConstraint);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method2");
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
                N(SyntaxKind.RecordStructDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.StructKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "IStruct");
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
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.StructDeclaration);
                    {
                        N(SyntaxKind.PartialKeyword);
                        N(SyntaxKind.StructKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TypeMissingIdentifier_RecordStruct02()
        {
            const string source =
                """
                public record struct (ImmutableArray<T> Array)
                    : IStruct<T>
                    where T : List<T>
                {
                    public int Property { get; set; }
                    public void Method1() { }

                    public partial record struct ;
                    public struct <U> : IStruct<U>
                        where U : List<U>;

                    public void Method2() { }
                }
                file record struct <T> : IStruct<T>
                {
                    file record struct <U>(IEnumerable<U> Enumerable);
                    file struct <U> { }
                    file struct : IStruct;
                    file struct;
                }
                """;

            UsingTree(source,
                // (1,22): error CS1001: Identifier expected
                // public record struct (ImmutableArray<T> Array)
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 22),
                // (8,34): error CS1001: Identifier expected
                //     public partial record struct ;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(8, 34),
                // (9,19): error CS1001: Identifier expected
                //     public struct <U> : IStruct<U>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<").WithLocation(9, 19),
                // (14,20): error CS1001: Identifier expected
                // file record struct <T> : IStruct<T>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<").WithLocation(14, 20),
                // (16,24): error CS1001: Identifier expected
                //     file record struct <U>(IEnumerable<U> Enumerable);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<").WithLocation(16, 24),
                // (17,17): error CS1001: Identifier expected
                //     file struct <U> { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<").WithLocation(17, 17),
                // (18,17): error CS1001: Identifier expected
                //     file struct : IStruct;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(18, 17),
                // (19,16): error CS1001: Identifier expected
                //     file struct;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(19, 16)
                );
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordStructDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.StructKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "ImmutableArray");
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
                            N(SyntaxKind.IdentifierToken, "Array");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "IStruct");
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
                        }
                    }
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.TypeConstraint);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
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
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Property");
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
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method1");
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
                    N(SyntaxKind.RecordStructDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PartialKeyword);
                        N(SyntaxKind.RecordKeyword);
                        N(SyntaxKind.StructKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.StructDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StructKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeParameterList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.TypeParameter);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.BaseList);
                        {
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleBaseType);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "IStruct");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.TypeParameterConstraintClause);
                        {
                            N(SyntaxKind.WhereKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.TypeConstraint);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Method2");
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
                N(SyntaxKind.RecordStructDeclaration);
                {
                    N(SyntaxKind.FileKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.StructKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "IStruct");
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
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.RecordStructDeclaration);
                    {
                        N(SyntaxKind.FileKeyword);
                        N(SyntaxKind.RecordKeyword);
                        N(SyntaxKind.StructKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeParameterList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.TypeParameter);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "IEnumerable");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "U");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                                N(SyntaxKind.IdentifierToken, "Enumerable");
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.StructDeclaration);
                    {
                        N(SyntaxKind.FileKeyword);
                        N(SyntaxKind.StructKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.TypeParameterList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.TypeParameter);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.StructDeclaration);
                    {
                        N(SyntaxKind.FileKeyword);
                        N(SyntaxKind.StructKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.BaseList);
                        {
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.SimpleBaseType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "IStruct");
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.StructDeclaration);
                    {
                        N(SyntaxKind.FileKeyword);
                        N(SyntaxKind.StructKeyword);
                        M(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Fixed()
        {
            UsingDeclaration("""
                void M()
                {
                    List<Type>
                    fixed
                }
                """,
                options: null,
                // (3,15): error CS1001: Identifier expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 15),
                // (3,15): error CS1002: ; expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 15),
                // (4,10): error CS1003: Syntax error, '(' expected
                //     fixed
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(4, 10),
                // (4,10): error CS1031: Type expected
                //     fixed
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(4, 10),
                // (4,10): error CS1001: Identifier expected
                //     fixed
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(4, 10),
                // (4,10): error CS1003: Syntax error, ',' expected
                //     fixed
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(4, 10),
                // (5,2): error CS1026: ) expected
                // }
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(5, 2),
                // (5,2): error CS1733: Expected expression
                // }
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(5, 2),
                // (5,2): error CS1002: ; expected
                // }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(5, 2),
                // (5,2): error CS1513: } expected
                // }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 2));
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
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FixedStatement);
                    {
                        N(SyntaxKind.FixedKeyword);
                        M(SyntaxKind.OpenParenToken);
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
                        M(SyntaxKind.CloseParenToken);
                        M(SyntaxKind.ExpressionStatement);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            M(SyntaxKind.SemicolonToken);
                        }
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Fixed_DoubleGeneric()
        {
            UsingDeclaration("""
                void M()
                {
                    List<List<Type>>
                    fixed
                }
                """,
                options: null,
                // (3,21): error CS1525: Invalid expression term 'fixed'
                //     List<List<Type>>
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("fixed").WithLocation(3, 21),
                // (3,21): error CS1002: ; expected
                //     List<List<Type>>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 21),
                // (4,10): error CS1003: Syntax error, '(' expected
                //     fixed
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(4, 10),
                // (4,10): error CS1031: Type expected
                //     fixed
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(4, 10),
                // (4,10): error CS1001: Identifier expected
                //     fixed
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(4, 10),
                // (4,10): error CS1003: Syntax error, ',' expected
                //     fixed
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(4, 10),
                // (5,2): error CS1026: ) expected
                // }
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(5, 2),
                // (5,2): error CS1733: Expected expression
                // }
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(5, 2),
                // (5,2): error CS1002: ; expected
                // }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(5, 2),
                // (5,2): error CS1513: } expected
                // }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 2));
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
                        N(SyntaxKind.LessThanExpression);
                        {
                            N(SyntaxKind.LessThanExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                }
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                }
                            }
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.RightShiftExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Type");
                                }
                                N(SyntaxKind.GreaterThanGreaterThanToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FixedStatement);
                    {
                        N(SyntaxKind.FixedKeyword);
                        M(SyntaxKind.OpenParenToken);
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
                        M(SyntaxKind.CloseParenToken);
                        M(SyntaxKind.ExpressionStatement);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            M(SyntaxKind.SemicolonToken);
                        }
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Break()
        {
            UsingDeclaration("""
                void M()
                {
                    List<Type>
                    break
                }
                """,
                options: null,
                // (3,15): error CS1001: Identifier expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 15),
                // (3,15): error CS1002: ; expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 15),
                // (4,10): error CS1002: ; expected
                //     break
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 10));
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
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.BreakStatement);
                    {
                        N(SyntaxKind.BreakKeyword);
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Continue()
        {
            UsingDeclaration("""
                void M()
                {
                    List<Type>
                    continue
                }
                """,
                options: null,
                // (3,15): error CS1001: Identifier expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 15),
                // (3,15): error CS1002: ; expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 15),
                // (4,13): error CS1002: ; expected
                //     continue
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 13));
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
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.ContinueStatement);
                    {
                        N(SyntaxKind.ContinueKeyword);
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Try()
        {
            UsingDeclaration("""
                void M()
                {
                    List<Type>
                    try
                }
                """,
                options: null,
                // (3,15): error CS1001: Identifier expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 15),
                // (3,15): error CS1002: ; expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 15),
                // (4,8): error CS1514: { expected
                //     try
                Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(4, 8),
                // (5,2): error CS1513: } expected
                // }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 2));
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
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.TryStatement);
                    {
                        N(SyntaxKind.TryKeyword);
                        N(SyntaxKind.Block);
                        {
                            M(SyntaxKind.OpenBraceToken);
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
                    M(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Do()
        {
            UsingDeclaration("""
                void M()
                {
                    List<Type>
                    do
                }
                """,
                options: null,
                // (3,15): error CS1001: Identifier expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 15),
                // (3,15): error CS1002: ; expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 15),
                // (4,7): error CS1525: Invalid expression term '}'
                //     do
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(4, 7),
                // (4,7): error CS1002: ; expected
                //     do
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 7),
                // (4,7): error CS1003: Syntax error, 'while' expected
                //     do
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("while").WithLocation(4, 7),
                // (4,7): error CS1003: Syntax error, '(' expected
                //     do
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(4, 7),
                // (4,7): error CS1525: Invalid expression term '}'
                //     do
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(4, 7),
                // (4,7): error CS1026: ) expected
                //     do
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(4, 7),
                // (4,7): error CS1002: ; expected
                //     do
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 7));
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
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.DoStatement);
                    {
                        N(SyntaxKind.DoKeyword);
                        M(SyntaxKind.ExpressionStatement);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            M(SyntaxKind.SemicolonToken);
                        }
                        M(SyntaxKind.WhileKeyword);
                        M(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CloseParenToken);
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_For()
        {
            UsingDeclaration("""
                void M()
                {
                    List<Type>
                    for
                }
                """,
                options: null,
                // (3,15): error CS1001: Identifier expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 15),
                // (3,15): error CS1002: ; expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 15),
                // (4,8): error CS1003: Syntax error, '(' expected
                //     for
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(4, 8),
                // (4,8): error CS1001: Identifier expected
                //     for
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(4, 8),
                // (5,2): error CS1002: ; expected
                // }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(5, 2),
                // (5,2): error CS1733: Expected expression
                // }
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(5, 2),
                // (5,2): error CS1002: ; expected
                // }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(5, 2),
                // (5,2): error CS1026: ) expected
                // }
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(5, 2),
                // (5,2): error CS1733: Expected expression
                // }
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(5, 2),
                // (5,2): error CS1002: ; expected
                // }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(5, 2),
                // (5,2): error CS1513: } expected
                // }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 2));
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
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.ForStatement);
                    {
                        N(SyntaxKind.ForKeyword);
                        M(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.SemicolonToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.SemicolonToken);
                        M(SyntaxKind.CloseParenToken);
                        M(SyntaxKind.ExpressionStatement);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            M(SyntaxKind.SemicolonToken);
                        }
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Foreach()
        {
            UsingDeclaration("""
                void M()
                {
                    List<Type>
                    foreach
                }
                """,
                options: null,
                // (3,15): error CS1001: Identifier expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 15),
                // (3,15): error CS1002: ; expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 15),
                // (4,12): error CS1003: Syntax error, '(' expected
                //     foreach
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(4, 12),
                // (4,12): error CS1525: Invalid expression term '}'
                //     foreach
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(4, 12),
                // (4,12): error CS1515: 'in' expected
                //     foreach
                Diagnostic(ErrorCode.ERR_InExpected, "").WithLocation(4, 12),
                // (4,12): error CS0230: Type and identifier are both required in a foreach statement
                //     foreach
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "").WithLocation(4, 12),
                // (4,12): error CS1525: Invalid expression term '}'
                //     foreach
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(4, 12),
                // (4,12): error CS1026: ) expected
                //     foreach
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(4, 12),
                // (4,12): error CS1525: Invalid expression term '}'
                //     foreach
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(4, 12),
                // (4,12): error CS1002: ; expected
                //     foreach
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 12));
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
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.ForEachVariableStatement);
                    {
                        N(SyntaxKind.ForEachKeyword);
                        M(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.InKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CloseParenToken);
                        M(SyntaxKind.ExpressionStatement);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            M(SyntaxKind.SemicolonToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Goto()
        {
            UsingDeclaration("""
                void M()
                {
                    List<Type>
                    goto
                }
                """,
                options: null,
                // (3,15): error CS1001: Identifier expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 15),
                // (3,15): error CS1002: ; expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 15),
                // (4,9): error CS1001: Identifier expected
                //     goto
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(4, 9),
                // (4,9): error CS1002: ; expected
                //     goto
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 9));
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
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.GotoStatement);
                    {
                        N(SyntaxKind.GotoKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_If()
        {
            UsingDeclaration("""
                void M()
                {
                    List<Type>
                    if
                }
                """,
                options: null,
                // (3,15): error CS1001: Identifier expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 15),
                // (3,15): error CS1002: ; expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 15),
                // (4,7): error CS1003: Syntax error, '(' expected
                //     if
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(4, 7),
                // (4,7): error CS1525: Invalid expression term '}'
                //     if
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(4, 7),
                // (4,7): error CS1026: ) expected
                //     if
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(4, 7),
                // (4,7): error CS1525: Invalid expression term '}'
                //     if
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(4, 7),
                // (4,7): error CS1002: ; expected
                //     if
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 7));

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
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.IfStatement);
                    {
                        N(SyntaxKind.IfKeyword);
                        M(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CloseParenToken);
                        M(SyntaxKind.ExpressionStatement);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            M(SyntaxKind.SemicolonToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Else()
        {
            UsingDeclaration("""
                void M()
                {
                    List<Type>
                    else
                }
                """,
                options: null,
                // (3,15): error CS1001: Identifier expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 15),
                // (3,15): error CS1002: ; expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 15),
                // (3,15): error CS8641: 'else' cannot start a statement.
                //     List<Type>
                Diagnostic(ErrorCode.ERR_ElseCannotStartStatement, "").WithLocation(3, 15),
                // (3,15): error CS1003: Syntax error, '(' expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(3, 15),
                // (3,15): error CS1525: Invalid expression term 'else'
                //     List<Type>
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("else").WithLocation(3, 15),
                // (3,15): error CS1026: ) expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(3, 15),
                // (3,15): error CS1525: Invalid expression term 'else'
                //     List<Type>
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("else").WithLocation(3, 15),
                // (3,15): error CS1002: ; expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 15),
                // (4,9): error CS1525: Invalid expression term '}'
                //     else
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(4, 9),
                // (4,9): error CS1002: ; expected
                //     else
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 9));
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
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.IfStatement);
                    {
                        M(SyntaxKind.IfKeyword);
                        M(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CloseParenToken);
                        M(SyntaxKind.ExpressionStatement);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            M(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.ElseClause);
                        {
                            N(SyntaxKind.ElseKeyword);
                            M(SyntaxKind.ExpressionStatement);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Lock()
        {
            UsingDeclaration("""
                void M()
                {
                    List<Type>
                    lock
                }
                """,
                options: null,
                // (3,15): error CS1001: Identifier expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 15),
                // (3,15): error CS1002: ; expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 15),
                // (4,9): error CS1003: Syntax error, '(' expected
                //     lock
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(4, 9),
                // (4,9): error CS1525: Invalid expression term '}'
                //     lock
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(4, 9),
                // (4,9): error CS1026: ) expected
                //     lock
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(4, 9),
                // (4,9): error CS1525: Invalid expression term '}'
                //     lock
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(4, 9),
                // (4,9): error CS1002: ; expected
                //     lock
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 9));
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
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.LockStatement);
                    {
                        N(SyntaxKind.LockKeyword);
                        M(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CloseParenToken);
                        M(SyntaxKind.ExpressionStatement);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            M(SyntaxKind.SemicolonToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Return()
        {
            UsingDeclaration("""
                void M()
                {
                    List<Type>
                    return
                }
                """,
                options: null,
                // (3,15): error CS1001: Identifier expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 15),
                // (3,15): error CS1002: ; expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 15),
                // (4,11): error CS1525: Invalid expression term '}'
                //     return
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(4, 11),
                // (4,11): error CS1002: ; expected
                //     return
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 11));
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
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.ReturnStatement);
                    {
                        N(SyntaxKind.ReturnKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Switch()
        {
            UsingDeclaration("""
                void M()
                {
                    List<Type>
                    switch
                }
                """,
                options: null,
                // (3,15): error CS1525: Invalid expression term 'switch'
                //     List<Type>
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("switch").WithLocation(3, 15),
                // (3,15): error CS1002: ; expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 15),
                // (4,11): error CS1525: Invalid expression term '}'
                //     switch
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(4, 11),
                // (4,11): error CS8515: Parentheses are required around the switch governing expression.
                //     switch
                Diagnostic(ErrorCode.ERR_SwitchGoverningExpressionRequiresParens, "").WithLocation(4, 11),
                // (4,11): error CS1514: { expected
                //     switch
                Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(4, 11),
                // (5,2): error CS1513: } expected
                // }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 2));
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
                        N(SyntaxKind.GreaterThanExpression);
                        {
                            N(SyntaxKind.LessThanExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                }
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Type");
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.SwitchStatement);
                    {
                        N(SyntaxKind.SwitchKeyword);
                        M(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CloseParenToken);
                        M(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Unsafe()
        {
            UsingDeclaration("""
                void M()
                {
                    List<Type>
                    unsafe
                }
                """,
                options: null,
                // (3,15): error CS1001: Identifier expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 15),
                // (3,15): error CS1002: ; expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 15),
                // (4,5): error CS0106: The modifier 'unsafe' is not valid for this item
                //     unsafe
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "unsafe").WithArguments("unsafe").WithLocation(4, 5),
                // (4,11): error CS1031: Type expected
                //     unsafe
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(4, 11),
                // (4,11): error CS1001: Identifier expected
                //     unsafe
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(4, 11),
                // (4,11): error CS1003: Syntax error, ',' expected
                //     unsafe
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(4, 11),
                // (5,2): error CS1002: ; expected
                // }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(5, 2),
                // (5,2): error CS1513: } expected
                // }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 2));
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
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.UnsafeKeyword);
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
                    M(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Using()
        {
            UsingDeclaration("""
                void M()
                {
                    List<Type>
                    using
                }
                """,
                options: null,
                // (3,15): error CS1001: Identifier expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 15),
                // (3,15): error CS1002: ; expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 15),
                // (4,10): error CS1031: Type expected
                //     using
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(4, 10),
                // (4,10): error CS1001: Identifier expected
                //     using
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(4, 10),
                // (4,10): error CS1003: Syntax error, ',' expected
                //     using
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(4, 10),
                // (5,2): error CS1002: ; expected
                // }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(5, 2),
                // (5,2): error CS1513: } expected
                // }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 2));
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
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
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
                    M(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_While()
        {
            UsingDeclaration("""
                void M()
                {
                    List<Type>
                    while
                }
                """,
                options: null,
                // (3,15): error CS1001: Identifier expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 15),
                // (3,15): error CS1002: ; expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 15),
                // (4,10): error CS1003: Syntax error, '(' expected
                //     while
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(4, 10),
                // (4,10): error CS1525: Invalid expression term '}'
                //     while
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(4, 10),
                // (4,10): error CS1026: ) expected
                //     while
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(4, 10),
                // (4,10): error CS1525: Invalid expression term '}'
                //     while
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(4, 10),
                // (4,10): error CS1002: ; expected
                //     while
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 10));
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
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.WhileStatement);
                    {
                        N(SyntaxKind.WhileKeyword);
                        M(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CloseParenToken);
                        M(SyntaxKind.ExpressionStatement);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            M(SyntaxKind.SemicolonToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Volatile()
        {
            UsingDeclaration("""
                void M()
                {
                    List<Type>
                    volatile
                }
                """,
                options: null,
                // (3,15): error CS1001: Identifier expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 15),
                // (3,15): error CS1002: ; expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 15),
                // (4,5): error CS0106: The modifier 'volatile' is not valid for this item
                //     volatile
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "volatile").WithArguments("volatile").WithLocation(4, 5),
                // (4,13): error CS1031: Type expected
                //     volatile
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(4, 13),
                // (4,13): error CS1001: Identifier expected
                //     volatile
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(4, 13),
                // (4,13): error CS1003: Syntax error, ',' expected
                //     volatile
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(4, 13),
                // (5,2): error CS1002: ; expected
                // }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(5, 2),
                // (5,2): error CS1513: } expected
                // }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 2));
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
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
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
                        M(SyntaxKind.SemicolonToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Extern()
        {
            UsingDeclaration("""
                void M()
                {
                    List<Type>
                    extern
                }
                """,
                options: null,
                // (3,15): error CS1001: Identifier expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 15),
                // (3,15): error CS1002: ; expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 15),
                // (4,5): error CS0106: The modifier 'extern' is not valid for this item
                //     extern
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "extern").WithArguments("extern").WithLocation(4, 5),
                // (4,11): error CS1031: Type expected
                //     extern
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(4, 11),
                // (4,11): error CS1001: Identifier expected
                //     extern
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(4, 11),
                // (4,11): error CS1003: Syntax error, ',' expected
                //     extern
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(4, 11),
                // (5,2): error CS1002: ; expected
                // }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(5, 2),
                // (5,2): error CS1513: } expected
                // }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 2));
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
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.ExternKeyword);
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
                    M(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Case()
        {
            UsingDeclaration("""
                void M()
                {
                    List<Type>
                    case
                }
                """,
                options: null,
                // (3,15): error CS1001: Identifier expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 15),
                // (3,15): error CS1002: ; expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 15),
                // (3,15): error CS1003: Syntax error, 'switch' expected
                //     List<Type>
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("switch").WithLocation(3, 15),
                // (4,9): error CS8504: Pattern missing
                //     case
                Diagnostic(ErrorCode.ERR_MissingPattern, "").WithLocation(4, 9),
                // (4,9): error CS1003: Syntax error, ':' expected
                //     case
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(4, 9),
                // (5,2): error CS1513: } expected
                // }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 2));
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
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.SwitchStatement);
                    {
                        M(SyntaxKind.SwitchKeyword);
                        M(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CloseParenToken);
                        M(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.SwitchSection);
                        {
                            N(SyntaxKind.CaseSwitchLabel);
                            {
                                N(SyntaxKind.CaseKeyword);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                                M(SyntaxKind.ColonToken);
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        [InlineData("checked", SyntaxKind.CheckedExpression, SyntaxKind.CheckedKeyword)]
        [InlineData("unchecked", SyntaxKind.UncheckedExpression, SyntaxKind.UncheckedKeyword)]
        public void DefiniteStatementAfterGenericType_Checked(string keyword, SyntaxKind expressionKind, SyntaxKind tokenKind)
        {
            UsingDeclaration($$"""
                void M()
                {
                    List<Type> {{keyword}}(1);
                }
                """);
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
                        N(SyntaxKind.GreaterThanExpression);
                        {
                            N(SyntaxKind.LessThanExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                }
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Type");
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
                            N(expressionKind);
                            {
                                N(tokenKind);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "1");
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Throw()
        {
            UsingDeclaration($$"""
                void M()
                {
                    List<Type> throw ex;
                }
                """,
                options: null,
                // (3,16): error CS1525: Invalid expression term 'throw'
                //     List<Type> throw ex;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "throw ex").WithArguments("throw").WithLocation(3, 16));
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
                        N(SyntaxKind.GreaterThanExpression);
                        {
                            N(SyntaxKind.LessThanExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                }
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Type");
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
                            N(SyntaxKind.ThrowExpression);
                            {
                                N(SyntaxKind.ThrowKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "ex");
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_OpenBrace()
        {
            UsingDeclaration($$"""
                void M()
                {
                    List<Type> {
                }
                """,
                options: null,
                // (3,16): error CS1002: ; expected
                //     List<Type> {
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(3, 16),
                // (4,2): error CS1513: } expected
                // }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 2));
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
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "List");
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Type");
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Semicolon()
        {
            UsingDeclaration($$"""
                void M()
                {
                    List<Type>;
                }
                """);

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
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "List");
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Type");
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Static()
        {
            UsingDeclaration($$"""
                void M()
                {
                    List<Type> static
                }
                """,
                null,
                // (3,16): error CS1525: Invalid expression term 'static'
                //     List<Type> static
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "static").WithArguments("static").WithLocation(3, 16),
                // (3,16): error CS1002: ; expected
                //     List<Type> static
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "static").WithLocation(3, 16),
                // (3,16): error CS0106: The modifier 'static' is not valid for this item
                //     List<Type> static
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "static").WithArguments("static").WithLocation(3, 16),
                // (3,22): error CS1031: Type expected
                //     List<Type> static
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(3, 22),
                // (3,22): error CS1001: Identifier expected
                //     List<Type> static
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 22),
                // (3,22): error CS1003: Syntax error, ',' expected
                //     List<Type> static
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(3, 22),
                // (4,2): error CS1002: ; expected
                // }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 2),
                // (4,2): error CS1513: } expected
                // }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 2));
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
                        N(SyntaxKind.GreaterThanExpression);
                        {
                            N(SyntaxKind.LessThanExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                }
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Type");
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.StaticKeyword);
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
                    M(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_ReadOnlyKeyword()
        {
            UsingDeclaration($$"""
                void M()
                {
                    List<Type> readonly
                }
                """,
                options: null,
                // (3,16): error CS1525: Invalid expression term 'readonly'
                //     List<Type> readonly
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "readonly").WithArguments("readonly").WithLocation(3, 16),
                // (3,16): error CS1002: ; expected
                //     List<Type> readonly
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "readonly").WithLocation(3, 16),
                // (3,16): error CS0106: The modifier 'readonly' is not valid for this item
                //     List<Type> readonly
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(3, 16),
                // (3,24): error CS1031: Type expected
                //     List<Type> readonly
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(3, 24),
                // (3,24): error CS1001: Identifier expected
                //     List<Type> readonly
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 24),
                // (3,24): error CS1003: Syntax error, ',' expected
                //     List<Type> readonly
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(3, 24),
                // (4,2): error CS1002: ; expected
                // }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 2),
                // (4,2): error CS1513: } expected
                // }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 2));
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
                        N(SyntaxKind.GreaterThanExpression);
                        {
                            N(SyntaxKind.LessThanExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                }
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Type");
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
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
                        M(SyntaxKind.SemicolonToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Ref()
        {
            UsingDeclaration($$"""
                void M()
                {
                    List<Type> ref
                }
                """,
                options: null,
                // (3,16): error CS1525: Invalid expression term 'ref'
                //     List<Type> ref
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, """
                    ref

                    """).WithArguments("ref").WithLocation(3, 16),
                // (3,19): error CS1525: Invalid expression term '}'
                //     List<Type> ref
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(3, 19),
                // (3,19): error CS1002: ; expected
                //     List<Type> ref
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 19));
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
                        N(SyntaxKind.GreaterThanExpression);
                        {
                            N(SyntaxKind.LessThanExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                }
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Type");
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
                            N(SyntaxKind.RefExpression);
                            {
                                N(SyntaxKind.RefKeyword);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Bracket()
        {
            UsingDeclaration($$"""
                void M()
                {
                    List<Type> [
                }
                """,
                options: null,
                // (3,17): error CS1001: Identifier expected
                //     List<Type> [
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 17),
                // (4,2): error CS1003: Syntax error, ']' expected
                // }
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("]").WithLocation(4, 2),
                // (4,2): error CS1002: ; expected
                // }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 2),
                // (4,2): error CS1513: } expected
                // }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 2));
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
                        N(SyntaxKind.ElementAccessExpression);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.BracketedArgumentList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                M(SyntaxKind.CloseBracketToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_Dot()
        {
            UsingDeclaration($$"""
                void M()
                {
                    List<Type> .
                }
                """,
                options: null,
                // (3,17): error CS1001: Identifier expected
                //     List<Type> .
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 17),
                // (3,17): error CS1002: ; expected
                //     List<Type> .
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 17));
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
                        N(SyntaxKind.SimpleMemberAccessExpression);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
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
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        [InlineData("class", SyntaxKind.ClassDeclaration, SyntaxKind.ClassKeyword)]
        [InlineData("struct", SyntaxKind.StructDeclaration, SyntaxKind.StructKeyword)]
        [InlineData("interface", SyntaxKind.InterfaceDeclaration, SyntaxKind.InterfaceKeyword)]
        [InlineData("enum", SyntaxKind.EnumDeclaration, SyntaxKind.EnumKeyword)]
        public void DefiniteStatementAfterGenericType_TypeDecl(string typeText, SyntaxKind declarationKind, SyntaxKind keywordKind)
        {
            int column = 1 + typeText.Length;
            UsingTree($$"""
                List<Type>

                {{typeText}}
                """,
                options: null,
                // (1,11): error CS1001: Identifier expected
                // List<Type>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 11),
                // (1,11): error CS1002: ; expected
                // List<Type>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 11),
                // (3,7): error CS1001: Identifier expected
                // struct
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, column),
                // (3,7): error CS1514: { expected
                // struct
                Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(3, column),
                // (3,7): error CS1513: } expected
                // struct
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(3, column));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(declarationKind);
                {
                    N(keywordKind);
                    M(SyntaxKind.IdentifierToken);
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_RecordDecl()
        {
            UsingTree($$"""
                List<Type>

                record
                """,
                options: null,
                // (3,7): error CS1002: ; expected
                // record
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 7));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "record");
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_RecordDecl2()
        {
            UsingTree($$"""
                List<Type>

                record TypeName
                """,
                options: null,
                // (3,8): error CS1003: Syntax error, ',' expected
                // record TypeName
                Diagnostic(ErrorCode.ERR_SyntaxError, "TypeName").WithArguments(",").WithLocation(3, 8),
                // (3,16): error CS1002: ; expected
                // record TypeName
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 16));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "record");
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        public void DefiniteStatementAfterGenericType_DelegateDecl()
        {
            UsingTree($$"""
                List<Type>

                delegate
                """,
                options: null,
                // (1,11): error CS1001: Identifier expected
                // List<Type>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 11),
                // (1,11): error CS1002: ; expected
                // List<Type>
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 11),
                // (3,9): error CS1031: Type expected
                // delegate
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(3, 9),
                // (3,9): error CS1001: Identifier expected
                // delegate
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 9),
                // (3,9): error CS1003: Syntax error, '(' expected
                // delegate
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(3, 9),
                // (3,9): error CS1026: ) expected
                // delegate
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(3, 9),
                // (3,9): error CS1002: ; expected
                // delegate
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 9));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "List");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.DelegateDeclaration);
                {
                    N(SyntaxKind.DelegateKeyword);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
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

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/79031")]
        [InlineData("public", SyntaxKind.PublicKeyword)]
        [InlineData("internal", SyntaxKind.InternalKeyword)]
        [InlineData("private", SyntaxKind.PrivateKeyword)]
        [InlineData("protected", SyntaxKind.ProtectedKeyword)]
        public void DefiniteStatementAfterGenericType_Accessibility(string accessibilityText, SyntaxKind accessibilityKind)
        {
            UsingTree($$"""
                List<Type>

                {{accessibilityText}}
                """,
                options: null,
                // (3,1): error CS1585: Member modifier 'internal' must precede the member type and name
                // internal
                Diagnostic(ErrorCode.ERR_BadModifierLocation, accessibilityText).WithArguments(accessibilityText).WithLocation(3, 1),
                // (3,1): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // internal
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, accessibilityText).WithLocation(3, 1));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "List");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Type");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                }
                N(SyntaxKind.IncompleteMember);
                {
                    N(accessibilityKind);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }
    }
}
