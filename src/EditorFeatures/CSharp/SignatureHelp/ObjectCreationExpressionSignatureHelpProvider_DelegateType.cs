// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SignatureHelp
{
    internal partial class ObjectCreationExpressionSignatureHelpProvider
    {
        private IEnumerable<SignatureHelpItem> GetDelegateTypeConstructors(
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
                return null;
            }

            var position = objectCreationExpression.SpanStart;
            var item = CreateItem(
                invokeMethod, semanticModel, position,
                symbolDisplayService, anonymousTypeDisplayService,
                isVariadic: false,
                documentationFactory: null,
                prefixParts: GetDelegateTypePreambleParts(invokeMethod, semanticModel, position),
                separatorParts: GetSeparatorParts(),
                suffixParts: GetDelegateTypePostambleParts(invokeMethod),
                parameters: GetDelegateTypeParameters(invokeMethod, semanticModel, position, cancellationToken));

            return SpecializedCollections.SingletonEnumerable(item);
        }

        private IEnumerable<SymbolDisplayPart> GetDelegateTypePreambleParts(IMethodSymbol invokeMethod, SemanticModel semanticModel, int position)
        {
            var result = new List<SymbolDisplayPart>();

            result.AddRange(invokeMethod.ContainingType.ToMinimalDisplayParts(semanticModel, position));
            result.Add(Punctuation(SyntaxKind.OpenParenToken));

            return result;
        }

        private IEnumerable<SignatureHelpParameter> GetDelegateTypeParameters(IMethodSymbol invokeMethod, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
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

            yield return new SignatureHelpParameter(
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
