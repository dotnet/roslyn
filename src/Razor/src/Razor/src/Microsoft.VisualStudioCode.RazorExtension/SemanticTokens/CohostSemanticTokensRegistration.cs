// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Text.Json;
using Microsoft.AspNetCore.Razor.LanguageServer.Hosting;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.SemanticTokens;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

[Shared]
[Export(typeof(IDynamicRegistrationProvider))]
[method: ImportingConstructor]
internal sealed class CohostSemanticTokensRegistration(ISemanticTokensLegendService semanticTokensLegendService) : IDynamicRegistrationProvider
{
    private readonly ISemanticTokensLegendService _semanticTokensLegendService = semanticTokensLegendService;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext)
    {
        if (clientCapabilities.TextDocument?.SemanticTokens?.DynamicRegistration == true)
        {
            var semanticTokensRefreshQueue = requestContext.GetRequiredService<IRazorSemanticTokensRefreshQueue>();
            var clientCapabilitiesString = JsonSerializer.Serialize(clientCapabilities);
            semanticTokensRefreshQueue.Initialize(clientCapabilitiesString);

            return [new Registration()
            {
                Method = Methods.TextDocumentSemanticTokensName,
                RegisterOptions = new SemanticTokensRegistrationOptions()
                    .EnableSemanticTokens(_semanticTokensLegendService)
            }];
        }

        return [];
    }
}