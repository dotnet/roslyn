// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;

public sealed class SetExtensionTests
{
    [Fact]
    public void TestAddAll()
    {
        var set = new HashSet<string>() { "a", "b", "c" };
        Assert.False(set.AddAll(["b", "c"]));
        Assert.True(set.AddAll(["c", "d"]));
        Assert.True(set.AddAll(["e", "f"]));
    }
}
