// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(CSharpSuggestionModeCompletionProvider), LanguageNames.CSharp)]
[ExtensionOrder(After = nameof(ObjectAndWithInitializerCompletionProvider))]
[Shared]
internal class CSharpSuggestionModeCompletionProvider : AbstractSuggestionModeCompletionProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpSuggestionModeCompletionProvider()
    {
    }

    internal override string Language => LanguageNames.CSharp;

    protected override async Task<CompletionItem?> GetSuggestionModeItemAsync(
        Document document, int position, TextSpan itemSpan, CompletionTrigger trigger, CancellationToken cancellationToken = default)
    {
        if (trigger.Kind != CompletionTriggerKind.Snippets)
        {
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = tree
                .FindTokenOnLeftOfPosition(position, cancellationToken)
                .GetPreviousTokenIfTouchingWord(position);

            if (token.Kind() == SyntaxKind.None)
                return null;

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(token.Parent, cancellationToken).ConfigureAwait(false);
            var typeInferrer = document.GetRequiredLanguageService<ITypeInferenceService>();
            if (IsLambdaExpression(semanticModel, tree, position, token, typeInferrer, cancellationToken))
            {
                return CreateSuggestionModeItem(CSharpFeaturesResources.lambda_expression, CSharpFeaturesResources.Autoselect_disabled_due_to_potential_lambda_declaration);
            }
            else if (IsAnonymousObjectCreation(token))
            {
                return CreateSuggestionModeItem(CSharpFeaturesResources.member_name, CSharpFeaturesResources.Autoselect_disabled_due_to_possible_explicitly_named_anonymous_type_member_creation);
            }
            else if (IsPotentialPatternVariableDeclaration(tree.FindTokenOnLeftOfPosition(position, cancellationToken)))
            {
                return CreateSuggestionModeItem(CSharpFeaturesResources.pattern_variable, CSharpFeaturesResources.Autoselect_disabled_due_to_potential_pattern_variable_declaration);
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

    private static bool IsAnonymousObjectCreation(SyntaxToken token)
    {
        if (token.Parent is AnonymousObjectCreationExpressionSyntax)
        {
            // We'll show the builder after an open brace or comma, because that's where the
            // user can start declaring new named parts. 
            return token.Kind() is SyntaxKind.OpenBraceToken or SyntaxKind.CommaToken;
        }

        return false;
    }

    private static bool IsLambdaExpression(SemanticModel semanticModel, SyntaxTree tree, int position, SyntaxToken token, ITypeInferenceService typeInferrer, CancellationToken cancellationToken)
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
        if (token.Kind() is SyntaxKind.OpenParenToken or SyntaxKind.CommaToken)
        {
            if (token.Parent is not null and ParameterListSyntax)
            {
                return token.Parent.Parent is not null and ParenthesizedLambdaExpressionSyntax;
            }
        }

        // A lambda that is being typed may be parsed as a tuple without names
        // For example, "(a, b" could be the start of either a tuple or lambda
        // But "(a: b, c" cannot be a lambda
        if (tree.IsPossibleTupleContext(token, position) &&
            token.Parent is TupleExpressionSyntax tupleExpression &&
            !tupleExpression.HasNames())
        {
            position = token.Parent.SpanStart;
        }

        // Walk up a single level to allow for typing the beginning of a lambda:
        // new AssemblyLoadEventHandler(($$
        if (token.Kind() == SyntaxKind.OpenParenToken &&
            token.GetRequiredParent().Kind() == SyntaxKind.ParenthesizedExpression)
        {
            position = token.GetRequiredParent().SpanStart;
        }

        // WorkItem 834609: Automatic brace completion inserts the closing paren, making it
        // like a cast.
        if (token.Kind() == SyntaxKind.OpenParenToken &&
            token.GetRequiredParent().Kind() == SyntaxKind.CastExpression)
        {
            position = token.GetRequiredParent().SpanStart;
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
        if (token.Kind() is SyntaxKind.OpenParenToken or SyntaxKind.CommaToken && token.Parent.IsKind(SyntaxKind.ArgumentList)
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
        return inferredTypeInfo.Any(static (type, semanticModel) => GetDelegateType(type, semanticModel.Compilation).IsDelegateType(), semanticModel);
    }

    private static ITypeSymbol? GetDelegateType(TypeInferenceInfo typeInferenceInfo, Compilation compilation)
    {
        var typeSymbol = typeInferenceInfo.InferredType;
        if (typeInferenceInfo.IsParams && typeInferenceInfo.InferredType.IsArrayType())
        {
            typeSymbol = ((IArrayTypeSymbol)typeInferenceInfo.InferredType).ElementType;
        }

        return typeSymbol.GetDelegateType(compilation);
    }

    private static bool IsPotentialPatternVariableDeclaration(SyntaxToken token)
    {
        var patternSyntax = token.GetAncestor<PatternSyntax>();
        if (patternSyntax == null)
        {
            return false;
        }

        for (var current = patternSyntax; current != null; current = current.Parent as PatternSyntax)
        {
            // Patterns containing 'or' cannot contain valid variable declarations, e.g. 'e is 1 or int $$'
            if (current.IsKind(SyntaxKind.OrPattern))
            {
                return false;
            }

            // Patterns containing 'not' cannot be valid variable declarations, e.g. 'e is not int $$' and 'e is not (1 and int $$)'
            if (current.IsKind(SyntaxKind.NotPattern))
            {
                return false;
            }
        }

        // e is int o$$
        // e is { P: 1 } o$$
        var lastTokenInPattern = patternSyntax.GetLastToken();
        if (lastTokenInPattern.Parent is SingleVariableDesignationSyntax variableDesignationSyntax &&
            token.Parent == variableDesignationSyntax)
        {
            return patternSyntax is DeclarationPatternSyntax or RecursivePatternSyntax;
        }

        return false;
    }
}
