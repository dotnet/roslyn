// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.VisualBasic.DocumentationComments;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.MetadataAsSource
{
    public class DocCommentFormatterTests
    {
        private CSharpDocumentationCommentFormattingService _csharpService = new CSharpDocumentationCommentFormattingService();
        private VisualBasicDocumentationCommentFormattingService _vbService = new VisualBasicDocumentationCommentFormattingService();

        private void TestFormat(string docCommentXmlFragment, string expected)
        {
            TestFormat(docCommentXmlFragment, expected, expected);
        }

        private void TestFormat(string docCommentXmlFragment, string expectedCSharp, string expectedVB)
        {
            var docComment = DocumentationComment.FromXmlFragment(docCommentXmlFragment);

            var csharpFormattedComment = string.Join("\r\n", AbstractMetadataAsSourceService.DocCommentFormatter.Format(_csharpService, docComment));
            var vbFormattedComment = string.Join("\r\n", AbstractMetadataAsSourceService.DocCommentFormatter.Format(_vbService, docComment));

            Assert.Equal(expectedCSharp, csharpFormattedComment);
            Assert.Equal(expectedVB, vbFormattedComment);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void Summary()
        {
            var comment = "<summary>This is a summary.</summary>";

            var expected =
$@"{FeaturesResources.Summary}
    This is a summary.";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void Wrapping1()
        {
            var comment = "<summary>I am the very model of a modern major general. This is a very long comment. And getting longer by the minute.</summary>";

            var expected =
$@"{FeaturesResources.Summary}
    I am the very model of a modern major general. This is a very long comment. And
    getting longer by the minute.";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void Wrapping2()
        {
            var comment = "<summary>I amtheverymodelofamodernmajorgeneral.Thisisaverylongcomment.Andgettinglongerbythe minute.</summary>";
            var expected =
$@"{FeaturesResources.Summary}
    I amtheverymodelofamodernmajorgeneral.Thisisaverylongcomment.Andgettinglongerbythe
    minute.";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void Exception()
        {
            var comment = @"<exception cref=""T:System.NotImplementedException"">throws NotImplementedException</exception>";

            var expected =
$@"{FeaturesResources.Exceptions}
  T:System.NotImplementedException:
    throws NotImplementedException";

            TestFormat(comment, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void MultipleExceptionTags()
        {
            var comment =
@"<exception cref=""T:System.NotImplementedException"">throws NotImplementedException</exception>
<exception cref=""T:System.InvalidOperationException"">throws InvalidOperationException</exception>";

            var expected =
$@"{FeaturesResources.Exceptions}
  T:System.NotImplementedException:
    throws NotImplementedException

  T:System.InvalidOperationException:
    throws InvalidOperationException";

            TestFormat(comment, expected);
        }

        [Fact, WorkItem(530760)]
        [Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void MultipleExceptionTagsWithSameType()
        {
            var comment =
@"<exception cref=""T:System.NotImplementedException"">throws NotImplementedException for reason X</exception>
<exception cref=""T:System.InvalidOperationException"">throws InvalidOperationException</exception>
<exception cref=""T:System.NotImplementedException"">also throws NotImplementedException for reason Y</exception>";

            var expected =
$@"{FeaturesResources.Exceptions}
  T:System.NotImplementedException:
    throws NotImplementedException for reason X

  T:System.NotImplementedException:
    also throws NotImplementedException for reason Y

  T:System.InvalidOperationException:
    throws InvalidOperationException";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void Returns()
        {
            var comment = @"<returns>A string is returned</returns>";

            var expected =
$@"{FeaturesResources.Returns}
    A string is returned";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void SummaryAndParams()
        {
            var comment =
@"<summary>This is the summary.</summary>
<param name=""a"">The param named 'a'</param>
<param name=""b"">The param named 'b'</param>";

            var expected =
$@"{FeaturesResources.Summary}
    This is the summary.

{FeaturesResources.Parameters}
  a:
    The param named 'a'

  b:
    The param named 'b'";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TypeParameters()
        {
            var comment =
@"<typeparam name=""T"">The type param named 'T'</typeparam>
<typeparam name=""U"">The type param named 'U'</typeparam>";

            var expected =
$@"{FeaturesResources.TypeParameters}
  T:
    The type param named 'T'

  U:
    The type param named 'U'";

            TestFormat(comment, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void FormatEverything()
        {
            var comment =
@"<summary>
This is a summary of something.
</summary>
<param name=""a"">The param named 'a'.</param>
<param name=""b""></param>
<param name=""c"">The param named 'c'.</param>
<typeparam name=""T"">A type parameter.</typeparam>
<typeparam name=""U""></typeparam>
<typeparam name=""V"">Another type parameter.</typeparam>
<returns>This returns nothing.</returns>
<exception cref=""System.FooException"">Thrown for an unknown reason</exception>
<exception cref=""System.BarException""></exception>
<exception cref=""System.BlahException"">Thrown when blah blah blah</exception>
<remarks>This doc comment is really not very remarkable.</remarks>";

            var expected =
$@"{FeaturesResources.Summary}
    This is a summary of something.

{FeaturesResources.Parameters}
  a:
    The param named 'a'.

  b:

  c:
    The param named 'c'.

{FeaturesResources.TypeParameters}
  T:
    A type parameter.

  U:

  V:
    Another type parameter.

{FeaturesResources.Returns}
    This returns nothing.

{FeaturesResources.Exceptions}
  System.FooException:
    Thrown for an unknown reason

  System.BarException:

  System.BlahException:
    Thrown when blah blah blah

{FeaturesResources.Remarks}
    This doc comment is really not very remarkable.";

            TestFormat(comment, expected);
        }
    }
}
