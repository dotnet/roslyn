// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class CSharpTokenizerIdentifierTest : CSharpTokenizerTestBase
{
    [Fact]
    public void Simple_Identifier_Is_Recognized()
    {
        TestTokenizer("foo", SyntaxFactory.Token(SyntaxKind.Identifier, "foo"));
    }

    [Fact]
    public void Identifier_Starting_With_Underscore_Is_Recognized()
    {
        TestTokenizer("_foo", SyntaxFactory.Token(SyntaxKind.Identifier, "_foo"));
    }

    [Fact]
    public void Identifier_Can_Contain_Digits()
    {
        TestTokenizer("foo4", SyntaxFactory.Token(SyntaxKind.Identifier, "foo4"));
    }

    [Fact]
    public void Identifier_Can_Start_With_Titlecase_Letter()
    {
        TestTokenizer("ῼfoo", SyntaxFactory.Token(SyntaxKind.Identifier, "ῼfoo"));
    }

    [Fact]
    public void Identifier_Can_Start_With_Letter_Modifier()
    {
        TestTokenizer("ᵊfoo", SyntaxFactory.Token(SyntaxKind.Identifier, "ᵊfoo"));
    }

    [Fact]
    public void Identifier_Can_Start_With_Other_Letter()
    {
        TestTokenizer("ƻfoo", SyntaxFactory.Token(SyntaxKind.Identifier, "ƻfoo"));
    }

    [Fact]
    public void Identifier_Can_Start_With_Number_Letter()
    {
        TestTokenizer("Ⅽool", SyntaxFactory.Token(SyntaxKind.Identifier, "Ⅽool"));
    }

    [Fact]
    public void Identifier_Can_Contain_Non_Spacing_Mark()
    {
        TestTokenizer("foo\u0300", SyntaxFactory.Token(SyntaxKind.Identifier, "foo\u0300"));
    }

    [Fact]
    public void Identifier_Can_Contain_Spacing_Combining_Mark()
    {
        TestTokenizer("fooः", SyntaxFactory.Token(SyntaxKind.Identifier, "fooः"));
    }

    [Fact]
    public void Identifier_Can_Contain_Non_English_Digit()
    {
        TestTokenizer("foo١", SyntaxFactory.Token(SyntaxKind.Identifier, "foo١"));
    }

    [Fact]
    public void Identifier_Can_Contain_Connector_Punctuation()
    {
        TestTokenizer("foo‿bar", SyntaxFactory.Token(SyntaxKind.Identifier, "foo‿bar"));
    }

    [Fact]
    public void Identifier_Can_Contain_Format_Character()
    {
        TestTokenizer("foo؃bar", SyntaxFactory.Token(SyntaxKind.Identifier, "foo؃bar"));
    }

    [Fact]
    public void Keywords_Are_Recognized_As_Keyword_Tokens()
    {
        TestKeyword("abstract");
        TestKeyword("byte");
        TestKeyword("class");
        TestKeyword("delegate");
        TestKeyword("event");
        TestKeyword("fixed");
        TestKeyword("if");
        TestKeyword("internal");
        TestKeyword("new");
        TestKeyword("override");
        TestKeyword("readonly");
        TestKeyword("short");
        TestKeyword("struct");
        TestKeyword("try");
        TestKeyword("unsafe");
        TestKeyword("volatile");
        TestKeyword("as");
        TestKeyword("do");
        TestKeyword("is");
        TestKeyword("params");
        TestKeyword("ref");
        TestKeyword("switch");
        TestKeyword("ushort");
        TestKeyword("while");
        TestKeyword("case");
        TestKeyword("const");
        TestKeyword("explicit");
        TestKeyword("float");
        TestKeyword("null");
        TestKeyword("sizeof");
        TestKeyword("typeof");
        TestKeyword("implicit");
        TestKeyword("private");
        TestKeyword("this");
        TestKeyword("using");
        TestKeyword("extern");
        TestKeyword("return");
        TestKeyword("stackalloc");
        TestKeyword("uint");
        TestKeyword("base");
        TestKeyword("catch");
        TestKeyword("continue");
        TestKeyword("double");
        TestKeyword("for");
        TestKeyword("in");
        TestKeyword("lock");
        TestKeyword("object");
        TestKeyword("protected");
        TestKeyword("static");
        TestKeyword("false");
        TestKeyword("public");
        TestKeyword("sbyte");
        TestKeyword("throw");
        TestKeyword("virtual");
        TestKeyword("decimal");
        TestKeyword("else");
        TestKeyword("operator");
        TestKeyword("string");
        TestKeyword("ulong");
        TestKeyword("bool");
        TestKeyword("char");
        TestKeyword("default");
        TestKeyword("foreach");
        TestKeyword("long");
        TestKeyword("void");
        TestKeyword("enum");
        TestKeyword("finally");
        TestKeyword("int");
        TestKeyword("out");
        TestKeyword("sealed");
        TestKeyword("true");
        TestKeyword("goto");
        TestKeyword("unchecked");
        TestKeyword("interface");
        TestKeyword("break");
        TestKeyword("checked");
        TestKeyword("namespace");
        TestKeyword("when");
    }

    private void TestKeyword(string keyword)
    {
        TestTokenizer(keyword, SyntaxFactory.Token(SyntaxKind.Keyword, keyword));
    }
}
