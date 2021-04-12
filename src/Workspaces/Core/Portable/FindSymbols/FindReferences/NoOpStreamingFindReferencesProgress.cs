﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    /// <summary>
    /// A does-nothing version of the <see cref="IStreamingFindReferencesProgress"/>. Useful for
    /// clients that have no need to report progress as they work.
    /// </summary>
    internal class NoOpStreamingFindReferencesProgress : IStreamingFindReferencesProgress
    {
        public static readonly IStreamingFindReferencesProgress Instance =
            new NoOpStreamingFindReferencesProgress();

        public IStreamingProgressTracker ProgressTracker { get; } = new NoOpProgressTracker();

        private NoOpStreamingFindReferencesProgress()
        {
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public static Task ReportProgressAsync(int current, int maximum) => Task.CompletedTask;
#pragma warning restore IDE0060 // Remove unused parameter

        public ValueTask OnCompletedAsync() => default;
        public ValueTask OnStartedAsync() => default;
        public ValueTask OnDefinitionFoundAsync(ISymbol symbol) => default;
        public ValueTask OnReferenceFoundAsync(ISymbol symbol, ReferenceLocation location) => default;
        public ValueTask OnFindInDocumentStartedAsync(Document document) => default;
        public ValueTask OnFindInDocumentCompletedAsync(Document document) => default;

        private class NoOpProgressTracker : IStreamingProgressTracker
        {
            public ValueTask AddItemsAsync(int count) => default;
            public ValueTask ItemCompletedAsync() => default;
        }
    }
}
