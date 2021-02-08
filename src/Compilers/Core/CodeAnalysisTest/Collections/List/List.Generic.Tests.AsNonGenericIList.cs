// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Tests
{
    /// <summary>
    /// Contains tests that ensure the correctness of the List class.
    /// </summary>
    public abstract partial class List_Generic_Tests_AsNonGenericIList : IList_NonGeneric_Tests
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

        protected virtual List<string> GenericListFactory()
        {
            return new List<string>();
        }

        protected virtual List<string> GenericListFactory(int count)
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
