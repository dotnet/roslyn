// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.ChangeSignature;

using static CSharpSyntaxTokens;

[ExportLanguageService(typeof(AbstractChangeSignatureService), LanguageNames.CSharp), Shared]
internal sealed class CSharpChangeSignatureService : AbstractChangeSignatureService
{
    protected override SyntaxGenerator Generator => CSharpSyntaxGenerator.Instance;
    protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;

    private static readonly ImmutableArray<SyntaxKind> _declarationKinds =
    [
        SyntaxKind.MethodDeclaration,
        SyntaxKind.ConstructorDeclaration,
        SyntaxKind.IndexerDeclaration,
        SyntaxKind.DelegateDeclaration,
        SyntaxKind.SimpleLambdaExpression,
        SyntaxKind.ParenthesizedLambdaExpression,
        SyntaxKind.LocalFunctionStatement,
        SyntaxKind.RecordStructDeclaration,
        SyntaxKind.RecordDeclaration,
        SyntaxKind.StructDeclaration,
        SyntaxKind.ClassDeclaration,
    ];

    private static readonly ImmutableArray<SyntaxKind> _declarationAndInvocableKinds =
        [.. _declarationKinds,
            SyntaxKind.InvocationExpression,
            SyntaxKind.ElementAccessExpression,
            SyntaxKind.ThisConstructorInitializer,
            SyntaxKind.BaseConstructorInitializer,
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.ImplicitObjectCreationExpression,
            SyntaxKind.Attribute,
            SyntaxKind.NameMemberCref];

    private static readonly ImmutableArray<SyntaxKind> _updatableAncestorKinds =
    [
        SyntaxKind.ConstructorDeclaration,
        SyntaxKind.IndexerDeclaration,
        SyntaxKind.InvocationExpression,
        SyntaxKind.ElementAccessExpression,
        SyntaxKind.ThisConstructorInitializer,
        SyntaxKind.BaseConstructorInitializer,
        SyntaxKind.ObjectCreationExpression,
        SyntaxKind.Attribute,
        SyntaxKind.DelegateDeclaration,
        SyntaxKind.SimpleLambdaExpression,
        SyntaxKind.ParenthesizedLambdaExpression,
        SyntaxKind.NameMemberCref,
        SyntaxKind.PrimaryConstructorBaseType,
    ];

    private static readonly ImmutableArray<SyntaxKind> _updatableNodeKinds =
    [
        SyntaxKind.MethodDeclaration,
        SyntaxKind.LocalFunctionStatement,
        SyntaxKind.ConstructorDeclaration,
        SyntaxKind.IndexerDeclaration,
        SyntaxKind.InvocationExpression,
        SyntaxKind.ElementAccessExpression,
        SyntaxKind.ThisConstructorInitializer,
        SyntaxKind.BaseConstructorInitializer,
        SyntaxKind.ObjectCreationExpression,
        SyntaxKind.ImplicitObjectCreationExpression,
        SyntaxKind.Attribute,
        SyntaxKind.DelegateDeclaration,
        SyntaxKind.NameMemberCref,
        SyntaxKind.AnonymousMethodExpression,
        SyntaxKind.ParenthesizedLambdaExpression,
        SyntaxKind.SimpleLambdaExpression,
        SyntaxKind.RecordStructDeclaration,
        SyntaxKind.RecordDeclaration,
        SyntaxKind.StructDeclaration,
        SyntaxKind.ClassDeclaration,
        SyntaxKind.PrimaryConstructorBaseType,
    ];

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpChangeSignatureService()
    {
    }

