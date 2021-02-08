// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    public abstract partial class List_Generic_Tests<T> : IList_Generic_Tests<T>
    {
        [Fact]
        public void ConvertAll()
        {
            var list = new List<int>(new int[] { 1, 2, 3});
            var before = list.ToList();
            var after = list.ConvertAll((i) => { return 10 * i; });

            Assert.Equal(before.Count, list.Count);
            Assert.Equal(before.Count, after.Count);

            for (int i=0; i<list.Count; i++) {
                Assert.Equal(before[i], list[i]);
                Assert.Equal(before[i] * 10, after[i]);
            }
        }
    }
}
