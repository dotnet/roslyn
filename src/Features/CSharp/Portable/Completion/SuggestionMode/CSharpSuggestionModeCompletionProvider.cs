// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.SuggestionMode;
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
        protected override async Task<CompletionItem> GetSuggestionModeItemAsync(
            Document document, int position, TextSpan itemSpan, CompletionTrigger trigger, CancellationToken cancellationToken = default)
        {
            if (trigger.Kind != CompletionTriggerKind.Snippets)
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
                    return CreateSuggestionModeItem(CSharpFeaturesResources.lambda_expression, CSharpFeaturesResources.Autoselect_disabled_due_to_potential_lambda_declaration);
                }
                else if (IsAnonymousObjectCreation(token))
                {
                    return CreateSuggestionModeItem(CSharpFeaturesResources.member_name, CSharpFeaturesResources.Autoselect_disabled_due_to_possible_explicitly_named_anonymous_type_member_creation);
                }
                else if (token.IsPreProcessorExpressionContext())
                {
                    return CreateEmptySuggestionModeItem();
                }
                else if (token.IsKindOrHasMatchingText(SyntaxKind.FromKeyword) || token.IsKindOrHasMatchingText(SyntaxKind.JoinKeyword))
                {
                    return CreateSuggestionModeItem(CSharpFeaturesResources.range_variable, CSharpFeaturesResources.Autoselect_disabled_due_to_potential_range_variable_declaration);
                }
                else if (tree.IsNamespaceDeclarationNameContext(position, cancellationToken))
                {
                    return CreateSuggestionModeItem(CSharpFeaturesResources.namespace_name, CSharpFeaturesResources.Autoselect_disabled_due_to_namespace_declaration);
                }
                else if (tree.IsPartialTypeDeclarationNameContext(position, cancellationToken, out var typeDeclaration))
                {
                    switch (typeDeclaration.Keyword.Kind())
                    {
                        case SyntaxKind.ClassKeyword:
                            return CreateSuggestionModeItem(CSharpFeaturesResources.class_name, CSharpFeaturesResources.Autoselect_disabled_due_to_type_declaration);

                        case SyntaxKind.StructKeyword:
                            return CreateSuggestionModeItem(CSharpFeaturesResources.struct_name, CSharpFeaturesResources.Autoselect_disabled_due_to_type_declaration);

                        case SyntaxKind.InterfaceKeyword:
                            return CreateSuggestionModeItem(CSharpFeaturesResources.interface_name, CSharpFeaturesResources.Autoselect_disabled_due_to_type_declaration);
                    }
                }
                else if (tree.IsPossibleDeconstructionDesignation(position, cancellationToken))
                {
                    return CreateSuggestionModeItem(CSharpFeaturesResources.designation_name,
                        CSharpFeaturesResources.Autoselect_disabled_due_to_possible_deconstruction_declaration);
                }
            }

            return null;
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
            // Not after `new`
            if (token.IsKind(SyntaxKind.NewKeyword) && token.Parent.IsKind(SyntaxKind.ObjectCreationExpression))
            {
                return false;
            }

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

            // A lambda that is being typed may be parsed as a tuple without names
            // For example, "(a, b" could be the start of either a tuple or lambda
            // But "(a: b, c" cannot be a lambda
            if (token.SyntaxTree.IsPossibleTupleContext(token, position) && token.Parent.IsKind(SyntaxKind.TupleExpression) &&
               !((TupleExpressionSyntax)token.Parent).HasNames())
            {
                position = token.Parent.SpanStart;
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

            // In the following situation, the type inferrer will infer Task to support target type preselection
            // Action a = Task.$$
            // We need to explicitly exclude invocation/member access from suggestion mode
            var previousToken = token.GetPreviousTokenIfTouchingWord(position);
            if (previousToken.IsKind(SyntaxKind.DotToken) &&
                previousToken.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                return false;
            }

            // async lambda: 
            //    Goo(async($$
            //    Goo(async(p1, $$
            if (token.IsKind(SyntaxKind.OpenParenToken, SyntaxKind.CommaToken) && token.Parent.IsKind(SyntaxKind.ArgumentList)
                && token.Parent.Parent is InvocationExpressionSyntax invocation
                && invocation.Expression is IdentifierNameSyntax identifier)
            {
                if (identifier.Identifier.IsKindOrHasMatchingText(SyntaxKind.AsyncKeyword))
                {
                    return true;
                }
            }

            // If we're an argument to a function with multiple overloads, 
            // open the builder if any overload takes a delegate at our argument position
            var inferredTypeInfo = typeInferrer.GetTypeInferenceInfo(semanticModel, position, cancellationToken: cancellationToken);
            return inferredTypeInfo.Any(type => GetDelegateType(type, semanticModel.Compilation).IsDelegateType());
        }

        private ITypeSymbol GetDelegateType(TypeInferenceInfo typeInferenceInfo, Compilation compilation)
        {
            var typeSymbol = typeInferenceInfo.InferredType;
            if (typeInferenceInfo.IsParams && typeInferenceInfo.InferredType.IsArrayType())
            {
                typeSymbol = ((IArrayTypeSymbol)typeInferenceInfo.InferredType).ElementType;
            }

            return typeSymbol.GetDelegateType(compilation);
        }
    }
}
