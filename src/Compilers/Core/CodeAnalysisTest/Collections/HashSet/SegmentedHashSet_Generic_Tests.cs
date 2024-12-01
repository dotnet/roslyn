// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v8.0.3/src/libraries/System.Collections/tests/Generic/HashSet/HashSet.Generic.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public class SegmentedHashSet_Generic_Tests_string : SegmentedHashSet_Generic_Tests<string>
    {
        protected override string CreateT(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes = new byte[stringLength];
            rand.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }

    public class SegmentedHashSet_Generic_Tests_int : SegmentedHashSet_Generic_Tests<int>
    {
        protected override int CreateT(int seed)
        {
            Random rand = new Random(seed);
            return rand.Next();
        }

        protected override bool DefaultValueAllowed => true;
    }

    public class SegmentedHashSet_Generic_Tests_int_With_Comparer_WrapStructural_Int : SegmentedHashSet_Generic_Tests<int>
    {
        protected override IEqualityComparer<int> GetIEqualityComparer()
        {
            return new WrapStructural_Int();
        }

        protected override IComparer<int> GetIComparer()
        {
            return new WrapStructural_Int();
        }

        protected override int CreateT(int seed)
        {
            Random rand = new Random(seed);
            return rand.Next();
        }

        protected override ISet<int> GenericISetFactory()
        {
            return new SegmentedHashSet<int>(new WrapStructural_Int());
        }
    }

    public class SegmentedHashSet_Generic_Tests_int_With_Comparer_WrapStructural_SimpleInt : SegmentedHashSet_Generic_Tests<SimpleInt>
    {
        protected override IEqualityComparer<SimpleInt> GetIEqualityComparer()
        {
            return new WrapStructural_SimpleInt();
        }

        protected override IComparer<SimpleInt> GetIComparer()
        {
            return new WrapStructural_SimpleInt();
        }

        protected override SimpleInt CreateT(int seed)
        {
            Random rand = new Random(seed);
            return new SimpleInt(rand.Next());
        }

        protected override ISet<SimpleInt> GenericISetFactory()
        {
            return new SegmentedHashSet<SimpleInt>(new WrapStructural_SimpleInt());
        }
    }

    public class SegmentedHashSet_Generic_Tests_EquatableBackwardsOrder : SegmentedHashSet_Generic_Tests<EquatableBackwardsOrder>
    {
        protected override EquatableBackwardsOrder CreateT(int seed)
        {
            Random rand = new Random(seed);
            return new EquatableBackwardsOrder(rand.Next());
        }

        protected override ISet<EquatableBackwardsOrder> GenericISetFactory()
        {
            return new SegmentedHashSet<EquatableBackwardsOrder>();
        }
    }

    public class SegmentedHashSet_Generic_Tests_int_With_Comparer_SameAsDefaultComparer : SegmentedHashSet_Generic_Tests<int>
    {
        protected override IEqualityComparer<int> GetIEqualityComparer()
        {
            return new Comparer_SameAsDefaultComparer();
        }

        protected override int CreateT(int seed)
        {
            Random rand = new Random(seed);
            return rand.Next();
        }

        protected override ISet<int> GenericISetFactory()
        {
            return new SegmentedHashSet<int>(new Comparer_SameAsDefaultComparer());
        }
    }

    public class SegmentedHashSet_Generic_Tests_int_With_Comparer_HashCodeAlwaysReturnsZero : SegmentedHashSet_Generic_Tests<int>
    {
        protected override IEqualityComparer<int> GetIEqualityComparer()
        {
            return new Comparer_HashCodeAlwaysReturnsZero();
        }

        protected override int CreateT(int seed)
        {
            Random rand = new Random(seed);
            return rand.Next();
        }

        protected override ISet<int> GenericISetFactory()
        {
            return new SegmentedHashSet<int>(new Comparer_HashCodeAlwaysReturnsZero());
        }
    }

    public class SegmentedHashSet_Generic_Tests_int_With_Comparer_ModOfInt : SegmentedHashSet_Generic_Tests<int>
    {
        protected override IEqualityComparer<int> GetIEqualityComparer()
        {
            return new Comparer_ModOfInt(15000);
        }

        protected override IComparer<int> GetIComparer()
        {
            return new Comparer_ModOfInt(15000);
        }

        protected override int CreateT(int seed)
        {
            Random rand = new Random(seed);
            return rand.Next();
        }

        protected override ISet<int> GenericISetFactory()
        {
            return new SegmentedHashSet<int>(new Comparer_ModOfInt(15000));
        }
    }

    public class SegmentedHashSet_Generic_Tests_int_With_Comparer_AbsOfInt : SegmentedHashSet_Generic_Tests<int>
    {
        protected override IEqualityComparer<int> GetIEqualityComparer()
        {
            return new Comparer_AbsOfInt();
        }

        protected override int CreateT(int seed)
        {
            Random rand = new Random(seed);
            return rand.Next();
        }

        protected override ISet<int> GenericISetFactory()
        {
            return new SegmentedHashSet<int>(new Comparer_AbsOfInt());
        }
    }

    public class SegmentedHashSet_Generic_Tests_int_With_Comparer_BadIntEqualityComparer : SegmentedHashSet_Generic_Tests<int>
    {
        protected override IEqualityComparer<int> GetIEqualityComparer()
        {
            return new BadIntEqualityComparer();
        }

        protected override int CreateT(int seed)
        {
            Random rand = new Random(seed);
            return rand.Next();
        }

        protected override ISet<int> GenericISetFactory()
        {
            return new SegmentedHashSet<int>(new BadIntEqualityComparer());
        }
    }
}
