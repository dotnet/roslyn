// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class NameParsingTests : ParsingTests
    {
        public NameParsingTests(ITestOutputHelper output) : base(output) { }

        private NameSyntax ParseName(string text)
        {
            return SyntaxFactory.ParseName(text);
        }

        private TypeSyntax ParseTypeName(string text)
        {
            return SyntaxFactory.ParseTypeName(text);
        }

        [Fact]
        public void TestBasicName()
        {
            var text = "goo";
            var name = ParseName(text);

            Assert.NotNull(name);
            Assert.Equal(SyntaxKind.IdentifierName, name.Kind());
            Assert.False(((IdentifierNameSyntax)name).Identifier.IsMissing);
            Assert.Equal(0, name.Errors().Length);
            Assert.Equal(text, name.ToString());
        }

        [Fact]
        public void TestBasicNameWithTrash()
        {
            var text = "/*comment*/goo/*comment2*/ bar";
            var name = ParseName(text);

            Assert.NotNull(name);
            Assert.Equal(SyntaxKind.IdentifierName, name.Kind());
            Assert.False(((IdentifierNameSyntax)name).Identifier.IsMissing);
            Assert.Equal(1, name.Errors().Length);
            Assert.Equal(text, name.ToFullString());
        }

        [Fact]
        public void TestMissingName()
        {
            var text = string.Empty;
            var name = ParseName(text);

            Assert.NotNull(name);
            Assert.Equal(SyntaxKind.IdentifierName, name.Kind());
            Assert.True(((IdentifierNameSyntax)name).Identifier.IsMissing);
            Assert.Equal(1, name.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, name.Errors()[0].Code);
            Assert.Equal(string.Empty, name.ToString());
        }

        [Fact]
        public void TestMissingNameDueToKeyword()
        {
            var text = "class";
            var name = ParseName(text);

            Assert.NotNull(name);
            Assert.Equal(SyntaxKind.IdentifierName, name.Kind());
            Assert.True(name.IsMissing);
            Assert.Equal(2, name.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedToken, name.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, name.Errors()[1].Code);
            Assert.Equal(string.Empty, name.ToString());
        }

        [Fact]
        public void TestMissingNameDueToPartialClassStart()
        {
            var text = "partial class";
            var name = ParseName(text);

            Assert.NotNull(name);
            Assert.Equal(SyntaxKind.IdentifierName, name.Kind());
            Assert.True(name.IsMissing);
            Assert.Equal(2, name.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedToken, name.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, name.Errors()[1].Code);
            Assert.Equal(string.Empty, name.ToString());
        }

        [Fact]
        public void TestMissingNameDueToPartialMethodStart()
        {
            var text = "partial void Method()";
            var name = ParseName(text);

            Assert.NotNull(name);
            Assert.Equal(SyntaxKind.IdentifierName, name.Kind());
            Assert.True(name.IsMissing);
            Assert.Equal(2, name.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedToken, name.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, name.Errors()[1].Code);
            Assert.Equal(string.Empty, name.ToString());
        }

        [Fact]
        public void TestAliasedName()
        {
            var text = "goo::bar";
            var name = ParseName(text);

            Assert.NotNull(name);
            Assert.Equal(SyntaxKind.AliasQualifiedName, name.Kind());
            Assert.False(name.IsMissing);
            Assert.Equal(0, name.Errors().Length);
            Assert.Equal(text, name.ToString());
        }

        [Fact]
        public void TestGlobalAliasedName()
        {
            var text = "global::bar";
            var name = ParseName(text);

            Assert.NotNull(name);
            Assert.False(name.IsMissing);
            Assert.Equal(SyntaxKind.AliasQualifiedName, name.Kind());
            var an = (AliasQualifiedNameSyntax)name;
            Assert.Equal(SyntaxKind.GlobalKeyword, an.Alias.Identifier.Kind());
            Assert.Equal(0, name.Errors().Length);
            Assert.Equal(text, name.ToString());
        }

        [Fact]
        public void TestDottedName()
        {
            var text = "goo.bar";
            var name = ParseName(text);

            Assert.NotNull(name);
            Assert.Equal(SyntaxKind.QualifiedName, name.Kind());
            Assert.False(name.IsMissing);
            Assert.Equal(0, name.Errors().Length);
            Assert.Equal(text, name.ToString());
        }

        [Fact]
        public void TestAliasedDottedName()
        {
            var text = "goo::bar.Zed";
            var name = ParseName(text);

            Assert.NotNull(name);
            Assert.Equal(SyntaxKind.QualifiedName, name.Kind());
            Assert.False(name.IsMissing);
            Assert.Equal(0, name.Errors().Length);
            Assert.Equal(text, name.ToString());

            name = ((QualifiedNameSyntax)name).Left;
            Assert.Equal(SyntaxKind.AliasQualifiedName, name.Kind());
            Assert.False(name.IsMissing);
            Assert.Equal(0, name.Errors().Length);
        }

        [Fact]
        public void TestDoubleAliasName()
        {
            // In the original implementation of the parser this error case was parsed as 
            //
            // (goo :: bar ) :: baz
            //
            // However, we have decided that the left hand side of a :: should always be
            // an identifier, not a name, even in error cases. Therefore instead we 
            // parse this as though the error was that the user intended to make the 
            // second :: a dot; we parse this as
            //
            // (goo :: bar ) . baz

            var text = "goo::bar::baz";
            var name = ParseName(text);

            Assert.NotNull(name);
            Assert.Equal(SyntaxKind.QualifiedName, name.Kind());
            Assert.False(name.IsMissing);
            Assert.Equal(1, name.Errors().Length);
            Assert.Equal(text, name.ToString());

            name = ((QualifiedNameSyntax)name).Left;
            Assert.Equal(SyntaxKind.AliasQualifiedName, name.Kind());
            Assert.False(name.IsMissing);
            Assert.Equal(0, name.Errors().Length);
        }

        [Fact]
        public void TestGenericName()
        {
            var text = "goo<bar>";
            var name = ParseName(text);

            Assert.NotNull(name);
            Assert.Equal(SyntaxKind.GenericName, name.Kind());
            Assert.False(name.IsMissing);
            Assert.Equal(0, name.Errors().Length);
            var gname = (GenericNameSyntax)name;
            Assert.Equal(1, gname.TypeArgumentList.Arguments.Count);
            Assert.False(gname.IsUnboundGenericName);
            Assert.Equal(text, name.ToString());
        }

        [Fact]
        public void TestGenericNameWithTwoArguments()
        {
            var text = "goo<bar,zed>";
            var name = ParseName(text);

            Assert.NotNull(name);
            Assert.Equal(SyntaxKind.GenericName, name.Kind());
            Assert.False(name.IsMissing);
            Assert.Equal(0, name.Errors().Length);
            var gname = (GenericNameSyntax)name;
            Assert.Equal(2, gname.TypeArgumentList.Arguments.Count);
            Assert.False(gname.IsUnboundGenericName);
            Assert.Equal(text, name.ToString());
        }

        [Fact]
        public void TestNestedGenericName_01()
        {
            var text = "goo<bar<zed>>";
            var name = ParseName(text);

            Assert.NotNull(name);
            Assert.Equal(SyntaxKind.GenericName, name.Kind());
            Assert.False(name.IsMissing);
            Assert.Equal(0, name.Errors().Length);
            var gname = (GenericNameSyntax)name;
            Assert.Equal(1, gname.TypeArgumentList.Arguments.Count);
            Assert.False(gname.IsUnboundGenericName);
            Assert.NotNull(gname.TypeArgumentList.Arguments[0]);
            Assert.Equal(SyntaxKind.GenericName, gname.TypeArgumentList.Arguments[0].Kind());
            Assert.Equal(text, name.ToString());
        }

        [Fact]
        public void TestNestedGenericName_02()
        {
            var text = "goo<bar<zed<U>>>";
            var name = ParseName(text);

            UsingNode(text, name);

            N(SyntaxKind.GenericName);
            {
                N(SyntaxKind.IdentifierToken, "goo");
                N(SyntaxKind.TypeArgumentList);
                {
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "bar");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "zed");
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
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
            }
            EOF();
        }

        [Fact]
        public void TestOpenNameWithNoCommas()
        {
            var text = "goo<>";
            var name = ParseName(text);

            Assert.NotNull(name);
            Assert.Equal(SyntaxKind.GenericName, name.Kind());
            Assert.False(name.IsMissing);
            Assert.Equal(0, name.Errors().Length);
            var gname = (GenericNameSyntax)name;
            Assert.Equal(1, gname.TypeArgumentList.Arguments.Count);
            Assert.Equal(0, gname.TypeArgumentList.Arguments.SeparatorCount);
            Assert.True(gname.IsUnboundGenericName);
            Assert.Equal(text, name.ToString());
        }

        [Fact]
        public void TestOpenNameWithAComma()
        {
            var text = "goo<,>";
            var name = ParseName(text);

            Assert.NotNull(name);
            Assert.Equal(SyntaxKind.GenericName, name.Kind());
            Assert.False(name.IsMissing);
            Assert.Equal(0, name.Errors().Length);
            var gname = (GenericNameSyntax)name;
            Assert.Equal(2, gname.TypeArgumentList.Arguments.Count);
            Assert.Equal(1, gname.TypeArgumentList.Arguments.SeparatorCount);
            Assert.True(gname.IsUnboundGenericName);
            Assert.Equal(text, name.ToString());
        }

        [Fact]
        public void TestBasicTypeName()
        {
            var text = "goo";
            var tname = ParseTypeName(text);

            Assert.NotNull(tname);
            Assert.Equal(SyntaxKind.IdentifierName, tname.Kind());
            var name = (NameSyntax)tname;
            Assert.False(name.IsMissing);
            Assert.Equal(0, name.Errors().Length);
            Assert.Equal(text, name.ToString());
        }

        [Fact]
        public void TestDottedTypeName()
        {
            var text = "goo.bar";
            var tname = ParseTypeName(text);

            Assert.NotNull(tname);
            Assert.Equal(SyntaxKind.QualifiedName, tname.Kind());
            var name = (NameSyntax)tname;
            Assert.False(name.IsMissing);
            Assert.Equal(0, name.Errors().Length);
            Assert.Equal(text, name.ToString());
        }

        [Fact]
        public void TestGenericTypeName()
        {
            var text = "goo<bar>";
            var tname = ParseTypeName(text);

            Assert.NotNull(tname);
            Assert.Equal(SyntaxKind.GenericName, tname.Kind());
            var name = (NameSyntax)tname;
            Assert.False(name.IsMissing);
            Assert.Equal(0, name.Errors().Length);
            var gname = (GenericNameSyntax)name;
            Assert.Equal(1, gname.TypeArgumentList.Arguments.Count);
            Assert.False(gname.IsUnboundGenericName);
            Assert.Equal(text, name.ToString());
        }

        [Fact]
        public void TestNestedGenericTypeName_01()
        {
            var text = "goo<bar<zed>>";
            var tname = ParseTypeName(text);

            Assert.NotNull(tname);
            Assert.Equal(SyntaxKind.GenericName, tname.Kind());
            var name = (NameSyntax)tname;
            Assert.False(name.IsMissing);
            Assert.Equal(0, name.Errors().Length);
            var gname = (GenericNameSyntax)name;
            Assert.Equal(1, gname.TypeArgumentList.Arguments.Count);
            Assert.False(gname.IsUnboundGenericName);
            Assert.NotNull(gname.TypeArgumentList.Arguments[0]);
            Assert.Equal(SyntaxKind.GenericName, gname.TypeArgumentList.Arguments[0].Kind());
            Assert.Equal(text, name.ToString());
        }

        [Fact]
        public void TestNestedGenericTypeName_02()
        {
            var text = "goo<bar<zed<U>>>";
            var tname = ParseTypeName(text);

            UsingNode(text, tname);

            N(SyntaxKind.GenericName);
            {
                N(SyntaxKind.IdentifierToken, "goo");
                N(SyntaxKind.TypeArgumentList);
                {
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "bar");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "zed");
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
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
            }
            EOF();
        }

        [Fact]
        public void TestOpenTypeNameWithNoCommas()
        {
            var text = "goo<>";
            var tname = ParseTypeName(text);

            Assert.NotNull(tname);
            Assert.Equal(SyntaxKind.GenericName, tname.Kind());
            var name = (NameSyntax)tname;
            Assert.False(name.IsMissing);
            Assert.Equal(0, name.Errors().Length);
            var gname = (GenericNameSyntax)name;
            Assert.Equal(1, gname.TypeArgumentList.Arguments.Count);
            Assert.Equal(0, gname.TypeArgumentList.Arguments.SeparatorCount);
            Assert.True(gname.IsUnboundGenericName);
            Assert.Equal(text, name.ToString());
        }

        [Fact]
        public void TestKnownTypeNames()
        {
            ParseKnownTypeName(SyntaxKind.BoolKeyword);
            ParseKnownTypeName(SyntaxKind.ByteKeyword);
            ParseKnownTypeName(SyntaxKind.SByteKeyword);
            ParseKnownTypeName(SyntaxKind.ShortKeyword);
            ParseKnownTypeName(SyntaxKind.UShortKeyword);
            ParseKnownTypeName(SyntaxKind.IntKeyword);
            ParseKnownTypeName(SyntaxKind.UIntKeyword);
            ParseKnownTypeName(SyntaxKind.LongKeyword);
            ParseKnownTypeName(SyntaxKind.ULongKeyword);
            ParseKnownTypeName(SyntaxKind.FloatKeyword);
            ParseKnownTypeName(SyntaxKind.DoubleKeyword);
            ParseKnownTypeName(SyntaxKind.DecimalKeyword);
            ParseKnownTypeName(SyntaxKind.StringKeyword);
            ParseKnownTypeName(SyntaxKind.ObjectKeyword);
        }

        private void ParseKnownTypeName(SyntaxKind kind)
        {
            var text = SyntaxFacts.GetText(kind);
            var tname = ParseTypeName(text);

            Assert.NotNull(tname);
            Assert.Equal(SyntaxKind.PredefinedType, tname.Kind());
            Assert.Equal(text, tname.ToString());
            var tok = ((PredefinedTypeSyntax)tname).Keyword;
            Assert.Equal(kind, tok.Kind());
        }

        [Fact]
        public void TestNullableTypeName()
        {
            var text = "goo?";
            var tname = ParseTypeName(text);

            Assert.NotNull(tname);
            Assert.Equal(SyntaxKind.NullableType, tname.Kind());
            Assert.Equal(text, tname.ToString());
            var name = (NameSyntax)((NullableTypeSyntax)tname).ElementType;
            Assert.Equal(SyntaxKind.IdentifierName, name.Kind());
            Assert.False(name.IsMissing);
            Assert.Equal(0, name.Errors().Length);
        }

        [Fact]
        public void TestPointerTypeName()
        {
            var text = "goo*";
            var tname = ParseTypeName(text);

            Assert.NotNull(tname);
            Assert.Equal(SyntaxKind.PointerType, tname.Kind());
            Assert.Equal(text, tname.ToString());
            var name = (NameSyntax)((PointerTypeSyntax)tname).ElementType;
            Assert.Equal(SyntaxKind.IdentifierName, name.Kind());
            Assert.False(name.IsMissing);
            Assert.Equal(0, name.Errors().Length);
        }

        [Fact]
        public void TestPointerTypeNameWithMultipleAsterisks()
        {
            var text = "goo***";
            var tname = ParseTypeName(text);

            Assert.NotNull(tname);
            Assert.Equal(text, tname.ToString());
            Assert.Equal(SyntaxKind.PointerType, tname.Kind());

            // check depth of pointer defers
            int depth = 0;
            while (tname.Kind() == SyntaxKind.PointerType)
            {
                tname = ((PointerTypeSyntax)tname).ElementType;
                depth++;
            }

            Assert.Equal(3, depth);

            var name = (NameSyntax)tname;
            Assert.Equal(SyntaxKind.IdentifierName, name.Kind());
            Assert.False(name.IsMissing);
            Assert.Equal(0, name.Errors().Length);
        }

        [Fact]
        public void TestArrayTypeName()
        {
            var text = "goo[]";
            var tname = ParseTypeName(text);

            Assert.NotNull(tname);
            Assert.Equal(text, tname.ToString());
            Assert.Equal(SyntaxKind.ArrayType, tname.Kind());

            var array = (ArrayTypeSyntax)tname;
            Assert.Equal(1, array.RankSpecifiers.Count);
            Assert.Equal(1, array.RankSpecifiers[0].Sizes.Count);
            Assert.Equal(0, array.RankSpecifiers[0].Sizes.SeparatorCount);
            Assert.Equal(1, array.RankSpecifiers[0].Rank);

            var name = (NameSyntax)array.ElementType;
            Assert.Equal(SyntaxKind.IdentifierName, name.Kind());
            Assert.False(name.IsMissing);
            Assert.Equal(0, name.Errors().Length);
        }

        [Fact]
        public void TestMultiDimensionalArrayTypeName()
        {
            var text = "goo[,,]";
            var tname = ParseTypeName(text);

            Assert.NotNull(tname);
            Assert.Equal(text, tname.ToString());
            Assert.Equal(SyntaxKind.ArrayType, tname.Kind());

            var array = (ArrayTypeSyntax)tname;
            Assert.Equal(1, array.RankSpecifiers.Count);
            Assert.Equal(3, array.RankSpecifiers[0].Sizes.Count);
            Assert.Equal(2, array.RankSpecifiers[0].Sizes.SeparatorCount);
            Assert.Equal(3, array.RankSpecifiers[0].Rank);

            var name = (NameSyntax)array.ElementType;
            Assert.Equal(SyntaxKind.IdentifierName, name.Kind());
            Assert.False(name.IsMissing);
            Assert.Equal(0, name.Errors().Length);
        }

        [Fact]
        public void TestMultiRankedArrayTypeName()
        {
            var text = "goo[][,][,,]";
            var tname = ParseTypeName(text);

            Assert.NotNull(tname);
            Assert.Equal(text, tname.ToString());
            Assert.Equal(SyntaxKind.ArrayType, tname.Kind());

            var array = (ArrayTypeSyntax)tname;
            Assert.Equal(3, array.RankSpecifiers.Count);

            Assert.Equal(1, array.RankSpecifiers[0].Sizes.Count);
            Assert.Equal(0, array.RankSpecifiers[0].Sizes.SeparatorCount);
            Assert.Equal(1, array.RankSpecifiers[0].Rank);

            Assert.Equal(2, array.RankSpecifiers[1].Sizes.Count);
            Assert.Equal(1, array.RankSpecifiers[1].Sizes.SeparatorCount);
            Assert.Equal(2, array.RankSpecifiers[1].Rank);

            Assert.Equal(3, array.RankSpecifiers[2].Sizes.Count);
            Assert.Equal(2, array.RankSpecifiers[2].Sizes.SeparatorCount);
            Assert.Equal(3, array.RankSpecifiers[2].Rank);

            var name = (NameSyntax)array.ElementType;
            Assert.Equal(SyntaxKind.IdentifierName, name.Kind());
            Assert.False(name.IsMissing);
            Assert.Equal(0, name.Errors().Length);
        }

        [Fact]
        public void TestVarianceInNameBad()
        {
            var text = "goo<in bar>";
            var tname = ParseName(text);

            Assert.NotNull(tname);
            Assert.Equal(text, tname.ToString());
            Assert.Equal(SyntaxKind.GenericName, tname.Kind());

            var gname = (GenericNameSyntax)tname;
            Assert.Equal("goo", gname.Identifier.ToString());
            Assert.False(gname.IsUnboundGenericName);
            Assert.Equal(1, gname.TypeArgumentList.Arguments.Count);
            Assert.NotNull(gname.TypeArgumentList.Arguments[0]);

            var arg = gname.TypeArgumentList.Arguments[0];
            Assert.Equal(SyntaxKind.IdentifierName, arg.Kind());
            Assert.True(arg.ContainsDiagnostics);
            Assert.Equal(1, arg.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IllegalVarianceSyntax, arg.Errors()[0].Code);

            Assert.Equal(text, tname.ToString());
        }

        [Fact]
        public void TestAttributeInNameBad()
        {
            var text = "goo<[My]bar>";
            var tname = ParseName(text);

            Assert.NotNull(tname);
            Assert.Equal(text, tname.ToString());
            Assert.Equal(SyntaxKind.GenericName, tname.Kind());

            var gname = (GenericNameSyntax)tname;
            Assert.Equal("goo", gname.Identifier.ToString());
            Assert.False(gname.IsUnboundGenericName);
            Assert.Equal(1, gname.TypeArgumentList.Arguments.Count);
            Assert.NotNull(gname.TypeArgumentList.Arguments[0]);

            var arg = gname.TypeArgumentList.Arguments[0];
            Assert.Equal(SyntaxKind.IdentifierName, arg.Kind());
            Assert.True(arg.ContainsDiagnostics);
            Assert.Equal(1, arg.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, arg.Errors()[0].Code);

            Assert.Equal(text, tname.ToString());
        }

        [WorkItem(7177, "https://github.com/dotnet/roslyn/issues/7177")]
        [Fact]
        public void TestConstantInGenericNameBad()
        {
            var text = "goo<0>";
            var tname = ParseName(text);

            Assert.NotNull(tname);
            Assert.Equal(text, tname.ToString());
            Assert.Equal(SyntaxKind.GenericName, tname.Kind());

            var gname = (GenericNameSyntax)tname;
            Assert.Equal("goo", gname.Identifier.ToString());
            Assert.False(gname.IsUnboundGenericName);
            Assert.Equal(1, gname.TypeArgumentList.Arguments.Count);
            Assert.NotNull(gname.TypeArgumentList.Arguments[0]);

            var arg = gname.TypeArgumentList.Arguments[0];
            Assert.Equal(SyntaxKind.IdentifierName, arg.Kind());
            Assert.True(arg.ContainsDiagnostics);
            Assert.Equal(1, arg.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, arg.Errors()[0].Code);

            Assert.Equal(text, tname.ToString());
        }

        [WorkItem(7177, "https://github.com/dotnet/roslyn/issues/7177")]
        [Fact]
        public void TestConstantInGenericNamePartiallyBad()
        {
            var text = "goo<0,bool>";
            var tname = ParseName(text);

            Assert.NotNull(tname);
            Assert.Equal(text, tname.ToString());
            Assert.Equal(SyntaxKind.GenericName, tname.Kind());

            var gname = (GenericNameSyntax)tname;
            Assert.Equal("goo", gname.Identifier.ToString());
            Assert.False(gname.IsUnboundGenericName);
            Assert.Equal(2, gname.TypeArgumentList.Arguments.Count);
            Assert.NotNull(gname.TypeArgumentList.Arguments[0]);
            Assert.NotNull(gname.TypeArgumentList.Arguments[1]);

            var arg = gname.TypeArgumentList.Arguments[0];
            Assert.Equal(SyntaxKind.IdentifierName, arg.Kind());
            Assert.True(arg.ContainsDiagnostics);
            Assert.Equal(1, arg.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, arg.Errors()[0].Code);

            var arg2 = gname.TypeArgumentList.Arguments[1];
            Assert.Equal(SyntaxKind.PredefinedType, arg2.Kind());
            Assert.False(arg2.ContainsDiagnostics);
            Assert.Equal(0, arg2.Errors().Length);

            Assert.Equal(text, tname.ToString());
        }

        [WorkItem(7177, "https://github.com/dotnet/roslyn/issues/7177")]
        [Fact]
        public void TestKeywordInGenericNameBad()
        {
            var text = "goo<static>";
            var tname = ParseName(text);

            Assert.NotNull(tname);
            Assert.Equal(text, tname.ToString());
            Assert.Equal(SyntaxKind.GenericName, tname.Kind());

            var gname = (GenericNameSyntax)tname;
            Assert.Equal("goo", gname.Identifier.ToString());
            Assert.False(gname.IsUnboundGenericName);
            Assert.Equal(1, gname.TypeArgumentList.Arguments.Count);
            Assert.NotNull(gname.TypeArgumentList.Arguments[0]);

            var arg = gname.TypeArgumentList.Arguments[0];
            Assert.Equal(SyntaxKind.IdentifierName, arg.Kind());
            Assert.True(arg.ContainsDiagnostics);
            Assert.Equal(1, arg.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, arg.Errors()[0].Code);

            Assert.Equal(text, tname.ToString());
        }

        [Fact]
        public void TestAttributeAndVarianceInNameBad()
        {
            var text = "goo<[My]in bar>";
            var tname = ParseName(text);

            Assert.NotNull(tname);
            Assert.Equal(text, tname.ToString());
            Assert.Equal(SyntaxKind.GenericName, tname.Kind());

            var gname = (GenericNameSyntax)tname;
            Assert.Equal("goo", gname.Identifier.ToString());
            Assert.False(gname.IsUnboundGenericName);
            Assert.Equal(1, gname.TypeArgumentList.Arguments.Count);
            Assert.NotNull(gname.TypeArgumentList.Arguments[0]);

            var arg = gname.TypeArgumentList.Arguments[0];
            Assert.Equal(SyntaxKind.IdentifierName, arg.Kind());
            Assert.True(arg.ContainsDiagnostics);
            Assert.Equal(2, arg.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, arg.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_IllegalVarianceSyntax, arg.Errors()[1].Code);

            Assert.Equal(text, tname.ToString());
        }

        [WorkItem(545778, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545778")]
        [Fact]
        public void TestFormattingCharacter()
        {
            var text = "\u0915\u094d\u200d\u0937";
            var tok = SyntaxFactory.ParseToken(text);

            Assert.NotEqual(default, tok);
            Assert.Equal(text, tok.ToString());
            Assert.NotEqual(text, tok.ValueText);
            Assert.Equal("\u0915\u094d\u0937", tok.ValueText); //formatting character \u200d removed

            Assert.True(SyntaxFacts.ContainsDroppedIdentifierCharacters(text));
            Assert.False(SyntaxFacts.ContainsDroppedIdentifierCharacters(tok.ValueText));
        }

        [WorkItem(959148, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/959148")]
        [Fact]
        public void TestSoftHyphen()
        {
            var text = "x\u00ady";
            var tok = SyntaxFactory.ParseToken(text);

            Assert.NotEqual(default, tok);
            Assert.Equal(text, tok.ToString());
            Assert.NotEqual(text, tok.ValueText);
            Assert.Equal("xy", tok.ValueText); // formatting character SOFT HYPHEN (U+00AD) removed

            Assert.True(SyntaxFacts.ContainsDroppedIdentifierCharacters(text));
            Assert.False(SyntaxFacts.ContainsDroppedIdentifierCharacters(tok.ValueText));
        }

        [WorkItem(545778, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545778")]
        [Fact]
        public void ContainsDroppedIdentifierCharacters()
        {
            Assert.False(SyntaxFacts.ContainsDroppedIdentifierCharacters(null));
            Assert.False(SyntaxFacts.ContainsDroppedIdentifierCharacters(""));
            Assert.False(SyntaxFacts.ContainsDroppedIdentifierCharacters("a"));
            Assert.False(SyntaxFacts.ContainsDroppedIdentifierCharacters("a@"));

            Assert.True(SyntaxFacts.ContainsDroppedIdentifierCharacters("@"));
            Assert.True(SyntaxFacts.ContainsDroppedIdentifierCharacters("@a"));
            Assert.True(SyntaxFacts.ContainsDroppedIdentifierCharacters("\u200d"));
            Assert.True(SyntaxFacts.ContainsDroppedIdentifierCharacters("a\u200d"));
        }
    }
}
