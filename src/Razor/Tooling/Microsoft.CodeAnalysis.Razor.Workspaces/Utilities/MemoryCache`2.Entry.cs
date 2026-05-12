// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.Razor.Utilities;

internal sealed partial class MemoryCache<TKey, TValue> where TKey : notnull
    where TValue : class
{
    private sealed class Entry(TValue value)
    {
        private long _lastAccessTicks = DateTime.UtcNow.Ticks;

        public DateTime LastAccess => new(Volatile.Read(ref _lastAccessTicks));
        public TValue Value => value;

        public void UpdateLastAccess()
        {
            Volatile.Write(ref _lastAccessTicks, DateTime.UtcNow.Ticks);
        }
    }
}
