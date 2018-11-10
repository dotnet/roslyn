// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.Editor.Wrapping
{
    /// <summary>
    /// Interface for types that can wrap some sort of language construct.  The main refactoring
    /// keeps walking up nodes until it finds the first IWrapper that can handle that node.  That
    /// way the user is not inundated with lots of wrapping options for all the nodes their cursor
    /// is contained within.
    /// </summary>
    internal interface IWrapper
    {
        Task<ImmutableArray<CodeAction>> ComputeRefactoringsAsync(
            Document document, int position, SyntaxNode node, CancellationToken cancellationToken);
    }
}
