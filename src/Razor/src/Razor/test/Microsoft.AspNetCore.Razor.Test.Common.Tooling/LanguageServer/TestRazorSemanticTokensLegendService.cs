// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.LanguageServer.Test;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.SemanticTokens;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic;

internal class TestRazorSemanticTokensLegendService(IClientCapabilitiesService clientCapabilitiesService) : AbstractRazorSemanticTokensLegendService(clientCapabilitiesService)
{
    private static readonly TestRazorSemanticTokensLegendService s_vsInstance = new(new TestClientCapabilitiesService(new VSInternalClientCapabilities() { SupportsVisualStudioExtensions = true }));
    private static readonly TestRazorSemanticTokensLegendService s_vsCodeInstance = new(new TestClientCapabilitiesService(new VSInternalClientCapabilities() { SupportsVisualStudioExtensions = false }));

    public static ISemanticTokensLegendService GetInstance(bool supportsVSExtensions)
        => supportsVSExtensions
            ? s_vsInstance
            : s_vsCodeInstance;
}
