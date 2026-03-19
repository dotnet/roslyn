// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.NavigateTo;

internal sealed class VisualStudioNavigateToPreviewService : INavigateToPreviewService
{
    public __VSPROVISIONALVIEWINGSTATUS GetProvisionalViewingStatus(INavigableItem.NavigableDocument document)
    {
        if (document.FilePath == null)
        {
            return __VSPROVISIONALVIEWINGSTATUS.PVS_Disabled;
        }

        return (__VSPROVISIONALVIEWINGSTATUS)VsShellUtilities.GetProvisionalViewingStatus(document.FilePath);
    }

    public bool CanPreview(Document document)
        => ContainedDocument.TryGetContainedDocument(document.Id) == null;

    public void PreviewItem(INavigateToItemDisplay itemDisplay)
    {
        // Because NavigateTo synchronously opens the file, and because
        // the NavigateTo UI automatically creates a NewDocumentStateScope,
        // preview can be accomplished by simply calling NavigateTo.

        // Navigation may fail to open the document, which can result in an exception
        // in expected cases if preview is not supported.  CallWithCOMConvention handles
        // non-critical exceptions
        ErrorHandler.CallWithCOMConvention(() => itemDisplay.NavigateTo());
    }
}
