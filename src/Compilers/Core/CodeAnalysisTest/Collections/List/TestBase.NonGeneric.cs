// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.2/src/libraries/Common/tests/System/Collections/TestBase.NonGeneric.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    /// <summary>
    /// Provides a base set of nongeneric operations that are used by all other testing interfaces.
    /// </summary>
    public abstract class TestBase
    {
        #region Helper Methods

        public static IEnumerable<object[]> ValidCollectionSizes()
        {
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { 75 };
        }

        public static IEnumerable<object[]> ValidPositiveCollectionSizes()
        {
            yield return new object[] { 1 };
            yield return new object[] { 75 };
        }

        public enum EnumerableType
        {
            SegmentedHashSet,
            SortedSet,
            List,
            Queue,
            Lazy,
        };

        [Flags]
        public enum ModifyOperation
        {
            None = 0,
            Add = 1,
            Insert = 2,
            Overwrite = 4,
            Remove = 8,
            Clear = 16
        }

        #endregion
    }
}
