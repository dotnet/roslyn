// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.UnitTests.Classification;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Shared;
using Microsoft.CodeAnalysis.Test.Utilities.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification;

public partial class SemanticClassifierTests : AbstractCSharpClassifierTests
{
    private const string s_testMarkup = """

        static class Test
        {
            public static void M([System.Diagnostics.CodeAnalysis.StringSyntax("C#-test")] string code) { }
        }
        """ + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp;

    protected async Task TestEmbeddedCSharpAsync(
       string code,
       TestHost testHost,
       params FormattedClassification[] expected)
    {
        var allCode = $$"""""
            class C
            {
                void M()
                {
                    Test.M(""""
            {{code}}
            """");
                }
            }
            """"" + s_testMarkup;

        var start = allCode.IndexOf(code, StringComparison.Ordinal);
        var length = code.Length;
        var span = new TextSpan(start, length);

        var actual = await GetClassificationSpansAsync(allCode, span, options: null, testHost);

        // Massage the results a bit so that the TestCode segments don't overlap the non-test-code segments.

        var nonTestCodeSpans = actual.Where(s => s.ClassificationType != ClassificationTypeNames.TestCode).OrderBy((t1, t2) => t1.TextSpan.Start - t2.TextSpan.Start).ToImmutableArray();
        var testCodeSpans = actual.Where(s => s.ClassificationType == ClassificationTypeNames.TestCode).OrderBy((t1, t2) => t1.TextSpan.Start - t2.TextSpan.Start).ToImmutableArray();

        using var _ = ArrayBuilder<ClassifiedSpan>.GetInstance(out var totalSpans);

        var normalizedNonTestCodeSpans = new NormalizedTextSpanCollection(nonTestCodeSpans.Select(c => c.TextSpan));
        totalSpans.AddRange(nonTestCodeSpans);
        foreach (var testCodeSpan in testCodeSpans)
        {
            var remainder = NormalizedTextSpanCollection.Difference(new NormalizedTextSpanCollection(testCodeSpan.TextSpan), normalizedNonTestCodeSpans);
            foreach (var current in remainder)
            {
                if (!current.IsEmpty)
                    totalSpans.Add(new ClassifiedSpan(current, testCodeSpan.ClassificationType));
            }
        }

        var actualOrdered = totalSpans.OrderBy(static (t1, t2) => t1.TextSpan.Start - t2.TextSpan.Start).ToImmutableArray();
        var actualFormatted = actualOrdered.SelectAsArray(a => new FormattedClassification(allCode.Substring(a.TextSpan.Start, a.TextSpan.Length), a.ClassificationType));
        AssertEx.Equal(expected, actualFormatted);
    }

