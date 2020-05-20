// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.SignatureHelp;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    internal partial class ObjectCreationExpressionSignatureHelpProvider
    {
        private static (IList<SignatureHelpItem> items, int? selectedItem) GetDelegateTypeConstructors(
            BaseObjectCreationExpressionSyntax objectCreationExpression,
            SemanticModel semanticModel,
            IAnonymousTypeDisplayService anonymousTypeDisplayService,
            INamedTypeSymbol delegateType)
        {
            var invokeMethod = delegateType.DelegateInvokeMethod;
            if (invokeMethod == null)
            {
                return (null, null);
            }

            var position = objectCreationExpression.SpanStart;
            var item = CreateItem(
                invokeMethod, semanticModel, position,
                anonymousTypeDisplayService,
                isVariadic: false,
                documentationFactory: null,
                prefixParts: GetDelegateTypePreambleParts(invokeMethod, semanticModel, position),
                separatorParts: GetSeparatorParts(),
                suffixParts: GetDelegateTypePostambleParts(),
                parameters: GetDelegateTypeParameters(invokeMethod, semanticModel, position));

            return (SpecializedCollections.SingletonList(item), 0);
        }

        private static IList<SymbolDisplayPart> GetDelegateTypePreambleParts(IMethodSymbol invokeMethod, SemanticModel semanticModel, int position)
        {
            var result = new List<SymbolDisplayPart>();

            result.AddRange(invokeMethod.ContainingType.ToMinimalDisplayParts(semanticModel, position));
            result.Add(Punctuation(SyntaxKind.OpenParenToken));

            return result;
        }

        private static IList<SignatureHelpSymbolParameter> GetDelegateTypeParameters(IMethodSymbol invokeMethod, SemanticModel semanticModel, int position)
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

            return SpecializedCollections.SingletonList(
                new SignatureHelpSymbolParameter(
                    TargetName,
                    isOptional: false,
                    documentationFactory: null,
                    displayParts: parts));
        }

        private static IList<SymbolDisplayPart> GetDelegateTypePostambleParts()
        {
            return SpecializedCollections.SingletonList(
                Punctuation(SyntaxKind.CloseParenToken));
        }
    }
}
