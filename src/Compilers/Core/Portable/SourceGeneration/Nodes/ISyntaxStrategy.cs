// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis
{
    internal interface ISyntaxSelectionStrategy<T>
    {
        ISyntaxInputBuilder GetBuilder(StateTableStore tableStore, object key, bool trackIncrementalSteps, string? name, IEqualityComparer<T> comparer);
    }

    internal interface ISyntaxInputBuilder
    {
        void VisitTree(Lazy<SyntaxNode> root, EntryState state, SemanticModel? model, CancellationToken cancellationToken);

        void SaveStateAndFree(StateTableStore.Builder tableStoreBuilder);
    }
}
