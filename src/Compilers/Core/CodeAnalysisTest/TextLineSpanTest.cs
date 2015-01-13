// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Microsoft.Languages.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Language.Text.UnitTests
{
    [TestClass()]
    public class TextLineSpanTest
    {
        [TestMethod()]
        public void Ctor1()
        {
            var body = new TextSpan(0, 3);
            var span = new TextLineSpan(body, 2);
            Assert.AreEqual(body, span.Span);
            Assert.AreEqual(2, span.LineBreakLength);
            Assert.AreEqual(new TextSpan(3, 2), span.LineBreakSpan);
        }

        [TestMethod]
        public void Equals1()
        {
            var left = new TextLineSpan(new TextSpan(0, 3), 2);
            var right = new TextLineSpan(new TextSpan(0, 3), 2);
            Assert.AreEqual(left, right);
            Assert.IsTrue(left == right);
            Assert.IsFalse(left != right);
        }

        [TestMethod, Description("Differ in length")]
        public void Equals2()
        {
            var left = new TextLineSpan(new TextSpan(0, 3), 2);
            var right = new TextLineSpan(new TextSpan(0, 3), 3);
            Assert.AreNotEqual(left, right);
            Assert.IsFalse(left == right);
            Assert.IsTrue(left != right);
        }

        [TestMethod, Description("Differnt body spans")]
        public void Equals3()
        {
            var left = new TextLineSpan(new TextSpan(0, 2), 3);
            var right = new TextLineSpan(new TextSpan(0, 3), 3);
            Assert.AreNotEqual(left, right);
            Assert.IsFalse(left == right);
            Assert.IsTrue(left != right);
        }

        [TestMethod]
        public void SpanIncludingLineBreak1()
        {
            var span = new TextLineSpan(new TextSpan(0, 2), 2);
            Assert.AreEqual(4, span.SpanIncludingLineBreak.Length);
            Assert.AreEqual(span.LengthIncludingLineBreak, span.SpanIncludingLineBreak.Length);
        }


    }
}
