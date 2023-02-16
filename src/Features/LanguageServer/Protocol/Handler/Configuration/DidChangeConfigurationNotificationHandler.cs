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

        [LanguageServerEndpoint(Methods.WorkspaceDidChangeConfigurationName)]
        public Task HandleNotificationAsync(DidChangeConfigurationParams request, RequestContext requestContext, CancellationToken cancellationToken)
            => RefreshOptionsAsync(cancellationToken);

        private async Task RefreshOptionsAsync(CancellationToken cancellationToken)
        {
            var globalConfigurationItems = s_supportedGlobalOptions.SelectAsArray(option => new ConfigurationItem() { ScopeUri = null, Section = option.Definition.ConfigName });
            var perLanguageConfigurationItems = s_supportedPerLanguageOptions.SelectAsArray(option => new ConfigurationItem() { ScopeUri = null, Section = option.Definition.ConfigName });
            var allConfigurationItems = globalConfigurationItems.Concat(perLanguageConfigurationItems);

            var configurationsFromClient = await GetConfigurationsAsync(allConfigurationItems, cancellationToken).ConfigureAwait(false);
            for (var i = 0; i < globalConfigurationItems.Length; i++)
            {
                var globalOptions = s_supportedGlobalOptions[i];
                var configurationValue = configurationsFromClient[i];
                globalOptions.WriteToGlobalOptionService(_globalOptionService, configurationValue);
            }

            for (var i = 0; i < perLanguageConfigurationItems.Length; i++)
            {
                var perLanguageOptions = s_supportedPerLanguageOptions[i];
                var configurationValue = configurationsFromClient[i + globalConfigurationItems.Length];
                perLanguageOptions.WriteToGlobalOptionService(_globalOptionService, LanguageNames.CSharp, configurationValue);
            }
        }

        private async Task<ImmutableArray<string>> GetConfigurationsAsync(ImmutableArray<ConfigurationItem> configurationItems, CancellationToken cancellationToken)
        {
            try
            {
                var configurationParams = new ConfigurationParams() { Items = configurationItems.AsArray() };
                var options = await _clientLanguageServerManager.SendRequestAsync<ConfigurationParams, JArray>(
                    Methods.WorkspaceConfigurationName, configurationParams, cancellationToken).ConfigureAwait(false);

                if (options == null)
                {
                    _lspLogger.LogError($"Failed to get the response of {Methods.WorkspaceConfigurationName}.");
                    return ImmutableArray<string>.Empty;
                }

                if (options.Count != configurationItems.Length)
                {
                    _lspLogger.LogError($"Unexpected configuration number from the response of {Methods.WorkspaceConfigurationName}, expected: {configurationItems.Length}, actual: {options.Count}.");
                    return ImmutableArray<string>.Empty;
                }

                var optionStrings = options.SelectAsArray(token => token.Value<string>());
                if (optionStrings.Contains(null))
                {
                    _lspLogger.LogError($"Configuration from client is null. The request is {configurationItems[optionStrings.IndexOf(null)]}.");
                    return ImmutableArray<string>.Empty;
                }
            }
            catch (Exception e)
            {
                _lspLogger.LogException(e, $"Exception occurs when make {Methods.WorkspaceConfigurationName}.");
            }

            return ImmutableArray<string>.Empty;
        }
    }
}
