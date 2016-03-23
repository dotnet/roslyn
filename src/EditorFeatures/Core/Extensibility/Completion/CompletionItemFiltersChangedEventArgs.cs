using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.Editor
{
    internal class CompletionItemFiltersChangedEventArgs : EventArgs
    {
        public ImmutableDictionary<CompletionItemFilter, bool> FilterState { get; }

        public CompletionItemFiltersChangedEventArgs(
            ImmutableDictionary<CompletionItemFilter, bool> filterState)
        {
            this.FilterState = filterState;
        }
    }
}