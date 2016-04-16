// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.DiaSymReader.PortablePdb.UnitTests
{
    public class EnumerableHelpersTests
    {
        [Fact]
        public void GroupBy1()
        {
            var pairs = new[]
            {
                KeyValuePair.Create("A", 1),
                KeyValuePair.Create("B", 2),
                KeyValuePair.Create("C", 3),
                KeyValuePair.Create("a", 4),
                KeyValuePair.Create("B", 5),
                KeyValuePair.Create("A", 6),
                KeyValuePair.Create("d", 7),
            };

            var groups = pairs.GroupBy(StringComparer.OrdinalIgnoreCase);
            AssertEx.SetEqual(new[] { "A", "B", "C", "d" }, groups.Keys);

            Assert.Equal(0, groups["A"].Key);
            AssertEx.Equal(new[] { 1, 4, 6 }, groups["A"].Value);

            Assert.Equal(0, groups["B"].Key);
            AssertEx.Equal(new[] { 2, 5 }, groups["B"].Value);

            Assert.Equal(3, groups["C"].Key);
            Assert.True(groups["C"].Value.IsDefault);

            Assert.Equal(7, groups["d"].Key);
            Assert.True(groups["d"].Value.IsDefault);
        }
    }
}
