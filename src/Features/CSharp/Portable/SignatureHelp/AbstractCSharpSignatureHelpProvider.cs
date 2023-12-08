// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    internal abstract partial class AbstractCSharpSignatureHelpProvider : AbstractSignatureHelpProvider
    {
        private static readonly SymbolDisplayFormat s_allowDefaultLiteralFormat = SymbolDisplayFormat.MinimallyQualifiedFormat
            .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral);

        protected AbstractCSharpSignatureHelpProvider()
        {
        }

        protected static SymbolDisplayPart Keyword(SyntaxKind kind)
            => new SymbolDisplayPart(SymbolDisplayPartKind.Keyword, null, SyntaxFacts.GetText(kind));

        protected static SymbolDisplayPart Operator(SyntaxKind kind)
            => new SymbolDisplayPart(SymbolDisplayPartKind.Operator, null, SyntaxFacts.GetText(kind));

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
                parameter.ToMinimalDisplayParts(semanticModel, position, s_allowDefaultLiteralFormat));
        }

        /// <summary>
        /// We no longer show awaitable usage text in SignatureHelp, but IntelliCode expects this
        /// method to exist.
        /// </summary>
        [Obsolete("Expected to exist by IntelliCode. This can be removed once their unnecessary use of this is removed.")]
#pragma warning disable CA1822 // Mark members as static - see obsolete message above.
        protected IList<TaggedText> GetAwaitableUsage(IMethodSymbol method, SemanticModel semanticModel, int position)
#pragma warning restore CA1822 // Mark members as static
            => SpecializedCollections.EmptyList<TaggedText>();
    }
}
