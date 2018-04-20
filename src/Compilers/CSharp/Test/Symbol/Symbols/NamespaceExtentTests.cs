// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class NamespaceExtentTests : CSharpTestBase
    {
        [Fact]
        public void Equality()
        {
            var c1 = CreateCompilation("");
            var c2 = CreateCompilation("");
            var a1 = c1.Assembly;
            var a2 = c2.Assembly;

            EqualityTesting.AssertEqual(default(NamespaceExtent), default(NamespaceExtent));
            EqualityTesting.AssertNotEqual(default(NamespaceExtent), new NamespaceExtent(c2));
            EqualityTesting.AssertNotEqual(new NamespaceExtent(c1), default(NamespaceExtent));

            EqualityTesting.AssertEqual(new NamespaceExtent(c1), new NamespaceExtent(c1));
            EqualityTesting.AssertNotEqual(new NamespaceExtent(c1), new NamespaceExtent(c2));
            EqualityTesting.AssertEqual(new NamespaceExtent(a1), new NamespaceExtent(a1));
            EqualityTesting.AssertNotEqual(new NamespaceExtent(a1), new NamespaceExtent(a2));
        }
    }
}
