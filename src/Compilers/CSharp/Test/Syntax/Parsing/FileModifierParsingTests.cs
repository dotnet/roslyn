﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class FileModifierParsingTests : ParsingTests
    {
        public FileModifierParsingTests(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions? options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options ?? TestOptions.Regular);
        }

        private void UsingNode(string text, params DiagnosticDescription[] expectedErrors) => UsingNode(text, options: TestOptions.RegularPreview, expectedErrors);

        [Theory]
        [InlineData(SyntaxKind.ClassKeyword)]
        [InlineData(SyntaxKind.StructKeyword)]
        [InlineData(SyntaxKind.InterfaceKeyword)]
        [InlineData(SyntaxKind.RecordKeyword)]
        [InlineData(SyntaxKind.EnumKeyword)]
        public void FileModifier_01(SyntaxKind typeKeyword)
        {
            UsingNode($$"""
                file {{SyntaxFacts.GetText(typeKeyword)}} C { }
                """);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxFacts.GetBaseTypeDeclarationKind(typeKeyword));
                {
                    N(SyntaxKind.FileKeyword);
                    N(typeKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(SyntaxKind.ClassKeyword)]
        [InlineData(SyntaxKind.StructKeyword)]
        [InlineData(SyntaxKind.InterfaceKeyword)]
        [InlineData(SyntaxKind.RecordKeyword)]
        public void FileModifier_02(SyntaxKind typeKeyword)
        {
            UsingNode($$"""
                file partial {{SyntaxFacts.GetText(typeKeyword)}} C { }
                """);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxFacts.GetBaseTypeDeclarationKind(typeKeyword));
                {
                    N(SyntaxKind.FileKeyword);
                    N(SyntaxKind.PartialKeyword);
                    N(typeKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void FileModifier_02_Enum()
        {
            UsingNode($$"""
                file partial enum C { }
                """,
                // (1,6): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                // file partial enum C { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(1, 6));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.EnumDeclaration);
                {
                    N(SyntaxKind.FileKeyword);
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.EnumKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(SyntaxKind.ClassKeyword)]
        [InlineData(SyntaxKind.StructKeyword)]
        [InlineData(SyntaxKind.InterfaceKeyword)]
        public void FileModifier_03(SyntaxKind typeKeyword)
        {
            UsingNode($$"""
                partial file {{SyntaxFacts.GetText(typeKeyword)}} C { }
                """,
                // (1,14): error CS1002: ; expected
                // partial file {{SyntaxFacts.GetText(typeKeyword)}} C { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, SyntaxFacts.GetText(typeKeyword)).WithLocation(1, 14));
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
                                N(SyntaxKind.IdentifierToken, "partial");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "file");
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxFacts.GetBaseTypeDeclarationKind(typeKeyword));
                {
                    N(typeKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        // PROTOTYPE(ft): is it fine that records parse as a single declaration here, but not other type kinds?
        [Theory]
        [InlineData(SyntaxKind.RecordKeyword)]
        public void FileModifier_04(SyntaxKind typeKeyword)
        {
            UsingNode($$"""
                partial file {{SyntaxFacts.GetText(typeKeyword)}} C { }
                """);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxFacts.GetBaseTypeDeclarationKind(typeKeyword));
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.FileKeyword);
                    N(typeKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void FileModifier_05()
        {
            UsingNode($$"""
                file partial record struct C { }
                """);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordStructDeclaration);
                {
                    N(SyntaxKind.FileKeyword);
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        // PROTOTYPE(ft): is it fine that records parse here, but not other type kinds?
        [Fact]
        public void FileModifier_06()
        {
            UsingNode($$"""
                partial file record struct C { }
                """);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordStructDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.FileKeyword);
                    N(SyntaxKind.RecordKeyword);
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
        public void FileModifier_07()
        {
            UsingNode($$"""
                file partial ref struct C { }
                """,
                // (1,14): error CS1585: Member modifier 'ref' must precede the member type and name
                // file partial ref struct C { }
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(1, 14));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.FileKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "partial");
                    }
                }
                N(SyntaxKind.StructDeclaration);
                {
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
        public void FileModifier_08()
        {
            UsingNode($$"""
                partial file ref struct C { }
                """,
                // (1,14): error CS1003: Syntax error, ',' expected
                // partial file ref struct C { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",", "ref").WithLocation(1, 14),
                // (1,18): error CS1002: ; expected
                // partial file ref struct C { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(1, 18));

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
                                N(SyntaxKind.IdentifierToken, "partial");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "file");
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.StructDeclaration);
                {
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
        public void FileModifier_09()
        {
            UsingNode($$"""
                file abstract class C { }
                """);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.FileKeyword);
                    N(SyntaxKind.AbstractKeyword);
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
        public void FileModifier_10()
        {
            UsingNode($$"""
                abstract file class C { }
                """);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.AbstractKeyword);
                    N(SyntaxKind.FileKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(SyntaxKind.ClassKeyword)]
        [InlineData(SyntaxKind.StructKeyword)]
        [InlineData(SyntaxKind.InterfaceKeyword)]
        [InlineData(SyntaxKind.RecordKeyword)]
        [InlineData(SyntaxKind.EnumKeyword)]
        public void FileModifier_11(SyntaxKind typeKeyword)
        {
            UsingNode($$"""
                public file {{SyntaxFacts.GetText(typeKeyword)}} C { }
                """);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxFacts.GetBaseTypeDeclarationKind(typeKeyword));
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.FileKeyword);
                    N(typeKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(SyntaxKind.ClassKeyword)]
        [InlineData(SyntaxKind.StructKeyword)]
        [InlineData(SyntaxKind.InterfaceKeyword)]
        [InlineData(SyntaxKind.RecordKeyword)]
        [InlineData(SyntaxKind.EnumKeyword)]
        public void FileModifier_12(SyntaxKind typeKeyword)
        {
            UsingNode($$"""
                file public {{SyntaxFacts.GetText(typeKeyword)}} C { }
                """);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxFacts.GetBaseTypeDeclarationKind(typeKeyword));
                {
                    N(SyntaxKind.FileKeyword);
                    N(SyntaxKind.PublicKeyword);
                    N(typeKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void FileModifier_13()
        {
            // note: LangVersion error is given during binding here
            var tree = UsingNode("""
                file class C { }
                """, options: TestOptions.Regular10);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.FileKeyword);
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
        public void FileModifier_14()
        {
            UsingNode("""
                file delegate void D();
                """);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.DelegateDeclaration);
                {
                    N(SyntaxKind.FileKeyword);
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "D");
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
        public void FileModifier_15()
        {
            UsingNode("""
                namespace NS
                {
                    file class C { }
                }
                """);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.NamespaceDeclaration);
                {
                    N(SyntaxKind.NamespaceKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "NS");
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.FileKeyword);
                        N(SyntaxKind.ClassKeyword);
                        N(SyntaxKind.IdentifierToken, "C");
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
        public void FileModifier_16()
        {
            UsingNode("""
                namespace NS;
                file class C { }
                """);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.FileScopedNamespaceDeclaration);
                {
                    N(SyntaxKind.NamespaceKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "NS");
                    }
                    N(SyntaxKind.SemicolonToken);
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.FileKeyword);
                        N(SyntaxKind.ClassKeyword);
                        N(SyntaxKind.IdentifierToken, "C");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void FileModifier_17()
        {
            UsingNode("""
                class Outer
                {
                    file class C { }
                }
                """);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Outer");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.FileKeyword);
                        N(SyntaxKind.ClassKeyword);
                        N(SyntaxKind.IdentifierToken, "C");
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
        public void FileModifier_18()
        {
            UsingNode("""
                class C
                {
                    file delegate*<int, void> M();
                }
                """,
                // (3,10): error CS1519: Invalid token 'delegate' in class, record, struct, or interface member declaration
                //     file delegate*<int, void> M();
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "delegate").WithArguments("delegate").WithLocation(3, 10));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "file");
                        }
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
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
                                        N(SyntaxKind.IntKeyword);
                                    }
                                }
                                N(SyntaxKind.CommaToken);
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
                        N(SyntaxKind.IdentifierToken, "M");
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
        public void FileMember_01()
        {
            // PROTOTYPE(ft): consider improving error recovery here
            UsingNode("""
                class C
                {
                    file void M() { }
                }
                """,
                // (3,10): error CS1519: Invalid token 'void' in class, record, struct, or interface member declaration
                //     file void M() { }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "void").WithArguments("void").WithLocation(3, 10));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "file");
                        }
                    }
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
        public void FileMember_02()
        {
            // PROTOTYPE(ft): consider improving error recovery here
            UsingNode("""
                class C
                {
                    file int x;
                }
                """,
                // (3,10): error CS1519: Invalid token 'int' in class, record, struct, or interface member declaration
                //     file int x;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "int").WithArguments("int").WithLocation(3, 10));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "file");
                        }
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
                                N(SyntaxKind.IdentifierToken, "x");
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
        public void FileMember_03()
        {
            // PROTOTYPE(ft): consider improving error recovery here
            UsingNode($$"""
                class C
                {
                    file event Action x;
                }
                """,
                // (3,10): error CS1519: Invalid token 'event' in class, record, struct, or interface member declaration
                //     file event Action x;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "event").WithArguments("event").WithLocation(3, 10));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "file");
                        }
                    }
                    N(SyntaxKind.EventFieldDeclaration);
                    {
                        N(SyntaxKind.EventKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Action");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
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
        public void FileMember_04()
        {
            // PROTOTYPE(ft): consider improving error recovery here
            UsingNode($$"""
                class C
                {
                    file int x { get; set; }
                }
                """,
                // (3,10): error CS1519: Invalid token 'int' in class, record, struct, or interface member declaration
                //     file int x { get; set; }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "int").WithArguments("int").WithLocation(3, 10));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "file");
                        }
                    }
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "x");
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void MemberNamedFile_01()
        {
            UsingNode($$"""
                class C
                {
                    int file;
                }
                """);

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
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "file");
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
        public void MemberNamedFile_02()
        {
            UsingNode($$"""
                class C
                {
                    int file { get; set; }
                }
                """);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "file");
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void MemberNamedFile_03()
        {
            UsingNode($$"""
                class C
                {
                    event Action file;
                }
                """);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.EventFieldDeclaration);
                    {
                        N(SyntaxKind.EventKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Action");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "file");
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
        public void MemberNamedFile_04()
        {
            UsingNode($$"""
                class C
                {
                    void file() { }
                }
                """);

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
                        N(SyntaxKind.IdentifierToken, "file");
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
        public void MemberNamedFile_05()
        {
            UsingNode($$"""
                file class file { }
                """);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.FileKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "file");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void MemberNamedFile_06()
        {
            UsingNode($$"""
                class C
                {
                    file async;
                }
                """);

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
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "file");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "async");
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
        public void Errors_01()
        {
            UsingNode($$"""
                file
                """,
                // (1,1): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // file
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "file").WithLocation(1, 1));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "file");
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Errors_02()
        {
            UsingNode($$"""
                file;
                """);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "file");
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Errors_03()
        {
            UsingNode($$"""
                file namespace NS;
                """,
                // (1,1): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // file namespace NS;
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "file").WithLocation(1, 1));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "file");
                    }
                }
                N(SyntaxKind.FileScopedNamespaceDeclaration);
                {
                    N(SyntaxKind.NamespaceKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "NS");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Errors_04()
        {
            UsingNode($$"""
                file namespace NS { }
                """,
                // (1,1): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // file namespace NS { }
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "file").WithLocation(1, 1));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "file");
                    }
                }
                N(SyntaxKind.NamespaceDeclaration);
                {
                    N(SyntaxKind.NamespaceKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "NS");
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void File_Repeated()
        {
            const int fileModifiersCount = 100000;
            var manyFileModifiers = string.Join(" ", Enumerable.Repeat<string>("file", fileModifiersCount));
            UsingNode(manyFileModifiers,
                Diagnostic(ErrorCode.ERR_SyntaxError, "file").WithArguments(",", "").WithLocation(1, 11),
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 500000));

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
                                N(SyntaxKind.IdentifierToken, "file");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "file");
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            UsingNode(manyFileModifiers + " class { }",
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(1, 500007));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    for (int i = 0; i < fileModifiersCount; i++)
                    {
                        N(SyntaxKind.FileKeyword);
                    }
                    N(SyntaxKind.ClassKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void MethodNamedRecord_01_CSharp8()
        {
            UsingNode("""
                class C
                {
                    file record();
                }
                """, options: TestOptions.Regular8);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "file");
                        }
                        N(SyntaxKind.IdentifierToken, "record");
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
        public void MethodNamedRecord_01_CSharpNext()
        {
            UsingNode("""
                class C
                {
                    file record();
                }
                """);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.ConstructorDeclaration);
                    {
                        N(SyntaxKind.FileKeyword);
                        N(SyntaxKind.IdentifierToken, "record");
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
        public void MethodNamedRecord_02_CSharp8()
        {
            UsingNode("""
                class C
                {
                    file record() { }
                }
                """, options: TestOptions.Regular8);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "file");
                        }
                        N(SyntaxKind.IdentifierToken, "record");
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
        public void MethodNamedRecord_02_CSharpNext()
        {
            UsingNode("""
                class C
                {
                    file record() { }
                }
                """);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.ConstructorDeclaration);
                    {
                        N(SyntaxKind.FileKeyword);
                        N(SyntaxKind.IdentifierToken, "record");
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
        public void FileRecord_01_CSharp8()
        {
            UsingNode("""
                class C
                {
                    file record X();
                }
                """,
                options: TestOptions.Regular8,
                // (3,17): error CS1002: ; expected
                //     file record X();
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "X").WithLocation(3, 17));

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
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "file");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "record");
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.ConstructorDeclaration);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
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
        public void FileRecord_01_CSharpNext()
        {
            UsingNode("""
                class C
                {
                    file record X();
                }
                """);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.RecordDeclaration);
                    {
                        N(SyntaxKind.FileKeyword);
                        N(SyntaxKind.RecordKeyword);
                        N(SyntaxKind.IdentifierToken, "X");
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
        public void FileRecord_02_CSharp8()
        {
            UsingNode("""
                class C
                {
                    file record X() { }
                }
                """,
                options: TestOptions.Regular8,
                // (3,17): error CS1002: ; expected
                //     file record X() { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "X").WithLocation(3, 17));

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
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "file");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "record");
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.ConstructorDeclaration);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
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
        public void FileRecord_02_CSharpNext()
        {
            UsingNode("""
                class C
                {
                    file record X() { }
                }
                """);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.RecordDeclaration);
                    {
                        N(SyntaxKind.FileKeyword);
                        N(SyntaxKind.RecordKeyword);
                        N(SyntaxKind.IdentifierToken, "X");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
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
        public void FileRecord_03_CSharp8()
        {
            UsingNode("""
                class C
                {
                    file record X;
                }
                """,
                options: TestOptions.Regular8,
                // (3,17): error CS1002: ; expected
                //     file record X;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "X").WithLocation(3, 17),
                // (3,18): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     file record X;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(3, 18),
                // (3,18): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     file record X;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(3, 18));

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
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "file");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "record");
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "X");
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void FileRecord_03_CSharpNext()
        {
            UsingNode("""
                class C
                {
                    file record X;
                }
                """);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.RecordDeclaration);
                    {
                        N(SyntaxKind.FileKeyword);
                        N(SyntaxKind.RecordKeyword);
                        N(SyntaxKind.IdentifierToken, "X");
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }
    }
}
