// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed partial class ModifierParserRecoveryTests
{
    [Fact]
    public void Ref_BeforeModifiersOnStruct()
    {
        const string source = "ref public readonly partial struct S { }";

        UsingTree(source);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.RefKeyword);
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.ReadOnlyKeyword);
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(source, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (1,1): error CS9327: Feature 'relaxed modifier ordering' is not available in C# 14.0. Please use language version 15.0 or greater.
            // ref public readonly partial struct S { }
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion14, "ref").WithArguments("relaxed modifier ordering", "15.0").WithLocation(1, 1));

        CreateCompilation(source, parseOptions: TestOptions.Regular15).VerifyDiagnostics();
    }

    [Theory]
    [InlineData("ref public struct S { }")]
    [InlineData("public ref readonly struct S { }")]
    [InlineData("partial ref readonly struct S { }")]
    public void Ref_NonCanonicalStructOrdering(string source)
    {
        CreateCompilation(source, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion14, "ref").WithArguments("relaxed modifier ordering", "15.0"));

        CreateCompilation(source, parseOptions: TestOptions.Regular15).VerifyDiagnostics();
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp7_2)]
    [InlineData(LanguageVersion.CSharp14)]
    [InlineData(LanguageVersion.CSharp15)]
    [InlineData(LanguageVersion.Preview)]
    public void Ref_CanonicalStructOrdering(LanguageVersion languageVersion)
    {
        const string source = """
            ref struct S1 { }
            public readonly ref struct S2 { }
            public ref partial struct S3 { }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics();
    }

    [Fact]
    public void Ref_MisplacedBeforeMemberModifiers()
    {
        const string source = "class C { ref public static int M() => 0; }";

        UsingTree(source);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
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

        CreateCompilation(source).VerifyDiagnostics(
            // (1,11): error CS1585: Member modifier 'ref' must precede the member type and name
            // class C { ref public static int M() => 0; }
            Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(1, 11));
    }

    [Fact]
    public void Ref_MisplacedBetweenMemberModifiers()
    {
        const string source = "class C { public ref static int M() => 0; }";

        UsingTree(source);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
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

        CreateCompilation(source).VerifyDiagnostics(
            // (1,18): error CS1585: Member modifier 'ref' must precede the member type and name
            // class C { public ref static int M() => 0; }
            Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(1, 18));
    }

    [Fact]
    public void RefReadonly_RemainsMemberReturnType()
    {
        const string source = "class C { public static ref readonly int M() => throw null; }";

        UsingTree(source);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
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

        CreateCompilation(source).VerifyDiagnostics();
    }
}
