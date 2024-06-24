// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot;

internal static class CopilotUtilities
{
    public static string GetCopilotSuggestionDiagnosticTag()
        => WellKnownDiagnosticCustomTags.CopilotSuggestion;

    public static bool IsResultantVisibilityPublic(this ISymbol symbol)
    {
        return symbol.GetResultantVisibility() == Shared.Utilities.SymbolVisibility.Public;
    }

    public static bool IsValidIdentifier([NotNullWhen(returnValue: true)] string? name)
    {
        return UnicodeCharacterUtilities.IsValidIdentifier(name);
    }

    public static async Task<SyntaxNode?> GetContainingMethodDeclarationAsync(Document document, int position, bool useFullSpan, CancellationToken cancellationToken)
    {
        if (document.GetLanguageService<ISyntaxFactsService>() is not ISyntaxFactsService service)
            return null;

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        return service.GetContainingMethodDeclaration(root, position, useFullSpan);
    }
}