    public override async Task<(ISymbol? symbol, int selectedIndex)> GetInvocationSymbolAsync(
        Document document, int position, bool restrictToDeclarations, CancellationToken cancellationToken)
    {
        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var token = root.FindToken(position != tree.Length ? position : Math.Max(0, position - 1));

        // Allow the user to invoke Change-Sig if they've written:   Goo(a, b, c);$$ 
        if (token.Kind() == SyntaxKind.SemicolonToken && token.Parent is StatementSyntax)
        {
            token = token.GetPreviousToken();
            position = token.Span.End;
        }

        if (token.Parent == null)
        {
            return default;
        }

        var matchingNode = GetMatchingNode(token.Parent, restrictToDeclarations);
        if (matchingNode == null)
        {
            return default;
        }

        // Don't show change-signature in the random whitespace/trivia for code.
        if (!matchingNode.Span.IntersectsWith(position))
        {
            return default;
        }

        // If we're actually on the declaration of some symbol, ensure that we're
        // in a good location for that symbol (i.e. not in the attributes/constraints).
        if (!InSymbolHeader(matchingNode, position))
        {
            return default;
        }

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var symbol = semanticModel.GetDeclaredSymbol(matchingNode, cancellationToken);
        if (symbol != null)
        {
            var selectedIndex = TryGetSelectedIndexFromDeclaration(position, matchingNode);
            return (symbol, selectedIndex);
        }

        if (matchingNode is ObjectCreationExpressionSyntax objectCreation &&
            token.Parent.AncestorsAndSelf().Any(a => a == objectCreation.Type))
        {
            var typeSymbol = semanticModel.GetSymbolInfo(objectCreation.Type, cancellationToken).Symbol;
            if (typeSymbol is INamedTypeSymbol {TypeKind: TypeKind.Delegate })
                return (typeSymbol, 0);
        }

        var symbolInfo = semanticModel.GetSymbolInfo(matchingNode, cancellationToken);
        var parameterIndex = 0;

        // If we're being called on an invocation and not a definition we need to find the selected argument index based on the original definition.
        var argumentList = matchingNode is ObjectCreationExpressionSyntax objCreation ? objCreation.ArgumentList
            : matchingNode.GetAncestorOrThis<InvocationExpressionSyntax>()?.ArgumentList;
        var argument = argumentList?.Arguments.FirstOrDefault(a => a.Span.Contains(position));
        if (argument != null)
        {
            parameterIndex = GetParameterIndexFromInvocationArgument(argument, document, semanticModel, cancellationToken);
        }

        return (symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault(), parameterIndex);
    }

    private static int TryGetSelectedIndexFromDeclaration(int position, SyntaxNode matchingNode)
    {
        var parameters = matchingNode.ChildNodes().OfType<BaseParameterListSyntax>().SingleOrDefault();
        return parameters != null ? GetParameterIndex(parameters.Parameters, position) : 0;
    }

    private static SyntaxNode? GetMatchingNode(SyntaxNode node, bool restrictToDeclarations)
    {
        var matchKinds = restrictToDeclarations
            ? _declarationKinds
            : _declarationAndInvocableKinds;

        for (var current = node; current != null; current = current.Parent)
        {
            if (restrictToDeclarations &&
                current.Kind() == SyntaxKind.Block || current.Kind() == SyntaxKind.ArrowExpressionClause)
            {
                return null;
            }

            if (matchKinds.Contains(current.Kind()))
            {
                return current;
            }
        }

        return null;
    }

    private static bool InSymbolHeader(SyntaxNode matchingNode, int position)
    {
        // Caret has to be after the attributes if the symbol has any.
        var lastAttributes = matchingNode.ChildNodes().LastOrDefault(n => n is AttributeListSyntax);
        var start = lastAttributes?.GetLastToken().GetNextToken().SpanStart ??
                    matchingNode.SpanStart;

        if (position < start)
        {
            return false;
        }

        // If the symbol has a parameter list, then the caret shouldn't be past the end of it.
        var parameterList = matchingNode.ChildNodes().LastOrDefault(n => n is ParameterListSyntax);
        if (parameterList != null)
        {
            return position <= parameterList.FullSpan.End;
        }

        // Case we haven't handled yet.  Just assume we're in the header.
        return true;
    }

    public override SyntaxNode? FindNodeToUpdate(Document document, SyntaxNode node)
    {
        if (_updatableNodeKinds.Contains(node.Kind()))
        {
            return node;
        }

        // TODO: file bug about this: var invocation = csnode.Ancestors().FirstOrDefault(a => a.Kind == SyntaxKind.InvocationExpression);
        var matchingNode = node.AncestorsAndSelf().FirstOrDefault(n => _updatableAncestorKinds.Contains(n.Kind()));
        if (matchingNode == null)
        {
            return null;
        }

        var nodeContainingOriginal = GetNodeContainingTargetNode(matchingNode);
        if (nodeContainingOriginal == null)
        {
            return null;
        }

        return node.AncestorsAndSelf().Any(n => n == nodeContainingOriginal) ? matchingNode : null;
    }

