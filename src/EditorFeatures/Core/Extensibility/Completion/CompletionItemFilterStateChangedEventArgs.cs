using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.Editor
{
    internal class CompletionItemFilterStateChangedEventArgs : EventArgs
    {
        public ImmutableDictionary<CompletionItemFilter, bool> FilterState { get; }

        public CompletionItemFilterStateChangedEventArgs(
            ImmutableDictionary<CompletionItemFilter, bool> filterState)
        {
            this.FilterState = filterState;
        }
    }
}