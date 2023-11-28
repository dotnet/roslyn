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
            // Don't poll on .NET Core because caching isn't supported, instead, we'll always re-download the file.
            // TimeSpan.Max is too big for an int, 6 days should be enough days for most people to restart their session.
            public TimeSpan CachePollDelay { get; } = TimeSpan.FromDays(6).TotalMinutes;
#endif
            public TimeSpan FileWriteDelay { get; } = TimeSpan.FromSeconds(10);
            public TimeSpan ExpectedFailureDelay { get; } = TimeSpan.FromMinutes(1);
            public TimeSpan CatastrophicFailureDelay { get; } = TimeSpan.FromDays(1);
            public TimeSpan UpdateSucceededDelay { get; } = TimeSpan.FromDays(1);
        }
    }
}
