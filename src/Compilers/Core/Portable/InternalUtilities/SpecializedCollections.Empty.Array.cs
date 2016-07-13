// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Roslyn.Utilities
{
    internal partial class SpecializedCollections
    {
        private partial class Empty
        {
            internal class Array<T>
            {
#if COMPILERCORE
                public static readonly T[] Instance = new T[0];
#else
                public static readonly T[] Instance = System.Array.Empty<T>();
#endif
            }
        }
    }
}
