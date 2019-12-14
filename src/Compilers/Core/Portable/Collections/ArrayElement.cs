// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis
{
    [DebuggerDisplay("{Value,nq}")]
    internal struct ArrayElement<T>
    {
        internal T Value;

        public static implicit operator T(ArrayElement<T> element)
        {
            return element.Value;
        }

        //NOTE: there is no opposite conversion operator T -> ArrayElement<T>
        //
        // that is because it is preferred to update array elements in-place
        // "elements[i].Value = v" results in much better code than "elements[i] = (ArrayElement<T>)v"
        //
        // The reason is that x86 ABI requires that structs must be returned in
        // a return buffer even if they can fit in a register like this one.
        // Also since struct contains a reference, the write to the buffer is done with a checked GC barrier 
        // as JIT does not know if the write goes to a stack or a heap location.
        // Assigning to Value directly easily avoids all this redundancy.

        [return: NotNullIfNotNull(parameterName: "items")]
        public static ArrayElement<T>[]? MakeElementArray(T[]? items)
        {
            if (items == null)
            {
                return null;
            }

            var array = new ArrayElement<T>[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                array[i].Value = items[i];
            }

            return array;
        }

        [return: NotNullIfNotNull(parameterName: "items")]
        public static T[]? MakeArray(ArrayElement<T>[]? items)
        {
            if (items == null)
            {
                return null;
            }

            var array = new T[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                array[i] = items[i].Value;
            }

            return array;
        }
    }
}
