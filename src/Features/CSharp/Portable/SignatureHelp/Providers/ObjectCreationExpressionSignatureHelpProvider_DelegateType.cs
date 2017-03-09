// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.SignatureHelp;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp.Providers
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
                return null;
            }

            var position = objectCreationExpression.SpanStart;
            var item = CreateItem(
                invokeMethod, semanticModel, position,
                symbolDisplayService, anonymousTypeDisplayService,
                isVariadic: false,
                prefixParts: GetDelegateTypePreambleParts(invokeMethod, semanticModel, position),
                separatorParts: GetSeparatorParts(),
                suffixParts: GetDelegateTypePostambleParts(invokeMethod),
                parameters: GetDelegateTypeParameters(invokeMethod, semanticModel, position, cancellationToken))
                .WithSymbol(null); // will cause documentation to be empty;

            return SpecializedCollections.SingletonList(item);
        }

        private IList<SymbolDisplayPart> GetDelegateTypePreambleParts(IMethodSymbol invokeMethod, SemanticModel semanticModel, int position)
        {
            var result = new List<SymbolDisplayPart>();

            result.AddRange(invokeMethod.ContainingType.ToMinimalDisplayParts(semanticModel, position));
            result.Add(Punctuation(SyntaxKind.OpenParenToken));

            return result;
        }

        private IList<CommonParameterData> GetDelegateTypeParameters(IMethodSymbol invokeMethod, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
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
                new CommonParameterData(
                    TargetName,
                    isOptional: false,
                    symbol: null,
                    position: 0,
                    displayParts: parts.ToImmutableArray()));
        }

        private IList<SymbolDisplayPart> GetDelegateTypePostambleParts(IMethodSymbol invokeMethod)
        {
            return SpecializedCollections.SingletonList(
                Punctuation(SyntaxKind.CloseParenToken));
        }
    }
}
