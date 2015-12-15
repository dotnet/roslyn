// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.LanguageServices;
using System.Linq;
using System;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class SymbolCompletionProvider : AbstractSymbolCompletionProvider
    {
        protected override Task<IEnumerable<ISymbol>> GetSymbolsWorker(AbstractSyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            return Recommender.GetRecommendedSymbolsAtPositionAsync(context.SemanticModel, position, context.Workspace, options, cancellationToken);
        }

        protected override TextSpan GetTextChangeSpan(SourceText text, int position)
        {
            return CompletionUtilities.GetTextChangeSpan(text, position);
        }

        public override bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);
        }

        protected override async Task<bool> IsSemanticTriggerCharacterAsync(Document document, int characterPosition, CancellationToken cancellationToken)
        {
            bool? result = await IsTriggerOnDotAsync(document, characterPosition, cancellationToken).ConfigureAwait(false);
            if (result.HasValue)
            {
                return result.Value;
            }

            return true;
        }

        private async Task<bool?> IsTriggerOnDotAsync(Document document, int characterPosition, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (text[characterPosition] != '.')
            {
                return null;
            }

            // don't want to trigger after a number.  All other cases after dot are ok.
            var tree = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = tree.FindToken(characterPosition);
            if (token.Kind() == SyntaxKind.DotToken)
            {
                token = token.GetPreviousToken();
            }

            return token.Kind() != SyntaxKind.NumericLiteralToken;
        }

        protected override async Task<AbstractSyntaxContext> CreateContext(Document document, int position, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;
            var span = new TextSpan(position, 0);
            var semanticModel = await document.GetSemanticModelForSpanAsync(span, cancellationToken).ConfigureAwait(false);
            return CSharpSyntaxContext.CreateContext(workspace, semanticModel, position, cancellationToken);
        }

        protected override ValueTuple<string, string> GetDisplayAndInsertionText(ISymbol symbol, AbstractSyntaxContext context)
        {
            var insertionText = ItemRules.GetInsertionText(symbol, context);
            var displayText = symbol.GetArity() == 0 ? insertionText : string.Format("{0}<>", insertionText);

            return ValueTuple.Create(displayText, insertionText);
        }

        protected override CompletionItemRules GetCompletionItemRules()
        {
            return ItemRules.Instance;
        }

        protected override async Task<IEnumerable<ISymbol>> GetPreselectedSymbolsWorker(AbstractSyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            var recommender = context.GetLanguageService<IRecommendationService>();
            var typeInferrer = context.GetLanguageService<ITypeInferenceService>();

            var inferredTypes = typeInferrer.InferTypes(context.SemanticModel, position, cancellationToken)
                ?.Where(t => t.SpecialType != SpecialType.System_Void)
                .ToSet();
            if (inferredTypes == null || !inferredTypes.Any())
            {
                return SpecializedCollections.EmptyEnumerable<ISymbol>();
            }

            var symbols = await recommender.GetRecommendedSymbolsAtPositionAsync(context.Workspace, context.SemanticModel, position, options, cancellationToken).ConfigureAwait(false);
            return symbols.Where(s => inferredTypes.Contains(s.GetSymbolType()) && !IsInstrinsic(s));
        }

        private bool IsInstrinsic(ISymbol s)
        {
            var ts = s as ITypeSymbol;
            return ts != null && ts.SpecialType != SpecialType.None;
        }
    }
}
