// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Extensions
{
    internal static class VsTextSpanExtensions
    {
        public static bool TryMapSpanFromSecondaryBufferToPrimaryBuffer(this VsTextSpan spanInSecondaryBuffer, Microsoft.CodeAnalysis.Workspace workspace, DocumentId documentId, out VsTextSpan spanInPrimaryBuffer)
        {
            spanInPrimaryBuffer = default;

            if (!(workspace is VisualStudioWorkspaceImpl visualStudioWorkspace))
            {
                return false;
            }

            var containedDocument = visualStudioWorkspace.TryGetContainedDocument(documentId);
            if (containedDocument == null)
            {
                return false;
            }

            var bufferCoordinator = containedDocument.BufferCoordinator;

            var primary = new VsTextSpan[1];
            var hresult = bufferCoordinator.MapSecondaryToPrimarySpan(spanInSecondaryBuffer, primary);

            spanInPrimaryBuffer = primary[0];

            return ErrorHandler.Succeeded(hresult);
        }
    }
}
