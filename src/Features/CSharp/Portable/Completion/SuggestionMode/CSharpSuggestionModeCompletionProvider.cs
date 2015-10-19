// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.SuggestionMode;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.SuggestionMode
{
    internal class CSharpSuggestionModeCompletionProvider : SuggestionModeCompletionProvider
    {
        protected override TextSpan GetFilterSpan(SourceText text, int position)
        {
            return CompletionUtilities.GetTextChangeSpan(text, position);
        }

        protected override async Task<CompletionItem> GetBuilderAsync(Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (triggerInfo.TriggerReason == CompletionTriggerReason.TypeCharCommand)
            {
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var token = tree
                    .FindTokenOnLeftOfPosition(position, cancellationToken)
                    .GetPreviousTokenIfTouchingWord(position);

                if (token.Kind() == SyntaxKind.None)
                {
                    return null;
                }

                var semanticModel = await document.GetSemanticModelForNodeAsync(token.Parent, cancellationToken).ConfigureAwait(false);
                var typeInferrer = document.GetLanguageService<ITypeInferenceService>();

                if (IsLambdaExpression(semanticModel, position, token, typeInferrer, cancellationToken))
                {
                    return CreateBuilder(text, position, CSharpFeaturesResources.LambdaExpression, CSharpFeaturesResources.AutoselectDisabledDueToPotentialLambdaDeclaration);
                }
                else if (IsAnonymousObjectCreation(token))
                {
                    return CreateBuilder(text, position, CSharpFeaturesResources.MemberName, CSharpFeaturesResources.AutoselectDisabledDueToPossibleExplicitlyNamesAnonTypeMemCreation);
                }
                else if (token.IsPreProcessorExpressionContext())
                {
                    return CreateEmptyBuilder(text, position);
                }
                else if (IsImplicitArrayCreation(semanticModel, token, position, typeInferrer, cancellationToken))
                {
                    return CreateBuilder(text, position, CSharpFeaturesResources.ImplicitArrayCreation, CSharpFeaturesResources.AutoselectDisabledDueToPotentialImplicitArray);
                }
                else if (token.IsKindOrHasMatchingText(SyntaxKind.FromKeyword) || token.IsKindOrHasMatchingText(SyntaxKind.JoinKeyword))
                {
                    return CreateBuilder(text, position, CSharpFeaturesResources.RangeVariable, CSharpFeaturesResources.AutoselectDisabledDueToPotentialRangeVariableDecl);
                }
            }

            return null;
        }

        private bool IsImplicitArrayCreation(SemanticModel semanticModel, SyntaxToken token, int position, ITypeInferenceService typeInferrer, CancellationToken cancellationToken)
        {
            if (token.IsKind(SyntaxKind.NewKeyword) && token.Parent.IsKind(SyntaxKind.ObjectCreationExpression))
            {
                var type = typeInferrer.InferType(semanticModel, token.Parent, objectAsDefault: false, cancellationToken: cancellationToken);
                return type != null && type is IArrayTypeSymbol;
            }

            return false;
        }

        private bool IsAnonymousObjectCreation(SyntaxToken token)
        {
            if (token.Parent is AnonymousObjectCreationExpressionSyntax)
            {
                // We'll show the builder after an open brace or comma, because that's where the
                // user can start declaring new named parts. 
                return token.Kind() == SyntaxKind.OpenBraceToken || token.Kind() == SyntaxKind.CommaToken;
            }

            return false;
        }

        private bool IsLambdaExpression(SemanticModel semanticModel, int position, SyntaxToken token, ITypeInferenceService typeInferrer, CancellationToken cancellationToken)
        {
            // Typing a generic type parameter, the tree might look like a binary expression around the < token.
            // If we infer a delegate type here (because that's what on the other side of the binop), 
            // ignore it.
            if (token.Kind() == SyntaxKind.LessThanToken && token.Parent is BinaryExpressionSyntax)
            {
                return false;
            }

            // We might be in the arguments to a parenthesized lambda
            if (token.Kind() == SyntaxKind.OpenParenToken || token.Kind() == SyntaxKind.CommaToken)
            {
                if (token.Parent != null && token.Parent is ParameterListSyntax)
                {
                    return token.Parent.Parent != null && token.Parent.Parent is ParenthesizedLambdaExpressionSyntax;
                }
            }

            // Walk up a single level to allow for typing the beginning of a lambda:
            // new AssemblyLoadEventHandler(($$
            if (token.Kind() == SyntaxKind.OpenParenToken &&
                token.Parent.Kind() == SyntaxKind.ParenthesizedExpression)
            {
                position = token.Parent.SpanStart;
            }

            // WorkItem 834609: Automatic brace completion inserts the closing paren, making it
            // like a cast.
            if (token.Kind() == SyntaxKind.OpenParenToken &&
                token.Parent.Kind() == SyntaxKind.CastExpression)
            {
                position = token.Parent.SpanStart;
            }

            // If we're an argument to a function with multiple overloads, 
            // open the builder if any overload takes a delegate at our argument position
            var inferredTypes = typeInferrer.InferTypes(semanticModel, position, cancellationToken: cancellationToken);

            return inferredTypes.Any(type => type.GetDelegateType(semanticModel.Compilation).IsDelegateType());
        }
    }
}
