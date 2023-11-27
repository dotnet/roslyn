// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal partial class SymbolSearchUpdateEngine
    {
        private class DelayService : IDelayService
        {
#if NETFRAMEWORK
            public TimeSpan CachePollDelay { get; } = TimeSpan.FromMinutes(1);
#else
            public TimeSpan CachePollDelay { get; } = TimeSpan.FromDays(6).TotalMinutes; // Max is too big for an int. 6 days ends up being almost infinite in the world of sessions.
#endif
            public TimeSpan FileWriteDelay { get; } = TimeSpan.FromSeconds(10);
            public TimeSpan ExpectedFailureDelay { get; } = TimeSpan.FromMinutes(1);
            public TimeSpan CatastrophicFailureDelay { get; } = TimeSpan.FromDays(1);
            public TimeSpan UpdateSucceededDelay { get; } = TimeSpan.FromDays(1);
        }
    }
}