    private static SyntaxNode? GetNodeContainingTargetNode(SyntaxNode matchingNode)
    {
        switch (matchingNode.Kind())
        {
            case SyntaxKind.InvocationExpression:
                return ((InvocationExpressionSyntax)matchingNode).Expression;

            case SyntaxKind.ElementAccessExpression:
                return ((ElementAccessExpressionSyntax)matchingNode).ArgumentList;

            case SyntaxKind.ObjectCreationExpression:
                return ((ObjectCreationExpressionSyntax)matchingNode).Type;

            case SyntaxKind.PrimaryConstructorBaseType:
            case SyntaxKind.ConstructorDeclaration:
            case SyntaxKind.IndexerDeclaration:
            case SyntaxKind.ThisConstructorInitializer:
            case SyntaxKind.BaseConstructorInitializer:
            case SyntaxKind.Attribute:
            case SyntaxKind.DelegateDeclaration:
            case SyntaxKind.NameMemberCref:
                return matchingNode;

            default:
                return null;
        }
    }

    public override SyntaxNode ChangeSignature(
        SemanticDocument document,
        ISymbol declarationSymbol,
        SyntaxNode potentiallyUpdatedNode,
        SyntaxNode originalNode,
        SignatureChange signaturePermutation,
        LineFormattingOptions lineFormattingOptions,
        CancellationToken cancellationToken)
    {
        var updatedNode = potentiallyUpdatedNode as CSharpSyntaxNode;

        // Update <param> tags.
        if (updatedNode?.Kind()
                is SyntaxKind.MethodDeclaration
                or SyntaxKind.ConstructorDeclaration
                or SyntaxKind.IndexerDeclaration
                or SyntaxKind.DelegateDeclaration
                or SyntaxKind.RecordStructDeclaration
                or SyntaxKind.RecordDeclaration
                or SyntaxKind.StructDeclaration
                or SyntaxKind.ClassDeclaration)
        {
            var updatedLeadingTrivia = UpdateParamTagsInLeadingTrivia(
                document.Document, updatedNode, declarationSymbol, signaturePermutation, lineFormattingOptions);
            if (updatedLeadingTrivia != default && !updatedLeadingTrivia.IsEmpty)
            {
                updatedNode = updatedNode.WithLeadingTrivia(updatedLeadingTrivia);
            }
        }

        // Update declarations parameter lists
        if (updatedNode is MethodDeclarationSyntax method)
        {
            var updatedParameters = UpdateDeclaration(method.ParameterList.Parameters, signaturePermutation, CreateNewParameterSyntax);
            return method.WithParameterList(method.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(ChangeSignatureFormattingAnnotation));
        }

        if (updatedNode is TypeDeclarationSyntax { ParameterList: not null } typeWithParameters)
        {
            var updatedParameters = UpdateDeclaration(typeWithParameters.ParameterList.Parameters, signaturePermutation, CreateNewParameterSyntax);
            return typeWithParameters.WithParameterList(typeWithParameters.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(ChangeSignatureFormattingAnnotation));
        }

        if (updatedNode is LocalFunctionStatementSyntax localFunction)
        {
            var updatedParameters = UpdateDeclaration(localFunction.ParameterList.Parameters, signaturePermutation, CreateNewParameterSyntax);
            return localFunction.WithParameterList(localFunction.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(ChangeSignatureFormattingAnnotation));
        }

        if (updatedNode is ConstructorDeclarationSyntax constructor)
        {
            var updatedParameters = UpdateDeclaration(constructor.ParameterList.Parameters, signaturePermutation, CreateNewParameterSyntax);
            return constructor.WithParameterList(constructor.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(ChangeSignatureFormattingAnnotation));
        }

        if (updatedNode is IndexerDeclarationSyntax indexer)
        {
            var updatedParameters = UpdateDeclaration(indexer.ParameterList.Parameters, signaturePermutation, CreateNewParameterSyntax);
            return indexer.WithParameterList(indexer.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(ChangeSignatureFormattingAnnotation));
        }

        if (updatedNode is DelegateDeclarationSyntax delegateDeclaration)
        {
            var updatedParameters = UpdateDeclaration(delegateDeclaration.ParameterList.Parameters, signaturePermutation, CreateNewParameterSyntax);
            return delegateDeclaration.WithParameterList(delegateDeclaration.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(ChangeSignatureFormattingAnnotation));
        }

        if (updatedNode is AnonymousMethodExpressionSyntax anonymousMethod)
        {
            // Delegates may omit parameters in C#
            if (anonymousMethod.ParameterList == null)
            {
                return anonymousMethod;
            }

            var updatedParameters = UpdateDeclaration(anonymousMethod.ParameterList.Parameters, signaturePermutation, CreateNewParameterSyntax);
            return anonymousMethod.WithParameterList(anonymousMethod.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(ChangeSignatureFormattingAnnotation));
        }

        if (updatedNode is SimpleLambdaExpressionSyntax lambda)
        {
            if (signaturePermutation.UpdatedConfiguration.ToListOfParameters().Any())
            {
                var updatedParameters = UpdateDeclaration([lambda.Parameter], signaturePermutation, CreateNewParameterSyntax);
                return ParenthesizedLambdaExpression(
                    lambda.AsyncKeyword,
                    ParameterList(updatedParameters),
                    lambda.ArrowToken,
                    lambda.Body);
            }
            else
            {
                // No parameters. Change to a parenthesized lambda expression
                var emptyParameterList = ParameterList()
                    .WithLeadingTrivia(lambda.Parameter.GetLeadingTrivia())
                    .WithTrailingTrivia(lambda.Parameter.GetTrailingTrivia());

                return ParenthesizedLambdaExpression(lambda.AsyncKeyword, emptyParameterList, lambda.ArrowToken, lambda.Body);
            }
        }

        if (updatedNode is ParenthesizedLambdaExpressionSyntax parenLambda)
        {
            var doNotSkipParameterType = parenLambda.ParameterList.Parameters.FirstOrDefault()?.Type != null;

            var updatedParameters = UpdateDeclaration(
                parenLambda.ParameterList.Parameters,
                signaturePermutation,
                p => CreateNewParameterSyntax(p, !doNotSkipParameterType));
            return parenLambda.WithParameterList(parenLambda.ParameterList.WithParameters(updatedParameters));
        }

        // Handle references in crefs
        if (updatedNode is NameMemberCrefSyntax nameMemberCref)
        {
            if (nameMemberCref.Parameters == null ||
                !nameMemberCref.Parameters.Parameters.Any())
            {
                return nameMemberCref;
            }

            var newParameters = UpdateDeclaration(nameMemberCref.Parameters.Parameters, signaturePermutation, CreateNewCrefParameterSyntax);

            var newCrefParameterList = nameMemberCref.Parameters.WithParameters(newParameters);
            return nameMemberCref.WithParameters(newCrefParameterList);
        }

        var semanticModel = document.SemanticModel;

        // Update reference site argument lists
        if (updatedNode is InvocationExpressionSyntax invocation)
        {
            var symbolInfo = semanticModel.GetSymbolInfo((InvocationExpressionSyntax)originalNode, cancellationToken);

            return invocation.WithArgumentList(
                UpdateArgumentList(
                    document,
                    declarationSymbol,
                    signaturePermutation,
                    invocation.ArgumentList,
                    symbolInfo.Symbol is IMethodSymbol { MethodKind: MethodKind.ReducedExtension },
                    IsParamsArrayExpanded(semanticModel, invocation, symbolInfo, cancellationToken),
                    originalNode.SpanStart,
                    cancellationToken));
        }

        // Handles both ObjectCreationExpressionSyntax and ImplicitObjectCreationExpressionSyntax
        if (updatedNode is BaseObjectCreationExpressionSyntax objCreation)
        {
            if (objCreation.ArgumentList == null)
            {
                return updatedNode;
            }

            var symbolInfo = semanticModel.GetSymbolInfo((BaseObjectCreationExpressionSyntax)originalNode, cancellationToken);

            return objCreation.WithArgumentList(
                UpdateArgumentList(
                    document,
                    declarationSymbol,
                    signaturePermutation,
                    objCreation.ArgumentList,
                    isReducedExtensionMethod: false,
                    IsParamsArrayExpanded(semanticModel, objCreation, symbolInfo, cancellationToken),
                    originalNode.SpanStart,
                    cancellationToken));
        }

        if (updatedNode is ConstructorInitializerSyntax constructorInit)
        {
            var symbolInfo = semanticModel.GetSymbolInfo((ConstructorInitializerSyntax)originalNode, cancellationToken);

            return constructorInit.WithArgumentList(
                UpdateArgumentList(
                    document,
                    declarationSymbol,
                    signaturePermutation,
                    constructorInit.ArgumentList,
                    isReducedExtensionMethod: false,
                    IsParamsArrayExpanded(semanticModel, constructorInit, symbolInfo, cancellationToken),
                    originalNode.SpanStart,
                    cancellationToken));
        }

        if (updatedNode is ElementAccessExpressionSyntax elementAccess)
        {
            var symbolInfo = semanticModel.GetSymbolInfo((ElementAccessExpressionSyntax)originalNode, cancellationToken);

            return elementAccess.WithArgumentList(
                UpdateArgumentList(
                    document,
                    declarationSymbol,
                    signaturePermutation,
                    elementAccess.ArgumentList,
                    isReducedExtensionMethod: false,
                    IsParamsArrayExpanded(semanticModel, elementAccess, symbolInfo, cancellationToken),
                    originalNode.SpanStart,
                    cancellationToken));
        }

        if (updatedNode is AttributeSyntax attribute)
        {
            var symbolInfo = semanticModel.GetSymbolInfo((AttributeSyntax)originalNode, cancellationToken);

            if (attribute.ArgumentList == null)
            {
                return updatedNode;
            }

            return attribute.WithArgumentList(
                UpdateAttributeArgumentList(
                    document,
                    declarationSymbol,
                    signaturePermutation,
                    attribute.ArgumentList,
                    isReducedExtensionMethod: false,
                    IsParamsArrayExpanded(semanticModel, attribute, symbolInfo, cancellationToken),
                    originalNode.SpanStart,
                    cancellationToken));
        }

        if (updatedNode is PrimaryConstructorBaseTypeSyntax primaryConstructor)
        {
            var symbolInfo = semanticModel.GetSymbolInfo((PrimaryConstructorBaseTypeSyntax)originalNode, cancellationToken);

            return primaryConstructor.WithArgumentList(
                UpdateArgumentList(
                    document,
                    declarationSymbol,
                    signaturePermutation,
                    primaryConstructor.ArgumentList,
                    isReducedExtensionMethod: false,
                    IsParamsArrayExpanded(semanticModel, primaryConstructor, symbolInfo, cancellationToken),
                    originalNode.SpanStart,
                    cancellationToken));
        }

        Debug.Assert(false, "Unknown reference location");
        return null;
    }

