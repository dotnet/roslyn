// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageServices;
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
                             .ToTaggedText();
            }

            return SpecializedCollections.EmptyList<TaggedText>();
        }

        protected ISymbol GuessCurrentSymbol(SeparatedSyntaxList<ArgumentSyntax> arguments, ImmutableArray<IMethodSymbol> methodGroup,
            SemanticModel semanticModel, ISemanticFactsService semanticFactsService, CancellationToken cancellationToken)
        {
            return methodGroup.FirstOrDefault(m => isAcceptable(m));

            bool isAcceptable(IMethodSymbol method)
            {
                if (arguments.Count == 0)
                {
                    return false;
                }

                int parameterCount = method.Parameters.Length;
                for (int i = 0; i < arguments.Count; i++)
                {
                    if (i >= parameterCount)
                    {
                        return false;
                    }
                    var parameter = method.Parameters[i];
                    var parameterRefKind = parameter.RefKind;
                    var argument = arguments[i];
                    if (parameterRefKind == RefKind.None)
                    {
                        if (!semanticFactsService.CanConvert(semanticModel, argument.Expression, parameter.Type))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        // We don't have an API to check conversion between type symbols, so we just check ref kind
                        var argumentRefKind = argument.GetRefKind();
                        if (parameterRefKind == RefKind.In && argumentRefKind == RefKind.None)
                        {
                            return true;
                        }
                        if (parameterRefKind == argumentRefKind)
                        {
                            return true;
                        }
                    }
                }
                return true;
            }
        }
    }
}
