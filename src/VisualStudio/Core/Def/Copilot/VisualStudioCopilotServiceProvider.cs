// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Copilot;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Copilot;

[ExportWorkspaceService(typeof(ICopilotServiceProvider), ServiceLayer.Host), Shared]
internal sealed class VisualStudioCopilotServiceProvider : ICopilotServiceProvider
{
    private readonly IVsService<IBrokeredServiceContainer> _brokeredServiceContainer;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioCopilotServiceProvider(IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> brokeredServiceContainer)
    {
        _brokeredServiceContainer = brokeredServiceContainer;
    }

    public Task<ImmutableArray<string>?> SendOneOffRequestAsync(ImmutableArray<string> promptParts, CancellationToken cancellationToken)
        => SendOneOffRequestSafeAsync(promptParts, cancellationToken);

    // Guard against when Copilot chat is not installed
    [MethodImpl(MethodImplOptions.NoInlining)]
    private async Task<ImmutableArray<string>?> SendOneOffRequestSafeAsync(ImmutableArray<string> promptParts, CancellationToken cancellationToken)
    {
        try
        {
            var serviceContainer = await _brokeredServiceContainer.GetValueAsync(cancellationToken).ConfigureAwait(false);
            var serviceBroker = serviceContainer.GetFullAccessServiceBroker();

            using (var copilotService = await serviceBroker.GetProxyAsync<ICopilotService>(CopilotDescriptors.CopilotService, cancellationToken).ConfigureAwait(false))
            {
                if (copilotService is null || !await copilotService.CheckAvailabilityAsync(cancellationToken).ConfigureAwait(false))
                    return null;

                using var session = await copilotService.GetCopilotSessionAsync(options: new() { ProvideUI = false }, cancellationToken).ConfigureAwait(false);
                var request = new CopilotRequest()
                {
                    Intent = CopilotIntent.None,
                    Content = promptParts.SelectAsArray((s, i) => new CopilotContentTextPart(new CopilotContentPartId(i), s))
                };

                var response = await session.SendRequestAsync(request, cancellationToken).ConfigureAwait(false);
                if (response.Status == CopilotResponseStatus.Success)
                {
                    return response.Content.SelectAsArray(c => c switch
                    {
                        CopilotContentTextPart textPart => textPart.Content,
                        _ => string.Empty
                    });
                }

                return null;
            }
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
            return null;
        }
    }
}
