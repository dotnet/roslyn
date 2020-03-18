// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class IListExtensions
    {
        public static int IndexOf<T>(this IList<T> list, Func<T, bool> predicate)
        {
            Contract.ThrowIfNull(list);
            Contract.ThrowIfNull(predicate);

            for (var i = 0; i < list.Count; i++)
            {
                if (predicate(list[i]))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
