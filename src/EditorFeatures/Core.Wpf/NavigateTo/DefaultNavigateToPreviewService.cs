// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Navigation;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;

internal sealed class DefaultNavigateToPreviewService : INavigateToPreviewService
{
    public __VSPROVISIONALVIEWINGSTATUS GetProvisionalViewingStatus(INavigableItem.NavigableDocument document)
        => __VSPROVISIONALVIEWINGSTATUS.PVS_Disabled;

    public bool CanPreview(Document document)
        => true;

    public void PreviewItem(INavigateToItemDisplay itemDisplay)
    {
    }
}
