// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Classification;
using Microsoft.CodeAnalysis.Remote.Testing;
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

        var actualOrdered = actual.OrderBy(static (t1, t2) => t1.TextSpan.Start - t2.TextSpan.Start);

        var actualFormatted = actualOrdered.Select(a => new FormattedClassification(allCode.Substring(a.TextSpan.Start, a.TextSpan.Length), a.ClassificationType));
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
            testHost);
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
            testHost);
    }

    [Theory, CombinatorialData]
    public async Task TestEmbeddedCSharpCaret2(TestHost testHost)
    {
        await TestEmbeddedCSharpAsync("""
            cla$$ss D
            {
            }
            """,
            testHost);
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
            Namespace("System"),
            Struct("Int32"));
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
            Namespace("System"),
            Struct("Int32"));
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
            testHost);
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
            testHost);
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
            testHost);
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
            testHost);
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
            testHost);
    }
}
