// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Xunit;

using CSharpSyntaxFacts = Microsoft.CodeAnalysis.CSharp.SyntaxFacts;
using CSharpSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

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
        TestKeyword("abstract", CSharpSyntaxKind.AbstractKeyword);
        TestKeyword("byte", CSharpSyntaxKind.ByteKeyword);
        TestKeyword("class", CSharpSyntaxKind.ClassKeyword);
        TestKeyword("delegate", CSharpSyntaxKind.DelegateKeyword);
        TestKeyword("event", CSharpSyntaxKind.EventKeyword);
        TestKeyword("fixed", CSharpSyntaxKind.FixedKeyword);
        TestKeyword("if", CSharpSyntaxKind.IfKeyword);
        TestKeyword("internal", CSharpSyntaxKind.InternalKeyword);
        TestKeyword("new", CSharpSyntaxKind.NewKeyword);
        TestKeyword("override", CSharpSyntaxKind.OverrideKeyword);
        TestKeyword("readonly", CSharpSyntaxKind.ReadOnlyKeyword);
        TestKeyword("short", CSharpSyntaxKind.ShortKeyword);
        TestKeyword("struct", CSharpSyntaxKind.StructKeyword);
        TestKeyword("try", CSharpSyntaxKind.TryKeyword);
        TestKeyword("unsafe", CSharpSyntaxKind.UnsafeKeyword);
        TestKeyword("volatile", CSharpSyntaxKind.VolatileKeyword);
        TestKeyword("as", CSharpSyntaxKind.AsKeyword);
        TestKeyword("do", CSharpSyntaxKind.DoKeyword);
        TestKeyword("is", CSharpSyntaxKind.IsKeyword);
        TestKeyword("params", CSharpSyntaxKind.ParamsKeyword);
        TestKeyword("ref", CSharpSyntaxKind.RefKeyword);
        TestKeyword("switch", CSharpSyntaxKind.SwitchKeyword);
        TestKeyword("ushort", CSharpSyntaxKind.UShortKeyword);
        TestKeyword("while", CSharpSyntaxKind.WhileKeyword);
        TestKeyword("case", CSharpSyntaxKind.CaseKeyword);
        TestKeyword("const", CSharpSyntaxKind.ConstKeyword);
        TestKeyword("explicit", CSharpSyntaxKind.ExplicitKeyword);
        TestKeyword("float", CSharpSyntaxKind.FloatKeyword);
        TestKeyword("null", CSharpSyntaxKind.NullKeyword);
        TestKeyword("sizeof", CSharpSyntaxKind.SizeOfKeyword);
        TestKeyword("typeof", CSharpSyntaxKind.TypeOfKeyword);
        TestKeyword("implicit", CSharpSyntaxKind.ImplicitKeyword);
        TestKeyword("private", CSharpSyntaxKind.PrivateKeyword);
        TestKeyword("this", CSharpSyntaxKind.ThisKeyword);
        TestKeyword("using", CSharpSyntaxKind.UsingKeyword);
        TestKeyword("extern", CSharpSyntaxKind.ExternKeyword);
        TestKeyword("return", CSharpSyntaxKind.ReturnKeyword);
        TestKeyword("stackalloc", CSharpSyntaxKind.StackAllocKeyword);
        TestKeyword("uint", CSharpSyntaxKind.UIntKeyword);
        TestKeyword("base", CSharpSyntaxKind.BaseKeyword);
        TestKeyword("catch", CSharpSyntaxKind.CatchKeyword);
        TestKeyword("continue", CSharpSyntaxKind.ContinueKeyword);
        TestKeyword("double", CSharpSyntaxKind.DoubleKeyword);
        TestKeyword("for", CSharpSyntaxKind.ForKeyword);
        TestKeyword("in", CSharpSyntaxKind.InKeyword);
        TestKeyword("lock", CSharpSyntaxKind.LockKeyword);
        TestKeyword("object", CSharpSyntaxKind.ObjectKeyword);
        TestKeyword("protected", CSharpSyntaxKind.ProtectedKeyword);
        TestKeyword("static", CSharpSyntaxKind.StaticKeyword);
        TestKeyword("false", CSharpSyntaxKind.FalseKeyword);
        TestKeyword("public", CSharpSyntaxKind.PublicKeyword);
        TestKeyword("sbyte", CSharpSyntaxKind.SByteKeyword);
        TestKeyword("throw", CSharpSyntaxKind.ThrowKeyword);
        TestKeyword("virtual", CSharpSyntaxKind.VirtualKeyword);
        TestKeyword("decimal", CSharpSyntaxKind.DecimalKeyword);
        TestKeyword("else", CSharpSyntaxKind.ElseKeyword);
        TestKeyword("operator", CSharpSyntaxKind.OperatorKeyword);
        TestKeyword("string", CSharpSyntaxKind.StringKeyword);
        TestKeyword("ulong", CSharpSyntaxKind.ULongKeyword);
        TestKeyword("bool", CSharpSyntaxKind.BoolKeyword);
        TestKeyword("char", CSharpSyntaxKind.CharKeyword);
        TestKeyword("default", CSharpSyntaxKind.DefaultKeyword);
        TestKeyword("foreach", CSharpSyntaxKind.ForEachKeyword);
        TestKeyword("long", CSharpSyntaxKind.LongKeyword);
        TestKeyword("void", CSharpSyntaxKind.VoidKeyword);
        TestKeyword("enum", CSharpSyntaxKind.EnumKeyword);
        TestKeyword("finally", CSharpSyntaxKind.FinallyKeyword);
        TestKeyword("int", CSharpSyntaxKind.IntKeyword);
        TestKeyword("out", CSharpSyntaxKind.OutKeyword);
        TestKeyword("sealed", CSharpSyntaxKind.SealedKeyword);
        TestKeyword("true", CSharpSyntaxKind.TrueKeyword);
        TestKeyword("goto", CSharpSyntaxKind.GotoKeyword);
        TestKeyword("unchecked", CSharpSyntaxKind.UncheckedKeyword);
        TestKeyword("interface", CSharpSyntaxKind.InterfaceKeyword);
        TestKeyword("break", CSharpSyntaxKind.BreakKeyword);
        TestKeyword("checked", CSharpSyntaxKind.CheckedKeyword);
        TestKeyword("namespace", CSharpSyntaxKind.NamespaceKeyword);
        TestKeyword("when", CSharpSyntaxKind.WhenKeyword);
    }

    private void TestKeyword(string keyword, CSharpSyntaxKind keywordType)
    {
        TestTokenizer(keyword, SyntaxFactory.Token(SyntaxKind.Keyword, CSharpSyntaxFacts.GetText(keywordType)));
    }
}
