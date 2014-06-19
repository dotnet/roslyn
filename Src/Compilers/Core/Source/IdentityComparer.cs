// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Roslyn.Compilers
{
    /// <summary>
    /// An identity comparer based on object identity.  (There's probably one in the platform somewhere, but I don't know where)
    /// </summary>
    internal class IdentityComparer : IEqualityComparer<object>
    {
        public static readonly IdentityComparer Instance = new IdentityComparer();

        private IdentityComparer() 
        {
        }

        bool IEqualityComparer<object>.Equals(object a, object b)
        {
            return object.ReferenceEquals(a, b);
        }

        int IEqualityComparer<object>.GetHashCode(object a)
        {
            return RuntimeHelpers.GetHashCode(a);
        }
    }
}
