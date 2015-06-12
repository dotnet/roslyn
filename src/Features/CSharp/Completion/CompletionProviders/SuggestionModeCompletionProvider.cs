// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class SuggestionModeCompletionProvider : ICompletionProvider
    {
        public bool IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            return false;
        }

        public bool SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar)
        {
            return false;
        }

        public bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            return false;
        }

        public TextChange GetTextChange(CompletionItem selectedItem, char? ch = null, string textTypedSoFar = null)
        {
            return new TextChange(selectedItem.FilterSpan, selectedItem.DisplayText);
        }

        public async Task<CompletionItemGroup> GetGroupAsync(Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            if (!triggerInfo.IsAugment)
            {
                return null;
            }

            var builder = await this.GetBuilderAsync(document, position, triggerInfo, cancellationToken).ConfigureAwait(false);
            if (builder == null)
            {
                return null;
            }

            return new CompletionItemGroup(
                SpecializedCollections.EmptyEnumerable<CompletionItem>(),
                builder,
                isExclusive: false);
        }

        private async Task<CompletionItem> GetBuilderAsync(Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (triggerInfo.TriggerReason == CompletionTriggerReason.TypeCharCommand)
            {
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                if (triggerInfo.IsDebugger)
                {
                    // Aggressive Intellisense in the debugger: always show the builder 
                    return new CompletionItem(this, "", CompletionUtilities.GetTextChangeSpan(text, position), isBuilder: true);
                }

                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
                token = token.GetPreviousTokenIfTouchingWord(position);
                if (token.Kind() == SyntaxKind.None)
                {
                    return null;
                }

                var semanticModel = await document.GetSemanticModelForNodeAsync(token.Parent, cancellationToken).ConfigureAwait(false);
                var typeInferrer = document.GetLanguageService<ITypeInferenceService>();

                if (IsLambdaExpression(semanticModel, position, token, typeInferrer, cancellationToken))
                {
                    return new CompletionItem(this, CSharpFeaturesResources.LambdaExpression,
                        CompletionUtilities.GetTextChangeSpan(text, position),
                        CSharpFeaturesResources.AutoselectDisabledDueToPotentialLambdaDeclaration.ToSymbolDisplayParts(),
                        isBuilder: true);
                }
                else if (IsAnonymousObjectCreation(token))
                {
                    return new CompletionItem(this, CSharpFeaturesResources.MemberName,
                        CompletionUtilities.GetTextChangeSpan(text, position),
                        CSharpFeaturesResources.AutoselectDisabledDueToPossibleExplicitlyNamesAnonTypeMemCreation.ToSymbolDisplayParts(),
                        isBuilder: true);
                }
                else if (token.IsPreProcessorExpressionContext())
                {
                    return new CompletionItem(this, "", CompletionUtilities.GetTextChangeSpan(text, position), isBuilder: true);
                }
                else if (IsImplicitArrayCreation(semanticModel, token, position, typeInferrer, cancellationToken))
                {
                    return new CompletionItem(this, CSharpFeaturesResources.ImplicitArrayCreation,
                        CompletionUtilities.GetTextChangeSpan(text, position),
                        CSharpFeaturesResources.AutoselectDisabledDueToPotentialImplicitArray.ToSymbolDisplayParts(),
                        isBuilder: true);
                }
                else
                {
                    return token.IsKindOrHasMatchingText(SyntaxKind.FromKeyword) || token.IsKindOrHasMatchingText(SyntaxKind.JoinKeyword)
                        ? new CompletionItem(this, CSharpFeaturesResources.RangeVariable,
                            CompletionUtilities.GetTextChangeSpan(text, position),
                            CSharpFeaturesResources.AutoselectDisabledDueToPotentialRangeVariableDecl.ToSymbolDisplayParts(),
                        isBuilder: true)
                        : null;
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

            // We might be in the arguments to a parenthsized lambda
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

        public bool IsFilterCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            return false;
        }
    }
}
