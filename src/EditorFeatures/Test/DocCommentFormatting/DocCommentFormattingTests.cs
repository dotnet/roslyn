// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic.DocumentationComments;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.DocCommentFormatting;

[Trait(Traits.Feature, Traits.Features.DocCommentFormatting)]
public sealed class DocCommentFormattingTests
{
    private readonly CSharpDocumentationCommentFormattingService _csharpService = new();
    private readonly VisualBasicDocumentationCommentFormattingService _vbService = new();

    private void TestFormat(string xmlFragment, string expectedCSharp, string expectedVB)
    {
        var csharpFormattedText = _csharpService.Format(xmlFragment);
        var vbFormattedText = _vbService.Format(xmlFragment);

        Assert.Equal(expectedCSharp, csharpFormattedText);
        Assert.Equal(expectedVB, vbFormattedText);
    }

    private void TestFormat(string xmlFragment, string expected)
        => TestFormat(xmlFragment, expected, expected);

    [Fact]
    public void CTag()
        => TestFormat("Class <c>Point</c> models a point in a two-dimensional plane.", "Class Point models a point in a two-dimensional plane.");

    [Fact]
    public void ExampleAndCodeTags()
        => TestFormat("""
            This method changes the point's location by the given x- and y-offsets.
                        <example>For example:
                        <code>
                        Point p = new Point(3,5);
                        p.Translate(-1,3);
                        </code>
                        results in <c>p</c>'s having the value (2,8).
                        </example>
            """, "This method changes the point's location by the given x- and y-offsets. For example:\r\n\r\n            Point p = new Point(3,5);\r\n            p.Translate(-1,3);\r\n            \r\n\r\nresults in p's having the value (2,8).");

    [Fact]
    public void ListTag()
        => TestFormat("""
            Here is an example of a bulleted list:
                    <list type="bullet">
                    <item>
                    <description>Item 1.</description>
                    </item>
                    <item>
                    <description>Item 2.</description>
                    </item>
                    </list>
            """, "Here is an example of a bulleted list:\r\n\r\n• Item 1.\r\n• Item 2.");

    [Fact]
    public void ParaTag()
        => TestFormat("""
            This is the entry point of the Point class testing program.
                    <para>This program tests each method and operator, and
                    is intended to be run after any non-trivial maintenance has
                    been performed on the Point class.</para>
            """, """
            This is the entry point of the Point class testing program.

            This program tests each method and operator, and is intended to be run after any non-trivial maintenance has been performed on the Point class.
            """);

    [Fact]
    public void TestPermissionTag()
        => TestFormat(@"<permission cref=""System.Security.PermissionSet"">Everyone can access this method.</permission>", @"Everyone can access this method.");

    [Fact]
    public void SeeTag()
        => TestFormat(@"<see cref=""AnotherFunction""/>", @"AnotherFunction");

    [Fact]
    public void SeeAlsoTag()
        => TestFormat(@"<seealso cref=""AnotherFunction""/>", @"AnotherFunction");

    [Fact]
    public void ValueTag()
        => TestFormat(@"<value>Property <c>X</c> represents the point's x-coordinate.</value>", @"Property X represents the point's x-coordinate.");

    [Fact]
    public void TestParamRefTag()
        => TestFormat("""
            This constructor initializes the new Point to 
            (<paramref name="xor"/>,<paramref name="yor"/>).
            """, "This constructor initializes the new Point to (xor,yor).");

    [Fact]
    public void TestTypeParamRefTag()
        => TestFormat(@"This method fetches data and returns a list of  <typeparamref name=""Z""/>.", @"This method fetches data and returns a list of Z.");

    [Fact]
    public void Whitespace1()
        => TestFormat("  This has extra whitespace.  ", "This has extra whitespace.");

    [Fact]
    public void Whitespace2()
        => TestFormat("""
            This has extra
            whitespace.
            """, "This has extra whitespace.");

    [Fact]
    public void Whitespace3()
        => TestFormat("This  has  extra  whitespace.", "This has extra whitespace.");

    [Fact]
    public void Paragraphs1()
        => TestFormat("""
            <para>This is part of a paragraph.</para>
            """, "This is part of a paragraph.");

    [Fact]
    public void Paragraphs2()
        => TestFormat("""
            <para>This is part of a paragraph.</para>
            <para>This is also part of a paragraph.</para>
            """, """
            This is part of a paragraph.

            This is also part of a paragraph.
            """);

    [Fact]
    public void Paragraphs3()
        => TestFormat("""
            This is a summary.
            <para>This is part of a paragraph.</para>
            """, """
            This is a summary.

            This is part of a paragraph.
            """);

    [Fact]
    public void Paragraphs4()
        => TestFormat("""
            <para>This is part of a paragraph.</para> This is part of the summary, too.
            """, """
            This is part of a paragraph.

            This is part of the summary, too.
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32838")]
    public void Paragraphs5()
        => TestFormat("""
            <para>This is part of a<br/>paragraph.</para>
            <para>This is also part of a paragraph.</para>
            """, """
            This is part of a
            paragraph.

            This is also part of a paragraph.
            """);

    [Theory]
    [InlineData("<br/><br/>")]
    [InlineData("<br/><br/><br/>")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/32838")]
    public void Paragraphs6(string lineBreak)
        => TestFormat($"""
            <para>This is part of a{lineBreak}paragraph.</para>
            <para>This is also part of a paragraph.</para>
            """, """
            This is part of a

            paragraph.

            This is also part of a paragraph.
            """);

    [Fact]
    public void See1()
        => TestFormat(@"See <see cref=""T:System.Object"" />", "See System.Object");

    [Fact]
    public void See2()
        => TestFormat(@"See <see />", @"See");

    [Fact]
    public void See3()
        => TestFormat(@"See <see langword=""true"" />", "See true");

    [Fact]
    public void See4()
        => TestFormat(@"See <see href=""https://github.com"" />", "See https://github.com");

    [Fact]
    public void See5()
        => TestFormat(@"See <see href=""https://github.com"">GitHub</see>", "See GitHub");

    [Fact]
    public void See6()
        => TestFormat(@"See <see href=""https://github.com""></see>", "See https://github.com");

    [Fact]
    public void SeeAlso1()
        => TestFormat(@"See also <seealso cref=""T:System.Object"" />", @"See also System.Object");

    [Fact]
    public void SeeAlso2()
        => TestFormat(@"See also <seealso />", @"See also");

    [Fact]
    public void SeeAlso3()
        => TestFormat(@"See also <seealso langword=""true"" />", "See also true");

    [Fact]
    public void SeeAlso4()
        => TestFormat(@"See also <seealso href=""https://github.com"" />", "See also https://github.com");

    [Fact]
    public void SeeAlso5()
        => TestFormat(@"See also <seealso href=""https://github.com"">GitHub</seealso>", "See also GitHub");

    [Fact]
    public void SeeAlso6()
        => TestFormat(@"See also <seealso href=""https://github.com""></seealso>", "See also https://github.com");
}
