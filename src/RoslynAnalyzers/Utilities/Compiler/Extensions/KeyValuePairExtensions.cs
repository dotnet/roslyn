// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Analyzer.Utilities.Extensions
{
    internal static class KeyValuePairExtensions
    {
        public static KeyValuePair<TKey?, TValue?> AsNullable<TKey, TValue>(this KeyValuePair<TKey, TValue> pair)
        {
            // This conversion is safe
            return pair!;
        }
    }
}
