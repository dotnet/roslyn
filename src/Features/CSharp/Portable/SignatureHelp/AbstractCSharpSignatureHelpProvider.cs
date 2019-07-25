// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    internal abstract class AbstractCSharpSignatureHelpProvider : AbstractSignatureHelpProvider
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

        protected static SignatureHelpSymbolParameter Convert(
            IParameterSymbol parameter,
            SemanticModel semanticModel,
            int position,
            IDocumentationCommentFormattingService formatter,
            CancellationToken cancellationToken)
        {
            return new SignatureHelpSymbolParameter(
                parameter.Name,
                parameter.IsOptional,
                parameter.GetDocumentationPartsFactory(semanticModel, position, formatter),
                parameter.ToMinimalDisplayParts(semanticModel, position));
        }

        protected IList<TaggedText> GetAwaitableUsage(IMethodSymbol method, SemanticModel semanticModel, int position)
        {
            if (method.IsAwaitableNonDynamic(semanticModel, position))
            {
                return method.ToAwaitableParts(SyntaxFacts.GetText(SyntaxKind.AwaitKeyword), "x", semanticModel, position)
                             .ToTaggedText(TaggedTextStyle.None);
            }

            return SpecializedCollections.EmptyList<TaggedText>();
        }
    }
}
