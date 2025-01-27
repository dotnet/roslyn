// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed class ProjectIdTests
{
    [Fact]
    public void DebugNameNotPartOfIdentity1()
    {
        var guid1 = Guid.NewGuid();

        // Same guid, so they should be considered the same, regardless of debug name.
        var id1 = ProjectId.CreateFromSerialized(guid1, "debug1");
        var id2 = ProjectId.CreateFromSerialized(guid1, "debug2");

        Assert.True(id1.Equals(id2));
        Assert.True(id1 == id2);
        Assert.True(id1.GetHashCode() == id2.GetHashCode());
        Assert.True(id1.Checksum == id2.Checksum);
    }

    [Fact]
    public void DebugNameNotPartOfIdentity2()
    {
        // Different guid, so they should be considered different, regardless of debug name.
        var id1 = ProjectId.CreateFromSerialized(Guid.NewGuid(), "debug1");
        var id2 = ProjectId.CreateFromSerialized(Guid.NewGuid(), "debug1");

        Assert.False(id1.Equals(id2));
        Assert.False(id1 == id2);
        Assert.False(id1.GetHashCode() == id2.GetHashCode());
        Assert.False(id1.Checksum == id2.Checksum);
    }
}
