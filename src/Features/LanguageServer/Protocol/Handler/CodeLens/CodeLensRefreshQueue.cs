// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeLens
{
    internal class CodeLensRefreshQueue : AbstractRefreshQueue
    {
        public CodeLensRefreshQueue(
            IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
            LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            LspWorkspaceManager lspWorkspaceManager,
            IClientLanguageServerManager notificationManager)
            : base(asynchronousOperationListenerProvider, lspWorkspaceRegistrationService, lspWorkspaceManager, notificationManager)
        {
        }

        protected override string GetFeatureAttribute()
            => FeatureAttribute.CodeLens;

        protected override bool? GetRefreshSupport(ClientCapabilities clientCapabilities)
        {
            return clientCapabilities.Workspace?.CodeLens?.RefreshSupport;
        }

        protected override string GetWorkspaceRefreshName()
        {
            return Methods.WorkspaceCodeLensRefreshName;
        }
    }
}
