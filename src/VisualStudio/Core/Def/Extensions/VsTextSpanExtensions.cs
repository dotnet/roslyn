// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Extensions
{
    internal static class VsTextSpanExtensions
    {
        public static bool TryMapSpanFromSecondaryBufferToPrimaryBuffer(this VsTextSpan spanInSecondaryBuffer, Workspace workspace, DocumentId documentId, out VsTextSpan spanInPrimaryBuffer)
        {
            spanInPrimaryBuffer = default;

            if (workspace is not VisualStudioWorkspaceImpl visualStudioWorkspace)
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
