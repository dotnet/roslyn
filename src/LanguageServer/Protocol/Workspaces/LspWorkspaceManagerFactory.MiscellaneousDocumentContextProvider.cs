// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal partial class LspWorkspaceManagerFactory
{
    private sealed class MiscellaneousDocumentContextProvider(ILspMiscellaneousFilesWorkspaceProvider? miscellaneousFilesWorkspaceProvider) : ILspDocumentContextProvider
    {
        public async ValueTask<(Workspace workspace, Solution solution, TextDocument document)?> TryGetDocumentContextAsync(
            LspDocumentContextLookupContext context)
        {
            if (miscellaneousFilesWorkspaceProvider is null ||
                !context.TrackedDocuments.TryGetValue(context.TextDocumentIdentifier.DocumentUri, out var trackedDocument))
            {
                return null;
            }

            var document = await miscellaneousFilesWorkspaceProvider.AddDocumentAsync(
                context.TextDocumentIdentifier.DocumentUri, trackedDocument).ConfigureAwait(false);
            return document is null
                ? null
                : (document.Project.Solution.Workspace, document.Project.Solution, document);
        }
    }
}
