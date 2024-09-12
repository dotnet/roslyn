// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.InlayHint
{
    internal class InlayHintRefreshQueue : AbstractRefreshQueue
    {
        private readonly IGlobalOptionService _globalOptionService;

        public InlayHintRefreshQueue(
            IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
            LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            IGlobalOptionService globalOptionService,
            LspWorkspaceManager lspWorkspaceManager,
            IClientLanguageServerManager notificationManager)
            : base(asynchronousOperationListenerProvider, lspWorkspaceRegistrationService, lspWorkspaceManager, notificationManager)
        {
            _globalOptionService = globalOptionService;
            _globalOptionService.AddOptionChangedHandler(this, OnOptionChanged);
        }

        public override void Dispose()
        {
            base.Dispose();
            _globalOptionService.RemoveOptionChangedHandler(this, OnOptionChanged);
        }

        private void OnOptionChanged(object? sender, OptionChangedEventArgs e)
        {
            if (e.Option.Equals(InlineHintsOptionsStorage.EnabledForParameters) ||
                e.Option.Equals(InlineHintsOptionsStorage.ForIndexerParameters) ||
                e.Option.Equals(InlineHintsOptionsStorage.ForLiteralParameters) ||
                e.Option.Equals(InlineHintsOptionsStorage.ForOtherParameters) ||
                e.Option.Equals(InlineHintsOptionsStorage.ForObjectCreationParameters) ||
                e.Option.Equals(InlineHintsOptionsStorage.SuppressForParametersThatDifferOnlyBySuffix) ||
                e.Option.Equals(InlineHintsOptionsStorage.SuppressForParametersThatMatchArgumentName) ||
                e.Option.Equals(InlineHintsOptionsStorage.SuppressForParametersThatMatchMethodIntent) ||
                e.Option.Equals(InlineHintsOptionsStorage.EnabledForTypes) ||
                e.Option.Equals(InlineHintsOptionsStorage.ForImplicitVariableTypes) ||
                e.Option.Equals(InlineHintsOptionsStorage.ForLambdaParameterTypes) ||
                e.Option.Equals(InlineHintsOptionsStorage.ForImplicitObjectCreation))
            {
                EnqueueRefreshNotification(documentUri: null);
            }
        }

        protected override string GetFeatureAttribute()
            => FeatureAttribute.InlineHints;

        protected override bool? GetRefreshSupport(ClientCapabilities clientCapabilities)
        {
            return clientCapabilities.Workspace?.InlayHint?.RefreshSupport;
        }

        protected override string GetWorkspaceRefreshName()
        {
            return Methods.WorkspaceInlayHintRefreshName;
        }
    }
}
