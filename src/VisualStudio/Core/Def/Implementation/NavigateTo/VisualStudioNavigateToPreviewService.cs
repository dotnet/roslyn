// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.NavigateTo
{
    internal sealed class VisualStudioNavigateToPreviewService : INavigateToPreviewService
    {
        public int GetProvisionalViewingStatus(Document document)
        {
            if (document.FilePath == null)
            {
                return (int)__VSPROVISIONALVIEWINGSTATUS.PVS_Disabled;
            }

            return (int)VsShellUtilities.GetProvisionalViewingStatus(document.FilePath);
        }

        public bool CanPreview(Document document)
        {
            if (!(document.Project.Solution.Workspace is VisualStudioWorkspaceImpl visualStudioWorkspace))
            {
                return false;
            }

            return visualStudioWorkspace.TryGetContainedDocument(document.Id) == null;
        }

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
}
