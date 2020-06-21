// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface ICodeActionEditHandlerService
    {
        ITextBufferAssociatedViewService AssociatedViewService { get; }

        SolutionPreviewResult GetPreviews(
            Workspace workspace, ImmutableArray<CodeActionOperation> operations, CancellationToken cancellationToken);

        bool Apply(
            Workspace workspace, Document fromDocument,
            ImmutableArray<CodeActionOperation> operations,
            string title, IProgressTracker progressTracker,
            CancellationToken cancellationToken);
    }
}
