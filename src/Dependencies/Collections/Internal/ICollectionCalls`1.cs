// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Collections.Internal
{
    internal static class ICollectionCalls<T>
    {
        public static bool IsReadOnly<TCollection>(ref TCollection collection)
            where TCollection : ICollection<T>
            => collection.IsReadOnly;
    }
}
