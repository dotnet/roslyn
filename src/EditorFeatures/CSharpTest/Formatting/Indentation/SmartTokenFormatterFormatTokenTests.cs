// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting.Indentation
{
    public class SmartTokenFormatterFormatTokenTests : CSharpFormatterTestsBase
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task EmptyFile1()
        {
            var code = @"{";

            await ExpectException_SmartTokenFormatterOpenBraceAsync(
                code,
                indentationLine: 0,
                expectedSpace: 0);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task EmptyFile2()
        {
            var code = @"}";

            await ExpectException_SmartTokenFormatterCloseBraceAsync(
                code,
                indentationLine: 0,
                expectedSpace: 0);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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

        [Fact]
        [WorkItem(537827, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537827")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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

        [Fact]
        [WorkItem(543142, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543142")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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

        [WorkItem(9216, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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

        [WorkItem(9216, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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

        [WorkItem(766159, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/766159")]
        [Fact, Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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

        [WorkItem(766159, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/766159")]
        [Fact, Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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

        [WorkItem(766159, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/766159")]
        [Fact, Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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

        [WorkItem(777467, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/777467")]
        [Fact, Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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

        [WorkItem(777467, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/777467")]
        [Fact, Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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

        private async Task AssertSmartTokenFormatterOpenBraceWithBaseIndentationAsync(string markup, int baseIndentation, int expectedIndentation)
        {
            await AssertSmartTokenFormatterOpenBraceWithBaseIndentationAsync(markup, baseIndentation, expectedIndentation, useTabs: false).ConfigureAwait(false);
            await AssertSmartTokenFormatterOpenBraceWithBaseIndentationAsync(markup.Replace("    ", "\t"), baseIndentation, expectedIndentation, useTabs: true).ConfigureAwait(false);
        }

        private Task AssertSmartTokenFormatterOpenBraceWithBaseIndentationAsync(string markup, int baseIndentation, int expectedIndentation, bool useTabs)
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

        private async Task AssertSmartTokenFormatterOpenBraceAsync(
            string code,
            int indentationLine,
            int expectedSpace,
            int? baseIndentation = null,
            TextSpan span = default)
        {
            await AssertSmartTokenFormatterOpenBraceAsync(code, indentationLine, expectedSpace, useTabs: false, baseIndentation, span).ConfigureAwait(false);
            await AssertSmartTokenFormatterOpenBraceAsync(code.Replace("    ", "\t"), indentationLine, expectedSpace, useTabs: true, baseIndentation, span).ConfigureAwait(false);
        }

        private async Task AssertSmartTokenFormatterOpenBraceAsync(
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

        private async Task AssertSmartTokenFormatterOpenBraceAsync(
            string expected,
            string code,
            int indentationLine)
        {
            await AssertSmartTokenFormatterOpenBraceAsync(expected, code, indentationLine, useTabs: false).ConfigureAwait(false);
            await AssertSmartTokenFormatterOpenBraceAsync(expected.Replace("    ", "\t"), code.Replace("    ", "\t"), indentationLine, useTabs: true).ConfigureAwait(false);
        }

        private async Task AssertSmartTokenFormatterOpenBraceAsync(
            string expected,
            string code,
            int indentationLine,
            bool useTabs)
        {
            // create tree service
            using (var workspace = TestWorkspace.CreateCSharp(code))
            {
                workspace.Options = workspace.Options.WithChangedOption(FormattingOptions.UseTabs, LanguageNames.CSharp, useTabs);

                var buffer = workspace.Documents.First().GetTextBuffer();

                var actual = await TokenFormatAsync(workspace, buffer, indentationLine, '{');
                Assert.Equal(expected, actual);
            }
        }

        private async Task AssertSmartTokenFormatterCloseBraceWithBaseIndentation(string markup, int baseIndentation, int expectedIndentation)
        {
            await AssertSmartTokenFormatterCloseBraceWithBaseIndentation(markup, baseIndentation, expectedIndentation, useTabs: false).ConfigureAwait(false);
            await AssertSmartTokenFormatterCloseBraceWithBaseIndentation(markup.Replace("    ", "\t"), baseIndentation, expectedIndentation, useTabs: true).ConfigureAwait(false);
        }

        private Task AssertSmartTokenFormatterCloseBraceWithBaseIndentation(string markup, int baseIndentation, int expectedIndentation, bool useTabs)
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

        private async Task AssertSmartTokenFormatterCloseBraceAsync(
            string code,
            int indentationLine,
            int expectedSpace,
            int? baseIndentation = null,
            TextSpan span = default)
        {
            await AssertSmartTokenFormatterCloseBraceAsync(code, indentationLine, expectedSpace, useTabs: false, baseIndentation, span).ConfigureAwait(false);
            await AssertSmartTokenFormatterCloseBraceAsync(code.Replace("    ", "\t"), indentationLine, expectedSpace, useTabs: true, baseIndentation, span).ConfigureAwait(false);
        }

        private async Task AssertSmartTokenFormatterCloseBraceAsync(
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

        private async Task ExpectException_SmartTokenFormatterOpenBraceAsync(
            string code,
            int indentationLine,
            int expectedSpace)
        {
            await ExpectException_SmartTokenFormatterOpenBraceAsync(code, indentationLine, expectedSpace, useTabs: false).ConfigureAwait(false);
            await ExpectException_SmartTokenFormatterOpenBraceAsync(code.Replace("    ", "\t"), indentationLine, expectedSpace, useTabs: true).ConfigureAwait(false);
        }

        private async Task ExpectException_SmartTokenFormatterOpenBraceAsync(
            string code,
            int indentationLine,
            int expectedSpace,
            bool useTabs)
        {
            Assert.NotNull(await Record.ExceptionAsync(() => GetSmartTokenFormatterIndentationAsync(code, indentationLine, '{', useTabs)));
        }

        private async Task ExpectException_SmartTokenFormatterCloseBraceAsync(
            string code,
            int indentationLine,
            int expectedSpace)
        {
            await ExpectException_SmartTokenFormatterCloseBraceAsync(code, indentationLine, expectedSpace, useTabs: false).ConfigureAwait(false);
            await ExpectException_SmartTokenFormatterCloseBraceAsync(code.Replace("    ", "\t"), indentationLine, expectedSpace, useTabs: true).ConfigureAwait(false);
        }

        private async Task ExpectException_SmartTokenFormatterCloseBraceAsync(
            string code,
            int indentationLine,
            int expectedSpace,
            bool useTabs)
        {
            Assert.NotNull(await Record.ExceptionAsync(() => GetSmartTokenFormatterIndentationAsync(code, indentationLine, '}', useTabs)));
        }
    }
}