    private T UpdateArgumentList<T>(
        SemanticDocument document,
        ISymbol declarationSymbol,
        SignatureChange signaturePermutation,
        T argumentList,
        bool isReducedExtensionMethod,
        bool isParamsArrayExpanded,
        int position,
        CancellationToken cancellationToken) where T : BaseArgumentListSyntax
    {
        // Reorders and removes arguments
        // e.g. P(a, b, c) ==> P(c, a)
        var newArguments = PermuteArgumentList(
            declarationSymbol,
            argumentList.Arguments,
            signaturePermutation.WithoutAddedParameters(),
            isReducedExtensionMethod);

        // Adds new arguments into the updated list
        // e.g. P(c, a) ==> P(x, c, a, y)
        newArguments = AddNewArgumentsToList(
            document,
            declarationSymbol,
            newArguments,
            argumentList.Arguments,
            signaturePermutation,
            isReducedExtensionMethod,
            isParamsArrayExpanded,
            generateAttributeArguments: false,
            position,
            cancellationToken);

        return (T)argumentList
            .WithArguments(newArguments)
            .WithAdditionalAnnotations(ChangeSignatureFormattingAnnotation);
    }

    private AttributeArgumentListSyntax UpdateAttributeArgumentList(
        SemanticDocument document,
        ISymbol declarationSymbol,
        SignatureChange signaturePermutation,
        AttributeArgumentListSyntax argumentList,
        bool isReducedExtensionMethod,
        bool isParamsArrayExpanded,
        int position,
        CancellationToken cancellationToken)
    {
        var newArguments = PermuteAttributeArgumentList(
            declarationSymbol,
            argumentList.Arguments,
            signaturePermutation.WithoutAddedParameters());

        newArguments = AddNewArgumentsToList(
            document,
            declarationSymbol,
            newArguments,
            argumentList.Arguments,
            signaturePermutation,
            isReducedExtensionMethod,
            isParamsArrayExpanded,
            generateAttributeArguments: true,
            position,
            cancellationToken);

        return argumentList
            .WithArguments(newArguments)
            .WithAdditionalAnnotations(ChangeSignatureFormattingAnnotation);
    }

