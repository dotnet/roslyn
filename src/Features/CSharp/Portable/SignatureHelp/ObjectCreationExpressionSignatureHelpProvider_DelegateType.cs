// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.SignatureHelp;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    internal partial class ObjectCreationExpressionSignatureHelpProvider
    {
        private IList<SignatureHelpItem> GetDelegateTypeConstructors(
            ObjectCreationExpressionSyntax objectCreationExpression,
            SemanticModel semanticModel,
            ISymbolDisplayService symbolDisplayService,
            IAnonymousTypeDisplayService anonymousTypeDisplayService,
            INamedTypeSymbol delegateType,
            INamedTypeSymbol containingType,
            CancellationToken cancellationToken)
        {
            var invokeMethod = delegateType.DelegateInvokeMethod;
            if (invokeMethod == null)
            {
                return SpecializedCollections.EmptyList<SignatureHelpItem>();
            }

            var position = objectCreationExpression.SpanStart;
            var item = CreateItem(
                invokeMethod, semanticModel, position,
                symbolDisplayService, anonymousTypeDisplayService,
                isVariadic: false,
                documentationFactory: null,
                prefixParts: GetDelegateTypePreambleParts(invokeMethod, semanticModel, position),
                separatorParts: GetSeparatorParts().ToList(),
                suffixParts: GetDelegateTypePostambleParts(invokeMethod).ToList(),
                parameters: GetDelegateTypeParameters(invokeMethod, semanticModel, position, cancellationToken).ToList());

            return SpecializedCollections.SingletonList(item);
        }

        private IList<SymbolDisplayPart> GetDelegateTypePreambleParts(IMethodSymbol invokeMethod, SemanticModel semanticModel, int position)
        {
            var result = new List<SymbolDisplayPart>();

            result.AddRange(invokeMethod.ContainingType.ToMinimalDisplayParts(semanticModel, position));
            result.Add(Punctuation(SyntaxKind.OpenParenToken));

            return result;
        }

        private IEnumerable<SignatureHelpSymbolParameter> GetDelegateTypeParameters(IMethodSymbol invokeMethod, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            const string TargetName = "target";

            var parts = new List<SymbolDisplayPart>();
            parts.AddRange(invokeMethod.ReturnType.ToMinimalDisplayParts(semanticModel, position));
            parts.Add(Space());
            parts.Add(Punctuation(SyntaxKind.OpenParenToken));

            var first = true;
            foreach (var parameter in invokeMethod.Parameters)
            {
                if (!first)
                {
                    parts.Add(Punctuation(SyntaxKind.CommaToken));
                    parts.Add(Space());
                }

                first = false;
                parts.AddRange(parameter.Type.ToMinimalDisplayParts(semanticModel, position));
            }

            parts.Add(Punctuation(SyntaxKind.CloseParenToken));
            parts.Add(Space());
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.ParameterName, null, TargetName));

            yield return new SignatureHelpSymbolParameter(
                TargetName,
                isOptional: false,
                documentationFactory: null,
                displayParts: parts);
        }

        private IEnumerable<SymbolDisplayPart> GetDelegateTypePostambleParts(IMethodSymbol invokeMethod)
        {
            yield return Punctuation(SyntaxKind.CloseParenToken);
        }
    }
}
