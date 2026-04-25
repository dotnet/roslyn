// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

[Export(typeof(ICohostStartupService))]
[method: ImportingConstructor]
internal sealed class CohostStartupService(
    [ImportMany] IEnumerable<Lazy<IRazorCohostStartupService>> lazyStartupServices,
    ILoggerFactory loggerFactory) : ICohostStartupService
{
    private readonly ImmutableArray<Lazy<IRazorCohostStartupService>> _lazyStartupServices = [.. lazyStartupServices];
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<CohostStartupService>();

    public async Task StartupAsync(string clientCapabilitiesString, RazorCohostRequestContext requestContext, CancellationToken cancellationToken)
    {
        var clientCapabilities = JsonSerializer.Deserialize<VSInternalClientCapabilities>(clientCapabilitiesString, JsonHelpers.JsonSerializerOptions) ?? new();

        var providers = _lazyStartupServices.SelectAndOrderByAsArray(p => p.Value, p => p.Order);

        foreach (var provider in providers)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Razor extension startup cancelled.");
                return;
            }

            try
            {
                await provider.StartupAsync(clientCapabilities, requestContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, $"Error initializing Razor startup service '{provider.GetType().Name}'");
            }
        }

        _logger.LogInformation($"Razor extension startup finished.");
    }
}
