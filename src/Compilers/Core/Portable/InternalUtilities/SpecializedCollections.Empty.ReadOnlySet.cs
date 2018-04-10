// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Roslyn.Utilities
{
    internal partial class SpecializedCollections
    {
        private partial class Empty
        {
            internal class ReadOnlySet<T> : IReadOnlySet<T>
            {
                public static readonly ReadOnlySet<T> Instance = new ReadOnlySet<T>();

                private ReadOnlySet()
                { }

                public int Count => 0;

                public bool Contains(T item) => false;
            }
        }
    }
}
