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

public sealed class ClosedModifierParsingTests : ParsingTests
{
    public ClosedModifierParsingTests(ITestOutputHelper output) : base(output) { }

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
    [InlineData(SyntaxKind.RecordKeyword)]
    public void ClosedModifier_01_Valid(SyntaxKind typeKeyword)
    {
        UsingNode($$"""
            closed {{SyntaxFacts.GetText(typeKeyword)}} C { }
            """);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxFacts.GetBaseTypeDeclarationKind(typeKeyword));
            {
                N(SyntaxKind.ClosedKeyword);
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
    [InlineData(SyntaxKind.StructKeyword)]
    [InlineData(SyntaxKind.InterfaceKeyword)]
    [InlineData(SyntaxKind.EnumKeyword)]
    public void ClosedModifier_01_Invalid(SyntaxKind typeKeyword)
    {
        UsingNode($$"""
            closed {{SyntaxFacts.GetText(typeKeyword)}} C { }
            """,
            expectedBindingDiagnostics: [
                // (1,15): error CS0106: The modifier 'closed' is not valid for this item
                // closed struct C { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("closed")
            ]);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxFacts.GetBaseTypeDeclarationKind(typeKeyword));
            {
                N(SyntaxKind.ClosedKeyword);
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
    [InlineData(SyntaxKind.RecordKeyword)]
    public void ClosedModifier_02_Valid(SyntaxKind typeKeyword)
    {
        UsingNode($$"""
            closed partial {{SyntaxFacts.GetText(typeKeyword)}} C { }
            """);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxFacts.GetBaseTypeDeclarationKind(typeKeyword));
            {
                N(SyntaxKind.ClosedKeyword);
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
    [InlineData(SyntaxKind.StructKeyword)]
    [InlineData(SyntaxKind.InterfaceKeyword)]
    public void ClosedModifier_02_Invalid(SyntaxKind typeKeyword)
    {
        UsingNode($$"""
            closed partial {{SyntaxFacts.GetText(typeKeyword)}} C { }
            """, expectedBindingDiagnostics: [
                // (1,26): error CS0106: The modifier 'closed' is not valid for this item
                // closed partial interface C { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("closed")
            ]);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxFacts.GetBaseTypeDeclarationKind(typeKeyword));
            {
                N(SyntaxKind.ClosedKeyword);
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
    public void ClosedModifier_02_Enum()
    {
        UsingNode($$"""
            closed partial enum C { }
            """,
            expectedBindingDiagnostics: [
                // (1,21): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
                // closed partial enum C { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "C").WithLocation(1, 21),
                // (1,21): error CS0106: The modifier 'closed' is not valid for this item
                // closed partial enum C { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("closed").WithLocation(1, 21)
            ]);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.EnumDeclaration);
            {
                N(SyntaxKind.ClosedKeyword);
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
    public void ClosedModifier_03(SyntaxKind typeKeyword)
    {
        UsingNode($$"""
            partial closed {{SyntaxFacts.GetText(typeKeyword)}} C { }
            """,
            expectedParsingDiagnostics: [
                // (1,16): error CS1002: ; expected
                // partial closed class C { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, SyntaxFacts.GetText(typeKeyword)).WithLocation(1, 16)
            ],
            expectedBindingDiagnostics: [
                // (1,1): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
                // partial closed interface C { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(1, 1),
                // (1,9): warning CS0168: The variable 'closed' is declared but never used
                // partial closed interface C { }
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "closed").WithArguments("closed").WithLocation(1, 9),
                // (1,14): error CS1002: ; expected
                // partial closed {{SyntaxFacts.GetText(typeKeyword)}} C { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, SyntaxFacts.GetText(typeKeyword)).WithLocation(1, 16)
            ]);
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
                            N(SyntaxKind.IdentifierToken, "closed");
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
    public void ClosedModifier_04()
    {
        UsingNode("""
            partial closed record C { }
            """,
            expectedBindingDiagnostics: [
                // (1,1): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                // partial closed record C { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(1, 1)
            ]);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RecordDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.ClosedKeyword);
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
    public void ClosedModifier_05()
    {
        UsingNode($$"""
            closed partial record class C { }
            """);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RecordDeclaration);
            {
                N(SyntaxKind.ClosedKeyword);
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.RecordKeyword);
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
    public void ClosedModifier_06()
    {
        UsingNode($$"""
            partial closed record struct C { }
            """,
            expectedBindingDiagnostics: [
                // (1,1): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
                // partial closed record struct C { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(1, 1),
                // (1,30): error CS0106: The modifier 'closed' is not valid for this item
                // partial closed record struct C { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("closed").WithLocation(1, 30)
            ]);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RecordStructDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.ClosedKeyword);
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
    public void ClosedModifier_07_CSharp14()
    {
        UsingNode($$"""
            closed partial ref struct C { }
            """,
            options: TestOptions.Regular14,
            expectedParsingDiagnostics: [
                // (1,16): error CS1003: Syntax error, ',' expected
                // closed partial ref struct C { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(1, 16),
                // (1,20): error CS1002: ; expected
                // closed partial ref struct C { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(1, 20)
            ],
            expectedBindingDiagnostics: [
                // (1,1): error CS0246: The type or namespace name 'closed' could not be found (are you missing a using directive or an assembly reference?)
                // closed partial ref struct C { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "closed").WithArguments("closed").WithLocation(1, 1),
                // (1,8): warning CS0168: The variable 'partial' is declared but never used
                // closed partial ref struct C { }
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "partial").WithArguments("partial").WithLocation(1, 8),
                // (1,16): error CS1003: Syntax error, ',' expected
                // closed partial ref struct C { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(1, 16),
                // (1,20): error CS1002: ; expected
                // closed partial ref struct C { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(1, 20)
            ]);
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
                            N(SyntaxKind.IdentifierToken, "closed");
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
    public void ClosedModifier_07()
    {
        UsingNode($$"""
            closed partial ref struct C { }
            """,
            expectedParsingDiagnostics: [
                // (1,16): error CS1003: Syntax error, ',' expected
                // closed partial ref struct C { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(1, 16),
                // (1,20): error CS1002: ; expected
                // closed partial ref struct C { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(1, 20)
            ],
            expectedBindingDiagnostics: [
                // (1,1): error CS0246: The type or namespace name 'closed' could not be found (are you missing a using directive or an assembly reference?)
                // closed partial ref struct C { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "closed").WithArguments("closed").WithLocation(1, 1),
                // (1,8): warning CS0168: The variable 'partial' is declared but never used
                // closed partial ref struct C { }
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "partial").WithArguments("partial").WithLocation(1, 8),
                // (1,16): error CS1003: Syntax error, ',' expected
                // closed partial ref struct C { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(1, 16),
                // (1,20): error CS1002: ; expected
                // closed partial ref struct C { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(1, 20)
            ]);

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
                            N(SyntaxKind.IdentifierToken, "closed");
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
    public void ClosedModifier_08()
    {
        UsingNode($$"""
            partial closed ref struct C { }
            """,
            expectedParsingDiagnostics: [
                // (1,16): error CS1003: Syntax error, ',' expected
                // partial closed ref struct C { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(1, 16),
                // (1,20): error CS1002: ; expected
                // partial closed ref struct C { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(1, 20)
            ],
            expectedBindingDiagnostics: [
                // (1,1): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
                // partial closed ref struct C { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(1, 1),
                // (1,9): warning CS0168: The variable 'closed' is declared but never used
                // partial closed ref struct C { }
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "closed").WithArguments("closed").WithLocation(1, 9),
                // (1,16): error CS1003: Syntax error, ',' expected
                // partial closed ref struct C { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(1, 16),
                // (1,20): error CS1002: ; expected
                // partial closed ref struct C { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(1, 20)
            ]);

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
                            N(SyntaxKind.IdentifierToken, "closed");
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
    public void ClosedModifier_09()
    {
        UsingNode($$"""
            closed abstract class C { }
            """);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClosedKeyword);
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
    public void ClosedModifier_10()
    {
        UsingNode($$"""
            abstract closed class C { }
            """);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.AbstractKeyword);
                N(SyntaxKind.ClosedKeyword);
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
    [InlineData(SyntaxKind.RecordKeyword)]
    public void ClosedModifier_11(SyntaxKind typeKeyword)
    {
        UsingNode($$"""
            public closed {{SyntaxFacts.GetText(typeKeyword)}} C { }
            """);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxFacts.GetBaseTypeDeclarationKind(typeKeyword));
            {
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.ClosedKeyword);
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
    [InlineData(SyntaxKind.StructKeyword)]
    [InlineData(SyntaxKind.InterfaceKeyword)]
    [InlineData(SyntaxKind.EnumKeyword)]
    public void ClosedModifier_11_Invalid(SyntaxKind typeKeyword)
    {
        UsingNode($$"""
            public closed {{SyntaxFacts.GetText(typeKeyword)}} C { }
            """, expectedBindingDiagnostics: [
                // (1,20): error CS0106: The modifier 'closed' is not valid for this item
                // public closed enum C { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("closed")
            ]);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxFacts.GetBaseTypeDeclarationKind(typeKeyword));
            {
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.ClosedKeyword);
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
    [InlineData(SyntaxKind.RecordKeyword)]
    public void ClosedModifier_12_Valid(SyntaxKind typeKeyword)
    {
        UsingNode($$"""
            closed public {{SyntaxFacts.GetText(typeKeyword)}} C { }
            """);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxFacts.GetBaseTypeDeclarationKind(typeKeyword));
            {
                N(SyntaxKind.ClosedKeyword);
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

    [Theory]
    [InlineData(SyntaxKind.StructKeyword)]
    [InlineData(SyntaxKind.InterfaceKeyword)]
    [InlineData(SyntaxKind.EnumKeyword)]
    public void ClosedModifier_12_Invalid(SyntaxKind typeKeyword)
    {
        UsingNode($$"""
            closed public {{SyntaxFacts.GetText(typeKeyword)}} C { }
            """, expectedBindingDiagnostics: [
                // (1,20): error CS0106: The modifier 'closed' is not valid for this item
                // closed public enum C { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("closed")
            ]);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxFacts.GetBaseTypeDeclarationKind(typeKeyword));
            {
                N(SyntaxKind.ClosedKeyword);
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
    public void ClosedModifier_13()
    {
        UsingNode("""
            closed class C { }
            """,
            options: TestOptions.Regular10,
            expectedBindingDiagnostics: [
                // (1,14): error CS8652: The feature 'closed classes' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // closed class C { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "C").WithArguments("closed classes").WithLocation(1, 14)
            ]);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClosedKeyword);
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
    public void ClosedModifier_14()
    {
        UsingNode("""
            closed delegate void D();
            """, expectedBindingDiagnostics: [
                // (1,22): error CS0106: The modifier 'closed' is not valid for this item
                // closed delegate void D();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "D").WithArguments("closed").WithLocation(1, 22)
            ]);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.DelegateDeclaration);
            {
                N(SyntaxKind.ClosedKeyword);
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
    public void ClosedModifier_15()
    {
        UsingNode("""
            namespace NS
            {
                closed class C { }
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
                    N(SyntaxKind.ClosedKeyword);
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
    public void ClosedModifier_16()
    {
        UsingNode("""
            namespace NS;
            closed class C { }
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
                    N(SyntaxKind.ClosedKeyword);
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
    public void ClosedModifier_17()
    {
        UsingNode("""
            class Outer
            {
                closed class C { }
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
                    N(SyntaxKind.ClosedKeyword);
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
    public void ClosedModifier_18()
    {
        UsingNode("""
            class C
            {
                closed delegate*<int, void> M();
            }
            """,
            expectedBindingDiagnostics: [
                // (3,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     closed delegate*<int, void> M();
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(3, 12),
                // (3,33): error CS0106: The modifier 'closed' is not valid for this item
                //     closed delegate*<int, void> M();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M").WithArguments("closed").WithLocation(3, 33),
                // (3,33): error CS0501: 'C.M()' must declare a body because it is not marked abstract, extern, or partial
                //     closed delegate*<int, void> M();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M").WithArguments("C.M()").WithLocation(3, 33)
            ]);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.ClosedKeyword);
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
    public void ClosedMember_01()
    {
        UsingNode("""
            class C
            {
                closed void M() { }
            }
            """,
            expectedBindingDiagnostics: [
                // (3,17): error CS0106: The modifier 'closed' is not valid for this item
                //     closed void M() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M").WithArguments("closed").WithLocation(3, 17)
            ]);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.ClosedKeyword);
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
    public void ClosedMember_02()
    {
        UsingNode("""
            class C
            {
                closed int x;
            }
            """,
            expectedBindingDiagnostics: [
                // (3,16): error CS0106: The modifier 'closed' is not valid for this item
                //     closed int x;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "x").WithArguments("closed").WithLocation(3, 16),
                // (3,16): warning CS0169: The field 'C.x' is never used
                //     closed int x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("C.x").WithLocation(3, 16)
            ]);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.ClosedKeyword);
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
    public void ClosedMember_03()
    {
        UsingNode($$"""
            class C
            {
                closed event Action x;
            }
            """,
            expectedBindingDiagnostics: [
                // (3,18): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                //     closed event Action x;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Action").WithArguments("Action").WithLocation(3, 18),
                // (3,25): error CS0106: The modifier 'closed' is not valid for this item
                //     closed event Action x;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "x").WithArguments("closed").WithLocation(3, 25),
                // (3,25): warning CS0067: The event 'C.x' is never used
                //     closed event Action x;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "x").WithArguments("C.x").WithLocation(3, 25)
            ]);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.EventFieldDeclaration);
                {
                    N(SyntaxKind.ClosedKeyword);
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
    public void ClosedMember_04()
    {
        var source = $$"""
            class C
            {
                closed int x { get; set; }
            }
            """;

        UsingNode(source, expectedBindingDiagnostics: [
            // (3,16): error CS0106: The modifier 'closed' is not valid for this item
            //     closed int x { get; set; }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "x").WithArguments("closed").WithLocation(3, 16)
        ]);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.ClosedKeyword);
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
    public void ClosedMember_05()
    {
        var source = $$"""
            class C
            {
                async closed void M() { }
            }
            """;

        UsingNode(source, expectedBindingDiagnostics: [
            // (3,23): error CS0106: The modifier 'closed' is not valid for this item
            //     async closed void M() { }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "M").WithArguments("closed").WithLocation(3, 23)
        ]);
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
                    N(SyntaxKind.ClosedKeyword);
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
    public void MemberNamedClosed_01()
    {
        UsingNode($$"""
            class C
            {
                int closed;
            }
            """, expectedBindingDiagnostics: [
                // (3,9): warning CS0169: The field 'C.closed' is never used
                //     int closed;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "closed").WithArguments("C.closed").WithLocation(3, 9)
            ]);

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
                            N(SyntaxKind.IdentifierToken, "closed");
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
    public void MemberNamedClosed_02()
    {
        UsingNode($$"""
            class C
            {
                int closed { get; set; }
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
                    N(SyntaxKind.IdentifierToken, "closed");
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
    public void MemberNamedClosed_03()
    {
        UsingNode($$"""
            class C
            {
                event Action closed;
            }
            """,
            expectedBindingDiagnostics: [
                // (3,11): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                //     event Action closed;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Action").WithArguments("Action").WithLocation(3, 11),
                // (3,18): warning CS0067: The event 'C.closed' is never used
                //     event Action closed;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "closed").WithArguments("C.closed").WithLocation(3, 18)
            ]);

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
                            N(SyntaxKind.IdentifierToken, "closed");
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
    public void MemberNamedClosed_04()
    {
        UsingNode($$"""
            class C
            {
                void closed() { }
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
                    N(SyntaxKind.IdentifierToken, "closed");
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
    public void MemberNamedClosed_05()
    {
        UsingNode($$"""
            closed class closed { }
            """,
            expectedBindingDiagnostics: [
                // (1,14): error CS9365: Types and aliases cannot be named 'closed'.
                // closed class closed { }
                Diagnostic(ErrorCode.ERR_ClosedTypeNameDisallowed, "closed").WithLocation(1, 14)
            ]);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClosedKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "closed");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void MemberNamedClosed_06_CSharp14()
    {
        UsingNode($$"""
            class C
            {
                closed async;
            }
            """,
            options: TestOptions.Regular14,
            expectedBindingDiagnostics: [
                // (3,5): error CS0246: The type or namespace name 'closed' could not be found (are you missing a using directive or an assembly reference?)
                //     closed async;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "closed").WithArguments("closed").WithLocation(3, 5),
                // (3,12): warning CS0169: The field 'C.async' is never used
                //     closed async;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "async").WithArguments("C.async").WithLocation(3, 12)
            ]);
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
                            N(SyntaxKind.IdentifierToken, "closed");
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
    public void MemberNamedClosed_06()
    {
        UsingNode($$"""
            class C
            {
                closed async;
            }
            """,
            // (3,17): error CS1519: Invalid token ';' in a member declaration
            //     closed async;
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(3, 17),
            // (3,17): error CS1519: Invalid token ';' in a member declaration
            //     closed async;
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(3, 17));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.ClosedKeyword);
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
    public void MemberNamedClosed_07_CSharp14()
    {
        UsingNode($$"""
            class C
            {
                closed item;
            }
            """,
            options: TestOptions.Regular10,
            expectedBindingDiagnostics: [
                // (3,5): error CS0246: The type or namespace name 'closed' could not be found (are you missing a using directive or an assembly reference?)
                //     closed item;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "closed").WithArguments("closed").WithLocation(3, 5),
                // (3,12): warning CS0169: The field 'C.item' is never used
                //     closed item;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "item").WithArguments("C.item").WithLocation(3, 12)
            ]);

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
                            N(SyntaxKind.IdentifierToken, "closed");
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
    public void MemberNamedClosed_07()
    {
        UsingNode($$"""
            class C
            {
                closed item;
            }
            """,
            expectedParsingDiagnostics: [
                // (3,16): error CS1519: Invalid token ';' in a member declaration
                //     closed item;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(3, 16),
                // (3,16): error CS1519: Invalid token ';' in a member declaration
                //     closed item;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(3, 16)
            ]);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.ClosedKeyword);
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
    public void MemberNamedClosed_08()
    {
        UsingNode($$"""
            record closed { }
            """,
            expectedBindingDiagnostics: [
                // (1,8): error CS9056: Types and aliases cannot be named 'closed'.
                // record closed { }
                Diagnostic(ErrorCode.ERR_ClosedTypeNameDisallowed, "closed").WithLocation(1, 8)
            ]);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RecordDeclaration);
            {
                N(SyntaxKind.RecordKeyword);
                N(SyntaxKind.IdentifierToken, "closed");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void TypeNamedClosed_01()
    {
        UsingNode($$"""
            class closed { }
            """,
            expectedBindingDiagnostics: [
                // (1,7): error CS9056: Types and aliases cannot be named 'closed'.
                // class closed { }
                Diagnostic(ErrorCode.ERR_ClosedTypeNameDisallowed, "closed").WithLocation(1, 7)
            ]);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "closed");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void TypeNamedClosed_01_CSharp14()
    {
        UsingNode($$"""
            class closed { }
            """,
            options: TestOptions.Regular10,
            expectedBindingDiagnostics: [
                // (1,7): warning CS8981: The type name 'closed' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // class closed { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "closed").WithArguments("closed").WithLocation(1, 7)
            ]);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "closed");
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
    public void TypeNamedClosed_02(LanguageVersion languageVersion)
    {
        UsingNode($$"""
            class @closed { }
            """,
            options: TestOptions.Regular.WithLanguageVersion(languageVersion));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "@closed");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void TypeNamedClosed_03_CSharp14()
    {
        UsingNode($$"""
            public struct closed { public int item; }

            public unsafe class C
            {
                public closed _closed;
                public closed[] _array;
                public closed* _ptr;
                public closed? _nullable;
                public delegate*<closed, closed> _funcPtr;
                public (closed, closed) _tuple;
            }
            """,
            options: TestOptions.Regular14,
            expectedBindingDiagnostics: [
                // (1,15): error CS9365: Types and aliases cannot be named 'closed'.
                // public struct closed { public int item; }
                Diagnostic(ErrorCode.ERR_ClosedTypeNameDisallowed, "closed").WithLocation(1, 15),
                // (3,21): error CS0227: Unsafe code may only appear if compiling with /unsafe
                // public unsafe class C
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "C").WithLocation(3, 21)
            ]);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "closed");
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
                            N(SyntaxKind.IdentifierToken, "closed");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "_closed");
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
                                N(SyntaxKind.IdentifierToken, "closed");
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
                                N(SyntaxKind.IdentifierToken, "closed");
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
                                N(SyntaxKind.IdentifierToken, "closed");
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
                                        N(SyntaxKind.IdentifierToken, "closed");
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.FunctionPointerParameter);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "closed");
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
                                    N(SyntaxKind.IdentifierToken, "closed");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "closed");
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
    public void TypeNamedClosed_03_CSharp15()
    {
        UsingNode($$"""
            public struct closed { public int item; }

            public unsafe class C
            {
                public closed _closed;
                public closed[] _array;
                public closed* _ptr;
                public closed? _nullable;
                public delegate*<closed, closed> _funcPtr;
                public (closed, closed) _tuple;
            }
            """,
            expectedParsingDiagnostics: [
                // (5,26): error CS1519: Invalid token ';' in a member declaration
                //     public closed _closed;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(5, 26),
                // (5,26): error CS1519: Invalid token ';' in a member declaration
                //     public closed _closed;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(5, 26),
                // (6,18): error CS1031: Type expected
                //     public closed[] _array;
                Diagnostic(ErrorCode.ERR_TypeExpected, "[").WithLocation(6, 18),
                // (7,18): error CS1031: Type expected
                //     public closed* _ptr;
                Diagnostic(ErrorCode.ERR_TypeExpected, "*").WithLocation(7, 18),
                // (8,18): error CS1031: Type expected
                //     public closed? _nullable;
                Diagnostic(ErrorCode.ERR_TypeExpected, "?").WithLocation(8, 18)
            ],
            expectedBindingDiagnostics: [
                // (1,15): error CS9365: Types and aliases cannot be named 'closed'.
                // public struct closed { public int item; }
                Diagnostic(ErrorCode.ERR_ClosedTypeNameDisallowed, "closed").WithLocation(1, 15),
                // (3,21): error CS0227: Unsafe code may only appear if compiling with /unsafe
                // public unsafe class C
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "C").WithLocation(3, 21),
                // (5,26): error CS1519: Invalid token ';' in a member declaration
                //     public closed _closed;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(5, 26),
                // (5,26): error CS1519: Invalid token ';' in a member declaration
                //     public closed _closed;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(5, 26),
                // (6,18): error CS1031: Type expected
                //     public closed[] _array;
                Diagnostic(ErrorCode.ERR_TypeExpected, "[").WithLocation(6, 18),
                // (6,21): error CS0106: The modifier 'closed' is not valid for this item
                //     public closed[] _array;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "_array").WithArguments("closed").WithLocation(6, 21),
                // (7,18): error CS1031: Type expected
                //     public closed* _ptr;
                Diagnostic(ErrorCode.ERR_TypeExpected, "*").WithLocation(7, 18),
                // (7,20): error CS0106: The modifier 'closed' is not valid for this item
                //     public closed* _ptr;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "_ptr").WithArguments("closed").WithLocation(7, 20),
                // (7,20): warning CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('?')
                //     public closed* _ptr;
                Diagnostic(ErrorCode.WRN_ManagedAddr, "_ptr").WithArguments("?").WithLocation(7, 20),
                // (8,18): error CS1031: Type expected
                //     public closed? _nullable;
                Diagnostic(ErrorCode.ERR_TypeExpected, "?").WithLocation(8, 18),
                // (8,20): error CS0106: The modifier 'closed' is not valid for this item
                //     public closed? _nullable;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "_nullable").WithArguments("closed").WithLocation(8, 20)
            ]);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "closed");
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
                    N(SyntaxKind.ClosedKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_closed");
                    }
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ClosedKeyword);
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
                    N(SyntaxKind.ClosedKeyword);
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
                    N(SyntaxKind.ClosedKeyword);
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
                                        N(SyntaxKind.IdentifierToken, "closed");
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.FunctionPointerParameter);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "closed");
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
                                    N(SyntaxKind.IdentifierToken, "closed");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "closed");
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
    public void Errors_01_CSharp14()
    {
        UsingNode($$"""
            closed
            """,
            options: TestOptions.Regular14,
            expectedParsingDiagnostics: [
                // (1,7): error CS1001: Identifier expected
                // closed
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 7),
                // (1,7): error CS1002: ; expected
                // closed
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 7)
            ],
            expectedBindingDiagnostics: [
                // (1,1): error CS0246: The type or namespace name 'closed' could not be found (are you missing a using directive or an assembly reference?)
                // closed
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "closed").WithArguments("closed").WithLocation(1, 1),
                // (1,7): error CS1001: Identifier expected
                // closed
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 7),
                // (1,7): error CS1002: ; expected
                // closed
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 7)
            ]);

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
                            N(SyntaxKind.IdentifierToken, "closed");
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
            closed
            """,
            expectedParsingDiagnostics: [
                // (1,7): error CS1001: Identifier expected
                // closed
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 7),
                // (1,7): error CS1002: ; expected
                // closed
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 7)
            ],
            expectedBindingDiagnostics: [
                // (1,1): error CS0246: The type or namespace name 'closed' could not be found (are you missing a using directive or an assembly reference?)
                // closed
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "closed").WithArguments("closed").WithLocation(1, 1),
                // (1,7): error CS1001: Identifier expected
                // closed
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 7),
                // (1,7): error CS1002: ; expected
                // closed
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 7)
            ]);

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
                            N(SyntaxKind.IdentifierToken, "closed");
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
            closed;
            """,
            options: TestOptions.Regular10,
            expectedBindingDiagnostics: [
                // (1,1): error CS0103: The name 'closed' does not exist in the current context
                // closed;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "closed").WithArguments("closed").WithLocation(1, 1),
                // (1,1): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                // closed;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "closed").WithLocation(1, 1)
            ]);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "closed");
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
            closed;
            """,
            expectedBindingDiagnostics: [
                // (1,1): error CS0103: The name 'closed' does not exist in the current context
                // closed;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "closed").WithArguments("closed").WithLocation(1, 1),
                // (1,1): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                // closed;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "closed").WithLocation(1, 1)
            ]);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "closed");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void Errors_03_CSharp14()
    {
        UsingNode($$"""
            closed namespace NS;
            """,
            options: TestOptions.Regular14,
            expectedParsingDiagnostics: [
                // (1,8): error CS1001: Identifier expected
                // closed namespace NS;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "namespace").WithLocation(1, 8),
                // (1,8): error CS1002: ; expected
                // closed namespace NS;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "namespace").WithLocation(1, 8)
            ],
            expectedBindingDiagnostics: [
                // (1,1): error CS0246: The type or namespace name 'closed' could not be found (are you missing a using directive or an assembly reference?)
                // closed namespace NS;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "closed").WithArguments("closed").WithLocation(1, 1),
                // (1,8): error CS1001: Identifier expected
                // closed namespace NS;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "namespace").WithLocation(1, 8),
                // (1,8): error CS1002: ; expected
                // closed namespace NS;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "namespace").WithLocation(1, 8),
                // (1,18): error CS8956: File-scoped namespace must precede all other members in a file.
                // closed namespace NS;
                Diagnostic(ErrorCode.ERR_FileScopedNamespaceNotBeforeAllMembers, "NS").WithLocation(1, 18)
            ]);

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
                            N(SyntaxKind.IdentifierToken, "closed");
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
            closed namespace NS;
            """,
            expectedParsingDiagnostics: [
                // (1,8): error CS1001: Identifier expected
                // closed namespace NS;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "namespace").WithLocation(1, 8),
                // (1,8): error CS1002: ; expected
                // closed namespace NS;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "namespace").WithLocation(1, 8)
            ],
            expectedBindingDiagnostics: [
                // (1,1): error CS0246: The type or namespace name 'closed' could not be found (are you missing a using directive or an assembly reference?)
                // closed namespace NS;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "closed").WithArguments("closed").WithLocation(1, 1),
                // (1,8): error CS1001: Identifier expected
                // closed namespace NS;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "namespace").WithLocation(1, 8),
                // (1,8): error CS1002: ; expected
                // closed namespace NS;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "namespace").WithLocation(1, 8),
                // (1,18): error CS8956: File-scoped namespace must precede all other members in a file.
                // closed namespace NS;
                Diagnostic(ErrorCode.ERR_FileScopedNamespaceNotBeforeAllMembers, "NS").WithLocation(1, 18)
            ]);

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
                            N(SyntaxKind.IdentifierToken, "closed");
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
    public void Errors_04_CSharp14()
    {
        UsingNode($$"""
            closed namespace NS { }
            """,
            options: TestOptions.Regular14,
            expectedParsingDiagnostics: [
                // (1,8): error CS1001: Identifier expected
                // closed namespace NS { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "namespace").WithLocation(1, 8),
                // (1,8): error CS1002: ; expected
                // closed namespace NS { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "namespace").WithLocation(1, 8)
            ],
            expectedBindingDiagnostics: [
                // (1,1): error CS0246: The type or namespace name 'closed' could not be found (are you missing a using directive or an assembly reference?)
                // closed namespace NS { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "closed").WithArguments("closed").WithLocation(1, 1),
                // (1,8): error CS1001: Identifier expected
                // closed namespace NS { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "namespace").WithLocation(1, 8),
                // (1,8): error CS1002: ; expected
                // closed namespace NS { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "namespace").WithLocation(1, 8)
            ]);

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
                            N(SyntaxKind.IdentifierToken, "closed");
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
            closed namespace NS { }
            """,
            expectedParsingDiagnostics: [
                // (1,8): error CS1001: Identifier expected
                // closed namespace NS { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "namespace").WithLocation(1, 8),
                // (1,8): error CS1002: ; expected
                // closed namespace NS { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "namespace").WithLocation(1, 8)
            ],
            expectedBindingDiagnostics: [
                // (1,1): error CS0246: The type or namespace name 'closed' could not be found (are you missing a using directive or an assembly reference?)
                // closed namespace NS { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "closed").WithArguments("closed").WithLocation(1, 1),
                // (1,8): error CS1001: Identifier expected
                // closed namespace NS { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "namespace").WithLocation(1, 8),
                // (1,8): error CS1002: ; expected
                // closed namespace NS { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "namespace").WithLocation(1, 8)
            ]);

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
                            N(SyntaxKind.IdentifierToken, "closed");
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
    public void Closed_Repeated()
    {
        const int ClosedModifiersCount = 100000;
        var manyClosedModifiers = string.Join(" ", Enumerable.Repeat("closed", ClosedModifiersCount));
        UsingNode(manyClosedModifiers,
            expectedParsingDiagnostics: [
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "closed").WithLocation(1, 699994)
            ]);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.IncompleteMember);
            {
                for (var i = 0; i < ClosedModifiersCount - 1; i++)
                {
                    N(SyntaxKind.ClosedKeyword);
                }
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "closed");
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }
        EOF();

        UsingNode(manyClosedModifiers + " class { }",
            expectedParsingDiagnostics: [
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(1, 700007)
            ],
            expectedBindingDiagnostics: [
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "closed").WithArguments("closed").WithLocation(1, 8),
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(1, 700007)
            ]);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                for (int i = 0; i < ClosedModifiersCount; i++)
                {
                    N(SyntaxKind.ClosedKeyword);
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
                closed record();
            }
            """,
            options: TestOptions.Regular8,
            expectedBindingDiagnostics: [
                // (3,5): error CS0246: The type or namespace name 'closed' could not be found (are you missing a using directive or an assembly reference?)
                //     closed record();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "closed").WithArguments("closed").WithLocation(3, 5),
                // (3,12): error CS0501: 'C.record()' must declare a body because it is not marked abstract, extern, or partial
                //     closed record();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "record").WithArguments("C.record()").WithLocation(3, 12)
            ]);

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
                        N(SyntaxKind.IdentifierToken, "closed");
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
    public void MethodNamedRecord_01_CSharp15()
    {
        UsingNode("""
            class C
            {
                closed record();
            }
            """,
            expectedBindingDiagnostics: [
                // (3,12): error CS0106: The modifier 'closed' is not valid for this item
                //     closed record();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "record").WithArguments("closed").WithLocation(3, 12),
                // (3,12): error CS1520: Method must have a return type
                //     closed record();
                Diagnostic(ErrorCode.ERR_MemberNeedsType, "record").WithLocation(3, 12)
            ]);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ConstructorDeclaration);
                {
                    N(SyntaxKind.ClosedKeyword);
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
                closed record() { }
            }
            """,
            options: TestOptions.Regular8,
            expectedBindingDiagnostics: [
                // (3,5): error CS0246: The type or namespace name 'closed' could not be found (are you missing a using directive or an assembly reference?)
                //     closed record() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "closed").WithArguments("closed").WithLocation(3, 5),
                // (3,12): error CS0161: 'C.record()': not all code paths return a value
                //     closed record() { }
                Diagnostic(ErrorCode.ERR_ReturnExpected, "record").WithArguments("C.record()").WithLocation(3, 12)
            ]);

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
                        N(SyntaxKind.IdentifierToken, "closed");
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
                closed record() { }
            }
            """, expectedBindingDiagnostics: [
                // (3,12): error CS0106: The modifier 'closed' is not valid for this item
                //     closed record() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "record").WithArguments("closed").WithLocation(3, 12),
                // (3,12): error CS1520: Method must have a return type
                //     closed record() { }
                Diagnostic(ErrorCode.ERR_MemberNeedsType, "record").WithLocation(3, 12)
            ]);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ConstructorDeclaration);
                {
                    N(SyntaxKind.ClosedKeyword);
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
    public void ClosedRecord_01_CSharp8()
    {
        UsingNode("""
            class C
            {
                closed record X();
            }
            """,
            options: TestOptions.Regular8,
            expectedBindingDiagnostics: [
                // (3,12): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                //     closed record X();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(3, 12),
                // (3,19): error CS0106: The modifier 'closed' is not valid for this item
                //     closed record X();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "X").WithArguments("closed").WithLocation(3, 19),
                // (3,19): error CS0501: 'C.X()' must declare a body because it is not marked abstract, extern, or partial
                //     closed record X();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "X").WithArguments("C.X()").WithLocation(3, 19)
            ]);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.ClosedKeyword);
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
    public void ClosedRecord_01_CSharp15()
    {
        UsingNode("""
            class C
            {
                closed record X();
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
                    N(SyntaxKind.ClosedKeyword);
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
    public void ClosedRecord_02_CSharp8()
    {
        UsingNode("""
            class C
            {
                closed record X() { }
            }
            """,
            options: TestOptions.Regular8,
            expectedBindingDiagnostics: [
                // (3,12): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                //     closed record X() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(3, 12),
                // (3,19): error CS0106: The modifier 'closed' is not valid for this item
                //     closed record X() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "X").WithArguments("closed").WithLocation(3, 19),
                // (3,19): error CS0161: 'C.X()': not all code paths return a value
                //     closed record X() { }
                Diagnostic(ErrorCode.ERR_ReturnExpected, "X").WithArguments("C.X()").WithLocation(3, 19)
            ]);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.ClosedKeyword);
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
    public void ClosedRecord_02_CSharp11()
    {
        UsingNode("""
            class C
            {
                closed record X() { }
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
                    N(SyntaxKind.ClosedKeyword);
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
    public void ClosedRecord_03_CSharp8()
    {
        UsingNode("""
            class C
            {
                closed record X;
            }
            """,
            options: TestOptions.Regular8, expectedBindingDiagnostics: [
                // (3,12): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                //     closed record X;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(3, 12),
                // (3,19): error CS0106: The modifier 'closed' is not valid for this item
                //     closed record X;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "X").WithArguments("closed").WithLocation(3, 19),
                // (3,19): warning CS0169: The field 'C.X' is never used
                //     closed record X;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "X").WithArguments("C.X").WithLocation(3, 19)
            ]);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.ClosedKeyword);
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
    public void ClosedRecord_03_CSharp14()
    {
        UsingNode("""
            class C
            {
                closed record X;
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
                    N(SyntaxKind.ClosedKeyword);
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
    public void ClosedRecord_04_CSharp11()
    {
        UsingNode("""
            closed record X();
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RecordDeclaration);
            {
                N(SyntaxKind.ClosedKeyword);
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
                closed closed;
            }
            """,
            expectedBindingDiagnostics: [
                // (1,6): warning CS8321: The local function 'M' is declared but never used
                // void M()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "M").WithArguments("M").WithLocation(1, 6),
                // (3,5): error CS0118: 'closed' is a variable but is used like a type
                //     closed closed;
                Diagnostic(ErrorCode.ERR_BadSKknown, "closed").WithArguments("closed", "variable", "type").WithLocation(3, 5),
                // (3,12): warning CS0168: The variable 'closed' is declared but never used
                //     closed closed;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "closed").WithArguments("closed").WithLocation(3, 12)
            ]);
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
                                    N(SyntaxKind.IdentifierToken, "closed");
                                }
                                N(SyntaxKind.VariableDeclarator);
                                {
                                    N(SyntaxKind.IdentifierToken, "closed");
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
                int closed;
            }
            """,
            expectedBindingDiagnostics: [
                // (1,6): warning CS8321: The local function 'M' is declared but never used
                // void M()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "M").WithArguments("M").WithLocation(1, 6),
                // (3,9): warning CS0168: The variable 'closed' is declared but never used
                //     int closed;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "closed").WithArguments("closed").WithLocation(3, 9)
            ]);
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
                                    N(SyntaxKind.IdentifierToken, "closed");
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
    [InlineData(LanguageVersion.CSharp14)]
    [InlineData(LanguageVersion.Preview)]
    public void TopLevelVariable_01(LanguageVersion languageVersion)
    {
        UsingNode("""
            closed closed;
            """,
            options: TestOptions.Regular.WithLanguageVersion(languageVersion),
            expectedBindingDiagnostics: [
                // (1,1): error CS0118: 'closed' is a variable but is used like a type
                // closed closed;
                Diagnostic(ErrorCode.ERR_BadSKknown, "closed").WithArguments("closed", "variable", "type").WithLocation(1, 1),
                // (1,8): warning CS0168: The variable 'closed' is declared but never used
                // closed closed;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "closed").WithArguments("closed").WithLocation(1, 8)
            ]);

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
                            N(SyntaxKind.IdentifierToken, "closed");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "closed");
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
            int closed;
            """,
            options: TestOptions.Regular.WithLanguageVersion(languageVersion),
            expectedBindingDiagnostics: [
                // (1,5): warning CS0168: The variable 'closed' is declared but never used
                // int closed;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "closed").WithArguments("closed").WithLocation(1, 5)
            ]);
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
                            N(SyntaxKind.IdentifierToken, "closed");
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
            bool closed;
            closed = true;
            """,
            options: TestOptions.Regular.WithLanguageVersion(languageVersion),
            expectedBindingDiagnostics: [
                // (1,6): warning CS0219: The variable 'closed' is assigned but its value is never used
                // bool closed;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "closed").WithArguments("closed").WithLocation(1, 6)
            ]);

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
                            N(SyntaxKind.IdentifierToken, "closed");
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
                            N(SyntaxKind.IdentifierToken, "closed");
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
                bool closed;
                closed = true;
            }
            """,
            expectedBindingDiagnostics: [
                // (1,6): warning CS8321: The local function 'M' is declared but never used
                // void M()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "M").WithArguments("M").WithLocation(1, 6),
                // (3,10): warning CS0219: The variable 'closed' is assigned but its value is never used
                //     bool closed;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "closed").WithArguments("closed").WithLocation(3, 10)
            ]);

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
                                    N(SyntaxKind.IdentifierToken, "closed");
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
                                    N(SyntaxKind.IdentifierToken, "closed");
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
            _ = closed () => { };
            """,
            expectedBindingDiagnostics: [
                // (1,1): error CS8183: Cannot infer the type of implicitly-typed discard.
                // _ = closed () => { };
                Diagnostic(ErrorCode.ERR_DiscardTypeInferenceFailed, "_").WithLocation(1, 1),
                // (1,5): error CS0246: The type or namespace name 'closed' could not be found (are you missing a using directive or an assembly reference?)
                // _ = closed () => { };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "closed").WithArguments("closed").WithLocation(1, 5)
            ]);
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
                                N(SyntaxKind.IdentifierToken, "closed");
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
            closed local() { };
            """,
            expectedBindingDiagnostics: [
                // (1,1): error CS0246: The type or namespace name 'closed' could not be found (are you missing a using directive or an assembly reference?)
                // closed local() { };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "closed").WithArguments("closed").WithLocation(1, 1),
                // (1,8): error CS0161: 'local()': not all code paths return a value
                // closed local() { };
                Diagnostic(ErrorCode.ERR_ReturnExpected, "local").WithArguments("local()").WithLocation(1, 8),
                // (1,8): warning CS8321: The local function 'local' is declared but never used
                // closed local() { };
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(1, 8)
            ]);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "closed");
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
                void M(closed int x) { }
            }
            """,
            expectedParsingDiagnostics: [
                // (3,19): error CS1001: Identifier expected
                //     void M(closed int x) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(3, 19),
                // (3,19): error CS1003: Syntax error, ',' expected
                //     void M(closed int x) { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",").WithLocation(3, 19)
            ],
            expectedBindingDiagnostics: [
                // (3,12): error CS0246: The type or namespace name 'closed' could not be found (are you missing a using directive or an assembly reference?)
                //     void M(closed int x) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "closed").WithArguments("closed").WithLocation(3, 12),
                // (3,19): error CS1001: Identifier expected
                //     void M(closed int x) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(3, 19),
                // (3,19): error CS1003: Syntax error, ',' expected
                //     void M(closed int x) { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",").WithLocation(3, 19)
            ]);
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
                                N(SyntaxKind.IdentifierToken, "closed");
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
                void M(closed x) { }
            }
            """,
            expectedBindingDiagnostics: [
                // (3,12): error CS0246: The type or namespace name 'closed' could not be found (are you missing a using directive or an assembly reference?)
                //     void M(closed x) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "closed").WithArguments("closed").WithLocation(3, 12)
            ]);
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
                                N(SyntaxKind.IdentifierToken, "closed");
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
    public void TestClosedModifierAfterIncompleteBaseList1()
    {
        UsingTree("""
            class C : B
            closed class D
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
                N(SyntaxKind.ClosedKeyword);
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
    public void TestClosedModifierAfterIncompleteBaseList2()
    {
        UsingTree("""
            class C : B, closed
            {
            }

            class closed
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
                            N(SyntaxKind.IdentifierToken, "closed");
                        }
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "closed");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }
}