    private static bool IsParamsArrayExpanded(SemanticModel semanticModel, SyntaxNode node, SymbolInfo symbolInfo, CancellationToken cancellationToken)
    {
        if (symbolInfo.Symbol == null)
        {
            return false;
        }

        int argumentCount;
        bool lastArgumentIsNamed;
        ExpressionSyntax lastArgumentExpression;

        if (node is AttributeSyntax attribute)
        {
            if (attribute.ArgumentList == null)
            {
                return false;
            }

            argumentCount = attribute.ArgumentList.Arguments.Count;
            lastArgumentIsNamed = attribute.ArgumentList.Arguments.LastOrDefault()?.NameColon != null ||
                attribute.ArgumentList.Arguments.LastOrDefault()?.NameEquals != null;

            var lastArgument = attribute.ArgumentList.Arguments.LastOrDefault();
            if (lastArgument == null)
            {
                return false;
            }

            lastArgumentExpression = lastArgument.Expression;
        }
        else
        {
            BaseArgumentListSyntax? argumentList = node switch
            {
                InvocationExpressionSyntax invocation => invocation.ArgumentList,
                BaseObjectCreationExpressionSyntax objectCreation => objectCreation.ArgumentList,
                ConstructorInitializerSyntax constructorInitializer => constructorInitializer.ArgumentList,
                ElementAccessExpressionSyntax elementAccess => elementAccess.ArgumentList,
                PrimaryConstructorBaseTypeSyntax primaryConstructor => primaryConstructor.ArgumentList,
                _ => throw ExceptionUtilities.UnexpectedValue(node.Kind())
            };

            if (argumentList == null)
            {
                return false;
            }

            argumentCount = argumentList.Arguments.Count;
            lastArgumentIsNamed = argumentList.Arguments.LastOrDefault()?.NameColon != null;

            var lastArgument = argumentList.Arguments.LastOrDefault();
            if (lastArgument == null)
            {
                return false;
            }

            lastArgumentExpression = lastArgument.Expression;
        }

        return IsParamsArrayExpandedHelper(symbolInfo.Symbol, argumentCount, lastArgumentIsNamed, semanticModel, lastArgumentExpression, cancellationToken);
    }

