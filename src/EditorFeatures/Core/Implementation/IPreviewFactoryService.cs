﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IPreviewFactoryService
    {
        SolutionPreviewResult GetSolutionPreviews(Solution oldSolution, Solution newSolution, CancellationToken cancellationToken);
        SolutionPreviewResult GetSolutionPreviews(Solution oldSolution, Solution newSolution, double zoomLevel, CancellationToken cancellationToken);

        Task<object> CreateAddedDocumentPreviewViewAsync(Document document, CancellationToken cancellationToken);
        Task<object> CreateAddedDocumentPreviewViewAsync(Document document, double zoomLevel, CancellationToken cancellationToken);

        Task<object> CreateChangedDocumentPreviewViewAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken);
        Task<object> CreateChangedDocumentPreviewViewAsync(Document oldDocument, Document newDocument, double zoomLevel, CancellationToken cancellationToken);

        Task<object> CreateRemovedDocumentPreviewViewAsync(Document document, CancellationToken cancellationToken);
        Task<object> CreateRemovedDocumentPreviewViewAsync(Document document, double zoomLevel, CancellationToken cancellationToken);
    }
}
