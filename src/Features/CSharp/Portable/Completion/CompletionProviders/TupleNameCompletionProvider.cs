// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class TupleNameCompletionProvider : CommonCompletionProvider
    {
        private static readonly CompletionItemRules _cachedRules = CompletionItemRules.Default
            .WithCommitCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ':'));

        public override async Task ProvideCompletionsAsync(CompletionContext completionContext)
        {
            var document = completionContext.Document;
            var position = completionContext.Position;
            var cancellationToken = completionContext.CancellationToken;

            var semanticModel = await document.GetSemanticModelForSpanAsync(new Text.TextSpan(position, 0), cancellationToken).ConfigureAwait(false);

            var workspace = document.Project.Solution.Workspace;
            var context = CSharpSyntaxContext.CreateContext(workspace, semanticModel, position, cancellationToken);

            var index = GetElementIndex(context);
            if (index == null)
            {
                return;
            }

            var typeInferrer = document.GetLanguageService<ITypeInferenceService>();
            var inferredTypes = typeInferrer.InferTypes(semanticModel, context.TargetToken.Parent.SpanStart, cancellationToken)
                    .Where(t => t.IsTupleType)
                    .Cast<INamedTypeSymbol>()
                    .ToImmutableArray();

            AddItems(inferredTypes, index.Value, completionContext);
        }

        private int? GetElementIndex(CSharpSyntaxContext context)
        {
            var token = context.TargetToken;
            if (token.IsKind(SyntaxKind.OpenParenToken))
            {
                if (token.Parent.IsKind(SyntaxKind.ParenthesizedExpression,
                    SyntaxKind.TupleExpression,
                    SyntaxKind.CastExpression))
                {
                    return 0;
                }
            }

            if (token.IsKind(SyntaxKind.CommaToken) && token.Parent.IsKind(SyntaxKind.TupleExpression))
            {
                var tupleExpr = (TupleExpressionSyntax)context.TargetToken.Parent as TupleExpressionSyntax;
                return (tupleExpr.Arguments.GetWithSeparators().IndexOf(context.TargetToken) + 1) / 2;
            }

            return null;
        }

        private void AddItems(ImmutableArray<INamedTypeSymbol> inferredTypes, int index, CompletionContext context)
        {
            foreach (var type in inferredTypes)
            {
                if (index >= type.TupleElements.Length)
                {
                    return;
                }

                var field = type.TupleElements[index];
                var item = CommonCompletionItem.Create(
                    field.Name + ":", _cachedRules, Glyph.FieldPublic);
                context.AddItem(item);
            }
        }
    }
}