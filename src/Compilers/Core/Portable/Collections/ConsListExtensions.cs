// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Extension methods associated with ConsList.
    /// </summary>
    internal static class ConsListExtensions
    {
        public static ConsList<T> Prepend<T>(this ConsList<T>? list, T head)
        {
            return new ConsList<T>(head, list ?? ConsList<T>.Empty);
        }

        public static bool ContainsReference<T>(this ConsList<T> list, T element)
        {
            for (; list != ConsList<T>.Empty; list = list.Tail)
            {
                if (ReferenceEquals(list.Head, element))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
