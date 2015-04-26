// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Differencing
{
    // currently doesnt care about actual result.
    public class CSharpSyntaxNodeComparerTests
    {
        [Fact]
        public void TestTokenUpdate()
        {
            var before = @"class A { }";
            var after = @"class B { }";

            Test(before, after);
        }

        [Fact]
        public void TestTokenUpdate2()
        {
            var before = @"class A { }";
            var after = @"class A { void Method() { } }";

            Test(before, after);
        }

        [Fact]
        public void TestTokenUpdate3()
        {
            var before = @"class A { void Method() { } }";
            var after = @"class A { }";

            Test(before, after);
        }

        [Fact]
        public void TestTokenUpdate4()
        {
            var before = @"class A { }";
            var after = @"class A {     }";

            Test(before, after);
        }

        [Fact]
        public void TestTokenUpdate5()
        {
            var before = @"public static class A { }";
            var after = @"static public class A { }";

            Test(before, after);
        }

        private static void Test(string before, string after)
        {
            var beforeTree = SyntaxFactory.ParseSyntaxTree(before);
            var afterTree = SyntaxFactory.ParseSyntaxTree(after);

            var differ = new CSharpSyntaxDiffService();
            var edits = differ.Diff(beforeTree.GetRoot(), afterTree.GetRoot());

            GC.KeepAlive(edits);
        }
    }
}
