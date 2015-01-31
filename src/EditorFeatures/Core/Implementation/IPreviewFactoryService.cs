// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.VisualStudio.Text.Differencing;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IPreviewFactoryService
    {
        SolutionPreviewResult GetSolutionPreviews(Solution oldSolution, Solution newSolution, CancellationToken cancellationToken);
        SolutionPreviewResult GetSolutionPreviews(Solution oldSolution, Solution newSolution, double zoomLevel, CancellationToken cancellationToken);

        IWpfDifferenceViewer CreateAddedDocumentPreviewView(Document document, CancellationToken cancellationToken);
        IWpfDifferenceViewer CreateAddedDocumentPreviewView(Document document, double zoomLevel, CancellationToken cancellationToken);

        IWpfDifferenceViewer CreateChangedDocumentPreviewView(Document oldDocument, Document newDocument, CancellationToken cancellationToken);
        IWpfDifferenceViewer CreateChangedDocumentPreviewView(Document oldDocument, Document newDocument, double zoomLevel, CancellationToken cancellationToken);

        IWpfDifferenceViewer CreateRemovedDocumentPreviewView(Document document, CancellationToken cancellationToken);
        IWpfDifferenceViewer CreateRemovedDocumentPreviewView(Document document, double zoomLevel, CancellationToken cancellationToken);
    }
}
