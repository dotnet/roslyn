// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Utilities.Extensions;
using Xunit;

namespace Analyzer.Utilities.Extensions
{
    public class IEnumerableExensionsTests
    {
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, true)]
        [InlineData(3, false)]
        [Theory]
        public void HasExactly2_ReturnsTheCorrectValue(int count, bool result)
        {
            Assert.Equal(result, IEnumerableExtensions.HasExactly(CreateIEnumerable(count), 2));
        }

        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(3, true)]
        [Theory]
        public void HasMoreThan2_ReturnsTheCorrectValue(int count, bool result)
        {
            Assert.Equal(result, IEnumerableExtensions.HasMoreThan(CreateIEnumerable(count), 2));
        }

        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(2, false)]
        [InlineData(3, false)]
        [Theory]
        public void HasLessThan2_ReturnsTheCorrectValue(int count, bool result)
        {
            Assert.Equal(result, IEnumerableExtensions.HasLessThan(CreateIEnumerable(count), 2));
        }

        private static IEnumerable<int> CreateIEnumerable(int count)
            => count > 0 ? Enumerable.Range(0, count) : Enumerable.Empty<int>();
    }
}
