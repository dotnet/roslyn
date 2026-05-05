// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.SemanticTokens;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

[Export(typeof(ISemanticTokensLegendService))]
[method: ImportingConstructor]
internal class CohostSemanticTokensLegendService(IClientCapabilitiesService clientCapabilitiesService)
    : AbstractRazorSemanticTokensLegendService(clientCapabilitiesService)
{
}
