// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Configuration
{
    [Method(Methods.WorkspaceDidChangeConfigurationName)]
    internal partial class DidChangeConfigurationNotificationHandler : ILspServiceNotificationHandler<LSP.DidChangeConfigurationParams>, IOnInitialized
    {
        private readonly ILspLogger _lspLogger;
        private readonly IGlobalOptionService _globalOptionService;
        private readonly IClientLanguageServerManager _clientLanguageServerManager;
        private readonly IAsynchronousOperationListener _asynchronousOperationListener;
        private readonly Guid _registrationId;
        public static readonly ImmutableArray<string> s_supportedLanguages = ImmutableArray.Create(LanguageNames.CSharp, LanguageNames.VisualBasic);

        public DidChangeConfigurationNotificationHandler(
            ILspLogger logger,
            IGlobalOptionService globalOptionService,
            IClientLanguageServerManager clientLanguageServerManager,
            IAsynchronousOperationListener asynchronousOperationListener)
        {
            _lspLogger = logger;
            _globalOptionService = globalOptionService;
            _clientLanguageServerManager = clientLanguageServerManager;
            _registrationId = Guid.NewGuid();
            _asynchronousOperationListener = asynchronousOperationListener;
        }

        public bool MutatesSolutionState => true;

        public bool RequiresLSPSolution => false;

        public async Task HandleNotificationAsync(DidChangeConfigurationParams request, RequestContext requestContext, CancellationToken cancellationToken)
        {
            using var _ = _asynchronousOperationListener.BeginAsyncOperation(Methods.WorkspaceDidChangeConfigurationName);
            await RefreshOptionsAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task RefreshOptionsAsync(CancellationToken cancellationToken)
        {
            var configurationItems = SupportedOptions.SelectAsArray(
                option => new ConfigurationItem() { ScopeUri = null, Section = GenerateSection(option) });
            var configurationsFromClient = await GetConfigurationsAsync(configurationItems, cancellationToken).ConfigureAwait(false);
            if (configurationsFromClient.IsEmpty)
            {
                // Failed to get values from client, do nothing.
                return;
            }

            RoslynDebug.Assert(configurationsFromClient.Length == SupportedOptions.Length);
            for (var i = 0; i < configurationsFromClient.Length; i++)
            {
                var option = SupportedOptions[i];
                var configurationValue = configurationsFromClient[i];
                if (option is IPerLanguageValuedOption)
                {
                    // It is expected for IPerLanguageOptions, client should gives us the result for both VB and CSharp.
                    var configurationObject = JsonConvert.DeserializeObject<JObject>(configurationValue);
                    foreach (var languageName in s_supportedLanguages)
                    {
                        var languageOptionValue = configurationObject?.Value<string>(languageName);
                        if (languageOptionValue != null && TryParseValueFromClient(languageOptionValue, option, out var perLanguageResult))
                        {
                            _globalOptionService.SetGlobalOption(new OptionKey2(option, languageName), perLanguageResult);
                        }
                        else
                        {
                            _lspLogger.LogError($"Failed to update option: {option.Name} for language: {languageName}.");
                        }
                    }
                }
                else
                {
                    if (TryParseValueFromClient(configurationValue.ToString(), option, out var result))
                    {
                        _globalOptionService.SetGlobalOption(new OptionKey2(option, language: null), result);
                    }
                    else
                    {
                        _lspLogger.LogError($"Failed to update option: {option.Name}.");
                    }
                }
            }
        }

        private async Task<ImmutableArray<string>> GetConfigurationsAsync(ImmutableArray<ConfigurationItem> configurationItems, CancellationToken cancellationToken)
        {
            try
            {
                var configurationParams = new ConfigurationParams() { Items = configurationItems.AsArray() };
                var options = await _clientLanguageServerManager.SendRequestAsync<ConfigurationParams, JArray>(
                    Methods.WorkspaceConfigurationName, configurationParams, cancellationToken).ConfigureAwait(false);

                // Failed to get result from client.
                Contract.ThrowIfNull(options);
                return options.SelectAsArray(token => token.ToString());
            }
            catch (Exception e)
            {
                _lspLogger.LogException(e, $"Exception occurs when make {Methods.WorkspaceConfigurationName}.");
            }

            return ImmutableArray<string>.Empty;
        }

        private bool TryParseValueFromClient(string value, IOption2 option, out object? result)
        {
            if (!option.Definition.Serializer.TryParse(value, out result))
            {
                _lspLogger.LogError($"Failed to parse client value: {value} to type: {option.Definition.Type}.");
                return false;
            }

            return true;
        }

        private static string GenerateSection(IOption2 option)
        {
            using var pooledStack = SharedPools.Default<Stack<string>>().GetPooledObject();
            var stack = pooledStack.Object;
            // Get the full name of option group, we are at the tail now, so use a stack to reverse it.
            var optionGroup = option.Definition.Group;
            while (optionGroup != null && optionGroup.Name != null)
            {
                stack.Push(optionGroup.Name);
                optionGroup = optionGroup.Parent;
            }

            var pooledStringBuilder = PooledStringBuilder.GetInstance();
            var stringBuilder = pooledStringBuilder.Builder;
            string groupFullName;
            try
            {
                while (!stack.IsEmpty())
                {
                    if (stringBuilder.Length > 0)
                    {
                        stringBuilder.Append('.');
                    }

                    stringBuilder.Append(stack.Pop());
                }
                groupFullName = stringBuilder.ToString();
            }
            finally
            {
                pooledStringBuilder.Free();
            }

            // All options send to the client should have group name and config name.
            RoslynDebug.Assert(!string.IsNullOrEmpty(groupFullName));
            RoslynDebug.Assert(!string.IsNullOrEmpty(option.Definition.ConfigName));
            return string.Concat(groupFullName, '.', option.Definition.ConfigName);
        }
    }
}
