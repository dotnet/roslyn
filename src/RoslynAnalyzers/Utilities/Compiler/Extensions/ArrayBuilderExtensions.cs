// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Analyzer.Utilities.PooledObjects.Extensions
{
    internal static class ArrayBuilderExtensions
    {
        public static void AddIfNotNull<T>(this ArrayBuilder<T> builder, T? item)
            where T : class
        {
            if (item != null)
            {
                builder.Add(item);
            }
        }
    }
}
