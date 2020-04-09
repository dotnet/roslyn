// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    internal abstract class AbstractCSharpSignatureHelpProvider : AbstractSignatureHelpProvider
    {
        protected AbstractCSharpSignatureHelpProvider()
        {
        }

        protected static SymbolDisplayPart Keyword(SyntaxKind kind)
            => new SymbolDisplayPart(SymbolDisplayPartKind.Keyword, null, SyntaxFacts.GetText(kind));

        protected static SymbolDisplayPart Punctuation(SyntaxKind kind)
            => new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, SyntaxFacts.GetText(kind));

        protected static SymbolDisplayPart Text(string text)
            => new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, text);

        protected static SymbolDisplayPart Space()
            => new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, " ");

        protected static SymbolDisplayPart NewLine()
            => new SymbolDisplayPart(SymbolDisplayPartKind.LineBreak, null, "\r\n");

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
            IDocumentationCommentFormattingService formatter)
        {
            return new SignatureHelpSymbolParameter(
                parameter.Name,
                parameter.IsOptional,
                parameter.GetDocumentationPartsFactory(semanticModel, position, formatter),
                parameter.ToMinimalDisplayParts(semanticModel, position));
        }
    }
}
