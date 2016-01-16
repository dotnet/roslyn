// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting.Indentation
{
    public class SmartTokenFormatterFormatTokenTests : FormatterTestsBase
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
        int Foo
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
        int Foo
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
        event EventHandler Foo
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
        event EventHandler Foo
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
        [WorkItem(537827)]
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
        [WorkItem(543142)]
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
        public void CloseBraceWithBaseIndentation()
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
            AssertSmartTokenFormatterCloseBraceWithBaseIndentation(markup, baseIndentation: 7, expectedIndentation: 11);
        }

        [WorkItem(766159)]
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
            var actualIndentation = await GetSmartTokenFormatterIndentationAsync(code, indentationLine: 5, ch: '#');
            Assert.Equal(0, actualIndentation);
        }

        [WorkItem(766159)]
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
            var actualIndentation = await GetSmartTokenFormatterIndentationAsync(code, indentationLine: 5, ch: 'n');
            Assert.Equal(8, actualIndentation);
        }

        [WorkItem(766159)]
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
            var actualIndentation = await GetSmartTokenFormatterIndentationAsync(code, indentationLine: 5, ch: 'n');

            Assert.Equal(8, actualIndentation);
        }

        [WorkItem(777467)]
        [Fact, Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task TestSelect()
        {
            var code = @"
using System;
using System.Linq;

class Program
{
    static IEnumerable<int> Foo()
    {
        return from a in new[] { 1, 2, 3 }
                    select
    }
}
";
            var actualIndentation = await GetSmartTokenFormatterIndentationAsync(code, indentationLine: 9, ch: 't');

            Assert.Equal(15, actualIndentation);
        }

        [WorkItem(777467)]
        [Fact, Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public async Task TestWhere()
        {
            var code = @"
using System;
using System.Linq;

class Program
{
    static IEnumerable<int> Foo()
    {
        return from a in new[] { 1, 2, 3 }
                    where
    }
}
";
            var actualIndentation = await GetSmartTokenFormatterIndentationAsync(code, indentationLine: 9, ch: 'e');

            Assert.Equal(15, actualIndentation);
        }

        private Task AssertSmartTokenFormatterOpenBraceWithBaseIndentationAsync(string markup, int baseIndentation, int expectedIndentation)
        {
            string code;
            int position;
            TextSpan span;
            MarkupTestFile.GetPositionAndSpan(markup, out code, out position, out span);

            return AssertSmartTokenFormatterOpenBraceAsync(
                code,
                SourceText.From(code).Lines.IndexOf(position),
                expectedIndentation,
                baseIndentation,
                span);
        }

        private async Task AssertSmartTokenFormatterOpenBraceAsync(
            string code,
            int indentationLine,
            int expectedSpace,
            int? baseIndentation = null,
            TextSpan span = default(TextSpan))
        {
            var actualIndentation = await GetSmartTokenFormatterIndentationAsync(code, indentationLine, '{', baseIndentation, span);
            Assert.Equal(expectedSpace, actualIndentation);
        }

        private async Task AssertSmartTokenFormatterOpenBraceAsync(
            string expected,
            string code,
            int indentationLine)
        {
            // create tree service
            using (var workspace = await TestWorkspaceFactory.CreateCSharpWorkspaceFromFileAsync(code))
            {
                var buffer = workspace.Documents.First().GetTextBuffer();

                var actual = await TokenFormatAsync(workspace, buffer, indentationLine, '{');
                Assert.Equal(expected, actual);
            }
        }

        private Task AssertSmartTokenFormatterCloseBraceWithBaseIndentation(string markup, int baseIndentation, int expectedIndentation)
        {
            string code;
            int position;
            TextSpan span;
            MarkupTestFile.GetPositionAndSpan(markup, out code, out position, out span);

            return AssertSmartTokenFormatterCloseBraceAsync(
                code,
                SourceText.From(code).Lines.IndexOf(position),
                expectedIndentation,
                baseIndentation,
                span);
        }

        private async Task AssertSmartTokenFormatterCloseBraceAsync(
            string code,
            int indentationLine,
            int expectedSpace,
            int? baseIndentation = null,
            TextSpan span = default(TextSpan))
        {
            var actualIndentation = await GetSmartTokenFormatterIndentationAsync(code, indentationLine, '}', baseIndentation, span);
            Assert.Equal(expectedSpace, actualIndentation);
        }

        private async Task ExpectException_SmartTokenFormatterOpenBraceAsync(
            string code,
            int indentationLine,
            int expectedSpace)
        {
            Assert.NotNull(await Record.ExceptionAsync(async () => await GetSmartTokenFormatterIndentationAsync(code, indentationLine, '{')));
        }

        private async Task ExpectException_SmartTokenFormatterCloseBraceAsync(
            string code,
            int indentationLine,
            int expectedSpace)
        {
            Assert.NotNull(await Record.ExceptionAsync(async () => await GetSmartTokenFormatterIndentationAsync(code, indentationLine, '}')));
        }
    }
}