    private static ParameterSyntax CreateNewParameterSyntax(AddedParameter addedParameter)
        => CreateNewParameterSyntax(addedParameter, skipParameterType: false);

    private static ParameterSyntax CreateNewParameterSyntax(AddedParameter addedParameter, bool skipParameterType)
    {
        var equalsValueClause = addedParameter.HasDefaultValue
            ? EqualsValueClause(ParseExpression(addedParameter.DefaultValue))
            : null;

        return Parameter(
            attributeLists: default,
            modifiers: default,
            type: skipParameterType
                ? null
                : addedParameter.Type.GenerateTypeSyntax(),
            Identifier(addedParameter.Name),
            @default: equalsValueClause);
    }

    private static CrefParameterSyntax CreateNewCrefParameterSyntax(AddedParameter addedParameter)
        => CrefParameter(type: addedParameter.Type.GenerateTypeSyntax())
            .WithLeadingTrivia(ElasticSpace);

    private SeparatedSyntaxList<T> UpdateDeclaration<T>(
        SeparatedSyntaxList<T> list,
        SignatureChange updatedSignature,
        Func<AddedParameter, T> createNewParameterMethod) where T : SyntaxNode
    {
        var (parameters, separators) = base.UpdateDeclarationBase<T>(list, updatedSignature, createNewParameterMethod);
        return SeparatedList(parameters, separators);
    }

    protected override T TransferLeadingWhitespaceTrivia<T>(T newArgument, SyntaxNode oldArgument)
    {
        var oldTrivia = oldArgument.GetLeadingTrivia();
        var oldOnlyHasWhitespaceTrivia = oldTrivia.All(t => t.IsKind(SyntaxKind.WhitespaceTrivia));

        var newTrivia = newArgument.GetLeadingTrivia();
        var newOnlyHasWhitespaceTrivia = newTrivia.All(t => t.IsKind(SyntaxKind.WhitespaceTrivia));

        if (oldOnlyHasWhitespaceTrivia && newOnlyHasWhitespaceTrivia)
        {
            newArgument = newArgument.WithLeadingTrivia(oldTrivia);
        }

        return newArgument;
    }

    private SeparatedSyntaxList<TArgumentSyntax> AddNewArgumentsToList<TArgumentSyntax>(
        SemanticDocument document,
        ISymbol declarationSymbol,
        SeparatedSyntaxList<TArgumentSyntax> newArguments,
        SeparatedSyntaxList<TArgumentSyntax> originalArguments,
        SignatureChange signaturePermutation,
        bool isReducedExtensionMethod,
        bool isParamsArrayExpanded,
        bool generateAttributeArguments,
        int position,
        CancellationToken cancellationToken)
        where TArgumentSyntax : SyntaxNode
    {
        var newArgumentList = AddNewArgumentsToList(
            document, declarationSymbol, newArguments, signaturePermutation,
            isReducedExtensionMethod, isParamsArrayExpanded, generateAttributeArguments, position, cancellationToken);

        return SeparatedList(
            TransferLeadingWhitespaceTrivia(newArgumentList, originalArguments),
            newArgumentList.GetSeparators());
    }

