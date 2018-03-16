// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
