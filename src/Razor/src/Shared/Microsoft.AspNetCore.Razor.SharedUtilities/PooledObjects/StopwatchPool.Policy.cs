// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal partial class StopwatchPool
{
    private sealed class Policy : PooledObjectPolicy
    {
        public static readonly Policy Default = new();

        private Policy()
        {
        }

        public override Stopwatch Create() => new();

        public override bool Return(Stopwatch watch)
        {
            watch.Reset();
            return true;
        }
    }
}
