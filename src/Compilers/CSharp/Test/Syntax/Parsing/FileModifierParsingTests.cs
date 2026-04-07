// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class FileModifierParsingTests : ParsingTests
{
    public FileModifierParsingTests(ITestOutputHelper output) : base(output) { }

    protected override SyntaxTree ParseTree(string text, CSharpParseOptions? options)
    {
        return SyntaxFactory.ParseSyntaxTree(text, options ?? TestOptions.Regular);
    }

    private void UsingNode(string text, params DiagnosticDescription[] expectedDiagnostics)
    {
        UsingNode(text, options: null, expectedParsingDiagnostics: expectedDiagnostics);
    }

    private void UsingNode(string text, CSharpParseOptions? options = null, DiagnosticDescription[]? expectedParsingDiagnostics = null, DiagnosticDescription[]? expectedBindingDiagnostics = null)
    {
        options ??= TestOptions.RegularPreview;
        expectedParsingDiagnostics ??= Array.Empty<DiagnosticDescription>();
        expectedBindingDiagnostics ??= expectedParsingDiagnostics;

        var tree = UsingTree(text, options, expectedParsingDiagnostics);
        Validate(text, (CSharpSyntaxNode)tree.GetRoot(), expectedParsingDiagnostics);

        var comp = CreateCompilation(tree);
        comp.VerifyDiagnostics(expectedBindingDiagnostics);
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
            expectedBindingDiagnostics: new[]
            {
                // (1,19): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                // file partial enum C { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "C").WithLocation(1, 19)
            });
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
            expectedParsingDiagnostics: new[]
            {
                // (1,14): error CS1002: ; expected
                // partial file {{SyntaxFacts.GetText(typeKeyword)}} C { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, SyntaxFacts.GetText(typeKeyword)).WithLocation(1, 14)
            },
            expectedBindingDiagnostics: new[]
            {
                // (1,1): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
                // partial file interface C { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(1, 1),
                // (1,9): warning CS0168: The variable 'file' is declared but never used
                // partial file interface C { }
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "file").WithArguments("file").WithLocation(1, 9),
                // (1,14): error CS1002: ; expected
                // partial file {{SyntaxFacts.GetText(typeKeyword)}} C { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, SyntaxFacts.GetText(typeKeyword)).WithLocation(1, 14)
            });
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

    [Fact]
    public void FileModifier_04()
    {
        UsingNode("""
            partial file record C { }
            """,
            expectedBindingDiagnostics: new DiagnosticDescription[]
            {
                // (1,1): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                // partial file record C { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(1, 1)
            });
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RecordDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.FileKeyword);
                N(SyntaxKind.RecordKeyword);
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

    [Fact]
    public void FileModifier_06()
    {
        UsingNode($$"""
            partial file record struct C { }
            """,
            expectedBindingDiagnostics: new DiagnosticDescription[]
            {
                // (1,1): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                // partial file record struct C { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(1, 1)
            });
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
    public void FileModifier_07_CSharp10()
    {
        UsingNode($$"""
            file partial ref struct C { }
            """,
            options: TestOptions.Regular10,
            expectedParsingDiagnostics: new[]
            {
                // (1,14): error CS1003: Syntax error, ',' expected
                // file partial ref struct C { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(1, 14),
                // (1,18): error CS1002: ; expected
                // file partial ref struct C { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(1, 18)
            },
            expectedBindingDiagnostics: new[]
            {
                // (1,1): error CS0246: The type or namespace name 'file' could not be found (are you missing a using directive or an assembly reference?)
                // file partial ref struct C { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "file").WithArguments("file").WithLocation(1, 1),
                // (1,6): warning CS0168: The variable 'partial' is declared but never used
                // file partial ref struct C { }
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "partial").WithArguments("partial").WithLocation(1, 6),
                // (1,14): error CS1003: Syntax error, ',' expected
                // file partial ref struct C { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(1, 14),
                // (1,18): error CS1002: ; expected
                // file partial ref struct C { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(1, 18)
            });
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
                            N(SyntaxKind.IdentifierToken, "partial");
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
    public void FileModifier_07()
    {
        UsingNode($$"""
            file partial ref struct C { }
            """,
            expectedParsingDiagnostics: new[]
            {
                // (1,14): error CS1003: Syntax error, ',' expected
                // file partial ref struct C { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(1, 14),
                // (1,18): error CS1002: ; expected
                // file partial ref struct C { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(1, 18)
            },
            expectedBindingDiagnostics: new[]
            {
                // (1,1): error CS0246: The type or namespace name 'file' could not be found (are you missing a using directive or an assembly reference?)
                // file partial ref struct C { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "file").WithArguments("file").WithLocation(1, 1),
                // (1,6): warning CS0168: The variable 'partial' is declared but never used
                // file partial ref struct C { }
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "partial").WithArguments("partial").WithLocation(1, 6),
                // (1,14): error CS1003: Syntax error, ',' expected
                // file partial ref struct C { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(1, 14),
                // (1,18): error CS1002: ; expected
                // file partial ref struct C { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(1, 18)
            });

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
                            N(SyntaxKind.IdentifierToken, "partial");
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
    public void FileModifier_08()
    {
        UsingNode($$"""
            partial file ref struct C { }
            """,
            expectedParsingDiagnostics: new[]
            {
                // (1,14): error CS1003: Syntax error, ',' expected
                // partial file ref struct C { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(1, 14),
                // (1,18): error CS1002: ; expected
                // partial file ref struct C { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(1, 18)
            },
            expectedBindingDiagnostics: new[]
            {
                // (1,1): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
                // partial file ref struct C { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(1, 1),
                // (1,9): warning CS0168: The variable 'file' is declared but never used
                // partial file ref struct C { }
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "file").WithArguments("file").WithLocation(1, 9),
                // (1,14): error CS1003: Syntax error, ',' expected
                // partial file ref struct C { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(1, 14),
                // (1,18): error CS1002: ; expected
                // partial file ref struct C { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(1, 18)
            });

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
            """,
            expectedBindingDiagnostics: new[]
            {
                // (1,20): error CS9052: File-local type 'C' cannot use accessibility modifiers.
                // public file {{SyntaxFacts.GetText(typeKeyword)}} C { }
                Diagnostic(ErrorCode.ERR_FileTypeNoExplicitAccessibility, "C").WithArguments("C")
            });
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
            """,
            expectedBindingDiagnostics: new[]
            {
                // (1,19): error CS9052: File-local type 'C' cannot use accessibility modifiers.
                // file public {{SyntaxFacts.GetText(typeKeyword)}} C { }
                Diagnostic(ErrorCode.ERR_FileTypeNoExplicitAccessibility, "C").WithArguments("C")
            });
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
        UsingNode("""
            file class C { }
            """,
            options: TestOptions.Regular10,
            expectedBindingDiagnostics: new[]
            {
                // (1,12): error CS8936: Feature 'file types' is not available in C# 10.0. Please use language version 11.0 or greater.
                // file class C { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "C").WithArguments("file types", "11.0").WithLocation(1, 12)
            });
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
            """,
            expectedBindingDiagnostics: new[]
            {
                // (3,16): error CS9054: File-local type 'Outer.C' must be defined in a top level type; 'Outer.C' is a nested type.
                //     file class C { }
                Diagnostic(ErrorCode.ERR_FileTypeNested, "C").WithArguments("Outer.C").WithLocation(3, 16)
            });

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
            expectedBindingDiagnostics: new[]
            {
                // (3,10): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     file delegate*<int, void> M();
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(3, 10),
                // (3,31): error CS0106: The modifier 'file' is not valid for this item
                //     file delegate*<int, void> M();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M").WithArguments("file").WithLocation(3, 31),
                // (3,31): error CS0501: 'C.M()' must declare a body because it is not marked abstract, extern, or partial
                //     file delegate*<int, void> M();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M").WithArguments("C.M()").WithLocation(3, 31)
            });

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.FileKeyword);
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
        UsingNode("""
            class C
            {
                file void M() { }
            }
            """,
            expectedBindingDiagnostics: new[]
            {
                // (3,15): error CS0106: The modifier 'file' is not valid for this item
                //     file void M() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M").WithArguments("file").WithLocation(3, 15)
            });
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.FileKeyword);
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
        UsingNode("""
            class C
            {
                file int x;
            }
            """,
            expectedBindingDiagnostics: new[]
            {
                // (3,14): error CS0106: The modifier 'file' is not valid for this item
                //     file int x;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "x").WithArguments("file").WithLocation(3, 14),
                // (3,14): warning CS0169: The field 'C.x' is never used
                //     file int x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("C.x").WithLocation(3, 14)
            });
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.FileKeyword);
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
        UsingNode($$"""
            class C
            {
                file event Action x;
            }
            """,
            expectedBindingDiagnostics: new[]
            {
                // (3,16): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                //     file event Action x;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Action").WithArguments("Action").WithLocation(3, 16),
                // (3,23): error CS0106: The modifier 'file' is not valid for this item
                //     file event Action x;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "x").WithArguments("file").WithLocation(3, 23),
                // (3,23): warning CS0067: The event 'C.x' is never used
                //     file event Action x;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "x").WithArguments("C.x").WithLocation(3, 23)
            });
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.EventFieldDeclaration);
                {
                    N(SyntaxKind.FileKeyword);
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
        var source = $$"""
            class C
            {
                file int x { get; set; }
            }
            """;

        UsingNode(source, expectedBindingDiagnostics: new[]
        {
            // (3,14): error CS0106: The modifier 'file' is not valid for this item
            //     file int x { get; set; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "x").WithArguments("file").WithLocation(3, 14)
        });
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.FileKeyword);
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
    public void FileMember_05()
    {
        var source = $$"""
            class C
            {
                async file void M() { }
            }
            """;

        UsingNode(source, expectedBindingDiagnostics: new[]
        {
            // (3,21): error CS0106: The modifier 'file' is not valid for this item
            //     async file void M() { }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "M").WithArguments("file").WithLocation(3, 21),
        });
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
                    N(SyntaxKind.FileKeyword);
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
    public void MemberNamedFile_01()
    {
        UsingNode($$"""
            class C
            {
                int file;
            }
            """, expectedBindingDiagnostics: new[]
            {
                // (3,9): warning CS0169: The field 'C.file' is never used
                //     int file;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "file").WithArguments("C.file").WithLocation(3, 9)
            });

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
            """,
            expectedBindingDiagnostics: new[]
            {
                // (3,11): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                //     event Action file;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Action").WithArguments("Action").WithLocation(3, 11),
                // (3,18): warning CS0067: The event 'C.file' is never used
                //     event Action file;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "file").WithArguments("C.file").WithLocation(3, 18)
            });

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
            """,
            expectedBindingDiagnostics: new[]
            {
                // (1,12): error CS9056: Types and aliases cannot be named 'file'.
                // file class file { }
                Diagnostic(ErrorCode.ERR_FileTypeNameDisallowed, "file").WithLocation(1, 12)
            });

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
    public void MemberNamedFile_06_CSharp10()
    {
        UsingNode($$"""
            class C
            {
                file async;
            }
            """,
            options: TestOptions.Regular10,
            expectedBindingDiagnostics: new[]
            {
                // (3,5): error CS0246: The type or namespace name 'file' could not be found (are you missing a using directive or an assembly reference?)
                //     file async;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "file").WithArguments("file").WithLocation(3, 5),
                // (3,10): warning CS0169: The field 'C.async' is never used
                //     file async;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "async").WithArguments("C.async").WithLocation(3, 10)
            });
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
    public void MemberNamedFile_06()
    {
        UsingNode($$"""
            class C
            {
                file async;
            }
            """,
            // (3,15): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
            //     file async;
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(3, 15),
            // (3,15): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
            //     file async;
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(3, 15));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.FileKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "async");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void MemberNamedFile_07_CSharp10()
    {
        UsingNode($$"""
            class C
            {
                file item;
            }
            """,
            options: TestOptions.Regular10,
            expectedBindingDiagnostics: new[]
            {
                // (3,5): error CS0246: The type or namespace name 'file' could not be found (are you missing a using directive or an assembly reference?)
                //     file item;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "file").WithArguments("file").WithLocation(3, 5),
                // (3,10): warning CS0169: The field 'C.item' is never used
                //     file item;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "item").WithArguments("C.item").WithLocation(3, 10)
            });

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
                            N(SyntaxKind.IdentifierToken, "item");
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
    public void MemberNamedFile_07()
    {
        UsingNode($$"""
            class C
            {
                file item;
            }
            """,
            expectedParsingDiagnostics: new[]
            {
                // (3,14): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     file item;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(3, 14),
                // (3,14): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     file item;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(3, 14)
            });

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.FileKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "item");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void MemberNamedFile_08()
    {
        UsingNode($$"""
            record file { }
            """,
            expectedBindingDiagnostics: new[]
            {
                // (1,8): error CS9056: Types and aliases cannot be named 'file'.
                // record file { }
                Diagnostic(ErrorCode.ERR_FileTypeNameDisallowed, "file").WithLocation(1, 8)
            });

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RecordDeclaration);
            {
                N(SyntaxKind.RecordKeyword);
                N(SyntaxKind.IdentifierToken, "file");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void TypeNamedFile_01()
    {
        UsingNode($$"""
            class file { }
            """,
            expectedBindingDiagnostics: new[]
            {
                // (1,7): error CS9056: Types and aliases cannot be named 'file'.
                // class file { }
                Diagnostic(ErrorCode.ERR_FileTypeNameDisallowed, "file").WithLocation(1, 7)
            });

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
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
    public void TypeNamedFile_01_CSharp10()
    {
        UsingNode($$"""
            class file { }
            """,
            options: TestOptions.Regular10,
            expectedBindingDiagnostics: new[]
            {
                // (1,7): warning CS8981: The type name 'file' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // class file { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "file").WithArguments("file").WithLocation(1, 7)
            });

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "file");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp10)]
    [InlineData(LanguageVersion.CSharp11)]
    public void TypeNamedFile_02(LanguageVersion languageVersion)
    {
        UsingNode($$"""
            class @file { }
            """,
            options: TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "@file");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void TypeNamedFile_03_CSharp10()
    {
        UsingNode($$"""
            public struct file { public int item; }

            public unsafe class C
            {
                public file _file;
                public file[] _array;
                public file* _ptr;
                public file? _nullable;
                public delegate*<file, file> _funcPtr;
                public (file, file) _tuple;
            }
            """,
            options: TestOptions.Regular10,
            expectedBindingDiagnostics: new[]
            {
                // (1,15): warning CS8981: The type name 'file' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // public struct file { public int item; }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "file").WithArguments("file").WithLocation(1, 15),
                // (3,21): error CS0227: Unsafe code may only appear if compiling with /unsafe
                // public unsafe class C
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "C").WithLocation(3, 21)
            });

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "file");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "item");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.UnsafeKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "file");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "_file");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "file");
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
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "_array");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PointerType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "file");
                            }
                            N(SyntaxKind.AsteriskToken);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "_ptr");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.NullableType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "file");
                            }
                            N(SyntaxKind.QuestionToken);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "_nullable");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.VariableDeclaration);
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
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "file");
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.FunctionPointerParameter);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "file");
                                    }
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "_funcPtr");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.TupleType);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "file");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "file");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "_tuple");
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
    public void TypeNamedFile_03_CSharp11()
    {
        UsingNode($$"""
            public struct file { public int item; }

            public unsafe class C
            {
                public file _file;
                public file[] _array;
                public file* _ptr;
                public file? _nullable;
                public delegate*<file, file> _funcPtr;
                public (file, file) _tuple;
            }
            """,
            expectedParsingDiagnostics: new[]
            {
                // (5,22): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     public file _file;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(5, 22),
                // (5,22): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     public file _file;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(5, 22),
                // (6,16): error CS1031: Type expected
                //     public file[] _array;
                Diagnostic(ErrorCode.ERR_TypeExpected, "[").WithLocation(6, 16),
                // (7,16): error CS1031: Type expected
                //     public file* _ptr;
                Diagnostic(ErrorCode.ERR_TypeExpected, "*").WithLocation(7, 16),
                // (8,16): error CS1031: Type expected
                //     public file? _nullable;
                Diagnostic(ErrorCode.ERR_TypeExpected, "?").WithLocation(8, 16)
            },
            expectedBindingDiagnostics: new[]
            {
                // (1,15): error CS9056: Types and aliases cannot be named 'file'.
                // public struct file { public int item; }
                Diagnostic(ErrorCode.ERR_FileTypeNameDisallowed, "file").WithLocation(1, 15),
                // (3,21): error CS0227: Unsafe code may only appear if compiling with /unsafe
                // public unsafe class C
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "C").WithLocation(3, 21),
                // (5,22): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     public file _file;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(5, 22),
                // (5,22): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     public file _file;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(5, 22),
                // (6,16): error CS1031: Type expected
                //     public file[] _array;
                Diagnostic(ErrorCode.ERR_TypeExpected, "[").WithLocation(6, 16),
                // (6,19): error CS0106: The modifier 'file' is not valid for this item
                //     public file[] _array;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "_array").WithArguments("file").WithLocation(6, 19),
                // (7,16): error CS1031: Type expected
                //     public file* _ptr;
                Diagnostic(ErrorCode.ERR_TypeExpected, "*").WithLocation(7, 16),
                // (7,18): error CS0106: The modifier 'file' is not valid for this item
                //     public file* _ptr;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "_ptr").WithArguments("file").WithLocation(7, 18),
                // (8,16): error CS1031: Type expected
                //     public file? _nullable;
                Diagnostic(ErrorCode.ERR_TypeExpected, "?").WithLocation(8, 16),
                // (8,18): error CS0106: The modifier 'file' is not valid for this item
                //     public file? _nullable;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "_nullable").WithArguments("file").WithLocation(8, 18)
            });

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "file");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "item");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.UnsafeKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.FileKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_file");
                    }
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.FileKeyword);
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
                                N(SyntaxKind.OmittedArraySizeExpression);
                                {
                                    N(SyntaxKind.OmittedArraySizeExpressionToken);
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "_array");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.FileKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PointerType);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.AsteriskToken);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "_ptr");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.FileKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.NullableType);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.QuestionToken);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "_nullable");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.VariableDeclaration);
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
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "file");
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.FunctionPointerParameter);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "file");
                                    }
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "_funcPtr");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.TupleType);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "file");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "file");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "_tuple");
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
    public void Errors_01_CSharp10()
    {
        UsingNode($$"""
            file
            """,
            options: TestOptions.Regular10,
            expectedParsingDiagnostics: new[]
            {
                // (1,5): error CS1001: Identifier expected
                // file
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 5),
                // (1,5): error CS1002: ; expected
                // file
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 5)
            },
            expectedBindingDiagnostics: new[]
            {
                // (1,1): error CS0246: The type or namespace name 'file' could not be found (are you missing a using directive or an assembly reference?)
                // file
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "file").WithArguments("file").WithLocation(1, 1),
                // (1,5): error CS1001: Identifier expected
                // file
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 5),
                // (1,5): error CS1002: ; expected
                // file
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 5)
            });

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
    public void Errors_01()
    {
        UsingNode($$"""
            file
            """,
            expectedParsingDiagnostics: new[]
            {
                // (1,5): error CS1001: Identifier expected
                // file
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 5),
                // (1,5): error CS1002: ; expected
                // file
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 5)
            },
            expectedBindingDiagnostics: new[]
            {
                // (1,1): error CS0246: The type or namespace name 'file' could not be found (are you missing a using directive or an assembly reference?)
                // file
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "file").WithArguments("file").WithLocation(1, 1),
                // (1,5): error CS1001: Identifier expected
                // file
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 5),
                // (1,5): error CS1002: ; expected
                // file
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 5)
            });

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
    public void Errors_02_CSharp10()
    {
        UsingNode($$"""
            file;
            """,
            options: TestOptions.Regular10,
            expectedBindingDiagnostics: new[]
            {
                // (1,1): error CS0103: The name 'file' does not exist in the current context
                // file;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "file").WithArguments("file").WithLocation(1, 1),
                // (1,1): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                // file;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "file").WithLocation(1, 1)
            });

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
    public void Errors_02()
    {
        UsingNode($$"""
            file;
            """,
            expectedBindingDiagnostics: new[]
            {
                // (1,1): error CS0103: The name 'file' does not exist in the current context
                // file;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "file").WithArguments("file").WithLocation(1, 1),
                // (1,1): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                // file;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "file").WithLocation(1, 1)
            });

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
    public void Errors_03_CSharp10()
    {
        UsingNode($$"""
            file namespace NS;
            """,
            options: TestOptions.Regular10,
            expectedParsingDiagnostics: new[]
            {
                // (1,6): error CS1001: Identifier expected
                // file namespace NS;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "namespace").WithLocation(1, 6),
                // (1,6): error CS1002: ; expected
                // file namespace NS;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "namespace").WithLocation(1, 6)
            },
            expectedBindingDiagnostics: new[]
            {
                // (1,1): error CS0246: The type or namespace name 'file' could not be found (are you missing a using directive or an assembly reference?)
                // file namespace NS;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "file").WithArguments("file").WithLocation(1, 1),
                // (1,6): error CS1001: Identifier expected
                // file namespace NS;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "namespace").WithLocation(1, 6),
                // (1,6): error CS1002: ; expected
                // file namespace NS;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "namespace").WithLocation(1, 6),
                // (1,16): error CS8956: File-scoped namespace must precede all other members in a file.
                // file namespace NS;
                Diagnostic(ErrorCode.ERR_FileScopedNamespaceNotBeforeAllMembers, "NS").WithLocation(1, 16)
            });

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
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
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
    public void Errors_03()
    {
        UsingNode($$"""
            file namespace NS;
            """,
            expectedParsingDiagnostics: new[]
            {
                // (1,6): error CS1001: Identifier expected
                // file namespace NS;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "namespace").WithLocation(1, 6),
                // (1,6): error CS1002: ; expected
                // file namespace NS;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "namespace").WithLocation(1, 6)
            },
            expectedBindingDiagnostics: new[]
            {
                // (1,1): error CS0246: The type or namespace name 'file' could not be found (are you missing a using directive or an assembly reference?)
                // file namespace NS;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "file").WithArguments("file").WithLocation(1, 1),
                // (1,6): error CS1001: Identifier expected
                // file namespace NS;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "namespace").WithLocation(1, 6),
                // (1,6): error CS1002: ; expected
                // file namespace NS;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "namespace").WithLocation(1, 6),
                // (1,16): error CS8956: File-scoped namespace must precede all other members in a file.
                // file namespace NS;
                Diagnostic(ErrorCode.ERR_FileScopedNamespaceNotBeforeAllMembers, "NS").WithLocation(1, 16)
            });

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
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
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
    public void Errors_04_CSharp10()
    {
        UsingNode($$"""
            file namespace NS { }
            """,
            options: TestOptions.Regular10,
            expectedParsingDiagnostics: new[]
            {
                // (1,6): error CS1001: Identifier expected
                // file namespace NS { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "namespace").WithLocation(1, 6),
                // (1,6): error CS1002: ; expected
                // file namespace NS { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "namespace").WithLocation(1, 6)
            },
            expectedBindingDiagnostics: new[]
            {
                // (1,1): error CS0246: The type or namespace name 'file' could not be found (are you missing a using directive or an assembly reference?)
                // file namespace NS { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "file").WithArguments("file").WithLocation(1, 1),
                // (1,6): error CS1001: Identifier expected
                // file namespace NS { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "namespace").WithLocation(1, 6),
                // (1,6): error CS1002: ; expected
                // file namespace NS { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "namespace").WithLocation(1, 6)
            });

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
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
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
    public void Errors_04()
    {
        UsingNode($$"""
            file namespace NS { }
            """,
            expectedParsingDiagnostics: new[]
            {
                // (1,6): error CS1001: Identifier expected
                // file namespace NS { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "namespace").WithLocation(1, 6),
                // (1,6): error CS1002: ; expected
                // file namespace NS { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "namespace").WithLocation(1, 6)
            },
            expectedBindingDiagnostics: new[]
            {
                // (1,1): error CS0246: The type or namespace name 'file' could not be found (are you missing a using directive or an assembly reference?)
                // file namespace NS { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "file").WithArguments("file").WithLocation(1, 1),
                // (1,6): error CS1001: Identifier expected
                // file namespace NS { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "namespace").WithLocation(1, 6),
                // (1,6): error CS1002: ; expected
                // file namespace NS { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "namespace").WithLocation(1, 6)
            });

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
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
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
        const int FileModifiersCount = 100000;
        var manyFileModifiers = string.Join(" ", Enumerable.Repeat("file", FileModifiersCount));
        UsingNode(manyFileModifiers,
            expectedParsingDiagnostics: new[]
            {
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "file").WithLocation(1, 499996)
            });

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.IncompleteMember);
            {
                for (var i = 0; i < FileModifiersCount - 1; i++)
                {
                    N(SyntaxKind.FileKeyword);
                }
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "file");
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }
        EOF();

        UsingNode(manyFileModifiers + " class { }",
            expectedParsingDiagnostics: new[]
            {
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(1, 500007)
            },
            expectedBindingDiagnostics: new[]
            {
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "file").WithArguments("file").WithLocation(1, 6),
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(1, 500007)
            });

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                for (int i = 0; i < FileModifiersCount; i++)
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
            """,
            options: TestOptions.Regular8,
            expectedBindingDiagnostics: new[]
            {
                // (3,5): error CS0246: The type or namespace name 'file' could not be found (are you missing a using directive or an assembly reference?)
                //     file record();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "file").WithArguments("file").WithLocation(3, 5),
                // (3,10): error CS0501: 'C.record()' must declare a body because it is not marked abstract, extern, or partial
                //     file record();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "record").WithArguments("C.record()").WithLocation(3, 10)
            });

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
    public void MethodNamedRecord_01_CSharp11()
    {
        UsingNode("""
            class C
            {
                file record();
            }
            """,
            expectedBindingDiagnostics: new[]
            {
                // (3,10): error CS0106: The modifier 'file' is not valid for this item
                //     file record();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "record").WithArguments("file").WithLocation(3, 10),
                // (3,10): error CS1520: Method must have a return type
                //     file record();
                Diagnostic(ErrorCode.ERR_MemberNeedsType, "record").WithLocation(3, 10)
            });

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
            """,
            options: TestOptions.Regular8,
            expectedBindingDiagnostics: new[]
            {
                // (3,5): error CS0246: The type or namespace name 'file' could not be found (are you missing a using directive or an assembly reference?)
                //     file record() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "file").WithArguments("file").WithLocation(3, 5),
                // (3,10): error CS0161: 'C.record()': not all code paths return a value
                //     file record() { }
                Diagnostic(ErrorCode.ERR_ReturnExpected, "record").WithArguments("C.record()").WithLocation(3, 10)
            });

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
    public void MethodNamedRecord_02_CSharp11()
    {
        UsingNode("""
            class C
            {
                file record() { }
            }
            """, expectedBindingDiagnostics: new[]
            {
                // (3,10): error CS0106: The modifier 'file' is not valid for this item
                //     file record() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "record").WithArguments("file").WithLocation(3, 10),
                // (3,10): error CS1520: Method must have a return type
                //     file record() { }
                Diagnostic(ErrorCode.ERR_MemberNeedsType, "record").WithLocation(3, 10)
            });

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
            expectedBindingDiagnostics: new[]
            {
                // (3,10): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                //     file record X();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(3, 10),
                // (3,17): error CS0106: The modifier 'file' is not valid for this item
                //     file record X();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "X").WithArguments("file").WithLocation(3, 17),
                // (3,17): error CS0501: 'C.X()' must declare a body because it is not marked abstract, extern, or partial
                //     file record X();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "X").WithArguments("C.X()").WithLocation(3, 17)
            });

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.FileKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "record");
                    }
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
    public void FileRecord_01_CSharp11()
    {
        UsingNode("""
            class C
            {
                file record X();
            }
            """,
            expectedBindingDiagnostics: new[]
            {
                // (3,17): error CS9054: File-local type 'C.X' must be defined in a top level type; 'C.X' is a nested type.
                //     file record X();
                Diagnostic(ErrorCode.ERR_FileTypeNested, "X").WithArguments("C.X").WithLocation(3, 17)
            });

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
            expectedBindingDiagnostics: new[]
            {
                // (3,10): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                //     file record X() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(3, 10),
                // (3,17): error CS0106: The modifier 'file' is not valid for this item
                //     file record X() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "X").WithArguments("file").WithLocation(3, 17),
                // (3,17): error CS0161: 'C.X()': not all code paths return a value
                //     file record X() { }
                Diagnostic(ErrorCode.ERR_ReturnExpected, "X").WithArguments("C.X()").WithLocation(3, 17)
            });

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.FileKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "record");
                    }
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
    public void FileRecord_02_CSharp11()
    {
        UsingNode("""
            class C
            {
                file record X() { }
            }
            """,
            expectedBindingDiagnostics: new[]
            {
                // (3,17): error CS9054: File-local type 'C.X' must be defined in a top level type; 'C.X' is a nested type.
                //     file record X() { }
                Diagnostic(ErrorCode.ERR_FileTypeNested, "X").WithArguments("C.X").WithLocation(3, 17)
            });

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
            options: TestOptions.Regular8, expectedBindingDiagnostics: new[]
            {
                // (3,10): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                //     file record X;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(3, 10),
                // (3,17): error CS0106: The modifier 'file' is not valid for this item
                //     file record X;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "X").WithArguments("file").WithLocation(3, 17),
                // (3,17): warning CS0169: The field 'C.X' is never used
                //     file record X;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "X").WithArguments("C.X").WithLocation(3, 17)
            });

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.FileKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "record");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "X");
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
    public void FileRecord_03_CSharp11()
    {
        UsingNode("""
            class C
            {
                file record X;
            }
            """,
            expectedBindingDiagnostics: new[]
            {
                // (3,17): error CS9054: File-local type 'C.X' must be defined in a top level type; 'C.X' is a nested type.
                //     file record X;
                Diagnostic(ErrorCode.ERR_FileTypeNested, "X").WithArguments("C.X").WithLocation(3, 17)
            });

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

    [Fact]
    public void FileRecord_04_CSharp11()
    {
        UsingNode("""
            file record X();
            """);

        N(SyntaxKind.CompilationUnit);
        {
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
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void LocalVariable_01()
    {
        UsingNode("""
            void M()
            {
                file file;
            }
            """,
            expectedBindingDiagnostics: new[]
            {
                // (1,6): warning CS8321: The local function 'M' is declared but never used
                // void M()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "M").WithArguments("M").WithLocation(1, 6),
                // (3,5): error CS0118: 'file' is a variable but is used like a type
                //     file file;
                Diagnostic(ErrorCode.ERR_BadSKknown, "file").WithArguments("file", "variable", "type").WithLocation(3, 5),
                // (3,10): warning CS0168: The variable 'file' is declared but never used
                //     file file;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "file").WithArguments("file").WithLocation(3, 10)
            });
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
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
                                    N(SyntaxKind.IdentifierToken, "file");
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
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void LocalVariable_02()
    {
        UsingNode("""
            void M()
            {
                int file;
            }
            """,
            expectedBindingDiagnostics: new[]
            {
                // (1,6): warning CS8321: The local function 'M' is declared but never used
                // void M()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "M").WithArguments("M").WithLocation(1, 6),
                // (3,9): warning CS0168: The variable 'file' is declared but never used
                //     int file;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "file").WithArguments("file").WithLocation(3, 9)
            });
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
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
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp10)]
    [InlineData(LanguageVersion.CSharp11)]
    public void TopLevelVariable_01(LanguageVersion languageVersion)
    {
        UsingNode("""
            file file;
            """,
            options: TestOptions.Regular.WithLanguageVersion(languageVersion),
            expectedBindingDiagnostics: new[]
            {
                // (1,1): error CS0118: 'file' is a variable but is used like a type
                // file file;
                Diagnostic(ErrorCode.ERR_BadSKknown, "file").WithArguments("file", "variable", "type").WithLocation(1, 1),
                // (1,6): warning CS0168: The variable 'file' is declared but never used
                // file file;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "file").WithArguments("file").WithLocation(1, 6)
            });

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
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp10)]
    [InlineData(LanguageVersion.CSharp11)]
    public void TopLevelVariable_02(LanguageVersion languageVersion)
    {
        UsingNode("""
            int file;
            """,
            options: TestOptions.Regular.WithLanguageVersion(languageVersion),
            expectedBindingDiagnostics: new[]
            {
                // (1,5): warning CS0168: The variable 'file' is declared but never used
                // int file;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "file").WithArguments("file").WithLocation(1, 5)
            });
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
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
                            N(SyntaxKind.IdentifierToken, "file");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp10)]
    [InlineData(LanguageVersion.CSharp11)]
    public void TopLevelVariable_03(LanguageVersion languageVersion)
    {
        UsingNode("""
            bool file;
            file = true;
            """,
            options: TestOptions.Regular.WithLanguageVersion(languageVersion),
            expectedBindingDiagnostics: new[]
            {
                // (1,6): warning CS0219: The variable 'file' is assigned but its value is never used
                // bool file;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "file").WithArguments("file").WithLocation(1, 6)
            });

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
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
                            N(SyntaxKind.IdentifierToken, "file");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "file");
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.TrueLiteralExpression);
                        {
                            N(SyntaxKind.TrueKeyword);
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
    public void Variable_01()
    {
        UsingNode("""
            void M()
            {
                bool file;
                file = true;
            }
            """,
            expectedBindingDiagnostics: new[]
            {
                // (1,6): warning CS8321: The local function 'M' is declared but never used
                // void M()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "M").WithArguments("M").WithLocation(1, 6),
                // (3,10): warning CS0219: The variable 'file' is assigned but its value is never used
                //     bool file;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "file").WithArguments("file").WithLocation(3, 10)
            });

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
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
                                    N(SyntaxKind.BoolKeyword);
                                }
                                N(SyntaxKind.VariableDeclarator);
                                {
                                    N(SyntaxKind.IdentifierToken, "file");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.SimpleAssignmentExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "file");
                                }
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.TrueLiteralExpression);
                                {
                                    N(SyntaxKind.TrueKeyword);
                                }
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
    public void LambdaReturn()
    {
        UsingNode("""
            _ = file () => { };
            """,
            expectedBindingDiagnostics: new[]
            {
                // (1,1): error CS8183: Cannot infer the type of implicitly-typed discard.
                // _ = file () => { };
                Diagnostic(ErrorCode.ERR_DiscardTypeInferenceFailed, "_").WithLocation(1, 1),
                // (1,5): error CS0246: The type or namespace name 'file' could not be found (are you missing a using directive or an assembly reference?)
                // _ = file () => { };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "file").WithArguments("file").WithLocation(1, 5)
            });
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
                            N(SyntaxKind.IdentifierToken, "_");
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.ParenthesizedLambdaExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "file");
                            }
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
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void LocalFunctionReturn()
    {
        UsingNode("""
            file local() { };
            """,
            expectedBindingDiagnostics: new[]
            {
                // (1,1): error CS0246: The type or namespace name 'file' could not be found (are you missing a using directive or an assembly reference?)
                // file local() { };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "file").WithArguments("file").WithLocation(1, 1),
                // (1,6): error CS0161: 'local()': not all code paths return a value
                // file local() { };
                Diagnostic(ErrorCode.ERR_ReturnExpected, "local").WithArguments("local()").WithLocation(1, 6),
                // (1,6): warning CS8321: The local function 'local' is declared but never used
                // file local() { };
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(1, 6)
            });
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "file");
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
    public void ParameterModifier()
    {
        UsingNode("""
            class C
            {
                void M(file int x) { }
            }
            """,
            expectedParsingDiagnostics: new[]
            {
                // (3,17): error CS1001: Identifier expected
                //     void M(file int x) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(3, 17),
                // (3,17): error CS1003: Syntax error, ',' expected
                //     void M(file int x) { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",").WithLocation(3, 17)
            },
            expectedBindingDiagnostics: new[]
            {
                // (3,12): error CS0246: The type or namespace name 'file' could not be found (are you missing a using directive or an assembly reference?)
                //     void M(file int x) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "file").WithArguments("file").WithLocation(3, 12),
                // (3,17): error CS1001: Identifier expected
                //     void M(file int x) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(3, 17),
                // (3,17): error CS1003: Syntax error, ',' expected
                //     void M(file int x) { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",").WithLocation(3, 17)
            });
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
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "file");
                            }
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "x");
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
    public void ParameterType()
    {
        UsingNode("""
            class C
            {
                void M(file x) { }
            }
            """,
            expectedBindingDiagnostics: new[]
            {
                // (3,12): error CS0246: The type or namespace name 'file' could not be found (are you missing a using directive or an assembly reference?)
                //     void M(file x) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "file").WithArguments("file").WithLocation(3, 12)
            });
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
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "file");
                            }
                            N(SyntaxKind.IdentifierToken, "x");
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75992")]
    public void TestFileModifierAfterIncompleteBaseList1()
    {
        UsingTree("""
            class C : B
            file class D
            {
            }
            """,
            // (1,12): error CS1514: { expected
            // class C : B
            Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(1, 12),
            // (1,12): error CS1513: } expected
            // class C : B
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 12));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.BaseList);
                {
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.SimpleBaseType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                    }
                }
                M(SyntaxKind.OpenBraceToken);
                M(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.FileKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "D");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75992")]
    public void TestFileModifierAfterIncompleteBaseList2()
    {
        UsingTree("""
            class C : B, file
            {
            }

            class file
            {
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.BaseList);
                {
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.SimpleBaseType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.SimpleBaseType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "file");
                        }
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "file");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }
}
