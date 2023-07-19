// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    internal partial class GenericNameSignatureHelpProvider
    {
        private static IList<SymbolDisplayPart> GetPreambleParts(
            INamedTypeSymbol namedType,
            SemanticModel semanticModel,
            int position)
        {
            var result = new List<SymbolDisplayPart>();

            result.AddRange(namedType.ToMinimalDisplayParts(semanticModel, position, MinimallyQualifiedWithoutTypeParametersFormat));
            result.Add(Punctuation(SyntaxKind.LessThanToken));

            return result;
        }

        private static IList<SymbolDisplayPart> GetPostambleParts()
        {
            return SpecializedCollections.SingletonList(
                Punctuation(SyntaxKind.GreaterThanToken));
        }
    }
}
