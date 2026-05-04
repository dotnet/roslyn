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
    [WorkItem("https://github.com/dotnet/roslyn/issues/68534")]
    public Task TestJson1(TestHost testHost)
        => TestAsync(
            """
            class Program
            {
                void Goo()
                {
                    // lang=json
                    var r = @"[/*comment*/{ 'goo': 0, bar: -Infinity, ""baz"": true }, new Date(), text, 'str'] // comment";
                }
            }
            """,
            testHost,
            Keyword("var"),
            Json.Array("["),
            Json.Comment("/*comment*/"),
            Json.Object("{"),
            Json.PropertyName("'goo'"),
            Json.Punctuation(":"),
            Json.Number("0"),
            Json.Punctuation(","),
            Json.PropertyName("bar"),
            Json.Punctuation(":"),
            Json.Operator("-"),
            Json.Keyword("Infinity"),
            Json.Punctuation(","),
            Json.PropertyName("""
                ""baz""
                """),
            Json.Punctuation(":"),
            Json.Keyword("true"),
            Json.Object("}"),
            Json.Punctuation(","),
            Json.Keyword("new"),
            Json.ConstructorName("Date"),
            Json.Punctuation("("),
            Json.Punctuation(")"),
            Json.Punctuation(","),
            Json.Text("text"),
            Json.Punctuation(","),
            Json.String("'str'"),
            Json.Array("]"),
            Json.Comment("// comment"));

    [Theory, CombinatorialData]
    public Task TestJson_RawString(TestHost testHost)
        => TestAsync(
            """"
            class Program
            {
                void Goo()
                {
                    // lang=json
                    var r = """[/*comment*/{ 'goo': 0 }]""";
                }
            }
            """",
            testHost,
            Keyword("var"),
            Json.Array("["),
            Json.Comment("/*comment*/"),
            Json.Object("{"),
            Json.PropertyName("'goo'"),
            Json.Punctuation(":"),
            Json.Number("0"),
            Json.Object("}"),
            Json.Array("]"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/68534")]
    public Task TestMultiLineJson1(TestHost testHost)
        => TestAsync(
            """
            class Program
            {
                void Goo()
                {
                    // lang=json
                    var r = @"[
                        /*comment*/
                        {
                            'goo': 0,
                            bar: -Infinity,
                            ""baz"": true,
                            0: null
                        },
                        new Date(),
                        text,
                        'str'] // comment";
                }
            }
            """,
            testHost,
            Keyword("var"),
            Json.Array("["),
            Json.Comment("/*comment*/"),
            Json.Object("{"),
            Json.PropertyName("'goo'"),
            Json.Punctuation(":"),
            Json.Number("0"),
            Json.Punctuation(","),
            Json.PropertyName("bar"),
            Json.Punctuation(":"),
            Json.Operator("-"),
            Json.Keyword("Infinity"),
            Json.Punctuation(","),
            Json.PropertyName("""
                ""baz""
                """),
            Json.Punctuation(":"),
            Json.Keyword("true"),
            Json.Punctuation(","),
            Json.PropertyName("0"),
            Json.Punctuation(":"),
            Json.Keyword("null"),
            Json.Object("}"),
            Json.Punctuation(","),
            Json.Keyword("new"),
            Json.ConstructorName("Date"),
            Json.Punctuation("("),
            Json.Punctuation(")"),
            Json.Punctuation(","),
            Json.Text("text"),
            Json.Punctuation(","),
            Json.String("'str'"),
            Json.Array("]"),
            Json.Comment("// comment"));

    [Theory, CombinatorialData]
    public Task TestJson_NoComment_NotLikelyJson(TestHost testHost)
        => TestAsync("""
            class C
            {
                void Goo()
                {
                    var r = @"[1, 2, 3]";
                }
            }
            """,
            testHost,
            Keyword("var"));

    [Theory, CombinatorialData]
    public Task TestJson_NoComment_LikelyJson(TestHost testHost)
        => TestAsync("""
            class C
            {
                void Goo()
                {
                    var r = @"[1, { prop: 0 }, 3]";
                }
            }
            """,
            testHost,
            Keyword("var"),
            Json.Array("["),
            Json.Number("1"),
            Json.Punctuation(","),
            Json.Object("{"),
            Json.PropertyName("prop"),
            Json.Punctuation(":"),
            Json.Number("0"),
            Json.Object("}"),
            Json.Punctuation(","),
            Json.Number("3"),
            Json.Array("]"));

    [Theory, CombinatorialData]
    public Task TestJsonOnApiWithStringSyntaxAttribute_Field(TestHost testHost)
        => TestAsync(
            """
            using System.Diagnostics.CodeAnalysis;

            class Program
            {
                [StringSyntax(StringSyntaxAttribute.Json)]
                private string field;
                void Goo()
                {
                    [|this.field = @"[{ 'goo': 0}]";|]
                }
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Field("field"),
            Json.Array("["),
            Json.Object("{"),
            Json.PropertyName("'goo'"),
            Json.Punctuation(":"),
            Json.Number("0"),
            Json.Object("}"),
            Json.Array("]"));

    [Theory, CombinatorialData]
    public Task TestJsonOnApiWithStringSyntaxAttribute_Field_FromLocal(TestHost testHost)
        => TestAsync(
            """
            using System.Diagnostics.CodeAnalysis;

            class Program
            {
                [StringSyntax(StringSyntaxAttribute.Json)]
                private string field;
                void Goo()
                {
                    [|var v = @"[{ 'goo': 0}]";|]
                    this.field = v;
                }
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Keyword("var"),
            Json.Array("["),
            Json.Object("{"),
            Json.PropertyName("'goo'"),
            Json.Punctuation(":"),
            Json.Number("0"),
            Json.Object("}"),
            Json.Array("]"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/74020")]
    public Task TestJsonOnApiWithStringSyntaxAttribute_OtherLanguage_Field(TestHost testHost)
        => TestAsync(
            """
            using System.Diagnostics.CodeAnalysis;

            class Program
            {
                [StringSyntax("notjson")]
                private string field;
                void Goo()
                {
                    [|this.field = @"[{ 'goo': 0}]";|]
                }
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Field("field"));

    [Theory, CombinatorialData]
    public Task TestJsonOnApiWithStringSyntaxAttribute_Property(TestHost testHost)
        => TestAsync(
            """
            using System.Diagnostics.CodeAnalysis;

            class Program
            {
                [StringSyntax(StringSyntaxAttribute.Json)]
                private string Prop { get; set; }
                void Goo()
                {
                    [|this.Prop = @"[{ 'goo': 0}]";|]
                }
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Property("Prop"),
            Json.Array("["),
            Json.Object("{"),
            Json.PropertyName("'goo'"),
            Json.Punctuation(":"),
            Json.Number("0"),
            Json.Object("}"),
            Json.Array("]"));

    [Theory, CombinatorialData]
    public Task TestJsonOnApiWithStringSyntaxAttribute_Argument(TestHost testHost)
        => TestAsync(
            """
            using System.Diagnostics.CodeAnalysis;

            class Program
            {
                private void M([StringSyntax(StringSyntaxAttribute.Json)] string p)
                {
                }

                void Goo()
                {
                    [|M(@"[{ 'goo': 0}]");|]
                }
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Method("M"),
            Json.Array("["),
            Json.Object("{"),
            Json.PropertyName("'goo'"),
            Json.Punctuation(":"),
            Json.Number("0"),
            Json.Object("}"),
            Json.Array("]"));

    [Theory, CombinatorialData]
    public Task TestJsonOnApiWithStringSyntaxAttribute_Argument_FromLocal(TestHost testHost)
        => TestAsync(
            """
            using System.Diagnostics.CodeAnalysis;

            class Program
            {
                private void M([StringSyntax(StringSyntaxAttribute.Json)] string p)
                {
                }

                void Goo()
                {
                    [|var v = @"[{ 'goo': 0}]";|]
                    M(v);
                }
            }
            """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Keyword("var"),
            Json.Array("["),
            Json.Object("{"),
            Json.PropertyName("'goo'"),
            Json.Punctuation(":"),
            Json.Number("0"),
            Json.Object("}"),
            Json.Array("]"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69237")]
    public Task TestJsonOnApiWithStringSyntaxAttribute_PropertyInitializer(TestHost testHost)
        => TestAsync(
            """"
            using System.Diagnostics.CodeAnalysis;

            public sealed record Foo
            {
                [StringSyntax(StringSyntaxAttribute.Json)]
                public required string Bar { get; set; }
            }

            class Program
            {
                void Goo()
                {
                    var f = new Foo { [|Bar = """[1, 2, 3]"""|] };
                }
            }
            """" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Property("Bar"),
            Json.Array("["),
            Json.Number("1"),
            Json.Punctuation(","),
            Json.Number("2"),
            Json.Punctuation(","),
            Json.Number("3"),
            Json.Array("]"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69237")]
    public Task TestJsonOnApiWithStringSyntaxAttribute_PropertyInitializer_FromLocal(TestHost testHost)
        => TestAsync(
            """"
            using System.Diagnostics.CodeAnalysis;

            public sealed record Foo
            {
                [StringSyntax(StringSyntaxAttribute.Json)]
                public required string Bar { get; set; }
            }

            class Program
            {
                void Goo()
                {
                    [|var v = """[1, 2, 3]""";|]
                    var f = new Foo { Bar = v };
                }
            }
            """" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Keyword("var"),
            Json.Array("["),
            Json.Number("1"),
            Json.Punctuation(","),
            Json.Number("2"),
            Json.Punctuation(","),
            Json.Number("3"),
            Json.Array("]"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69237")]
    public Task TestJsonOnApiWithStringSyntaxAttribute_WithExpression(TestHost testHost)
        => TestAsync(
            """"
            using System.Diagnostics.CodeAnalysis;

            public sealed record Foo
            {
                [StringSyntax(StringSyntaxAttribute.Json)]
                public required string Bar { get; set; }
            }

            class Program
            {
                void Goo()
                {
                    var f = new Foo { Bar =  "" };
                    f = f with { [|Bar = """[1, 2, 3]"""|] };
                }
            }
            """" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Property("Bar"),
            Json.Array("["),
            Json.Number("1"),
            Json.Punctuation(","),
            Json.Number("2"),
            Json.Punctuation(","),
            Json.Number("3"),
            Json.Array("]"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69237")]
    public Task TestJsonOnApiWithStringSyntaxAttribute_WithExpression_FromLocal(TestHost testHost)
        => TestAsync(
            """"
            using System.Diagnostics.CodeAnalysis;

            public sealed record Foo
            {
                [StringSyntax(StringSyntaxAttribute.Json)]
                public required string Bar { get; set; }
            }

            class Program
            {
                void Goo()
                {
                    var f = new Foo { Bar =  "" };
                    [|var v = """[1, 2, 3]""";|]
                    f = f with { Bar = v };
                }
            }
            """" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Keyword("var"),
            Json.Array("["),
            Json.Number("1"),
            Json.Punctuation(","),
            Json.Number("2"),
            Json.Punctuation(","),
            Json.Number("3"),
            Json.Array("]"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69237")]
    public Task TestJsonOnApiWithStringSyntaxAttribute_WithExpression_FromLocal2(TestHost testHost)
        => TestAsync(
            """"
            using System.Diagnostics.CodeAnalysis;

            public sealed record Foo
            {
                [StringSyntax(StringSyntaxAttribute.Json)]
                public required string Bar { get; set; }
            }

            class Program
            {
                void Goo()
                {
                    var f = new Foo { Bar =  "" };
                    string v;
                    [|v = """[1, 2, 3]""";|]
                    f = f with { Bar = v };
                }
            }
            """" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
            testHost,
            Local("v"),
            Json.Array("["),
            Json.Number("1"),
            Json.Punctuation(","),
            Json.Number("2"),
            Json.Punctuation(","),
            Json.Number("3"),
            Json.Array("]"));
}
