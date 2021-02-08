// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.2/src/libraries/System.Collections/tests/Generic/List/List.Generic.Tests.ForEach.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    /// <summary>
    /// Contains tests that ensure the correctness of the List class.
    /// </summary>
    public abstract partial class List_Generic_Tests<T> : IList_Generic_Tests<T>
    {
        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ForEach_Verify(int count)
        {
            List<T> list = GenericListFactory(count);
            List<T> visitedItems = new List<T>();
            Action<T> action = delegate (T item) { visitedItems.Add(item); };

            //[] Verify ForEach looks at every item
            visitedItems.Clear();
            list.ForEach(action);
            VerifyList(list, visitedItems);
        }

        [Fact]
        public void ForEach_NullAction_ThrowsArgumentNullException()
        {
            List<T> list = GenericListFactory();
            Assert.Throws<ArgumentNullException>(() => list.ForEach(null!));
        }
    }
}