    [Theory, CombinatorialData]
    public async Task TestEmbeddedCSharpMarkup1(TestHost testHost)
    {
        await TestEmbeddedCSharpAsync("""
            class D
            {
            }
            """,
            testHost,
            Keyword("class"),
            TestCode(" "),
            Class("D"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);
    }

    [Theory, CombinatorialData]
    public async Task TestEmbeddedCSharpCaret1(TestHost testHost)
    {
        await TestEmbeddedCSharpAsync("""
            class D
            {
                $$
            }
            """,
            testHost,
            Keyword("class"),
            TestCode(" "),
            Class("D"),
            Punctuation.OpenCurly,
            TestCode("    "),
            TestCodeMarkdown("$$"),
            Punctuation.CloseCurly);
    }

    [Theory, CombinatorialData]
    public async Task TestEmbeddedCSharpCaret2(TestHost testHost)
    {
        await TestEmbeddedCSharpAsync("""
            cla$$ss D
            {
            }
            """,
            testHost,
            Keyword("cla"),
            TestCodeMarkdown("$$"),
            Keyword("ss"),
            TestCode(" "),
            Class("D"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);
    }

    [Theory, CombinatorialData]
    public async Task TestEmbeddedCSharpSpan1(TestHost testHost)
    {
        await TestEmbeddedCSharpAsync("""
            class D
            {
                [|System.Int32 i;|]
            }
            """,
            testHost,
            Keyword("class"),
            TestCode(" "),
            Class("D"),
            Punctuation.OpenCurly,
            TestCode("    "),
            TestCodeMarkdown("[|"),
            Namespace("System"),
            Operators.Dot,
            Struct("Int32"),
            TestCode(" "),
            Field("i"),
            Punctuation.Semicolon,
            TestCodeMarkdown("|]"),
            Punctuation.CloseCurly);
    }

    [Theory, CombinatorialData]
    public async Task TestEmbeddedCSharpSpan2(TestHost testHost)
    {
        await TestEmbeddedCSharpAsync("""
            class D
            {
                {|Example:System.Int32 i;|}
            }
            """,
            testHost,
            Keyword("class"),
            TestCode(" "),
            Class("D"),
            Punctuation.OpenCurly,
            TestCode("    "),
            TestCodeMarkdown("{|Example:"),
            Namespace("System"),
            Operators.Dot,
            Struct("Int32"),
            TestCode(" "),
            Field("i"),
            Punctuation.Semicolon,
            TestCodeMarkdown("|}"),
            Punctuation.CloseCurly);
    }

    [Theory, CombinatorialData]
    public async Task TestEmbeddedCSharpSpan3(TestHost testHost)
    {
        await TestEmbeddedCSharpAsync("""
            class D
            {
                [|System.Int32 i;
            }
            """,
            testHost,
            Keyword("class"),
            TestCode(" "),
            Class("D"),
            Punctuation.OpenCurly,
            TestCode("    "),
            TestCodeMarkdown("[|"),
            Namespace("System"),
            Operators.Dot,
            Struct("Int32"),
            TestCode(" "),
            Field("i"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly);
    }

    [Theory, CombinatorialData]
    public async Task TestEmbeddedCSharpSpan4(TestHost testHost)
    {
        await TestEmbeddedCSharpAsync("""
            class D
            {
                System.Int32 i;|]
            }
            """,
            testHost,
            Keyword("class"),
            TestCode(" "),
            Class("D"),
            Punctuation.OpenCurly,
            TestCode("    "),
            Namespace("System"),
            Operators.Dot,
            Struct("Int32"),
            TestCode(" "),
            Field("i"),
            Punctuation.Semicolon,
            TestCodeMarkdown("|]"),
            Punctuation.CloseCurly);
    }

    [Theory, CombinatorialData]
    public async Task TestEmbeddedCSharpSpan5(TestHost testHost)
    {
        await TestEmbeddedCSharpAsync("""
            class D
            {
                {|Example:System.Int32 i;
            }
            """,
            testHost,
            Keyword("class"),
            TestCode(" "),
            Class("D"),
            Punctuation.OpenCurly,
            TestCode("    "),
            TestCodeMarkdown("{|Example:"),
            Namespace("System"),
            Operators.Dot,
            Struct("Int32"),
            TestCode(" "),
            Field("i"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly);
    }

    [Theory, CombinatorialData]
    public async Task TestEmbeddedCSharpSpan6(TestHost testHost)
    {
        await TestEmbeddedCSharpAsync("""
            class D
            {
                System.Int32 i;|}
            }
            """,
            testHost,
            Keyword("class"),
            TestCode(" "),
            Class("D"),
            Punctuation.OpenCurly,
            TestCode("    "),
            Namespace("System"),
            Operators.Dot,
            Struct("Int32"),
            TestCode(" "),
            Field("i"),
            Punctuation.Semicolon,
            TestCodeMarkdown("|}"),
            Punctuation.CloseCurly);
    }

    [Theory, CombinatorialData]
    public async Task TestEmbeddedCSharpSpan7(TestHost testHost)
    {
        await TestEmbeddedCSharpAsync("""
            class D
            {
                {|Example System.Int32 i;|}
            }
            """,
            testHost,
            Keyword("class"),
            TestCode(" "),
            Class("D"),
            Punctuation.OpenCurly,
            TestCode("    "),
            Punctuation.OpenCurly,
            Operators.Bar,
            Identifier("Example"),
            TestCode(" "),
            Namespace("System"),
            Operators.Dot,
            Method("Int32"),
            TestCode(" "),
            Identifier("i"),
            Punctuation.Semicolon,
            TestCodeMarkdown("|}"),
            Punctuation.CloseCurly);
    }

    [Theory, CombinatorialData]
    public async Task TestEmbeddedCSharpSpan8(TestHost testHost)
    {
        await TestEmbeddedCSharpAsync("""
            class D
            {
                Sys[|tem.In$$t3|]2 i;
            }
            """,
            testHost,
            Keyword("class"),
            TestCode(" "),
            Class("D"),
            Punctuation.OpenCurly,
            TestCode("    "),
            Namespace("Sys"),
            TestCodeMarkdown("[|"),
            Namespace("tem"),
            Operators.Dot,
            Struct("In"),
            TestCodeMarkdown("$$"),
            Struct("t3"),
            TestCodeMarkdown("|]"),
            Struct("2"),
            TestCode(" "),
            Field("i"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly);
    }

    [Theory, CombinatorialData]
    public async Task TestEmbeddedCSharpString1(TestHost testHost)
    {
        await TestEmbeddedCSharpAsync("""
            class D
            {
                // Embedded escapes not classified.
                string s = "\r\n";
            }
            """,
            testHost,
            Keyword("class"),
            TestCode(" "),
            Class("D"),
            Punctuation.OpenCurly,
            TestCode("    "),
            Comment("// Embedded escapes not classified."),
            TestCode("    "),
            Keyword("string"),
            TestCode(" "),
            Field("s"),
            TestCode(" "),
            Operators.Equals,
            TestCode(" "),
            String(@"""\r\n"""),
            Punctuation.Semicolon,
            Punctuation.CloseCurly);
    }

    [Theory, CombinatorialData]
    public async Task TestEmbeddedCSharpString2(TestHost testHost)
    {
        await TestEmbeddedCSharpAsync(""""
            class D
            {
                string s = """
                    Goo
                    """;
            }
            """",
            testHost,
            Keyword("class"),
            TestCode(" "),
            Class("D"),
            Punctuation.OpenCurly,
            TestCode("    "),
            Keyword("string"),
            TestCode(" "),
            Field("s"),
            TestCode(" "),
            Operators.Equals,
            TestCode(" "),
            String(""""
                    """
                            Goo
                            """
                    """"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly);
    }
}
