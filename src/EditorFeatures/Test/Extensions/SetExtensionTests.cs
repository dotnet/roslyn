// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
