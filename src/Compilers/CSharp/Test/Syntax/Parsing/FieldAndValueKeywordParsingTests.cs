// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class FieldAndValueKeywordParsingTests : ParsingTests
    {
        public FieldAndValueKeywordParsingTests(ITestOutputHelper output) : base(output)
        {
        }

        private static bool IsParsedAsToken(LanguageVersion languageVersion, bool escapeIdentifier)
        {
            return !escapeIdentifier && languageVersion > LanguageVersion.CSharp12;
        }

        private void IdentifierNameOrFieldOrValueExpression(LanguageVersion languageVersion, SyntaxKind tokenKind, bool escapeIdentifier)
        {
            IdentifierNameOrFieldOrValueExpression(languageVersion, tokenKind, escapeIdentifier, IsParsedAsToken(languageVersion, escapeIdentifier));
        }

        private void IdentifierNameOrFieldOrValueExpression(LanguageVersion languageVersion, SyntaxKind tokenKind, bool escapeIdentifier, bool isParsedAsToken)
        {
            if (isParsedAsToken)
            {
                N(tokenKind == SyntaxKind.FieldKeyword ? SyntaxKind.FieldExpression : SyntaxKind.ValueExpression);
                {
                    N(tokenKind);
                }
            }
            else
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, GetIdentifier(tokenKind, escapeIdentifier));
                }
            }
        }

        private static string GetIdentifier(SyntaxKind tokenKind, bool escapeIdentifier)
        {
            string identifier = SyntaxFacts.GetText(tokenKind);
            return escapeIdentifier ? "@" + identifier : identifier;
        }

        // PROTOTYPE: Search for all uses of LanguageParser.IsTrueIdentifier(). Most (all?) of those should also allow field or value because those were identifiers previously.

        [Theory]
        [CombinatorialData]
        public void Context_Property_Initializer(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(SyntaxKind.FieldKeyword, SyntaxKind.ValueKeyword)] SyntaxKind tokenKind)
        {
            bool expectedParsedAsToken = false;
            UsingTree($$"""
                class C
                {
                    object P { get; set; } = {{SyntaxFacts.GetText(tokenKind)}};
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
                            IdentifierNameOrFieldOrValueExpression(languageVersion, tokenKind, escapeIdentifier: false, expectedParsedAsToken);
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
        public void Context_Property_ExpressionBody(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(SyntaxKind.FieldKeyword, SyntaxKind.ValueKeyword)] SyntaxKind tokenKind)
        {
            bool expectedParsedAsToken = languageVersion > LanguageVersion.CSharp12 && tokenKind == SyntaxKind.FieldKeyword;
            UsingTree($$"""
                class C
                {
                    object P => {{SyntaxFacts.GetText(tokenKind)}};
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
                            IdentifierNameOrFieldOrValueExpression(languageVersion, tokenKind, escapeIdentifier: false, expectedParsedAsToken);
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
        public void Context_PropertyGet_ExpressionBody(
           [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(SyntaxKind.FieldKeyword, SyntaxKind.ValueKeyword)] SyntaxKind tokenKind)
        {
            bool expectedParsedAsToken = languageVersion > LanguageVersion.CSharp12 && tokenKind == SyntaxKind.FieldKeyword;
            UsingTree($$"""
                class C
                {
                    object P { get => {{SyntaxFacts.GetText(tokenKind)}}; }
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
                                    IdentifierNameOrFieldOrValueExpression(languageVersion, tokenKind, escapeIdentifier: false, expectedParsedAsToken);
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
        public void Context_PropertyGet_BlockBody(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(SyntaxKind.FieldKeyword, SyntaxKind.ValueKeyword)] SyntaxKind tokenKind)
        {
            bool expectedParsedAsToken = languageVersion > LanguageVersion.CSharp12 && tokenKind == SyntaxKind.FieldKeyword;
            UsingTree($$"""
                class C
                {
                    object P { get { return {{SyntaxFacts.GetText(tokenKind)}}; } }
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
                                        IdentifierNameOrFieldOrValueExpression(languageVersion, tokenKind, escapeIdentifier: false, expectedParsedAsToken);
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
        public void Context_PropertySet_BlockBody(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(SyntaxKind.FieldKeyword, SyntaxKind.ValueKeyword)] SyntaxKind tokenKind,
            bool useInit)
        {
            bool expectedParsedAsToken = languageVersion > LanguageVersion.CSharp12;
            UsingTree($$"""
                class C
                {
                    object P { {{(useInit ? "init" : "set")}} { {{SyntaxFacts.GetText(tokenKind)}} = 0; } }
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
                                            IdentifierNameOrFieldOrValueExpression(languageVersion, tokenKind, escapeIdentifier: false, expectedParsedAsToken);
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
        public void Context_Indexer_ExpressionBody(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(SyntaxKind.FieldKeyword, SyntaxKind.ValueKeyword)] SyntaxKind tokenKind)
        {
            bool expectedParsedAsToken = false;
            UsingTree($$"""
                class C
                {
                    object this[int i] => {{SyntaxFacts.GetText(tokenKind)}};
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
                            IdentifierNameOrFieldOrValueExpression(languageVersion, tokenKind, escapeIdentifier: false, expectedParsedAsToken);
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
        public void Context_IndexerGet_ExpressionBody(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(SyntaxKind.FieldKeyword, SyntaxKind.ValueKeyword)] SyntaxKind tokenKind)
        {
            bool expectedParsedAsToken = false;
            UsingTree($$"""
                class C
                {
                    object this[int i] { get => {{SyntaxFacts.GetText(tokenKind)}}; }
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
                                    IdentifierNameOrFieldOrValueExpression(languageVersion, tokenKind, escapeIdentifier: false, expectedParsedAsToken);
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
        public void Context_IndexerGet_BlockBody(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(SyntaxKind.FieldKeyword, SyntaxKind.ValueKeyword)] SyntaxKind tokenKind)
        {
            bool expectedParsedAsToken = false;
            UsingTree($$"""
                class C
                {
                    object this[int i] { get { return {{SyntaxFacts.GetText(tokenKind)}}; } }
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
                                        IdentifierNameOrFieldOrValueExpression(languageVersion, tokenKind, escapeIdentifier: false, expectedParsedAsToken);
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
        public void Context_IndexerSet_BlockBody(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(SyntaxKind.FieldKeyword, SyntaxKind.ValueKeyword)] SyntaxKind tokenKind,
            bool useInit)
        {
            bool expectedParsedAsToken = languageVersion > LanguageVersion.CSharp12 && tokenKind == SyntaxKind.ValueKeyword;
            UsingTree($$"""
                class C
                {
                    object this[int i] { {{(useInit ? "init" : "set")}} { {{SyntaxFacts.GetText(tokenKind)}} = 0; } }
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
                                            IdentifierNameOrFieldOrValueExpression(languageVersion, tokenKind, escapeIdentifier: false, expectedParsedAsToken);
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
        public void Context_EventAccessor(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(SyntaxKind.FieldKeyword, SyntaxKind.ValueKeyword)] SyntaxKind tokenKind,
            bool useRemove)
        {
            bool expectedParsedAsToken = languageVersion > LanguageVersion.CSharp12 && tokenKind == SyntaxKind.ValueKeyword;
            UsingTree($$"""
                class C
                {
                    event EventHandler E { {{(useRemove ? "remove" : "add")}} { {{SyntaxFacts.GetText(tokenKind)}} = null; } }
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
                                            IdentifierNameOrFieldOrValueExpression(languageVersion, tokenKind, escapeIdentifier: false, expectedParsedAsToken);
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
        public void Context_ExplicitImplementation_PropertySet_BlockBody(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(SyntaxKind.FieldKeyword, SyntaxKind.ValueKeyword)] SyntaxKind tokenKind,
            bool useInit)
        {
            bool expectedParsedAsToken = languageVersion > LanguageVersion.CSharp12;
            UsingTree($$"""
                class C
                {
                    object I<object>.P { {{(useInit ? "init" : "set")}} { {{SyntaxFacts.GetText(tokenKind)}} = 0; } }
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
                                            IdentifierNameOrFieldOrValueExpression(languageVersion, tokenKind, escapeIdentifier: false, expectedParsedAsToken);
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
        public void Context_ExplicitImplementation_IndexerSet_BlockBody(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(SyntaxKind.FieldKeyword, SyntaxKind.ValueKeyword)] SyntaxKind tokenKind,
            bool useInit)
        {
            bool expectedParsedAsToken = languageVersion > LanguageVersion.CSharp12 && tokenKind == SyntaxKind.ValueKeyword;
            UsingTree($$"""
                class C
                {
                    object I<int>.this[int i] { {{(useInit ? "init" : "set")}} { {{SyntaxFacts.GetText(tokenKind)}} = 0; } }
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
                                            IdentifierNameOrFieldOrValueExpression(languageVersion, tokenKind, escapeIdentifier: false, expectedParsedAsToken);
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
        public void Context_ExplicitImplementation_EventAccessor(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(SyntaxKind.FieldKeyword, SyntaxKind.ValueKeyword)] SyntaxKind tokenKind,
            bool useRemove)
        {
            bool expectedParsedAsToken = languageVersion > LanguageVersion.CSharp12 && tokenKind == SyntaxKind.ValueKeyword;
            UsingTree($$"""
                class C
                {
                    event EventHandler I.E { {{(useRemove ? "remove" : "add")}} { {{SyntaxFacts.GetText(tokenKind)}} = null; } }
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
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                            }
                            N(SyntaxKind.DotToken);
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
                                            IdentifierNameOrFieldOrValueExpression(languageVersion, tokenKind, escapeIdentifier: false, expectedParsedAsToken);
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
        public void Invocation(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(SyntaxKind.FieldKeyword, SyntaxKind.ValueKeyword)] SyntaxKind tokenKind,
            bool escapeIdentifier)
        {
            UsingTree($$"""
                class C
                {
                    object P { set { {{GetIdentifier(tokenKind, escapeIdentifier)}}(); } }
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
                                        N(SyntaxKind.InvocationExpression);
                                        {
                                            IdentifierNameOrFieldOrValueExpression(languageVersion, tokenKind, escapeIdentifier);
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
        public void Arguments(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(SyntaxKind.FieldKeyword, SyntaxKind.ValueKeyword)] SyntaxKind tokenKind,
            bool escapeIdentifier)
        {
            string identifier = GetIdentifier(tokenKind, escapeIdentifier);
            UsingTree($$"""
                class C
                {
                    object P { set { F({{identifier}}, {{identifier}}, out {{identifier}}); } }
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
                                                    IdentifierNameOrFieldOrValueExpression(languageVersion, tokenKind, escapeIdentifier);
                                                }
                                                N(SyntaxKind.CommaToken);
                                                N(SyntaxKind.Argument);
                                                {
                                                    IdentifierNameOrFieldOrValueExpression(languageVersion, tokenKind, escapeIdentifier);
                                                }
                                                N(SyntaxKind.CommaToken);
                                                N(SyntaxKind.Argument);
                                                {
                                                    N(SyntaxKind.OutKeyword);
                                                    IdentifierNameOrFieldOrValueExpression(languageVersion, tokenKind, escapeIdentifier);
                                                }
                                                N(SyntaxKind.CloseParenToken);
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
        public void NameOf(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(SyntaxKind.FieldKeyword, SyntaxKind.ValueKeyword)] SyntaxKind tokenKind,
            bool escapeIdentifier)
        {
            UsingTree($$"""
                class C
                {
                    object P { set { _ = nameof({{GetIdentifier(tokenKind, escapeIdentifier)}}); } }
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
                                                        IdentifierNameOrFieldOrValueExpression(languageVersion, tokenKind, escapeIdentifier);
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
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(SyntaxKind.FieldKeyword, SyntaxKind.ValueKeyword)] SyntaxKind tokenKind,
            bool escapeIdentifier)
        {
            UsingTree($$"""
                class C
                {
                    object P { set { {{GetIdentifier(tokenKind, escapeIdentifier)}} = 0; } }
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
                                            IdentifierNameOrFieldOrValueExpression(languageVersion, tokenKind, escapeIdentifier);
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
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(SyntaxKind.FieldKeyword, SyntaxKind.ValueKeyword)] SyntaxKind tokenKind,
            bool escapeIdentifier)
        {
            string identifier = GetIdentifier(tokenKind, escapeIdentifier);
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
        public void CatchDeclaration(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues(SyntaxKind.FieldKeyword, SyntaxKind.ValueKeyword)] SyntaxKind tokenKind,
            bool escapeIdentifier)
        {
            string identifier = GetIdentifier(tokenKind, escapeIdentifier);
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

        // PROTOTYPE: Test all possible identifier token cases. See FieldAndValueKeywordTests.IdentifierToken_*. Already covered:
        // IdentifierToken_IdentifierNameSyntax
        // IdentifierToken_CatchDeclarationSyntax
    }
}
