// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal partial class SymbolSearchUpdateEngine
    {
        private class DelayService : IDelayService
        {
            public TimeSpan CachePollDelay { get; } = TimeSpan.FromMinutes(1);
            public TimeSpan FileWriteDelay { get; } = TimeSpan.FromSeconds(10);
            public TimeSpan ExpectedFailureDelay { get; } = TimeSpan.FromMinutes(1);
            public TimeSpan CatastrophicFailureDelay { get; } = TimeSpan.FromDays(1);
            public TimeSpan UpdateSucceededDelay { get; } = TimeSpan.FromDays(1);
        }
    }
}
