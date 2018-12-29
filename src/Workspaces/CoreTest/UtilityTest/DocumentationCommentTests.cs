﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class DocumentationCommentTests
    {
        [Fact]
        public void ParseEmptyXmlFragment()
        {
            var document = DocumentationComment.FromXmlFragment("");

            Assert.Equal(null, document.ExampleText);
            Assert.Equal(null, document.ReturnsText);
            Assert.Equal(null, document.SummaryText);
        }

        [Fact]
        public void ParseFullTag()
        {
            var comment = DocumentationComment.FromXmlFragment(
                @"<summary>Hello, world!</summary>
                  <returns>42.</returns>
                  <example>goo.Bar();</example>
                  <param name=""goo"">A goo.</param>
                  <typeparam name=""T"">A type.</typeparam>
                  <exception cref=""System.Exception"">An exception</exception>
                  <remarks>A remark</remarks>");

            Assert.Equal("Hello, world!", comment.SummaryText);
            Assert.Equal("42.", comment.ReturnsText);
            Assert.Equal("goo.Bar();", comment.ExampleText);
            Assert.Equal("goo", comment.ParameterNames[0]);
            Assert.Equal("A goo.", comment.GetParameterText("goo"));
            Assert.Equal("T", comment.TypeParameterNames[0]);
            Assert.Equal("A type.", comment.GetTypeParameterText("T"));
            Assert.Equal("System.Exception", comment.ExceptionTypes[0]);
            Assert.Equal("An exception", comment.GetExceptionTexts("System.Exception")[0]);
            Assert.Equal("A remark", comment.RemarksText);
        }

        [Fact]
        public void ParseTagWithMultipleSummaries()
        {
            var comment = DocumentationComment.FromXmlFragment("<summary>Summary 1</summary><summary>Summary 2</summary>");

            Assert.Equal("Summary 1", comment.SummaryText);
        }

        [WorkItem(522741, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/522741")]
        [Fact(Skip = "Bug 522741")]
        public void ParseTagWithMultiLineComments()
        {
            var comment = DocumentationComment.FromXmlFragment(@"<summary>
Summary 1
Summary 2
</summary>");

            Assert.Equal("Summary 1 Summary 2", comment.SummaryText);
        }

        [Fact]
        public void ParseInvalidXML()
        {
            var comment = DocumentationComment.FromXmlFragment("<summary>goo");

            Assert.True(comment.HadXmlParseError);
            Assert.Null(comment.SummaryText);
        }

        [Fact]
        public void PreserveParameterNameOrdering()
        {
            var comment = DocumentationComment.FromXmlFragment(
@"<param name=""z"">Z</param>
<param name=""a"">A</param>
<param name=""b"">B</param>");

            Assert.Equal("z", comment.ParameterNames[0]);
            Assert.Equal("a", comment.ParameterNames[1]);
            Assert.Equal("b", comment.ParameterNames[2]);
        }

        [Fact]
        public void PreserveTypeParameterNameOrdering()
        {
            var comment = DocumentationComment.FromXmlFragment(
@"<typeparam name=""z"">Z</typeparam>
<typeparam name=""a"">A</typeparam>
<typeparam name=""b"">B</typeparam>");

            Assert.Equal("z", comment.TypeParameterNames[0]);
            Assert.Equal("a", comment.TypeParameterNames[1]);
            Assert.Equal("b", comment.TypeParameterNames[2]);
        }

        [Fact]
        public void PreserveExceptionTypeOrdering()
        {
            var comment = DocumentationComment.FromXmlFragment(
@"<exception cref=""z"">Z</exception>
<exception cref=""a"">A</exception>
<exception cref=""b"">B</exception>");

            Assert.Equal("z", comment.ExceptionTypes[0]);
            Assert.Equal("a", comment.ExceptionTypes[1]);
            Assert.Equal("b", comment.ExceptionTypes[2]);
        }

        [Fact, WorkItem(546732, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546732")]
        public void UnknownTag()
        {
            var comment = DocumentationComment.FromXmlFragment(
@"<summary>This is a summary.</summary>
<RandomTag>This is another summary.</RandomTag>
<param name=""a"">The param named 'a'</param>");

            Assert.Equal("This is a summary.", comment.SummaryText);
            Assert.Equal("a", comment.ParameterNames[0]);
            Assert.Equal("The param named 'a'", comment.GetParameterText("a"));
        }

        [Fact, WorkItem(546732, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546732")]
        public void TextOutsideTag()
        {
            var comment = DocumentationComment.FromXmlFragment(
@"<summary>This is a summary.</summary>
This is random top-level text.
<param name=""a"">The param named 'a'</param>");

            Assert.Equal("This is a summary.", comment.SummaryText);
            Assert.Equal("a", comment.ParameterNames[0]);
            Assert.Equal("The param named 'a'", comment.GetParameterText("a"));
        }

        [Fact, WorkItem(546732, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546732")]
        public void SingleTopLevelTag()
        {
            var comment = DocumentationComment.FromXmlFragment(
@"<member>
<summary>This is a summary.</summary>
This is random top-level text.
<param name=""a"">The param named 'a'</param>
</member>");

            Assert.Equal("This is a summary.", comment.SummaryText);
            Assert.Equal("a", comment.ParameterNames[0]);
            Assert.Equal("The param named 'a'", comment.GetParameterText("a"));
        }

        [Fact, WorkItem(530760, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530760")]
        public void MultipleParamsWithSameName()
        {
            var comment = DocumentationComment.FromXmlFragment(
@"<param name=""a"">This comment should be retained.</param>
<param name=""a"">This comment should not be retained.</param>");

            Assert.Equal(1, comment.ParameterNames.Length);
            Assert.Equal("a", comment.ParameterNames[0]);
            Assert.Equal("This comment should be retained.", comment.GetParameterText("a"));
        }

        [Fact, WorkItem(530760, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530760")]
        public void MultipleTypeParamsWithSameName()
        {
            var comment = DocumentationComment.FromXmlFragment(
@"<typeparam name=""a"">This comment should be retained.</typeparam>
<typeparam name=""a"">This comment should not be retained.</typeparam>");

            Assert.Equal(1, comment.TypeParameterNames.Length);
            Assert.Equal("a", comment.TypeParameterNames[0]);
            Assert.Equal("This comment should be retained.", comment.GetTypeParameterText("a"));
        }

        [Fact, WorkItem(530760, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530760")]
        public void MultipleExceptionsWithSameName()
        {
            var comment = DocumentationComment.FromXmlFragment(
@"<exception cref=""A"">First A description</exception>
<exception cref=""B"">First B description</exception>
<exception cref=""A"">Second A description</exception>
<exception cref=""B"">Second B description</exception>");

            Assert.Equal(2, comment.ExceptionTypes.Length);
            Assert.Equal("A", comment.ExceptionTypes[0]);
            Assert.Equal("B", comment.ExceptionTypes[1]);
            Assert.Equal(2, comment.GetExceptionTexts("A").Length);
            Assert.Equal("First A description", comment.GetExceptionTexts("A")[0]);
            Assert.Equal("Second A description", comment.GetExceptionTexts("A")[1]);
            Assert.Equal(2, comment.GetExceptionTexts("B").Length);
            Assert.Equal("First B description", comment.GetExceptionTexts("B")[0]);
            Assert.Equal("Second B description", comment.GetExceptionTexts("B")[1]);
        }

        [Fact, WorkItem(530760, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530760")]
        public void NoExceptionWithGivenName()
        {
            var comment = DocumentationComment.FromXmlFragment(@"<summary>This is a summary</summary>");

            Assert.Equal(0, comment.GetExceptionTexts("A").Length);
        }

        [Fact, WorkItem(531189, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531189")]
        public void NoNameAttribute()
        {
            var comment = DocumentationComment.FromXmlFragment(@"<param/><typeparam/><exception/>");

            Assert.Equal(0, comment.ParameterNames.Length);
            Assert.Equal(0, comment.TypeParameterNames.Length);
            Assert.Equal(0, comment.ExceptionTypes.Length);
        }

        [Fact, WorkItem(612456, "DevDiv2/DevDiv")]
        public void ReservedXmlNamespaceInName()
        {
            string fragment = @"<summary><xmlns:boo /></summary>";

            var comments = DocumentationComment.FromXmlFragment(fragment);

            Assert.Equal(fragment, comments.FullXmlFragment);
            Assert.True(comments.HadXmlParseError);
        }

        [Fact, WorkItem(18901, "https://github.com/dotnet/roslyn/pull/18901")]
        public void TrimEachLine()
        {
            string multiLineText = @"



Hello
     World     .        
+
.......




123

                                           1";

            string fullXml = $@"<summary>{multiLineText}</summary>
                  <returns>{multiLineText}</returns>
                  <example>{multiLineText}</example>
                  <param name=""goo"">{multiLineText}</param>
                  <typeparam name=""T"">{multiLineText}</typeparam>
                  <remarks>{multiLineText}</remarks>";


            string expected = @"Hello
World     .
+
.......
123
1";

            var comment = DocumentationComment.FromXmlFragment(fullXml);

            Assert.Equal(expected, comment.SummaryText);
            Assert.Equal(expected, comment.ReturnsText);
            Assert.Equal(expected, comment.ExampleText);
            Assert.Equal(expected, comment.GetParameterText("goo"));
            Assert.Equal(expected, comment.GetTypeParameterText("T"));
            Assert.Equal(expected, comment.RemarksText);
        }
    }
}
