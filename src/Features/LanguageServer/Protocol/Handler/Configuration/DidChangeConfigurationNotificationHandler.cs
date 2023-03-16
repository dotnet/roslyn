// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
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

        /// <summary>
        /// All the global <see cref="ConfigurationItem.Section"/> needs to be refreshed from the client. 
        /// </summary>
        private readonly ImmutableArray<ConfigurationItem> _configurationItems;
        private static readonly ImmutableDictionary<string, string> s_languageNameToPrefix = ImmutableDictionary<string, string>.Empty
            .Add(LanguageNames.CSharp, "csharp")
            .Add(LanguageNames.VisualBasic, "visual_basic");

        public static readonly ImmutableArray<string> SupportedLanguages = ImmutableArray.Create(LanguageNames.CSharp, LanguageNames.VisualBasic);

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
            _configurationItems = GenerateGlobalConfigurationItems();
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
            var configurationsFromClient = await GetConfigurationsAsync(cancellationToken).ConfigureAwait(false);
            if (configurationsFromClient.IsEmpty)
            {
                // Failed to get values from client, do nothing.
                return;
            }

            // We always fetch VB and C# value from client if the option is IPerLanguageValuedOption.
            RoslynDebug.Assert(configurationsFromClient.Length == SupportedOptions.Sum(option => option is IPerLanguageValuedOption ? 2 : 1));
            var optionsToRefresh = SupportedOptions.SelectManyAsArray(option => option is IPerLanguageValuedOption
                ? SupportedLanguages.SelectAsArray(language => (option, language))
                : SpecializedCollections.SingletonEnumerable((option, string.Empty)));

            // LSP ensures the order of result from client should match the order we sent from server.
            for (var i = 0; i < configurationsFromClient.Length; i++)
            {
                var valueFromClient = configurationsFromClient[i];
                if (valueFromClient == string.Empty)
                {
                    // Configuration doesn't exist in client.
                    continue;
                }

                var (option, languageName) = optionsToRefresh[i];
                if (option is IPerLanguageValuedOption perLanguageValuedOption)
                {
                    SetOption(option, valueFromClient, languageName);
                }
                else
                {
                    SetOption(option, valueFromClient);
                }
            }
        }

        private void SetOption(IOption2 option, string valueFromClient, string? languageName = null)
        {
            if (option.Definition.Serializer.TryParse(valueFromClient, out var result))
            {
                if (option is IPerLanguageValuedOption && languageName != null)
                {
                    _globalOptionService.SetGlobalOption(new OptionKey2(option, language: languageName), result);
                }
                else
                {
                    _globalOptionService.SetGlobalOption(new OptionKey2(option, language: null), result);
                }
            }
        }

        private async Task<ImmutableArray<string>> GetConfigurationsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var configurationParams = new ConfigurationParams() { Items = _configurationItems.AsArray() };
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

        private static ImmutableArray<ConfigurationItem> GenerateGlobalConfigurationItems()
        {
            using var _ = ArrayBuilder<ConfigurationItem>.GetInstance(out var builder);
            foreach (var option in SupportedOptions)
            {
                var fullOptionName = GenerateFullNameForOption(option);
                if (option is IPerLanguageValuedOption)
                {
                    foreach (var language in SupportedLanguages)
                    {
                        builder.Add(new ConfigurationItem()
                        {
                            Section = string.Concat(s_languageNameToPrefix[language], '.', fullOptionName),
                        });
                    }
                }
                else
                {
                    builder.Add(new ConfigurationItem()
                    {
                        Section = fullOptionName,
                    });
                }
            }

            return builder.ToImmutable();
        }

        private static string GenerateFullNameForOption(IOption2 option)
        {
            var optionGroupName = GenerateOptionGroupName(option);
            // All options send to the client should have group name and config name.
            RoslynDebug.Assert(!string.IsNullOrEmpty(optionGroupName));
            RoslynDebug.Assert(!string.IsNullOrEmpty(option.Definition.ConfigName));
            return string.Concat(optionGroupName, '.', option.Definition.ConfigName);
        }

        private static string GenerateOptionGroupName(IOption2 option)
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

            return groupFullName;
        }
    }
}
