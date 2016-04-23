// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Very cheap trivial comparer that never matches the keys,
    /// should only be used in empty dictionaries.
    /// </summary>
    internal sealed class EmptyComparer : IEqualityComparer<object>
    {
        public static readonly EmptyComparer Instance = new EmptyComparer();

        private EmptyComparer()
        {
        }

        bool IEqualityComparer<object>.Equals(object a, object b)
        {
            Debug.Assert(false, "Are we using empty comparer with nonempty dictionary?");
            return false;
        }

        int IEqualityComparer<object>.GetHashCode(object s)
        {
            // dictionary will call this often
            return 0;
        }
    }
}
