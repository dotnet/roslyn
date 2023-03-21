// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class ExtensionParsingTests : ParsingTests
{
    private SyntaxTree UsingTreeWithCSharpNext(string text, params DiagnosticDescription[] expectedErrors)
        => UsingTree(text, TestOptions.RegularNext, expectedErrors);

    public ExtensionParsingTests(ITestOutputHelper output) : base(output) { }

    [Theory, CombinatorialData]
    public void ExtensionParsing(bool isExplicit)
    {
        Assert.True(SyntaxFacts.IsTypeDeclaration(SyntaxKind.ExtensionDeclaration));

        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }""";
        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "UnderlyingType");
                    }
                }
                N(SyntaxKind.BaseList);
                {
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.SimpleBaseType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "BaseExtension1");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.SimpleBaseType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "BaseExtension2");
                        }
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        UsingTree(text, options: TestOptions.Regular11,
            // (1,1): error CS1031: Type expected
            // explicit extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_TypeExpected, keyword).WithLocation(1, 1),
            // (1,1): error CS1003: Syntax error, 'operator' expected
            // explicit extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_SyntaxError, keyword).WithArguments("operator").WithLocation(1, 1),
            // (1,1): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
            // explicit extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_BadOperatorSyntax, keyword).WithArguments("+").WithLocation(1, 1),
            // (1,1): error CS1037: Overloadable operator expected
            // explicit extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_OvlOperatorExpected, keyword).WithLocation(1, 1),
            // (1,10): error CS1003: Syntax error, '(' expected
            // explicit extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "extension").WithArguments("(").WithLocation(1, 10),
            // (1,22): error CS1003: Syntax error, ',' expected
            // explicit extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "for").WithArguments(",").WithLocation(1, 22),
            // (1,26): error CS1003: Syntax error, ',' expected
            // explicit extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "UnderlyingType").WithArguments(",").WithLocation(1, 26),
            // (1,41): error CS1001: Identifier expected
            // explicit extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(1, 41),
            // (1,41): error CS1003: Syntax error, ',' expected
            // explicit extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 41),
            // (1,43): error CS1003: Syntax error, ',' expected
            // explicit extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "BaseExtension1").WithArguments(",").WithLocation(1, 43),
            // (1,57): error CS1001: Identifier expected
            // explicit extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ",").WithLocation(1, 57),
            // (1,74): error CS1001: Identifier expected
            // explicit extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(1, 74),
            // (1,74): error CS1003: Syntax error, ',' expected
            // explicit extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(1, 74),
            // (1,76): error CS1026: ) expected
            // explicit extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "}").WithLocation(1, 76),
            // (1,76): error CS1002: ; expected
            // explicit extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(1, 76),
            // (1,76): error CS1022: Type or namespace definition, or end-of-file expected
            // explicit extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(1, 76)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.OperatorDeclaration);
            {
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
                M(SyntaxKind.OperatorKeyword);
                M(SyntaxKind.PlusToken);
                N(SyntaxKind.ParameterList);
                {
                    M(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "extension");
                        }
                        N(SyntaxKind.IdentifierToken, "C");
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "UnderlyingType");
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "BaseExtension1");
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "BaseExtension2");
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void ExtensionParsing_WithoutImplicitOrExplicit()
    {
        Assert.True(SyntaxFacts.IsTypeDeclaration(SyntaxKind.ExtensionDeclaration));

        var text = $$"""extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }""";
        UsingTreeWithCSharpNext(text,
            // (1,13): error CS1003: Syntax error, ',' expected
            // extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "for").WithArguments(",").WithLocation(1, 13),
            // (1,32): error CS1002: ; expected
            // extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(1, 32),
            // (1,32): error CS1022: Type or namespace definition, or end-of-file expected
            // extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_EOFExpected, ":").WithLocation(1, 32),
            // (1,48): error CS1001: Identifier expected
            // extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ",").WithLocation(1, 48),
            // (1,65): error CS1003: Syntax error, ',' expected
            // extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(1, 65),
            // (1,67): error CS1002: ; expected
            // extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(1, 67),
            // (1,67): error CS1022: Type or namespace definition, or end-of-file expected
            // extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(1, 67)
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
                            N(SyntaxKind.IdentifierToken, "extension");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
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
                            N(SyntaxKind.IdentifierToken, "BaseExtension1");
                        }
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "BaseExtension2");
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        UsingTree(text, options: TestOptions.Regular11,
            // (1,13): error CS1003: Syntax error, ',' expected
            // extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "for").WithArguments(",").WithLocation(1, 13),
            // (1,32): error CS1002: ; expected
            // extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(1, 32),
            // (1,32): error CS1022: Type or namespace definition, or end-of-file expected
            // extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_EOFExpected, ":").WithLocation(1, 32),
            // (1,48): error CS1001: Identifier expected
            // extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ",").WithLocation(1, 48),
            // (1,65): error CS1003: Syntax error, ',' expected
            // extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(1, 65),
            // (1,67): error CS1002: ; expected
            // extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(1, 67),
            // (1,67): error CS1022: Type or namespace definition, or end-of-file expected
            // extension C for UnderlyingType : BaseExtension1, BaseExtension2 { }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(1, 67)
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
                            N(SyntaxKind.IdentifierToken, "extension");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
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
                            N(SyntaxKind.IdentifierToken, "BaseExtension1");
                        }
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "BaseExtension2");
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void WithPartial(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""partial {{keyword}} extension C for UnderlyingType { }""";
        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "UnderlyingType");
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        UsingTree(text, options: TestOptions.Regular11,
            // (1,1): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
            // partial explicit extension C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "partial").WithArguments("+").WithLocation(1, 1),
            // (1,9): error CS1003: Syntax error, 'operator' expected
            // partial explicit extension C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SyntaxError, keyword).WithArguments("operator").WithLocation(1, 9),
            // (1,9): error CS1020: Overloadable binary operator expected
            // partial explicit extension C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, keyword).WithLocation(1, 9),
            // (1,18): error CS1003: Syntax error, '(' expected
            // partial explicit extension C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "extension").WithArguments("(").WithLocation(1, 18),
            // (1,30): error CS1003: Syntax error, ',' expected
            // partial explicit extension C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "for").WithArguments(",").WithLocation(1, 30),
            // (1,34): error CS1003: Syntax error, ',' expected
            // partial explicit extension C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "UnderlyingType").WithArguments(",").WithLocation(1, 34),
            // (1,49): error CS1001: Identifier expected
            // partial explicit extension C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(1, 49),
            // (1,49): error CS1003: Syntax error, ',' expected
            // partial explicit extension C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(1, 49),
            // (1,51): error CS1026: ) expected
            // partial explicit extension C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "}").WithLocation(1, 51),
            // (1,51): error CS1002: ; expected
            // partial explicit extension C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(1, 51),
            // (1,51): error CS1022: Type or namespace definition, or end-of-file expected
            // partial explicit extension C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(1, 51)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.OperatorDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "partial");
                }
                M(SyntaxKind.OperatorKeyword);
                M(SyntaxKind.PlusToken);
                N(SyntaxKind.ParameterList);
                {
                    M(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "extension");
                        }
                        N(SyntaxKind.IdentifierToken, "C");
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "UnderlyingType");
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void WithPartial_WithoutImplicitOrExplicit()
    {
        var text = $$"""partial extension C for UnderlyingType { }""";
        UsingTreeWithCSharpNext(text,
            // (1,1): error CS1031: Type expected
            // partial extension C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_TypeExpected, "partial").WithLocation(1, 1),
            // (1,1): error CS1525: Invalid expression term 'partial'
            // partial extension C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(1, 1),
            // (1,1): error CS1003: Syntax error, ',' expected
            // partial extension C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "partial").WithArguments(",").WithLocation(1, 1),
            // (1,42): error CS1002: ; expected
            // partial extension C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(1, 42),
            // (1,42): error CS1022: Type or namespace definition, or end-of-file expected
            // partial extension C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(1, 42)
            );

        N(SyntaxKind.CompilationUnit);
        {
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
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        UsingTree(text, options: TestOptions.Regular11,
            // (1,1): error CS1031: Type expected
            // partial extension C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_TypeExpected, "partial").WithLocation(1, 1),
            // (1,1): error CS1525: Invalid expression term 'partial'
            // partial extension C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(1, 1),
            // (1,1): error CS1003: Syntax error, ',' expected
            // partial extension C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "partial").WithArguments(",").WithLocation(1, 1),
            // (1,42): error CS1002: ; expected
            // partial extension C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(1, 42),
            // (1,42): error CS1022: Type or namespace definition, or end-of-file expected
            // partial extension C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(1, 42)
            );

        N(SyntaxKind.CompilationUnit);
        {
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
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void WithReadonlyPartial(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""readonly partial {{keyword}} extension S for U { }""";
        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(SyntaxKind.ReadOnlyKeyword);
                N(SyntaxKind.PartialKeyword);
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "U");
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        UsingTree(text, options: TestOptions.Regular11,
            // (1,10): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
            // readonly partial explicit extension S for U { }
            Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "partial").WithArguments("+").WithLocation(1, 10),
            // (1,18): error CS1003: Syntax error, 'operator' expected
            // readonly partial explicit extension S for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, keyword).WithArguments("operator").WithLocation(1, 18),
            // (1,18): error CS1020: Overloadable binary operator expected
            // readonly partial explicit extension S for U { }
            Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, keyword).WithLocation(1, 18),
            // (1,27): error CS1003: Syntax error, '(' expected
            // readonly partial explicit extension S for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "extension").WithArguments("(").WithLocation(1, 27),
            // (1,39): error CS1003: Syntax error, ',' expected
            // readonly partial explicit extension S for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "for").WithArguments(",").WithLocation(1, 39),
            // (1,43): error CS1003: Syntax error, ',' expected
            // readonly partial explicit extension S for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "U").WithArguments(",").WithLocation(1, 43),
            // (1,45): error CS1001: Identifier expected
            // readonly partial explicit extension S for U { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(1, 45),
            // (1,45): error CS1003: Syntax error, ',' expected
            // readonly partial explicit extension S for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(1, 45),
            // (1,47): error CS1026: ) expected
            // readonly partial explicit extension S for U { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "}").WithLocation(1, 47),
            // (1,47): error CS1002: ; expected
            // readonly partial explicit extension S for U { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(1, 47),
            // (1,47): error CS1022: Type or namespace definition, or end-of-file expected
            // readonly partial explicit extension S for U { }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(1, 47)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.OperatorDeclaration);
            {
                N(SyntaxKind.ReadOnlyKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "partial");
                }
                M(SyntaxKind.OperatorKeyword);
                M(SyntaxKind.PlusToken);
                N(SyntaxKind.ParameterList);
                {
                    M(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "extension");
                        }
                        N(SyntaxKind.IdentifierToken, "S");
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "U");
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void WithPublicStatic(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""public static {{keyword}} extension C for UnderlyingType { }""";
        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.StaticKeyword);
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "UnderlyingType");
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void WithMembers(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension C for UnderlyingType { void M() { } }""";
        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "UnderlyingType");
                    }
                }
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
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void WithTypeParameters(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension C<T1, T2> for UnderlyingType { }""";
        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.TypeParameterList);
                {
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.TypeParameter);
                    {
                        N(SyntaxKind.IdentifierToken, "T1");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.TypeParameter);
                    {
                        N(SyntaxKind.IdentifierToken, "T2");
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "UnderlyingType");
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void WithTypeParameterAndConstraints(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension C<T> for UnderlyingType where T : class { }""";
        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.TypeParameterList);
                {
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.TypeParameter);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "UnderlyingType");
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
                    N(SyntaxKind.ClassConstraint);
                    {
                        N(SyntaxKind.ClassKeyword);
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void WithoutFor(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension C UnderlyingType { }""";

        UsingTreeWithCSharpNext(text,
            // (1,22): error CS1514: { expected
            // explicit extension C UnderlyingType { }
            Diagnostic(ErrorCode.ERR_LbraceExpected, "UnderlyingType").WithLocation(1, 22),
            // (1,22): error CS1513: } expected
            // explicit extension C UnderlyingType { }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "UnderlyingType").WithLocation(1, 22),
            // (1,22): error CS8803: Top-level statements must precede namespace and type declarations.
            // explicit extension C UnderlyingType { }
            Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "UnderlyingType { ").WithLocation(1, 22),
            // (1,37): error CS1001: Identifier expected
            // explicit extension C UnderlyingType { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(1, 37),
            // (1,37): error CS1003: Syntax error, ',' expected
            // explicit extension C UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(1, 37),
            // (1,39): error CS1002: ; expected
            // explicit extension C UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(1, 39),
            // (1,39): error CS1022: Type or namespace definition, or end-of-file expected
            // explicit extension C UnderlyingType { }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(1, 39)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "C");
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
                            N(SyntaxKind.IdentifierToken, "UnderlyingType");
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

    [Theory, CombinatorialData]
    public void WithoutUnderlyingType(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension C for { }""";

        UsingTreeWithCSharpNext(text,
            // (1,26): error CS1031: Type expected
            // explicit extension C for { }
            Diagnostic(ErrorCode.ERR_TypeExpected, "{").WithLocation(1, 26)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void WithoutForUnderlyingType(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension C { }""";

        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void WithParameterList(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension C() for UnderlyingType { }""";

        UsingTreeWithCSharpNext(text,
            // (1,21): error CS1514: { expected
            // implicit extension C() for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(1, 21),
            // (1,21): error CS1513: } expected
            // implicit extension C() for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(1, 21),
            // (1,21): error CS8803: Top-level statements must precede namespace and type declarations.
            // implicit extension C() for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "() ").WithLocation(1, 21),
            // (1,22): error CS1525: Invalid expression term ')'
            // implicit extension C() for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(1, 22),
            // (1,24): error CS1002: ; expected
            // implicit extension C() for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "for").WithLocation(1, 24),
            // (1,28): error CS1003: Syntax error, '(' expected
            // implicit extension C() for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "UnderlyingType").WithArguments("(").WithLocation(1, 28),
            // (1,43): error CS1002: ; expected
            // implicit extension C() for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(1, 43),
            // (1,43): error CS1525: Invalid expression term '{'
            // implicit extension C() for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(1, 43),
            // (1,43): error CS1002: ; expected
            // implicit extension C() for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(1, 43),
            // (1,43): error CS1026: ) expected
            // implicit extension C() for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "{").WithLocation(1, 43)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                M(SyntaxKind.OpenBraceToken);
                M(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.ParenthesizedExpression);
                    {
                        N(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ForStatement);
                {
                    N(SyntaxKind.ForKeyword);
                    M(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "UnderlyingType");
                    }
                    M(SyntaxKind.SemicolonToken);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                    M(SyntaxKind.CloseParenToken);
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

    [Theory, CombinatorialData]
    public void WithParameterList_OnUnderlyingType(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension C for UnderlyingType() { }""";

        UsingTreeWithCSharpNext(text,
            // (1,40): error CS1514: { expected
            // explicit extension C for UnderlyingType() { }
            Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(1, 40),
            // (1,40): error CS1513: } expected
            // explicit extension C for UnderlyingType() { }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(1, 40),
            // (1,40): error CS8803: Top-level statements must precede namespace and type declarations.
            // explicit extension C for UnderlyingType() { }
            Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "() ").WithLocation(1, 40),
            // (1,41): error CS1525: Invalid expression term ')'
            // explicit extension C for UnderlyingType() { }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(1, 41),
            // (1,43): error CS1002: ; expected
            // explicit extension C for UnderlyingType() { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(1, 43)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "UnderlyingType");
                    }
                }
                M(SyntaxKind.OpenBraceToken);
                M(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.ParenthesizedExpression);
                    {
                        N(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CloseParenToken);
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

    [Theory, CombinatorialData]
    public void WithParameterList_OnBaseExtension(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension C for UnderlyingType : BaseExtension() { }""";

        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "UnderlyingType");
                    }
                }
                N(SyntaxKind.BaseList);
                {
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.PrimaryConstructorBaseType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "BaseExtension");
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void WithParameterList_WithoutBody(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension C() for UnderlyingType;""";

        UsingTreeWithCSharpNext(text,
            // (1,21): error CS1514: { expected
            // explicit extension C() for UnderlyingType;
            Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(1, 21),
            // (1,21): error CS1513: } expected
            // explicit extension C() for UnderlyingType;
            Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(1, 21),
            // (1,21): error CS8803: Top-level statements must precede namespace and type declarations.
            // explicit extension C() for UnderlyingType;
            Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "() ").WithLocation(1, 21),
            // (1,22): error CS1525: Invalid expression term ')'
            // explicit extension C() for UnderlyingType;
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(1, 22),
            // (1,24): error CS1002: ; expected
            // explicit extension C() for UnderlyingType;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "for").WithLocation(1, 24),
            // (1,28): error CS1003: Syntax error, '(' expected
            // explicit extension C() for UnderlyingType;
            Diagnostic(ErrorCode.ERR_SyntaxError, "UnderlyingType").WithArguments("(").WithLocation(1, 28),
            // (1,43): error CS1733: Expected expression
            // explicit extension C() for UnderlyingType;
            Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 43),
            // (1,43): error CS1002: ; expected
            // explicit extension C() for UnderlyingType;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 43),
            // (1,43): error CS1026: ) expected
            // explicit extension C() for UnderlyingType;
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 43),
            // (1,43): error CS1733: Expected expression
            // explicit extension C() for UnderlyingType;
            Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 43),
            // (1,43): error CS1002: ; expected
            // explicit extension C() for UnderlyingType;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 43)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                M(SyntaxKind.OpenBraceToken);
                M(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.ParenthesizedExpression);
                    {
                        N(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ForStatement);
                {
                    N(SyntaxKind.ForKeyword);
                    M(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "UnderlyingType");
                    }
                    N(SyntaxKind.SemicolonToken);
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
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void WithParameterList_WithOneParameter(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension C(int i) for UnderlyingType { }""";

        UsingTreeWithCSharpNext(text,
            // (1,21): error CS1514: { expected
            // explicit extension C(int i) for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(1, 21),
            // (1,21): error CS1513: } expected
            // explicit extension C(int i) for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(1, 21),
            // (1,21): error CS8803: Top-level statements must precede namespace and type declarations.
            // explicit extension C(int i) for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "(int ").WithLocation(1, 21),
            // (1,22): error CS1525: Invalid expression term 'int'
            // explicit extension C(int i) for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(1, 22),
            // (1,26): error CS1026: ) expected
            // explicit extension C(int i) for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "i").WithLocation(1, 26),
            // (1,26): error CS1002: ; expected
            // explicit extension C(int i) for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "i").WithLocation(1, 26),
            // (1,27): error CS1001: Identifier expected
            // explicit extension C(int i) for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(1, 27),
            // (1,27): error CS1002: ; expected
            // explicit extension C(int i) for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(1, 27),
            // (1,27): error CS1022: Type or namespace definition, or end-of-file expected
            // explicit extension C(int i) for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_EOFExpected, ")").WithLocation(1, 27),
            // (1,33): error CS1003: Syntax error, '(' expected
            // explicit extension C(int i) for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "UnderlyingType").WithArguments("(").WithLocation(1, 33),
            // (1,48): error CS1002: ; expected
            // explicit extension C(int i) for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(1, 48),
            // (1,48): error CS1525: Invalid expression term '{'
            // explicit extension C(int i) for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(1, 48),
            // (1,48): error CS1002: ; expected
            // explicit extension C(int i) for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(1, 48),
            // (1,48): error CS1026: ) expected
            // explicit extension C(int i) for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "{").WithLocation(1, 48)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                M(SyntaxKind.OpenBraceToken);
                M(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.ParenthesizedExpression);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        M(SyntaxKind.CloseParenToken);
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
                            N(SyntaxKind.IdentifierToken, "i");
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
                N(SyntaxKind.ForStatement);
                {
                    N(SyntaxKind.ForKeyword);
                    M(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "UnderlyingType");
                    }
                    M(SyntaxKind.SemicolonToken);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                    M(SyntaxKind.CloseParenToken);
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

    [Theory, CombinatorialData]
    public void WithoutBody(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension C for UnderlyingType;""";
        UsingTreeWithCSharpNext(text,
            // (1,40): error CS1514: { expected
            // explicit extension C for UnderlyingType;
            Diagnostic(ErrorCode.ERR_LbraceExpected, ";").WithLocation(1, 40),
            // (1,40): error CS1513: } expected
            // explicit extension C for UnderlyingType;
            Diagnostic(ErrorCode.ERR_RbraceExpected, ";").WithLocation(1, 40)
            );
        // PROTOTYPE should parse

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "UnderlyingType");
                    }
                }
                M(SyntaxKind.OpenBraceToken);
                M(SyntaxKind.CloseBraceToken);
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void NewModifier_TopLevel(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        UsingTreeWithCSharpNext($$"""new {{keyword}} extension C for U { }""",
            // (1,5): error CS1526: A new expression requires an argument list or (), [], or {} after type
            // new explicit extension C for U { }
            Diagnostic(ErrorCode.ERR_BadNewExpr, keyword).WithLocation(1, 5),
            // (1,5): error CS1002: ; expected
            // new explicit extension C for U { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, keyword).WithLocation(1, 5)
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
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "U");
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void NewModifier_Nested(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        UsingTreeWithCSharpNext($$"""class C { new {{keyword}} extension C for U { } }""");

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.ForType);
                    {
                        N(SyntaxKind.ForKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "U");
                        }
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

    [Theory, CombinatorialData]
    public void NewModifier_ReverseOrder(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        UsingTreeWithCSharpNext($$"""{{keyword}} new extension C for U { }""",
            // (1,10): error CS1003: Syntax error, 'operator' expected
            // implicit new extension C for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "new").WithArguments("operator").WithLocation(1, 10),
            // (1,10): error CS1031: Type expected
            // implicit new extension C for U { }
            Diagnostic(ErrorCode.ERR_TypeExpected, "new").WithLocation(1, 10),
            // (1,10): error CS1003: Syntax error, '(' expected
            // implicit new extension C for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "new").WithArguments("(").WithLocation(1, 10),
            // (1,10): error CS1026: ) expected
            // implicit new extension C for U { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "new").WithLocation(1, 10),
            // (1,10): error CS1002: ; expected
            // implicit new extension C for U { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "new").WithLocation(1, 10),
            // (1,26): error CS1002: ; expected
            // implicit new extension C for U { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "for").WithLocation(1, 26),
            // (1,30): error CS1003: Syntax error, '(' expected
            // implicit new extension C for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "U").WithArguments("(").WithLocation(1, 30),
            // (1,32): error CS1002: ; expected
            // implicit new extension C for U { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(1, 32),
            // (1,32): error CS1525: Invalid expression term '{'
            // implicit new extension C for U { }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(1, 32),
            // (1,32): error CS1002: ; expected
            // implicit new extension C for U { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(1, 32),
            // (1,32): error CS1026: ) expected
            // implicit new extension C for U { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "{").WithLocation(1, 32)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ConversionOperatorDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                M(SyntaxKind.OperatorKeyword);
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
                M(SyntaxKind.ParameterList);
                {
                    M(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "extension");
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "C");
                    }
                }
                M(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ForStatement);
                {
                    N(SyntaxKind.ForKeyword);
                    M(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "U");
                    }
                    M(SyntaxKind.SemicolonToken);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                    M(SyntaxKind.CloseParenToken);
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

    [Theory, CombinatorialData]
    public void FileModifier(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        UsingTreeWithCSharpNext($$"""file {{keyword}} extension C for UnderlyingType { }""");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(SyntaxKind.FileKeyword);
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "UnderlyingType");
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void FileModifier_WithPublic(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        UsingTreeWithCSharpNext($$"""public file {{keyword}} extension C for UnderlyingType { }""");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.FileKeyword);
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "UnderlyingType");
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void AfterIncompleteUsing(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""
using
{{keyword}} extension R for U { }
""";

        UsingTreeWithCSharpNext(text,
            // (1,6): error CS1041: Identifier expected; 'explicit' is a keyword
            // using
            Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "").WithArguments("", keyword).WithLocation(1, 6)
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
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "R");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "U");
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        UsingTree(text, options: TestOptions.Regular11,
            // (1,6): error CS1041: Identifier expected; 'explicit' is a keyword
            // using
            Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "").WithArguments("", keyword).WithLocation(1, 6),
            // (1,6): error CS1031: Type expected
            // using
            Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(1, 6),
            // (1,6): error CS1003: Syntax error, 'operator' expected
            // using
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("operator").WithLocation(1, 6),
            // (1,6): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
            // using
            Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "").WithArguments("+").WithLocation(1, 6),
            // (1,6): error CS1020: Overloadable binary operator expected
            // using
            Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, "").WithLocation(1, 6),
            // (2,10): error CS1003: Syntax error, '(' expected
            // explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "extension").WithArguments("(").WithLocation(2, 10),
            // (2,22): error CS1003: Syntax error, ',' expected
            // explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "for").WithArguments(",").WithLocation(2, 22),
            // (2,26): error CS1003: Syntax error, ',' expected
            // explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "U").WithArguments(",").WithLocation(2, 26),
            // (2,28): error CS1001: Identifier expected
            // explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(2, 28),
            // (2,28): error CS1003: Syntax error, ',' expected
            // explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(2, 28),
            // (2,30): error CS1026: ) expected
            // explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "}").WithLocation(2, 30),
            // (2,30): error CS1002: ; expected
            // explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(2, 30),
            // (2,30): error CS1022: Type or namespace definition, or end-of-file expected
            // explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(2, 30)
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
            N(SyntaxKind.OperatorDeclaration);
            {
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
                M(SyntaxKind.OperatorKeyword);
                M(SyntaxKind.PlusToken);
                N(SyntaxKind.ParameterList);
                {
                    M(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "extension");
                        }
                        N(SyntaxKind.IdentifierToken, "R");
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "U");
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void AfterIncompleteUsing_MissingSemiColon(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""
using Type
{{keyword}} extension R for U { }
""";

        UsingTreeWithCSharpNext(text,
            // (1,11): error CS1002: ; expected
            // using Type
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 11)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Type");
                }
                M(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "R");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "U");
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        UsingTree(text, options: TestOptions.Regular11,
            // (1,11): error CS1002: ; expected
            // using Type
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 11),
            // (1,11): error CS1031: Type expected
            // using Type
            Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(1, 11),
            // (1,11): error CS1003: Syntax error, 'operator' expected
            // using Type
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("operator").WithLocation(1, 11),
            // (1,11): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
            // using Type
            Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "").WithArguments("+").WithLocation(1, 11),
            // (1,11): error CS1020: Overloadable binary operator expected
            // using Type
            Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, "").WithLocation(1, 11),
            // (2,10): error CS1003: Syntax error, '(' expected
            // explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "extension").WithArguments("(").WithLocation(2, 10),
            // (2,22): error CS1003: Syntax error, ',' expected
            // explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "for").WithArguments(",").WithLocation(2, 22),
            // (2,26): error CS1003: Syntax error, ',' expected
            // explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "U").WithArguments(",").WithLocation(2, 26),
            // (2,28): error CS1001: Identifier expected
            // explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(2, 28),
            // (2,28): error CS1003: Syntax error, ',' expected
            // explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(2, 28),
            // (2,30): error CS1026: ) expected
            // explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "}").WithLocation(2, 30),
            // (2,30): error CS1002: ; expected
            // explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(2, 30),
            // (2,30): error CS1022: Type or namespace definition, or end-of-file expected
            // explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(2, 30)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Type");
                }
                M(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.OperatorDeclaration);
            {
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
                M(SyntaxKind.OperatorKeyword);
                M(SyntaxKind.PlusToken);
                N(SyntaxKind.ParameterList);
                {
                    M(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "extension");
                        }
                        N(SyntaxKind.IdentifierToken, "R");
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "U");
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void AfterIncompleteUsing_MissingSemiColon_WithPartial(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""
using Type
partial {{keyword}} extension R for U { }
""";

        UsingTreeWithCSharpNext(text,
            // (1,11): error CS1525: Invalid expression term 'partial'
            // using Type
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("partial").WithLocation(1, 11),
            // (1,11): error CS1002: ; expected
            // using Type
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 11)
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
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "R");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "U");
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        UsingTree(text, options: TestOptions.Regular11,
            // (2,9): error CS1002: ; expected
            // partial explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, keyword).WithLocation(2, 9),
            // (2,9): error CS1031: Type expected
            // partial explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_TypeExpected, keyword).WithLocation(2, 9),
            // (2,9): error CS1003: Syntax error, 'operator' expected
            // partial explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, keyword).WithArguments("operator").WithLocation(2, 9),
            // (2,9): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
            // partial explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_BadOperatorSyntax, keyword).WithArguments("+").WithLocation(2, 9),
            // (2,9): error CS1020: Overloadable binary operator expected
            // partial explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, keyword).WithLocation(2, 9),
            // (2,18): error CS1003: Syntax error, '(' expected
            // partial explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "extension").WithArguments("(").WithLocation(2, 18),
            // (2,30): error CS1003: Syntax error, ',' expected
            // partial explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "for").WithArguments(",").WithLocation(2, 30),
            // (2,34): error CS1003: Syntax error, ',' expected
            // partial explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "U").WithArguments(",").WithLocation(2, 34),
            // (2,36): error CS1001: Identifier expected
            // partial explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(2, 36),
            // (2,36): error CS1003: Syntax error, ',' expected
            // partial explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(2, 36),
            // (2,38): error CS1026: ) expected
            // partial explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "}").WithLocation(2, 38),
            // (2,38): error CS1002: ; expected
            // partial explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(2, 38),
            // (2,38): error CS1022: Type or namespace definition, or end-of-file expected
            // partial explicit extension R for U { }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(2, 38)
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
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.OperatorDeclaration);
            {
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
                M(SyntaxKind.OperatorKeyword);
                M(SyntaxKind.PlusToken);
                N(SyntaxKind.ParameterList);
                {
                    M(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "extension");
                        }
                        N(SyntaxKind.IdentifierToken, "R");
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "U");
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void PartialInIncompleteNamespace()
    {
        var text = """
namespace N
partial explicit extension S for U { }
""";
        UsingTreeWithCSharpNext(text,
            // (1,12): error CS1514: { expected
            // namespace N
            Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(1, 12),
            // (2,39): error CS1513: } expected
            // explicit partial extension S for U { }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(2, 39)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.NamespaceDeclaration);
            {
                N(SyntaxKind.NamespaceKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "N");
                }
                M(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.ExplicitKeyword);
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.IdentifierToken, "S");
                    N(SyntaxKind.ForType);
                    {
                        N(SyntaxKind.ForKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "U");
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                M(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void PartialInIncompleteNamespace_ExplicitBeforePartial()
    {
        var text = """
namespace N
explicit partial extension S for U { }
""";
        UsingTreeWithCSharpNext(text,
            // (1,12): error CS1514: { expected
            // namespace N
            Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(1, 12),
            // (2,10): error CS1003: Syntax error, 'operator' expected
            // explicit partial extension S for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "partial").WithArguments("operator").WithLocation(2, 10),
            // (2,10): error CS1031: Type expected
            // explicit partial extension S for U { }
            Diagnostic(ErrorCode.ERR_TypeExpected, "partial").WithLocation(2, 10),
            // (2,10): error CS1003: Syntax error, '(' expected
            // explicit partial extension S for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "partial").WithArguments("(").WithLocation(2, 10),
            // (2,30): error CS1003: Syntax error, ',' expected
            // explicit partial extension S for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "for").WithArguments(",").WithLocation(2, 30),
            // (2,34): error CS1003: Syntax error, ',' expected
            // explicit partial extension S for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "U").WithArguments(",").WithLocation(2, 34),
            // (2,36): error CS1001: Identifier expected
            // explicit partial extension S for U { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(2, 36),
            // (2,36): error CS1003: Syntax error, ',' expected
            // explicit partial extension S for U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(2, 36),
            // (2,38): error CS1026: ) expected
            // explicit partial extension S for U { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "}").WithLocation(2, 38),
            // (2,38): error CS1002: ; expected
            // explicit partial extension S for U { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(2, 38)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.NamespaceDeclaration);
            {
                N(SyntaxKind.NamespaceKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "N");
                }
                M(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.ExplicitKeyword);
                    M(SyntaxKind.OperatorKeyword);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.ParameterList);
                    {
                        M(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "extension");
                            }
                            N(SyntaxKind.IdentifierToken, "S");
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "U");
                            }
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void InIncompleteNamespace()
    {
        var text = """
namespace N
explicit extension S for U { }
""";
        UsingTreeWithCSharpNext(text,
            // (1,12): error CS1514: { expected
            // namespace N
            Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(1, 12),
            // (2,31): error CS1513: } expected
            // explicit extension S for U { }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(2, 31)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.NamespaceDeclaration);
            {
                N(SyntaxKind.NamespaceKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "N");
                }
                M(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionDeclaration);
                {
                    N(SyntaxKind.ExplicitKeyword);
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.IdentifierToken, "S");
                    N(SyntaxKind.ForType);
                    {
                        N(SyntaxKind.ForKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "U");
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                M(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void BeforeTopLevelStatement(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""
{{keyword}} extension R for U { }
Write();
""";

        UsingTreeWithCSharpNext(text,
            // (2,1): error CS8803: Top-level statements must precede namespace and type declarations.
            // Write();
            Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "Write();").WithLocation(2, 1)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "R");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "U");
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Write");
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

    [Theory, CombinatorialData]
    public void WithoutBody_WithoutUnderlyingType(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension C;""";
        UsingTreeWithCSharpNext(text,
            // (1,21): error CS1514: { expected
            // explicit extension C;
            Diagnostic(ErrorCode.ERR_LbraceExpected, ";").WithLocation(1, 21),
            // (1,21): error CS1513: } expected
            // explicit extension C;
            Diagnostic(ErrorCode.ERR_RbraceExpected, ";").WithLocation(1, 21)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                M(SyntaxKind.OpenBraceToken);
                M(SyntaxKind.CloseBraceToken);
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void WithoutBody_WithoutBaseList_WithTypeParameter(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension C<T> for UnderlyingType;""";
        UsingTreeWithCSharpNext(text,
            // (1,43): error CS1514: { expected
            // explicit extension C<T> for UnderlyingType;
            Diagnostic(ErrorCode.ERR_LbraceExpected, ";").WithLocation(1, 43),
            // (1,43): error CS1513: } expected
            // explicit extension C<T> for UnderlyingType;
            Diagnostic(ErrorCode.ERR_RbraceExpected, ";").WithLocation(1, 43)
            );
        // PROTOTYPE should parse

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.TypeParameterList);
                {
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.TypeParameter);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "UnderlyingType");
                    }
                }
                M(SyntaxKind.OpenBraceToken);
                M(SyntaxKind.CloseBraceToken);
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void SyntaxFactory_TypeDeclaration()
    {
        var expected = """
explicit extension X
{
}
""";
        AssertEx.AssertEqualToleratingWhitespaceDifferences(expected,
            SyntaxFactory.TypeDeclaration(SyntaxKind.ExtensionDeclaration, "X").NormalizeWhitespace().ToString());
    }

    [Fact]
    public void SyntaxFacts_GetText()
    {
        Assert.Equal("extension", SyntaxFacts.GetText(SyntaxKind.ExtensionKeyword));
    }

    [Fact]
    public void ClassWithFor()
    {
        var text = $$"""class C for UnderlyingType { }""";
        UsingTreeWithCSharpNext(text,
            // (1,9): error CS1514: { expected
            // class C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_LbraceExpected, "for").WithLocation(1, 9),
            // (1,9): error CS1513: } expected
            // class C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "for").WithLocation(1, 9),
            // (1,9): error CS8803: Top-level statements must precede namespace and type declarations.
            // class C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "for UnderlyingType { }").WithLocation(1, 9),
            // (1,13): error CS1003: Syntax error, '(' expected
            // class C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "UnderlyingType").WithArguments("(").WithLocation(1, 13),
            // (1,28): error CS1002: ; expected
            // class C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(1, 28),
            // (1,28): error CS1525: Invalid expression term '{'
            // class C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(1, 28),
            // (1,28): error CS1002: ; expected
            // class C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(1, 28),
            // (1,28): error CS1026: ) expected
            // class C for UnderlyingType { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "{").WithLocation(1, 28)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                M(SyntaxKind.OpenBraceToken);
                M(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ForStatement);
                {
                    N(SyntaxKind.ForKeyword);
                    M(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "UnderlyingType");
                    }
                    M(SyntaxKind.SemicolonToken);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                    M(SyntaxKind.CloseParenToken);
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

    [Theory, CombinatorialData]
    public void ClassWithExplicit(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} class C { }""";
        UsingTreeWithCSharpNext(text,
            // (1,10): error CS1003: Syntax error, 'operator' expected
            // explicit class C { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "class").WithArguments("operator").WithLocation(1, 10),
            // (1,10): error CS1031: Type expected
            // explicit class C { }
            Diagnostic(ErrorCode.ERR_TypeExpected, "class").WithLocation(1, 10),
            // (1,10): error CS1003: Syntax error, '(' expected
            // explicit class C { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "class").WithArguments("(").WithLocation(1, 10),
            // (1,10): error CS1026: ) expected
            // explicit class C { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "class").WithLocation(1, 10),
            // (1,10): error CS1002: ; expected
            // explicit class C { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "class").WithLocation(1, 10)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ConversionOperatorDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                M(SyntaxKind.OperatorKeyword);
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
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

    [Theory, CombinatorialData]
    public void WithTupleUnderlyingType(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension X for (int, int) { }""";
        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "X");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
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
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void WithPointerUnderlyingType(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension X for int* { }""";
        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "X");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.PointerType);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.AsteriskToken);
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void WithArrayUnderlyingType(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension X for int[] { }""";
        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "X");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
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
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void WithRefTypeUnderlyingType(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension X for ref int { }""";
        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "X");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void WithScopedTypeUnderlyingType(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension X for scoped int { }""";
        UsingTreeWithCSharpNext(text,
            // (1,33): error CS1514: { expected
            // explicit extension X for scoped int { }
            Diagnostic(ErrorCode.ERR_LbraceExpected, "int").WithLocation(1, 33),
            // (1,33): error CS1513: } expected
            // explicit extension X for scoped int { }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "int").WithLocation(1, 33),
            // (1,33): error CS8803: Top-level statements must precede namespace and type declarations.
            // explicit extension X for scoped int { }
            Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "int { ").WithLocation(1, 33),
            // (1,37): error CS1001: Identifier expected
            // explicit extension X for scoped int { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(1, 37),
            // (1,37): error CS1003: Syntax error, ',' expected
            // explicit extension X for scoped int { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(1, 37),
            // (1,39): error CS1002: ; expected
            // explicit extension X for scoped int { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(1, 39),
            // (1,39): error CS1022: Type or namespace definition, or end-of-file expected
            // explicit extension X for scoped int { }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(1, 39)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "X");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "scoped");
                    }
                }
                M(SyntaxKind.OpenBraceToken);
                M(SyntaxKind.CloseBraceToken);
            }
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

    [Theory, CombinatorialData]
    public void WithFunctionPointerUnderlyingType(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension X for delegate*<void> { }""";
        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "X");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
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
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void WithFunctionPointerUnderlyingType_WithBaseList(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension X for delegate*<void> : Base1, Base2 { }""";
        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "X");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
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
                }
                N(SyntaxKind.BaseList);
                {
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.SimpleBaseType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Base1");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.SimpleBaseType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Base2");
                        }
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void WithFunctionPointerUnderlyingType_WithConstraints(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension X<T> for delegate*<void> where T : class { }""";
        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "X");
                N(SyntaxKind.TypeParameterList);
                {
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.TypeParameter);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
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
                }
                N(SyntaxKind.TypeParameterConstraintClause);
                {
                    N(SyntaxKind.WhereKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.ClassConstraint);
                    {
                        N(SyntaxKind.ClassKeyword);
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void WithMember_Constructor(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension X for int { X() { } }""";
        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "X");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                }
                N(SyntaxKind.OpenBraceToken);
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
    public void WithMember_Constructor_WithoutImplicitOrExplicit()
    {
        var text = $$"""extension X for int { X() { } }""";
        UsingTreeWithCSharpNext(text,
            // (1,13): error CS1003: Syntax error, ',' expected
            // extension X for int { X() { } }
            Diagnostic(ErrorCode.ERR_SyntaxError, "for").WithArguments(",").WithLocation(1, 13),
            // (1,25): error CS1002: ; expected
            // extension X for int { X() { } }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(1, 25),
            // (1,25): error CS1022: Type or namespace definition, or end-of-file expected
            // extension X for int { X() { } }
            Diagnostic(ErrorCode.ERR_EOFExpected, ")").WithLocation(1, 25),
            // (1,31): error CS1022: Type or namespace definition, or end-of-file expected
            // extension X for int { X() { } }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(1, 31)
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
                            N(SyntaxKind.IdentifierToken, "extension");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "X");
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

    [Theory, CombinatorialData]
    public void WithMember_Const(bool isExplicit)
    {
        var keyword = isExplicit ? "explicit" : "implicit";
        var text = $$"""{{keyword}} extension X for int { const int Y = 0; }""";
        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(isExplicit ? SyntaxKind.ExplicitKeyword : SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "X");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.ConstKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "Y");
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
    public void WithMember_Const_WithoutImplicitOrExplicit()
    {
        var text = $$"""extension X for int { const int Y = 0; }""";
        UsingTreeWithCSharpNext(text,
            // (1,13): error CS1003: Syntax error, ',' expected
            // extension X for int { const int Y = 0; }
            Diagnostic(ErrorCode.ERR_SyntaxError, "for").WithArguments(",").WithLocation(1, 13),
            // (1,40): error CS1022: Type or namespace definition, or end-of-file expected
            // extension X for int { const int Y = 0; }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(1, 40)
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
                            N(SyntaxKind.IdentifierToken, "extension");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "X");
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
    public void ForAfterBaseList()
    {
        var text = """explicit extension X : X1 for int { }""";
        UsingTreeWithCSharpNext(text,
            // (1,27): error CS1003: Syntax error, ',' expected
            // explicit extension X : X1 for int
            Diagnostic(ErrorCode.ERR_SyntaxError, "for").WithArguments(",").WithLocation(1, 27)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(SyntaxKind.ExplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "X");
                N(SyntaxKind.BaseList);
                {
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.SimpleBaseType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "X1");
                        }
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void ExtensionNamedExtension()
    {
        var text = """explicit extension extension for int { }""";
        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(SyntaxKind.ExplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "extension");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void ExplicitInterfaceOperators()
    {
        var text = """
class C : extension
{
    static extension extension.operator+(extension one, extension other) => throw null;
}
""";
        UsingTreeWithCSharpNext(text);

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
                            N(SyntaxKind.IdentifierToken, "extension");
                        }
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.OperatorDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "extension");
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "extension");
                        }
                        N(SyntaxKind.DotToken);
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
                                N(SyntaxKind.IdentifierToken, "extension");
                            }
                            N(SyntaxKind.IdentifierToken, "one");
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "extension");
                            }
                            N(SyntaxKind.IdentifierToken, "other");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.ThrowExpression);
                        {
                            N(SyntaxKind.ThrowKeyword);
                            N(SyntaxKind.NullLiteralExpression);
                            {
                                N(SyntaxKind.NullKeyword);
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
    public void ExplicitInterfaceOperators_FromNamespace()
    {
        var text = """
class C : extension.I
{
    static extension.I extension.I.operator+(extension.I one, extension.I other) => throw null;
}
""";
        UsingTreeWithCSharpNext(text);

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
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "extension");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                            }
                        }
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.OperatorDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "extension");
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "I");
                        }
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "extension");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                            }
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PlusToken);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "extension");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "I");
                                }
                            }
                            N(SyntaxKind.IdentifierToken, "one");
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "extension");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "I");
                                }
                            }
                            N(SyntaxKind.IdentifierToken, "other");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.ThrowExpression);
                        {
                            N(SyntaxKind.ThrowKeyword);
                            N(SyntaxKind.NullLiteralExpression);
                            {
                                N(SyntaxKind.NullKeyword);
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
    public void ExplicitConversionOperator_FromExtensionType()
    {
        var text = """
class C
{
    public static explicit operator C(extension i) { throw null; }
}
""";
        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.ExplicitKeyword);
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "C");
                    }
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "extension");
                            }
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.ThrowStatement);
                        {
                            N(SyntaxKind.ThrowKeyword);
                            N(SyntaxKind.NullLiteralExpression);
                            {
                                N(SyntaxKind.NullKeyword);
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
    public void ExplicitConversionOperator_ToExtensionType()
    {
        var text = """
class extension
{
    public static explicit operator extension(int i) { throw null; }
}
""";
        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "extension");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.ExplicitKeyword);
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "extension");
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
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.ThrowStatement);
                        {
                            N(SyntaxKind.ThrowKeyword);
                            N(SyntaxKind.NullLiteralExpression);
                            {
                                N(SyntaxKind.NullKeyword);
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
    public void ExplicitConversionOperator_ExplicitImplementationInExtensionNamespace()
    {
        var text = """
class C : I<C>
{
    static explicit extension.I<C>.operator int(C i)
    {
        return 1;
    }
}
""";
        // PROTOTYPE consider avoiding this break (in following context)
        // namespace extension;
        // public interface I<T> where T : I<T>
        // {
        //    public abstract static explicit operator int(T i);
        // }
        UsingTreeWithCSharpNext(text,
            // (3,30): error CS1001: Identifier expected
            //     static explicit extension.I<C>.operator int(C i)
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ".").WithLocation(3, 30),
            // (3,30): error CS1514: { expected
            //     static explicit extension.I<C>.operator int(C i)
            Diagnostic(ErrorCode.ERR_LbraceExpected, ".").WithLocation(3, 30),
            // (3,30): error CS1513: } expected
            //     static explicit extension.I<C>.operator int(C i)
            Diagnostic(ErrorCode.ERR_RbraceExpected, ".").WithLocation(3, 30),
            // (3,30): error CS1519: Invalid token '.' in class, record, struct, or interface member declaration
            //     static explicit extension.I<C>.operator int(C i)
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ".").WithArguments(".").WithLocation(3, 30),
            // (3,31): error CS1003: Syntax error, 'explicit' expected
            //     static explicit extension.I<C>.operator int(C i)
            Diagnostic(ErrorCode.ERR_SyntaxError, "I").WithArguments("explicit").WithLocation(3, 31)
            );

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
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "I");
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.ExplicitKeyword);
                    N(SyntaxKind.ExtensionKeyword);
                    M(SyntaxKind.IdentifierToken);
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    M(SyntaxKind.ExplicitKeyword);
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "I");
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
                            }
                            N(SyntaxKind.IdentifierToken, "i");
                        }
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
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void ExplicitConversionOperator_ExplicitImplementationOfExtensionInterface()
    {
        var text = """
class C : extension<C>
{
    static explicit extension<C>.operator int(C i)
    {
        return 1;
    }
}
""";
        // PROTOTYPE consider avoiding this break (in following context)
        // public interface extension<T> where T : extension<T>
        // {
        //    public abstract static explicit operator int(T i);
        // }
        UsingTreeWithCSharpNext(text,
            // (3,30): error CS1001: Identifier expected
            //     static explicit extension<C>.operator int(C i)
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "<").WithLocation(3, 30),
            // (3,33): error CS1514: { expected
            //     static explicit extension<C>.operator int(C i)
            Diagnostic(ErrorCode.ERR_LbraceExpected, ".").WithLocation(3, 33),
            // (3,33): error CS1513: } expected
            //     static explicit extension<C>.operator int(C i)
            Diagnostic(ErrorCode.ERR_RbraceExpected, ".").WithLocation(3, 33),
            // (3,33): error CS1519: Invalid token '.' in class, record, struct, or interface member declaration
            //     static explicit extension<C>.operator int(C i)
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ".").WithArguments(".").WithLocation(3, 33),
            // (3,46): error CS1519: Invalid token '(' in class, record, struct, or interface member declaration
            //     static explicit extension<C>.operator int(C i)
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "(").WithArguments("(").WithLocation(3, 46),
            // (3,50): error CS8124: Tuple must contain at least two elements.
            //     static explicit extension<C>.operator int(C i)
            Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(3, 50),
            // (4,5): error CS1519: Invalid token '{' in class, record, struct, or interface member declaration
            //     {
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(4, 5),
            // (7,1): error CS1022: Type or namespace definition, or end-of-file expected
            // }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(7, 1)
            );

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
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "extension");
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.ExplicitKeyword);
                    N(SyntaxKind.ExtensionKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                }
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.TupleType);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
                            }
                            N(SyntaxKind.IdentifierToken, "i");
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
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void ForDynamic()
    {
        var text = """explicit extension X for dynamic { }""";
        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(SyntaxKind.ExplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "X");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "dynamic");
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void ForVar()
    {
        var text = """explicit extension X for var { }""";
        UsingTreeWithCSharpNext(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ExtensionDeclaration);
            {
                N(SyntaxKind.ExplicitKeyword);
                N(SyntaxKind.ExtensionKeyword);
                N(SyntaxKind.IdentifierToken, "X");
                N(SyntaxKind.ForType);
                {
                    N(SyntaxKind.ForKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "var");
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }
}
