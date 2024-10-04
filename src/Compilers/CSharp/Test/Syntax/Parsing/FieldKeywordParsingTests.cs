// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class FieldKeywordParsingTests : ParsingTests
    {
        public FieldKeywordParsingTests(ITestOutputHelper output) : base(output)
        {
        }

        private static bool IsParsedAsToken(LanguageVersion languageVersion, bool escapeIdentifier)
        {
            return !escapeIdentifier && languageVersion > LanguageVersion.CSharp13;
        }

        private void IdentifierNameOrFieldExpression(LanguageVersion languageVersion, bool escapeIdentifier)
        {
            IdentifierNameOrFieldExpression(languageVersion, escapeIdentifier, IsParsedAsToken(languageVersion, escapeIdentifier));
        }

        private void IdentifierNameOrFieldExpression(LanguageVersion languageVersion, bool escapeIdentifier, bool isParsedAsToken)
        {
            if (isParsedAsToken)
            {
                N(SyntaxKind.FieldExpression);
                {
                    N(SyntaxKind.FieldKeyword);
                }
            }
            else
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, GetFieldIdentifier(escapeIdentifier));
                }
            }
        }

        private static string GetFieldIdentifier(bool escapeIdentifier)
        {
            return escapeIdentifier ? "@field" : "field";
        }

        [Theory]
        [CombinatorialData]
        public void Property_Initializer(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            UsingTree($$"""
                class C
                {
                    object P { get; set; } = field;
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
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
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "field");
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

        [Theory]
        [CombinatorialData]
        public void Property_ExpressionBody(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            bool expectedParsedAsToken = languageVersion > LanguageVersion.CSharp13;
            UsingTree($$"""
                class C
                {
                    object P => field;
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            IdentifierNameOrFieldExpression(languageVersion, escapeIdentifier: false, expectedParsedAsToken);
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [CombinatorialData]
        public void PropertyGet_ExpressionBody(
           [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            bool expectedParsedAsToken = languageVersion > LanguageVersion.CSharp13;
            UsingTree($$"""
                class C
                {
                    object P { get => field; }
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.GetAccessorDeclaration);
                            {
                                N(SyntaxKind.GetKeyword);
                                N(SyntaxKind.ArrowExpressionClause);
                                {
                                    N(SyntaxKind.EqualsGreaterThanToken);
                                    IdentifierNameOrFieldExpression(languageVersion, escapeIdentifier: false, expectedParsedAsToken);
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

        [Theory]
        [CombinatorialData]
        public void PropertyGet_BlockBody(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            bool expectedParsedAsToken = languageVersion > LanguageVersion.CSharp13;
            UsingTree($$"""
                class C
                {
                    object P { get { return field; } }
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.GetAccessorDeclaration);
                            {
                                N(SyntaxKind.GetKeyword);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.ReturnStatement);
                                    {
                                        N(SyntaxKind.ReturnKeyword);
                                        IdentifierNameOrFieldExpression(languageVersion, escapeIdentifier: false, expectedParsedAsToken);
                                        N(SyntaxKind.SemicolonToken);
                                    }
                                    N(SyntaxKind.CloseBraceToken);
                                }
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

        [Theory]
        [CombinatorialData]
        public void PropertySet_BlockBody(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool useInit)
        {
            bool expectedParsedAsToken = languageVersion > LanguageVersion.CSharp13;
            UsingTree($$"""
                class C
                {
                    object P { {{(useInit ? "init" : "set")}} { field = 0; } }
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(useInit ? SyntaxKind.InitAccessorDeclaration : SyntaxKind.SetAccessorDeclaration);
                            {
                                N(useInit ? SyntaxKind.InitKeyword : SyntaxKind.SetKeyword);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.ExpressionStatement);
                                    {
                                        N(SyntaxKind.SimpleAssignmentExpression);
                                        {
                                            IdentifierNameOrFieldExpression(languageVersion, escapeIdentifier: false, expectedParsedAsToken);
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "0");
                                            }
                                        }
                                        N(SyntaxKind.SemicolonToken);
                                    }
                                    N(SyntaxKind.CloseBraceToken);
                                }
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

        [Theory]
        [CombinatorialData]
        public void Indexer_ExpressionBody(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            UsingTree($$"""
                class C
                {
                    object this[int i] => field;
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IndexerDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.ThisKeyword);
                        N(SyntaxKind.BracketedParameterList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "i");
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "field");
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

        [Theory]
        [CombinatorialData]
        public void IndexerGet_ExpressionBody(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            UsingTree($$"""
                class C
                {
                    object this[int i] { get => field; }
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IndexerDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.ThisKeyword);
                        N(SyntaxKind.BracketedParameterList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "i");
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.GetAccessorDeclaration);
                            {
                                N(SyntaxKind.GetKeyword);
                                N(SyntaxKind.ArrowExpressionClause);
                                {
                                    N(SyntaxKind.EqualsGreaterThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "field");
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

        [Theory]
        [CombinatorialData]
        public void IndexerGet_BlockBody(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            UsingTree($$"""
                class C
                {
                    object this[int i] { get { return field; } }
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IndexerDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.ThisKeyword);
                        N(SyntaxKind.BracketedParameterList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "i");
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.GetAccessorDeclaration);
                            {
                                N(SyntaxKind.GetKeyword);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.ReturnStatement);
                                    {
                                        N(SyntaxKind.ReturnKeyword);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "field");
                                        }
                                        N(SyntaxKind.SemicolonToken);
                                    }
                                    N(SyntaxKind.CloseBraceToken);
                                }
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

        [Theory]
        [CombinatorialData]
        public void IndexerSet_BlockBody(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool useInit)
        {
            UsingTree($$"""
                class C
                {
                    object this[int i] { {{(useInit ? "init" : "set")}} { field = 0; } }
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IndexerDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.ThisKeyword);
                        N(SyntaxKind.BracketedParameterList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "i");
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(useInit ? SyntaxKind.InitAccessorDeclaration : SyntaxKind.SetAccessorDeclaration);
                            {
                                N(useInit ? SyntaxKind.InitKeyword : SyntaxKind.SetKeyword);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.ExpressionStatement);
                                    {
                                        N(SyntaxKind.SimpleAssignmentExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "field");
                                            }
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "0");
                                            }
                                        }
                                        N(SyntaxKind.SemicolonToken);
                                    }
                                    N(SyntaxKind.CloseBraceToken);
                                }
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

        [Theory]
        [CombinatorialData]
        public void EventAccessor(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool useRemove)
        {
            UsingTree($$"""
                class C
                {
                    event EventHandler E { {{(useRemove ? "remove" : "add")}} { field = null; } }
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.EventDeclaration);
                    {
                        N(SyntaxKind.EventKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "EventHandler");
                        }
                        N(SyntaxKind.IdentifierToken, "E");
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(useRemove ? SyntaxKind.RemoveAccessorDeclaration : SyntaxKind.AddAccessorDeclaration);
                            {
                                N(useRemove ? SyntaxKind.RemoveKeyword : SyntaxKind.AddKeyword);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.ExpressionStatement);
                                    {
                                        N(SyntaxKind.SimpleAssignmentExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "field");
                                            }
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.NullLiteralExpression);
                                            {
                                                N(SyntaxKind.NullKeyword);
                                            }
                                        }
                                        N(SyntaxKind.SemicolonToken);
                                    }
                                    N(SyntaxKind.CloseBraceToken);
                                }
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

        [Theory]
        [CombinatorialData]
        public void ExplicitImplementation_PropertySet_BlockBody(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool useInit)
        {
            bool expectedParsedAsToken = languageVersion > LanguageVersion.CSharp13;
            UsingTree($$"""
                class C
                {
                    object I<object>.P { {{(useInit ? "init" : "set")}} { field = 0; } }
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.ObjectKeyword);
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(useInit ? SyntaxKind.InitAccessorDeclaration : SyntaxKind.SetAccessorDeclaration);
                            {
                                N(useInit ? SyntaxKind.InitKeyword : SyntaxKind.SetKeyword);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.ExpressionStatement);
                                    {
                                        N(SyntaxKind.SimpleAssignmentExpression);
                                        {
                                            IdentifierNameOrFieldExpression(languageVersion, escapeIdentifier: false, expectedParsedAsToken);
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "0");
                                            }
                                        }
                                        N(SyntaxKind.SemicolonToken);
                                    }
                                    N(SyntaxKind.CloseBraceToken);
                                }
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

        [Theory]
        [CombinatorialData]
        public void ExplicitImplementation_IndexerSet_BlockBody(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool useInit)
        {
            UsingTree($$"""
                class C
                {
                    object I<int>.this[int i] { {{(useInit ? "init" : "set")}} { field = 0; } }
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IndexerDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
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
                            N(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.ThisKeyword);
                        N(SyntaxKind.BracketedParameterList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "i");
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(useInit ? SyntaxKind.InitAccessorDeclaration : SyntaxKind.SetAccessorDeclaration);
                            {
                                N(useInit ? SyntaxKind.InitKeyword : SyntaxKind.SetKeyword);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.ExpressionStatement);
                                    {
                                        N(SyntaxKind.SimpleAssignmentExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "field");
                                            }
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "0");
                                            }
                                        }
                                        N(SyntaxKind.SemicolonToken);
                                    }
                                    N(SyntaxKind.CloseBraceToken);
                                }
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

        [Theory]
        [CombinatorialData]
        public void Invocation(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            UsingTree($$"""
                class C
                {
                    object P => {{GetFieldIdentifier(escapeIdentifier)}}();
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                IdentifierNameOrFieldExpression(languageVersion, escapeIdentifier);
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
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

        [Theory]
        [CombinatorialData]
        public void ElementAccess(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            UsingTree($$"""
                class C
                {
                    object P => {{GetFieldIdentifier(escapeIdentifier)}}[0];
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.ElementAccessExpression);
                            {
                                IdentifierNameOrFieldExpression(languageVersion, escapeIdentifier);
                                N(SyntaxKind.BracketedArgumentList);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "0");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
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

        [Theory]
        [CombinatorialData]
        public void PreIncrement(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            UsingTree($$"""
                class C
                {
                    object P => ++{{GetFieldIdentifier(escapeIdentifier)}};
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.PreIncrementExpression);
                            {
                                N(SyntaxKind.PlusPlusToken);
                                IdentifierNameOrFieldExpression(languageVersion, escapeIdentifier);
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

        [Theory]
        [CombinatorialData]
        public void PostIncrement(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            UsingTree($$"""
                class C
                {
                    object P => {{GetFieldIdentifier(escapeIdentifier)}}++;
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.PostIncrementExpression);
                            {
                                IdentifierNameOrFieldExpression(languageVersion, escapeIdentifier);
                                N(SyntaxKind.PlusPlusToken);
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

        [Theory]
        [CombinatorialData]
        public void PointerIndirection(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            UsingTree($$"""
                class C
                {
                    object P => *{{GetFieldIdentifier(escapeIdentifier)}};
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.PointerIndirectionExpression);
                            {
                                N(SyntaxKind.AsteriskToken);
                                IdentifierNameOrFieldExpression(languageVersion, escapeIdentifier);
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

        [Theory]
        [CombinatorialData]
        public void PointerMemberAccess(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            UsingTree($$"""
                class C
                {
                    object P => {{GetFieldIdentifier(escapeIdentifier)}}->F;
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.PointerMemberAccessExpression);
                            {
                                IdentifierNameOrFieldExpression(languageVersion, escapeIdentifier);
                                N(SyntaxKind.MinusGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "F");
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

        [Theory]
        [CombinatorialData]
        public void ConditionalAccess(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            UsingTree($$"""
                class C
                {
                    object P => {{GetFieldIdentifier(escapeIdentifier)}}?.F;
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.ConditionalAccessExpression);
                            {
                                IdentifierNameOrFieldExpression(languageVersion, escapeIdentifier);
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.MemberBindingExpression);
                                {
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "F");
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

        [Theory]
        [CombinatorialData]
        public void NullableSuppression(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            UsingTree($$"""
                class C
                {
                    object P => {{GetFieldIdentifier(escapeIdentifier)}}!;
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.SuppressNullableWarningExpression);
                            {
                                IdentifierNameOrFieldExpression(languageVersion, escapeIdentifier);
                                N(SyntaxKind.ExclamationToken);
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

        [Theory]
        [CombinatorialData]
        public void Arguments(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            string identifier = GetFieldIdentifier(escapeIdentifier);
            UsingTree($$"""
                class C
                {
                    object P => F({{identifier}}, {{identifier}}, out {{identifier}});
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "F");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        IdentifierNameOrFieldExpression(languageVersion, escapeIdentifier);
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        IdentifierNameOrFieldExpression(languageVersion, escapeIdentifier);
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.OutKeyword);
                                        IdentifierNameOrFieldExpression(languageVersion, escapeIdentifier);
                                    }
                                    N(SyntaxKind.CloseParenToken);
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

        [Theory]
        [CombinatorialData]
        public void QualifiedName_01(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            string identifier = GetFieldIdentifier(escapeIdentifier);
            UsingTree($$"""
                class C
                {
                    object P => {{identifier}}.B;
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                IdentifierNameOrFieldExpression(languageVersion, escapeIdentifier);
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "B");
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

        [Theory]
        [CombinatorialData]
        public void QualifiedName_02(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            string identifier = GetFieldIdentifier(escapeIdentifier);
            UsingTree($$"""
                class C
                {
                    object P => A.{{identifier}};
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "A");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, identifier);
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

        [Theory]
        [CombinatorialData]
        public void AliasQualifiedName(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            string identifier = GetFieldIdentifier(escapeIdentifier);
            UsingTree($$"""
                class C
                {
                    object P => {{identifier}}::A.B;
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.AliasQualifiedName);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, identifier);
                                    }
                                    N(SyntaxKind.ColonColonToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "A");
                                    }
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "B");
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

        [Theory]
        [CombinatorialData]
        public void NameOf(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            UsingTree($$"""
                class C
                {
                    object P { set { _ = nameof({{GetFieldIdentifier(escapeIdentifier)}}); } }
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SetAccessorDeclaration);
                            {
                                N(SyntaxKind.SetKeyword);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.ExpressionStatement);
                                    {
                                        N(SyntaxKind.SimpleAssignmentExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "_");
                                            }
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.InvocationExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "nameof");
                                                }
                                                N(SyntaxKind.ArgumentList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.Argument);
                                                    {
                                                        IdentifierNameOrFieldExpression(languageVersion, escapeIdentifier);
                                                    }
                                                    N(SyntaxKind.CloseParenToken);
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
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [CombinatorialData]
        public void Lvalue(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            UsingTree($$"""
                class C
                {
                    object P { set { {{GetFieldIdentifier(escapeIdentifier)}} = 0; } }
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SetAccessorDeclaration);
                            {
                                N(SyntaxKind.SetKeyword);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.ExpressionStatement);
                                    {
                                        N(SyntaxKind.SimpleAssignmentExpression);
                                        {
                                            IdentifierNameOrFieldExpression(languageVersion, escapeIdentifier);
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "0");
                                            }
                                        }
                                        N(SyntaxKind.SemicolonToken);
                                    }
                                    N(SyntaxKind.CloseBraceToken);
                                }
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

        [Theory]
        [CombinatorialData]
        public void NewTypeName(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            string identifier = GetFieldIdentifier(escapeIdentifier);
            UsingTree($$"""
                class C
                {
                    object P { set { _ = new {{identifier}}(); } }
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SetAccessorDeclaration);
                            {
                                N(SyntaxKind.SetKeyword);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.ExpressionStatement);
                                    {
                                        N(SyntaxKind.SimpleAssignmentExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "_");
                                            }
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ObjectCreationExpression);
                                            {
                                                N(SyntaxKind.NewKeyword);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, identifier);
                                                }
                                                N(SyntaxKind.ArgumentList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
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
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [CombinatorialData]
        public void LambdaBody(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            string identifier = GetFieldIdentifier(escapeIdentifier);
            UsingTree($$"""
                class C
                {
                    object P => {{identifier}} => {{identifier}};
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.SimpleLambdaExpression);
                            {
                                N(SyntaxKind.Parameter);
                                {
                                    N(SyntaxKind.IdentifierToken, identifier);
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                IdentifierNameOrFieldExpression(languageVersion, escapeIdentifier);
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

        [Theory]
        [CombinatorialData]
        public void LocalFunctionBody(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            string identifier = GetFieldIdentifier(escapeIdentifier);
            UsingTree($$"""
                class C
                {
                    object P { set { void Local(object {{identifier}}) { _ = {{identifier}}; } } }
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SetAccessorDeclaration);
                            {
                                N(SyntaxKind.SetKeyword);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.LocalFunctionStatement);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.VoidKeyword);
                                        }
                                        N(SyntaxKind.IdentifierToken, "Local");
                                        N(SyntaxKind.ParameterList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.Parameter);
                                            {
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.ObjectKeyword);
                                                }
                                                N(SyntaxKind.IdentifierToken, identifier);
                                            }
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                        N(SyntaxKind.Block);
                                        {
                                            N(SyntaxKind.OpenBraceToken);
                                            N(SyntaxKind.ExpressionStatement);
                                            {
                                                N(SyntaxKind.SimpleAssignmentExpression);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "_");
                                                    }
                                                    N(SyntaxKind.EqualsToken);
                                                    IdentifierNameOrFieldExpression(languageVersion, escapeIdentifier);
                                                }
                                                N(SyntaxKind.SemicolonToken);
                                            }
                                            N(SyntaxKind.CloseBraceToken);
                                        }
                                    }
                                    N(SyntaxKind.CloseBraceToken);
                                }
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

        [Theory]
        [CombinatorialData]
        public void CatchDeclaration(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            string identifier = GetFieldIdentifier(escapeIdentifier);
            UsingTree($$"""
                class C
                {
                    object P { set { try { } catch (Exception {{identifier}}) { } } }
                }
                """,
                TestOptions.Regular.WithLanguageVersion(languageVersion));

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
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "P");
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SetAccessorDeclaration);
                            {
                                N(SyntaxKind.SetKeyword);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.TryStatement);
                                    {
                                        N(SyntaxKind.TryKeyword);
                                        N(SyntaxKind.Block);
                                        {
                                            N(SyntaxKind.OpenBraceToken);
                                            N(SyntaxKind.CloseBraceToken);
                                        }
                                        N(SyntaxKind.CatchClause);
                                        {
                                            N(SyntaxKind.CatchKeyword);
                                            N(SyntaxKind.CatchDeclaration);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Exception");
                                                }
                                                N(SyntaxKind.IdentifierToken, identifier);
                                                N(SyntaxKind.CloseParenToken);
                                            }
                                            N(SyntaxKind.Block);
                                            {
                                                N(SyntaxKind.OpenBraceToken);
                                                N(SyntaxKind.CloseBraceToken);
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CloseBraceToken);
                                }
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
        public void Incremental_ChangeBetweenMethodAndProperty()
        {
            var tree = ParseTree("""
                class C
                {
                    object F() => field;
                }
                """,
                TestOptions.RegularPreview);

            verifyMethod(tree);
            verifyProperty(tree.WithRemoveFirst("()"));

            tree = ParseTree("""
                class C
                {
                    object F => field;
                }
                """,
                TestOptions.RegularPreview);

            verifyProperty(tree);
            verifyMethod(tree.WithInsertBefore(" =>", "()"));

            void verifyMethod(SyntaxTree tree)
            {
                UsingTree(tree);
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
                                N(SyntaxKind.ObjectKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "F");
                            N(SyntaxKind.ParameterList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "field");
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

            void verifyProperty(SyntaxTree tree)
            {
                UsingTree(tree);
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
                                N(SyntaxKind.ObjectKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "F");
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.FieldExpression);
                                {
                                    N(SyntaxKind.FieldKeyword);
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
        }
    }
}
