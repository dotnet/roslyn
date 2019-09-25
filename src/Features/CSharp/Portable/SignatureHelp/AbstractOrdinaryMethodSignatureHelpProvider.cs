// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    internal abstract class AbstractOrdinaryMethodSignatureHelpProvider : AbstractCSharpSignatureHelpProvider
    {
        protected SignatureHelpItem ConvertMethodGroupMethod(
            Document document,
            IMethodSymbol method,
            int position,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var anonymousTypeDisplayService = document.GetLanguageService<IAnonymousTypeDisplayService>();
            var documentationCommentFormattingService = document.GetLanguageService<IDocumentationCommentFormattingService>();
            var symbolDisplayService = document.GetLanguageService<ISymbolDisplayService>();

            return CreateItem(
                method, semanticModel, position,
                symbolDisplayService, anonymousTypeDisplayService,
                method.IsParams(),
                c => method.OriginalDefinition.GetDocumentationParts(semanticModel, position, documentationCommentFormattingService, c).Concat(GetAwaitableUsage(method, semanticModel, position)),
                GetMethodGroupPreambleParts(method, semanticModel, position),
                GetSeparatorParts(),
                GetMethodGroupPostambleParts(method),
                method.Parameters.Select(p => Convert(p, semanticModel, position, documentationCommentFormattingService, cancellationToken)).ToList());
        }

        private IList<SymbolDisplayPart> GetMethodGroupPreambleParts(
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

            result.AddRange(method.ToMinimalDisplayParts(semanticModel, position, MinimallyQualifiedWithoutParametersFormat));
            result.Add(Punctuation(SyntaxKind.OpenParenToken));

            return result;
        }

        private IList<SymbolDisplayPart> GetMethodGroupPostambleParts(IMethodSymbol method)
            => SpecializedCollections.SingletonList(Punctuation(SyntaxKind.CloseParenToken));
    }
}
