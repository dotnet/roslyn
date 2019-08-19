// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
