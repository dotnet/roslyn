// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.SymbolSearch;

internal sealed partial class SymbolSearchUpdateEngine
{
    private sealed class DelayService : IDelayService
    {
        public TimeSpan CachePollDelay { get; } = TimeSpan.FromMinutes(1);
        public TimeSpan FileWriteDelay { get; } = TimeSpan.FromSeconds(10);
        public TimeSpan ExpectedFailureDelay { get; } = TimeSpan.FromMinutes(1);
        public TimeSpan CatastrophicFailureDelay { get; } = TimeSpan.FromDays(1);
        public TimeSpan UpdateSucceededDelay { get; } = TimeSpan.FromDays(1);
    }
}
