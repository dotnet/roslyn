// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    internal partial class GenericNameSignatureHelpProvider
    {
        private IList<SymbolDisplayPart> GetPreambleParts(
            IMethodSymbol method,
            SemanticModel semanticModel,
            int position)
        {
            var result = new List<SymbolDisplayPart>();

            var awaitable = method.GetOriginalUnreducedDefinition().IsAwaitableNonDynamic(semanticModel, position);
            var extension = method.GetOriginalUnreducedDefinition().IsExtensionMethod();

            if (awaitable && extension)
            {
                result.Add(Punctuation(SyntaxKind.OpenParenToken));
                result.Add(Text(CSharpFeaturesResources.awaitable));
                result.Add(Punctuation(SyntaxKind.CommaToken));
                result.Add(Text(CSharpFeaturesResources.extension));
                result.Add(Punctuation(SyntaxKind.CloseParenToken));
                result.Add(Space());
            }
            else if (awaitable)
            {
                result.Add(Punctuation(SyntaxKind.OpenParenToken));
                result.Add(Text(CSharpFeaturesResources.awaitable));
                result.Add(Punctuation(SyntaxKind.CloseParenToken));
                result.Add(Space());
            }
            else if (extension)
            {
                result.Add(Punctuation(SyntaxKind.OpenParenToken));
                result.Add(Text(CSharpFeaturesResources.extension));
                result.Add(Punctuation(SyntaxKind.CloseParenToken));
                result.Add(Space());
            }

            result.AddRange(method.ReturnType.ToMinimalDisplayParts(semanticModel, position));
            result.Add(Space());
            var containingType = GetContainingType(method);
            if (containingType != null)
            {
                result.AddRange(containingType.ToMinimalDisplayParts(semanticModel, position));
                result.Add(Punctuation(SyntaxKind.DotToken));
            }

            result.Add(new SymbolDisplayPart(SymbolDisplayPartKind.MethodName, method, method.Name));
            result.Add(Punctuation(SyntaxKind.LessThanToken));

            return result;
        }

        private ITypeSymbol GetContainingType(IMethodSymbol method)
        {
            var result = method.ReceiverType;

            if (result.Kind != SymbolKind.NamedType || !((INamedTypeSymbol)result).IsScriptClass)
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        private IList<SymbolDisplayPart> GetPostambleParts(IMethodSymbol method, SemanticModel semanticModel, int position)
        {
            var result = new List<SymbolDisplayPart>
            {
                Punctuation(SyntaxKind.GreaterThanToken),
                Punctuation(SyntaxKind.OpenParenToken)
            };

            var first = true;
            foreach (var parameter in method.Parameters)
            {
                if (!first)
                {
                    result.Add(Punctuation(SyntaxKind.CommaToken));
                    result.Add(Space());
                }

                first = false;
                result.AddRange(parameter.ToMinimalDisplayParts(semanticModel, position));
            }

            result.Add(Punctuation(SyntaxKind.CloseParenToken));
            return result;
        }
    }
}