    private SeparatedSyntaxList<AttributeArgumentSyntax> PermuteAttributeArgumentList(
        ISymbol declarationSymbol,
        SeparatedSyntaxList<AttributeArgumentSyntax> arguments,
        SignatureChange updatedSignature)
    {
        var newArguments = PermuteArguments(declarationSymbol, [.. arguments.Select(a => UnifiedArgumentSyntax.Create(a))],
            updatedSignature);
        var numSeparatorsToSkip = arguments.Count - newArguments.Length;

        // copy whitespace trivia from original position
        var newArgumentsWithTrivia = TransferLeadingWhitespaceTrivia(
            newArguments.Select(a => (AttributeArgumentSyntax)(UnifiedArgumentSyntax)a), arguments);

        return SeparatedList(newArgumentsWithTrivia, GetSeparators(arguments, numSeparatorsToSkip));
    }

    private SeparatedSyntaxList<ArgumentSyntax> PermuteArgumentList(
        ISymbol declarationSymbol,
        SeparatedSyntaxList<ArgumentSyntax> arguments,
        SignatureChange updatedSignature,
        bool isReducedExtensionMethod = false)
    {
        var newArguments = PermuteArguments(
            declarationSymbol,
            [.. arguments.Select(a => UnifiedArgumentSyntax.Create(a))],
            updatedSignature,
            isReducedExtensionMethod);

        // copy whitespace trivia from original position
        var newArgumentsWithTrivia = TransferLeadingWhitespaceTrivia(
            newArguments.Select(a => (ArgumentSyntax)(UnifiedArgumentSyntax)a), arguments);

        var numSeparatorsToSkip = arguments.Count - newArguments.Length;
        return SeparatedList(newArgumentsWithTrivia, GetSeparators(arguments, numSeparatorsToSkip));
    }

    private ImmutableArray<T> TransferLeadingWhitespaceTrivia<T, U>(IEnumerable<T> newArguments, SeparatedSyntaxList<U> oldArguments)
        where T : SyntaxNode
        where U : SyntaxNode
    {
        var result = ImmutableArray.CreateBuilder<T>();
        var index = 0;
        foreach (var newArgument in newArguments)
        {
            result.Add(index < oldArguments.Count
                ? TransferLeadingWhitespaceTrivia(newArgument, oldArguments[index])
                : newArgument);

            index++;
        }

        return result.ToImmutableAndClear();
    }

    private ImmutableArray<SyntaxTrivia> UpdateParamTagsInLeadingTrivia(
        Document document,
        CSharpSyntaxNode node,
        ISymbol declarationSymbol,
        SignatureChange updatedSignature,
        LineFormattingOptions lineFormattingOptions)
    {
        if (!node.HasLeadingTrivia)
            return [];

        var paramNodes = node
            .DescendantNodes(descendIntoTrivia: true)
            .OfType<XmlElementSyntax>()
            .Where(e => e.StartTag.Name.ToString() == DocumentationCommentXmlNames.ParameterElementName);

        var permutedParamNodes = VerifyAndPermuteParamNodes(paramNodes, declarationSymbol, updatedSignature);
        if (permutedParamNodes.IsEmpty)
            return [];

        return GetPermutedDocCommentTrivia(node, permutedParamNodes, document.Project.Services, lineFormattingOptions);
    }

    private ImmutableArray<SyntaxNode> VerifyAndPermuteParamNodes(IEnumerable<XmlElementSyntax> paramNodes, ISymbol declarationSymbol, SignatureChange updatedSignature)
    {
        // Only reorder if count and order match originally.
        var originalParameters = updatedSignature.OriginalConfiguration.ToListOfParameters();
        var reorderedParameters = updatedSignature.UpdatedConfiguration.ToListOfParameters();

        var declaredParameters = GetParameters(declarationSymbol);

        if (paramNodes.Count() != declaredParameters.Length)
        {
            return [];
        }

        // No parameters originally, so no param nodes to permute.
        if (declaredParameters.Length == 0)
        {
            return [];
        }

        var dictionary = new Dictionary<string, XmlElementSyntax>();
        var i = 0;
        foreach (var paramNode in paramNodes)
        {
            var nameAttribute = paramNode.StartTag.Attributes.FirstOrDefault(a => a.Name.ToString().Equals("name", StringComparison.OrdinalIgnoreCase));
            if (nameAttribute == null)
            {
                return [];
            }

            var identifier = nameAttribute.DescendantNodes(descendIntoTrivia: true).OfType<IdentifierNameSyntax>().FirstOrDefault();
            if (identifier == null || identifier.ToString() != declaredParameters.ElementAt(i).Name)
            {
                return [];
            }

            dictionary.Add(originalParameters[i].Name.ToString(), paramNode);
            i++;
        }

        // Everything lines up, so permute them.
        var permutedParams = ArrayBuilder<SyntaxNode>.GetInstance();
        foreach (var parameter in reorderedParameters)
        {
            if (dictionary.TryGetValue(parameter.Name, out var permutedParam))
            {
                permutedParams.Add(permutedParam);
            }
            else
            {
                permutedParams.Add(XmlElement(
                    XmlElementStartTag(
                        XmlName("param"),
                        [XmlNameAttribute(parameter.Name)]),
                    XmlElementEndTag(XmlName("param"))));
            }
        }

        return permutedParams.ToImmutableAndFree();
    }

