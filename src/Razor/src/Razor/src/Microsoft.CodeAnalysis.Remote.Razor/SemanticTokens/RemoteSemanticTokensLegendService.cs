// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.SemanticTokens;
using SemanticTokenModifiers = Microsoft.CodeAnalysis.Razor.SemanticTokens.SemanticTokenModifiers;
using SemanticTokenTypes = Microsoft.CodeAnalysis.Razor.SemanticTokens.SemanticTokenTypes;

namespace Microsoft.CodeAnalysis.Remote.Razor.SemanticTokens;

[Shared]
[Export(typeof(ISemanticTokensLegendService))]
[Export(typeof(ILspLifetimeService))]
internal sealed class RemoteSemanticTokensLegendService : ISemanticTokensLegendService, ILspLifetimeService
{
    private SemanticTokenModifiers _tokenModifiers = null!;
    private SemanticTokenTypes _tokenTypes = null!;

    public SemanticTokenModifiers TokenModifiers => _tokenModifiers;

    public SemanticTokenTypes TokenTypes => _tokenTypes;

    public void OnLspInitialized(RemoteClientLSPInitializationOptions options)
    {
        _tokenTypes = new SemanticTokenTypes(options.TokenTypes);
        _tokenModifiers = new SemanticTokenModifiers(options.TokenModifiers);
    }
}
