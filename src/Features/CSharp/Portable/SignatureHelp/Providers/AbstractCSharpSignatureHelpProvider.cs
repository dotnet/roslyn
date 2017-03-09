// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp.Providers
{
    internal abstract class AbstractCSharpSignatureHelpProvider : CommonSignatureHelpProvider
    {
        protected AbstractCSharpSignatureHelpProvider()
        {
        }

        protected static SymbolDisplayPart Keyword(SyntaxKind kind)
        {
            return new SymbolDisplayPart(SymbolDisplayPartKind.Keyword, null, SyntaxFacts.GetText(kind));
        }

        protected static SymbolDisplayPart Punctuation(SyntaxKind kind)
        {
            return new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, SyntaxFacts.GetText(kind));
        }

        protected static SymbolDisplayPart Text(string text)
        {
            return new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, text);
        }

        protected static SymbolDisplayPart Space()
        {
            return new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, " ");
        }

        protected static SymbolDisplayPart NewLine()
        {
            return new SymbolDisplayPart(SymbolDisplayPartKind.LineBreak, null, "\r\n");
        }

        private static readonly IList<SymbolDisplayPart> _separatorParts = new List<SymbolDisplayPart>
            {
                Punctuation(SyntaxKind.CommaToken),
                Space()
            };

        protected static IList<SymbolDisplayPart> GetSeparatorParts() => _separatorParts;

        protected static CommonParameterData Convert(
            IParameterSymbol parameter,
            SemanticModel semanticModel,
            int position,
            CancellationToken cancellationToken)
        {
            return new CommonParameterData(
                parameter.Name,
                parameter.IsOptional,
                parameter,
                position,
                parameter.ToMinimalDisplayParts(semanticModel, position));
        }

        protected IList<TaggedText> GetAwaitableUsage(IMethodSymbol method, SemanticModel semanticModel, int position)
        {
            if (method.IsAwaitableNonDynamic(semanticModel, position))
            {
                return method.ToAwaitableParts(SyntaxFacts.GetText(SyntaxKind.AwaitKeyword), "x", semanticModel, position)
                             .ToTaggedText();
            }

            return SpecializedCollections.EmptyList<TaggedText>();
        }

        public override async Task<ImmutableArray<TaggedText>> GetItemDocumentationAsync(Document document, SignatureHelpItem item, CancellationToken cancellationToken)
        {
            var symbol = await item.GetSymbolAsync(document, cancellationToken).ConfigureAwait(false);
            if (symbol != null)
            {
                var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var parts = await base.GetItemDocumentationAsync(document, item, cancellationToken).ConfigureAwait(false);

                var method = symbol as IMethodSymbol;
                if (method != null)
                {
                    parts = parts.Concat(GetAwaitableUsage(method, model, item.GetPosition()).ToImmutableArray());
                }

                return parts;
            }

            return ImmutableArray<TaggedText>.Empty;
        }
    }
}
