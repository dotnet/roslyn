// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting.Indentation;

[Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
public class SmartTokenFormatterFormatTokenTests : CSharpFormatterTestsBase
{
    public SmartTokenFormatterFormatTokenTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
    public async Task EmptyFile1()
    {
        var code = @"{";

        await AssertSmartTokenFormatterOpenBraceAsync(
            code,
            indentationLine: 0,
            expectedSpace: 0);
    }

    [Fact]
    public async Task EmptyFile2()
    {
        var code = @"}";

        await ExpectException_SmartTokenFormatterCloseBraceAsync(
            code,
            indentationLine: 0);
    }

    [Fact]
    public async Task Namespace1()
    {
        var code = @"namespace NS
{";

        await AssertSmartTokenFormatterOpenBraceAsync(
            code,
            indentationLine: 1,
            expectedSpace: 0);
    }

    [Fact]
    public async Task Namespace2()
    {
        var code = @"namespace NS
}";

        await AssertSmartTokenFormatterCloseBraceAsync(
            code,
            indentationLine: 1,
            expectedSpace: 0);
    }

    [Fact]
    public async Task Namespace3()
    {
        var code = @"namespace NS
{
    }";

        await AssertSmartTokenFormatterCloseBraceAsync(
            code,
            indentationLine: 2,
            expectedSpace: 0);
    }

    [Fact]
    public async Task Class1()
    {
        var code = @"namespace NS
{
    class Class
    {";

        await AssertSmartTokenFormatterOpenBraceAsync(
            code,
            indentationLine: 3,
            expectedSpace: 4);
    }

    [Fact]
    public async Task Class2()
    {
        var code = @"namespace NS
{
    class Class
    }";

        await AssertSmartTokenFormatterCloseBraceAsync(
            code,
            indentationLine: 3,
            expectedSpace: 4);
    }

    [Fact]
    public async Task Class3()
    {
        var code = @"namespace NS
{
    class Class
    {
        }";

        await AssertSmartTokenFormatterCloseBraceAsync(
            code,
            indentationLine: 4,
            expectedSpace: 4);
    }

    [Fact]
    public async Task Method1()
    {
        var code = @"namespace NS
{
    class Class
    {
        void Method(int i)
        {";

        await AssertSmartTokenFormatterOpenBraceAsync(
            code,
            indentationLine: 5,
            expectedSpace: 8);
    }

    [Fact]
    public async Task Method2()
    {
        var code = @"namespace NS
{
    class Class
    {
        void Method(int i)
        }";

        await AssertSmartTokenFormatterCloseBraceAsync(
            code,
            indentationLine: 5,
            expectedSpace: 8);
    }

    [Fact]
    public async Task Method3()
    {
        var code = @"namespace NS
{
    class Class
    {
        void Method(int i)
        {
            }";

        await AssertSmartTokenFormatterCloseBraceAsync(
            code,
            indentationLine: 6,
            expectedSpace: 8);
    }

    [Fact]
    public async Task Property1()
    {
        var code = @"namespace NS
{
    class Class
    {
        int Goo
            {";

        await AssertSmartTokenFormatterOpenBraceAsync(
            code,
            indentationLine: 5,
            expectedSpace: 8);
    }

    [Fact]
    public async Task Property2()
    {
        var code = @"namespace NS
{
    class Class
    {
        int Goo
        {
            }";

        await AssertSmartTokenFormatterCloseBraceAsync(
            code,
            indentationLine: 6,
            expectedSpace: 8);
    }

    [Fact]
    public async Task Event1()
    {
        var code = @"namespace NS
{
    class Class
    {
        event EventHandler Goo
            {";

        await AssertSmartTokenFormatterOpenBraceAsync(
            code,
            indentationLine: 5,
            expectedSpace: 8);
    }

    [Fact]
    public async Task Event2()
    {
        var code = @"namespace NS
{
    class Class
    {
        event EventHandler Goo
        {
            }";

        await AssertSmartTokenFormatterCloseBraceAsync(
            code,
            indentationLine: 6,
            expectedSpace: 8);
    }

    [Fact]
    public async Task Indexer1()
    {
        var code = @"namespace NS
{
    class Class
    {
        int this[int index]
            {";

        await AssertSmartTokenFormatterOpenBraceAsync(
            code,
            indentationLine: 5,
            expectedSpace: 8);
    }

    [Fact]
    public async Task Indexer2()
    {
        var code = @"namespace NS
{
    class Class
    {
        int this[int index]
        {
            }";

        await AssertSmartTokenFormatterCloseBraceAsync(
            code,
            indentationLine: 6,
            expectedSpace: 8);
    }

    [Fact]
    public async Task Block1()
    {
        var code = @"namespace NS
{
    class Class
    {
        void Method(int i)
        {
        {";

        await AssertSmartTokenFormatterOpenBraceAsync(
            code,
            indentationLine: 6,
            expectedSpace: 12);
    }

    [Fact]
    public async Task Block2()
    {
        var code = @"namespace NS
{
    class Class
    {
        void Method(int i)
        }
        }";

        await AssertSmartTokenFormatterCloseBraceAsync(
            code,
            indentationLine: 6,
            expectedSpace: 0);
    }

    [Fact]
    public async Task Block3()
    {
        var code = @"namespace NS
{
    class Class
    {
        void Method(int i)
        {
            {
                }";

        await AssertSmartTokenFormatterCloseBraceAsync(
            code,
            indentationLine: 7,
            expectedSpace: 12);
    }

    [Fact]
    public async Task Block4()
    {
        var code = @"namespace NS
{
    class Class
    {
        void Method(int i)
        {
                {
        }";

        await AssertSmartTokenFormatterCloseBraceAsync(
            code,
            indentationLine: 7,
            expectedSpace: 12);
    }

    [Fact]
    public async Task ArrayInitializer1()
    {
        var code = @"namespace NS
{
    class Class
    {
        void Method(int i)
        {
            var a = new []          {
        }";

        var expected = @"namespace NS
{
    class Class
    {
        void Method(int i)
        {
            var a = new [] {
        }";

        await AssertSmartTokenFormatterOpenBraceAsync(
            expected,
            code,
            indentationLine: 6);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537827")]
    public async Task ArrayInitializer3()
    {
        var code = @"namespace NS
{
    class Class
    {
        void Method(int i)
        {
            int[,] arr =
            {
                {1,1}, {2,2}
}
        }";

        await AssertSmartTokenFormatterCloseBraceAsync(
            code,
            indentationLine: 9,
            expectedSpace: 12);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543142")]
    public async Task EnterWithTrailingWhitespace()
    {
        var code = @"class Class
{
    void Method(int i)
    {
        var a = new {
 };
";

        await AssertSmartTokenFormatterCloseBraceAsync(
            code,
            indentationLine: 5,
            expectedSpace: 8);
    }

    [Fact, WorkItem(9216, "DevDiv_Projects/Roslyn")]
    public async Task OpenBraceWithBaseIndentation()
    {
        var markup = @"
class C
{
    void M()
    {
[|#line ""Default.aspx"", 273
        if (true)
$${
        }
#line default
#line hidden|]
    }
}";
        await AssertSmartTokenFormatterOpenBraceWithBaseIndentationAsync(markup, baseIndentation: 7, expectedIndentation: 11);
    }

    [Fact, WorkItem(9216, "DevDiv_Projects/Roslyn")]
    public async Task CloseBraceWithBaseIndentation()
    {
        var markup = @"
class C
{
    void M()
    {
[|#line ""Default.aspx"", 273
        if (true)
        {
$$}
#line default
#line hidden|]
    }
}";
        await AssertSmartTokenFormatterCloseBraceWithBaseIndentation(markup, baseIndentation: 7, expectedIndentation: 11);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/766159")]
    public async Task TestPreprocessor()
    {
        var code = @"
class C
{
    void M()
    {
        #
    }
}";

        var actualIndentation = await GetSmartTokenFormatterIndentationAsync(code, indentationLine: 5, ch: '#', useTabs: false);
        Assert.Equal(0, actualIndentation);

        actualIndentation = await GetSmartTokenFormatterIndentationAsync(code.Replace("    ", "\t"), indentationLine: 5, ch: '#', useTabs: true);
        Assert.Equal(0, actualIndentation);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/766159")]
    public async Task TestRegion()
    {
        var code = @"
class C
{
    void M()
    {
#region
    }
}";

        var actualIndentation = await GetSmartTokenFormatterIndentationAsync(code, indentationLine: 5, ch: 'n', useTabs: false);
        Assert.Equal(8, actualIndentation);

        actualIndentation = await GetSmartTokenFormatterIndentationAsync(code.Replace("    ", "\t"), indentationLine: 5, ch: 'n', useTabs: true);
        Assert.Equal(8, actualIndentation);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/766159")]
    public async Task TestEndRegion()
    {
        var code = @"
class C
{
    void M()
    {
        #region
#endregion
    }
}";

        var actualIndentation = await GetSmartTokenFormatterIndentationAsync(code, indentationLine: 5, ch: 'n', useTabs: false);
        Assert.Equal(8, actualIndentation);

        actualIndentation = await GetSmartTokenFormatterIndentationAsync(code.Replace("    ", "\t"), indentationLine: 5, ch: 'n', useTabs: true);
        Assert.Equal(8, actualIndentation);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/777467")]
    public async Task TestSelect()
    {
        var code = @"
using System;
using System.Linq;

class Program
{
    static IEnumerable<int> Goo()
    {
        return from a in new[] { 1, 2, 3 }
                    select
    }
}
";

        var actualIndentation = await GetSmartTokenFormatterIndentationAsync(code, indentationLine: 9, ch: 't', useTabs: false);
        Assert.Equal(15, actualIndentation);

        actualIndentation = await GetSmartTokenFormatterIndentationAsync(code.Replace("    ", "\t"), indentationLine: 9, ch: 't', useTabs: true);
        Assert.Equal(15, actualIndentation);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/777467")]
    public async Task TestWhere()
    {
        var code = @"
using System;
using System.Linq;

class Program
{
    static IEnumerable<int> Goo()
    {
        return from a in new[] { 1, 2, 3 }
                    where
    }
}
";

        var actualIndentation = await GetSmartTokenFormatterIndentationAsync(code, indentationLine: 9, ch: 'e', useTabs: false);
        Assert.Equal(15, actualIndentation);

        actualIndentation = await GetSmartTokenFormatterIndentationAsync(code.Replace("    ", "\t"), indentationLine: 9, ch: 'e', useTabs: true);
        Assert.Equal(15, actualIndentation);
    }

    private static async Task AssertSmartTokenFormatterOpenBraceWithBaseIndentationAsync(string markup, int baseIndentation, int expectedIndentation)
    {
        await AssertSmartTokenFormatterOpenBraceWithBaseIndentationAsync(markup, baseIndentation, expectedIndentation, useTabs: false).ConfigureAwait(false);
        await AssertSmartTokenFormatterOpenBraceWithBaseIndentationAsync(markup.Replace("    ", "\t"), baseIndentation, expectedIndentation, useTabs: true).ConfigureAwait(false);
    }

    private static Task AssertSmartTokenFormatterOpenBraceWithBaseIndentationAsync(string markup, int baseIndentation, int expectedIndentation, bool useTabs)
    {
        MarkupTestFile.GetPositionAndSpan(markup,
            out var code, out var position, out TextSpan span);

        return AssertSmartTokenFormatterOpenBraceAsync(
            code,
            SourceText.From(code).Lines.IndexOf(position),
            expectedIndentation,
            useTabs,
            baseIndentation,
            span);
    }

    private static async Task AssertSmartTokenFormatterOpenBraceAsync(
        string code,
        int indentationLine,
        int expectedSpace,
        int? baseIndentation = null,
        TextSpan span = default)
    {
        await AssertSmartTokenFormatterOpenBraceAsync(code, indentationLine, expectedSpace, useTabs: false, baseIndentation, span).ConfigureAwait(false);
        await AssertSmartTokenFormatterOpenBraceAsync(code.Replace("    ", "\t"), indentationLine, expectedSpace, useTabs: true, baseIndentation, span).ConfigureAwait(false);
    }

    private static async Task AssertSmartTokenFormatterOpenBraceAsync(
        string code,
        int indentationLine,
        int expectedSpace,
        bool useTabs,
        int? baseIndentation,
        TextSpan span)
    {
        var actualIndentation = await GetSmartTokenFormatterIndentationAsync(code, indentationLine, '{', useTabs, baseIndentation, span);
        Assert.Equal(expectedSpace, actualIndentation);
    }

    private static async Task AssertSmartTokenFormatterOpenBraceAsync(
        string expected,
        string code,
        int indentationLine)
    {
        await AssertSmartTokenFormatterOpenBraceAsync(expected, code, indentationLine, useTabs: false).ConfigureAwait(false);
        await AssertSmartTokenFormatterOpenBraceAsync(expected.Replace("    ", "\t"), code.Replace("    ", "\t"), indentationLine, useTabs: true).ConfigureAwait(false);
    }

    private static async Task AssertSmartTokenFormatterOpenBraceAsync(
        string expected,
        string code,
        int indentationLine,
        bool useTabs)
    {
        // create tree service
        using var workspace = EditorTestWorkspace.CreateCSharp(code);

        var buffer = workspace.Documents.First().GetTextBuffer();

        var actual = await TokenFormatAsync(workspace, buffer, indentationLine, '{', useTabs);
        Assert.Equal(expected, actual);
    }

    private static async Task AssertSmartTokenFormatterCloseBraceWithBaseIndentation(string markup, int baseIndentation, int expectedIndentation)
    {
        await AssertSmartTokenFormatterCloseBraceWithBaseIndentation(markup, baseIndentation, expectedIndentation, useTabs: false).ConfigureAwait(false);
        await AssertSmartTokenFormatterCloseBraceWithBaseIndentation(markup.Replace("    ", "\t"), baseIndentation, expectedIndentation, useTabs: true).ConfigureAwait(false);
    }

    private static Task AssertSmartTokenFormatterCloseBraceWithBaseIndentation(string markup, int baseIndentation, int expectedIndentation, bool useTabs)
    {
        MarkupTestFile.GetPositionAndSpan(markup,
            out var code, out var position, out TextSpan span);

        return AssertSmartTokenFormatterCloseBraceAsync(
            code,
            SourceText.From(code).Lines.IndexOf(position),
            expectedIndentation,
            useTabs,
            baseIndentation,
            span);
    }

    private static async Task AssertSmartTokenFormatterCloseBraceAsync(
        string code,
        int indentationLine,
        int expectedSpace,
        int? baseIndentation = null,
        TextSpan span = default)
    {
        await AssertSmartTokenFormatterCloseBraceAsync(code, indentationLine, expectedSpace, useTabs: false, baseIndentation, span).ConfigureAwait(false);
        await AssertSmartTokenFormatterCloseBraceAsync(code.Replace("    ", "\t"), indentationLine, expectedSpace, useTabs: true, baseIndentation, span).ConfigureAwait(false);
    }

    private static async Task AssertSmartTokenFormatterCloseBraceAsync(
        string code,
        int indentationLine,
        int expectedSpace,
        bool useTabs,
        int? baseIndentation,
        TextSpan span)
    {
        var actualIndentation = await GetSmartTokenFormatterIndentationAsync(code, indentationLine, '}', useTabs, baseIndentation, span);
        Assert.Equal(expectedSpace, actualIndentation);
    }

    private static async Task ExpectException_SmartTokenFormatterCloseBraceAsync(
        string code,
        int indentationLine)
    {
        await ExpectException_SmartTokenFormatterCloseBraceAsync(code, indentationLine, useTabs: false).ConfigureAwait(false);
        await ExpectException_SmartTokenFormatterCloseBraceAsync(code.Replace("    ", "\t"), indentationLine, useTabs: true).ConfigureAwait(false);
    }

    private static async Task ExpectException_SmartTokenFormatterCloseBraceAsync(
        string code,
        int indentationLine,
        bool useTabs)
    {
        Assert.NotNull(await Record.ExceptionAsync(() => GetSmartTokenFormatterIndentationAsync(code, indentationLine, '}', useTabs)));
    }
}
