// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class ArrayBuilderExtensions
    {
        /// <summary>
        /// Realizes the OneOrMany and disposes the builder in one operation.
        /// </summary>
        public static OneOrMany<T> ToOneOrManyAndFree<T>(this ArrayBuilder<T> builder)
        {
            if (builder.Count == 1)
            {
                var result = OneOrMany.Create(builder[0]);
                builder.Free();
                return result;
            }
            else
            {
                return OneOrMany.Create(builder.ToImmutableAndFree());
            }
        }

        public static void AddRange<T>(this ArrayBuilder<T> builder, OneOrMany<T> items)
        {
            items.AddRangeTo(builder);
        }
    }
}
