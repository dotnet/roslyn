// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    internal partial class GenericNameSignatureHelpProvider
    {
        private IList<SymbolDisplayPart> GetPreambleParts(
            INamedTypeSymbol namedType,
            SemanticModel semanticModel,
            int position)
        {
            var result = new List<SymbolDisplayPart>();

            result.AddRange(namedType.ToMinimalDisplayParts(semanticModel, position, MinimallyQualifiedWithoutTypeParametersFormat));
            result.Add(Punctuation(SyntaxKind.LessThanToken));

            return result;
        }

        private IList<SymbolDisplayPart> GetPostambleParts(INamedTypeSymbol namedType)
        {
            return SpecializedCollections.SingletonList(
                Punctuation(SyntaxKind.GreaterThanToken));
        }
    }
}