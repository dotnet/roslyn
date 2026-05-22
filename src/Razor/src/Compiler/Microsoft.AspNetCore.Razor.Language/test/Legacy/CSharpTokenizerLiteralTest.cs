// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class CSharpTokenizerLiteralTest : CSharpTokenizerTestBase
{
    private new SyntaxToken IgnoreRemaining => (SyntaxToken)base.IgnoreRemaining;

    [Fact]
    public void Simple_Integer_Literal_Is_Recognized()
    {
        TestSingleToken("01189998819991197253", SyntaxKind.NumericLiteral);
    }

    [Fact]
    public void Integer_Type_Suffix_Is_Recognized()
    {
        TestSingleToken("42U", SyntaxKind.NumericLiteral);
        TestSingleToken("42u", SyntaxKind.NumericLiteral);

        TestSingleToken("42L", SyntaxKind.NumericLiteral);
        TestSingleToken("42l", SyntaxKind.NumericLiteral);

        TestSingleToken("42UL", SyntaxKind.NumericLiteral);
        TestSingleToken("42Ul", SyntaxKind.NumericLiteral);

        TestSingleToken("42uL", SyntaxKind.NumericLiteral);
        TestSingleToken("42ul", SyntaxKind.NumericLiteral);

        TestSingleToken("42LU", SyntaxKind.NumericLiteral);
        TestSingleToken("42Lu", SyntaxKind.NumericLiteral);

        TestSingleToken("42lU", SyntaxKind.NumericLiteral);
        TestSingleToken("42lu", SyntaxKind.NumericLiteral);
    }

    [Fact]
    public void Trailing_Letter_Is_Not_Part_Of_Integer_Literal_If_Not_Type_Sufix()
    {
        TestTokenizer("42a", SyntaxFactory.Token(SyntaxKind.NumericLiteral, "42"), IgnoreRemaining);
    }

    [Fact]
    public void Simple_Hex_Literal_Is_Recognized()
    {
        TestSingleToken("0x0123456789ABCDEF", SyntaxKind.NumericLiteral);
    }

    [Fact]
    public void Integer_Type_Suffix_Is_Recognized_In_Hex_Literal()
    {
        TestSingleToken("0xDEADBEEFU", SyntaxKind.NumericLiteral);
        TestSingleToken("0xDEADBEEFu", SyntaxKind.NumericLiteral);

        TestSingleToken("0xDEADBEEFL", SyntaxKind.NumericLiteral);
        TestSingleToken("0xDEADBEEFl", SyntaxKind.NumericLiteral);

        TestSingleToken("0xDEADBEEFUL", SyntaxKind.NumericLiteral);
        TestSingleToken("0xDEADBEEFUl", SyntaxKind.NumericLiteral);

        TestSingleToken("0xDEADBEEFuL", SyntaxKind.NumericLiteral);
        TestSingleToken("0xDEADBEEFul", SyntaxKind.NumericLiteral);

        TestSingleToken("0xDEADBEEFLU", SyntaxKind.NumericLiteral);
        TestSingleToken("0xDEADBEEFLu", SyntaxKind.NumericLiteral);

        TestSingleToken("0xDEADBEEFlU", SyntaxKind.NumericLiteral);
        TestSingleToken("0xDEADBEEFlu", SyntaxKind.NumericLiteral);
    }

    [Fact]
    public void Trailing_Letter_Is_Not_Part_Of_Hex_Literal_If_Not_Type_Suffix()
    {
        TestTokenizer("0xDEADBEEFz", SyntaxFactory.Token(SyntaxKind.NumericLiteral, "0xDEADBEEF"), IgnoreRemaining);
    }

    [Fact]
    public void Binary_Literal_Is_Recognized()
    {
        TestSingleToken("0b01010101", SyntaxKind.NumericLiteral);
    }

    [Fact]
    public void Integer_Type_Suffix_Is_Recognized_In_Binary_Literal()
    {
        TestSingleToken("0b01010101U", SyntaxKind.NumericLiteral);
        TestSingleToken("0b01010101u", SyntaxKind.NumericLiteral);

        TestSingleToken("0b01010101L", SyntaxKind.NumericLiteral);
        TestSingleToken("0b01010101l", SyntaxKind.NumericLiteral);

        TestSingleToken("0b01010101UL", SyntaxKind.NumericLiteral);
        TestSingleToken("0b01010101Ul", SyntaxKind.NumericLiteral);

        TestSingleToken("0b01010101uL", SyntaxKind.NumericLiteral);
        TestSingleToken("0b01010101ul", SyntaxKind.NumericLiteral);

        TestSingleToken("0b01010101LU", SyntaxKind.NumericLiteral);
        TestSingleToken("0b01010101Lu", SyntaxKind.NumericLiteral);

        TestSingleToken("0b01010101lU", SyntaxKind.NumericLiteral);
        TestSingleToken("0b01010101lu", SyntaxKind.NumericLiteral);
    }

    [Fact]
    public void Trailing_Letter_Is_Not_Part_Of_Binary_Literal_If_Not_Type_Suffix()
    {
        TestTokenizer("0b01010101z", SyntaxFactory.Token(SyntaxKind.NumericLiteral, "0b01010101"), IgnoreRemaining);
    }

    [Fact]
    public void Dot_Followed_By_Non_Digit_Is_Not_Part_Of_Real_Literal()
    {
        TestTokenizer("3.a", SyntaxFactory.Token(SyntaxKind.NumericLiteral, "3"), IgnoreRemaining);
    }

    [Fact]
    public void Simple_Real_Literal_Is_Recognized()
    {
        TestTokenizer("3.14159", SyntaxFactory.Token(SyntaxKind.NumericLiteral, "3.14159"));
    }

    [Fact]
    public void Real_Literal_Between_Zero_And_One_Is_Recognized()
    {
        TestTokenizer(".14159", SyntaxFactory.Token(SyntaxKind.NumericLiteral, ".14159"));
    }

    [Fact]
    public void Integer_With_Real_Type_Suffix_Is_Recognized()
    {
        TestSingleToken("42F", SyntaxKind.NumericLiteral);
        TestSingleToken("42f", SyntaxKind.NumericLiteral);
        TestSingleToken("42D", SyntaxKind.NumericLiteral);
        TestSingleToken("42d", SyntaxKind.NumericLiteral);
        TestSingleToken("42M", SyntaxKind.NumericLiteral);
        TestSingleToken("42m", SyntaxKind.NumericLiteral);
    }

    [Fact]
    public void Integer_With_Exponent_Is_Recognized()
    {
        TestSingleToken("1e10", SyntaxKind.NumericLiteral);
        TestSingleToken("1E10", SyntaxKind.NumericLiteral);
        TestSingleToken("1e+10", SyntaxKind.NumericLiteral);
        TestSingleToken("1E+10", SyntaxKind.NumericLiteral);
        TestSingleToken("1e-10", SyntaxKind.NumericLiteral);
        TestSingleToken("1E-10", SyntaxKind.NumericLiteral);
    }

    [Fact]
    public void Real_Number_With_Type_Suffix_Is_Recognized()
    {
        TestSingleToken("3.14F", SyntaxKind.NumericLiteral);
        TestSingleToken("3.14f", SyntaxKind.NumericLiteral);
        TestSingleToken("3.14D", SyntaxKind.NumericLiteral);
        TestSingleToken("3.14d", SyntaxKind.NumericLiteral);
        TestSingleToken("3.14M", SyntaxKind.NumericLiteral);
        TestSingleToken("3.14m", SyntaxKind.NumericLiteral);
    }

    [Fact]
    public void Real_Number_With_Exponent_Is_Recognized()
    {
        TestSingleToken("3.14E10", SyntaxKind.NumericLiteral);
        TestSingleToken("3.14e10", SyntaxKind.NumericLiteral);
        TestSingleToken("3.14E+10", SyntaxKind.NumericLiteral);
        TestSingleToken("3.14e+10", SyntaxKind.NumericLiteral);
        TestSingleToken("3.14E-10", SyntaxKind.NumericLiteral);
        TestSingleToken("3.14e-10", SyntaxKind.NumericLiteral);
    }

    [Fact]
    public void Real_Number_With_Exponent_And_Type_Suffix_Is_Recognized()
    {
        TestSingleToken("3.14E+10F", SyntaxKind.NumericLiteral);
    }

    [Fact]
    public void Single_Character_Literal_Is_Recognized()
    {
        TestSingleToken("'f'", SyntaxKind.CharacterLiteral);
    }

    [Fact]
    public void Multi_Character_Literal_Is_Recognized()
    {
        TestSingleToken("'goo'", SyntaxKind.CharacterLiteral);
    }

    [Fact]
    public void Character_Literal_Is_Terminated_By_EOF_If_Unterminated()
    {
        TestSingleToken("'goo bar", SyntaxKind.CharacterLiteral);
    }

    [Fact]
    public void Character_Literal_Not_Terminated_By_Escaped_Quote()
    {
        TestSingleToken("'goo\\'bar'", SyntaxKind.CharacterLiteral);
    }

    [Fact]
    public void Character_Literal_Is_Terminated_By_EOL_If_Unterminated()
    {
        TestTokenizer("'goo\n", SyntaxFactory.Token(SyntaxKind.CharacterLiteral, "'goo"), IgnoreRemaining);
    }

    [Fact]
    public void Character_Literal_Eats_EOL_When_Escaped()
    {
        TestTokenizer("'goo\\\n", SyntaxFactory.Token(SyntaxKind.CharacterLiteral, "'goo\\\n"), IgnoreRemaining);
    }

    [Fact]
    public void Character_Literal_Eats_EOL_When_Escaped_And_Followed_By_Stuff()
    {
        TestTokenizer("'goo\\\nflarg", SyntaxFactory.Token(SyntaxKind.CharacterLiteral, "'goo\\\nflarg"), IgnoreRemaining);
    }

    [Fact]
    public void Character_Literal_Eats_CR_When_Escaped()
    {
        TestTokenizer("'goo\\\r\n", SyntaxFactory.Token(SyntaxKind.CharacterLiteral, "'goo\\\r"), IgnoreRemaining);
    }

    [Fact]
    public void Character_Literal_Eats_CR_When_Escaped_And_Followed_By_Stuff()
    {
        TestTokenizer($"'goo\\\r\nflarg", SyntaxFactory.Token(SyntaxKind.CharacterLiteral, "'goo\\\r"), IgnoreRemaining);
    }

    [Fact]
    public void Character_Literal_Allows_Escaped_Escape()
    {
        TestTokenizer("'goo\\\\'blah", SyntaxFactory.Token(SyntaxKind.CharacterLiteral, "'goo\\\\'"), IgnoreRemaining);
    }

    [Fact]
    public void Character_Literal_Allows_Trailing_Comments()
    {
        TestTokenizer("'f' // This is a comment",
            SyntaxFactory.Token(SyntaxKind.CharacterLiteral, "'f'"),
            SyntaxFactory.Token(SyntaxKind.Whitespace, " "),
            SyntaxFactory.Token(SyntaxKind.CSharpComment, "// This is a comment"));
    }

    [Fact]
    public void Multi_Character_Literal_Allows_Trailing_Comments()
    {
        TestTokenizer("'goo' // This is a comment",
            SyntaxFactory.Token(SyntaxKind.CharacterLiteral, "'goo'"),
            SyntaxFactory.Token(SyntaxKind.Whitespace, " "),
            SyntaxFactory.Token(SyntaxKind.CSharpComment, "// This is a comment"));
    }

    [Fact]
    public void String_Literal_Is_Recognized()
    {
        TestSingleToken("\"goo\"", SyntaxKind.StringLiteral);
    }

    [Fact]
    public void String_Literal_Empty_string()
    {
        TestSingleToken("\"\"", SyntaxKind.StringLiteral);
    }

    [Fact]
    public void String_Literal_Is_Terminated_By_EOF_If_Unterminated()
    {
        TestSingleToken("\"goo bar", SyntaxKind.StringLiteral);
    }

    [Fact]
    public void String_Literal_Not_Terminated_By_Escaped_Quote()
    {
        TestSingleToken("\"goo\\\"bar\"", SyntaxKind.StringLiteral);
    }

    [Fact]
    public void String_Literal_Is_Terminated_By_EOL_If_Unterminated()
    {
        TestTokenizer("\"goo\n", SyntaxFactory.Token(SyntaxKind.StringLiteral, "\"goo"), IgnoreRemaining);
    }

    [Fact]
    public void String_Literal_Terminated_By_EOL_Even_When_Last_Char_Is_Slash()
    {
        TestTokenizer("\"goo\\\n", SyntaxFactory.Token(SyntaxKind.StringLiteral, "\"goo\\\n"), IgnoreRemaining);
    }

    [Fact]
    public void String_Literal_Terminated_By_EOL_Even_When_Last_Char_Is_Slash_And_Followed_By_Stuff()
    {
        TestTokenizer("\"goo\\\nflarg", SyntaxFactory.Token(SyntaxKind.StringLiteral, "\"goo\\\nflarg"), IgnoreRemaining);
    }

    [Fact]
    public void String_Literal_Eats_Escaped_CR()
    {
        TestTokenizer("\"goo\\\r\n", SyntaxFactory.Token(SyntaxKind.StringLiteral, "\"goo\\\r"), IgnoreRemaining);
    }

    [Fact]
    public void String_Literal_Eats_Escaped_CR_And_Followed_By_Stuff()
    {
        TestTokenizer($"\"goo\\\r\nflarg", SyntaxFactory.Token(SyntaxKind.StringLiteral, "\"goo\\\r"), IgnoreRemaining);
    }

    [Fact]
    public void String_Literal_Allows_Escaped_Escape()
    {
        TestTokenizer("\"goo\\\\\"blah", SyntaxFactory.Token(SyntaxKind.StringLiteral, "\"goo\\\\\""), IgnoreRemaining);
    }

    [Fact]
    public void Verbatim_String_Literal_Can_Contain_Newlines()
    {
        TestSingleToken("@\"goo\nbar\nbaz\"", SyntaxKind.StringLiteral);
    }

    [Fact]
    public void Verbatim_String_Literal_Not_Terminated_By_Escaped_Double_Quote()
    {
        TestSingleToken("@\"goo\"\"bar\"", SyntaxKind.StringLiteral);
    }

    [Fact]
    public void Verbatim_String_Literal_Is_Terminated_By_Slash_Double_Quote()
    {
        TestTokenizer("@\"goo\\\"bar\"", SyntaxFactory.Token(SyntaxKind.StringLiteral, "@\"goo\\\""), IgnoreRemaining);
    }

    [Fact]
    public void Verbatim_String_Literal_Is_Terminated_By_EOF()
    {
        TestSingleToken("@\"goo", SyntaxKind.StringLiteral);
    }

    [Fact]
    public void String_Literal_Allows_Trailing_Comments()
    {
        TestTokenizer("\"goo\" // This is a comment",
            SyntaxFactory.Token(SyntaxKind.StringLiteral, "\"goo\""),
            SyntaxFactory.Token(SyntaxKind.Whitespace, " "),
            SyntaxFactory.Token(SyntaxKind.CSharpComment, "// This is a comment"));
    }

    [Fact]
    public void Verbatim_String_Literal_Allows_Trailing_Comments()
    {
        TestTokenizer("@\"goo\" // This is a comment",
            SyntaxFactory.Token(SyntaxKind.StringLiteral, "@\"goo\""),
            SyntaxFactory.Token(SyntaxKind.Whitespace, " "),
            SyntaxFactory.Token(SyntaxKind.CSharpComment, "// This is a comment"));
    }

    [Fact]
    public void Utf8_String_Literal_Is_Recognized_Lowercase()
    {
        TestSingleToken("\"hello\"u8", SyntaxKind.StringLiteral);
    }

    [Fact]
    public void Utf8_String_Literal_Is_Recognized_Uppercase()
    {
        TestSingleToken("\"hello\"U8", SyntaxKind.StringLiteral);
    }

    [Fact]
    public void Utf8_String_Literal_Empty_String()
    {
        TestSingleToken("\"\"u8", SyntaxKind.StringLiteral);
    }

    [Fact]
    public void Utf8_String_Literal_With_Escape_Sequences()
    {
        TestSingleToken("\"hello\\nworld\"u8", SyntaxKind.StringLiteral);
    }

    [Fact]
    public void Utf8_String_Literal_Allows_Trailing_Content()
    {
        TestTokenizer("\"hello\"u8;",
            SyntaxFactory.Token(SyntaxKind.StringLiteral, "\"hello\"u8"),
            SyntaxFactory.Token(SyntaxKind.Semicolon, ";"));
    }

    [Fact]
    public void Utf8_Verbatim_String_Literal_Is_Recognized()
    {
        TestSingleToken("@\"hello\\nworld\"u8", SyntaxKind.StringLiteral);
    }

    [Fact]
    public void Utf8_Raw_String_Literal_Is_Recognized()
    {
        TestSingleToken("\"\"\"hello\"\"\"u8", SyntaxKind.StringLiteral);
    }

    [Fact]
    public void Utf8_Raw_String_Literal_Multiline_Is_Recognized()
    {
        TestSingleToken("\"\"\"\nhello\nworld\n\"\"\"u8", SyntaxKind.StringLiteral);
    }

    [Fact]
    public void Utf8_Raw_String_Literal_With_Quotes_Is_Recognized()
    {
        TestSingleToken("\"\"\"She said \"hello\"\"\"\"u8", SyntaxKind.StringLiteral);
    }

    [Fact]
    public void Utf8_Raw_String_Literal_Uppercase_Is_Recognized()
    {
        TestSingleToken("\"\"\"content\"\"\"U8", SyntaxKind.StringLiteral);
    }

    [Fact]
    public void Interpolated_String_Is_Recognized()
    {
        TestSingleToken("""
            $"Hello, {name}!"
            """,
            SyntaxKind.StringLiteral);
    }

    [Fact]
    public void Interpolated_String_Empty_String()
    {
        TestSingleToken("""
            $""
            """,
            SyntaxKind.StringLiteral);
    }

    [Fact]
    public void Interpolated_String_Allows_Nested_Strings()
    {
        TestSingleToken("""
            $"Hello, {"world!"}!"
            """,
            SyntaxKind.StringLiteral);
    }

    [Fact]
    public void Interpolated_String_Allows_Escaped_Curly_Braces()
    {
        TestSingleToken("""
            $"Hello, {{name}}!"
            """,
            SyntaxKind.StringLiteral);
    }

    [Fact]
    public void Interpolated_String_Allows_Newlines_In_Interpolation_Hole()
    {
        TestSingleToken("""
            $"Hello, {name
                + 1}!"
            """,
            SyntaxKind.StringLiteral);
    }

    [Fact]
    public void Interpolated_String_Newlines_Terminate_Content()
    {
        TestTokenizer("""
            $"Hello, {name + 1}
            !"
            """,
            SyntaxFactory.Token(SyntaxKind.StringLiteral, """
            $"Hello, {name + 1}
            """), IgnoreRemaining);
    }

    [Fact]
    public void Interpolated_String_EndOfFile_In_Interpolation_Hole_Ends_String()
    {
        TestSingleToken("""
            $"Hello, {name + 1
            """,
            SyntaxKind.StringLiteral);
    }

    [Fact]
    public void Interpolated_String_Allows_Comment_In_Interpolation_Hole()
    {
        TestSingleToken("""
            $"Hello, {name + 1 // Test!
              }!"
            """,
            SyntaxKind.StringLiteral);
    }

    [Theory]
    [InlineData("$@")]
    [InlineData("@$")]
    public void Verbatim_Interpolated_String_Is_Recognized(string prefix)
    {
        TestSingleToken($$"""
            {{prefix}}"Hello, {name}!"
            """,
            SyntaxKind.StringLiteral);
    }

    [Theory]
    [InlineData("$@")]
    [InlineData("@$")]
    public void Verbatim_Interpolated_String_Allows_Nested_Strings(string prefix)
    {
        TestSingleToken($$"""
            {{prefix}}"Hello, {"world!"}!"
            """,
            SyntaxKind.StringLiteral);
    }

    [Theory]
    [InlineData("$@")]
    [InlineData("@$")]
    public void Verbatim_Interpolated_String_Allows_Escaped_Curly_Braces(string prefix)
    {
        TestSingleToken($$$"""
            {{{prefix}}}"Hello, {{name}}!"
            """,
            SyntaxKind.StringLiteral);
    }

    [Theory]
    [InlineData("$@")]
    [InlineData("@$")]
    public void Verbatim_Interpolated_String_Allows_Newlines_In_Interpolation_Hole(string prefix)
    {
        TestSingleToken($$"""
            {{prefix}}"Hello, {name
                + 1}!"
            """,
            SyntaxKind.StringLiteral);
    }

    [Theory]
    [InlineData("$@")]
    [InlineData("@$")]
    public void Verbatim_Interpolated_String_Allows_Newlines_In_Content(string prefix)
    {
        TestSingleToken($$"""
            {{prefix}}"Hello, {name + 1}
            !"
            """,
            SyntaxKind.StringLiteral);
    }

    [Theory]
    [InlineData("$@")]
    [InlineData("@$")]
    public void Verbatim_Interpolated_String_EndOfFile_In_Interpolation_Hole_Ends_String(string prefix)
    {
        TestSingleToken($$"""
            {{prefix}}"Hello, {name + 1
            """,
            SyntaxKind.StringLiteral);
    }

    [Theory]
    [InlineData("$@")]
    [InlineData("@$")]
    public void Verbatim_Interpolated_String_Allows_Comment_In_Interpolation_Hole(string prefix)
    {
        TestSingleToken($$"""
            {{prefix}}"Hello, {name + 1 // Test!
              }!"
            """,
            SyntaxKind.StringLiteral);
    }

    [Theory]
    [InlineData(""""
        """
        """")]
    [InlineData("""""
        """"
        """"")]
    [InlineData(""""""
        """""
        """""")]
    public void Single_Line_Raw_String_Literal_Is_Recognized(string quotes)
    {
        TestSingleToken($"""
            {quotes}goo{quotes}
            """, SyntaxKind.StringLiteral);
    }

    [Theory, CombinatorialData]
    public void Single_Line_Raw_Interpolated_String_Literal_Is_Recognized(
        [CombinatorialValues("$", "$$", "$$$")]
        string dollars,
        [CombinatorialValues(""""
        """
        """",
        """""
        """"
        """"",
        """"""
        """""
        """"""
        )]
        string quotes)
    {
        TestSingleToken($"""
            {dollars}{quotes}goo{quotes}
            """, SyntaxKind.StringLiteral);
    }

    [Theory]
    [InlineData(""""
        """
        """")]
    [InlineData("""""
        """"
        """"")]
    [InlineData(""""""
        """""
        """""")]
    public void Multi_Line_Raw_String_Literal_Is_Recognized(string quotes)
    {
        TestSingleToken($"""
            {quotes}
            goo
            {quotes}
            """, SyntaxKind.StringLiteral);
    }

    [Theory, CombinatorialData]
    public void Multi_Line_Raw_Interpolated_String_Literal_Is_Recognized(
        [CombinatorialValues("$", "$$", "$$$")]
        string dollars,
        [CombinatorialValues(""""
        """
        """",
        """""
        """"
        """"",
        """"""
        """""
        """"""
        )]
        string quotes)
    {
        TestSingleToken($"""
            {dollars}{quotes}
            goo
            {quotes}
            """, SyntaxKind.StringLiteral);
    }

    [Theory]
    [InlineData(""""
        """
        """")]
    [InlineData("""""
        """"
        """"")]
    [InlineData(""""""
        """""
        """""")]
    public void Single_Line_Raw_String_Literal_Is_Terminated_By_EOF_If_Unterminated(string quotes)
    {
        TestSingleToken($"""
            {quotes}goo
            """, SyntaxKind.StringLiteral);
    }

    [Theory, CombinatorialData]
    public void Single_Line_Raw_Interpolated_String_Literal_Is_Terminated_By_EOF_If_Unterminated(
        [CombinatorialValues("$", "$$", "$$$")]
        string dollars,
        [CombinatorialValues(""""
        """
        """",
        """""
        """"
        """"",
        """"""
        """""
        """"""
        )]
        string quotes)
    {
        TestSingleToken($"""
            {dollars}{quotes}goo
            """, SyntaxKind.StringLiteral);
    }

    [Theory]
    [InlineData(""""
        """
        """")]
    [InlineData("""""
        """"
        """"")]
    [InlineData(""""""
        """""
        """""")]
    public void Multi_Line_Raw_String_Literal_Is_Terminated_By_EOF_If_Unterminated(string quotes)
    {
        TestSingleToken($"""
            {quotes}
            goo
            """, SyntaxKind.StringLiteral);
    }

    [Theory, CombinatorialData]
    public void Multi_Line_Raw_Interpolated_String_Literal_Is_Terminated_By_EOF_If_Unterminated(
        [CombinatorialValues("$", "$$", "$$$")]
        string dollars,
        [CombinatorialValues(""""
        """
        """",
        """""
        """"
        """"",
        """"""
        """""
        """"""
        )]
        string quotes)
    {
        TestSingleToken($"""
            {dollars}{quotes}
            goo
            """, SyntaxKind.StringLiteral);
    }

    [Fact]
    public void Raw_Interpolated_String_Just_Dollars_Is_Recognized()
    {
        TestSingleToken("$$", SyntaxKind.StringLiteral);
    }
}
