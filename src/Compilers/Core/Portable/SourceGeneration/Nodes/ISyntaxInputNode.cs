// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis
{
    internal interface ISyntaxInputNode
    {
        ISyntaxInputBuilder GetBuilder(DriverStateTable table);
    }

    internal interface ISyntaxInputBuilder
    {
        ISyntaxInputNode SyntaxInputNode { get; }

        void VisitTree(Lazy<SyntaxNode> root, EntryState state, SemanticModel? model, CancellationToken cancellationToken);

        void SaveStateAndFree(ImmutableSegmentedDictionary<object, IStateTable>.Builder tables);
    }
}
