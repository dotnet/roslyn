// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal interface INavigateToPreviewService : IWorkspaceService
    {
        int GetProvisionalViewingStatus(Document document);
        bool CanPreview(Document document);
        void PreviewItem(INavigateToItemDisplay itemDisplay);
    }
}
