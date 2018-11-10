// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.Editor.Wrapping
{
    internal interface IWrapper
    {
        Task<ImmutableArray<CodeAction>> ComputeRefactoringsAsync(
            Document document, int position, SyntaxNode node, CancellationToken cancellationToken);
    }
}
