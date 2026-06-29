// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Razor.SemanticTokens;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Hosting;

internal static class LspInitializationHelpers
{
    public static CodeActionOptions EnableCodeActions(this CodeActionOptions options)
    {
        options.CodeActionKinds =
        [
            CodeActionKind.RefactorExtract,
            CodeActionKind.QuickFix,
            CodeActionKind.Refactor
        ];
        options.ResolveProvider = true;

        return options;
    }

    public static SemanticTokensOptions EnableSemanticTokens(this SemanticTokensOptions options, ISemanticTokensLegendService legend, bool supportsSemanticTokensRange)
    {
        options.Full = !supportsSemanticTokensRange;
        options.Legend = new SemanticTokensLegend
        {
            TokenModifiers = legend.TokenModifiers.All,
            TokenTypes = legend.TokenTypes.All
        };
        options.Range = supportsSemanticTokensRange;

        return options;
    }
}
