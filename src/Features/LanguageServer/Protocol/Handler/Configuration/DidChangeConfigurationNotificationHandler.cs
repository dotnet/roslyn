// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        private static readonly ImmutableArray<string> s_supportLanguages = ImmutableArray.Create(LanguageNames.CSharp);

        private static readonly ImmutableArray<ISingleValuedOption> s_supportedGlobalOptions = ImmutableArray.Create<ISingleValuedOption>(
            LspOptions.MaxCompletionListSize,
            LspOptions.LspSemanticTokensFeatureFlag,
            LspOptions.LspEditorFeatureFlag);

        private static readonly ImmutableArray<IPerLanguageValuedOption> s_supportedPerLanguageOptions = ImmutableArray<IPerLanguageValuedOption>.Empty;

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
            var globalPerLanguageConfigurations = s_supportedPerLanguageOptions.SelectManyAsArray(
                option => GetPerLanguageOptionNames(option).Select(name => new ConfigurationItem() { ScopeUri = null, Section = name }));
            var configurationItems = globalConfigurations.Concat(globalPerLanguageConfigurations);

            var configurationsFromClient = await GetConfigurationsAsync(configurationItems, cancellationToken).ConfigureAwait(false);

            for (var i = 0; i < globalConfigurations.Length; i++)
            {
                var globalOptions = s_supportedGlobalOptions[i];
                var configurationValue = configurationsFromClient[i];
                globalOptions.WriteToGlobalOptionService(_globalOptionService, configurationValue);
            }

            for (var i = globalConfigurations.Length; i < configurationsFromClient.Length; i++)
            {
                var languageName = s_supportLanguages[(i - globalConfigurations.Length) % s_supportLanguages.Length];
                var perLanguageOptionIndex = (i - globalConfigurations.Length) / s_supportLanguages.Length;
                var perLanguageGlobalOptions = s_supportedPerLanguageOptions[perLanguageOptionIndex];
                var configurationValue = configurationsFromClient[i];
                perLanguageGlobalOptions.WriteToGlobalOptionService(_globalOptionService, languageName, configurationValue);
            }

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


        private static IEnumerable<string> GetPerLanguageOptionNames(IPerLanguageValuedOption option)
            => s_supportLanguages.Select(language =>
            {
                if (language == LanguageNames.CSharp)
                    return string.Concat(OptionDefinition.CSharpConfigNamePrefix, ".", option.Definition.ConfigName);
                if (language == LanguageNames.VisualBasic)
                    return string.Concat(OptionDefinition.VisualBasicConfigNamePrefix, ".", option.Definition.ConfigName);

                throw ExceptionUtilities.UnexpectedValue(language);
            });
    }
}
