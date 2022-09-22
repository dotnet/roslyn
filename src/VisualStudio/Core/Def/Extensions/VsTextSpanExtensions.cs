// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Extensions
{
    internal static class VsTextSpanExtensions
    {
        public static async Task<VsTextSpan?> MapSpanFromSecondaryBufferToPrimaryBufferAsync(
            this VsTextSpan spanInSecondaryBuffer,
            IThreadingContext threadingContext,
            Workspace workspace,
            DocumentId documentId,
            CancellationToken cancellationToken)
        {
            if (workspace is not VisualStudioWorkspaceImpl visualStudioWorkspace)
                return null;

            var containedDocument = visualStudioWorkspace.TryGetContainedDocument(documentId);
            if (containedDocument == null)
                return null;

            await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var bufferCoordinator = containedDocument.BufferCoordinator;

            var primary = new VsTextSpan[1];
            var hresult = bufferCoordinator.MapSecondaryToPrimarySpan(spanInSecondaryBuffer, primary);

            var result = primary[0];

            return ErrorHandler.Succeeded(hresult) ? result : null;
        }
    }
}
