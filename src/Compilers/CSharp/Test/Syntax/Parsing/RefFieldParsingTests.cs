// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class RefFieldParsingTests : ParsingTests
    {
        public RefFieldParsingTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersionFacts.CSharpNext)]
        public void FieldDeclaration_01(LanguageVersion languageVersion)
        {
            string source = "struct S { ref T F; }";
            UsingDeclaration(source, TestOptions.Regular.WithLanguageVersion(languageVersion));

            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "F");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersionFacts.CSharpNext)]
        public void FieldDeclaration_02(LanguageVersion languageVersion)
        {
            string source = "struct S { ref readonly T F; }";
            UsingDeclaration(source, TestOptions.Regular.WithLanguageVersion(languageVersion));

            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "F");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersionFacts.CSharpNext)]
        public void FieldDeclaration_03(LanguageVersion languageVersion)
        {
            string source = "struct S { out T F; }";
            UsingDeclaration(source, TestOptions.Regular.WithLanguageVersion(languageVersion),
                // (1,12): error CS1519: Invalid token 'out' in class, record, struct, or interface member declaration
                // struct S { out T F; }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "out").WithArguments("out").WithLocation(1, 12));

            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "F");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersionFacts.CSharpNext)]
        public void FieldDeclaration_04(LanguageVersion languageVersion)
        {
            string source = "struct S { in T F; }";
            UsingDeclaration(source, TestOptions.Regular.WithLanguageVersion(languageVersion),
                // (1,12): error CS1519: Invalid token 'in' in class, record, struct, or interface member declaration
                // struct S { in T F; }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "in").WithArguments("in").WithLocation(1, 12));

            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "F");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        // PROTOTYPE: Should ref expressions be supported in object initializers?
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersionFacts.CSharpNext)]
        public void ObjectInitializer(LanguageVersion languageVersion)
        {
            string source = "new S { F = ref t }";
            UsingExpression(source, TestOptions.Regular.WithLanguageVersion(languageVersion),
                // (1,13): error CS1525: Invalid expression term 'ref'
                // new S { F = ref t }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref t").WithArguments("ref").WithLocation(1, 13));

            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "S");
                }
                N(SyntaxKind.ObjectInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "F");
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.RefExpression);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "t");
                            }
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }
    }
}
