// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;

internal interface INavigateToPreviewService : IWorkspaceService
{
    __VSPROVISIONALVIEWINGSTATUS GetProvisionalViewingStatus(INavigableItem.NavigableDocument document);
    bool CanPreview(Document document);
    void PreviewItem(INavigateToItemDisplay itemDisplay);
}
