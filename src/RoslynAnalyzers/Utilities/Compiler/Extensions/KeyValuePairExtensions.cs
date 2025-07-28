// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Analyzer.Utilities.Extensions
{
    internal static class KeyValuePairExtensions
    {
        extension<TKey, TValue>(KeyValuePair<TKey, TValue> pair)
        {
            public KeyValuePair<TKey?, TValue?> AsNullable()
            {
                // This conversion is safe
                return pair!;
            }
        }
    }
}
