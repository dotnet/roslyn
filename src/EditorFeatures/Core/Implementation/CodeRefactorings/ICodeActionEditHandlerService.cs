// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
            Workspace workspace, IEnumerable<CodeActionOperation> operations, CancellationToken cancellationToken);

        Task ApplyAsync(
            Workspace workspace, Document fromDocument,
            IEnumerable<CodeActionOperation> operations,
            string title, IProgressTracker progressTracker,
            CancellationToken cancellationToken);
    }
}