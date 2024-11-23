// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic.DocumentationComments;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.MetadataAsSource;

[Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
public class DocCommentFormatterTests
{
    private readonly CSharpDocumentationCommentFormattingService _csharpService = new CSharpDocumentationCommentFormattingService();
    private readonly VisualBasicDocumentationCommentFormattingService _vbService = new VisualBasicDocumentationCommentFormattingService();

    private void TestFormat(string docCommentXmlFragment, string expected)
        => TestFormat(docCommentXmlFragment, expected, expected);

    private void TestFormat(string docCommentXmlFragment, string expectedCSharp, string expectedVB)
    {
        var docComment = DocumentationComment.FromXmlFragment(docCommentXmlFragment);

        var csharpFormattedComment = string.Join("\r\n", AbstractMetadataAsSourceService.DocCommentFormatter.Format(_csharpService, docComment));
        var vbFormattedComment = string.Join("\r\n", AbstractMetadataAsSourceService.DocCommentFormatter.Format(_vbService, docComment));

        Assert.Equal(expectedCSharp, csharpFormattedComment);
        Assert.Equal(expectedVB, vbFormattedComment);
    }

    [Fact]
    public void Summary()
    {
        var comment = "<summary>This is a summary.</summary>";

        var expected =
$@"{FeaturesResources.Summary_colon}
    This is a summary.";

        TestFormat(comment, expected);
    }

    [Fact]
    public void Wrapping1()
    {
        var comment = "<summary>I am the very model of a modern major general. This is a very long comment. And getting longer by the minute.</summary>";

        var expected =
$@"{FeaturesResources.Summary_colon}
    I am the very model of a modern major general. This is a very long comment. And
    getting longer by the minute.";

        TestFormat(comment, expected);
    }

    [Fact]
    public void Wrapping2()
    {
        var comment = "<summary>I amtheverymodelofamodernmajorgeneral.Thisisaverylongcomment.Andgettinglongerbythe minute.</summary>";
        var expected =
$@"{FeaturesResources.Summary_colon}
    I amtheverymodelofamodernmajorgeneral.Thisisaverylongcomment.Andgettinglongerbythe
    minute.";

        TestFormat(comment, expected);
    }

    [Fact]
    public void Exception()
    {
        var comment = @"<exception cref=""T:System.NotImplementedException"">throws NotImplementedException</exception>";

        var expected =
$@"{FeaturesResources.Exceptions_colon}
  T:System.NotImplementedException:
    throws NotImplementedException";

        TestFormat(comment, expected);
    }

    [Fact]
    public void MultipleExceptionTags()
    {
        var comment =
@"<exception cref=""T:System.NotImplementedException"">throws NotImplementedException</exception>
<exception cref=""T:System.InvalidOperationException"">throws InvalidOperationException</exception>";

        var expected =
$@"{FeaturesResources.Exceptions_colon}
  T:System.NotImplementedException:
    throws NotImplementedException

  T:System.InvalidOperationException:
    throws InvalidOperationException";

        TestFormat(comment, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530760")]
    public void MultipleExceptionTagsWithSameType()
    {
        var comment =
@"<exception cref=""T:System.NotImplementedException"">throws NotImplementedException for reason X</exception>
<exception cref=""T:System.InvalidOperationException"">throws InvalidOperationException</exception>
<exception cref=""T:System.NotImplementedException"">also throws NotImplementedException for reason Y</exception>";

        var expected =
$@"{FeaturesResources.Exceptions_colon}
  T:System.NotImplementedException:
    throws NotImplementedException for reason X

  T:System.NotImplementedException:
    also throws NotImplementedException for reason Y

  T:System.InvalidOperationException:
    throws InvalidOperationException";

        TestFormat(comment, expected);
    }

    [Fact]
    public void Returns()
    {
        var comment = @"<returns>A string is returned</returns>";

        var expected =
$@"{FeaturesResources.Returns_colon}
    A string is returned";

        TestFormat(comment, expected);
    }

    [Fact]
    public void Value()
    {
        var comment = @"<value>A string value</value>";

        var expected =
$@"{FeaturesResources.Value_colon}
    A string value";

        TestFormat(comment, expected);
    }

    [Fact]
    public void SummaryAndParams()
    {
        var comment =
@"<summary>This is the summary.</summary>
<param name=""a"">The param named 'a'</param>
<param name=""b"">The param named 'b'</param>";

        var expected =
$@"{FeaturesResources.Summary_colon}
    This is the summary.

{FeaturesResources.Parameters_colon}
  a:
    The param named 'a'

  b:
    The param named 'b'";

        TestFormat(comment, expected);
    }

    [Fact]
    public void TypeParameters()
    {
        var comment =
@"<typeparam name=""T"">The type param named 'T'</typeparam>
<typeparam name=""U"">The type param named 'U'</typeparam>";

        var expected =
$@"{FeaturesResources.Type_parameters_colon}
  T:
    The type param named 'T'

  U:
    The type param named 'U'";

        TestFormat(comment, expected);
    }

    [Fact]
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
<value>This has no value.</value>
<exception cref=""System.GooException"">Thrown for an unknown reason</exception>
<exception cref=""System.BarException""></exception>
<exception cref=""System.BlahException"">Thrown when blah blah blah</exception>
<remarks>This doc comment is really not very remarkable.</remarks>";

        var expected =
$@"{FeaturesResources.Summary_colon}
    This is a summary of something.

{FeaturesResources.Parameters_colon}
  a:
    The param named 'a'.

  b:

  c:
    The param named 'c'.

{FeaturesResources.Type_parameters_colon}
  T:
    A type parameter.

  U:

  V:
    Another type parameter.

{FeaturesResources.Returns_colon}
    This returns nothing.

{FeaturesResources.Value_colon}
    This has no value.

{FeaturesResources.Exceptions_colon}
  System.GooException:
    Thrown for an unknown reason

  System.BarException:

  System.BlahException:
    Thrown when blah blah blah

{FeaturesResources.Remarks_colon}
    This doc comment is really not very remarkable.";

        TestFormat(comment, expected);
    }
}
