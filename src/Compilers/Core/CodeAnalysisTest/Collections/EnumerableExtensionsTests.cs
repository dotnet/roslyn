// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Text;
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
