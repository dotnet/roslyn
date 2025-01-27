// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.LanguageServer.Protocol;

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

        private void OnOptionChanged(object sender, object target, OptionChangedEventArgs e)
        {
            if (e.HasOption(static option =>
                    option.Equals(InlineHintsOptionsStorage.EnabledForParameters) ||
                    option.Equals(InlineHintsOptionsStorage.ForIndexerParameters) ||
                    option.Equals(InlineHintsOptionsStorage.ForLiteralParameters) ||
                    option.Equals(InlineHintsOptionsStorage.ForOtherParameters) ||
                    option.Equals(InlineHintsOptionsStorage.ForObjectCreationParameters) ||
                    option.Equals(InlineHintsOptionsStorage.SuppressForParametersThatDifferOnlyBySuffix) ||
                    option.Equals(InlineHintsOptionsStorage.SuppressForParametersThatMatchArgumentName) ||
                    option.Equals(InlineHintsOptionsStorage.SuppressForParametersThatMatchMethodIntent) ||
                    option.Equals(InlineHintsOptionsStorage.EnabledForTypes) ||
                    option.Equals(InlineHintsOptionsStorage.ForImplicitVariableTypes) ||
                    option.Equals(InlineHintsOptionsStorage.ForLambdaParameterTypes) ||
                    option.Equals(InlineHintsOptionsStorage.ForImplicitObjectCreation) ||
                    option.Equals(InlineHintsOptionsStorage.ForCollectionExpressions)))
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
