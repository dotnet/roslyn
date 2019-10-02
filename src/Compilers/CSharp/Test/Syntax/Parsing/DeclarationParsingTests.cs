// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class DeclarationParsingTests : ParsingTests
    {
        public DeclarationParsingTests(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options ?? TestOptions.Regular);
        }

        [Fact]
        public void TestExternAlias()
        {
            var text = "extern alias a;";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Externs.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            var ea = file.Externs[0];

            Assert.NotEqual(default, ea.ExternKeyword);
            Assert.Equal(SyntaxKind.ExternKeyword, ea.ExternKeyword.Kind());
            Assert.NotEqual(default, ea.AliasKeyword);
            Assert.Equal(SyntaxKind.AliasKeyword, ea.AliasKeyword.Kind());
            Assert.False(ea.AliasKeyword.IsMissing);
            Assert.NotEqual(default, ea.Identifier);
            Assert.Equal("a", ea.Identifier.ToString());
            Assert.NotEqual(default, ea.SemicolonToken);
        }

        [Fact]
        public void TestExternWithoutAlias()
        {
            var text = "extern a;";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Externs.Count);
            Assert.Equal(text, file.ToString());
            var errors = file.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, errors[0].Code);

            var ea = file.Externs[0];

            Assert.NotEqual(default, ea.ExternKeyword);
            Assert.Equal(SyntaxKind.ExternKeyword, ea.ExternKeyword.Kind());
            Assert.NotEqual(default, ea.AliasKeyword);
            Assert.Equal(SyntaxKind.AliasKeyword, ea.AliasKeyword.Kind());
            Assert.True(ea.AliasKeyword.IsMissing);
            Assert.NotEqual(default, ea.Identifier);
            Assert.Equal("a", ea.Identifier.ToString());
            Assert.NotEqual(default, ea.SemicolonToken);
        }

        [Fact]
        public void TestUsing()
        {
            var text = "using a;";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Usings.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            var ud = file.Usings[0];

            Assert.NotEqual(default, ud.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, ud.UsingKeyword.Kind());
            Assert.Null(ud.Alias);
            Assert.True(ud.StaticKeyword == default(SyntaxToken));
            Assert.NotNull(ud.Name);
            Assert.Equal("a", ud.Name.ToString());
            Assert.NotEqual(default, ud.SemicolonToken);
        }

        [Fact]
        public void TestUsingStatic()
        {
            var text = "using static a;";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Usings.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            var ud = file.Usings[0];

            Assert.NotEqual(default, ud.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, ud.UsingKeyword.Kind());
            Assert.Equal(SyntaxKind.StaticKeyword, ud.StaticKeyword.Kind());
            Assert.Null(ud.Alias);
            Assert.NotNull(ud.Name);
            Assert.Equal("a", ud.Name.ToString());
            Assert.NotEqual(default, ud.SemicolonToken);
        }

        [Fact]
        public void TestUsingStaticInWrongOrder()
        {
            var text = "static using a;";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Usings.Count);
            Assert.Equal(text, file.ToFullString());

            var errors = file.Errors();
            Assert.True(errors.Length > 0);
            Assert.Equal((int)ErrorCode.ERR_NamespaceUnexpected, errors[0].Code);
        }

        [Fact]
        public void TestDuplicateStatic()
        {
            var text = "using static static a;";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Usings.Count);
            Assert.Equal(text, file.ToString());

            var errors = file.Errors();
            Assert.True(errors.Length > 0);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpectedKW, errors[0].Code);
        }

        [Fact]
        public void TestUsingNamespace()
        {
            var text = "using namespace a;";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Usings.Count);
            Assert.Equal(text, file.ToString());

            var errors = file.Errors();
            Assert.True(errors.Length > 0);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpectedKW, errors[0].Code);
        }

        [Fact]
        public void TestUsingDottedName()
        {
            var text = "using a.b;";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Usings.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            var ud = file.Usings[0];

            Assert.NotEqual(default, ud.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, ud.UsingKeyword.Kind());
            Assert.True(ud.StaticKeyword == default(SyntaxToken));
            Assert.Null(ud.Alias);
            Assert.NotNull(ud.Name);
            Assert.Equal("a.b", ud.Name.ToString());
            Assert.NotEqual(default, ud.SemicolonToken);
        }

        [Fact]
        public void TestUsingStaticDottedName()
        {
            var text = "using static a.b;";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Usings.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            var ud = file.Usings[0];

            Assert.NotEqual(default, ud.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, ud.UsingKeyword.Kind());
            Assert.Equal(SyntaxKind.StaticKeyword, ud.StaticKeyword.Kind());
            Assert.Null(ud.Alias);
            Assert.NotNull(ud.Name);
            Assert.Equal("a.b", ud.Name.ToString());
            Assert.NotEqual(default, ud.SemicolonToken);
        }

        [Fact]
        public void TestUsingStaticGenericName()
        {
            var text = "using static a<int?>;";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Usings.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            var ud = file.Usings[0];

            Assert.NotEqual(default, ud.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, ud.UsingKeyword.Kind());
            Assert.Equal(SyntaxKind.StaticKeyword, ud.StaticKeyword.Kind());
            Assert.Null(ud.Alias);
            Assert.NotNull(ud.Name);
            Assert.Equal("a<int?>", ud.Name.ToString());
            Assert.NotEqual(default, ud.SemicolonToken);
        }

        [Fact]
        public void TestUsingAliasName()
        {
            var text = "using a = b;";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Usings.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            var ud = file.Usings[0];

            Assert.NotEqual(default, ud.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, ud.UsingKeyword.Kind());
            Assert.NotNull(ud.Alias);
            Assert.NotNull(ud.Alias.Name);
            Assert.Equal("a", ud.Alias.Name.ToString());
            Assert.NotEqual(default, ud.Alias.EqualsToken);
            Assert.NotNull(ud.Name);
            Assert.Equal("b", ud.Name.ToString());
            Assert.NotEqual(default, ud.SemicolonToken);
        }

        [Fact]
        public void TestUsingAliasGenericName()
        {
            var text = "using a = b<c>;";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Usings.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            var ud = file.Usings[0];

            Assert.NotEqual(default, ud.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, ud.UsingKeyword.Kind());
            Assert.NotNull(ud.Alias);
            Assert.NotNull(ud.Alias.Name);
            Assert.Equal("a", ud.Alias.Name.ToString());
            Assert.NotEqual(default, ud.Alias.EqualsToken);
            Assert.NotNull(ud.Name);
            Assert.Equal("b<c>", ud.Name.ToString());
            Assert.NotEqual(default, ud.SemicolonToken);
        }

        [Fact]
        public void TestGlobalAttribute()
        {
            var text = "[assembly:a]";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.AttributeList, file.AttributeLists[0].Kind());
            var ad = (AttributeListSyntax)file.AttributeLists[0];

            Assert.NotEqual(default, ad.OpenBracketToken);
            Assert.NotNull(ad.Target);
            Assert.NotEqual(default, ad.Target.Identifier);
            Assert.Equal("assembly", ad.Target.Identifier.ToString());
            Assert.NotEqual(default, ad.Target.ColonToken);
            Assert.Equal(1, ad.Attributes.Count);
            Assert.NotNull(ad.Attributes[0].Name);
            Assert.Equal("a", ad.Attributes[0].Name.ToString());
            Assert.Null(ad.Attributes[0].ArgumentList);
            Assert.NotEqual(default, ad.CloseBracketToken);
        }

        [Fact]
        public void TestGlobalAttribute_Verbatim()
        {
            var text = "[@assembly:a]";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.AttributeList, file.AttributeLists[0].Kind());
            var ad = (AttributeListSyntax)file.AttributeLists[0];

            Assert.NotEqual(default, ad.OpenBracketToken);
            Assert.NotNull(ad.Target);
            Assert.NotEqual(default, ad.Target.Identifier);
            Assert.Equal("@assembly", ad.Target.Identifier.ToString());
            Assert.Equal("assembly", ad.Target.Identifier.ValueText);
            Assert.Equal(SyntaxKind.IdentifierToken, ad.Target.Identifier.Kind());
            Assert.Equal(AttributeLocation.Assembly, ad.Target.Identifier.ToAttributeLocation());
            Assert.NotEqual(default, ad.Target.ColonToken);
            Assert.Equal(1, ad.Attributes.Count);
            Assert.NotNull(ad.Attributes[0].Name);
            Assert.Equal("a", ad.Attributes[0].Name.ToString());
            Assert.Null(ad.Attributes[0].ArgumentList);
            Assert.NotEqual(default, ad.CloseBracketToken);
        }

        [Fact]
        public void TestGlobalAttribute_Escape()
        {
            var text = @"[as\u0073embly:a]";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.AttributeList, file.AttributeLists[0].Kind());
            var ad = (AttributeListSyntax)file.AttributeLists[0];

            Assert.NotEqual(default, ad.OpenBracketToken);
            Assert.NotEqual(default, ad.Target);
            Assert.NotEqual(default, ad.Target.Identifier);
            Assert.Equal(@"as\u0073embly", ad.Target.Identifier.ToString());
            Assert.Equal("assembly", ad.Target.Identifier.ValueText);
            Assert.Equal(SyntaxKind.IdentifierToken, ad.Target.Identifier.Kind());
            Assert.Equal(AttributeLocation.Assembly, ad.Target.Identifier.ToAttributeLocation());
            Assert.NotEqual(default, ad.Target.ColonToken);
            Assert.Equal(1, ad.Attributes.Count);
            Assert.NotNull(ad.Attributes[0].Name);
            Assert.Equal("a", ad.Attributes[0].Name.ToString());
            Assert.Null(ad.Attributes[0].ArgumentList);
            Assert.NotEqual(default, ad.CloseBracketToken);
        }

        [Fact]
        public void TestGlobalModuleAttribute()
        {
            var text = "[module:a]";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.AttributeList, file.AttributeLists[0].Kind());
            var ad = (AttributeListSyntax)file.AttributeLists[0];

            Assert.NotEqual(default, ad.OpenBracketToken);
            Assert.NotNull(ad.Target);
            Assert.NotEqual(default, ad.Target.Identifier);
            Assert.Equal("module", ad.Target.Identifier.ToString());
            Assert.Equal(SyntaxKind.ModuleKeyword, ad.Target.Identifier.Kind());
            Assert.NotEqual(default, ad.Target.ColonToken);
            Assert.Equal(1, ad.Attributes.Count);
            Assert.NotNull(ad.Attributes[0].Name);
            Assert.Equal("a", ad.Attributes[0].Name.ToString());
            Assert.Null(ad.Attributes[0].ArgumentList);
            Assert.NotEqual(default, ad.CloseBracketToken);
        }

        [Fact]
        public void TestGlobalModuleAttribute_Verbatim()
        {
            var text = "[@module:a]";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.AttributeList, file.AttributeLists[0].Kind());
            var ad = (AttributeListSyntax)file.AttributeLists[0];

            Assert.NotEqual(default, ad.OpenBracketToken);
            Assert.NotNull(ad.Target);
            Assert.NotEqual(default, ad.Target.Identifier);
            Assert.Equal("@module", ad.Target.Identifier.ToString());
            Assert.Equal(SyntaxKind.IdentifierToken, ad.Target.Identifier.Kind());
            Assert.Equal(AttributeLocation.Module, ad.Target.Identifier.ToAttributeLocation());
            Assert.NotEqual(default, ad.Target.ColonToken);
            Assert.Equal(1, ad.Attributes.Count);
            Assert.NotNull(ad.Attributes[0].Name);
            Assert.Equal("a", ad.Attributes[0].Name.ToString());
            Assert.Null(ad.Attributes[0].ArgumentList);
            Assert.NotEqual(default, ad.CloseBracketToken);
        }

        [Fact]
        public void TestGlobalAttributeWithParentheses()
        {
            var text = "[assembly:a()]";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.AttributeList, file.AttributeLists[0].Kind());
            var ad = (AttributeListSyntax)file.AttributeLists[0];

            Assert.NotEqual(default, ad.OpenBracketToken);
            Assert.NotNull(ad.Target);
            Assert.NotEqual(default, ad.Target.Identifier);
            Assert.Equal("assembly", ad.Target.Identifier.ToString());
            Assert.Equal(SyntaxKind.AssemblyKeyword, ad.Target.Identifier.Kind());
            Assert.NotEqual(default, ad.Target.ColonToken);
            Assert.Equal(1, ad.Attributes.Count);
            Assert.NotNull(ad.Attributes[0].Name);
            Assert.Equal("a", ad.Attributes[0].Name.ToString());
            Assert.NotNull(ad.Attributes[0].ArgumentList);
            Assert.NotEqual(default, ad.Attributes[0].ArgumentList.OpenParenToken);
            Assert.Equal(0, ad.Attributes[0].ArgumentList.Arguments.Count);
            Assert.NotEqual(default, ad.Attributes[0].ArgumentList.CloseParenToken);
            Assert.NotEqual(default, ad.CloseBracketToken);
        }

        [Fact]
        public void TestGlobalAttributeWithMultipleArguments()
        {
            var text = "[assembly:a(b, c)]";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.AttributeList, file.AttributeLists[0].Kind());
            var ad = (AttributeListSyntax)file.AttributeLists[0];

            Assert.NotEqual(default, ad.OpenBracketToken);
            Assert.NotNull(ad.Target);
            Assert.NotEqual(default, ad.Target.Identifier);
            Assert.Equal("assembly", ad.Target.Identifier.ToString());
            Assert.NotEqual(default, ad.Target.ColonToken);
            Assert.Equal(1, ad.Attributes.Count);
            Assert.NotNull(ad.Attributes[0].Name);
            Assert.Equal("a", ad.Attributes[0].Name.ToString());
            Assert.NotNull(ad.Attributes[0].ArgumentList);
            Assert.NotEqual(default, ad.Attributes[0].ArgumentList.OpenParenToken);
            Assert.Equal(2, ad.Attributes[0].ArgumentList.Arguments.Count);
            Assert.Equal("b", ad.Attributes[0].ArgumentList.Arguments[0].ToString());
            Assert.Equal("c", ad.Attributes[0].ArgumentList.Arguments[1].ToString());
            Assert.NotEqual(default, ad.Attributes[0].ArgumentList.CloseParenToken);
            Assert.NotEqual(default, ad.CloseBracketToken);
        }

        [Fact]
        public void TestGlobalAttributeWithNamedArguments()
        {
            var text = "[assembly:a(b = c)]";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.AttributeList, file.AttributeLists[0].Kind());
            var ad = (AttributeListSyntax)file.AttributeLists[0];

            Assert.NotEqual(default, ad.OpenBracketToken);
            Assert.NotNull(ad.Target);
            Assert.NotEqual(default, ad.Target.Identifier);
            Assert.Equal("assembly", ad.Target.Identifier.ToString());
            Assert.NotEqual(default, ad.Target.ColonToken);
            Assert.Equal(1, ad.Attributes.Count);
            Assert.NotNull(ad.Attributes[0].Name);
            Assert.Equal("a", ad.Attributes[0].Name.ToString());
            Assert.NotNull(ad.Attributes[0].ArgumentList);
            Assert.NotEqual(default, ad.Attributes[0].ArgumentList.OpenParenToken);
            Assert.Equal(1, ad.Attributes[0].ArgumentList.Arguments.Count);
            Assert.Equal("b = c", ad.Attributes[0].ArgumentList.Arguments[0].ToString());
            Assert.NotNull(ad.Attributes[0].ArgumentList.Arguments[0].NameEquals);
            Assert.NotNull(ad.Attributes[0].ArgumentList.Arguments[0].NameEquals.Name);
            Assert.Equal("b", ad.Attributes[0].ArgumentList.Arguments[0].NameEquals.Name.ToString());
            Assert.NotEqual(default, ad.Attributes[0].ArgumentList.Arguments[0].NameEquals.EqualsToken);
            Assert.NotNull(ad.Attributes[0].ArgumentList.Arguments[0].Expression);
            Assert.Equal("c", ad.Attributes[0].ArgumentList.Arguments[0].Expression.ToString());
            Assert.NotEqual(default, ad.Attributes[0].ArgumentList.CloseParenToken);
            Assert.NotEqual(default, ad.CloseBracketToken);
        }

        [Fact]
        public void TestGlobalAttributeWithMultipleAttributes()
        {
            var text = "[assembly:a, b]";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.AttributeList, file.AttributeLists[0].Kind());
            var ad = (AttributeListSyntax)file.AttributeLists[0];

            Assert.NotEqual(default, ad.OpenBracketToken);
            Assert.NotNull(ad.Target);
            Assert.NotEqual(default, ad.Target.Identifier);
            Assert.Equal("assembly", ad.Target.Identifier.ToString());
            Assert.NotEqual(default, ad.Target.ColonToken);
            Assert.Equal(2, ad.Attributes.Count);

            Assert.NotNull(ad.Attributes[0].Name);
            Assert.Equal("a", ad.Attributes[0].Name.ToString());
            Assert.Null(ad.Attributes[0].ArgumentList);

            Assert.NotNull(ad.Attributes[1].Name);
            Assert.Equal("b", ad.Attributes[1].Name.ToString());
            Assert.Null(ad.Attributes[1].ArgumentList);

            Assert.NotEqual(default, ad.CloseBracketToken);
        }

        [Fact]
        public void TestMultipleGlobalAttributeDeclarations()
        {
            var text = "[assembly:a] [assembly:b]";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(2, file.AttributeLists.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.AttributeList, file.AttributeLists[0].Kind());
            var ad = (AttributeListSyntax)file.AttributeLists[0];
            Assert.NotEqual(default, ad.OpenBracketToken);
            Assert.NotNull(ad.Target);
            Assert.NotEqual(default, ad.Target.Identifier);
            Assert.Equal("assembly", ad.Target.Identifier.ToString());
            Assert.NotEqual(default, ad.Target.ColonToken);
            Assert.Equal(1, ad.Attributes.Count);
            Assert.NotNull(ad.Attributes[0].Name);
            Assert.Equal("a", ad.Attributes[0].Name.ToString());
            Assert.Null(ad.Attributes[0].ArgumentList);
            Assert.NotEqual(default, ad.CloseBracketToken);

            ad = (AttributeListSyntax)file.AttributeLists[1];
            Assert.NotEqual(default, ad.OpenBracketToken);
            Assert.NotNull(ad.Target);
            Assert.NotEqual(default, ad.Target.Identifier);
            Assert.Equal("assembly", ad.Target.Identifier.ToString());
            Assert.NotEqual(default, ad.Target.ColonToken);
            Assert.Equal(1, ad.Attributes.Count);
            Assert.NotNull(ad.Attributes[0].Name);
            Assert.Equal("b", ad.Attributes[0].Name.ToString());
            Assert.Null(ad.Attributes[0].ArgumentList);
            Assert.NotEqual(default, ad.CloseBracketToken);
        }

        [Fact]
        public void TestNamespace()
        {
            var text = "namespace a { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.NamespaceDeclaration, file.Members[0].Kind());
            var ns = (NamespaceDeclarationSyntax)file.Members[0];
            Assert.NotEqual(default, ns.NamespaceKeyword);
            Assert.NotNull(ns.Name);
            Assert.Equal("a", ns.Name.ToString());
            Assert.NotEqual(default, ns.OpenBraceToken);
            Assert.Equal(0, ns.Usings.Count);
            Assert.Equal(0, ns.Members.Count);
            Assert.NotEqual(default, ns.CloseBraceToken);
        }

        [Fact]
        public void TestNamespaceWithDottedName()
        {
            var text = "namespace a.b.c { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.NamespaceDeclaration, file.Members[0].Kind());
            var ns = (NamespaceDeclarationSyntax)file.Members[0];
            Assert.NotEqual(default, ns.NamespaceKeyword);
            Assert.NotNull(ns.Name);
            Assert.Equal("a.b.c", ns.Name.ToString());
            Assert.NotEqual(default, ns.OpenBraceToken);
            Assert.Equal(0, ns.Usings.Count);
            Assert.Equal(0, ns.Members.Count);
            Assert.NotEqual(default, ns.CloseBraceToken);
        }

        [Fact]
        public void TestNamespaceWithUsing()
        {
            var text = "namespace a { using b.c; }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.NamespaceDeclaration, file.Members[0].Kind());
            var ns = (NamespaceDeclarationSyntax)file.Members[0];
            Assert.NotEqual(default, ns.NamespaceKeyword);
            Assert.NotNull(ns.Name);
            Assert.Equal("a", ns.Name.ToString());
            Assert.NotEqual(default, ns.OpenBraceToken);
            Assert.Equal(1, ns.Usings.Count);
            Assert.Equal("using b.c;", ns.Usings[0].ToString());
            Assert.Equal(0, ns.Members.Count);
            Assert.NotEqual(default, ns.CloseBraceToken);
        }

        [Fact]
        public void TestNamespaceWithExternAlias()
        {
            var text = "namespace a { extern alias b; }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.NamespaceDeclaration, file.Members[0].Kind());
            var ns = (NamespaceDeclarationSyntax)file.Members[0];
            Assert.NotEqual(default, ns.NamespaceKeyword);
            Assert.NotNull(ns.Name);
            Assert.Equal("a", ns.Name.ToString());
            Assert.NotEqual(default, ns.OpenBraceToken);
            Assert.Equal(1, ns.Externs.Count);
            Assert.Equal("extern alias b;", ns.Externs[0].ToString());
            Assert.Equal(0, ns.Members.Count);
            Assert.NotEqual(default, ns.CloseBraceToken);
        }

        [Fact]
        public void TestNamespaceWithExternAliasFollowingUsingBad()
        {
            var text = "namespace a { using b; extern alias c; }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Errors().Length);

            Assert.Equal(SyntaxKind.NamespaceDeclaration, file.Members[0].Kind());
            var ns = (NamespaceDeclarationSyntax)file.Members[0];
            Assert.NotEqual(default, ns.NamespaceKeyword);
            Assert.NotNull(ns.Name);
            Assert.Equal("a", ns.Name.ToString());
            Assert.NotEqual(default, ns.OpenBraceToken);
            Assert.Equal(1, ns.Usings.Count);
            Assert.Equal("using b;", ns.Usings[0].ToString());
            Assert.Equal(0, ns.Externs.Count);
            Assert.Equal(0, ns.Members.Count);
            Assert.NotEqual(default, ns.CloseBraceToken);
        }

        [Fact]
        public void TestNamespaceWithNestedNamespace()
        {
            var text = "namespace a { namespace b { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.NamespaceDeclaration, file.Members[0].Kind());
            var ns = (NamespaceDeclarationSyntax)file.Members[0];
            Assert.NotEqual(default, ns.NamespaceKeyword);
            Assert.NotNull(ns.Name);
            Assert.Equal("a", ns.Name.ToString());
            Assert.NotEqual(default, ns.OpenBraceToken);
            Assert.Equal(0, ns.Usings.Count);
            Assert.Equal(1, ns.Members.Count);
            Assert.Equal(SyntaxKind.NamespaceDeclaration, ns.Members[0].Kind());
            var ns2 = (NamespaceDeclarationSyntax)ns.Members[0];
            Assert.NotEqual(default, ns2.NamespaceKeyword);
            Assert.NotNull(ns2.Name);
            Assert.Equal("b", ns2.Name.ToString());
            Assert.NotEqual(default, ns2.OpenBraceToken);
            Assert.Equal(0, ns2.Usings.Count);
            Assert.Equal(0, ns2.Members.Count);

            Assert.NotEqual(default, ns.CloseBraceToken);
        }

        [Fact]
        public void TestClass()
        {
            var text = "class a { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.Equal(0, cs.Members.Count);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestClassWithPublic()
        {
            var text = "public class a { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(1, cs.Modifiers.Count);
            Assert.Equal(SyntaxKind.PublicKeyword, cs.Modifiers[0].Kind());
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.Equal(0, cs.Members.Count);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestClassWithInternal()
        {
            var text = "internal class a { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(1, cs.Modifiers.Count);
            Assert.Equal(SyntaxKind.InternalKeyword, cs.Modifiers[0].Kind());
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.Equal(0, cs.Members.Count);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestClassWithStatic()
        {
            var text = "static class a { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(1, cs.Modifiers.Count);
            Assert.Equal(SyntaxKind.StaticKeyword, cs.Modifiers[0].Kind());
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.Equal(0, cs.Members.Count);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestClassWithSealed()
        {
            var text = "sealed class a { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(1, cs.Modifiers.Count);
            Assert.Equal(SyntaxKind.SealedKeyword, cs.Modifiers[0].Kind());
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.Equal(0, cs.Members.Count);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestClassWithAbstract()
        {
            var text = "abstract class a { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(1, cs.Modifiers.Count);
            Assert.Equal(SyntaxKind.AbstractKeyword, cs.Modifiers[0].Kind());
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.Equal(0, cs.Members.Count);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestClassWithPartial()
        {
            var text = "partial class a { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(1, cs.Modifiers.Count);
            Assert.Equal(SyntaxKind.PartialKeyword, cs.Modifiers[0].Kind());
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.Equal(0, cs.Members.Count);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestClassWithAttribute()
        {
            var text = "[attr] class a { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, cs.AttributeLists.Count);
            Assert.Equal("[attr]", cs.AttributeLists[0].ToString());
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.Equal(0, cs.Members.Count);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestClassWithMultipleAttributes()
        {
            var text = "[attr1] [attr2] class a { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, cs.AttributeLists.Count);
            Assert.Equal("[attr1]", cs.AttributeLists[0].ToString());
            Assert.Equal("[attr2]", cs.AttributeLists[1].ToString());
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.Equal(0, cs.Members.Count);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestClassWithMultipleAttributesInAList()
        {
            var text = "[attr1, attr2] class a { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, cs.AttributeLists.Count);
            Assert.Equal("[attr1, attr2]", cs.AttributeLists[0].ToString());
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.Equal(0, cs.Members.Count);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestClassWithBaseType()
        {
            var text = "class a : b { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());

            Assert.NotNull(cs.BaseList);
            Assert.NotEqual(default, cs.BaseList.ColonToken);
            Assert.Equal(1, cs.BaseList.Types.Count);
            Assert.Equal("b", cs.BaseList.Types[0].Type.ToString());

            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.Equal(0, cs.Members.Count);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestClassWithMultipleBases()
        {
            var text = "class a : b, c { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());

            Assert.NotNull(cs.BaseList);
            Assert.NotEqual(default, cs.BaseList.ColonToken);
            Assert.Equal(2, cs.BaseList.Types.Count);
            Assert.Equal("b", cs.BaseList.Types[0].Type.ToString());
            Assert.Equal("c", cs.BaseList.Types[1].Type.ToString());

            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.Equal(0, cs.Members.Count);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestClassWithTypeConstraintBound()
        {
            var text = "class a<b> where b : c { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Equal("<b>", cs.TypeParameterList.ToString());

            Assert.Null(cs.BaseList);

            Assert.Equal(1, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.ConstraintClauses[0].WhereKeyword);
            Assert.NotNull(cs.ConstraintClauses[0].Name);
            Assert.Equal("b", cs.ConstraintClauses[0].Name.ToString());
            Assert.NotEqual(default, cs.ConstraintClauses[0].ColonToken);
            Assert.False(cs.ConstraintClauses[0].ColonToken.IsMissing);
            Assert.Equal(1, cs.ConstraintClauses[0].Constraints.Count);
            Assert.Equal(SyntaxKind.TypeConstraint, cs.ConstraintClauses[0].Constraints[0].Kind());
            var bound = (TypeConstraintSyntax)cs.ConstraintClauses[0].Constraints[0];
            Assert.NotNull(bound.Type);
            Assert.Equal("c", bound.Type.ToString());

            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.Equal(0, cs.Members.Count);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestNonGenericClassWithTypeConstraintBound()
        {
            var text = "class a where b : c { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());

            var errors = file.Errors();
            Assert.Equal(0, errors.Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());

            Assert.Null(cs.BaseList);

            Assert.Equal(1, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.ConstraintClauses[0].WhereKeyword);
            Assert.NotNull(cs.ConstraintClauses[0].Name);
            Assert.Equal("b", cs.ConstraintClauses[0].Name.ToString());
            Assert.NotEqual(default, cs.ConstraintClauses[0].ColonToken);
            Assert.False(cs.ConstraintClauses[0].ColonToken.IsMissing);
            Assert.Equal(1, cs.ConstraintClauses[0].Constraints.Count);
            Assert.Equal(SyntaxKind.TypeConstraint, cs.ConstraintClauses[0].Constraints[0].Kind());
            var bound = (TypeConstraintSyntax)cs.ConstraintClauses[0].Constraints[0];
            Assert.NotNull(bound.Type);
            Assert.Equal("c", bound.Type.ToString());

            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.Equal(0, cs.Members.Count);
            Assert.NotEqual(default, cs.CloseBraceToken);

            CreateCompilation(text).GetDeclarationDiagnostics().Verify(
                // (1,9): error CS0080: Constraints are not allowed on non-generic declarations
                // class a where b : c { }
                Diagnostic(ErrorCode.ERR_ConstraintOnlyAllowedOnGenericDecl, "where").WithLocation(1, 9));
        }

        [Fact]
        public void TestNonGenericMethodWithTypeConstraintBound()
        {
            var text = "class a { void M() where b : c { } }";

            CreateCompilation(text).GetDeclarationDiagnostics().Verify(
                // (1,20): error CS0080: Constraints are not allowed on non-generic declarations
                // class a { void M() where b : c { } }
                Diagnostic(ErrorCode.ERR_ConstraintOnlyAllowedOnGenericDecl, "where").WithLocation(1, 20));
        }

        [Fact]
        public void TestClassWithNewConstraintBound()
        {
            var text = "class a<b> where b : new() { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Equal("<b>", cs.TypeParameterList.ToString());

            Assert.Null(cs.BaseList);

            Assert.Equal(1, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.ConstraintClauses[0].WhereKeyword);
            Assert.NotNull(cs.ConstraintClauses[0].Name);
            Assert.Equal("b", cs.ConstraintClauses[0].Name.ToString());
            Assert.NotEqual(default, cs.ConstraintClauses[0].ColonToken);
            Assert.False(cs.ConstraintClauses[0].ColonToken.IsMissing);
            Assert.Equal(1, cs.ConstraintClauses[0].Constraints.Count);
            Assert.Equal(SyntaxKind.ConstructorConstraint, cs.ConstraintClauses[0].Constraints[0].Kind());
            var bound = (ConstructorConstraintSyntax)cs.ConstraintClauses[0].Constraints[0];
            Assert.NotEqual(default, bound.NewKeyword);
            Assert.False(bound.NewKeyword.IsMissing);
            Assert.NotEqual(default, bound.OpenParenToken);
            Assert.False(bound.OpenParenToken.IsMissing);
            Assert.NotEqual(default, bound.CloseParenToken);
            Assert.False(bound.CloseParenToken.IsMissing);

            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.Equal(0, cs.Members.Count);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestClassWithClassConstraintBound()
        {
            var text = "class a<b> where b : class { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Equal("<b>", cs.TypeParameterList.ToString());

            Assert.Null(cs.BaseList);

            Assert.Equal(1, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.ConstraintClauses[0].WhereKeyword);
            Assert.NotNull(cs.ConstraintClauses[0].Name);
            Assert.Equal("b", cs.ConstraintClauses[0].Name.ToString());
            Assert.NotEqual(default, cs.ConstraintClauses[0].ColonToken);
            Assert.False(cs.ConstraintClauses[0].ColonToken.IsMissing);
            Assert.Equal(1, cs.ConstraintClauses[0].Constraints.Count);
            Assert.Equal(SyntaxKind.ClassConstraint, cs.ConstraintClauses[0].Constraints[0].Kind());
            var bound = (ClassOrStructConstraintSyntax)cs.ConstraintClauses[0].Constraints[0];
            Assert.NotEqual(default, bound.ClassOrStructKeyword);
            Assert.False(bound.ClassOrStructKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ClassKeyword, bound.ClassOrStructKeyword.Kind());

            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.Equal(0, cs.Members.Count);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestClassWithStructConstraintBound()
        {
            var text = "class a<b> where b : struct { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Equal("<b>", cs.TypeParameterList.ToString());

            Assert.Null(cs.BaseList);

            Assert.Equal(1, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.ConstraintClauses[0].WhereKeyword);
            Assert.NotNull(cs.ConstraintClauses[0].Name);
            Assert.Equal("b", cs.ConstraintClauses[0].Name.ToString());
            Assert.NotEqual(default, cs.ConstraintClauses[0].ColonToken);
            Assert.False(cs.ConstraintClauses[0].ColonToken.IsMissing);
            Assert.Equal(1, cs.ConstraintClauses[0].Constraints.Count);
            Assert.Equal(SyntaxKind.StructConstraint, cs.ConstraintClauses[0].Constraints[0].Kind());
            var bound = (ClassOrStructConstraintSyntax)cs.ConstraintClauses[0].Constraints[0];
            Assert.NotEqual(default, bound.ClassOrStructKeyword);
            Assert.False(bound.ClassOrStructKeyword.IsMissing);
            Assert.Equal(SyntaxKind.StructKeyword, bound.ClassOrStructKeyword.Kind());

            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.Equal(0, cs.Members.Count);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestClassWithMultipleConstraintBounds()
        {
            var text = "class a<b> where b : class, c, new() { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Equal("<b>", cs.TypeParameterList.ToString());

            Assert.Null(cs.BaseList);

            Assert.Equal(1, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.ConstraintClauses[0].WhereKeyword);
            Assert.NotNull(cs.ConstraintClauses[0].Name);
            Assert.Equal("b", cs.ConstraintClauses[0].Name.ToString());
            Assert.NotEqual(default, cs.ConstraintClauses[0].ColonToken);
            Assert.False(cs.ConstraintClauses[0].ColonToken.IsMissing);
            Assert.Equal(3, cs.ConstraintClauses[0].Constraints.Count);

            Assert.Equal(SyntaxKind.ClassConstraint, cs.ConstraintClauses[0].Constraints[0].Kind());
            var classBound = (ClassOrStructConstraintSyntax)cs.ConstraintClauses[0].Constraints[0];
            Assert.NotEqual(default, classBound.ClassOrStructKeyword);
            Assert.False(classBound.ClassOrStructKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ClassKeyword, classBound.ClassOrStructKeyword.Kind());

            Assert.Equal(SyntaxKind.TypeConstraint, cs.ConstraintClauses[0].Constraints[1].Kind());
            var typeBound = (TypeConstraintSyntax)cs.ConstraintClauses[0].Constraints[1];
            Assert.NotNull(typeBound.Type);
            Assert.Equal("c", typeBound.Type.ToString());

            Assert.Equal(SyntaxKind.ConstructorConstraint, cs.ConstraintClauses[0].Constraints[2].Kind());
            var bound = (ConstructorConstraintSyntax)cs.ConstraintClauses[0].Constraints[2];
            Assert.NotEqual(default, bound.NewKeyword);
            Assert.False(bound.NewKeyword.IsMissing);
            Assert.NotEqual(default, bound.OpenParenToken);
            Assert.False(bound.OpenParenToken.IsMissing);
            Assert.NotEqual(default, bound.CloseParenToken);
            Assert.False(bound.CloseParenToken.IsMissing);

            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.Equal(0, cs.Members.Count);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestClassWithMultipleConstraints()
        {
            var text = "class a<b> where b : c where b : new() { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("<b>", cs.TypeParameterList.ToString());

            Assert.Null(cs.BaseList);

            Assert.Equal(2, cs.ConstraintClauses.Count);

            Assert.NotEqual(default, cs.ConstraintClauses[0].WhereKeyword);
            Assert.NotNull(cs.ConstraintClauses[0].Name);
            Assert.Equal("b", cs.ConstraintClauses[0].Name.ToString());
            Assert.NotEqual(default, cs.ConstraintClauses[0].ColonToken);
            Assert.False(cs.ConstraintClauses[0].ColonToken.IsMissing);
            Assert.Equal(1, cs.ConstraintClauses[0].Constraints.Count);
            Assert.Equal(SyntaxKind.TypeConstraint, cs.ConstraintClauses[0].Constraints[0].Kind());
            var typeBound = (TypeConstraintSyntax)cs.ConstraintClauses[0].Constraints[0];
            Assert.NotNull(typeBound.Type);
            Assert.Equal("c", typeBound.Type.ToString());

            Assert.NotEqual(default, cs.ConstraintClauses[1].WhereKeyword);
            Assert.NotNull(cs.ConstraintClauses[1].Name);
            Assert.Equal("b", cs.ConstraintClauses[1].Name.ToString());
            Assert.NotEqual(default, cs.ConstraintClauses[1].ColonToken);
            Assert.False(cs.ConstraintClauses[1].ColonToken.IsMissing);
            Assert.Equal(1, cs.ConstraintClauses[1].Constraints.Count);
            Assert.Equal(SyntaxKind.ConstructorConstraint, cs.ConstraintClauses[1].Constraints[0].Kind());
            var bound = (ConstructorConstraintSyntax)cs.ConstraintClauses[1].Constraints[0];
            Assert.NotEqual(default, bound.NewKeyword);
            Assert.False(bound.NewKeyword.IsMissing);
            Assert.NotEqual(default, bound.OpenParenToken);
            Assert.False(bound.OpenParenToken.IsMissing);
            Assert.NotEqual(default, bound.CloseParenToken);
            Assert.False(bound.CloseParenToken.IsMissing);

            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.Equal(0, cs.Members.Count);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestClassWithMultipleConstraints001()
        {
            var text = "class a<b> where b : c where b { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(2, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("<b>", cs.TypeParameterList.ToString());

            Assert.Null(cs.BaseList);

            Assert.Equal(2, cs.ConstraintClauses.Count);

            Assert.NotEqual(default, cs.ConstraintClauses[0].WhereKeyword);
            Assert.NotNull(cs.ConstraintClauses[0].Name);
            Assert.Equal("b", cs.ConstraintClauses[0].Name.ToString());
            Assert.NotEqual(default, cs.ConstraintClauses[0].ColonToken);
            Assert.False(cs.ConstraintClauses[0].ColonToken.IsMissing);
            Assert.Equal(1, cs.ConstraintClauses[0].Constraints.Count);
            Assert.Equal(SyntaxKind.TypeConstraint, cs.ConstraintClauses[0].Constraints[0].Kind());
            var typeBound = (TypeConstraintSyntax)cs.ConstraintClauses[0].Constraints[0];
            Assert.NotNull(typeBound.Type);
            Assert.Equal("c", typeBound.Type.ToString());

            Assert.NotEqual(default, cs.ConstraintClauses[1].WhereKeyword);
            Assert.NotNull(cs.ConstraintClauses[1].Name);
            Assert.Equal("b", cs.ConstraintClauses[1].Name.ToString());
            Assert.NotEqual(default, cs.ConstraintClauses[1].ColonToken);
            Assert.True(cs.ConstraintClauses[1].ColonToken.IsMissing);
            Assert.Equal(1, cs.ConstraintClauses[1].Constraints.Count);
            Assert.Equal(SyntaxKind.TypeConstraint, cs.ConstraintClauses[1].Constraints[0].Kind());
            var bound = (TypeConstraintSyntax)cs.ConstraintClauses[1].Constraints[0];
            Assert.True(bound.Type.IsMissing);
        }

        [Fact]
        public void TestClassWithMultipleConstraints002()
        {
            var text = "class a<b> where b : c where { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(3, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("<b>", cs.TypeParameterList.ToString());

            Assert.Null(cs.BaseList);

            Assert.Equal(2, cs.ConstraintClauses.Count);

            Assert.NotEqual(default, cs.ConstraintClauses[0].WhereKeyword);
            Assert.NotNull(cs.ConstraintClauses[0].Name);
            Assert.Equal("b", cs.ConstraintClauses[0].Name.ToString());
            Assert.NotEqual(default, cs.ConstraintClauses[0].ColonToken);
            Assert.False(cs.ConstraintClauses[0].ColonToken.IsMissing);
            Assert.Equal(1, cs.ConstraintClauses[0].Constraints.Count);
            Assert.Equal(SyntaxKind.TypeConstraint, cs.ConstraintClauses[0].Constraints[0].Kind());
            var typeBound = (TypeConstraintSyntax)cs.ConstraintClauses[0].Constraints[0];
            Assert.NotNull(typeBound.Type);
            Assert.Equal("c", typeBound.Type.ToString());

            Assert.NotEqual(default, cs.ConstraintClauses[1].WhereKeyword);
            Assert.True(cs.ConstraintClauses[1].Name.IsMissing);
            Assert.True(cs.ConstraintClauses[1].ColonToken.IsMissing);
            Assert.Equal(1, cs.ConstraintClauses[1].Constraints.Count);
            Assert.Equal(SyntaxKind.TypeConstraint, cs.ConstraintClauses[1].Constraints[0].Kind());
            var bound = (TypeConstraintSyntax)cs.ConstraintClauses[1].Constraints[0];
            Assert.True(bound.Type.IsMissing);
        }

        [Fact]
        public void TestClassWithMultipleBasesAndConstraints()
        {
            var text = "class a<b> : c, d where b : class, e, new() { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Equal("<b>", cs.TypeParameterList.ToString());

            Assert.NotNull(cs.BaseList);
            Assert.NotEqual(default, cs.BaseList.ColonToken);
            Assert.Equal(2, cs.BaseList.Types.Count);
            Assert.Equal("c", cs.BaseList.Types[0].Type.ToString());
            Assert.Equal("d", cs.BaseList.Types[1].Type.ToString());

            Assert.Equal(1, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.ConstraintClauses[0].WhereKeyword);
            Assert.NotNull(cs.ConstraintClauses[0].Name);
            Assert.Equal("b", cs.ConstraintClauses[0].Name.ToString());
            Assert.NotEqual(default, cs.ConstraintClauses[0].ColonToken);
            Assert.False(cs.ConstraintClauses[0].ColonToken.IsMissing);
            Assert.Equal(3, cs.ConstraintClauses[0].Constraints.Count);

            Assert.Equal(SyntaxKind.ClassConstraint, cs.ConstraintClauses[0].Constraints[0].Kind());
            var classBound = (ClassOrStructConstraintSyntax)cs.ConstraintClauses[0].Constraints[0];
            Assert.NotEqual(default, classBound.ClassOrStructKeyword);
            Assert.False(classBound.ClassOrStructKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ClassKeyword, classBound.ClassOrStructKeyword.Kind());

            Assert.Equal(SyntaxKind.TypeConstraint, cs.ConstraintClauses[0].Constraints[1].Kind());
            var typeBound = (TypeConstraintSyntax)cs.ConstraintClauses[0].Constraints[1];
            Assert.NotNull(typeBound.Type);
            Assert.Equal("e", typeBound.Type.ToString());

            Assert.Equal(SyntaxKind.ConstructorConstraint, cs.ConstraintClauses[0].Constraints[2].Kind());
            var bound = (ConstructorConstraintSyntax)cs.ConstraintClauses[0].Constraints[2];
            Assert.NotEqual(default, bound.NewKeyword);
            Assert.False(bound.NewKeyword.IsMissing);
            Assert.NotEqual(default, bound.OpenParenToken);
            Assert.False(bound.OpenParenToken.IsMissing);
            Assert.NotEqual(default, bound.CloseParenToken);
            Assert.False(bound.CloseParenToken.IsMissing);

            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.Equal(0, cs.Members.Count);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestInterface()
        {
            var text = "interface a { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.InterfaceDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.InterfaceKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestGenericInterface()
        {
            var text = "interface A<B> { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.InterfaceDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.InterfaceKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            var gn = cs.TypeParameterList;
            Assert.Equal("<B>", gn.ToString());
            Assert.Equal("A", cs.Identifier.ToString());
            Assert.Equal(0, gn.Parameters[0].AttributeLists.Count);
            Assert.Equal(SyntaxKind.None, gn.Parameters[0].VarianceKeyword.Kind());
            Assert.Equal("B", gn.Parameters[0].Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestGenericInterfaceWithAttributesAndVariance()
        {
            var text = "interface A<[B] out C> { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.InterfaceDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.InterfaceKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);

            var gn = cs.TypeParameterList;
            Assert.Equal("<[B] out C>", gn.ToString());
            Assert.Equal("A", cs.Identifier.ToString());
            Assert.Equal(1, gn.Parameters[0].AttributeLists.Count);
            Assert.Equal("B", gn.Parameters[0].AttributeLists[0].Attributes[0].Name.ToString());
            Assert.NotEqual(default, gn.Parameters[0].VarianceKeyword);
            Assert.Equal(SyntaxKind.OutKeyword, gn.Parameters[0].VarianceKeyword.Kind());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestStruct()
        {
            var text = "struct a { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.StructDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.StructKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestNestedClass()
        {
            var text = "class a { class b { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.ClassDeclaration, cs.Members[0].Kind());
            cs = (TypeDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("b", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestNestedPrivateClass()
        {
            var text = "class a { private class b { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.ClassDeclaration, cs.Members[0].Kind());
            cs = (TypeDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(1, cs.Modifiers.Count);
            Assert.Equal(SyntaxKind.PrivateKeyword, cs.Modifiers[0].Kind());
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("b", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestNestedProtectedClass()
        {
            var text = "class a { protected class b { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.ClassDeclaration, cs.Members[0].Kind());
            cs = (TypeDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(1, cs.Modifiers.Count);
            Assert.Equal(SyntaxKind.ProtectedKeyword, cs.Modifiers[0].Kind());
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("b", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestNestedProtectedInternalClass()
        {
            var text = "class a { protected internal class b { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.ClassDeclaration, cs.Members[0].Kind());
            cs = (TypeDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(2, cs.Modifiers.Count);
            Assert.Equal(SyntaxKind.ProtectedKeyword, cs.Modifiers[0].Kind());
            Assert.Equal(SyntaxKind.InternalKeyword, cs.Modifiers[1].Kind());
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("b", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestNestedInternalProtectedClass()
        {
            var text = "class a { internal protected class b { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.ClassDeclaration, cs.Members[0].Kind());
            cs = (TypeDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(2, cs.Modifiers.Count);
            Assert.Equal(SyntaxKind.InternalKeyword, cs.Modifiers[0].Kind());
            Assert.Equal(SyntaxKind.ProtectedKeyword, cs.Modifiers[1].Kind());
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("b", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestNestedPublicClass()
        {
            var text = "class a { public class b { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.ClassDeclaration, cs.Members[0].Kind());
            cs = (TypeDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(1, cs.Modifiers.Count);
            Assert.Equal(SyntaxKind.PublicKeyword, cs.Modifiers[0].Kind());
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("b", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestNestedInternalClass()
        {
            var text = "class a { internal class b { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.ClassDeclaration, cs.Members[0].Kind());
            cs = (TypeDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(1, cs.Modifiers.Count);
            Assert.Equal(SyntaxKind.InternalKeyword, cs.Modifiers[0].Kind());
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("b", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);
        }

        [Fact]
        public void TestDelegate()
        {
            var text = "delegate a b();";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            var ds = (DelegateDeclarationSyntax)file.Members[0];
            Assert.NotEqual(default, ds.DelegateKeyword);
            Assert.NotNull(ds.ReturnType);
            Assert.Equal("a", ds.ReturnType.ToString());
            Assert.NotEqual(default, ds.Identifier);
            Assert.Equal("b", ds.Identifier.ToString());
            Assert.NotEqual(default, ds.ParameterList.OpenParenToken);
            Assert.False(ds.ParameterList.OpenParenToken.IsMissing);
            Assert.Equal(0, ds.ParameterList.Parameters.Count);
            Assert.NotEqual(default, ds.ParameterList.CloseParenToken);
            Assert.False(ds.ParameterList.CloseParenToken.IsMissing);
            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestDelegateWithRefReturnType()
        {
            var text = "delegate ref a b();";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            var ds = (DelegateDeclarationSyntax)file.Members[0];
            Assert.NotEqual(default, ds.DelegateKeyword);
            Assert.NotNull(ds.ReturnType);
            Assert.Equal("ref a", ds.ReturnType.ToString());
            Assert.NotEqual(default, ds.Identifier);
            Assert.Equal("b", ds.Identifier.ToString());
            Assert.NotEqual(default, ds.ParameterList.OpenParenToken);
            Assert.False(ds.ParameterList.OpenParenToken.IsMissing);
            Assert.Equal(0, ds.ParameterList.Parameters.Count);
            Assert.NotEqual(default, ds.ParameterList.CloseParenToken);
            Assert.False(ds.ParameterList.CloseParenToken.IsMissing);
            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public void TestDelegateWithRefReadonlyReturnType()
        {
            var text = "delegate ref readonly a b();";
            var file = this.ParseFile(text, TestOptions.Regular);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            var ds = (DelegateDeclarationSyntax)file.Members[0];
            Assert.NotEqual(default, ds.DelegateKeyword);
            Assert.NotNull(ds.ReturnType);
            Assert.Equal("ref readonly a", ds.ReturnType.ToString());
            Assert.NotEqual(default, ds.Identifier);
            Assert.Equal("b", ds.Identifier.ToString());
            Assert.NotEqual(default, ds.ParameterList.OpenParenToken);
            Assert.False(ds.ParameterList.OpenParenToken.IsMissing);
            Assert.Equal(0, ds.ParameterList.Parameters.Count);
            Assert.NotEqual(default, ds.ParameterList.CloseParenToken);
            Assert.False(ds.ParameterList.CloseParenToken.IsMissing);
            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestDelegateWithBuiltInReturnTypes()
        {
            TestDelegateWithBuiltInReturnType(SyntaxKind.VoidKeyword);
            TestDelegateWithBuiltInReturnType(SyntaxKind.BoolKeyword);
            TestDelegateWithBuiltInReturnType(SyntaxKind.SByteKeyword);
            TestDelegateWithBuiltInReturnType(SyntaxKind.IntKeyword);
            TestDelegateWithBuiltInReturnType(SyntaxKind.UIntKeyword);
            TestDelegateWithBuiltInReturnType(SyntaxKind.ShortKeyword);
            TestDelegateWithBuiltInReturnType(SyntaxKind.UShortKeyword);
            TestDelegateWithBuiltInReturnType(SyntaxKind.LongKeyword);
            TestDelegateWithBuiltInReturnType(SyntaxKind.ULongKeyword);
            TestDelegateWithBuiltInReturnType(SyntaxKind.FloatKeyword);
            TestDelegateWithBuiltInReturnType(SyntaxKind.DoubleKeyword);
            TestDelegateWithBuiltInReturnType(SyntaxKind.DecimalKeyword);
            TestDelegateWithBuiltInReturnType(SyntaxKind.StringKeyword);
            TestDelegateWithBuiltInReturnType(SyntaxKind.CharKeyword);
            TestDelegateWithBuiltInReturnType(SyntaxKind.ObjectKeyword);
        }

        private void TestDelegateWithBuiltInReturnType(SyntaxKind builtInType)
        {
            var typeText = SyntaxFacts.GetText(builtInType);
            var text = "delegate " + typeText + " b();";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            var ds = (DelegateDeclarationSyntax)file.Members[0];
            Assert.NotEqual(default, ds.DelegateKeyword);
            Assert.NotNull(ds.ReturnType);
            Assert.Equal(typeText, ds.ReturnType.ToString());
            Assert.NotEqual(default, ds.Identifier);
            Assert.Equal("b", ds.Identifier.ToString());
            Assert.NotEqual(default, ds.ParameterList.OpenParenToken);
            Assert.False(ds.ParameterList.OpenParenToken.IsMissing);
            Assert.Equal(0, ds.ParameterList.Parameters.Count);
            Assert.NotEqual(default, ds.ParameterList.CloseParenToken);
            Assert.False(ds.ParameterList.CloseParenToken.IsMissing);
            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestDelegateWithBuiltInParameterTypes()
        {
            TestDelegateWithBuiltInParameterType(SyntaxKind.BoolKeyword);
            TestDelegateWithBuiltInParameterType(SyntaxKind.SByteKeyword);
            TestDelegateWithBuiltInParameterType(SyntaxKind.IntKeyword);
            TestDelegateWithBuiltInParameterType(SyntaxKind.UIntKeyword);
            TestDelegateWithBuiltInParameterType(SyntaxKind.ShortKeyword);
            TestDelegateWithBuiltInParameterType(SyntaxKind.UShortKeyword);
            TestDelegateWithBuiltInParameterType(SyntaxKind.LongKeyword);
            TestDelegateWithBuiltInParameterType(SyntaxKind.ULongKeyword);
            TestDelegateWithBuiltInParameterType(SyntaxKind.FloatKeyword);
            TestDelegateWithBuiltInParameterType(SyntaxKind.DoubleKeyword);
            TestDelegateWithBuiltInParameterType(SyntaxKind.DecimalKeyword);
            TestDelegateWithBuiltInParameterType(SyntaxKind.StringKeyword);
            TestDelegateWithBuiltInParameterType(SyntaxKind.CharKeyword);
            TestDelegateWithBuiltInParameterType(SyntaxKind.ObjectKeyword);
        }

        private void TestDelegateWithBuiltInParameterType(SyntaxKind builtInType)
        {
            var typeText = SyntaxFacts.GetText(builtInType);
            var text = "delegate a b(" + typeText + " c);";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            var ds = (DelegateDeclarationSyntax)file.Members[0];
            Assert.NotEqual(default, ds.DelegateKeyword);
            Assert.NotNull(ds.ReturnType);
            Assert.Equal("a", ds.ReturnType.ToString());
            Assert.NotEqual(default, ds.Identifier);
            Assert.Equal("b", ds.Identifier.ToString());
            Assert.NotEqual(default, ds.ParameterList.OpenParenToken);
            Assert.False(ds.ParameterList.OpenParenToken.IsMissing);

            Assert.Equal(1, ds.ParameterList.Parameters.Count);
            Assert.Equal(0, ds.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal(0, ds.ParameterList.Parameters[0].Modifiers.Count);
            Assert.NotNull(ds.ParameterList.Parameters[0].Type);
            Assert.Equal(typeText, ds.ParameterList.Parameters[0].Type.ToString());
            Assert.NotEqual(default, ds.ParameterList.Parameters[0].Identifier);
            Assert.Equal("c", ds.ParameterList.Parameters[0].Identifier.ToString());

            Assert.NotEqual(default, ds.ParameterList.CloseParenToken);
            Assert.False(ds.ParameterList.CloseParenToken.IsMissing);
            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestDelegateWithParameter()
        {
            var text = "delegate a b(c d);";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            var ds = (DelegateDeclarationSyntax)file.Members[0];
            Assert.NotEqual(default, ds.DelegateKeyword);
            Assert.NotNull(ds.ReturnType);
            Assert.Equal("a", ds.ReturnType.ToString());
            Assert.NotEqual(default, ds.Identifier);
            Assert.Equal("b", ds.Identifier.ToString());
            Assert.NotEqual(default, ds.ParameterList.OpenParenToken);
            Assert.False(ds.ParameterList.OpenParenToken.IsMissing);

            Assert.Equal(1, ds.ParameterList.Parameters.Count);
            Assert.Equal(0, ds.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal(0, ds.ParameterList.Parameters[0].Modifiers.Count);
            Assert.NotNull(ds.ParameterList.Parameters[0].Type);
            Assert.Equal("c", ds.ParameterList.Parameters[0].Type.ToString());
            Assert.NotEqual(default, ds.ParameterList.Parameters[0].Identifier);
            Assert.Equal("d", ds.ParameterList.Parameters[0].Identifier.ToString());

            Assert.NotEqual(default, ds.ParameterList.CloseParenToken);
            Assert.False(ds.ParameterList.CloseParenToken.IsMissing);
            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestDelegateWithMultipleParameters()
        {
            var text = "delegate a b(c d, e f);";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            var ds = (DelegateDeclarationSyntax)file.Members[0];
            Assert.NotEqual(default, ds.DelegateKeyword);
            Assert.NotNull(ds.ReturnType);
            Assert.Equal("a", ds.ReturnType.ToString());
            Assert.NotEqual(default, ds.Identifier);
            Assert.Equal("b", ds.Identifier.ToString());
            Assert.NotEqual(default, ds.ParameterList.OpenParenToken);
            Assert.False(ds.ParameterList.OpenParenToken.IsMissing);

            Assert.Equal(2, ds.ParameterList.Parameters.Count);
            Assert.Equal(0, ds.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal(0, ds.ParameterList.Parameters[0].Modifiers.Count);
            Assert.NotNull(ds.ParameterList.Parameters[0].Type);
            Assert.Equal("c", ds.ParameterList.Parameters[0].Type.ToString());
            Assert.NotEqual(default, ds.ParameterList.Parameters[0].Identifier);
            Assert.Equal("d", ds.ParameterList.Parameters[0].Identifier.ToString());

            Assert.Equal(0, ds.ParameterList.Parameters[1].AttributeLists.Count);
            Assert.Equal(0, ds.ParameterList.Parameters[1].Modifiers.Count);
            Assert.NotNull(ds.ParameterList.Parameters[1].Type);
            Assert.Equal("e", ds.ParameterList.Parameters[1].Type.ToString());
            Assert.NotEqual(default, ds.ParameterList.Parameters[1].Identifier);
            Assert.Equal("f", ds.ParameterList.Parameters[1].Identifier.ToString());

            Assert.NotEqual(default, ds.ParameterList.CloseParenToken);
            Assert.False(ds.ParameterList.CloseParenToken.IsMissing);
            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestDelegateWithRefParameter()
        {
            var text = "delegate a b(ref c d);";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            var ds = (DelegateDeclarationSyntax)file.Members[0];
            Assert.NotEqual(default, ds.DelegateKeyword);
            Assert.NotNull(ds.ReturnType);
            Assert.Equal("a", ds.ReturnType.ToString());
            Assert.NotEqual(default, ds.Identifier);
            Assert.Equal("b", ds.Identifier.ToString());
            Assert.NotEqual(default, ds.ParameterList.OpenParenToken);
            Assert.False(ds.ParameterList.OpenParenToken.IsMissing);

            Assert.Equal(1, ds.ParameterList.Parameters.Count);
            Assert.Equal(0, ds.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal(1, ds.ParameterList.Parameters[0].Modifiers.Count);
            Assert.Equal(SyntaxKind.RefKeyword, ds.ParameterList.Parameters[0].Modifiers[0].Kind());
            Assert.NotNull(ds.ParameterList.Parameters[0].Type);
            Assert.Equal("c", ds.ParameterList.Parameters[0].Type.ToString());
            Assert.NotEqual(default, ds.ParameterList.Parameters[0].Identifier);
            Assert.Equal("d", ds.ParameterList.Parameters[0].Identifier.ToString());

            Assert.NotEqual(default, ds.ParameterList.CloseParenToken);
            Assert.False(ds.ParameterList.CloseParenToken.IsMissing);
            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestDelegateWithOutParameter()
        {
            var text = "delegate a b(out c d);";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            var ds = (DelegateDeclarationSyntax)file.Members[0];
            Assert.NotEqual(default, ds.DelegateKeyword);
            Assert.NotNull(ds.ReturnType);
            Assert.Equal("a", ds.ReturnType.ToString());
            Assert.NotEqual(default, ds.Identifier);
            Assert.Equal("b", ds.Identifier.ToString());
            Assert.NotEqual(default, ds.ParameterList.OpenParenToken);
            Assert.False(ds.ParameterList.OpenParenToken.IsMissing);

            Assert.Equal(1, ds.ParameterList.Parameters.Count);
            Assert.Equal(0, ds.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal(1, ds.ParameterList.Parameters[0].Modifiers.Count);
            Assert.Equal(SyntaxKind.OutKeyword, ds.ParameterList.Parameters[0].Modifiers[0].Kind());
            Assert.NotNull(ds.ParameterList.Parameters[0].Type);
            Assert.Equal("c", ds.ParameterList.Parameters[0].Type.ToString());
            Assert.NotEqual(default, ds.ParameterList.Parameters[0].Identifier);
            Assert.Equal("d", ds.ParameterList.Parameters[0].Identifier.ToString());

            Assert.NotEqual(default, ds.ParameterList.CloseParenToken);
            Assert.False(ds.ParameterList.CloseParenToken.IsMissing);
            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestDelegateWithParamsParameter()
        {
            var text = "delegate a b(params c d);";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            var ds = (DelegateDeclarationSyntax)file.Members[0];
            Assert.NotEqual(default, ds.DelegateKeyword);
            Assert.NotNull(ds.ReturnType);
            Assert.Equal("a", ds.ReturnType.ToString());
            Assert.NotEqual(default, ds.Identifier);
            Assert.Equal("b", ds.Identifier.ToString());
            Assert.NotEqual(default, ds.ParameterList.OpenParenToken);
            Assert.False(ds.ParameterList.OpenParenToken.IsMissing);

            Assert.Equal(1, ds.ParameterList.Parameters.Count);
            Assert.Equal(0, ds.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal(1, ds.ParameterList.Parameters[0].Modifiers.Count);
            Assert.Equal(SyntaxKind.ParamsKeyword, ds.ParameterList.Parameters[0].Modifiers[0].Kind());
            Assert.NotNull(ds.ParameterList.Parameters[0].Type);
            Assert.Equal("c", ds.ParameterList.Parameters[0].Type.ToString());
            Assert.NotEqual(default, ds.ParameterList.Parameters[0].Identifier);
            Assert.Equal("d", ds.ParameterList.Parameters[0].Identifier.ToString());

            Assert.NotEqual(default, ds.ParameterList.CloseParenToken);
            Assert.False(ds.ParameterList.CloseParenToken.IsMissing);
            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestDelegateWithArgListParameter()
        {
            var text = "delegate a b(__arglist);";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            var errors = file.Errors();
            Assert.Equal(0, errors.Length);

            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            var ds = (DelegateDeclarationSyntax)file.Members[0];
            Assert.NotEqual(default, ds.DelegateKeyword);
            Assert.NotNull(ds.ReturnType);
            Assert.Equal("a", ds.ReturnType.ToString());
            Assert.NotEqual(default, ds.Identifier);
            Assert.Equal("b", ds.Identifier.ToString());
            Assert.NotEqual(default, ds.ParameterList.OpenParenToken);
            Assert.False(ds.ParameterList.OpenParenToken.IsMissing);

            Assert.Equal(1, ds.ParameterList.Parameters.Count);
            Assert.Equal(0, ds.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal(0, ds.ParameterList.Parameters[0].Modifiers.Count);
            Assert.Null(ds.ParameterList.Parameters[0].Type);
            Assert.NotEqual(default, ds.ParameterList.Parameters[0].Identifier);

            Assert.NotEqual(default, ds.ParameterList.CloseParenToken);
            Assert.False(ds.ParameterList.CloseParenToken.IsMissing);
            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestDelegateWithParameterAttribute()
        {
            var text = "delegate a b([attr] c d);";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            var ds = (DelegateDeclarationSyntax)file.Members[0];
            Assert.NotEqual(default, ds.DelegateKeyword);
            Assert.NotNull(ds.ReturnType);
            Assert.Equal("a", ds.ReturnType.ToString());
            Assert.NotEqual(default, ds.Identifier);
            Assert.Equal("b", ds.Identifier.ToString());
            Assert.NotEqual(default, ds.ParameterList.OpenParenToken);
            Assert.False(ds.ParameterList.OpenParenToken.IsMissing);

            Assert.Equal(1, ds.ParameterList.Parameters.Count);
            Assert.Equal(1, ds.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal("[attr]", ds.ParameterList.Parameters[0].AttributeLists[0].ToString());
            Assert.Equal(0, ds.ParameterList.Parameters[0].Modifiers.Count);
            Assert.NotNull(ds.ParameterList.Parameters[0].Type);
            Assert.Equal("c", ds.ParameterList.Parameters[0].Type.ToString());
            Assert.NotEqual(default, ds.ParameterList.Parameters[0].Identifier);
            Assert.Equal("d", ds.ParameterList.Parameters[0].Identifier.ToString());

            Assert.NotEqual(default, ds.ParameterList.CloseParenToken);
            Assert.False(ds.ParameterList.CloseParenToken.IsMissing);
            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestNestedDelegate()
        {
            var text = "class a { delegate b c(); }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.DelegateDeclaration, cs.Members[0].Kind());
            var ds = (DelegateDeclarationSyntax)cs.Members[0];
            Assert.NotEqual(default, ds.DelegateKeyword);
            Assert.NotNull(ds.ReturnType);
            Assert.Equal("b", ds.ReturnType.ToString());
            Assert.NotEqual(default, ds.Identifier);
            Assert.Equal("c", ds.Identifier.ToString());
            Assert.NotEqual(default, ds.ParameterList.OpenParenToken);
            Assert.False(ds.ParameterList.OpenParenToken.IsMissing);
            Assert.Equal(0, ds.ParameterList.Parameters.Count);
            Assert.NotEqual(default, ds.ParameterList.CloseParenToken);
            Assert.False(ds.ParameterList.CloseParenToken.IsMissing);
            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestClassMethod()
        {
            var text = "class a { b X() { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.MethodDeclaration, cs.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ms.AttributeLists.Count);
            Assert.Equal(0, ms.Modifiers.Count);
            Assert.NotNull(ms.ReturnType);
            Assert.Equal("b", ms.ReturnType.ToString());
            Assert.NotEqual(default, ms.Identifier);
            Assert.Equal("X", ms.Identifier.ToString());
            Assert.NotEqual(default, ms.ParameterList.OpenParenToken);
            Assert.False(ms.ParameterList.OpenParenToken.IsMissing);
            Assert.Equal(0, ms.ParameterList.Parameters.Count);
            Assert.NotEqual(default, ms.ParameterList.CloseParenToken);
            Assert.False(ms.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(0, ms.ConstraintClauses.Count);
            Assert.NotNull(ms.Body);
            Assert.NotEqual(SyntaxKind.None, ms.Body.OpenBraceToken.Kind());
            Assert.NotEqual(SyntaxKind.None, ms.Body.CloseBraceToken.Kind());
            Assert.Equal(SyntaxKind.None, ms.SemicolonToken.Kind());
        }

        [Fact]
        public void TestClassMethodWithRefReturn()
        {
            var text = "class a { ref b X() { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.MethodDeclaration, cs.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ms.AttributeLists.Count);
            Assert.NotNull(ms.ReturnType);
            Assert.Equal("ref b", ms.ReturnType.ToString());
            Assert.NotEqual(default, ms.Identifier);
            Assert.Equal("X", ms.Identifier.ToString());
            Assert.NotEqual(default, ms.ParameterList.OpenParenToken);
            Assert.False(ms.ParameterList.OpenParenToken.IsMissing);
            Assert.Equal(0, ms.ParameterList.Parameters.Count);
            Assert.NotEqual(default, ms.ParameterList.CloseParenToken);
            Assert.False(ms.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(0, ms.ConstraintClauses.Count);
            Assert.NotNull(ms.Body);
            Assert.NotEqual(SyntaxKind.None, ms.Body.OpenBraceToken.Kind());
            Assert.NotEqual(SyntaxKind.None, ms.Body.CloseBraceToken.Kind());
            Assert.Equal(SyntaxKind.None, ms.SemicolonToken.Kind());
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public void TestClassMethodWithRefReadonlyReturn()
        {
            var text = "class a { ref readonly b X() { } }";
            var file = this.ParseFile(text, TestOptions.Regular);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.MethodDeclaration, cs.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ms.AttributeLists.Count);
            Assert.NotNull(ms.ReturnType);
            Assert.Equal("ref readonly b", ms.ReturnType.ToString());
            Assert.NotEqual(default, ms.Identifier);
            Assert.Equal("X", ms.Identifier.ToString());
            Assert.NotEqual(default, ms.ParameterList.OpenParenToken);
            Assert.False(ms.ParameterList.OpenParenToken.IsMissing);
            Assert.Equal(0, ms.ParameterList.Parameters.Count);
            Assert.NotEqual(default, ms.ParameterList.CloseParenToken);
            Assert.False(ms.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(0, ms.ConstraintClauses.Count);
            Assert.NotNull(ms.Body);
            Assert.NotEqual(SyntaxKind.None, ms.Body.OpenBraceToken.Kind());
            Assert.NotEqual(SyntaxKind.None, ms.Body.CloseBraceToken.Kind());
            Assert.Equal(SyntaxKind.None, ms.SemicolonToken.Kind());
        }

        [Fact]
        public void TestClassMethodWithRef()
        {
            var text = "class a { ref }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(1, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.IncompleteMember, cs.Members[0].Kind());
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public void TestClassMethodWithRefReadonly()
        {
            var text = "class a { ref readonly }";
            var file = this.ParseFile(text, parseOptions: TestOptions.Regular);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(1, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.IncompleteMember, cs.Members[0].Kind());
        }

        private void TestClassMethodModifiers(params SyntaxKind[] modifiers)
        {
            var text = "class a { " + string.Join(" ", modifiers.Select(SyntaxFacts.GetText)) + " b X() { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.MethodDeclaration, cs.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ms.AttributeLists.Count);
            Assert.Equal(modifiers.Length, ms.Modifiers.Count);
            for (int i = 0; i < modifiers.Length; ++i)
            {
                Assert.Equal(modifiers[i], ms.Modifiers[i].Kind());
            }
            Assert.NotNull(ms.ReturnType);
            Assert.Equal("b", ms.ReturnType.ToString());
            Assert.NotEqual(default, ms.Identifier);
            Assert.Equal("X", ms.Identifier.ToString());
            Assert.NotEqual(default, ms.ParameterList.OpenParenToken);
            Assert.False(ms.ParameterList.OpenParenToken.IsMissing);
            Assert.Equal(0, ms.ParameterList.Parameters.Count);
            Assert.NotEqual(default, ms.ParameterList.CloseParenToken);
            Assert.False(ms.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(0, ms.ConstraintClauses.Count);
            Assert.NotNull(ms.Body);
            Assert.NotEqual(SyntaxKind.None, ms.Body.OpenBraceToken.Kind());
            Assert.NotEqual(SyntaxKind.None, ms.Body.CloseBraceToken.Kind());
            Assert.Equal(SyntaxKind.None, ms.SemicolonToken.Kind());
        }

        [Fact]
        public void TestClassMethodAccessModes()
        {
            TestClassMethodModifiers(SyntaxKind.PublicKeyword);
            TestClassMethodModifiers(SyntaxKind.PrivateKeyword);
            TestClassMethodModifiers(SyntaxKind.InternalKeyword);
            TestClassMethodModifiers(SyntaxKind.ProtectedKeyword);
        }

        [Fact]
        public void TestClassMethodModifiersOrder()
        {
            TestClassMethodModifiers(SyntaxKind.PublicKeyword, SyntaxKind.VirtualKeyword);
            TestClassMethodModifiers(SyntaxKind.VirtualKeyword, SyntaxKind.PublicKeyword);
            TestClassMethodModifiers(SyntaxKind.InternalKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.VirtualKeyword);
            TestClassMethodModifiers(SyntaxKind.InternalKeyword, SyntaxKind.VirtualKeyword, SyntaxKind.ProtectedKeyword);
        }

        [Fact]
        public void TestClassMethodWithPartial()
        {
            var text = "class a { partial void M() { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.MethodDeclaration, cs.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ms.AttributeLists.Count);
            Assert.Equal(1, ms.Modifiers.Count);
            Assert.Equal(SyntaxKind.PartialKeyword, ms.Modifiers[0].Kind());
            Assert.NotNull(ms.ReturnType);
            Assert.Equal("void", ms.ReturnType.ToString());
            Assert.NotEqual(default, ms.Identifier);
            Assert.Equal("M", ms.Identifier.ToString());
            Assert.NotEqual(default, ms.ParameterList.OpenParenToken);
            Assert.False(ms.ParameterList.OpenParenToken.IsMissing);
            Assert.Equal(0, ms.ParameterList.Parameters.Count);
            Assert.NotEqual(default, ms.ParameterList.CloseParenToken);
            Assert.False(ms.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(0, ms.ConstraintClauses.Count);
            Assert.NotNull(ms.Body);
            Assert.NotEqual(SyntaxKind.None, ms.Body.OpenBraceToken.Kind());
            Assert.NotEqual(SyntaxKind.None, ms.Body.CloseBraceToken.Kind());
            Assert.Equal(SyntaxKind.None, ms.SemicolonToken.Kind());
        }

        [Fact]
        public void TestStructMethodWithReadonly()
        {
            var text = "struct a { readonly void M() { } }";
            var file = this.ParseFile(text, TestOptions.Regular);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.StructDeclaration, file.Members[0].Kind());
            var structDecl = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, structDecl.AttributeLists.Count);
            Assert.Equal(0, structDecl.Modifiers.Count);
            Assert.NotEqual(default, structDecl.Keyword);
            Assert.Equal(SyntaxKind.StructKeyword, structDecl.Keyword.Kind());
            Assert.NotEqual(default, structDecl.Identifier);
            Assert.Equal("a", structDecl.Identifier.ToString());
            Assert.Null(structDecl.BaseList);
            Assert.Equal(0, structDecl.ConstraintClauses.Count);
            Assert.NotEqual(default, structDecl.OpenBraceToken);
            Assert.NotEqual(default, structDecl.CloseBraceToken);

            Assert.Equal(1, structDecl.Members.Count);

            Assert.Equal(SyntaxKind.MethodDeclaration, structDecl.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)structDecl.Members[0];
            Assert.Equal(0, ms.AttributeLists.Count);
            Assert.Equal(1, ms.Modifiers.Count);
            Assert.Equal(SyntaxKind.ReadOnlyKeyword, ms.Modifiers[0].Kind());
            Assert.NotNull(ms.ReturnType);
            Assert.Equal("void", ms.ReturnType.ToString());
            Assert.NotEqual(default, ms.Identifier);
            Assert.Equal("M", ms.Identifier.ToString());
            Assert.NotEqual(default, ms.ParameterList.OpenParenToken);
            Assert.False(ms.ParameterList.OpenParenToken.IsMissing);
            Assert.Equal(0, ms.ParameterList.Parameters.Count);
            Assert.NotEqual(default, ms.ParameterList.CloseParenToken);
            Assert.False(ms.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(0, ms.ConstraintClauses.Count);
            Assert.NotNull(ms.Body);
            Assert.NotEqual(SyntaxKind.None, ms.Body.OpenBraceToken.Kind());
            Assert.NotEqual(SyntaxKind.None, ms.Body.CloseBraceToken.Kind());
            Assert.Equal(SyntaxKind.None, ms.SemicolonToken.Kind());
        }

        [Fact]
        public void TestReadOnlyRefReturning()
        {
            var text = "struct a { readonly ref readonly int M() { } }";
            var file = this.ParseFile(text, TestOptions.Regular);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.StructDeclaration, file.Members[0].Kind());
            var structDecl = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, structDecl.AttributeLists.Count);
            Assert.Equal(0, structDecl.Modifiers.Count);
            Assert.NotEqual(default, structDecl.Keyword);
            Assert.Equal(SyntaxKind.StructKeyword, structDecl.Keyword.Kind());
            Assert.NotEqual(default, structDecl.Identifier);
            Assert.Equal("a", structDecl.Identifier.ToString());
            Assert.Null(structDecl.BaseList);
            Assert.Equal(0, structDecl.ConstraintClauses.Count);
            Assert.NotEqual(default, structDecl.OpenBraceToken);
            Assert.NotEqual(default, structDecl.CloseBraceToken);

            Assert.Equal(1, structDecl.Members.Count);

            Assert.Equal(SyntaxKind.MethodDeclaration, structDecl.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)structDecl.Members[0];
            Assert.Equal(0, ms.AttributeLists.Count);
            Assert.Equal(1, ms.Modifiers.Count);
            Assert.Equal(SyntaxKind.ReadOnlyKeyword, ms.Modifiers[0].Kind());
            Assert.Equal(SyntaxKind.RefType, ms.ReturnType.Kind());
            var rt = (RefTypeSyntax)ms.ReturnType;
            Assert.Equal(SyntaxKind.RefKeyword, rt.RefKeyword.Kind());
            Assert.Equal(SyntaxKind.ReadOnlyKeyword, rt.ReadOnlyKeyword.Kind());
            Assert.Equal("int", rt.Type.ToString());
            Assert.NotEqual(default, ms.Identifier);
            Assert.Equal("M", ms.Identifier.ToString());
            Assert.NotEqual(default, ms.ParameterList.OpenParenToken);
            Assert.False(ms.ParameterList.OpenParenToken.IsMissing);
            Assert.Equal(0, ms.ParameterList.Parameters.Count);
            Assert.NotEqual(default, ms.ParameterList.CloseParenToken);
            Assert.False(ms.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(0, ms.ConstraintClauses.Count);
            Assert.NotNull(ms.Body);
            Assert.NotEqual(SyntaxKind.None, ms.Body.OpenBraceToken.Kind());
            Assert.NotEqual(SyntaxKind.None, ms.Body.CloseBraceToken.Kind());
            Assert.Equal(SyntaxKind.None, ms.SemicolonToken.Kind());
        }

        [Fact]
        public void TestStructExpressionPropertyWithReadonly()
        {
            var text = "struct a { readonly int M => 42; }";
            var file = this.ParseFile(text, TestOptions.Regular);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.StructDeclaration, file.Members[0].Kind());
            var structDecl = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, structDecl.AttributeLists.Count);
            Assert.Equal(0, structDecl.Modifiers.Count);
            Assert.NotEqual(default, structDecl.Keyword);
            Assert.Equal(SyntaxKind.StructKeyword, structDecl.Keyword.Kind());
            Assert.NotEqual(default, structDecl.Identifier);
            Assert.Equal("a", structDecl.Identifier.ToString());
            Assert.Null(structDecl.BaseList);
            Assert.Equal(0, structDecl.ConstraintClauses.Count);
            Assert.NotEqual(default, structDecl.OpenBraceToken);
            Assert.NotEqual(default, structDecl.CloseBraceToken);

            Assert.Equal(1, structDecl.Members.Count);

            Assert.Equal(SyntaxKind.PropertyDeclaration, structDecl.Members[0].Kind());
            var propertySyntax = (PropertyDeclarationSyntax)structDecl.Members[0];
            Assert.Equal(0, propertySyntax.AttributeLists.Count);
            Assert.Equal(1, propertySyntax.Modifiers.Count);
            Assert.Equal(SyntaxKind.ReadOnlyKeyword, propertySyntax.Modifiers[0].Kind());
            Assert.NotNull(propertySyntax.Type);
            Assert.Equal("int", propertySyntax.Type.ToString());
            Assert.NotEqual(default, propertySyntax.Identifier);
            Assert.Equal("M", propertySyntax.Identifier.ToString());
            Assert.NotNull(propertySyntax.ExpressionBody);
            Assert.NotEqual(SyntaxKind.None, propertySyntax.ExpressionBody.ArrowToken.Kind());
            Assert.NotNull(propertySyntax.ExpressionBody.Expression);
            Assert.Equal(SyntaxKind.SemicolonToken, propertySyntax.SemicolonToken.Kind());
        }

        [Fact]
        public void TestStructGetterPropertyWithReadonly()
        {
            var text = "struct a { int P { readonly get { return 42; } } }";
            var file = this.ParseFile(text, TestOptions.Regular);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.StructDeclaration, file.Members[0].Kind());
            var structDecl = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, structDecl.AttributeLists.Count);
            Assert.Equal(0, structDecl.Modifiers.Count);
            Assert.NotEqual(default, structDecl.Keyword);
            Assert.Equal(SyntaxKind.StructKeyword, structDecl.Keyword.Kind());
            Assert.NotEqual(default, structDecl.Identifier);
            Assert.Equal("a", structDecl.Identifier.ToString());
            Assert.Null(structDecl.BaseList);
            Assert.Equal(0, structDecl.ConstraintClauses.Count);
            Assert.NotEqual(default, structDecl.OpenBraceToken);
            Assert.NotEqual(default, structDecl.CloseBraceToken);

            Assert.Equal(1, structDecl.Members.Count);

            Assert.Equal(SyntaxKind.PropertyDeclaration, structDecl.Members[0].Kind());
            var propertySyntax = (PropertyDeclarationSyntax)structDecl.Members[0];
            Assert.Equal(0, propertySyntax.AttributeLists.Count);
            Assert.Equal(0, propertySyntax.Modifiers.Count);
            Assert.NotNull(propertySyntax.Type);
            Assert.Equal("int", propertySyntax.Type.ToString());
            Assert.NotEqual(default, propertySyntax.Identifier);
            Assert.Equal("P", propertySyntax.Identifier.ToString());
            var accessors = propertySyntax.AccessorList.Accessors;
            Assert.Equal(1, accessors.Count);
            Assert.Equal(1, accessors[0].Modifiers.Count);
            Assert.Equal(SyntaxKind.ReadOnlyKeyword, accessors[0].Modifiers[0].Kind());
        }

        [Fact]
        public void TestStructBadExpressionProperty()
        {
            var text =
@"public struct S
{
    public int P readonly => 0;
}
";
            var file = this.ParseFile(text, TestOptions.Regular);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());

            Assert.Equal(3, file.Errors().Length);
            Assert.Equal(ErrorCode.ERR_SemicolonExpected, (ErrorCode)file.Errors()[0].Code);
            Assert.Equal(ErrorCode.ERR_InvalidMemberDecl, (ErrorCode)file.Errors()[1].Code);
            Assert.Equal(ErrorCode.ERR_InvalidMemberDecl, (ErrorCode)file.Errors()[2].Code);
        }

        [Fact]
        public void TestClassMethodWithParameter()
        {
            var text = "class a { b X(c d) { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.MethodDeclaration, cs.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ms.AttributeLists.Count);
            Assert.Equal(0, ms.Modifiers.Count);
            Assert.NotNull(ms.ReturnType);
            Assert.Equal("b", ms.ReturnType.ToString());
            Assert.NotEqual(default, ms.Identifier);
            Assert.Equal("X", ms.Identifier.ToString());
            Assert.NotEqual(default, ms.ParameterList.OpenParenToken);
            Assert.False(ms.ParameterList.OpenParenToken.IsMissing);
            Assert.Equal(1, ms.ParameterList.Parameters.Count);
            Assert.Equal(0, ms.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal(0, ms.ParameterList.Parameters[0].Modifiers.Count);
            Assert.NotNull(ms.ParameterList.Parameters[0].Type);
            Assert.Equal("c", ms.ParameterList.Parameters[0].Type.ToString());
            Assert.NotEqual(default, ms.ParameterList.Parameters[0].Identifier);
            Assert.Equal("d", ms.ParameterList.Parameters[0].Identifier.ToString());
            Assert.NotEqual(default, ms.ParameterList.CloseParenToken);
            Assert.False(ms.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(0, ms.ConstraintClauses.Count);
            Assert.NotNull(ms.Body);
            Assert.NotEqual(SyntaxKind.None, ms.Body.OpenBraceToken.Kind());
            Assert.NotEqual(SyntaxKind.None, ms.Body.CloseBraceToken.Kind());
            Assert.Equal(SyntaxKind.None, ms.SemicolonToken.Kind());
        }

        [Fact]
        public void TestClassMethodWithMultipleParameters()
        {
            var text = "class a { b X(c d, e f) { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.MethodDeclaration, cs.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ms.AttributeLists.Count);
            Assert.Equal(0, ms.Modifiers.Count);
            Assert.NotNull(ms.ReturnType);
            Assert.Equal("b", ms.ReturnType.ToString());
            Assert.NotEqual(default, ms.Identifier);
            Assert.Equal("X", ms.Identifier.ToString());
            Assert.NotEqual(default, ms.ParameterList.OpenParenToken);
            Assert.False(ms.ParameterList.OpenParenToken.IsMissing);

            Assert.Equal(2, ms.ParameterList.Parameters.Count);

            Assert.Equal(0, ms.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal(0, ms.ParameterList.Parameters[0].Modifiers.Count);
            Assert.NotEqual(default, ms.ParameterList.Parameters[0].Type);
            Assert.Equal("c", ms.ParameterList.Parameters[0].Type.ToString());
            Assert.NotEqual(default, ms.ParameterList.Parameters[0].Identifier);
            Assert.Equal("d", ms.ParameterList.Parameters[0].Identifier.ToString());

            Assert.Equal(0, ms.ParameterList.Parameters[1].AttributeLists.Count);
            Assert.Equal(0, ms.ParameterList.Parameters[1].Modifiers.Count);
            Assert.NotEqual(default, ms.ParameterList.Parameters[1].Type);
            Assert.Equal("e", ms.ParameterList.Parameters[1].Type.ToString());
            Assert.NotEqual(default, ms.ParameterList.Parameters[1].Identifier);
            Assert.Equal("f", ms.ParameterList.Parameters[1].Identifier.ToString());

            Assert.NotEqual(default, ms.ParameterList.CloseParenToken);
            Assert.False(ms.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(0, ms.ConstraintClauses.Count);
            Assert.NotEqual(default, ms.Body);
            Assert.NotEqual(SyntaxKind.None, ms.Body.OpenBraceToken.Kind());
            Assert.NotEqual(SyntaxKind.None, ms.Body.CloseBraceToken.Kind());
            Assert.Equal(SyntaxKind.None, ms.SemicolonToken.Kind());
        }

        private void TestClassMethodWithParameterModifier(SyntaxKind mod)
        {
            var text = "class a { b X(" + SyntaxFacts.GetText(mod) + " c d) { } }";
            var file = this.ParseFile(text);

            Assert.NotEqual(default, file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.MethodDeclaration, cs.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ms.AttributeLists.Count);
            Assert.Equal(0, ms.Modifiers.Count);
            Assert.NotNull(ms.ReturnType);
            Assert.Equal("b", ms.ReturnType.ToString());
            Assert.NotEqual(default, ms.Identifier);
            Assert.Equal("X", ms.Identifier.ToString());
            Assert.NotEqual(default, ms.ParameterList.OpenParenToken);
            Assert.False(ms.ParameterList.OpenParenToken.IsMissing);

            Assert.Equal(1, ms.ParameterList.Parameters.Count);

            Assert.Equal(0, ms.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal(1, ms.ParameterList.Parameters[0].Modifiers.Count);
            Assert.Equal(mod, ms.ParameterList.Parameters[0].Modifiers[0].Kind());
            Assert.NotNull(ms.ParameterList.Parameters[0].Type);
            Assert.Equal("c", ms.ParameterList.Parameters[0].Type.ToString());
            Assert.NotEqual(default, ms.ParameterList.Parameters[0].Identifier);
            Assert.Equal("d", ms.ParameterList.Parameters[0].Identifier.ToString());

            Assert.NotEqual(default, ms.ParameterList.CloseParenToken);
            Assert.False(ms.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(0, ms.ConstraintClauses.Count);
            Assert.NotNull(ms.Body);
            Assert.NotEqual(SyntaxKind.None, ms.Body.OpenBraceToken.Kind());
            Assert.NotEqual(SyntaxKind.None, ms.Body.CloseBraceToken.Kind());
            Assert.Equal(SyntaxKind.None, ms.SemicolonToken.Kind());
        }

        [Fact]
        public void TestClassMethodWithParameterModifiers()
        {
            TestClassMethodWithParameterModifier(SyntaxKind.RefKeyword);
            TestClassMethodWithParameterModifier(SyntaxKind.OutKeyword);
            TestClassMethodWithParameterModifier(SyntaxKind.ParamsKeyword);
            TestClassMethodWithParameterModifier(SyntaxKind.ThisKeyword);
        }

        [Fact]
        public void TestClassMethodWithArgListParameter()
        {
            var text = "class a { b X(__arglist) { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.MethodDeclaration, cs.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ms.AttributeLists.Count);
            Assert.Equal(0, ms.Modifiers.Count);
            Assert.NotNull(ms.ReturnType);
            Assert.Equal("b", ms.ReturnType.ToString());
            Assert.NotEqual(default, ms.Identifier);
            Assert.Equal("X", ms.Identifier.ToString());
            Assert.NotEqual(default, ms.ParameterList.OpenParenToken);
            Assert.False(ms.ParameterList.OpenParenToken.IsMissing);

            Assert.Equal(1, ms.ParameterList.Parameters.Count);

            Assert.Equal(0, ms.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal(0, ms.ParameterList.Parameters[0].Modifiers.Count);
            Assert.Null(ms.ParameterList.Parameters[0].Type);
            Assert.NotEqual(default, ms.ParameterList.Parameters[0].Identifier);
            Assert.Equal(SyntaxKind.ArgListKeyword, ms.ParameterList.Parameters[0].Identifier.Kind());

            Assert.NotEqual(default, ms.ParameterList.CloseParenToken);
            Assert.False(ms.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(0, ms.ConstraintClauses.Count);
            Assert.NotNull(ms.Body);
            Assert.NotEqual(SyntaxKind.None, ms.Body.OpenBraceToken.Kind());
            Assert.NotEqual(SyntaxKind.None, ms.Body.CloseBraceToken.Kind());
            Assert.Equal(SyntaxKind.None, ms.SemicolonToken.Kind());
        }

        [Fact]
        public void TestClassMethodWithBuiltInReturnTypes()
        {
            TestClassMethodWithBuiltInReturnType(SyntaxKind.VoidKeyword);
            TestClassMethodWithBuiltInReturnType(SyntaxKind.BoolKeyword);
            TestClassMethodWithBuiltInReturnType(SyntaxKind.SByteKeyword);
            TestClassMethodWithBuiltInReturnType(SyntaxKind.IntKeyword);
            TestClassMethodWithBuiltInReturnType(SyntaxKind.UIntKeyword);
            TestClassMethodWithBuiltInReturnType(SyntaxKind.ShortKeyword);
            TestClassMethodWithBuiltInReturnType(SyntaxKind.UShortKeyword);
            TestClassMethodWithBuiltInReturnType(SyntaxKind.LongKeyword);
            TestClassMethodWithBuiltInReturnType(SyntaxKind.ULongKeyword);
            TestClassMethodWithBuiltInReturnType(SyntaxKind.FloatKeyword);
            TestClassMethodWithBuiltInReturnType(SyntaxKind.DoubleKeyword);
            TestClassMethodWithBuiltInReturnType(SyntaxKind.DecimalKeyword);
            TestClassMethodWithBuiltInReturnType(SyntaxKind.StringKeyword);
            TestClassMethodWithBuiltInReturnType(SyntaxKind.CharKeyword);
            TestClassMethodWithBuiltInReturnType(SyntaxKind.ObjectKeyword);
        }

        private void TestClassMethodWithBuiltInReturnType(SyntaxKind type)
        {
            var typeText = SyntaxFacts.GetText(type);
            var text = "class a { " + typeText + " M() { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.MethodDeclaration, cs.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ms.AttributeLists.Count);
            Assert.Equal(0, ms.Modifiers.Count);
            Assert.NotNull(ms.ReturnType);
            Assert.Equal(typeText, ms.ReturnType.ToString());
            Assert.NotEqual(default, ms.Identifier);
            Assert.Equal("M", ms.Identifier.ToString());
            Assert.NotEqual(default, ms.ParameterList.OpenParenToken);
            Assert.False(ms.ParameterList.OpenParenToken.IsMissing);
            Assert.Equal(0, ms.ParameterList.Parameters.Count);
            Assert.NotEqual(default, ms.ParameterList.CloseParenToken);
            Assert.False(ms.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(0, ms.ConstraintClauses.Count);
            Assert.NotNull(ms.Body);
            Assert.NotEqual(SyntaxKind.None, ms.Body.OpenBraceToken.Kind());
            Assert.NotEqual(SyntaxKind.None, ms.Body.CloseBraceToken.Kind());
            Assert.Equal(SyntaxKind.None, ms.SemicolonToken.Kind());
        }

        [Fact]
        public void TestClassMethodWithBuiltInParameterTypes()
        {
            TestClassMethodWithBuiltInParameterType(SyntaxKind.BoolKeyword);
            TestClassMethodWithBuiltInParameterType(SyntaxKind.SByteKeyword);
            TestClassMethodWithBuiltInParameterType(SyntaxKind.IntKeyword);
            TestClassMethodWithBuiltInParameterType(SyntaxKind.UIntKeyword);
            TestClassMethodWithBuiltInParameterType(SyntaxKind.ShortKeyword);
            TestClassMethodWithBuiltInParameterType(SyntaxKind.UShortKeyword);
            TestClassMethodWithBuiltInParameterType(SyntaxKind.LongKeyword);
            TestClassMethodWithBuiltInParameterType(SyntaxKind.ULongKeyword);
            TestClassMethodWithBuiltInParameterType(SyntaxKind.FloatKeyword);
            TestClassMethodWithBuiltInParameterType(SyntaxKind.DoubleKeyword);
            TestClassMethodWithBuiltInParameterType(SyntaxKind.DecimalKeyword);
            TestClassMethodWithBuiltInParameterType(SyntaxKind.StringKeyword);
            TestClassMethodWithBuiltInParameterType(SyntaxKind.CharKeyword);
            TestClassMethodWithBuiltInParameterType(SyntaxKind.ObjectKeyword);
        }

        private void TestClassMethodWithBuiltInParameterType(SyntaxKind type)
        {
            var typeText = SyntaxFacts.GetText(type);
            var text = "class a { b X(" + typeText + " c) { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.MethodDeclaration, cs.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ms.AttributeLists.Count);
            Assert.Equal(0, ms.Modifiers.Count);
            Assert.NotNull(ms.ReturnType);
            Assert.Equal("b", ms.ReturnType.ToString());
            Assert.NotEqual(default, ms.Identifier);
            Assert.Equal("X", ms.Identifier.ToString());
            Assert.NotEqual(default, ms.ParameterList.OpenParenToken);
            Assert.False(ms.ParameterList.OpenParenToken.IsMissing);
            Assert.Equal(1, ms.ParameterList.Parameters.Count);
            Assert.Equal(0, ms.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal(0, ms.ParameterList.Parameters[0].Modifiers.Count);
            Assert.NotNull(ms.ParameterList.Parameters[0].Type);
            Assert.Equal(typeText, ms.ParameterList.Parameters[0].Type.ToString());
            Assert.NotEqual(default, ms.ParameterList.Parameters[0].Identifier);
            Assert.Equal("c", ms.ParameterList.Parameters[0].Identifier.ToString());
            Assert.NotEqual(default, ms.ParameterList.CloseParenToken);
            Assert.False(ms.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(0, ms.ConstraintClauses.Count);
            Assert.NotNull(ms.Body);
            Assert.NotEqual(SyntaxKind.None, ms.Body.OpenBraceToken.Kind());
            Assert.NotEqual(SyntaxKind.None, ms.Body.CloseBraceToken.Kind());
            Assert.Equal(SyntaxKind.None, ms.SemicolonToken.Kind());
        }

        [Fact]
        public void TestGenericClassMethod()
        {
            var text = "class a { b<c> M() { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.MethodDeclaration, cs.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ms.AttributeLists.Count);
            Assert.Equal(0, ms.Modifiers.Count);
            Assert.NotNull(ms.ReturnType);
            Assert.Equal("b<c>", ms.ReturnType.ToString());
            Assert.NotEqual(default, ms.Identifier);
            Assert.Equal("M", ms.Identifier.ToString());
            Assert.NotEqual(default, ms.ParameterList.OpenParenToken);
            Assert.False(ms.ParameterList.OpenParenToken.IsMissing);
            Assert.Equal(0, ms.ParameterList.Parameters.Count);
            Assert.NotEqual(default, ms.ParameterList.CloseParenToken);
            Assert.False(ms.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(0, ms.ConstraintClauses.Count);
            Assert.NotNull(ms.Body);
            Assert.NotEqual(SyntaxKind.None, ms.Body.OpenBraceToken.Kind());
            Assert.NotEqual(SyntaxKind.None, ms.Body.CloseBraceToken.Kind());
            Assert.Equal(SyntaxKind.None, ms.SemicolonToken.Kind());
        }

        [Fact]
        public void TestGenericClassMethodWithTypeConstraintBound()
        {
            var text = "class a { b X<c>() where b : d { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.MethodDeclaration, cs.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ms.AttributeLists.Count);
            Assert.Equal(0, ms.Modifiers.Count);
            Assert.NotNull(ms.ReturnType);
            Assert.Equal("b", ms.ReturnType.ToString());
            Assert.NotEqual(default, ms.Identifier);
            Assert.NotNull(ms.TypeParameterList);
            Assert.Equal("X", ms.Identifier.ToString());
            Assert.Equal("<c>", ms.TypeParameterList.ToString());
            Assert.NotEqual(default, ms.ParameterList.OpenParenToken);
            Assert.False(ms.ParameterList.OpenParenToken.IsMissing);
            Assert.Equal(0, ms.ParameterList.Parameters.Count);
            Assert.NotEqual(default, ms.ParameterList.CloseParenToken);
            Assert.False(ms.ParameterList.CloseParenToken.IsMissing);

            Assert.Equal(1, ms.ConstraintClauses.Count);
            Assert.NotEqual(default, ms.ConstraintClauses[0].WhereKeyword);
            Assert.NotNull(ms.ConstraintClauses[0].Name);
            Assert.Equal("b", ms.ConstraintClauses[0].Name.ToString());
            Assert.NotEqual(default, ms.ConstraintClauses[0].ColonToken);
            Assert.False(ms.ConstraintClauses[0].ColonToken.IsMissing);
            Assert.Equal(1, ms.ConstraintClauses[0].Constraints.Count);
            Assert.Equal(SyntaxKind.TypeConstraint, ms.ConstraintClauses[0].Constraints[0].Kind());
            var typeBound = (TypeConstraintSyntax)ms.ConstraintClauses[0].Constraints[0];
            Assert.NotNull(typeBound.Type);
            Assert.Equal("d", typeBound.Type.ToString());

            Assert.NotNull(ms.Body);
            Assert.NotEqual(SyntaxKind.None, ms.Body.OpenBraceToken.Kind());
            Assert.NotEqual(SyntaxKind.None, ms.Body.CloseBraceToken.Kind());
            Assert.Equal(SyntaxKind.None, ms.SemicolonToken.Kind());
        }

        [WorkItem(899685, "DevDiv/Personal")]
        [Fact]
        public void TestGenericClassConstructor()
        {
            var text = @"
class Class1<T>{
    public Class1() { }
}
";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);

            // verify that we can roundtrip
            Assert.Equal(text, file.ToFullString());

            // verify that we don't produce any errors
            Assert.Equal(0, file.Errors().Length);
        }

        [Fact]
        public void TestClassConstructor()
        {
            var text = "class a { a() { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(SyntaxKind.None, cs.OpenBraceToken.Kind());
            Assert.NotEqual(SyntaxKind.None, cs.CloseBraceToken.Kind());
            Assert.Equal(SyntaxKind.None, cs.SemicolonToken.Kind());

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.ConstructorDeclaration, cs.Members[0].Kind());
            var cn = (ConstructorDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, cn.AttributeLists.Count);
            Assert.Equal(0, cn.Modifiers.Count);
            Assert.NotNull(cn.Body);
            Assert.NotEqual(default, cn.Body.OpenBraceToken);
            Assert.NotEqual(default, cn.Body.CloseBraceToken);
        }

        private void TestClassConstructorWithModifier(SyntaxKind mod)
        {
            var text = "class a { " + SyntaxFacts.GetText(mod) + " a() { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(SyntaxKind.None, cs.OpenBraceToken.Kind());
            Assert.NotEqual(SyntaxKind.None, cs.CloseBraceToken.Kind());
            Assert.Equal(SyntaxKind.None, cs.SemicolonToken.Kind());

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.ConstructorDeclaration, cs.Members[0].Kind());
            var cn = (ConstructorDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, cn.AttributeLists.Count);
            Assert.Equal(1, cn.Modifiers.Count);
            Assert.Equal(mod, cn.Modifiers[0].Kind());
            Assert.NotNull(cn.Body);
            Assert.NotEqual(default, cn.Body.OpenBraceToken);
            Assert.NotEqual(default, cn.Body.CloseBraceToken);
        }

        [Fact]
        public void TestClassConstructorWithModifiers()
        {
            TestClassConstructorWithModifier(SyntaxKind.PublicKeyword);
            TestClassConstructorWithModifier(SyntaxKind.PrivateKeyword);
            TestClassConstructorWithModifier(SyntaxKind.ProtectedKeyword);
            TestClassConstructorWithModifier(SyntaxKind.InternalKeyword);
            TestClassConstructorWithModifier(SyntaxKind.StaticKeyword);
        }

        [Fact]
        public void TestClassDestructor()
        {
            var text = "class a { ~a() { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(SyntaxKind.None, cs.OpenBraceToken.Kind());
            Assert.NotEqual(SyntaxKind.None, cs.CloseBraceToken.Kind());
            Assert.Equal(SyntaxKind.None, cs.SemicolonToken.Kind());

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.DestructorDeclaration, cs.Members[0].Kind());
            var cn = (DestructorDeclarationSyntax)cs.Members[0];
            Assert.NotEqual(default, cn.TildeToken);
            Assert.Equal(0, cn.AttributeLists.Count);
            Assert.Equal(0, cn.Modifiers.Count);
            Assert.NotNull(cn.Body);
            Assert.NotEqual(default, cn.Body.OpenBraceToken);
            Assert.NotEqual(default, cn.Body.CloseBraceToken);
        }

        [Fact]
        public void TestClassField()
        {
            var text = "class a { b c; }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.FieldDeclaration, cs.Members[0].Kind());
            var fs = (FieldDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, fs.AttributeLists.Count);
            Assert.Equal(0, fs.Modifiers.Count);
            Assert.NotNull(fs.Declaration.Type);
            Assert.Equal("b", fs.Declaration.Type.ToString());
            Assert.Equal(1, fs.Declaration.Variables.Count);
            Assert.NotEqual(default, fs.Declaration.Variables[0].Identifier);
            Assert.Equal("c", fs.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(fs.Declaration.Variables[0].ArgumentList);
            Assert.Null(fs.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, fs.SemicolonToken);
            Assert.False(fs.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestClassFieldWithBuiltInTypes()
        {
            TestClassFieldWithBuiltInType(SyntaxKind.BoolKeyword);
            TestClassFieldWithBuiltInType(SyntaxKind.SByteKeyword);
            TestClassFieldWithBuiltInType(SyntaxKind.IntKeyword);
            TestClassFieldWithBuiltInType(SyntaxKind.UIntKeyword);
            TestClassFieldWithBuiltInType(SyntaxKind.ShortKeyword);
            TestClassFieldWithBuiltInType(SyntaxKind.UShortKeyword);
            TestClassFieldWithBuiltInType(SyntaxKind.LongKeyword);
            TestClassFieldWithBuiltInType(SyntaxKind.ULongKeyword);
            TestClassFieldWithBuiltInType(SyntaxKind.FloatKeyword);
            TestClassFieldWithBuiltInType(SyntaxKind.DoubleKeyword);
            TestClassFieldWithBuiltInType(SyntaxKind.DecimalKeyword);
            TestClassFieldWithBuiltInType(SyntaxKind.StringKeyword);
            TestClassFieldWithBuiltInType(SyntaxKind.CharKeyword);
            TestClassFieldWithBuiltInType(SyntaxKind.ObjectKeyword);
        }

        private void TestClassFieldWithBuiltInType(SyntaxKind type)
        {
            var typeText = SyntaxFacts.GetText(type);
            var text = "class a { " + typeText + " c; }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.FieldDeclaration, cs.Members[0].Kind());
            var fs = (FieldDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, fs.AttributeLists.Count);
            Assert.Equal(0, fs.Modifiers.Count);
            Assert.NotNull(fs.Declaration.Type);
            Assert.Equal(typeText, fs.Declaration.Type.ToString());
            Assert.Equal(1, fs.Declaration.Variables.Count);
            Assert.NotEqual(default, fs.Declaration.Variables[0].Identifier);
            Assert.Equal("c", fs.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(fs.Declaration.Variables[0].ArgumentList);
            Assert.Null(fs.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, fs.SemicolonToken);
            Assert.False(fs.SemicolonToken.IsMissing);
        }

        private void TestClassFieldModifier(SyntaxKind mod)
        {
            var text = "class a { " + SyntaxFacts.GetText(mod) + " b c; }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.FieldDeclaration, cs.Members[0].Kind());
            var fs = (FieldDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, fs.AttributeLists.Count);
            Assert.Equal(1, fs.Modifiers.Count);
            Assert.Equal(mod, fs.Modifiers[0].Kind());
            Assert.NotNull(fs.Declaration.Type);
            Assert.Equal("b", fs.Declaration.Type.ToString());
            Assert.Equal(1, fs.Declaration.Variables.Count);
            Assert.NotEqual(default, fs.Declaration.Variables[0].Identifier);
            Assert.Equal("c", fs.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(fs.Declaration.Variables[0].ArgumentList);
            Assert.Null(fs.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, fs.SemicolonToken);
            Assert.False(fs.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestClassFieldModifiers()
        {
            TestClassFieldModifier(SyntaxKind.PublicKeyword);
            TestClassFieldModifier(SyntaxKind.PrivateKeyword);
            TestClassFieldModifier(SyntaxKind.ProtectedKeyword);
            TestClassFieldModifier(SyntaxKind.InternalKeyword);
            TestClassFieldModifier(SyntaxKind.StaticKeyword);
            TestClassFieldModifier(SyntaxKind.ReadOnlyKeyword);
            TestClassFieldModifier(SyntaxKind.VolatileKeyword);
            TestClassFieldModifier(SyntaxKind.ExternKeyword);
        }

        private void TestClassEventFieldModifier(SyntaxKind mod)
        {
            var text = "class a { " + SyntaxFacts.GetText(mod) + " event b c; }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.EventFieldDeclaration, cs.Members[0].Kind());
            var fs = (EventFieldDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, fs.AttributeLists.Count);
            Assert.Equal(1, fs.Modifiers.Count);
            Assert.Equal(mod, fs.Modifiers[0].Kind());
            Assert.NotEqual(default, fs.EventKeyword);
            Assert.NotNull(fs.Declaration.Type);
            Assert.Equal("b", fs.Declaration.Type.ToString());
            Assert.Equal(1, fs.Declaration.Variables.Count);
            Assert.NotEqual(default, fs.Declaration.Variables[0].Identifier);
            Assert.Equal("c", fs.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(fs.Declaration.Variables[0].ArgumentList);
            Assert.Null(fs.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, fs.SemicolonToken);
            Assert.False(fs.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestClassEventFieldModifiers()
        {
            TestClassEventFieldModifier(SyntaxKind.PublicKeyword);
            TestClassEventFieldModifier(SyntaxKind.PrivateKeyword);
            TestClassEventFieldModifier(SyntaxKind.ProtectedKeyword);
            TestClassEventFieldModifier(SyntaxKind.InternalKeyword);
            TestClassEventFieldModifier(SyntaxKind.StaticKeyword);
            TestClassEventFieldModifier(SyntaxKind.ReadOnlyKeyword);
            TestClassEventFieldModifier(SyntaxKind.VolatileKeyword);
            TestClassEventFieldModifier(SyntaxKind.ExternKeyword);
        }

        [Fact]
        public void TestClassConstField()
        {
            var text = "class a { const b c = d; }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.FieldDeclaration, cs.Members[0].Kind());
            var fs = (FieldDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, fs.AttributeLists.Count);
            Assert.Equal(1, fs.Modifiers.Count);
            Assert.Equal(SyntaxKind.ConstKeyword, fs.Modifiers[0].Kind());
            Assert.NotNull(fs.Declaration.Type);
            Assert.Equal("b", fs.Declaration.Type.ToString());
            Assert.Equal(1, fs.Declaration.Variables.Count);
            Assert.NotEqual(default, fs.Declaration.Variables[0].Identifier);
            Assert.Equal("c", fs.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(fs.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(fs.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, fs.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(fs.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("d", fs.Declaration.Variables[0].Initializer.Value.ToString());
            Assert.NotEqual(default, fs.SemicolonToken);
            Assert.False(fs.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestClassFieldWithInitializer()
        {
            var text = "class a { b c = e; }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.FieldDeclaration, cs.Members[0].Kind());
            var fs = (FieldDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, fs.AttributeLists.Count);
            Assert.Equal(0, fs.Modifiers.Count);
            Assert.NotNull(fs.Declaration.Type);
            Assert.Equal("b", fs.Declaration.Type.ToString());
            Assert.Equal(1, fs.Declaration.Variables.Count);
            Assert.NotEqual(default, fs.Declaration.Variables[0].Identifier);
            Assert.Equal("c", fs.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(fs.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(fs.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, fs.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(fs.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("e", fs.Declaration.Variables[0].Initializer.Value.ToString());
            Assert.NotEqual(default, fs.SemicolonToken);
            Assert.False(fs.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestClassFieldWithArrayInitializer()
        {
            var text = "class a { b c = { }; }";
            var file = this.ParseFile(text);

            Assert.NotEqual(default, file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.FieldDeclaration, cs.Members[0].Kind());
            var fs = (FieldDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, fs.AttributeLists.Count);
            Assert.Equal(0, fs.Modifiers.Count);
            Assert.NotEqual(default, fs.Declaration.Type);
            Assert.Equal("b", fs.Declaration.Type.ToString());
            Assert.Equal(1, fs.Declaration.Variables.Count);
            Assert.NotEqual(default, fs.Declaration.Variables[0].Identifier);
            Assert.Equal("c", fs.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(fs.Declaration.Variables[0].ArgumentList);
            Assert.NotEqual(default, fs.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, fs.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotEqual(default, fs.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ArrayInitializerExpression, fs.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal("{ }", fs.Declaration.Variables[0].Initializer.Value.ToString());
            Assert.NotEqual(default, fs.SemicolonToken);
            Assert.False(fs.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestClassFieldWithMultipleVariables()
        {
            var text = "class a { b c, d, e; }";
            var file = this.ParseFile(text);

            Assert.NotEqual(default, file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.FieldDeclaration, cs.Members[0].Kind());
            var fs = (FieldDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, fs.AttributeLists.Count);
            Assert.Equal(0, fs.Modifiers.Count);
            Assert.NotNull(fs.Declaration.Type);
            Assert.Equal("b", fs.Declaration.Type.ToString());

            Assert.Equal(3, fs.Declaration.Variables.Count);

            Assert.NotEqual(default, fs.Declaration.Variables[0].Identifier);
            Assert.Equal("c", fs.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(fs.Declaration.Variables[0].ArgumentList);
            Assert.Null(fs.Declaration.Variables[0].Initializer);

            Assert.NotEqual(default, fs.Declaration.Variables[1].Identifier);
            Assert.Equal("d", fs.Declaration.Variables[1].Identifier.ToString());
            Assert.Null(fs.Declaration.Variables[1].ArgumentList);
            Assert.Null(fs.Declaration.Variables[1].Initializer);

            Assert.NotEqual(default, fs.Declaration.Variables[2].Identifier);
            Assert.Equal("e", fs.Declaration.Variables[2].Identifier.ToString());
            Assert.Null(fs.Declaration.Variables[2].ArgumentList);
            Assert.Null(fs.Declaration.Variables[2].Initializer);

            Assert.NotEqual(default, fs.SemicolonToken);
            Assert.False(fs.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestClassFieldWithMultipleVariablesAndInitializers()
        {
            var text = "class a { b c = x, d = y, e = z; }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.FieldDeclaration, cs.Members[0].Kind());
            var fs = (FieldDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, fs.AttributeLists.Count);
            Assert.Equal(0, fs.Modifiers.Count);
            Assert.NotNull(fs.Declaration.Type);
            Assert.Equal("b", fs.Declaration.Type.ToString());

            Assert.Equal(3, fs.Declaration.Variables.Count);

            Assert.NotEqual(default, fs.Declaration.Variables[0].Identifier);
            Assert.Equal("c", fs.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(fs.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(fs.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, fs.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(fs.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("x", fs.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.NotEqual(default, fs.Declaration.Variables[1].Identifier);
            Assert.Equal("d", fs.Declaration.Variables[1].Identifier.ToString());
            Assert.Null(fs.Declaration.Variables[1].ArgumentList);
            Assert.NotNull(fs.Declaration.Variables[1].Initializer);
            Assert.NotEqual(default, fs.Declaration.Variables[1].Initializer.EqualsToken);
            Assert.NotNull(fs.Declaration.Variables[1].Initializer.Value);
            Assert.Equal("y", fs.Declaration.Variables[1].Initializer.Value.ToString());

            Assert.NotEqual(default, fs.Declaration.Variables[2].Identifier);
            Assert.Equal("e", fs.Declaration.Variables[2].Identifier.ToString());
            Assert.Null(fs.Declaration.Variables[2].ArgumentList);
            Assert.NotNull(fs.Declaration.Variables[2].Initializer);
            Assert.NotEqual(default, fs.Declaration.Variables[2].Initializer.EqualsToken);
            Assert.NotNull(fs.Declaration.Variables[2].Initializer.Value);
            Assert.Equal("z", fs.Declaration.Variables[2].Initializer.Value.ToString());

            Assert.NotEqual(default, fs.SemicolonToken);
            Assert.False(fs.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestClassFixedField()
        {
            var text = "class a { fixed b c[10]; }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.FieldDeclaration, cs.Members[0].Kind());
            var fs = (FieldDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, fs.AttributeLists.Count);
            Assert.Equal(1, fs.Modifiers.Count);
            Assert.Equal(SyntaxKind.FixedKeyword, fs.Modifiers[0].Kind());
            Assert.NotNull(fs.Declaration.Type);
            Assert.Equal("b", fs.Declaration.Type.ToString());
            Assert.Equal(1, fs.Declaration.Variables.Count);
            Assert.NotEqual(default, fs.Declaration.Variables[0].Identifier);
            Assert.Equal("c", fs.Declaration.Variables[0].Identifier.ToString());
            Assert.NotNull(fs.Declaration.Variables[0].ArgumentList);
            Assert.NotEqual(default, fs.Declaration.Variables[0].ArgumentList.OpenBracketToken);
            Assert.NotEqual(default, fs.Declaration.Variables[0].ArgumentList.CloseBracketToken);
            Assert.Equal(1, fs.Declaration.Variables[0].ArgumentList.Arguments.Count);
            Assert.Equal("10", fs.Declaration.Variables[0].ArgumentList.Arguments[0].ToString());
            Assert.Null(fs.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, fs.SemicolonToken);
            Assert.False(fs.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestClassProperty()
        {
            var text = "class a { b c { get; set; } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.PropertyDeclaration, cs.Members[0].Kind());
            var ps = (PropertyDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ps.AttributeLists.Count);
            Assert.Equal(0, ps.Modifiers.Count);
            Assert.NotNull(ps.Type);
            Assert.Equal("b", ps.Type.ToString());
            Assert.NotEqual(default, ps.Identifier);
            Assert.Equal("c", ps.Identifier.ToString());

            Assert.NotEqual(default, ps.AccessorList.OpenBraceToken);
            Assert.NotEqual(default, ps.AccessorList.CloseBraceToken);
            Assert.Equal(2, ps.AccessorList.Accessors.Count);

            Assert.Equal(0, ps.AccessorList.Accessors[0].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[0].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].Keyword);
            Assert.Equal(SyntaxKind.GetKeyword, ps.AccessorList.Accessors[0].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[0].Body);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].SemicolonToken);

            Assert.Equal(0, ps.AccessorList.Accessors[1].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[1].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].Keyword);
            Assert.Equal(SyntaxKind.SetKeyword, ps.AccessorList.Accessors[1].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[1].Body);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].SemicolonToken);
        }

        [Fact]
        public void TestClassPropertyWithRefReturn()
        {
            var text = "class a { ref b c { get; set; } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.PropertyDeclaration, cs.Members[0].Kind());
            var ps = (PropertyDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ps.AttributeLists.Count);
            Assert.Equal(0, ps.Modifiers.Count);
            Assert.NotNull(ps.Type);
            Assert.Equal("ref b", ps.Type.ToString());
            Assert.NotEqual(default, ps.Identifier);
            Assert.Equal("c", ps.Identifier.ToString());

            Assert.NotEqual(default, ps.AccessorList.OpenBraceToken);
            Assert.NotEqual(default, ps.AccessorList.CloseBraceToken);
            Assert.Equal(2, ps.AccessorList.Accessors.Count);

            Assert.Equal(0, ps.AccessorList.Accessors[0].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[0].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].Keyword);
            Assert.Equal(SyntaxKind.GetKeyword, ps.AccessorList.Accessors[0].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[0].Body);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].SemicolonToken);

            Assert.Equal(0, ps.AccessorList.Accessors[1].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[1].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].Keyword);
            Assert.Equal(SyntaxKind.SetKeyword, ps.AccessorList.Accessors[1].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[1].Body);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].SemicolonToken);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public void TestClassPropertyWithRefReadonlyReturn()
        {
            var text = "class a { ref readonly b c { get; set; } }";
            var file = this.ParseFile(text, TestOptions.Regular);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.PropertyDeclaration, cs.Members[0].Kind());
            var ps = (PropertyDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ps.AttributeLists.Count);
            Assert.Equal(0, ps.Modifiers.Count);
            Assert.NotNull(ps.Type);
            Assert.Equal("ref readonly b", ps.Type.ToString());
            Assert.NotEqual(default, ps.Identifier);
            Assert.Equal("c", ps.Identifier.ToString());

            Assert.NotEqual(default, ps.AccessorList.OpenBraceToken);
            Assert.NotEqual(default, ps.AccessorList.CloseBraceToken);
            Assert.Equal(2, ps.AccessorList.Accessors.Count);

            Assert.Equal(0, ps.AccessorList.Accessors[0].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[0].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].Keyword);
            Assert.Equal(SyntaxKind.GetKeyword, ps.AccessorList.Accessors[0].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[0].Body);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].SemicolonToken);

            Assert.Equal(0, ps.AccessorList.Accessors[1].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[1].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].Keyword);
            Assert.Equal(SyntaxKind.SetKeyword, ps.AccessorList.Accessors[1].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[1].Body);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].SemicolonToken);
        }

        [Fact]
        public void TestClassPropertyWithBuiltInTypes()
        {
            TestClassPropertyWithBuiltInType(SyntaxKind.BoolKeyword);
            TestClassPropertyWithBuiltInType(SyntaxKind.SByteKeyword);
            TestClassPropertyWithBuiltInType(SyntaxKind.IntKeyword);
            TestClassPropertyWithBuiltInType(SyntaxKind.UIntKeyword);
            TestClassPropertyWithBuiltInType(SyntaxKind.ShortKeyword);
            TestClassPropertyWithBuiltInType(SyntaxKind.UShortKeyword);
            TestClassPropertyWithBuiltInType(SyntaxKind.LongKeyword);
            TestClassPropertyWithBuiltInType(SyntaxKind.ULongKeyword);
            TestClassPropertyWithBuiltInType(SyntaxKind.FloatKeyword);
            TestClassPropertyWithBuiltInType(SyntaxKind.DoubleKeyword);
            TestClassPropertyWithBuiltInType(SyntaxKind.DecimalKeyword);
            TestClassPropertyWithBuiltInType(SyntaxKind.StringKeyword);
            TestClassPropertyWithBuiltInType(SyntaxKind.CharKeyword);
            TestClassPropertyWithBuiltInType(SyntaxKind.ObjectKeyword);
        }

        private void TestClassPropertyWithBuiltInType(SyntaxKind type)
        {
            var typeText = SyntaxFacts.GetText(type);
            var text = "class a { " + typeText + " c { get; set; } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.PropertyDeclaration, cs.Members[0].Kind());
            var ps = (PropertyDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ps.AttributeLists.Count);
            Assert.Equal(0, ps.Modifiers.Count);
            Assert.NotNull(ps.Type);
            Assert.Equal(typeText, ps.Type.ToString());
            Assert.NotEqual(default, ps.Identifier);
            Assert.Equal("c", ps.Identifier.ToString());

            Assert.NotEqual(default, ps.AccessorList.OpenBraceToken);
            Assert.NotEqual(default, ps.AccessorList.CloseBraceToken);
            Assert.Equal(2, ps.AccessorList.Accessors.Count);

            Assert.Equal(0, ps.AccessorList.Accessors[0].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[0].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].Keyword);
            Assert.Equal(SyntaxKind.GetKeyword, ps.AccessorList.Accessors[0].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[0].Body);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].SemicolonToken);

            Assert.Equal(0, ps.AccessorList.Accessors[1].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[1].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].Keyword);
            Assert.Equal(SyntaxKind.SetKeyword, ps.AccessorList.Accessors[1].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[1].Body);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].SemicolonToken);
        }

        [Fact]
        public void TestClassPropertyWithBodies()
        {
            var text = "class a { b c { get { } set { } } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.PropertyDeclaration, cs.Members[0].Kind());
            var ps = (PropertyDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ps.AttributeLists.Count);
            Assert.Equal(0, ps.Modifiers.Count);
            Assert.NotNull(ps.Type);
            Assert.Equal("b", ps.Type.ToString());
            Assert.NotEqual(default, ps.Identifier);
            Assert.Equal("c", ps.Identifier.ToString());

            Assert.NotEqual(default, ps.AccessorList.OpenBraceToken);
            Assert.NotEqual(default, ps.AccessorList.CloseBraceToken);
            Assert.Equal(2, ps.AccessorList.Accessors.Count);

            Assert.Equal(0, ps.AccessorList.Accessors[0].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[0].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].Keyword);
            Assert.Equal(SyntaxKind.GetKeyword, ps.AccessorList.Accessors[0].Keyword.Kind());
            Assert.NotNull(ps.AccessorList.Accessors[0].Body);
            Assert.Equal(SyntaxKind.None, ps.AccessorList.Accessors[0].SemicolonToken.Kind());

            Assert.Equal(0, ps.AccessorList.Accessors[1].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[1].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].Keyword);
            Assert.Equal(SyntaxKind.SetKeyword, ps.AccessorList.Accessors[1].Keyword.Kind());
            Assert.NotNull(ps.AccessorList.Accessors[1].Body);
            Assert.Equal(SyntaxKind.None, ps.AccessorList.Accessors[1].SemicolonToken.Kind());
        }

        [Fact]
        public void TestClassAutoPropertyWithInitializer()
        {
            var text = "class a { b c { get; set; } = d; }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (ClassDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.PropertyDeclaration, cs.Members[0].Kind());
            var ps = (PropertyDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ps.AttributeLists.Count);
            Assert.Equal(0, ps.Modifiers.Count);
            Assert.NotNull(ps.Type);
            Assert.Equal("b", ps.Type.ToString());
            Assert.NotEqual(default, ps.Identifier);
            Assert.Equal("c", ps.Identifier.ToString());

            Assert.NotEqual(default, ps.AccessorList.OpenBraceToken);
            Assert.NotEqual(default, ps.AccessorList.CloseBraceToken);
            Assert.Equal(2, ps.AccessorList.Accessors.Count);

            Assert.Equal(0, ps.AccessorList.Accessors[0].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[0].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].Keyword);
            Assert.Equal(SyntaxKind.GetKeyword, ps.AccessorList.Accessors[0].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[0].Body);

            Assert.Equal(0, ps.AccessorList.Accessors[1].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[1].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].Keyword);
            Assert.Equal(SyntaxKind.SetKeyword, ps.AccessorList.Accessors[1].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[1].Body);

            Assert.NotNull(ps.Initializer);
            Assert.NotNull(ps.Initializer.Value);
            Assert.Equal("d", ps.Initializer.Value.ToString());
        }

        [Fact]
        public void InitializerOnNonAutoProp()
        {
            var text = "class C { int P { set {} } = 0; }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (ClassDeclarationSyntax)file.Members[0];

            Assert.Equal(1, cs.Members.Count);
            Assert.Equal(SyntaxKind.PropertyDeclaration, cs.Members[0].Kind());
            var ps = (PropertyDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ps.Errors().Length);
        }

        [Fact]
        public void TestClassPropertyOrEventWithValue()
        {
            TestClassPropertyWithValue(SyntaxKind.GetAccessorDeclaration, SyntaxKind.GetKeyword, SyntaxKind.IdentifierToken);
            TestClassPropertyWithValue(SyntaxKind.SetAccessorDeclaration, SyntaxKind.SetKeyword, SyntaxKind.IdentifierToken);
            TestClassEventWithValue(SyntaxKind.AddAccessorDeclaration, SyntaxKind.AddKeyword, SyntaxKind.IdentifierToken);
            TestClassEventWithValue(SyntaxKind.RemoveAccessorDeclaration, SyntaxKind.RemoveKeyword, SyntaxKind.IdentifierToken);
        }

        private void TestClassPropertyWithValue(SyntaxKind accessorKind, SyntaxKind accessorKeyword, SyntaxKind tokenKind)
        {
            bool isEvent = accessorKeyword == SyntaxKind.AddKeyword || accessorKeyword == SyntaxKind.RemoveKeyword;
            var text = "class a { " + (isEvent ? "event" : string.Empty) + " b c { " + SyntaxFacts.GetText(accessorKeyword) + " { x = value; } } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(isEvent ? SyntaxKind.EventDeclaration : SyntaxKind.PropertyDeclaration, cs.Members[0].Kind());
            var ps = (PropertyDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ps.AttributeLists.Count);
            Assert.Equal(0, ps.Modifiers.Count);
            Assert.NotNull(ps.Type);
            Assert.Equal("b", ps.Type.ToString());
            Assert.NotEqual(default, ps.Identifier);
            Assert.Equal("c", ps.Identifier.ToString());

            Assert.NotEqual(default, ps.AccessorList.OpenBraceToken);
            Assert.NotEqual(default, ps.AccessorList.CloseBraceToken);
            Assert.Equal(1, ps.AccessorList.Accessors.Count);

            Assert.Equal(0, ps.AccessorList.Accessors[0].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[0].Modifiers.Count);
            Assert.Equal(accessorKind, ps.AccessorList.Accessors[0].Kind());
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].Keyword);
            Assert.Equal(accessorKeyword, ps.AccessorList.Accessors[0].Keyword.Kind());
            Assert.Equal(SyntaxKind.None, ps.AccessorList.Accessors[0].SemicolonToken.Kind());
            var body = ps.AccessorList.Accessors[0].Body;
            Assert.NotNull(body);
            Assert.Equal(1, body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, body.Statements[0].Kind());
            var es = (ExpressionStatementSyntax)body.Statements[0];
            Assert.NotNull(es.Expression);
            Assert.Equal(SyntaxKind.SimpleAssignmentExpression, es.Expression.Kind());
            var bx = (AssignmentExpressionSyntax)es.Expression;
            Assert.Equal(SyntaxKind.IdentifierName, bx.Right.Kind());
            Assert.Equal(tokenKind, ((IdentifierNameSyntax)bx.Right).Identifier.Kind());
        }

        private void TestClassEventWithValue(SyntaxKind accessorKind, SyntaxKind accessorKeyword, SyntaxKind tokenKind)
        {
            var text = "class a { event b c { " + SyntaxFacts.GetText(accessorKeyword) + " { x = value; } } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.EventDeclaration, cs.Members[0].Kind());
            var es = (EventDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, es.AttributeLists.Count);
            Assert.Equal(0, es.Modifiers.Count);
            Assert.NotNull(es.Type);
            Assert.Equal("b", es.Type.ToString());
            Assert.NotEqual(default, es.Identifier);
            Assert.Equal("c", es.Identifier.ToString());

            Assert.NotEqual(default, es.AccessorList.OpenBraceToken);
            Assert.NotEqual(default, es.AccessorList.CloseBraceToken);
            Assert.Equal(1, es.AccessorList.Accessors.Count);

            Assert.Equal(0, es.AccessorList.Accessors[0].AttributeLists.Count);
            Assert.Equal(0, es.AccessorList.Accessors[0].Modifiers.Count);
            Assert.Equal(accessorKind, es.AccessorList.Accessors[0].Kind());
            Assert.NotEqual(default, es.AccessorList.Accessors[0].Keyword);
            Assert.Equal(accessorKeyword, es.AccessorList.Accessors[0].Keyword.Kind());
            Assert.Equal(SyntaxKind.None, es.AccessorList.Accessors[0].SemicolonToken.Kind());
            var body = es.AccessorList.Accessors[0].Body;
            Assert.NotNull(body);
            Assert.Equal(1, body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, body.Statements[0].Kind());
            var xs = (ExpressionStatementSyntax)body.Statements[0];
            Assert.NotNull(xs.Expression);
            Assert.Equal(SyntaxKind.SimpleAssignmentExpression, xs.Expression.Kind());
            var bx = (AssignmentExpressionSyntax)xs.Expression;
            Assert.Equal(SyntaxKind.IdentifierName, bx.Right.Kind());
            Assert.Equal(tokenKind, ((IdentifierNameSyntax)bx.Right).Identifier.Kind());
        }

        private void TestClassPropertyWithModifier(SyntaxKind mod)
        {
            var text = "class a { " + SyntaxFacts.GetText(mod) + " b c { get; set; } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.PropertyDeclaration, cs.Members[0].Kind());
            var ps = (PropertyDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ps.AttributeLists.Count);
            Assert.Equal(1, ps.Modifiers.Count);
            Assert.Equal(mod, ps.Modifiers[0].Kind());
            Assert.NotNull(ps.Type);
            Assert.Equal("b", ps.Type.ToString());
            Assert.NotEqual(default, ps.Identifier);
            Assert.Equal("c", ps.Identifier.ToString());

            Assert.NotEqual(default, ps.AccessorList.OpenBraceToken);
            Assert.NotEqual(default, ps.AccessorList.CloseBraceToken);
            Assert.Equal(2, ps.AccessorList.Accessors.Count);

            Assert.Equal(0, ps.AccessorList.Accessors[0].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[0].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].Keyword);
            Assert.Equal(SyntaxKind.GetKeyword, ps.AccessorList.Accessors[0].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[0].Body);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].SemicolonToken);

            Assert.Equal(0, ps.AccessorList.Accessors[1].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[1].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].Keyword);
            Assert.Equal(SyntaxKind.SetKeyword, ps.AccessorList.Accessors[1].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[1].Body);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].SemicolonToken);
        }

        [Fact]
        public void TestClassPropertyWithModifiers()
        {
            TestClassPropertyWithModifier(SyntaxKind.PublicKeyword);
            TestClassPropertyWithModifier(SyntaxKind.PrivateKeyword);
            TestClassPropertyWithModifier(SyntaxKind.ProtectedKeyword);
            TestClassPropertyWithModifier(SyntaxKind.InternalKeyword);
            TestClassPropertyWithModifier(SyntaxKind.StaticKeyword);
            TestClassPropertyWithModifier(SyntaxKind.AbstractKeyword);
            TestClassPropertyWithModifier(SyntaxKind.VirtualKeyword);
            TestClassPropertyWithModifier(SyntaxKind.OverrideKeyword);
            TestClassPropertyWithModifier(SyntaxKind.NewKeyword);
            TestClassPropertyWithModifier(SyntaxKind.SealedKeyword);
        }

        private void TestClassPropertyWithAccessorModifier(SyntaxKind mod)
        {
            var text = "class a { b c { " + SyntaxFacts.GetText(mod) + " get { } } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.PropertyDeclaration, cs.Members[0].Kind());
            var ps = (PropertyDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ps.AttributeLists.Count);
            Assert.Equal(0, ps.Modifiers.Count);
            Assert.NotNull(ps.Type);
            Assert.Equal("b", ps.Type.ToString());
            Assert.NotEqual(default, ps.Identifier);
            Assert.Equal("c", ps.Identifier.ToString());

            Assert.NotEqual(default, ps.AccessorList.OpenBraceToken);
            Assert.NotEqual(default, ps.AccessorList.CloseBraceToken);

            Assert.Equal(1, ps.AccessorList.Accessors.Count);

            Assert.Equal(0, ps.AccessorList.Accessors[0].AttributeLists.Count);
            Assert.Equal(1, ps.AccessorList.Accessors[0].Modifiers.Count);
            Assert.Equal(mod, ps.AccessorList.Accessors[0].Modifiers[0].Kind());
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].Keyword);
            Assert.Equal(SyntaxKind.GetKeyword, ps.AccessorList.Accessors[0].Keyword.Kind());
            Assert.NotNull(ps.AccessorList.Accessors[0].Body);
            Assert.Equal(SyntaxKind.None, ps.AccessorList.Accessors[0].SemicolonToken.Kind());
        }

        [Fact]
        public void TestClassPropertyWithAccessorModifiers()
        {
            TestClassPropertyWithModifier(SyntaxKind.PublicKeyword);
            TestClassPropertyWithModifier(SyntaxKind.PrivateKeyword);
            TestClassPropertyWithModifier(SyntaxKind.ProtectedKeyword);
            TestClassPropertyWithModifier(SyntaxKind.InternalKeyword);
            TestClassPropertyWithModifier(SyntaxKind.AbstractKeyword);
            TestClassPropertyWithModifier(SyntaxKind.VirtualKeyword);
            TestClassPropertyWithModifier(SyntaxKind.OverrideKeyword);
            TestClassPropertyWithModifier(SyntaxKind.NewKeyword);
            TestClassPropertyWithModifier(SyntaxKind.SealedKeyword);
        }

        [Fact]
        public void TestClassPropertyExplicit()
        {
            var text = "class a { b I.c { get; set; } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.PropertyDeclaration, cs.Members[0].Kind());
            var ps = (PropertyDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ps.AttributeLists.Count);
            Assert.Equal(0, ps.Modifiers.Count);
            Assert.NotNull(ps.Type);
            Assert.Equal("b", ps.Type.ToString());
            Assert.NotEqual(default, ps.Identifier);
            Assert.NotNull(ps.ExplicitInterfaceSpecifier);
            Assert.Equal("I", ps.ExplicitInterfaceSpecifier.Name.ToString());
            Assert.Equal("c", ps.Identifier.ToString());

            Assert.NotEqual(default, ps.AccessorList.OpenBraceToken);
            Assert.NotEqual(default, ps.AccessorList.CloseBraceToken);
            Assert.Equal(2, ps.AccessorList.Accessors.Count);

            Assert.Equal(0, ps.AccessorList.Accessors[0].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[0].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].Keyword);
            Assert.Equal(SyntaxKind.GetKeyword, ps.AccessorList.Accessors[0].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[0].Body);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].SemicolonToken);

            Assert.Equal(0, ps.AccessorList.Accessors[1].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[1].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].Keyword);
            Assert.Equal(SyntaxKind.SetKeyword, ps.AccessorList.Accessors[1].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[1].Body);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].SemicolonToken);
        }

        [Fact]
        public void TestClassEventProperty()
        {
            var text = "class a { event b c { add { } remove { } } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.EventDeclaration, cs.Members[0].Kind());
            var es = (EventDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, es.AttributeLists.Count);
            Assert.Equal(0, es.Modifiers.Count);
            Assert.NotEqual(default, es.EventKeyword);
            Assert.NotNull(es.Type);
            Assert.Equal("b", es.Type.ToString());
            Assert.NotEqual(default, es.Identifier);
            Assert.Equal("c", es.Identifier.ToString());

            Assert.NotEqual(default, es.AccessorList.OpenBraceToken);
            Assert.NotEqual(default, es.AccessorList.CloseBraceToken);
            Assert.Equal(2, es.AccessorList.Accessors.Count);

            Assert.Equal(0, es.AccessorList.Accessors[0].AttributeLists.Count);
            Assert.Equal(0, es.AccessorList.Accessors[0].Modifiers.Count);
            Assert.NotEqual(default, es.AccessorList.Accessors[0].Keyword);
            Assert.Equal(SyntaxKind.AddKeyword, es.AccessorList.Accessors[0].Keyword.Kind());
            Assert.NotNull(es.AccessorList.Accessors[0].Body);
            Assert.Equal(SyntaxKind.None, es.AccessorList.Accessors[0].SemicolonToken.Kind());

            Assert.Equal(0, es.AccessorList.Accessors[1].AttributeLists.Count);
            Assert.Equal(0, es.AccessorList.Accessors[1].Modifiers.Count);
            Assert.NotEqual(default, es.AccessorList.Accessors[1].Keyword);
            Assert.Equal(SyntaxKind.RemoveKeyword, es.AccessorList.Accessors[1].Keyword.Kind());
            Assert.NotNull(es.AccessorList.Accessors[1].Body);
            Assert.Equal(SyntaxKind.None, es.AccessorList.Accessors[1].SemicolonToken.Kind());
        }

        private void TestClassEventPropertyWithModifier(SyntaxKind mod)
        {
            var text = "class a { " + SyntaxFacts.GetText(mod) + " event b c { add { } remove { } } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.EventDeclaration, cs.Members[0].Kind());
            var es = (EventDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, es.AttributeLists.Count);
            Assert.Equal(1, es.Modifiers.Count);
            Assert.Equal(mod, es.Modifiers[0].Kind());
            Assert.NotEqual(default, es.EventKeyword);
            Assert.NotNull(es.Type);
            Assert.Equal("b", es.Type.ToString());
            Assert.NotEqual(default, es.Identifier);
            Assert.Equal("c", es.Identifier.ToString());

            Assert.NotEqual(default, es.AccessorList.OpenBraceToken);
            Assert.NotEqual(default, es.AccessorList.CloseBraceToken);
            Assert.Equal(2, es.AccessorList.Accessors.Count);

            Assert.Equal(0, es.AccessorList.Accessors[0].AttributeLists.Count);
            Assert.Equal(0, es.AccessorList.Accessors[0].Modifiers.Count);
            Assert.NotEqual(default, es.AccessorList.Accessors[0].Keyword);
            Assert.Equal(SyntaxKind.AddKeyword, es.AccessorList.Accessors[0].Keyword.Kind());
            Assert.NotNull(es.AccessorList.Accessors[0].Body);
            Assert.Equal(SyntaxKind.None, es.AccessorList.Accessors[0].SemicolonToken.Kind());

            Assert.Equal(0, es.AccessorList.Accessors[1].AttributeLists.Count);
            Assert.Equal(0, es.AccessorList.Accessors[1].Modifiers.Count);
            Assert.NotEqual(default, es.AccessorList.Accessors[1].Keyword);
            Assert.Equal(SyntaxKind.RemoveKeyword, es.AccessorList.Accessors[1].Keyword.Kind());
            Assert.NotNull(es.AccessorList.Accessors[1].Body);
            Assert.Equal(SyntaxKind.None, es.AccessorList.Accessors[1].SemicolonToken.Kind());
        }

        [Fact]
        public void TestClassEventPropertyWithModifiers()
        {
            TestClassEventPropertyWithModifier(SyntaxKind.PublicKeyword);
            TestClassEventPropertyWithModifier(SyntaxKind.PrivateKeyword);
            TestClassEventPropertyWithModifier(SyntaxKind.ProtectedKeyword);
            TestClassEventPropertyWithModifier(SyntaxKind.InternalKeyword);
            TestClassEventPropertyWithModifier(SyntaxKind.StaticKeyword);
            TestClassEventPropertyWithModifier(SyntaxKind.AbstractKeyword);
            TestClassEventPropertyWithModifier(SyntaxKind.VirtualKeyword);
            TestClassEventPropertyWithModifier(SyntaxKind.OverrideKeyword);
        }

        private void TestClassEventPropertyWithAccessorModifier(SyntaxKind mod)
        {
            var text = "class a { event b c { " + SyntaxFacts.GetText(mod) + " add { } } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.PropertyDeclaration, cs.Members[0].Kind());
            var ps = (PropertyDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ps.AttributeLists.Count);
            Assert.Equal(1, ps.Modifiers.Count);
            Assert.Equal(SyntaxKind.EventKeyword, ps.Modifiers[0].Kind());
            Assert.NotNull(ps.Type);
            Assert.Equal("b", ps.Type.ToString());
            Assert.NotEqual(default, ps.Identifier);
            Assert.Equal("c", ps.Identifier.ToString());

            Assert.NotEqual(default, ps.AccessorList.OpenBraceToken);
            Assert.NotEqual(default, ps.AccessorList.CloseBraceToken);

            Assert.Equal(1, ps.AccessorList.Accessors.Count);

            Assert.Equal(0, ps.AccessorList.Accessors[0].AttributeLists.Count);
            Assert.Equal(1, ps.AccessorList.Accessors[0].Modifiers.Count);
            Assert.Equal(mod, ps.AccessorList.Accessors[0].Modifiers[0].Kind());
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].Keyword);
            Assert.Equal(SyntaxKind.AddKeyword, ps.AccessorList.Accessors[0].Keyword.Kind());
            Assert.NotNull(ps.AccessorList.Accessors[0].Body);
            Assert.Equal(SyntaxKind.None, ps.AccessorList.Accessors[0].SemicolonToken.Kind());
        }

        [Fact]
        public void TestClassEventPropertyWithAccessorModifiers()
        {
            TestClassEventPropertyWithModifier(SyntaxKind.PublicKeyword);
            TestClassEventPropertyWithModifier(SyntaxKind.PrivateKeyword);
            TestClassEventPropertyWithModifier(SyntaxKind.ProtectedKeyword);
            TestClassEventPropertyWithModifier(SyntaxKind.InternalKeyword);
            TestClassEventPropertyWithModifier(SyntaxKind.AbstractKeyword);
            TestClassEventPropertyWithModifier(SyntaxKind.VirtualKeyword);
            TestClassEventPropertyWithModifier(SyntaxKind.OverrideKeyword);
            TestClassEventPropertyWithModifier(SyntaxKind.NewKeyword);
            TestClassEventPropertyWithModifier(SyntaxKind.SealedKeyword);
        }

        [Fact]
        public void TestClassEventPropertyExplicit()
        {
            var text = "class a { event b I.c { add { } remove { } } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.EventDeclaration, cs.Members[0].Kind());
            var es = (EventDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, es.AttributeLists.Count);
            Assert.Equal(0, es.Modifiers.Count);
            Assert.NotEqual(default, es.EventKeyword);
            Assert.NotNull(es.Type);
            Assert.Equal("b", es.Type.ToString());
            Assert.NotEqual(default, es.Identifier);
            Assert.NotNull(es.ExplicitInterfaceSpecifier);
            Assert.Equal("I", es.ExplicitInterfaceSpecifier.Name.ToString());
            Assert.Equal("c", es.Identifier.ToString());

            Assert.NotEqual(default, es.AccessorList.OpenBraceToken);
            Assert.NotEqual(default, es.AccessorList.CloseBraceToken);
            Assert.Equal(2, es.AccessorList.Accessors.Count);

            Assert.Equal(0, es.AccessorList.Accessors[0].AttributeLists.Count);
            Assert.Equal(0, es.AccessorList.Accessors[0].Modifiers.Count);
            Assert.NotEqual(default, es.AccessorList.Accessors[0].Keyword);
            Assert.Equal(SyntaxKind.AddKeyword, es.AccessorList.Accessors[0].Keyword.Kind());
            Assert.NotNull(es.AccessorList.Accessors[0].Body);
            Assert.Equal(SyntaxKind.None, es.AccessorList.Accessors[0].SemicolonToken.Kind());

            Assert.Equal(0, es.AccessorList.Accessors[1].AttributeLists.Count);
            Assert.Equal(0, es.AccessorList.Accessors[1].Modifiers.Count);
            Assert.NotEqual(default, es.AccessorList.Accessors[1].Keyword);
            Assert.Equal(SyntaxKind.RemoveKeyword, es.AccessorList.Accessors[1].Keyword.Kind());
            Assert.NotNull(es.AccessorList.Accessors[1].Body);
            Assert.Equal(SyntaxKind.None, es.AccessorList.Accessors[1].SemicolonToken.Kind());
        }

        [Fact]
        public void TestClassIndexer()
        {
            var text = "class a { b this[c d] { get; set; } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.IndexerDeclaration, cs.Members[0].Kind());
            var ps = (IndexerDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ps.AttributeLists.Count);
            Assert.Equal(0, ps.Modifiers.Count);
            Assert.NotNull(ps.Type);
            Assert.Equal("b", ps.Type.ToString());
            Assert.NotEqual(default, ps.ThisKeyword);
            Assert.Equal("this", ps.ThisKeyword.ToString());

            Assert.NotNull(ps.ParameterList); // used with indexer property
            Assert.NotEqual(default, ps.ParameterList.OpenBracketToken);
            Assert.Equal(SyntaxKind.OpenBracketToken, ps.ParameterList.OpenBracketToken.Kind());
            Assert.NotEqual(default, ps.ParameterList.CloseBracketToken);
            Assert.Equal(SyntaxKind.CloseBracketToken, ps.ParameterList.CloseBracketToken.Kind());
            Assert.Equal(1, ps.ParameterList.Parameters.Count);
            Assert.Equal(0, ps.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal(0, ps.ParameterList.Parameters[0].Modifiers.Count);
            Assert.NotNull(ps.ParameterList.Parameters[0].Type);
            Assert.Equal("c", ps.ParameterList.Parameters[0].Type.ToString());
            Assert.NotEqual(default, ps.ParameterList.Parameters[0].Identifier);
            Assert.Equal("d", ps.ParameterList.Parameters[0].Identifier.ToString());

            Assert.NotEqual(default, ps.AccessorList.OpenBraceToken);
            Assert.NotEqual(default, ps.AccessorList.CloseBraceToken);
            Assert.Equal(2, ps.AccessorList.Accessors.Count);

            Assert.Equal(0, ps.AccessorList.Accessors[0].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[0].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].Keyword);
            Assert.Equal(SyntaxKind.GetKeyword, ps.AccessorList.Accessors[0].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[0].Body);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].SemicolonToken);

            Assert.Equal(0, ps.AccessorList.Accessors[1].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[1].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].Keyword);
            Assert.Equal(SyntaxKind.SetKeyword, ps.AccessorList.Accessors[1].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[1].Body);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].SemicolonToken);
        }

        [Fact]
        public void TestClassIndexerWithRefReturn()
        {
            var text = "class a { ref b this[c d] { get; set; } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.IndexerDeclaration, cs.Members[0].Kind());
            var ps = (IndexerDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ps.AttributeLists.Count);
            Assert.Equal(0, ps.Modifiers.Count);
            Assert.NotNull(ps.Type);
            Assert.Equal("ref b", ps.Type.ToString());
            Assert.NotEqual(default, ps.ThisKeyword);
            Assert.Equal("this", ps.ThisKeyword.ToString());

            Assert.NotNull(ps.ParameterList); // used with indexer property
            Assert.NotEqual(default, ps.ParameterList.OpenBracketToken);
            Assert.Equal(SyntaxKind.OpenBracketToken, ps.ParameterList.OpenBracketToken.Kind());
            Assert.NotEqual(default, ps.ParameterList.CloseBracketToken);
            Assert.Equal(SyntaxKind.CloseBracketToken, ps.ParameterList.CloseBracketToken.Kind());
            Assert.Equal(1, ps.ParameterList.Parameters.Count);
            Assert.Equal(0, ps.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal(0, ps.ParameterList.Parameters[0].Modifiers.Count);
            Assert.NotNull(ps.ParameterList.Parameters[0].Type);
            Assert.Equal("c", ps.ParameterList.Parameters[0].Type.ToString());
            Assert.NotEqual(default, ps.ParameterList.Parameters[0].Identifier);
            Assert.Equal("d", ps.ParameterList.Parameters[0].Identifier.ToString());

            Assert.NotEqual(default, ps.AccessorList.OpenBraceToken);
            Assert.NotEqual(default, ps.AccessorList.CloseBraceToken);
            Assert.Equal(2, ps.AccessorList.Accessors.Count);

            Assert.Equal(0, ps.AccessorList.Accessors[0].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[0].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].Keyword);
            Assert.Equal(SyntaxKind.GetKeyword, ps.AccessorList.Accessors[0].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[0].Body);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].SemicolonToken);

            Assert.Equal(0, ps.AccessorList.Accessors[1].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[1].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].Keyword);
            Assert.Equal(SyntaxKind.SetKeyword, ps.AccessorList.Accessors[1].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[1].Body);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].SemicolonToken);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public void TestClassIndexerWithRefReadonlyReturn()
        {
            var text = "class a { ref readonly b this[c d] { get; set; } }";
            var file = this.ParseFile(text, TestOptions.Regular);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.IndexerDeclaration, cs.Members[0].Kind());
            var ps = (IndexerDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ps.AttributeLists.Count);
            Assert.Equal(0, ps.Modifiers.Count);
            Assert.NotNull(ps.Type);
            Assert.Equal("ref readonly b", ps.Type.ToString());
            Assert.NotEqual(default, ps.ThisKeyword);
            Assert.Equal("this", ps.ThisKeyword.ToString());

            Assert.NotNull(ps.ParameterList); // used with indexer property
            Assert.NotEqual(default, ps.ParameterList.OpenBracketToken);
            Assert.Equal(SyntaxKind.OpenBracketToken, ps.ParameterList.OpenBracketToken.Kind());
            Assert.NotEqual(default, ps.ParameterList.CloseBracketToken);
            Assert.Equal(SyntaxKind.CloseBracketToken, ps.ParameterList.CloseBracketToken.Kind());
            Assert.Equal(1, ps.ParameterList.Parameters.Count);
            Assert.Equal(0, ps.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal(0, ps.ParameterList.Parameters[0].Modifiers.Count);
            Assert.NotNull(ps.ParameterList.Parameters[0].Type);
            Assert.Equal("c", ps.ParameterList.Parameters[0].Type.ToString());
            Assert.NotEqual(default, ps.ParameterList.Parameters[0].Identifier);
            Assert.Equal("d", ps.ParameterList.Parameters[0].Identifier.ToString());

            Assert.NotEqual(default, ps.AccessorList.OpenBraceToken);
            Assert.NotEqual(default, ps.AccessorList.CloseBraceToken);
            Assert.Equal(2, ps.AccessorList.Accessors.Count);

            Assert.Equal(0, ps.AccessorList.Accessors[0].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[0].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].Keyword);
            Assert.Equal(SyntaxKind.GetKeyword, ps.AccessorList.Accessors[0].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[0].Body);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].SemicolonToken);

            Assert.Equal(0, ps.AccessorList.Accessors[1].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[1].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].Keyword);
            Assert.Equal(SyntaxKind.SetKeyword, ps.AccessorList.Accessors[1].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[1].Body);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].SemicolonToken);
        }

        [Fact]
        public void TestClassIndexerWithMultipleParameters()
        {
            var text = "class a { b this[c d, e f] { get; set; } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.IndexerDeclaration, cs.Members[0].Kind());
            var ps = (IndexerDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ps.AttributeLists.Count);
            Assert.Equal(0, ps.Modifiers.Count);
            Assert.NotNull(ps.Type);
            Assert.Equal("b", ps.Type.ToString());
            Assert.NotEqual(default, ps.ThisKeyword);
            Assert.Equal("this", ps.ThisKeyword.ToString());

            Assert.NotNull(ps.ParameterList); // used with indexer property
            Assert.NotEqual(default, ps.ParameterList.OpenBracketToken);
            Assert.Equal(SyntaxKind.OpenBracketToken, ps.ParameterList.OpenBracketToken.Kind());
            Assert.NotEqual(default, ps.ParameterList.CloseBracketToken);
            Assert.Equal(SyntaxKind.CloseBracketToken, ps.ParameterList.CloseBracketToken.Kind());

            Assert.Equal(2, ps.ParameterList.Parameters.Count);

            Assert.Equal(0, ps.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal(0, ps.ParameterList.Parameters[0].Modifiers.Count);
            Assert.NotNull(ps.ParameterList.Parameters[0].Type);
            Assert.Equal("c", ps.ParameterList.Parameters[0].Type.ToString());
            Assert.NotEqual(default, ps.ParameterList.Parameters[0].Identifier);
            Assert.Equal("d", ps.ParameterList.Parameters[0].Identifier.ToString());

            Assert.Equal(0, ps.ParameterList.Parameters[1].AttributeLists.Count);
            Assert.Equal(0, ps.ParameterList.Parameters[1].Modifiers.Count);
            Assert.NotNull(ps.ParameterList.Parameters[1].Type);
            Assert.Equal("e", ps.ParameterList.Parameters[1].Type.ToString());
            Assert.NotEqual(default, ps.ParameterList.Parameters[1].Identifier);
            Assert.Equal("f", ps.ParameterList.Parameters[1].Identifier.ToString());

            Assert.NotEqual(default, ps.AccessorList.OpenBraceToken);
            Assert.NotEqual(default, ps.AccessorList.CloseBraceToken);
            Assert.Equal(2, ps.AccessorList.Accessors.Count);

            Assert.Equal(0, ps.AccessorList.Accessors[0].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[0].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].Keyword);
            Assert.Equal(SyntaxKind.GetKeyword, ps.AccessorList.Accessors[0].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[0].Body);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].SemicolonToken);

            Assert.Equal(0, ps.AccessorList.Accessors[1].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[1].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].Keyword);
            Assert.Equal(SyntaxKind.SetKeyword, ps.AccessorList.Accessors[1].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[1].Body);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].SemicolonToken);
        }

        [Fact]
        public void TestClassIndexerExplicit()
        {
            var text = "class a { b I.this[c d] { get; set; } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.IndexerDeclaration, cs.Members[0].Kind());
            var ps = (IndexerDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ps.AttributeLists.Count);
            Assert.Equal(0, ps.Modifiers.Count);
            Assert.NotNull(ps.Type);
            Assert.Equal("b", ps.Type.ToString());
            Assert.NotNull(ps.ExplicitInterfaceSpecifier);
            Assert.Equal("I", ps.ExplicitInterfaceSpecifier.Name.ToString());
            Assert.Equal(".", ps.ExplicitInterfaceSpecifier.DotToken.ToString());
            Assert.Equal("this", ps.ThisKeyword.ToString());

            Assert.NotNull(ps.ParameterList); // used with indexer property
            Assert.NotEqual(default, ps.ParameterList.OpenBracketToken);
            Assert.Equal(SyntaxKind.OpenBracketToken, ps.ParameterList.OpenBracketToken.Kind());
            Assert.NotEqual(default, ps.ParameterList.CloseBracketToken);
            Assert.Equal(SyntaxKind.CloseBracketToken, ps.ParameterList.CloseBracketToken.Kind());
            Assert.Equal(1, ps.ParameterList.Parameters.Count);
            Assert.Equal(0, ps.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal(0, ps.ParameterList.Parameters[0].Modifiers.Count);
            Assert.NotNull(ps.ParameterList.Parameters[0].Type);
            Assert.Equal("c", ps.ParameterList.Parameters[0].Type.ToString());
            Assert.NotEqual(default, ps.ParameterList.Parameters[0].Identifier);
            Assert.Equal("d", ps.ParameterList.Parameters[0].Identifier.ToString());

            Assert.NotEqual(default, ps.AccessorList.OpenBraceToken);
            Assert.NotEqual(default, ps.AccessorList.CloseBraceToken);
            Assert.Equal(2, ps.AccessorList.Accessors.Count);

            Assert.Equal(0, ps.AccessorList.Accessors[0].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[0].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].Keyword);
            Assert.Equal(SyntaxKind.GetKeyword, ps.AccessorList.Accessors[0].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[0].Body);
            Assert.NotEqual(default, ps.AccessorList.Accessors[0].SemicolonToken);

            Assert.Equal(0, ps.AccessorList.Accessors[1].AttributeLists.Count);
            Assert.Equal(0, ps.AccessorList.Accessors[1].Modifiers.Count);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].Keyword);
            Assert.Equal(SyntaxKind.SetKeyword, ps.AccessorList.Accessors[1].Keyword.Kind());
            Assert.Null(ps.AccessorList.Accessors[1].Body);
            Assert.NotEqual(default, ps.AccessorList.Accessors[1].SemicolonToken);
        }

        private void TestClassBinaryOperatorMethod(SyntaxKind op1)
        {
            var text = "class a { b operator " + SyntaxFacts.GetText(op1) + " (c d, e f) { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.OperatorDeclaration, cs.Members[0].Kind());
            var ps = (OperatorDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ps.AttributeLists.Count);
            Assert.Equal(0, ps.Modifiers.Count);
            Assert.NotNull(ps.ReturnType);
            Assert.Equal("b", ps.ReturnType.ToString());
            Assert.NotEqual(default, ps.OperatorKeyword);
            Assert.Equal(SyntaxKind.OperatorKeyword, ps.OperatorKeyword.Kind());
            Assert.NotEqual(default, ps.OperatorToken);
            Assert.Equal(op1, ps.OperatorToken.Kind());
            Assert.NotEqual(default, ps.ParameterList.OpenParenToken);
            Assert.NotEqual(default, ps.ParameterList.CloseParenToken);
            Assert.NotNull(ps.Body);

            Assert.Equal(2, ps.ParameterList.Parameters.Count);

            Assert.Equal(0, ps.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal(0, ps.ParameterList.Parameters[0].Modifiers.Count);
            Assert.NotNull(ps.ParameterList.Parameters[0].Type);
            Assert.Equal("c", ps.ParameterList.Parameters[0].Type.ToString());
            Assert.NotEqual(default, ps.ParameterList.Parameters[0].Identifier);
            Assert.Equal("d", ps.ParameterList.Parameters[0].Identifier.ToString());

            Assert.Equal(0, ps.ParameterList.Parameters[1].AttributeLists.Count);
            Assert.Equal(0, ps.ParameterList.Parameters[1].Modifiers.Count);
            Assert.NotNull(ps.ParameterList.Parameters[1].Type);
            Assert.Equal("e", ps.ParameterList.Parameters[1].Type.ToString());
            Assert.NotEqual(default, ps.ParameterList.Parameters[1].Identifier);
            Assert.Equal("f", ps.ParameterList.Parameters[1].Identifier.ToString());
        }

        [Fact]
        public void TestClassBinaryOperatorMethods()
        {
            TestClassBinaryOperatorMethod(SyntaxKind.PlusToken);
            TestClassBinaryOperatorMethod(SyntaxKind.MinusToken);
            TestClassBinaryOperatorMethod(SyntaxKind.AsteriskToken);
            TestClassBinaryOperatorMethod(SyntaxKind.SlashToken);
            TestClassBinaryOperatorMethod(SyntaxKind.PercentToken);
            TestClassBinaryOperatorMethod(SyntaxKind.CaretToken);
            TestClassBinaryOperatorMethod(SyntaxKind.AmpersandToken);
            TestClassBinaryOperatorMethod(SyntaxKind.BarToken);

            // TestClassBinaryOperatorMethod(SyntaxKind.AmpersandAmpersandToken);
            // TestClassBinaryOperatorMethod(SyntaxKind.BarBarToken);
            TestClassBinaryOperatorMethod(SyntaxKind.LessThanToken);
            TestClassBinaryOperatorMethod(SyntaxKind.LessThanEqualsToken);
            TestClassBinaryOperatorMethod(SyntaxKind.LessThanLessThanToken);
            TestClassBinaryOperatorMethod(SyntaxKind.GreaterThanToken);
            TestClassBinaryOperatorMethod(SyntaxKind.GreaterThanEqualsToken);
            TestClassBinaryOperatorMethod(SyntaxKind.EqualsEqualsToken);
            TestClassBinaryOperatorMethod(SyntaxKind.ExclamationEqualsToken);
        }

        [Fact]
        public void TestClassRightShiftOperatorMethod()
        {
            var text = "class a { b operator >> (c d, e f) { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.OperatorDeclaration, cs.Members[0].Kind());
            var ps = (OperatorDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ps.AttributeLists.Count);
            Assert.Equal(0, ps.Modifiers.Count);
            Assert.NotNull(ps.ReturnType);
            Assert.Equal("b", ps.ReturnType.ToString());
            Assert.NotEqual(default, ps.OperatorKeyword);
            Assert.Equal(SyntaxKind.OperatorKeyword, ps.OperatorKeyword.Kind());
            Assert.NotEqual(default, ps.OperatorToken);
            Assert.Equal(SyntaxKind.GreaterThanGreaterThanToken, ps.OperatorToken.Kind());
            Assert.NotEqual(default, ps.ParameterList.OpenParenToken);
            Assert.NotEqual(default, ps.ParameterList.CloseParenToken);
            Assert.NotNull(ps.Body);

            Assert.Equal(2, ps.ParameterList.Parameters.Count);

            Assert.Equal(0, ps.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal(0, ps.ParameterList.Parameters[0].Modifiers.Count);
            Assert.NotNull(ps.ParameterList.Parameters[0].Type);
            Assert.Equal("c", ps.ParameterList.Parameters[0].Type.ToString());
            Assert.NotEqual(default, ps.ParameterList.Parameters[0].Identifier);
            Assert.Equal("d", ps.ParameterList.Parameters[0].Identifier.ToString());

            Assert.Equal(0, ps.ParameterList.Parameters[1].AttributeLists.Count);
            Assert.Equal(0, ps.ParameterList.Parameters[1].Modifiers.Count);
            Assert.NotNull(ps.ParameterList.Parameters[1].Type);
            Assert.Equal("e", ps.ParameterList.Parameters[1].Type.ToString());
            Assert.NotEqual(default, ps.ParameterList.Parameters[1].Identifier);
            Assert.Equal("f", ps.ParameterList.Parameters[1].Identifier.ToString());
        }

        private void TestClassUnaryOperatorMethod(SyntaxKind op1)
        {
            var text = "class a { b operator " + SyntaxFacts.GetText(op1) + " (c d) { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.OperatorDeclaration, cs.Members[0].Kind());
            var ps = (OperatorDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ps.AttributeLists.Count);
            Assert.Equal(0, ps.Modifiers.Count);
            Assert.NotNull(ps.ReturnType);
            Assert.Equal("b", ps.ReturnType.ToString());
            Assert.NotEqual(default, ps.OperatorKeyword);
            Assert.Equal(SyntaxKind.OperatorKeyword, ps.OperatorKeyword.Kind());
            Assert.NotEqual(default, ps.OperatorToken);
            Assert.Equal(op1, ps.OperatorToken.Kind());
            Assert.NotEqual(default, ps.ParameterList.OpenParenToken);
            Assert.NotEqual(default, ps.ParameterList.CloseParenToken);
            Assert.NotNull(ps.Body);

            Assert.Equal(1, ps.ParameterList.Parameters.Count);

            Assert.Equal(0, ps.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal(0, ps.ParameterList.Parameters[0].Modifiers.Count);
            Assert.NotNull(ps.ParameterList.Parameters[0].Type);
            Assert.Equal("c", ps.ParameterList.Parameters[0].Type.ToString());
            Assert.NotEqual(default, ps.ParameterList.Parameters[0].Identifier);
            Assert.Equal("d", ps.ParameterList.Parameters[0].Identifier.ToString());
        }

        [Fact]
        public void TestClassUnaryOperatorMethods()
        {
            TestClassUnaryOperatorMethod(SyntaxKind.PlusToken);
            TestClassUnaryOperatorMethod(SyntaxKind.MinusToken);
            TestClassUnaryOperatorMethod(SyntaxKind.TildeToken);
            TestClassUnaryOperatorMethod(SyntaxKind.ExclamationToken);
            TestClassUnaryOperatorMethod(SyntaxKind.PlusPlusToken);
            TestClassUnaryOperatorMethod(SyntaxKind.MinusMinusToken);
            TestClassUnaryOperatorMethod(SyntaxKind.TrueKeyword);
            TestClassUnaryOperatorMethod(SyntaxKind.FalseKeyword);
        }

        [Fact]
        public void TestClassImplicitConversionOperatorMethod()
        {
            var text = "class a { implicit operator b (c d) { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.ConversionOperatorDeclaration, cs.Members[0].Kind());
            var ms = (ConversionOperatorDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ms.AttributeLists.Count);
            Assert.Equal(0, ms.Modifiers.Count);
            Assert.NotEqual(default, ms.ImplicitOrExplicitKeyword);
            Assert.Equal(SyntaxKind.ImplicitKeyword, ms.ImplicitOrExplicitKeyword.Kind());
            Assert.NotEqual(default, ms.OperatorKeyword);
            Assert.Equal(SyntaxKind.OperatorKeyword, ms.OperatorKeyword.Kind());
            Assert.NotNull(ms.Type);
            Assert.Equal("b", ms.Type.ToString());
            Assert.NotEqual(default, ms.ParameterList.OpenParenToken);
            Assert.NotEqual(default, ms.ParameterList.CloseParenToken);

            Assert.Equal(1, ms.ParameterList.Parameters.Count);
            Assert.Equal(0, ms.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal(0, ms.ParameterList.Parameters[0].Modifiers.Count);
            Assert.NotNull(ms.ParameterList.Parameters[0].Type);
            Assert.Equal("c", ms.ParameterList.Parameters[0].Type.ToString());
            Assert.NotEqual(default, ms.ParameterList.Parameters[0].Identifier);
            Assert.Equal("d", ms.ParameterList.Parameters[0].Identifier.ToString());
        }

        [Fact]
        public void TestClassExplicitConversionOperatorMethod()
        {
            var text = "class a { explicit operator b (c d) { } }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(text, file.ToString());
            Assert.Equal(0, file.Errors().Length);

            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var cs = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(0, cs.AttributeLists.Count);
            Assert.Equal(0, cs.Modifiers.Count);
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.ClassKeyword, cs.Keyword.Kind());
            Assert.NotEqual(default, cs.Identifier);
            Assert.Equal("a", cs.Identifier.ToString());
            Assert.Null(cs.BaseList);
            Assert.Equal(0, cs.ConstraintClauses.Count);
            Assert.NotEqual(default, cs.OpenBraceToken);
            Assert.NotEqual(default, cs.CloseBraceToken);

            Assert.Equal(1, cs.Members.Count);

            Assert.Equal(SyntaxKind.ConversionOperatorDeclaration, cs.Members[0].Kind());
            var ms = (ConversionOperatorDeclarationSyntax)cs.Members[0];
            Assert.Equal(0, ms.AttributeLists.Count);
            Assert.Equal(0, ms.Modifiers.Count);
            Assert.NotEqual(default, ms.ImplicitOrExplicitKeyword);
            Assert.Equal(SyntaxKind.ExplicitKeyword, ms.ImplicitOrExplicitKeyword.Kind());
            Assert.NotEqual(default, ms.OperatorKeyword);
            Assert.Equal(SyntaxKind.OperatorKeyword, ms.OperatorKeyword.Kind());
            Assert.NotNull(ms.Type);
            Assert.Equal("b", ms.Type.ToString());
            Assert.NotEqual(default, ms.ParameterList.OpenParenToken);
            Assert.NotEqual(default, ms.ParameterList.CloseParenToken);

            Assert.Equal(1, ms.ParameterList.Parameters.Count);
            Assert.Equal(0, ms.ParameterList.Parameters[0].AttributeLists.Count);
            Assert.Equal(0, ms.ParameterList.Parameters[0].Modifiers.Count);
            Assert.NotNull(ms.ParameterList.Parameters[0].Type);
            Assert.Equal("c", ms.ParameterList.Parameters[0].Type.ToString());
            Assert.NotEqual(default, ms.ParameterList.Parameters[0].Identifier);
            Assert.Equal("d", ms.ParameterList.Parameters[0].Identifier.ToString());
        }

        [Fact]
        public void TestNamespaceDeclarationsBadNames()
        {
            var text = "namespace A::B { }";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(0, file.Errors().Length);
            Assert.Equal(text, file.ToString());

            var ns = (NamespaceDeclarationSyntax)file.Members[0];
            Assert.Equal(0, ns.Errors().Length);
            Assert.Equal(SyntaxKind.AliasQualifiedName, ns.Name.Kind());

            text = "namespace A<B> { }";
            file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(0, file.Errors().Length);
            Assert.Equal(text, file.ToString());

            ns = (NamespaceDeclarationSyntax)file.Members[0];
            Assert.Equal(0, ns.Errors().Length);
            Assert.Equal(SyntaxKind.GenericName, ns.Name.Kind());

            text = "namespace A<,> { }";
            file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(0, file.Errors().Length);
            Assert.Equal(text, file.ToString());

            ns = (NamespaceDeclarationSyntax)file.Members[0];
            Assert.Equal(0, ns.Errors().Length);
            Assert.Equal(SyntaxKind.GenericName, ns.Name.Kind());
        }

        [Fact]
        public void TestNamespaceDeclarationsBadNames1()
        {
            var text = @"namespace A::B { }";
            CreateCompilation(text).VerifyDiagnostics(
                // (1,11): error CS7000: Unexpected use of an aliased name
                // namespace A::B { }
                Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "A::B").WithLocation(1, 11));
        }

        [Fact]
        public void TestNamespaceDeclarationsBadNames2()
        {
            var text = @"namespace A<B> { }";
            CreateCompilation(text).VerifyDiagnostics(
                // (1,11): error CS7002: Unexpected use of a generic name
                // namespace A<B> { }
                Diagnostic(ErrorCode.ERR_UnexpectedGenericName, "A<B>").WithLocation(1, 11));
        }

        [Fact]
        public void TestNamespaceDeclarationsBadNames3()
        {
            var text = @"namespace A<,> { }";
            CreateCompilation(text).VerifyDiagnostics(
                // (1,11): error CS7002: Unexpected use of a generic name
                // namespace A<,> { }
                Diagnostic(ErrorCode.ERR_UnexpectedGenericName, "A<,>").WithLocation(1, 11));
        }

        [WorkItem(537690, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537690")]
        [Fact]
        public void TestMissingSemicolonAfterListInitializer()
        {
            var text = @"using System;
using System.Linq;
class Program {
  static void Main() {
    var r = new List<int>() { 3, 3 }
    var s = 2;
  }
}
";
            var file = this.ParseFile(text);
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestPartialPartial()
        {
            var text = @"
partial class PartialPartial
{
    int i = 1;
    partial partial void PM();
    partial partial void PM()
    {
        i = 0;
    }
    static int Main()
    {
        PartialPartial t = new PartialPartial();
        t.PM();
        return t.i;
    }
}
";
            // These errors aren't great.  Ideally we can improve things in the future.
            CreateCompilation(text).VerifyDiagnostics(
                // (5,13): error CS1525: Invalid expression term 'partial'
                //     partial partial void PM();
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(5, 13),
                // (5,13): error CS1002: ; expected
                //     partial partial void PM();
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "partial").WithLocation(5, 13),
                // (6,13): error CS1525: Invalid expression term 'partial'
                //     partial partial void PM()
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(6, 13),
                // (6,13): error CS1002: ; expected
                //     partial partial void PM()
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "partial").WithLocation(6, 13),
                // (6,13): error CS0102: The type 'PartialPartial' already contains a definition for ''
                //     partial partial void PM()
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "").WithArguments("PartialPartial", "").WithLocation(6, 13),
                // (5,5): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
                //     partial partial void PM();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(5, 5),
                // (6,5): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
                //     partial partial void PM()
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(6, 5));
        }

        [Fact]
        public void TestPartialEnum()
        {
            var text = @"partial enum E{}";
            CreateCompilationWithMscorlib45(text).VerifyDiagnostics(
                // (1,1): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'struct', 'interface', or 'void'
                // partial enum E{}
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(1, 1),
                // (1,14): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'struct', 'interface', or 'void'
                // partial enum E{}
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "E").WithLocation(1, 14));
        }

        [WorkItem(539120, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539120")]
        [Fact]
        public void TestEscapedConstructor()
        {
            var text = @"
class @class
{
    public @class()
    {
    }
}
";
            var file = this.ParseFile(text);
            Assert.Equal(0, file.Errors().Length);
        }

        [WorkItem(536956, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536956")]
        [Fact]
        public void TestAnonymousMethodWithDefaultParameter()
        {
            var text = @"
delegate void F(int x);
class C {
   void M() {
     F f = delegate (int x = 0) { };
   }
}
";
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(0, file.Errors().Length);

            CreateCompilation(text).VerifyDiagnostics(
                // (5,28): error CS1065: Default values are not valid in this context.
                //      F f = delegate (int x = 0) { };
                Diagnostic(ErrorCode.ERR_DefaultValueNotAllowed, "=").WithLocation(5, 28));
        }

        [WorkItem(537865, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537865")]
        [Fact]
        public void RegressIfDevTrueUnicode()
        {
            var text = @"
class P
{
static void Main()
{
#if tru\u0065
System.Console.WriteLine(""Good, backwards compatible"");
#else
System.Console.WriteLine(""Bad, breaking change"");
#endif
}
}
";

            TestConditionalCompilation(text, desiredText: "Good", undesiredText: "Bad");
        }

        [WorkItem(537815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537815")]
        [Fact]
        public void RegressLongDirectiveIdentifierDefn()
        {
            var text = @"
//130 chars (max is 128)
#define A234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
class P
{
static void Main()
{
//first 128 chars of defined value
#if A2345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678
System.Console.WriteLine(""Good, backwards compatible"");
#else
System.Console.WriteLine(""Bad, breaking change"");
#endif
}
}
";

            TestConditionalCompilation(text, desiredText: "Good", undesiredText: "Bad");
        }

        [WorkItem(537815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537815")]
        [Fact]
        public void RegressLongDirectiveIdentifierUse()
        {
            var text = @"
//128 chars (max)
#define A2345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678
class P
{
static void Main()
{
//defined value + two chars (larger than max)
#if A234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
System.Console.WriteLine(""Good, backwards compatible"");
#else
System.Console.WriteLine(""Bad, breaking change"");
#endif
}
}
";

            TestConditionalCompilation(text, desiredText: "Good", undesiredText: "Bad");
        }

        //Expects a single class, containing a single method, containing a single statement.
        //Presumably, the statement depends on a conditional compilation directive.
        private void TestConditionalCompilation(string text, string desiredText, string undesiredText)
        {
            var file = this.ParseFile(text);

            Assert.NotNull(file);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(0, file.Errors().Length);
            Assert.Equal(text, file.ToFullString());

            var @class = (TypeDeclarationSyntax)file.Members[0];
            var mainMethod = (MethodDeclarationSyntax)@class.Members[0];

            Assert.NotNull(mainMethod.Body);
            Assert.Equal(1, mainMethod.Body.Statements.Count);

            var statement = mainMethod.Body.Statements[0];
            var stmtText = statement.ToString();

            //make sure we compiled out the right statement
            Assert.Contains(desiredText, stmtText, StringComparison.Ordinal);
            Assert.DoesNotContain(undesiredText, stmtText, StringComparison.Ordinal);
        }

        private void TestError(string text, ErrorCode error)
        {
            var file = this.ParseFile(text);
            Assert.NotNull(file);
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal(error, (ErrorCode)file.Errors()[0].Code);
        }

        [Fact]
        public void TestBadlyPlacedParams()
        {
            var text1 = @"
class C 
{
   void M(params int[] i, int j)  {}
}";
            var text2 = @"
class C 
{
   void M(__arglist, int j)  {}
}";

            CreateCompilation(text1).VerifyDiagnostics(
                // (4,11): error CS0231: A params parameter must be the last parameter in a formal parameter list
                //    void M(params int[] i, int j)  {}
                Diagnostic(ErrorCode.ERR_ParamsLast, "params int[] i").WithLocation(4, 11));
            CreateCompilation(text2).VerifyDiagnostics(
                // (4,11): error CS0257: An __arglist parameter must be the last parameter in a formal parameter list
                //    void M(__arglist, int j)  {}
                Diagnostic(ErrorCode.ERR_VarargsLast, "__arglist").WithLocation(4, 11));
        }

        [Fact]
        public void ValidFixedBufferTypes()
        {
            var text = @"
unsafe struct s
{
    public fixed bool _Type1[10];
    internal fixed int _Type3[10];
    private fixed short _Type4[10];
    unsafe fixed long _Type5[10];
    new fixed char _Type6[10];    
}
";
            var file = this.ParseFile(text);
            Assert.Equal(0, file.Errors().Length);
        }

        [Fact]
        public void ValidFixedBufferTypesMultipleDeclarationsOnSameLine()
        {
            var text = @"
unsafe struct s
{
    public fixed bool _Type1[10], _Type2[10], _Type3[20];
}
";
            var file = this.ParseFile(text);
            Assert.Equal(0, file.Errors().Length);
        }

        [Fact]
        public void ValidFixedBufferTypesWithCountFromConstantOrLiteral()
        {
            var text = @"
unsafe struct s
{
    public const int abc = 10;
    public fixed bool _Type1[abc];
    public fixed bool _Type2[20];
    }
";
            var file = this.ParseFile(text);
            Assert.Equal(0, file.Errors().Length);
        }

        [Fact]
        public void ValidFixedBufferTypesAllValidTypes()
        {
            var text = @"
unsafe struct s
{
    public fixed bool _Type1[10]; 
    public fixed byte _Type12[10]; 
    public fixed int _Type2[10]; 
    public fixed short _Type3[10]; 
    public fixed long _Type4[10]; 
    public fixed char _Type5[10]; 
    public fixed sbyte _Type6[10]; 
    public fixed ushort _Type7[10]; 
    public fixed uint _Type8[10]; 
    public fixed ulong _Type9[10]; 
    public fixed float _Type10[10]; 
    public fixed double _Type11[10];     
 }


";
            var file = this.ParseFile(text);
            Assert.Equal(0, file.Errors().Length);
        }

        [Fact]
        public void CS0071_01()
        {
            UsingTree(@"
public interface I2 { }
public interface I1
{
    event System.Action I2.P10;
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "I2");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "I1");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.EventDeclaration);
                    {
                        N(SyntaxKind.EventKeyword);
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "System");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Action");
                            }
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I2");
                            }
                            N(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.IdentifierToken, "P10");
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void CS0071_02()
        {
            UsingTree(@"
public interface I2 { }
public interface I1
{
    event System.Action I2.
P10;
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "I2");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "I1");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.EventDeclaration);
                    {
                        N(SyntaxKind.EventKeyword);
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "System");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Action");
                            }
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I2");
                            }
                            N(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.IdentifierToken, "P10");
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void CS0071_03()
        {
            UsingTree(@"
public interface I2 { }
public interface I1
{
    event System.Action I2.
P10
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "I2");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "I1");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.EventDeclaration);
                    {
                        N(SyntaxKind.EventKeyword);
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "System");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Action");
                            }
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I2");
                            }
                            N(SyntaxKind.DotToken);
                        }
                        M(SyntaxKind.IdentifierToken);
                        M(SyntaxKind.AccessorList);
                        {
                            M(SyntaxKind.OpenBraceToken);
                            M(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "P10");
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void CS0071_04()
        {
            UsingTree(@"
public interface I2 { }
public interface I1
{
    event System.Action I2.P10
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "I2");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "I1");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.EventDeclaration);
                    {
                        N(SyntaxKind.EventKeyword);
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "System");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Action");
                            }
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I2");
                            }
                            N(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.IdentifierToken, "P10");
                        M(SyntaxKind.AccessorList);
                        {
                            M(SyntaxKind.OpenBraceToken);
                            M(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        [WorkItem(4826, "https://github.com/dotnet/roslyn/pull/4826")]
        public void NonAccessorAfterIncompleteProperty()
        {
            UsingTree(@"
class C
{
    int A { get { return this.
    public int B;
}
");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken);
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
                                        N(SyntaxKind.SimpleMemberAccessExpression);
                                        {
                                            N(SyntaxKind.ThisExpression);
                                            N(SyntaxKind.ThisKeyword);
                                            N(SyntaxKind.DotToken);
                                            N(SyntaxKind.IdentifierName);
                                            N(SyntaxKind.IdentifierToken);
                                        }
                                        N(SyntaxKind.SemicolonToken);
                                    }
                                    N(SyntaxKind.CloseBraceToken);
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
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
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void TupleArgument01()
        {
            var text = @"
class C1
{
    static (T, T) Test1<T>(int a, (byte, byte) arg0)
    {
        return default((T, T));
    }

    static (T, T) Test2<T>(ref (byte, byte) arg0)
    {
        return default((T, T));
    }
}
";
            var file = this.ParseFile(text);
            Assert.Equal(0, file.Errors().Length);
        }

        [Fact]
        public void TupleArgument02()
        {
            var text = @"
class C1
{
    static (T, T) Test3<T>((byte, byte) arg0)
    {
        return default((T, T));
    }

    (T, T) Test3<T>((byte a, byte b)[] arg0)
    {
        return default((T, T));
    }
}
";
            var file = this.ParseFile(text);
            Assert.Equal(0, file.Errors().Length);
        }

        [Fact]
        [WorkItem(13578, "https://github.com/dotnet/roslyn/issues/13578")]
        [CompilerTrait(CompilerFeature.ExpressionBody)]
        public void ExpressionBodiedCtorDtorProp()
        {
            UsingTree(@"
class C
{
    C() : base() => M();
    C() => M();
    ~C() => M();
    int P { set => M(); }
}
");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.ConstructorDeclaration);
                    {
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.BaseConstructorInitializer);
                        {
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.BaseKeyword);
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken);
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
                    N(SyntaxKind.ConstructorDeclaration);
                    {
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken);
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
                    N(SyntaxKind.DestructorDeclaration);
                    {
                        N(SyntaxKind.TildeToken);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken);
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
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SetAccessorDeclaration);
                            {
                                N(SyntaxKind.SetKeyword);
                                N(SyntaxKind.ArrowExpressionClause);
                                {
                                    N(SyntaxKind.EqualsGreaterThanToken);
                                    N(SyntaxKind.InvocationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken);
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
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void ParseOutVar()
        {
            var tree = UsingTree(@"
class C
{
    void Goo()
    {
        M(out var x);
    }
}", options: TestOptions.Regular.WithTuplesFeature());
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
                        N(SyntaxKind.IdentifierToken, "Goo");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.InvocationExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "M");
                                    }
                                    N(SyntaxKind.ArgumentList);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.OutKeyword);
                                            N(SyntaxKind.DeclarationExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "var");
                                                }
                                                N(SyntaxKind.SingleVariableDesignation);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "x");
                                                }
                                            }
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TestPartiallyWrittenConstraintClauseInBaseList1()
        {
            var tree = UsingTree(@"
class C<T> : where
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
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
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "where");
                            }
                        }
                    }
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TestPartiallyWrittenConstraintClauseInBaseList2()
        {
            var tree = UsingTree(@"
class C<T> : where T
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
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
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "where");
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                        }
                    }
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TestPartiallyWrittenConstraintClauseInBaseList3()
        {
            var tree = UsingTree(@"
class C<T> : where T :
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
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
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        M(SyntaxKind.SimpleBaseType);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
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
                        M(SyntaxKind.TypeConstraint);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TestPartiallyWrittenConstraintClauseInBaseList4()
        {
            var tree = UsingTree(@"
class C<T> : where T : X
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
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
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        M(SyntaxKind.SimpleBaseType);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
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
                        N(SyntaxKind.TypeConstraint);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "X");
                            }
                        }
                    }
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        [WorkItem(23833, "https://github.com/dotnet/roslyn/issues/23833")]
        public void ProduceErrorsOnRef_Properties_Ref_Get()
        {
            var code = @"
class Program
{
    public int P
    {
        ref get => throw null;
    }
}";

            CreateCompilation(code).VerifyDiagnostics(
                // (6,13): error CS0106: The modifier 'ref' is not valid for this item
                //         ref get => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("ref").WithLocation(6, 13));
        }

        [Fact]
        [WorkItem(23833, "https://github.com/dotnet/roslyn/issues/23833")]
        public void ProduceErrorsOnRef_Properties_Ref_Get_SecondModifier()
        {
            var code = @"
class Program
{
    public int P
    {
        abstract ref get => throw null;
    }
}";

            CreateCompilation(code).VerifyDiagnostics(
                // (6,22): error CS0106: The modifier 'abstract' is not valid for this item
                //         abstract ref get => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("abstract").WithLocation(6, 22),
                // (6,22): error CS0106: The modifier 'ref' is not valid for this item
                //         abstract ref get => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("ref").WithLocation(6, 22));
        }

        [Fact]
        [WorkItem(23833, "https://github.com/dotnet/roslyn/issues/23833")]
        public void ProduceErrorsOnRef_Properties_Ref_Set()
        {
            var code = @"
class Program
{
    public int P
    {
        ref set => throw null;
    }
}";

            CreateCompilation(code).VerifyDiagnostics(
                // (6,13): error CS0106: The modifier 'ref' is not valid for this item
                //         ref set => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("ref").WithLocation(6, 13));
        }

        [Fact]
        [WorkItem(23833, "https://github.com/dotnet/roslyn/issues/23833")]
        public void ProduceErrorsOnRef_Properties_Ref_Set_SecondModifier()
        {
            var code = @"
class Program
{
    public int P
    {
        abstract ref set => throw null;
    }
}";

            CreateCompilation(code).VerifyDiagnostics(
                // (6,22): error CS0106: The modifier 'abstract' is not valid for this item
                //         abstract ref set => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("abstract").WithLocation(6, 22),
                // (6,22): error CS0106: The modifier 'ref' is not valid for this item
                //         abstract ref set => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("ref").WithLocation(6, 22));
        }

        [Fact]
        [WorkItem(23833, "https://github.com/dotnet/roslyn/issues/23833")]
        public void ProduceErrorsOnRef_Events_Ref()
        {
            var code = @"
public class Program
{
    event System.EventHandler E
    {
        ref add => throw null; 
        ref remove => throw null; 
    }
}";

            CreateCompilation(code).VerifyDiagnostics(
                // (6,9): error CS1609: Modifiers cannot be placed on event accessor declarations
                //         ref add => throw null; 
                Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "ref").WithLocation(6, 9),
                // (7,9): error CS1609: Modifiers cannot be placed on event accessor declarations
                //         ref remove => throw null; 
                Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "ref").WithLocation(7, 9));
        }

        [Fact]
        [WorkItem(23833, "https://github.com/dotnet/roslyn/issues/23833")]
        public void ProduceErrorsOnRef_Events_Ref_SecondModifier()
        {
            var code = @"
public class Program
{
    event System.EventHandler E
    {
        abstract ref add => throw null; 
        abstract ref remove => throw null; 
    }
}";

            CreateCompilation(code).VerifyDiagnostics(
                // (6,9): error CS1609: Modifiers cannot be placed on event accessor declarations
                //         abstract ref add => throw null; 
                Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "abstract").WithLocation(6, 9),
                // (7,9): error CS1609: Modifiers cannot be placed on event accessor declarations
                //         abstract ref remove => throw null; 
                Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "abstract").WithLocation(7, 9));
        }

        [Fact]
        public void NullableClassConstraint_01()
        {
            var tree = UsingNode(@"
class C<T> where T : class {}
");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
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

        [Fact]
        public void NullableClassConstraint_02()
        {
            var tree = UsingNode(@"
class C<T> where T : struct {}
");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
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
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.StructConstraint);
                        {
                            N(SyntaxKind.StructKeyword);
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
        public void NullableClassConstraint_03()
        {
            var tree = UsingNode(@"
class C<T> where T : class? {}
");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
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
                            N(SyntaxKind.QuestionToken);
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
        public void NullableClassConstraint_04()
        {
            var tree = UsingNode(@"
class C<T> where T : struct? {}
", TestOptions.Regular,
                // (2,28): error CS1073: Unexpected token '?'
                // class C<T> where T : struct? {}
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "?").WithArguments("?").WithLocation(2, 28)
);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
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
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.StructConstraint);
                        {
                            N(SyntaxKind.StructKeyword);
                            N(SyntaxKind.QuestionToken);
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
        public void NullableClassConstraint_05()
        {
            var tree = UsingNode(@"
class C<T> where T : class? {}
", TestOptions.Regular7_3);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
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
                            N(SyntaxKind.QuestionToken);
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
        public void NullableClassConstraint_06()
        {
            var tree = UsingNode(@"
class C<T> where T : struct? {}
", TestOptions.Regular7_3,
                // (2,28): error CS1073: Unexpected token '?'
                // class C<T> where T : struct? {}
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "?").WithArguments("?").WithLocation(2, 28)
);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
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
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.StructConstraint);
                        {
                            N(SyntaxKind.StructKeyword);
                            N(SyntaxKind.QuestionToken);
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
}
