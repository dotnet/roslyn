// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
