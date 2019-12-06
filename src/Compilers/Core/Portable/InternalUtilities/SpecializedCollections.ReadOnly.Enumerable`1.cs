// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections;

namespace Roslyn.Utilities
{
    internal partial class SpecializedCollections
    {
        private partial class ReadOnly
        {
            internal class Enumerable<TUnderlying> : IEnumerable
                where TUnderlying : IEnumerable
            {
                protected readonly TUnderlying Underlying;

                public Enumerable(TUnderlying underlying)
                {
                    this.Underlying = underlying;
                }

                public IEnumerator GetEnumerator()
                {
                    return this.Underlying.GetEnumerator();
                }
            }
        }
    }
}
