// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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
        [InlineData(SyntaxKind.EnumKeyword)]
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

        [Theory]
        [InlineData(SyntaxKind.ClassKeyword)]
        [InlineData(SyntaxKind.StructKeyword)]
        [InlineData(SyntaxKind.InterfaceKeyword)]
        public void FileModifier_03(SyntaxKind typeKeyword)
        {
            UsingNode($$"""
                partial file {{SyntaxFacts.GetText(typeKeyword)}} C { }
                """);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "partial");
                    }
                }
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
                """);
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
                """);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "partial");
                    }
                }
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.FileKeyword);
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
        public void FileMember_01()
        {
            // PROTOTYPE(ft): consider improving error recovery here
            UsingNode("""
                class C
                {
                    file void M() { }
                }
                """);
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
                """);
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
                """);
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
                """);
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
    }
}
