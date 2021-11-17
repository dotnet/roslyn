// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public class HashSet_Generic_Tests_string : HashSet_Generic_Tests<string>
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

    public class HashSet_Generic_Tests_int : HashSet_Generic_Tests<int>
    {
        protected override int CreateT(int seed)
        {
            Random rand = new Random(seed);
            return rand.Next();
        }

        protected override bool DefaultValueAllowed => true;
    }

    public class HashSet_Generic_Tests_int_With_Comparer_WrapStructural_Int : HashSet_Generic_Tests<int>
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
            return new HashSet<int>(new WrapStructural_Int());
        }
    }

    public class HashSet_Generic_Tests_int_With_Comparer_WrapStructural_SimpleInt : HashSet_Generic_Tests<SimpleInt>
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
            return new HashSet<SimpleInt>(new WrapStructural_SimpleInt());
        }
    }
}
