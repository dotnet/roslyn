// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Scripting
{
    internal static class IListExtensions
    {
        public static void AddRange<T>(this IList<T> list, ImmutableArray<T> items)
        {
            foreach (var item in items)
            {
                list.Add(item);
            }
        }
    }
}