    public override async Task<ImmutableArray<ISymbol>> DetermineCascadedSymbolsFromDelegateInvokeAsync(
        IMethodSymbol symbol,
        Document document,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var convertedMethodNodes);

        foreach (var node in root.DescendantNodes())
        {
            if (!node.IsKind(SyntaxKind.IdentifierName) ||
                !semanticModel.GetMemberGroup(node, cancellationToken).Any())
            {
                continue;
            }

            var convertedType = (ISymbol?)semanticModel.GetTypeInfo(node, cancellationToken).ConvertedType;
            convertedType = convertedType?.OriginalDefinition;

            if (convertedType != null)
            {
                convertedType = await SymbolFinder.FindSourceDefinitionAsync(convertedType, document.Project.Solution, cancellationToken).ConfigureAwait(false)
                    ?? convertedType;
            }

            if (Equals(convertedType, symbol.ContainingType))
                convertedMethodNodes.Add(node);
        }

        var convertedMethodGroups = convertedMethodNodes
            .Select(n => semanticModel.GetSymbolInfo(n, cancellationToken).Symbol)
            .WhereNotNull()
            .ToImmutableArray();

        return convertedMethodGroups;
    }

    protected override ImmutableArray<AbstractFormattingRule> GetFormattingRules(Document document)
        => [.. Formatter.GetDefaultFormattingRules(document), new ChangeSignatureFormattingRule()];

    protected override TArgumentSyntax AddNameToArgument<TArgumentSyntax>(TArgumentSyntax newArgument, string name)
    {
        return newArgument switch
        {
            ArgumentSyntax a => (TArgumentSyntax)(SyntaxNode)a.WithNameColon(NameColon(name)),
            AttributeArgumentSyntax a => (TArgumentSyntax)(SyntaxNode)a.WithNameColon(NameColon(name)),
            _ => throw ExceptionUtilities.UnexpectedValue(newArgument.Kind())
        };
    }

    protected override TArgumentSyntax CreateExplicitParamsArrayFromIndividualArguments<TArgumentSyntax>(SeparatedSyntaxList<TArgumentSyntax> newArguments, int indexInExistingList, IParameterSymbol parameterSymbol)
    {
        RoslynDebug.Assert(parameterSymbol.IsParams);

        // These arguments are part of a params array, and should not have any modifiers, making it okay to just use their expressions.
        var listOfArguments = SeparatedList(newArguments.Skip(indexInExistingList).Select(a => ((ArgumentSyntax)(SyntaxNode)a).Expression), newArguments.GetSeparators().Skip(indexInExistingList));
        var initializerExpression = InitializerExpression(SyntaxKind.ArrayInitializerExpression, listOfArguments);
        var objectCreation = ArrayCreationExpression((ArrayTypeSyntax)parameterSymbol.Type.GenerateTypeSyntax(), initializerExpression);
        return (TArgumentSyntax)(SyntaxNode)Argument(objectCreation);
    }

    protected override bool SupportsOptionalAndParamsArrayParametersSimultaneously()
    {
        return true;
    }

    protected override SyntaxToken CommaTokenWithElasticSpace()
        => CommaToken.WithTrailingTrivia(ElasticSpace);

    protected override bool TryGetRecordPrimaryConstructor(INamedTypeSymbol typeSymbol, [NotNullWhen(true)] out IMethodSymbol? primaryConstructor)
        => typeSymbol.TryGetPrimaryConstructor(out primaryConstructor);

    protected override ImmutableArray<IParameterSymbol> GetParameters(ISymbol declarationSymbol)
    {
        var declaredParameters = declarationSymbol.GetParameters();
        if (declarationSymbol is INamedTypeSymbol namedTypeSymbol &&
            namedTypeSymbol.TryGetPrimaryConstructor(out var primaryConstructor))
        {
            declaredParameters = primaryConstructor.Parameters;
        }

        return declaredParameters;
    }
}
