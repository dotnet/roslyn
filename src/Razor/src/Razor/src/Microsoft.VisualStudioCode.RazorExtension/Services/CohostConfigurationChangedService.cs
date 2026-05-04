// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

namespace Microsoft.VisualStudioCode.RazorExtension.Endpoints;

[Shared]
[Export(typeof(ICohostConfigurationChangedService))]
[Export(typeof(IRazorCohostStartupService))]
[method: ImportingConstructor]
internal sealed class CohostConfigurationChangedService(
    IClientSettingsManager clientSettingsManager,
    ILoggerFactory loggerFactory) : ICohostConfigurationChangedService, IRazorCohostStartupService
{
    private readonly IClientSettingsManager _clientSettingsManager = clientSettingsManager;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<CohostConfigurationChangedService>();

    public int Order => WellKnownStartupOrder.Default;

    public Task StartupAsync(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext, CancellationToken cancellationToken)
    {
        return RefreshOptionsAsync(requestContext, cancellationToken);
    }

    public Task OnConfigurationChangedAsync(RazorCohostRequestContext requestContext, CancellationToken cancellationToken)
    {
        return RefreshOptionsAsync(requestContext, cancellationToken);
    }

    private async Task RefreshOptionsAsync(RazorCohostRequestContext requestContext, CancellationToken cancellationToken)
    {
        _logger.LogDebug($"Refreshing options from client.");

        var razorClientLanguageServerManager = requestContext.GetRequiredService<IRazorClientLanguageServerManager>();

        var configurationParams = new ConfigurationParams()
        {
            Items = [
                //TODO: new ConfigurationItem { Section = "razor.format.enable" },
                new ConfigurationItem { Section = "razor.format.code_block_brace_on_next_line" },
                new ConfigurationItem { Section = "razor.format.attribute_indent_style" },
                new ConfigurationItem { Section = "razor.completion.commit_elements_with_space" },
                // Note: VS Code settings use snake_case, so this is "auto_closing_tags" not "autoClosingTags"
                // TypeScript code in the VS Code extension converts between camelCase and snake_case
                new ConfigurationItem { Section = "html.auto_closing_tags" },
            ]
        };

        var options = await razorClientLanguageServerManager.SendRequestAsync<ConfigurationParams, JsonArray>(
            Methods.WorkspaceConfigurationName,
            configurationParams,
            cancellationToken).ConfigureAwait(false);

        var current = _clientSettingsManager.GetClientSettings().AdvancedSettings;
        var settings = UpdateSettingsFromJson(current, options);

        _clientSettingsManager.Update(settings);
    }

    private static ClientAdvancedSettings UpdateSettingsFromJson(ClientAdvancedSettings settings, JsonArray jsonArray)
    {
        return settings with
        {
            CodeBlockBraceOnNextLine = GetBooleanOptionValue(TryGetElement(jsonArray, 0), settings.CodeBlockBraceOnNextLine),
            AttributeIndentStyle = GetEnumOptionValue(TryGetElement(jsonArray, 1), settings.AttributeIndentStyle),
            CommitElementsWithSpace = GetBooleanOptionValue(TryGetElement(jsonArray, 2), settings.CommitElementsWithSpace),
            AutoClosingTags = GetBooleanOptionValue(TryGetElement(jsonArray, 3), settings.AutoClosingTags),
        };
    }

    private static JsonNode? TryGetElement(JsonArray jsonArray, int index)
    {
        if (index < jsonArray.Count)
        {
            return jsonArray[index];
        }

        return null;
    }

    private static bool GetBooleanOptionValue(JsonNode? jsonNode, bool defaultValue)
    {
        if (jsonNode is null)
        {
            return defaultValue;
        }

        return jsonNode.ToString() == "true";
    }

    private static T GetEnumOptionValue<T>(JsonNode? jsonNode, T defaultValue) where T : struct
    {
        if (jsonNode is null)
        {
            return defaultValue;
        }

        if (Enum.TryParse<T>(jsonNode.GetValue<string>(), ignoreCase: true, out var value))
        {
            return value;
        }

        return defaultValue;
    }

    public static class TestAccessor
    {
        public static ClientAdvancedSettings UpdateSettingsFromJson(ClientAdvancedSettings settigns, JsonArray jsonArray)
            => CohostConfigurationChangedService.UpdateSettingsFromJson(settigns, jsonArray);
    }
}
