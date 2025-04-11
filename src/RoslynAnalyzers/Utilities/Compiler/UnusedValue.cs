// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace Analyzer.Utilities
{
    /// <summary>
    /// A placeholder value type for <see cref="ConcurrentDictionary{TKey, TValue}"/> used as a set.
    /// </summary>
    internal readonly struct UnusedValue
    {
    }
}
