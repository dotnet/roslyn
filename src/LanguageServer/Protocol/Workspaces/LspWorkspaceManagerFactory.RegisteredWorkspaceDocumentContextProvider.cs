// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal partial class LspWorkspaceManagerFactory
{
    private sealed class RegisteredWorkspaceDocumentContextProvider(LspWorkspaceManager manager) : ILspDocumentContextProvider
    {
        public ValueTask<(Workspace workspace, Solution solution, TextDocument document)?> TryGetDocumentContextAsync(
            LspDocumentContextLookupContext context)
            => manager.TryGetRegisteredDocumentContextAsync(context);
    }
}
