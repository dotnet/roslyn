// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public class EnumerableExtensionsTests
    {
        [Fact]
        public void AsSingleton()
        {
            Assert.Equal(0, new int[] { }.AsSingleton());
            Assert.Equal(1, new int[] { 1 }.AsSingleton());
            Assert.Equal(0, new int[] { 1, 2 }.AsSingleton());

            Assert.Equal(0, Enumerable.Range(1, 0).AsSingleton());
            Assert.Equal(1, Enumerable.Range(1, 1).AsSingleton());
            Assert.Equal(0, Enumerable.Range(1, 2).AsSingleton());
        }
    }
}
