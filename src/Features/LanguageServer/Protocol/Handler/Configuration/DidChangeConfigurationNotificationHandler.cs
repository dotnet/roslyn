// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Configuration
{
    internal partial class DidChangeConfigurationNotificationHandler : ILspServiceNotificationHandler<LSP.DidChangeConfigurationParams>, IOnInitialized
    {
        private static readonly ImmutableArray<IOption2> s_supportedGlobalOptions = ImmutableArray.Create<IOption2>(
            LspOptions.MaxCompletionListSize,
            LspOptions.LspSemanticTokensFeatureFlag,
            LspOptions.LspEditorFeatureFlag);

        private readonly ILspLogger _lspLogger;
        private readonly IGlobalOptionService _globalOptionService;
        private readonly IClientLanguageServerManager _clientLanguageServerManager;
        private readonly Guid _registrationId;

        public DidChangeConfigurationNotificationHandler(ILspLogger logger, IGlobalOptionService globalOptionService, IClientLanguageServerManager clientLanguageServerManager)
        {
            _lspLogger = logger;
            _globalOptionService = globalOptionService;
            _clientLanguageServerManager = clientLanguageServerManager;
            _registrationId = Guid.NewGuid();
        }

        public bool MutatesSolutionState => false;

        public bool RequiresLSPSolution => true;

        [LanguageServerEndpoint(LSP.Methods.WorkspaceDidChangeConfigurationName)]
        public Task HandleNotificationAsync(DidChangeConfigurationParams request, RequestContext requestContext, CancellationToken cancellationToken)
            => RefreshOptionsAsync(cancellationToken);

        private async Task RefreshOptionsAsync(CancellationToken cancellationToken)
        {
            var globalConfigurations = s_supportedGlobalOptions.SelectAsArray(option => new ConfigurationItem() { ScopeUri = null, Section = option.Definition.ConfigName });
            var configurations = await GetConfigurationsAsync(globalConfigurations, cancellationToken).ConfigureAwait(false);
            // TODO: For .editorconfig options, there should be a document-based (uri) to options value service.
        }

        private async Task<ImmutableArray<string>> GetConfigurationsAsync(ImmutableArray<ConfigurationItem> configurationItems, CancellationToken cancellationToken)
        {
            try
            {
                var configurationParams = new ConfigurationParams() { Items = configurationItems.AsArray() };

                var options = await _clientLanguageServerManager.SendRequestAsync<ConfigurationParams, JArray>(
                    Methods.WorkspaceConfigurationName, configurationParams, cancellationToken).ConfigureAwait(false);

                if (options.Count != configurationItems.Length)
                {
                    _lspLogger.LogError($"Unexpected configuration number from the response of {Methods.WorkspaceConfigurationName}, expected: {configurationItems.Length}, actual: {options.Count}.");
                    return ImmutableArray<string>.Empty;
                }

                return options.SelectAsArray(token => token.Value<string>());
            }
            catch (Exception e)
            {
                _lspLogger.LogException(e, $"Exception occurs when make {Methods.WorkspaceConfigurationName}.");
            }

            return ImmutableArray<string>.Empty;
        }

        private void UpdateOption<T>(string value, IOption2<T> option)
        {
            if (option.Definition.Serializer.TryParse(value, out var result) || result is not T)
            {
            }
            else
            {
                _lspLogger.LogError($"Can't parse {result} from client to type: {}");
            }
        }
    }
}
