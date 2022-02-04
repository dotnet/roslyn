// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class AssemblyIdentityMapTests
    {
        [Fact]
        public void Map()
        {
            var map = new AssemblyIdentityMap<int>();
            map.Add(new AssemblyIdentity("a", new Version(1, 0, 0, 0)), 10);
            map.Add(new AssemblyIdentity("a", new Version(1, 8, 0, 0)), 18);
            map.Add(new AssemblyIdentity("a", new Version(1, 5, 0, 0)), 15);

            map.Add(new AssemblyIdentity("b", new Version(1, 0, 0, 0)), 10);
            map.Add(new AssemblyIdentity("b", new Version(1, 0, 0, 0)), 20);

            int value;
            Assert.True(map.Contains(new AssemblyIdentity("a", new Version(1, 0, 0, 0))));
            Assert.True(map.TryGetValue(new AssemblyIdentity("a", new Version(1, 0, 0, 0)), out value));
            Assert.Equal(10, value);

            Assert.True(map.Contains(new AssemblyIdentity("a", new Version(1, 1, 0, 0))));
            Assert.True(map.TryGetValue(new AssemblyIdentity("a", new Version(1, 1, 0, 0)), out value));
            Assert.Equal(15, value);

            Assert.True(map.Contains(new AssemblyIdentity("a", new Version(1, 0, 0, 0)), allowHigherVersion: false));
            Assert.True(map.TryGetValue(new AssemblyIdentity("a", new Version(1, 0, 0, 0)), out value, allowHigherVersion: false));
            Assert.Equal(10, value);

            Assert.False(map.Contains(new AssemblyIdentity("a", new Version(1, 1, 0, 0)), allowHigherVersion: false));
            Assert.False(map.TryGetValue(new AssemblyIdentity("a", new Version(1, 1, 0, 0)), out value, allowHigherVersion: false));
            Assert.Equal(0, value);

            Assert.False(map.Contains(new AssemblyIdentity("b", new Version(1, 1, 0, 0)), allowHigherVersion: true));
            Assert.False(map.Contains(new AssemblyIdentity("b", new Version(1, 1, 0, 0)), allowHigherVersion: false));

            // returns the first value added to the map if there are multiple matching identities:
            Assert.True(map.TryGetValue(new AssemblyIdentity("b", new Version(1, 0, 0, 0)), out value));
            Assert.Equal(10, value);
        }
    }
}
