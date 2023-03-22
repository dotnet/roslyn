// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.7/src/libraries/System.Collections/tests/Generic/HashSet/HashSet.Generic.Tests.AsNonGenericIEnumerable.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public class SegmentedHashSet_IEnumerable_NonGeneric_Tests : IEnumerable_NonGeneric_Tests
    {
        protected override IEnumerable NonGenericIEnumerableFactory(int count)
        {
            var set = new SegmentedHashSet<string>();
            int seed = 12354;
            while (set.Count < count)
                set.Add(CreateT(set, seed++));
            return set;
        }

        protected override bool Enumerator_Current_UndefinedOperation_Throws => true;

        protected override ModifyOperation ModifyEnumeratorThrows => base.ModifyEnumeratorAllowed & ~ModifyOperation.Remove;

        protected override ModifyOperation ModifyEnumeratorAllowed => ModifyOperation.Overwrite | ModifyOperation.Remove;

        /// <summary>
        /// Returns a set of ModifyEnumerable delegates that modify the enumerable passed to them.
        /// </summary>
        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations)
        {
            if ((operations & ModifyOperation.Clear) == ModifyOperation.Clear)
            {
                yield return (IEnumerable enumerable) =>
                {
                    SegmentedHashSet<string> casted = ((SegmentedHashSet<string>)enumerable);
                    if (casted.Count > 0)
                    {
                        casted.Clear();
                        return true;
                    }
                    return false;
                };
            }
        }

        private protected static string CreateT(SegmentedHashSet<string> set, int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes = new byte[stringLength];
            rand.NextBytes(bytes);
            string ret = Convert.ToBase64String(bytes);
            while (set.Contains(ret))
            {
                rand.NextBytes(bytes);
                ret = Convert.ToBase64String(bytes);
            }
            return ret;
        }
    }
}
