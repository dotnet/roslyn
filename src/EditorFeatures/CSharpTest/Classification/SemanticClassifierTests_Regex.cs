// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities.EmbeddedLanguages;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification;

public partial class SemanticClassifierTests
{
    [Theory, CombinatorialData]
    public Task TestRegex1(TestHost testHost)
        => TestAsync(
            """
            using System.Text.RegularExpressions;

            class Program
            {
                void Goo()
                {
                    var r = new Regex(@"$(\a\t\u0020)|[^\p{Lu}-a\w\sa-z-[m-p]]+?(?#comment)|(\b\G\z)|(?<name>sub){0,5}?^");
                }
            }
            """,
            testHost,
            Namespace("System"),
            Namespace("Text"),
            Namespace("RegularExpressions"),
            Keyword("var"),
            Class("Regex"),
            Regex.Anchor("$"),
            Regex.Grouping("("),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("t"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("u"),
            Regex.OtherEscape("0020"),
            Regex.Grouping(")"),
            Regex.Alternation("|"),
            Regex.CharacterClass("["),
            Regex.CharacterClass("^"),
            Regex.CharacterClass("\\"),
            Regex.CharacterClass("p"),
            Regex.CharacterClass("{"),
            Regex.CharacterClass("Lu"),
            Regex.CharacterClass("}"),
            Regex.Text("-a"),
            Regex.CharacterClass("\\"),
            Regex.CharacterClass("w"),
            Regex.CharacterClass("\\"),
            Regex.CharacterClass("s"),
            Regex.Text("a"),
            Regex.CharacterClass("-"),
            Regex.Text("z"),
            Regex.CharacterClass("-"),
            Regex.CharacterClass("["),
            Regex.Text("m"),
            Regex.CharacterClass("-"),
            Regex.Text("p"),
            Regex.CharacterClass("]"),
            Regex.CharacterClass("]"),
            Regex.Quantifier("+"),
            Regex.Quantifier("?"),
            Regex.Comment("(?#comment)"),
            Regex.Alternation("|"),
            Regex.Grouping("("),
            Regex.Anchor("\\"),
            Regex.Anchor("b"),
            Regex.Anchor("\\"),
            Regex.Anchor("G"),
            Regex.Anchor("\\"),
            Regex.Anchor("z"),
            Regex.Grouping(")"),
            Regex.Alternation("|"),
            Regex.Grouping("("),
            Regex.Grouping("?"),
            Regex.Grouping("<"),
            Regex.Grouping("name"),
            Regex.Grouping(">"),
            Regex.Text("sub"),
            Regex.Grouping(")"),
            Regex.Quantifier("{"),
            Regex.Quantifier("0"),
            Regex.Quantifier(","),
            Regex.Quantifier("5"),
            Regex.Quantifier("}"),
            Regex.Quantifier("?"),
            Regex.Anchor("^"));

    [Theory, CombinatorialData]
    public Task TestRegex2(TestHost testHost)
        => TestAsync(
            """
            using System.Text.RegularExpressions;

            class Program
            {
                void Goo()
                {
                    // language=regex
                    var r = @"$(\a\t\u0020)|[^\p{Lu}-a\w\sa-z-[m-p]]+?(?#comment)|(\b\G\z)|(?<name>sub){0,5}?^";
                }
            }
            """,
            testHost,
            Namespace("System"),
            Namespace("Text"),
            Namespace("RegularExpressions"),
            Keyword("var"),
            Regex.Anchor("$"),
            Regex.Grouping("("),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("t"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("u"),
            Regex.OtherEscape("0020"),
            Regex.Grouping(")"),
            Regex.Alternation("|"),
            Regex.CharacterClass("["),
            Regex.CharacterClass("^"),
            Regex.CharacterClass("\\"),
            Regex.CharacterClass("p"),
            Regex.CharacterClass("{"),
            Regex.CharacterClass("Lu"),
            Regex.CharacterClass("}"),
            Regex.Text("-a"),
            Regex.CharacterClass("\\"),
            Regex.CharacterClass("w"),
            Regex.CharacterClass("\\"),
            Regex.CharacterClass("s"),
            Regex.Text("a"),
            Regex.CharacterClass("-"),
            Regex.Text("z"),
            Regex.CharacterClass("-"),
            Regex.CharacterClass("["),
            Regex.Text("m"),
            Regex.CharacterClass("-"),
            Regex.Text("p"),
            Regex.CharacterClass("]"),
            Regex.CharacterClass("]"),
            Regex.Quantifier("+"),
            Regex.Quantifier("?"),
            Regex.Comment("(?#comment)"),
            Regex.Alternation("|"),
            Regex.Grouping("("),
            Regex.Anchor("\\"),
            Regex.Anchor("b"),
            Regex.Anchor("\\"),
            Regex.Anchor("G"),
            Regex.Anchor("\\"),
            Regex.Anchor("z"),
            Regex.Grouping(")"),
            Regex.Alternation("|"),
            Regex.Grouping("("),
            Regex.Grouping("?"),
            Regex.Grouping("<"),
            Regex.Grouping("name"),
            Regex.Grouping(">"),
            Regex.Text("sub"),
            Regex.Grouping(")"),
            Regex.Quantifier("{"),
            Regex.Quantifier("0"),
            Regex.Quantifier(","),
            Regex.Quantifier("5"),
            Regex.Quantifier("}"),
            Regex.Quantifier("?"),
            Regex.Anchor("^"));

    [Theory, CombinatorialData]
    public Task TestRegex3(TestHost testHost)
        => TestAsync(
            """
            using System.Text.RegularExpressions;

            class Program
            {
                void Goo()
                {
                    var r = /* language=regex */@"$(\a\t\u0020\\)|[^\p{Lu}-a\w\sa-z-[m-p]]+?(?#comment)|(\b\G\z)|(?<name>sub){0,5}?^";
                }
            }
            """,
            testHost, Namespace("System"),
            Namespace("Text"),
            Namespace("RegularExpressions"),
            Keyword("var"),
            Regex.Anchor("$"),
            Regex.Grouping("("),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("t"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("u"),
            Regex.OtherEscape("0020"),
            Regex.SelfEscapedCharacter("\\"),
            Regex.SelfEscapedCharacter("\\"),
            Regex.Grouping(")"),
            Regex.Alternation("|"),
            Regex.CharacterClass("["),
            Regex.CharacterClass("^"),
            Regex.CharacterClass("\\"),
            Regex.CharacterClass("p"),
            Regex.CharacterClass("{"),
            Regex.CharacterClass("Lu"),
            Regex.CharacterClass("}"),
            Regex.Text("-a"),
            Regex.CharacterClass("\\"),
            Regex.CharacterClass("w"),
            Regex.CharacterClass("\\"),
            Regex.CharacterClass("s"),
            Regex.Text("a"),
            Regex.CharacterClass("-"),
            Regex.Text("z"),
            Regex.CharacterClass("-"),
            Regex.CharacterClass("["),
            Regex.Text("m"),
            Regex.CharacterClass("-"),
            Regex.Text("p"),
            Regex.CharacterClass("]"),
            Regex.CharacterClass("]"),
            Regex.Quantifier("+"),
            Regex.Quantifier("?"),
            Regex.Comment("(?#comment)"),
            Regex.Alternation("|"),
            Regex.Grouping("("),
            Regex.Anchor("\\"),
            Regex.Anchor("b"),
            Regex.Anchor("\\"),
            Regex.Anchor("G"),
            Regex.Anchor("\\"),
            Regex.Anchor("z"),
            Regex.Grouping(")"),
            Regex.Alternation("|"),
            Regex.Grouping("("),
            Regex.Grouping("?"),
            Regex.Grouping("<"),
            Regex.Grouping("name"),
            Regex.Grouping(">"),
            Regex.Text("sub"),
            Regex.Grouping(")"),
            Regex.Quantifier("{"),
            Regex.Quantifier("0"),
            Regex.Quantifier(","),
            Regex.Quantifier("5"),
            Regex.Quantifier("}"),
            Regex.Quantifier("?"),
            Regex.Anchor("^"));

    [Theory, CombinatorialData]
    public Task TestRegex4(TestHost testHost)
        => TestAsync(
            """
            using System.Text.RegularExpressions;

            class Program
            {
                void Goo()
                {
                    var r = /* lang=regex */@"$\a(?#comment)";
                }
            }
            """,
            testHost, Namespace("System"),
            Namespace("Text"),
            Namespace("RegularExpressions"),
            Keyword("var"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    public Task TestRegex4_utf8_1(TestHost testHost)
        => TestAsync(
            """
            using System.Text.RegularExpressions;

            class Program
            {
                void Goo()
                {
                    var r = /* lang=regex */"$\\a(?#comment)";
                }
            }
            """,
            testHost, Namespace("System"),
            Namespace("Text"),
            Namespace("RegularExpressions"),
            Keyword("var"),
            Regex.Anchor("$"),
            Regex.OtherEscape(@"\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    public Task TestRegex4_utf8_2(TestHost testHost)
        => TestAsync(
            """
            using System.Text.RegularExpressions;

            class Program
            {
                void Goo()
                {
                    var r = /* lang=regex */@"$\a(?#comment)"u8;
                }
            }
            """,
            testHost, Namespace("System"),
            Namespace("Text"),
            Namespace("RegularExpressions"),
            Keyword("var"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    public Task TestRegex5(TestHost testHost)
        => TestAsync(
            """
            using System.Text.RegularExpressions;

            class Program
            {
                void Goo()
                {
                    var r = /* lang=regexp */@"$\a(?#comment)";
                }
            }
            """,
            testHost, Namespace("System"),
            Namespace("Text"),
            Namespace("RegularExpressions"),
            Keyword("var"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    public Task TestRegex6(TestHost testHost)
        => TestAsync(
            """
            using System.Text.RegularExpressions;

            class Program
            {
                void Goo()
                {
                    var r = /* lang=regexp */@"$\a(?#comment) # not end of line comment";
                }
            }
            """,
            testHost, Namespace("System"),
            Namespace("Text"),
            Namespace("RegularExpressions"),
            Keyword("var"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"),
            Regex.Text(" # not end of line comment"));

    [Theory, CombinatorialData]
    public Task TestRegex7(TestHost testHost)
        => TestAsync(
            """
            using System.Text.RegularExpressions;

            class Program
            {
                void Goo()
                {
                    var r = /* lang=regexp,ignorepatternwhitespace */@"$\a(?#comment) # is end of line comment";
                }
            }
            """,
            testHost, Namespace("System"),
            Namespace("Text"),
            Namespace("RegularExpressions"),
            Keyword("var"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"),
            Regex.Comment("# is end of line comment"));

    [Theory, CombinatorialData]
    public Task TestRegex8(TestHost testHost)
        => TestAsync(
            """
            using System.Text.RegularExpressions;

            class Program
            {
                void Goo()
                {
                    var r = /* lang = regexp , ignorepatternwhitespace */@"$\a(?#comment) # is end of line comment";
                }
            }
            """,
            testHost, Namespace("System"),
            Namespace("Text"),
            Namespace("RegularExpressions"),
            Keyword("var"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"),
            Regex.Comment("# is end of line comment"));

    [Theory, CombinatorialData]
    public Task TestRegex9(TestHost testHost)
        => TestAsync(
            """
            using System.Text.RegularExpressions;

            class Program
            {
                void Goo()
                {
                    var r = new Regex(@"$\a(?#comment) # is end of line comment", RegexOptions.IgnorePatternWhitespace);
                }
            }
            """,
            testHost, Namespace("System"),
            Namespace("Text"),
            Namespace("RegularExpressions"),
            Keyword("var"),
            Class("Regex"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"),
            Regex.Comment("# is end of line comment"),
            Enum("RegexOptions"),
            EnumMember("IgnorePatternWhitespace"));

    [Theory, CombinatorialData]
    public Task TestRegex10(TestHost testHost)
        => TestAsync(
            """
            using System.Text.RegularExpressions;

            class Program
            {
                void Goo()
                {
                    var r = new Regex(@"$\a(?#comment) # is not end of line comment");
                }
            }
            """,
            testHost, Namespace("System"),
            Namespace("Text"),
            Namespace("RegularExpressions"),
            Keyword("var"),
            Class("Regex"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"),
            Regex.Text(" # is not end of line comment"));

    [Theory, CombinatorialData]
    public Task TestRegex10_utf8(TestHost testHost)
        => TestAsync(
            """
            using System.Text.RegularExpressions;

            class Program
            {
                void Goo()
                {
                    // lang=regex
                    var r = @"$\a(?#comment) # is not end of line comment"u8;
                }
            }
            """,
            testHost, Namespace("System"),
            Namespace("Text"),
            Namespace("RegularExpressions"),
            Keyword("var"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"),
            Regex.Text(" # is not end of line comment"));

    [Theory, CombinatorialData]
    public Task TestRegex11(TestHost testHost)
        => TestAsync(
            """
            using System.Text.RegularExpressions;

            class Program
            {
                // language=regex
                private static string myRegex = @"$(\a\t\u0020)";
            }
            """,
            testHost, Namespace("System"),
            Namespace("Text"),
            Namespace("RegularExpressions"),
            Regex.Anchor("$"),
            Regex.Grouping("("),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("t"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("u"),
            Regex.OtherEscape("0020"),
            Regex.Grouping(")"));

    [Theory, CombinatorialData]
    public Task TestRegexSingleLineRawStringLiteral(TestHost testHost)
        => TestAsync(
            """"
            using System.Text.RegularExpressions;

            class Program
            {
                void Goo()
                {
                    var r = /* lang=regex */ """$\a(?#comment)""";
                }
            }
            """",
            testHost, Namespace("System"),
            Namespace("Text"),
            Namespace("RegularExpressions"),
            Keyword("var"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    public Task TestRegexSingleLineRawStringLiteral_utf8(TestHost testHost)
        => TestAsync(
            """"
            using System.Text.RegularExpressions;

            class Program
            {
                void Goo()
                {
                    var r = /* lang=regex */ """$\a(?#comment)"""u8;
                }
            }
            """",
            testHost, Namespace("System"),
            Namespace("Text"),
            Namespace("RegularExpressions"),
            Keyword("var"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    public Task TestRegexMultiLineRawStringLiteral(TestHost testHost)
        => TestAsync(
            """"
            using System.Text.RegularExpressions;

            class Program
            {
                void Goo()
                {
                    var r = /* lang=regex */ """
                        $\a(?#comment)
                        """;
                }
            }
            """",
            testHost, Namespace("System"),
            Namespace("Text"),
            Namespace("RegularExpressions"),
            Keyword("var"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    public Task TestRegexMultiLineRawStringLiteral_utf8(TestHost testHost)
        => TestAsync(
            """"
            using System.Text.RegularExpressions;

            class Program
            {
                void Goo()
                {
                    var r = /* lang=regex */ """
                        $\a(?#comment)
                        """u8;
                }
            }
            """",
            testHost, Namespace("System"),
            Namespace("Text"),
            Namespace("RegularExpressions"),
            Keyword("var"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/47079")]
    [CombinatorialData]
    public Task TestRegexWithSpecialCSharpCharLiterals(TestHost testHost)
        => TestAsync(
            """
            using System.Text.RegularExpressions;

            class Program
            {
                // the double-quote inside the string should not affect this being classified as a regex.
                private Regex myRegex = new Regex(@"^ "" $";
            }
            """,
            testHost,
            Namespace("System"),
            Namespace("Text"),
            Namespace("RegularExpressions"),
            Class("Regex"),
            Class("Regex"),
            Regex.Anchor("^"),
            Regex.Text(@" """" "),
            Regex.Anchor("$"));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/47079")]
    [CombinatorialData]
    public Task TestRegexWithSpecialCSharpCharLiterals_utf8(TestHost testHost)
        => TestAsync(
            """
            using System.Text.RegularExpressions;

            class Program
            {
                // lang=regex
                private string myRegex = @"^ "" $"u8;
            }
            """,
            testHost,
            Namespace("System"),
            Namespace("Text"),
            Namespace("RegularExpressions"),
            Regex.Anchor("^"),
            Regex.Text(@" """" "),
            Regex.Anchor("$"));

    [Theory, CombinatorialData]
    public Task TestRegexOnApiWithStringSyntaxAttribute_Field(TestHost testHost)
        => TestAsync(
            """
            using System.Diagnostics.CodeAnalysis;
            using System.Text.RegularExpressions;

            class Program
            {
                [StringSyntax(StringSyntaxAttribute.Regex)]
                private string field;

                void Goo()
                {
                    [|this.field = @"$\a(?#comment)";|]
                }
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Field("field"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    public Task TestRegexOnApiWithStringSyntaxAttribute_Field2(TestHost testHost)
        => TestAsync(
            """
            using System.Diagnostics.CodeAnalysis;
            using System.Text.RegularExpressions;

            class Program
            {
                [StringSyntax(StringSyntaxAttribute.Regex)]
                [|private string field = @"$\a(?#comment)";|]
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    public Task TestRegexOnApiWithStringSyntaxAttribute_Property(TestHost testHost)
        => TestAsync(
            """
            using System.Diagnostics.CodeAnalysis;
            using System.Text.RegularExpressions;

            class Program
            {
                [StringSyntax(StringSyntaxAttribute.Regex)]
                private string Prop { get; set; }

                void Goo()
                {
                    [|this.Prop = @"$\a(?#comment)";|]
                }
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Property("Prop"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    public Task TestRegexOnApiWithStringSyntaxAttribute_Property2(TestHost testHost)
        => TestAsync(
            """
            using System.Diagnostics.CodeAnalysis;
            using System.Text.RegularExpressions;

            class Program
            {
                [StringSyntax(StringSyntaxAttribute.Regex)]
                [|private string Prop { get; set; } = @"$\a(?#comment)";|]
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    public Task TestRegexOnApiWithStringSyntaxAttribute_Argument(TestHost testHost)
        => TestAsync(
            """
            using System.Diagnostics.CodeAnalysis;
            using System.Text.RegularExpressions;

            class Program
            {
                private void M([StringSyntax(StringSyntaxAttribute.Regex)] string p)
                {
                }

                void Goo()
                {
                    [|M(@"$\a(?#comment)");|]
                }
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Method("M"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    public Task TestRegexOnApiWithStringSyntaxAttribute_ParamsArgument(TestHost testHost)
        => TestAsync(
            """
            using System.Diagnostics.CodeAnalysis;
            using System.Text.RegularExpressions;

            class Program
            {
                private void M([StringSyntax(StringSyntaxAttribute.Regex)] params string[] p)
                {
                }

                void Goo()
                {
                    [|M(@"$\a(?#comment)");|]
                }
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Method("M"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/64549")]
    [CombinatorialData]
    public Task TestRegexOnApiWithStringSyntaxAttribute_ParamsArgument2(TestHost testHost)
        => TestAsync(
            """
            using System.Diagnostics.CodeAnalysis;
            using System.Text.RegularExpressions;

            class Program
            {
                private void M([StringSyntax(StringSyntaxAttribute.Regex)] params string[] p)
                {
                }

                void Goo()
                {
                    [|M(@"$\a(?#comment)", @"$\a(?#comment)");|]
                }
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Method("M"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    public Task TestRegexOnApiWithStringSyntaxAttribute_ArrayArgument(TestHost testHost)
        => TestAsync(
            """
            using System.Diagnostics.CodeAnalysis;
            using System.Text.RegularExpressions;

            class Program
            {
                private void M([StringSyntax(StringSyntaxAttribute.Regex)] string[] p)
                {
                }

                void Goo()
                {
                    [|M(new string[] { @"$\a(?#comment)" });|]
                }
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Method("M"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    public Task TestRegexOnApiWithStringSyntaxAttribute_ImplicitArrayArgument(TestHost testHost)
        => TestAsync(
            """
            using System.Diagnostics.CodeAnalysis;
            using System.Text.RegularExpressions;

            class Program
            {
                private void M([StringSyntax(StringSyntaxAttribute.Regex)] string[] p)
                {
                }

                void Goo()
                {
                    [|M(new[] { @"$\a(?#comment)" });|]
                }
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Method("M"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    public Task TestRegexOnApiWithStringSyntaxAttribute_CollectionArgument(TestHost testHost)
        => TestAsync(
            """
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Text.RegularExpressions;

            class Program
            {
                private void M([StringSyntax(StringSyntaxAttribute.Regex)] List<string> p)
                {
                }

                void Goo()
                {
                    [|M(new List<string> { @"$\a(?#comment)" });|]
                }
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Method("M"),
            Class("List"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    public Task TestRegexOnApiWithStringSyntaxAttribute_ImplicitCollectionArgument(TestHost testHost)
        => TestAsync(
            """
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Text.RegularExpressions;

            class Program
            {
                private void M([StringSyntax(StringSyntaxAttribute.Regex)] List<string> p)
                {
                }

                void Goo()
                {
                    [|M(new() { @"$\a(?#comment)" });|]
                }
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Method("M"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    public Task TestRegexOnApiWithStringSyntaxAttribute_Argument_Options(TestHost testHost)
        => TestAsync(
            """
            using System.Diagnostics.CodeAnalysis;
            using System.Text.RegularExpressions;

            class Program
            {
                private void M([StringSyntax(StringSyntaxAttribute.Regex)] string p, RegexOptions options)
                {
                }

                void Goo()
                {
                    [|M(@"$\a(?#comment) # is end of line comment", RegexOptions.IgnorePatternWhitespace);|]
                }
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Method("M"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"),
            Regex.Comment("# is end of line comment"),
            Enum("RegexOptions"),
            EnumMember("IgnorePatternWhitespace"));

    [Theory, CombinatorialData]
    public Task TestRegexOnApiWithStringSyntaxAttribute_Attribute(TestHost testHost)
        => TestAsync(
            """
            using System;
            using System.Diagnostics.CodeAnalysis;
            using System.Text.RegularExpressions;

            [AttributeUsage(AttributeTargets.Field)]
            class RegexTestAttribute : Attribute
            {
                public RegexTestAttribute([StringSyntax(StringSyntaxAttribute.Regex)] string value) { }
            }

            class Program
            {
                [|[RegexTest(@"$\a(?#comment)")]|]
                private string field;
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Class("RegexTest"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/61947")]
    public Task TestRegexOnApiWithStringSyntaxAttribute_AttributeField(TestHost testHost)
        => TestAsync(
            """
            using System;
            using System.Diagnostics.CodeAnalysis;
            using System.Text.RegularExpressions;

            [AttributeUsage(AttributeTargets.Field)]
            class RegexTestAttribute : Attribute
            {
                public RegexTestAttribute() { }

                [StringSyntax(StringSyntaxAttribute.Regex)]
                public string value;
            }

            class Program
            {
                [|[RegexTest(value = @"$\a(?#comment)")]|]
                private string field;
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Class("RegexTest"),
            Field("value"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/61947")]
    public Task TestRegexOnApiWithStringSyntaxAttribute_AttributeProperty(TestHost testHost)
        => TestAsync(
            """
            using System;
            using System.Diagnostics.CodeAnalysis;
            using System.Text.RegularExpressions;

            [AttributeUsage(AttributeTargets.Field)]
            class RegexTestAttribute : Attribute
            {
                public RegexTestAttribute() { }

                [StringSyntax(StringSyntaxAttribute.Regex)]
                public string value { get; set; }
            }

            class Program
            {
                [|[RegexTest(value = @"$\a(?#comment)")]|]
                private string field;
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Class("RegexTest"),
            Property("value"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    public Task TestRegexOnApiWithStringSyntaxAttribute_ParamsAttribute(TestHost testHost)
        => TestAsync(
            """
            using System;
            using System.Diagnostics.CodeAnalysis;
            using System.Text.RegularExpressions;

            [AttributeUsage(AttributeTargets.Field)]
            class RegexTestAttribute : Attribute
            {
                public RegexTestAttribute([StringSyntax(StringSyntaxAttribute.Regex)] params string[] value) { }
            }

            class Program
            {
                [|[RegexTest(@"$\a(?#comment)")]|]
                private string field;
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Class("RegexTest"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    public Task TestRegexOnApiWithStringSyntaxAttribute_ArrayAttribute(TestHost testHost)
        => TestAsync(
            """
            using System;
            using System.Diagnostics.CodeAnalysis;
            using System.Text.RegularExpressions;

            [AttributeUsage(AttributeTargets.Field)]
            class RegexTestAttribute : Attribute
            {
                public RegexTestAttribute([StringSyntax(StringSyntaxAttribute.Regex)] string[] value) { }
            }

            class Program
            {
                [|[RegexTest(new string[] { @"$\a(?#comment)" })]|]
                private string field;
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Class("RegexTest"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    public Task TestRegexOnApiWithStringSyntaxAttribute_ImplicitArrayAttribute(TestHost testHost)
        => TestAsync(
            """
            using System;
            using System.Diagnostics.CodeAnalysis;
            using System.Text.RegularExpressions;

            [AttributeUsage(AttributeTargets.Field)]
            class RegexTestAttribute : Attribute
            {
                public RegexTestAttribute([StringSyntax(StringSyntaxAttribute.Regex)] string[] value) { }
            }

            class Program
            {
                [|[RegexTest(new[] { @"$\a(?#comment)" })]|]
                private string field;
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Class("RegexTest"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    public Task TestIncompleteRegexLeadingToStringInsideSkippedTokensInsideADirective(TestHost testHost)
        => TestAsync(
            """
            using System.Text.RegularExpressions;

            class Program
            {
                void M()
                {
                    // not terminating this string caused us to eat up to the quote on the next line.
                    // we then treated #comment as a directive with a lot of skipped tokens on it, including
                    // a skipped token for ";
                    //
                    // Because it's a comment on a directive, special lexing rules apply (i.e. no escape
                    // characters are supposed, and we want our system to bail there and not try to validate
                    // it.
                    var r = new Regex(@"$;
                    var s = /* language=regex */ @"(?#comment)|(\b\G\z)|(?<name>sub){0,5}?^";
                }
            }
            """,
            testHost, Namespace("System"),
            Namespace("Text"),
            Namespace("RegularExpressions"),
            Keyword("var"),
            Class("Regex"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/61982")]
    public Task TestRegexAmbiguity1(TestHost testHost)
        => TestAsync(
            """
            using System.Text.RegularExpressions;

            class Program
            {
                void Goo()
                {
                    var r = Regex.Match("", [|@"$\a(?#comment)"|]
            """,
            testHost,
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/61982")]
    public Task TestRegexAmbiguity2(TestHost testHost)
        => TestAsync(
            """
            using System.Text.RegularExpressions;

            class Program
            {
                void Goo()
                {
                    var r = Regex.Match("", [|@"$\a(?#comment)"|],
            """,
            testHost,
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));

    [Theory, CombinatorialData]
    public Task TestRegexNotOnBinaryExpression(TestHost testHost)
        => TestAsync(
            """
            using System.Text.RegularExpressions;

            class Program
            {
                void Goo()
                {
                    // language=regex
                    var r = @"[a-" + @"z]";
                }
            }
            """,
            testHost,
            Namespace("System"),
            Namespace("Text"),
            Namespace("RegularExpressions"),
            Keyword("var"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/77189")]
    public Task TestStringFieldUsedLater_ProperModifiers(
        TestHost testHost,
        [CombinatorialValues("const", "static readonly")] string modifiers)
        => TestAsync(
            $$"""
            using System.Diagnostics.CodeAnalysis;
            using System.Text.RegularExpressions;

            class Program
            {
                private {{modifiers}} string regexValue = [|@"$(\a\t\u0020)"|];

                void Goo()
                {
                    Bar(regexValue);
                }

                void Bar([StringSyntax(StringSyntaxAttribute.Regex)] string p)
                {
                }
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Regex.Anchor("$"),
            Regex.Grouping("("),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("t"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("u"),
            Regex.OtherEscape("0020"),
            Regex.Grouping(")"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/77189")]
    public Task TestStringFieldUsedLater_ImproperModifiers(
        TestHost testHost,
        [CombinatorialValues("", "static", "readonly")] string modifiers)
        => TestAsync(
            $$"""
            using System.Diagnostics.CodeAnalysis;
            using System.Text.RegularExpressions;

            class Program
            {
                private {{modifiers}} string regexValue = [|@"$(\a\t\u0020)"|];

                void Goo()
                {
                    Bar(regexValue);
                }

                void Bar([StringSyntax(StringSyntaxAttribute.Regex)] string p)
                {
                }
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/80179")]
    public Task TestRegexOnApiWithStringSyntaxAttribute_ParamsReadOnlyCollectionArgument(TestHost testHost)
        => TestAsync(
            """
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Text.RegularExpressions;

            class Program
            {
                private void M([StringSyntax(StringSyntaxAttribute.Regex)] params IReadOnlyCollection<string> p)
                {
                }

                void Goo()
                {
                    [|M([@"$\a(?#comment)"]);|]
                }
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Method("M"),
            Regex.Anchor("$"),
            Regex.OtherEscape("\\"),
            Regex.OtherEscape("a"),
            Regex.Comment("(?#comment)"));
}
