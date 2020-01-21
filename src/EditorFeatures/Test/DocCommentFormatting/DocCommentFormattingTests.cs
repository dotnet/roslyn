// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic.DocumentationComments;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.DocCommentFormatting
{
    public class DocCommentFormattingTests
    {
        private readonly CSharpDocumentationCommentFormattingService _csharpService = new CSharpDocumentationCommentFormattingService();
        private readonly VisualBasicDocumentationCommentFormattingService _vbService = new VisualBasicDocumentationCommentFormattingService();

        private void TestFormat(string xmlFragment, string expectedCSharp, string expectedVB)
        {
            var csharpFormattedText = _csharpService.Format(xmlFragment);
            var vbFormattedText = _vbService.Format(xmlFragment);

            Assert.Equal(expectedCSharp, csharpFormattedText);
            Assert.Equal(expectedVB, vbFormattedText);
        }

        private void TestFormat(string xmlFragment, string expected)
        {
            TestFormat(xmlFragment, expected, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void CTag()
        {
            var comment = "Class <c>Point</c> models a point in a two-dimensional plane.";
            var expected = "Class Point models a point in a two-dimensional plane.";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void ExampleAndCodeTags()
        {
            var comment = @"This method changes the point's location by the given x- and y-offsets.
            <example>For example:
            <code>
            Point p = new Point(3,5);
            p.Translate(-1,3);
            </code>
            results in <c>p</c>'s having the value (2,8).
            </example>";

            var expected = "This method changes the point's location by the given x- and y-offsets. For example:\r\n\r\nPoint p = new Point(3,5); p.Translate(-1,3);\r\n\r\nresults in p's having the value (2,8).";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void ListTag()
        {
            var comment = @"Here is an example of a bulleted list:
        <list type=""bullet"">
        <item>
        <description>Item 1.</description>
        </item>
        <item>
        <description>Item 2.</description>
        </item>
        </list>";

            var expected = "Here is an example of a bulleted list:\r\n\r\n• Item 1.\r\n• Item 2.";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void ParaTag()
        {
            var comment = @"This is the entry point of the Point class testing program.
        <para>This program tests each method and operator, and
        is intended to be run after any non-trivial maintenance has
        been performed on the Point class.</para>";

            var expected =
@"This is the entry point of the Point class testing program.

This program tests each method and operator, and is intended to be run after any non-trivial maintenance has been performed on the Point class.";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void TestPermissionTag()
        {
            var comment = @"<permission cref=""System.Security.PermissionSet"">Everyone can access this method.</permission>";

            var expected = @"Everyone can access this method.";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void SeeTag()
        {
            var comment = @"<see cref=""AnotherFunction""/>";

            var expected = @"AnotherFunction";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void SeeAlsoTag()
        {
            var comment = @"<seealso cref=""AnotherFunction""/>";

            var expected = @"AnotherFunction";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void ValueTag()
        {
            var comment = @"<value>Property <c>X</c> represents the point's x-coordinate.</value>";

            var expected = @"Property X represents the point's x-coordinate.";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void TestParamRefTag()
        {
            var comment =
@"This constructor initializes the new Point to 
(<paramref name=""xor""/>,<paramref name=""yor""/>).";

            var expected = "This constructor initializes the new Point to (xor,yor).";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void TestTypeParamRefTag()
        {
            var comment = @"This method fetches data and returns a list of  <typeparamref name=""Z""/>.";

            var expected = @"This method fetches data and returns a list of Z.";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void Whitespace1()
        {
            var comment = "  This has extra whitespace.  ";

            var expected = "This has extra whitespace.";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void Whitespace2()
        {
            var comment =
@"
This has extra
whitespace.
";

            var expected = "This has extra whitespace.";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void Whitespace3()
        {
            var comment = "This  has  extra  whitespace.";
            var expected = "This has extra whitespace.";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void Paragraphs1()
        {
            var comment =
@"
<para>This is part of a paragraph.</para>
";
            var expected = "This is part of a paragraph.";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void Paragraphs2()
        {
            var comment =
@"
<para>This is part of a paragraph.</para>
<para>This is also part of a paragraph.</para>
";

            var expected =
@"This is part of a paragraph.

This is also part of a paragraph.";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void Paragraphs3()
        {
            var comment =
@"
This is a summary.
<para>This is part of a paragraph.</para>
";

            var expected =
@"This is a summary.

This is part of a paragraph.";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void Paragraphs4()
        {
            var comment =
@"
<para>This is part of a paragraph.</para> This is part of the summary, too.
";

            var expected =
@"This is part of a paragraph.

This is part of the summary, too.";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        [WorkItem(32838, "https://github.com/dotnet/roslyn/issues/32838")]
        public void Paragraphs5()
        {
            var comment =
@"
<para>This is part of a<br/>paragraph.</para>
<para>This is also part of a paragraph.</para>
";

            var expected =
@"This is part of a
paragraph.

This is also part of a paragraph.";

            TestFormat(comment, expected);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        [InlineData("<br/><br/>")]
        [InlineData("<br/><br/><br/>")]
        [WorkItem(32838, "https://github.com/dotnet/roslyn/issues/32838")]
        public void Paragraphs6(string lineBreak)
        {
            var comment =
$@"
<para>This is part of a{lineBreak}paragraph.</para>
<para>This is also part of a paragraph.</para>
";

            var expected =
@"This is part of a

paragraph.

This is also part of a paragraph.";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void See1()
        {
            var comment = @"See <see cref=""T:System.Object"" />";

            var expected = "See System.Object";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void See2()
        {
            var comment = @"See <see />";

            var expected = @"See";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void See3()
        {
            var comment = @"See <see langword=""true"" />";

            var expected = "See true";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void See4()
        {
            var comment = @"See <see href=""https://github.com"" />";

            var expected = "See https://github.com";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void See5()
        {
            var comment = @"See <see href=""https://github.com"">GitHub</see>";

            var expected = "See GitHub";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void See6()
        {
            var comment = @"See <see href=""https://github.com""></see>";

            var expected = "See https://github.com";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void SeeAlso1()
        {
            var comment = @"See also <seealso cref=""T:System.Object"" />";

            var expected = @"See also System.Object";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void SeeAlso2()
        {
            var comment = @"See also <seealso />";

            var expected = @"See also";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void SeeAlso3()
        {
            var comment = @"See also <seealso langword=""true"" />";

            var expected = "See also true";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void SeeAlso4()
        {
            var comment = @"See also <seealso href=""https://github.com"" />";

            var expected = "See also https://github.com";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void SeeAlso5()
        {
            var comment = @"See also <seealso href=""https://github.com"">GitHub</seealso>";

            var expected = "See also GitHub";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
        public void SeeAlso6()
        {
            var comment = @"See also <seealso href=""https://github.com""></seealso>";

            var expected = "See also https://github.com";

            TestFormat(comment, expected);
        }
    }
}
