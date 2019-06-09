// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public class ArrayBuilderTests
    {
        [Fact]
        public void RemoveDuplicates1()
        {
            var builder = new ArrayBuilder<int> { 1, 2, 3, 2, 4, 5, 1 };
            builder.RemoveDuplicates();
            AssertEx.Equal(new[] { 1, 2, 3, 4, 5 }, builder);

            builder = new ArrayBuilder<int> { 1 };
            builder.RemoveDuplicates();
            AssertEx.Equal(new[] { 1 }, builder);

            builder = new ArrayBuilder<int>();
            builder.RemoveDuplicates();
            AssertEx.Equal(new int[0], builder);
        }

        [Fact]
        public void SortAndRemoveDuplicates1()
        {
            var builder = new ArrayBuilder<int> { 5, 1, 3, 2, 4, 1, 2 };
            builder.SortAndRemoveDuplicates(Comparer<int>.Default);
            AssertEx.Equal(new[] { 1, 2, 3, 4, 5 }, builder);

            builder = new ArrayBuilder<int> { 1 };
            builder.SortAndRemoveDuplicates(Comparer<int>.Default);
            AssertEx.Equal(new[] { 1 }, builder);

            builder = new ArrayBuilder<int> { 1, 2 };
            builder.SortAndRemoveDuplicates(Comparer<int>.Default);
            AssertEx.Equal(new[] { 1, 2 }, builder);

            builder = new ArrayBuilder<int> { 1, 2, 3 };
            builder.SortAndRemoveDuplicates(Comparer<int>.Default);
            AssertEx.Equal(new[] { 1, 2, 3 }, builder);

            builder = new ArrayBuilder<int> { 1, 2, 2 };
            builder.SortAndRemoveDuplicates(Comparer<int>.Default);
            AssertEx.Equal(new[] { 1, 2 }, builder);

            builder = new ArrayBuilder<int>();
            builder.SortAndRemoveDuplicates(Comparer<int>.Default);
            AssertEx.Equal(new int[0], builder);
        }

        [Fact]
        public void SelectDistinct1()
        {
            var builder = new ArrayBuilder<int> { 1, 2, 3, 2, 4, 5, 1 };
            AssertEx.Equal(new[] { 1, 2, 3, 4, 5 }, builder.SelectDistinct(n => n));

            builder = new ArrayBuilder<int> { 1 };
            AssertEx.Equal(new[] { 1 }, builder.SelectDistinct(n => n));

            builder = new ArrayBuilder<int>();
            AssertEx.Equal(new int[0], builder.SelectDistinct(n => n));

            builder = new ArrayBuilder<int> { 1, 2, 3, 2, 4, 5, 1 };
            AssertEx.Equal(new[] { 10 }, builder.SelectDistinct(n => 10));

            builder = new ArrayBuilder<int> { 1, 2, 3, 2, 4, 5, 1 };
            AssertEx.Equal(new byte[] { 1, 2, 3, 4, 5 }, builder.SelectDistinct(n => (byte)n));
        }

        [Fact]
        public void AddRange()
        {
            var builder = new ArrayBuilder<int>();

            builder.AddRange(new int[0], 0, 0);
            AssertEx.Equal(new int[0], builder.ToArray());

            builder.AddRange(new[] { 1, 2, 3 }, 0, 3);
            AssertEx.Equal(new[] { 1, 2, 3 }, builder.ToArray());

            builder.AddRange(new[] { 1, 2, 3 }, 2, 0);
            AssertEx.Equal(new[] { 1, 2, 3 }, builder.ToArray());

            builder.AddRange(new[] { 1, 2, 3 }, 1, 1);
            AssertEx.Equal(new[] { 1, 2, 3, 2 }, builder.ToArray());

            builder.AddRange(new[] { 1, 2, 3 }, 1, 2);
            AssertEx.Equal(new[] { 1, 2, 3, 2, 2, 3 }, builder.ToArray());

            builder.AddRange(new[] { 1, 2, 3 }, 2, 1);
            AssertEx.Equal(new[] { 1, 2, 3, 2, 2, 3, 3 }, builder.ToArray());
        }
    }
}
