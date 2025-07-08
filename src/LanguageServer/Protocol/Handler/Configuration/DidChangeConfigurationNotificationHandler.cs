// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Configuration;

[Method(Methods.WorkspaceDidChangeConfigurationName)]
internal sealed partial class DidChangeConfigurationNotificationHandler : ILspServiceNotificationHandler<LSP.DidChangeConfigurationParams>, IOnInitialized
{
    private bool _supportWorkspaceConfiguration;
    private readonly ILspLogger _lspLogger;
    private readonly IGlobalOptionService _globalOptionService;
    private readonly IClientLanguageServerManager _clientLanguageServerManager;
    private readonly Guid _registrationId;

    /// <summary>
    /// All the <see cref="ConfigurationItem.Section"/> needs to be refreshed from the client. 
    /// </summary>
    private readonly ImmutableArray<ConfigurationItem> _configurationItems;

    /// <summary>
    /// The matching option and its language name needs to be refreshed. The order matches <see cref="_configurationItems"/> sent to the client.
    /// LanguageName would be null if the option is <see cref="ISingleValuedOption"/>.
    /// </summary>
    private readonly ImmutableArray<(IOption2 option, string? lanugageName)> _optionsAndLanguageNamesToRefresh;

    private static readonly ImmutableDictionary<string, string> s_languageNameToPrefix = ImmutableDictionary<string, string>.Empty
        .Add(LanguageNames.CSharp, "csharp")
        .Add(LanguageNames.VisualBasic, "visual_basic");

    public static readonly ImmutableArray<string> SupportedLanguages = [LanguageNames.CSharp, LanguageNames.VisualBasic];

    public DidChangeConfigurationNotificationHandler(
        ILspLogger logger,
        IGlobalOptionService globalOptionService,
        IClientLanguageServerManager clientLanguageServerManager)
    {
        _lspLogger = logger;
        _globalOptionService = globalOptionService;
        _clientLanguageServerManager = clientLanguageServerManager;
        _registrationId = Guid.NewGuid();
        _configurationItems = GenerateGlobalConfigurationItems();
        _optionsAndLanguageNamesToRefresh = GenerateOptionsNeedsToRefresh();
        RoslynDebug.Assert(_configurationItems.Length == _optionsAndLanguageNamesToRefresh.Length);
    }

    public bool MutatesSolutionState => true;

    public bool RequiresLSPSolution => false;

    public async Task HandleNotificationAsync(DidChangeConfigurationParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        await RefreshOptionsAsync(cancellationToken).ConfigureAwait(false);

        var onChangedList = requestContext.GetRequiredServices<IOnConfigurationChanged>();
        foreach (var onConfigurationChanged in onChangedList)
        {
            await onConfigurationChanged.OnConfigurationChangedAsync(requestContext, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RefreshOptionsAsync(CancellationToken cancellationToken)
    {
        // We rely on the workspace/configuration to get the option values. If client doesn't support this, don't update.
        if (!_supportWorkspaceConfiguration)
        {
            return;
        }

        var configurationsFromClient = await GetConfigurationsAsync(cancellationToken).ConfigureAwait(false);
        if (configurationsFromClient.IsEmpty)
        {
            // Failed to get values from client, do nothing.
            return;
        }

        // We always fetch VB and C# value from client if the option is IPerLanguageValuedOption.
        RoslynDebug.Assert(configurationsFromClient.Length == SupportedOptions.Sum(option => option is IPerLanguageValuedOption ? 2 : 1));

        // LSP ensures the order of result from client should match the order we sent from server.
        using var _ = ArrayBuilder<KeyValuePair<OptionKey2, object?>>.GetInstance(out var optionsToUpdate);

        for (var i = 0; i < configurationsFromClient.Length; i++)
        {
            var valueFromClient = configurationsFromClient[i];
            var (option, languageName) = _optionsAndLanguageNamesToRefresh[i];

            // If option doesn't exist in the client, don't try to update the option.
            if (!string.IsNullOrEmpty(valueFromClient))
            {
                if (option.Definition.Serializer.TryParse(valueFromClient, out var parsedValue))
                {
                    optionsToUpdate.Add(KeyValuePair.Create(new OptionKey2(option, language: languageName), parsedValue));
                }
                else
                {
                    _lspLogger.LogWarning($"Failed to parse '{valueFromClient}' to type: '{option.Type.Name}'. '{option.Name}' would not be updated.");
                }
            }
        }

        _globalOptionService.SetGlobalOptions(optionsToUpdate.ToImmutable());
    }

    private async Task<ImmutableArray<string?>> GetConfigurationsAsync(CancellationToken cancellationToken)
    {
        // Attempt to get configurations from the client.  If this throws we'll get NFW reports.
        var configurationParams = new ConfigurationParams() { Items = _configurationItems.AsArray() };
        var options = await _clientLanguageServerManager.SendRequestAsync<ConfigurationParams, JsonArray>(
            Methods.WorkspaceConfigurationName, configurationParams, cancellationToken).ConfigureAwait(false);

        // Failed to get result from client.
        Contract.ThrowIfNull(options);
        var converted = options.SelectAsArray(token => token?.ToString());
        return converted;
    }

    private static ImmutableArray<(IOption2 option, string? langaugeName)> GenerateOptionsNeedsToRefresh()
    {
        using var _ = ArrayBuilder<(IOption2, string?)>.GetInstance(out var builder);
        foreach (var option in SupportedOptions)
        {
            if (option is IPerLanguageValuedOption)
            {
                foreach (var language in SupportedLanguages)
                {
                    builder.Add((option, language));
                }
            }
            else
            {
                builder.Add((option, null));
            }
        }

        return builder.ToImmutableAndClear();
    }

    /// <summary>
    /// Generate the configuration items send to the client.
    /// For each option, generate its full name. If the option is <see cref="ISingleValuedOption"/> it's is what we sent to the client.
    /// If it is <see cref="IPerLanguageValuedOption"/>, then generate two configurationItems with prefix visual_basic and csharp.
    /// </summary>
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
                        Section = string.Concat(s_languageNameToPrefix[language], '|', fullOptionName),
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

        return builder.ToImmutableAndClear();
    }

    /// <summary>
    /// Generate the full name of <param name="option"/>.
    /// It would be in the format like {optionGroupName}.{OptionName}
    /// </summary>
    /// <remarks>
    /// Example:Full name of <see cref="ImplementTypeOptionsStorage.InsertionBehavior"/> would be:
    /// implement_type.dotnet_member_insertion_location
    /// </remarks>
    internal static string GenerateFullNameForOption(IOption2 option)
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

        using var _ = PooledStringBuilder.GetInstance(out var stringBuilder);
        while (!stack.IsEmpty())
        {
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Append('.');
            }

            stringBuilder.Append(stack.Pop());
        }

        return stringBuilder.ToString();
    }
}
