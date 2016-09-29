// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
{
    public class SetExtensionTests
    {
        [Fact]
        public void TestAddAll()
        {
            var set = new HashSet<string>() { "a", "b", "c" };
            Assert.False(set.AddAll(new[] { "b", "c" }));
            Assert.True(set.AddAll(new[] { "c", "d" }));
            Assert.True(set.AddAll(new[] { "e", "f" }));
        }
    }
}
