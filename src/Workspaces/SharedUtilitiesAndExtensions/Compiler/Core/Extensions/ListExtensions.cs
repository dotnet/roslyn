// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class ListExtensions
    {
        public static void RemoveOrTransformAll<T, TArg>(this List<T> list, Func<T, TArg, T?> transform, TArg arg)
            where T : class
        {
            var targetIndex = 0;
            for (var sourceIndex = 0; sourceIndex < list.Count; sourceIndex++)
            {
                var newValue = transform(list[sourceIndex], arg);
                if (newValue is null)
                    continue;

                list[targetIndex++] = newValue;
            }

            list.RemoveRange(targetIndex, list.Count - targetIndex);
        }
    }
}
