// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions
{
    internal interface ICodeActionEditHandlerService
    {
        ITextBufferAssociatedViewService AssociatedViewService { get; }

        Task<SolutionPreviewResult?> GetPreviewsAsync(
            Workspace workspace, ImmutableArray<CodeActionOperation> operations, CancellationToken cancellationToken);

        Task<bool> ApplyAsync(
            Workspace workspace, Document? fromDocument,
            ImmutableArray<CodeActionOperation> operations,
            string title, IProgressTracker progressTracker,
            CancellationToken cancellationToken);
    }
}
