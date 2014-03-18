// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public sealed class StringTableTests
    {
        [Fact]
        public void TestAddSameWithStringBuilderProducesSameStringInstance()
        {
            var st = new StringTable();
            var sb1 = new StringBuilder("foo");
            var sb2 = new StringBuilder("foo");
            var s1 = st.Add(sb1);
            var s2 = st.Add(sb2);
            Assert.Same(s1, s2);
        }

        [Fact]
        public void TestAddDifferentWithStringBuilderProducesDifferentStringInstance()
        {
            var st = new StringTable();
            var sb1 = new StringBuilder("foo");
            var sb2 = new StringBuilder("bar");
            var s1 = st.Add(sb1);
            var s2 = st.Add(sb2);
            Assert.NotEqual((object)s1, (object)s2);
        }

        [Fact]
        public void TestAddSameWithVariousInputsProducesSameStringInstance()
        {
            var st = new StringTable();

            var s1 = st.Add(new StringBuilder(" "));
            var s2 = st.Add(' ');
            Assert.Same(s1, s2);

            var s3 = st.Add(" ");
            Assert.Same(s2, s3);

            var s4 = st.Add(new char[1] { ' ' }, 0, 1);
            Assert.Same(s3, s4);

            var s5 = st.Add("ABC DEF", 3, 1);
            Assert.Same(s4, s5);
        }

        [Fact]
        public void TestAddSameWithCharProducesSameStringInstance()
        {
            var st = new StringTable();
            var s1 = st.Add(' ');
            var s2 = st.Add(' ');
            Assert.Same(s1, s2);
        }

        [Fact]
        public void TestSharedAddSameWithStringBuilderProducesSameStringInstance()
        {
            var sb1 = new StringBuilder("foo");
            var sb2 = new StringBuilder("foo");
            var s1 = new StringTable().Add(sb1);
            var s2 = new StringTable().Add(sb2);
            Assert.Same(s1, s2);
        }

        [Fact]
        public void TestSharedAddSameWithCharProducesSameStringInstance()
        {
            // Regression test for a bug where single-char strings were not being
            // found in the shared table.
            var s1 = new StringTable().Add(' ');
            var s2 = new StringTable().Add(' ');
            Assert.Same(s1, s2);
        }
    }
}
