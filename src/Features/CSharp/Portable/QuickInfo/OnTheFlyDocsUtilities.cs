// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.QuickInfo;

internal static class OnTheFlyDocsUtilities
{
    public static ImmutableArray<OnTheFlyDocsRelevantFileInfo?> GetAdditionalOnTheFlyDocsContext(Solution solution, ISymbol symbol)
    {
        var parameterStrings = symbol.GetParameters().SelectAsArray(parameter => GetOnTheFlyDocsRelevantFileInfo(parameter.Type));
        var typeArgumentStrings = symbol.GetTypeArguments().SelectAsArray(GetOnTheFlyDocsRelevantFileInfo);

        return [.. parameterStrings, .. typeArgumentStrings];

        OnTheFlyDocsRelevantFileInfo? GetOnTheFlyDocsRelevantFileInfo(ITypeSymbol typeSymbol)
        {
            var typeSyntaxReference = typeSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (typeSyntaxReference is { Span: var typeSpan })
            {
                var syntaxReferenceDocument = solution.GetDocument(typeSyntaxReference.SyntaxTree);
                if (syntaxReferenceDocument is not null)
                    return new OnTheFlyDocsRelevantFileInfo(syntaxReferenceDocument, typeSpan);
            }

            return null;
        }
    }
}
