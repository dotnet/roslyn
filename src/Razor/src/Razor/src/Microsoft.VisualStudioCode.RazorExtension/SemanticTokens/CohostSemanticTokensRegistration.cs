// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.AspNetCore.Razor.LanguageServer.Hosting;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Razor.SemanticTokens;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

[Shared]
[Export(typeof(IDynamicRegistrationProvider))]
[method: ImportingConstructor]
internal sealed class CohostSemanticTokensRegistration(ISemanticTokensLegendService semanticTokensLegendService) : IDynamicRegistrationProvider
{
    private readonly ISemanticTokensLegendService _semanticTokensLegendService = semanticTokensLegendService;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RequestContext requestContext)
    {
        if (clientCapabilities.TextDocument?.SemanticTokens?.DynamicRegistration == true)
        {
            var semanticTokensRefreshQueue = requestContext.GetRequiredService<IRazorSemanticTokensRefreshQueue>();
            semanticTokensRefreshQueue.Initialize(clientCapabilities);

            // We prefer Range over Full for performance reasons, so only advertise full support if Range isn't
            // available. The Range capability is SumType<bool, object> which is why the check is a bit odd.
            var supportsSemanticTokensRange = clientCapabilities.TextDocument?.SemanticTokens?.Requests?.Range?.Value is not (false or null);

            return [new Registration()
            {
                Method = Methods.TextDocumentSemanticTokensName,
                RegisterOptions = new SemanticTokensRegistrationOptions()
                    .EnableSemanticTokens(_semanticTokensLegendService, supportsSemanticTokensRange)
            }];
        }

        return [];
    }
}
