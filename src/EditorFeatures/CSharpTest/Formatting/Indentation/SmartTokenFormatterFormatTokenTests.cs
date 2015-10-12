// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition.Hosting;
using System.Linq;
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
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void EmptyFile1()
        {
            var code = @"{";

            ExpectException_SmartTokenFormatterOpenBrace(
                code,
                indentationLine: 0,
                expectedSpace: 0);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void EmptyFile2()
        {
            var code = @"}";

            ExpectException_SmartTokenFormatterCloseBrace(
                code,
                indentationLine: 0,
                expectedSpace: 0);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Namespace1()
        {
            var code = @"namespace NS
{";

            AssertSmartTokenFormatterOpenBrace(
                code,
                indentationLine: 1,
                expectedSpace: 0);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Namespace2()
        {
            var code = @"namespace NS
}";

            AssertSmartTokenFormatterCloseBrace(
                code,
                indentationLine: 1,
                expectedSpace: 0);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Namespace3()
        {
            var code = @"namespace NS
{
    }";

            AssertSmartTokenFormatterCloseBrace(
                code,
                indentationLine: 2,
                expectedSpace: 0);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Class1()
        {
            var code = @"namespace NS
{
    class Class
    {";

            AssertSmartTokenFormatterOpenBrace(
                code,
                indentationLine: 3,
                expectedSpace: 4);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Class2()
        {
            var code = @"namespace NS
{
    class Class
    }";

            AssertSmartTokenFormatterCloseBrace(
                code,
                indentationLine: 3,
                expectedSpace: 4);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Class3()
        {
            var code = @"namespace NS
{
    class Class
    {
        }";

            AssertSmartTokenFormatterCloseBrace(
                code,
                indentationLine: 4,
                expectedSpace: 4);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Method1()
        {
            var code = @"namespace NS
{
    class Class
    {
        void Method(int i)
        {";

            AssertSmartTokenFormatterOpenBrace(
                code,
                indentationLine: 5,
                expectedSpace: 8);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Method2()
        {
            var code = @"namespace NS
{
    class Class
    {
        void Method(int i)
        }";

            AssertSmartTokenFormatterCloseBrace(
                code,
                indentationLine: 5,
                expectedSpace: 8);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Method3()
        {
            var code = @"namespace NS
{
    class Class
    {
        void Method(int i)
        {
            }";

            AssertSmartTokenFormatterCloseBrace(
                code,
                indentationLine: 6,
                expectedSpace: 8);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Property1()
        {
            var code = @"namespace NS
{
    class Class
    {
        int Foo
            {";

            AssertSmartTokenFormatterOpenBrace(
                code,
                indentationLine: 5,
                expectedSpace: 8);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Property2()
        {
            var code = @"namespace NS
{
    class Class
    {
        int Foo
        {
            }";

            AssertSmartTokenFormatterCloseBrace(
                code,
                indentationLine: 6,
                expectedSpace: 8);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Event1()
        {
            var code = @"namespace NS
{
    class Class
    {
        event EventHandler Foo
            {";

            AssertSmartTokenFormatterOpenBrace(
                code,
                indentationLine: 5,
                expectedSpace: 8);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Event2()
        {
            var code = @"namespace NS
{
    class Class
    {
        event EventHandler Foo
        {
            }";

            AssertSmartTokenFormatterCloseBrace(
                code,
                indentationLine: 6,
                expectedSpace: 8);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Indexer1()
        {
            var code = @"namespace NS
{
    class Class
    {
        int this[int index]
            {";

            AssertSmartTokenFormatterOpenBrace(
                code,
                indentationLine: 5,
                expectedSpace: 8);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Indexer2()
        {
            var code = @"namespace NS
{
    class Class
    {
        int this[int index]
        {
            }";

            AssertSmartTokenFormatterCloseBrace(
                code,
                indentationLine: 6,
                expectedSpace: 8);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Block1()
        {
            var code = @"namespace NS
{
    class Class
    {
        void Method(int i)
        {
        {";

            AssertSmartTokenFormatterOpenBrace(
                code,
                indentationLine: 6,
                expectedSpace: 12);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Block2()
        {
            var code = @"namespace NS
{
    class Class
    {
        void Method(int i)
        }
        }";

            AssertSmartTokenFormatterCloseBrace(
                code,
                indentationLine: 6,
                expectedSpace: 0);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Block3()
        {
            var code = @"namespace NS
{
    class Class
    {
        void Method(int i)
        {
            {
                }";

            AssertSmartTokenFormatterCloseBrace(
                code,
                indentationLine: 7,
                expectedSpace: 12);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void Block4()
        {
            var code = @"namespace NS
{
    class Class
    {
        void Method(int i)
        {
                {
        }";

            AssertSmartTokenFormatterCloseBrace(
                code,
                indentationLine: 7,
                expectedSpace: 12);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void ArrayInitializer1()
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

            AssertSmartTokenFormatterOpenBrace(
                expected,
                code,
                indentationLine: 6);
        }

        [WpfFact]
        [WorkItem(537827)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void ArrayInitializer3()
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

            AssertSmartTokenFormatterCloseBrace(
                code,
                indentationLine: 9,
                expectedSpace: 12);
        }

        [WpfFact]
        [WorkItem(543142)]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void EnterWithTrailingWhitespace()
        {
            var code = @"class Class
{
    void Method(int i)
    {
        var a = new {
 };
";

            AssertSmartTokenFormatterCloseBrace(
                code,
                indentationLine: 5,
                expectedSpace: 8);
        }

        [WorkItem(9216, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void OpenBraceWithBaseIndentation()
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
            AssertSmartTokenFormatterOpenBraceWithBaseIndentation(markup, baseIndentation: 7, expectedIndentation: 11);
        }

        [WorkItem(9216, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void TestPreprocessor()
        {
            var code = @"
class C
{
    void M()
    {
        #
    }
}";
            var actualIndentation = GetSmartTokenFormatterIndentation(code, indentationLine: 5, ch: '#');
            Assert.Equal(0, actualIndentation);
        }

        [WorkItem(766159)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void TestRegion()
        {
            var code = @"
class C
{
    void M()
    {
#region
    }
}";
            var actualIndentation = GetSmartTokenFormatterIndentation(code, indentationLine: 5, ch: 'n');
            Assert.Equal(8, actualIndentation);
        }

        [WorkItem(766159)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void TestEndRegion()
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
            var actualIndentation = GetSmartTokenFormatterIndentation(code, indentationLine: 5, ch: 'n');

            Assert.Equal(8, actualIndentation);
        }

        [WorkItem(777467)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void TestSelect()
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
            var actualIndentation = GetSmartTokenFormatterIndentation(code, indentationLine: 9, ch: 't');

            Assert.Equal(15, actualIndentation);
        }

        [WorkItem(777467)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void TestWhere()
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
            var actualIndentation = GetSmartTokenFormatterIndentation(code, indentationLine: 9, ch: 'e');

            Assert.Equal(15, actualIndentation);
        }

        private void AssertSmartTokenFormatterOpenBraceWithBaseIndentation(string markup, int baseIndentation, int expectedIndentation)
        {
            string code;
            int position;
            TextSpan span;
            MarkupTestFile.GetPositionAndSpan(markup, out code, out position, out span);

            AssertSmartTokenFormatterOpenBrace(
                code,
                SourceText.From(code).Lines.IndexOf(position),
                expectedIndentation,
                baseIndentation,
                span);
        }

        private void AssertSmartTokenFormatterOpenBrace(
            string code,
            int indentationLine,
            int expectedSpace,
            int? baseIndentation = null,
            TextSpan span = default(TextSpan))
        {
            var actualIndentation = GetSmartTokenFormatterIndentation(code, indentationLine, '{', baseIndentation, span);
            Assert.Equal(expectedSpace, actualIndentation);
        }

        private void AssertSmartTokenFormatterOpenBrace(
            string expected,
            string code,
            int indentationLine)
        {
            // create tree service
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(code))
            {
                var buffer = workspace.Documents.First().GetTextBuffer();

                var actual = TokenFormat(workspace, buffer, indentationLine, '{');
                Assert.Equal(expected, actual);
            }
        }

        private void AssertSmartTokenFormatterCloseBraceWithBaseIndentation(string markup, int baseIndentation, int expectedIndentation)
        {
            string code;
            int position;
            TextSpan span;
            MarkupTestFile.GetPositionAndSpan(markup, out code, out position, out span);

            AssertSmartTokenFormatterCloseBrace(
                code,
                SourceText.From(code).Lines.IndexOf(position),
                expectedIndentation,
                baseIndentation,
                span);
        }

        private void AssertSmartTokenFormatterCloseBrace(
            string code,
            int indentationLine,
            int expectedSpace,
            int? baseIndentation = null,
            TextSpan span = default(TextSpan))
        {
            var actualIndentation = GetSmartTokenFormatterIndentation(code, indentationLine, '}', baseIndentation, span);
            Assert.Equal(expectedSpace, actualIndentation);
        }

        private void ExpectException_SmartTokenFormatterOpenBrace(
            string code,
            int indentationLine,
            int expectedSpace)
        {
            Assert.NotNull(Record.Exception(() => GetSmartTokenFormatterIndentation(code, indentationLine, '{')));
        }

        private void ExpectException_SmartTokenFormatterCloseBrace(
            string code,
            int indentationLine,
            int expectedSpace)
        {
            Assert.NotNull(Record.Exception(() => GetSmartTokenFormatterIndentation(code, indentationLine, '}')));
        }
    }
}
