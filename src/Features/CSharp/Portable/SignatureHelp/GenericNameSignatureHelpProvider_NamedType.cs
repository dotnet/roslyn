// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp;

internal partial class GenericNameSignatureHelpProvider
{
    private static IList<SymbolDisplayPart> GetPreambleParts(
        INamedTypeSymbol namedType,
        SemanticModel semanticModel,
        int position)
    {
        return [.. namedType.ToMinimalDisplayParts(semanticModel, position, MinimallyQualifiedWithoutTypeParametersFormat), Punctuation(SyntaxKind.LessThanToken)];
    }

    private static IList<SymbolDisplayPart> GetPostambleParts()
        => [Punctuation(SyntaxKind.GreaterThanToken)];
}
