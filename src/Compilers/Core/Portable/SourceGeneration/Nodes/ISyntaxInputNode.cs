// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    internal interface ISyntaxInputNode
    {
        ISyntaxInputBuilder GetBuilder(DriverStateTable table);
    }

    internal interface ISyntaxInputBuilder
    {
        void VisitTree(SyntaxNode root, EntryState state, SemanticModel? model);

        void SaveStateAndFree(ImmutableDictionary<object, IStateTable>.Builder tables);
    }
}
