// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class XmlDocumentationCommentTextReaderTests
    {
        [Fact]
        public void Reader()
        {
            char[] buffer = new char[200];
            int charsRead;

            var s = new XmlDocumentationCommentTextReader.Reader();
            Assert.Equal(0, s.Position);

            s.SetText("abc");

            charsRead = s.Read(buffer, 0, 200);

            Assert.Equal(109, charsRead);
            Assert.Equal(
                XmlDocumentationCommentTextReader.Reader.RootStart +
                XmlDocumentationCommentTextReader.Reader.CurrentStart +
                "abc" +
                XmlDocumentationCommentTextReader.Reader.CurrentEnd, new string(buffer, 0, charsRead));

            charsRead = s.Read(buffer, 0, 10);

            Assert.Equal(1, 1);
            Assert.Equal(" ", new string(buffer, 0, charsRead));

            s.SetText("hello");

            charsRead = s.Read(buffer, 0, 200);

            Assert.Equal(76, charsRead);
            Assert.Equal(
                XmlDocumentationCommentTextReader.Reader.CurrentStart +
                "hello" +
                XmlDocumentationCommentTextReader.Reader.CurrentEnd, new string(buffer, 0, charsRead));

            s.SetText("");

            charsRead = s.Read(buffer, 0, 200);

            Assert.Equal(71, charsRead);
            Assert.Equal(
                XmlDocumentationCommentTextReader.Reader.CurrentStart +
                "" +
                XmlDocumentationCommentTextReader.Reader.CurrentEnd, new string(buffer, 0, charsRead));

            s.SetText("xxxxxxxxxxxxxxxxxxxxxxxx");

            charsRead = s.Read(buffer, 0, 200);

            Assert.Equal(95, charsRead);
            Assert.Equal(
                XmlDocumentationCommentTextReader.Reader.CurrentStart +
                "xxxxxxxxxxxxxxxxxxxxxxxx" +
                XmlDocumentationCommentTextReader.Reader.CurrentEnd, new string(buffer, 0, charsRead));
        }

        [Fact]
        public void XmlValidation()
        {
            var reader = new XmlDocumentationCommentTextReader();

            Assert.Null(reader.ParseInternal("<a>aaa</a>"));
            Assert.Null(reader.ParseInternal("<a><b x='goo'></b></a>"));
            Assert.NotNull(reader.ParseInternal("<a><b x='goo'></a>"));
            Assert.NotNull(reader.ParseInternal("<a>/a>"));
            Assert.NotNull(reader.ParseInternal("<a>"));
            Assert.Null(reader.ParseInternal("<a></a>"));
            Assert.Null(reader.ParseInternal(""));
        }
    }
}
