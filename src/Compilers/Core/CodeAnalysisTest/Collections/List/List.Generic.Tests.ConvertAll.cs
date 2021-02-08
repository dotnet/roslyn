// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.2/src/libraries/System.Collections/tests/Generic/List/List.Generic.Tests.ConvertAll.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

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
