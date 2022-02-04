// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.2/src/libraries/System.Collections/tests/Generic/List/List.Generic.Tests.AsNonGenericIList.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    /// <summary>
    /// Contains tests that ensure the correctness of the List class.
    /// </summary>
    public abstract partial class SegmentedList_Generic_Tests_AsNonGenericIList : IList_NonGeneric_Tests
    {
        #region IList_Generic_Tests

        protected override bool NullAllowed => true;

        protected override IList NonGenericIListFactory()
        {
            return GenericListFactory();
        }

        protected override IList NonGenericIListFactory(int count)
        {
            return GenericListFactory(count);
        }

        private protected virtual SegmentedList<string> GenericListFactory()
        {
            return new SegmentedList<string>();
        }

        private protected virtual SegmentedList<string> GenericListFactory(int count)
        {
            var list = GenericListFactory();
            int seed = 5321;
            while (list.Count < count)
                list.Add((string)CreateT(seed++));
            return list;
        }

        protected override object CreateT(int seed)
        {
            if (seed % 2 == 0)
            {
                int stringLength = seed % 10 + 5;
                Random rand = new Random(seed);
                byte[] bytes = new byte[stringLength];
                rand.NextBytes(bytes);
                return Convert.ToBase64String(bytes);
            }
            else
            {
                Random rand = new Random(seed);
                return rand.Next();
            }
        }

        #endregion
    }
}
