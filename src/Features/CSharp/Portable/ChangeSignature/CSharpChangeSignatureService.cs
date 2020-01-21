// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ChangeSignature
{
    [ExportLanguageService(typeof(AbstractChangeSignatureService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpChangeSignatureService : AbstractChangeSignatureService
    {
        private static readonly ImmutableArray<SyntaxKind> _declarationKinds = ImmutableArray.Create(
            SyntaxKind.MethodDeclaration,
            SyntaxKind.ConstructorDeclaration,
            SyntaxKind.IndexerDeclaration,
            SyntaxKind.DelegateDeclaration,
            SyntaxKind.SimpleLambdaExpression,
            SyntaxKind.ParenthesizedLambdaExpression,
            SyntaxKind.LocalFunctionStatement);

        private static readonly ImmutableArray<SyntaxKind> _declarationAndInvocableKinds =
            _declarationKinds.Concat(ImmutableArray.Create(
                SyntaxKind.InvocationExpression,
                SyntaxKind.ElementAccessExpression,
                SyntaxKind.ThisConstructorInitializer,
                SyntaxKind.BaseConstructorInitializer,
                SyntaxKind.ObjectCreationExpression,
                SyntaxKind.Attribute,
                SyntaxKind.NameMemberCref));

        private static readonly ImmutableArray<SyntaxKind> _updatableAncestorKinds = ImmutableArray.Create(
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
            SyntaxKind.NameMemberCref);

        private static readonly ImmutableArray<SyntaxKind> _updatableNodeKinds = ImmutableArray.Create(
            SyntaxKind.MethodDeclaration,
            SyntaxKind.LocalFunctionStatement,
            SyntaxKind.ConstructorDeclaration,
            SyntaxKind.IndexerDeclaration,
            SyntaxKind.InvocationExpression,
            SyntaxKind.ElementAccessExpression,
            SyntaxKind.ThisConstructorInitializer,
            SyntaxKind.BaseConstructorInitializer,
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.Attribute,
            SyntaxKind.DelegateDeclaration,
            SyntaxKind.NameMemberCref,
            SyntaxKind.AnonymousMethodExpression,
            SyntaxKind.ParenthesizedLambdaExpression,
            SyntaxKind.SimpleLambdaExpression);

        [ImportingConstructor]
        public CSharpChangeSignatureService()
        {
        }

        public override async Task<(ISymbol symbol, int selectedIndex)> GetInvocationSymbolAsync(
            Document document, int position, bool restrictToDeclarations, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var token = root.FindToken(position != tree.Length ? position : Math.Max(0, position - 1));

            // Allow the user to invoke Change-Sig if they've written:   Goo(a, b, c);$$ 
            if (token.Kind() == SyntaxKind.SemicolonToken && token.Parent is StatementSyntax)
            {
                token = token.GetPreviousToken();
                position = token.Span.End;
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

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbol = semanticModel.GetDeclaredSymbol(matchingNode, cancellationToken);
            if (symbol != null)
            {
                var selectedIndex = TryGetSelectedIndexFromDeclaration(position, matchingNode);
                return (symbol, selectedIndex);
            }

            if (matchingNode.IsKind(SyntaxKind.ObjectCreationExpression))
            {
                var objectCreation = matchingNode as ObjectCreationExpressionSyntax;

                if (token.Parent.AncestorsAndSelf().Any(a => a == objectCreation.Type))
                {
                    var typeSymbol = semanticModel.GetSymbolInfo(objectCreation.Type, cancellationToken).Symbol;

                    if (typeSymbol != null && typeSymbol.IsKind(SymbolKind.NamedType) && (typeSymbol as ITypeSymbol).TypeKind == TypeKind.Delegate)
                    {
                        return (typeSymbol, 0);
                    }
                }
            }

            var symbolInfo = semanticModel.GetSymbolInfo(matchingNode, cancellationToken);
            return (symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault(), 0);
        }

        private static int TryGetSelectedIndexFromDeclaration(int position, SyntaxNode matchingNode)
        {
            var parameters = matchingNode.ChildNodes().OfType<BaseParameterListSyntax>().SingleOrDefault();
            return parameters != null ? GetParameterIndex(parameters.Parameters, position) : 0;
        }

        // Find the position to insert the new parameter.
        // We will insert a new comma and a parameter.
        protected override int? TryGetInsertPositionFromDeclaration(SyntaxNode matchingNode)
        {
            var parameters = matchingNode.ChildNodes().OfType<BaseParameterListSyntax>().SingleOrDefault();

            if (parameters == null)
            {
                return null;
            }

            switch (parameters)
            {
                case ParameterListSyntax parameterListSyntax:
                    return parameterListSyntax.CloseParenToken.SpanStart;
                case BracketedParameterListSyntax bracketedParameterListSyntax:
                    return bracketedParameterListSyntax.CloseBracketToken.SpanStart;
            }

            return null;
        }

        private SyntaxNode GetMatchingNode(SyntaxNode node, bool restrictToDeclarations)
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

        private bool InSymbolHeader(SyntaxNode matchingNode, int position)
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

        public override SyntaxNode FindNodeToUpdate(Document document, SyntaxNode node)
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

        private SyntaxNode GetNodeContainingTargetNode(SyntaxNode matchingNode)
        {
            switch (matchingNode.Kind())
            {
                case SyntaxKind.InvocationExpression:
                    return (matchingNode as InvocationExpressionSyntax).Expression;

                case SyntaxKind.ElementAccessExpression:
                    return (matchingNode as ElementAccessExpressionSyntax).ArgumentList;

                case SyntaxKind.ObjectCreationExpression:
                    return (matchingNode as ObjectCreationExpressionSyntax).Type;

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

        public override async Task<SyntaxNode> ChangeSignatureAsync(
            Document document,
            ISymbol declarationSymbol,
            SyntaxNode potentiallyUpdatedNode,
            SyntaxNode originalNode,
            SignatureChange signaturePermutation,
            CancellationToken cancellationToken)
        {
            var updatedNode = potentiallyUpdatedNode as CSharpSyntaxNode;

            // Update <param> tags.
            if (updatedNode.IsKind(SyntaxKind.MethodDeclaration) ||
                updatedNode.IsKind(SyntaxKind.ConstructorDeclaration) ||
                updatedNode.IsKind(SyntaxKind.IndexerDeclaration) ||
                updatedNode.IsKind(SyntaxKind.DelegateDeclaration))
            {
                var updatedLeadingTrivia = UpdateParamTagsInLeadingTrivia(document, updatedNode, declarationSymbol, signaturePermutation);
                if (updatedLeadingTrivia != null)
                {
                    updatedNode = updatedNode.WithLeadingTrivia(updatedLeadingTrivia);
                }
            }

            // Update declarations parameter lists
            if (updatedNode.IsKind(SyntaxKind.MethodDeclaration))
            {
                var method = (MethodDeclarationSyntax)updatedNode;
                var updatedParameters = PermuteDeclaration(method.ParameterList.Parameters, signaturePermutation, CreateNewParameterSyntax);
                return method.WithParameterList(method.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.LocalFunctionStatement))
            {
                var localFunction = (LocalFunctionStatementSyntax)updatedNode;
                var updatedParameters = PermuteDeclaration(localFunction.ParameterList.Parameters, signaturePermutation, CreateNewParameterSyntax);
                return localFunction.WithParameterList(localFunction.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.ConstructorDeclaration))
            {
                var constructor = (ConstructorDeclarationSyntax)updatedNode;
                var updatedParameters = PermuteDeclaration(constructor.ParameterList.Parameters, signaturePermutation, CreateNewParameterSyntax);
                return constructor.WithParameterList(constructor.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.IndexerDeclaration))
            {
                var indexer = (IndexerDeclarationSyntax)updatedNode;
                var updatedParameters = PermuteDeclaration(indexer.ParameterList.Parameters, signaturePermutation, CreateNewParameterSyntax);
                return indexer.WithParameterList(indexer.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.DelegateDeclaration))
            {
                var delegateDeclaration = (DelegateDeclarationSyntax)updatedNode;
                var updatedParameters = PermuteDeclaration(delegateDeclaration.ParameterList.Parameters, signaturePermutation, CreateNewParameterSyntax);
                return delegateDeclaration.WithParameterList(delegateDeclaration.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.AnonymousMethodExpression))
            {
                var anonymousMethod = (AnonymousMethodExpressionSyntax)updatedNode;

                // Delegates may omit parameters in C#
                if (anonymousMethod.ParameterList == null)
                {
                    return anonymousMethod;
                }

                var updatedParameters = PermuteDeclaration(anonymousMethod.ParameterList.Parameters, signaturePermutation, CreateNewParameterSyntax);
                return anonymousMethod.WithParameterList(anonymousMethod.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.SimpleLambdaExpression))
            {
                var lambda = (SimpleLambdaExpressionSyntax)updatedNode;

                if (signaturePermutation.UpdatedConfiguration.ToListOfParameters().Any())
                {
                    var updatedParameters = PermuteDeclaration(SyntaxFactory.SeparatedList<ParameterSyntax>(new[] { lambda.Parameter }), signaturePermutation, CreateNewParameterSyntax);
                    return SyntaxFactory.ParenthesizedLambdaExpression(
                        lambda.AsyncKeyword,
                        SyntaxFactory.ParameterList(updatedParameters),
                        lambda.ArrowToken,
                        lambda.Body);
                }
                else
                {
                    // No parameters. Change to a parenthesized lambda expression
                    var emptyParameterList = SyntaxFactory.ParameterList()
                        .WithLeadingTrivia(lambda.Parameter.GetLeadingTrivia())
                        .WithTrailingTrivia(lambda.Parameter.GetTrailingTrivia());

                    return SyntaxFactory.ParenthesizedLambdaExpression(lambda.AsyncKeyword, emptyParameterList, lambda.ArrowToken, lambda.Body);
                }
            }

            if (updatedNode.IsKind(SyntaxKind.ParenthesizedLambdaExpression))
            {
                var lambda = (ParenthesizedLambdaExpressionSyntax)updatedNode;
                bool doNotSkipType = lambda.ParameterList.Parameters.Any() && lambda.ParameterList.Parameters.First().Type != null;
                Func<AddedParameter, ParameterSyntax> createNewParameterDelegate =
                    p => CreateNewParameterSyntax(p, !doNotSkipType);

                var updatedParameters = PermuteDeclaration(
                    lambda.ParameterList.Parameters,
                    signaturePermutation,
                    createNewParameterDelegate);
                return lambda.WithParameterList(lambda.ParameterList.WithParameters(updatedParameters));
            }

            // Update reference site argument lists
            if (updatedNode.IsKind(SyntaxKind.InvocationExpression))
            {
                var invocation = (InvocationExpressionSyntax)updatedNode;
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                var symbolInfo = semanticModel.GetSymbolInfo((InvocationExpressionSyntax)originalNode, cancellationToken);
                var isReducedExtensionMethod = false;

                if (symbolInfo.Symbol is IMethodSymbol methodSymbol && methodSymbol.MethodKind == MethodKind.ReducedExtension)
                {
                    isReducedExtensionMethod = true;
                }

                SignatureChange signaturePermutationWithoutAddedParameters = signaturePermutation.WithoutAddedParameters();

                var newArguments = PermuteArgumentList(declarationSymbol, invocation.ArgumentList.Arguments, signaturePermutationWithoutAddedParameters, isReducedExtensionMethod);
                newArguments = AddNewArgumentsToList(newArguments, signaturePermutation, isReducedExtensionMethod);
                return invocation.WithArgumentList(invocation.ArgumentList.WithArguments(newArguments).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.ObjectCreationExpression))
            {
                var objCreation = (ObjectCreationExpressionSyntax)updatedNode;
                var newArguments = PermuteArgumentList(declarationSymbol, objCreation.ArgumentList.Arguments, signaturePermutation);
                return objCreation.WithArgumentList(objCreation.ArgumentList.WithArguments(newArguments).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.ThisConstructorInitializer) ||
                updatedNode.IsKind(SyntaxKind.BaseConstructorInitializer))
            {
                var objCreation = (ConstructorInitializerSyntax)updatedNode;
                var newArguments = PermuteArgumentList(declarationSymbol, objCreation.ArgumentList.Arguments, signaturePermutation);
                return objCreation.WithArgumentList(objCreation.ArgumentList.WithArguments(newArguments).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.ElementAccessExpression))
            {
                var elementAccess = (ElementAccessExpressionSyntax)updatedNode;
                var newArguments = PermuteArgumentList(declarationSymbol, elementAccess.ArgumentList.Arguments, signaturePermutation);
                return elementAccess.WithArgumentList(elementAccess.ArgumentList.WithArguments(newArguments).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.Attribute))
            {
                var attribute = (AttributeSyntax)updatedNode;
                var newArguments = PermuteAttributeArgumentList(declarationSymbol, attribute.ArgumentList.Arguments, signaturePermutation);
                return attribute.WithArgumentList(attribute.ArgumentList.WithArguments(newArguments).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            // Handle references in crefs
            if (updatedNode.IsKind(SyntaxKind.NameMemberCref))
            {
                var nameMemberCref = (NameMemberCrefSyntax)updatedNode;

                if (nameMemberCref.Parameters == null ||
                    !nameMemberCref.Parameters.Parameters.Any())
                {
                    return nameMemberCref;
                }

                var newParameters = PermuteDeclaration(nameMemberCref.Parameters.Parameters, signaturePermutation, CreateNewCrefParameterSyntax);

                var newCrefParameterList = nameMemberCref.Parameters.WithParameters(newParameters);
                return nameMemberCref.WithParameters(newCrefParameterList);
            }

            Debug.Assert(false, "Unknown reference location");
            return null;
        }

        private SeparatedSyntaxList<ArgumentSyntax> AddNewArgumentsToList(
            SeparatedSyntaxList<ArgumentSyntax> newArguments,
            SignatureChange signaturePermutation,
            bool isReducedExtensionMethod)
        {
            List<ArgumentSyntax> fullList = new List<ArgumentSyntax>();
            List<SyntaxToken> separators = new List<SyntaxToken>();

            var updatedParameters = signaturePermutation.UpdatedConfiguration.ToListOfParameters();

            int indexInExistingList = 0;

            bool seenNameEquals = false;

            for (int i = 0; i < updatedParameters.Count; i++)
            {
                // Skip this parameter in list of arguments for extension method calls but not for reduced ones.
                if (updatedParameters[i] != signaturePermutation.UpdatedConfiguration.ThisParameter
                    || !isReducedExtensionMethod)
                {
                    if (updatedParameters[i] is AddedParameter addedParameter)
                    {
                        if (addedParameter.CallSiteValue != null)
                        {
                            fullList.Add(SyntaxFactory.Argument(
                                seenNameEquals ? SyntaxFactory.NameColon(addedParameter.Name) : default,
                                refKindKeyword: default,
                                expression: SyntaxFactory.ParseExpression(addedParameter.CallSiteValue)));
                            separators.Add(SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.ElasticSpace));
                        }
                    }
                    else
                    {
                        if (indexInExistingList < newArguments.Count)
                        {
                            if (newArguments[indexInExistingList].NameColon != default)
                            {
                                seenNameEquals = true;
                            }

                            if (indexInExistingList < newArguments.SeparatorCount)
                            {
                                separators.Add(newArguments.GetSeparator(indexInExistingList));
                            }

                            fullList.Add(newArguments[indexInExistingList++]);
                        }
                    }
                }
            }

            // Add the rest of existing parameters, e.g. from the params argument.
            while (indexInExistingList < newArguments.Count)
            {
                if (indexInExistingList < newArguments.SeparatorCount)
                {
                    separators.Add(newArguments.GetSeparator(indexInExistingList));
                }

                fullList.Add(newArguments[indexInExistingList++]);
            }

            if (fullList.Count == separators.Count && separators.Count != 0)
            {
                separators.Remove(separators.Last());
            }

            return SyntaxFactory.SeparatedList(fullList, separators);
        }

        private static ParameterSyntax CreateNewParameterSyntax(AddedParameter addedParameter)
            => CreateNewParameterSyntax(addedParameter, skipType: false);

        private static ParameterSyntax CreateNewParameterSyntax(AddedParameter addedParameter, bool skipType)
            => SyntaxFactory.Parameter(
                attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                modifiers: SyntaxFactory.TokenList(),
                type: skipType ? default : SyntaxFactory.ParseTypeName(addedParameter.TypeName).WithTrailingTrivia(SyntaxFactory.ElasticSpace),
                SyntaxFactory.Identifier(addedParameter.ParameterName),
                @default: default);

        private static CrefParameterSyntax CreateNewCrefParameterSyntax(AddedParameter addedParameter)
            => SyntaxFactory.CrefParameter(type: SyntaxFactory.ParseTypeName(addedParameter.TypeName)).WithLeadingTrivia(SyntaxFactory.ElasticSpace);

        private SeparatedSyntaxList<T> PermuteDeclaration<T>(
            SeparatedSyntaxList<T> list,
            SignatureChange updatedSignature,
            Func<AddedParameter, T> createNewParameterMethod) where T : SyntaxNode
        {
            var permuteDeclarationBase = base.PermuteDeclarationBase<T>(list, updatedSignature, createNewParameterMethod);
            return SyntaxFactory.SeparatedList(permuteDeclarationBase.parameters, permuteDeclarationBase.separators);
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

        private SeparatedSyntaxList<AttributeArgumentSyntax> PermuteAttributeArgumentList(
            ISymbol declarationSymbol,
            SeparatedSyntaxList<AttributeArgumentSyntax> arguments,
            SignatureChange updatedSignature)
        {
            var newArguments = PermuteArguments(declarationSymbol, arguments.Select(a => UnifiedArgumentSyntax.Create(a)).ToList(),
                updatedSignature,
                callSiteValue => UnifiedArgumentSyntax.Create(SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression(callSiteValue))));
            var numSeparatorsToSkip = arguments.Count - newArguments.Count;

            // copy whitespace trivia from original position
            var newArgumentsWithTrivia = TransferLeadingWhitespaceTrivia(
                newArguments.Select(a => (AttributeArgumentSyntax)(UnifiedArgumentSyntax)a), arguments);

            return SyntaxFactory.SeparatedList(newArgumentsWithTrivia, GetSeparators(arguments, numSeparatorsToSkip));
        }

        private SeparatedSyntaxList<ArgumentSyntax> PermuteArgumentList(
            ISymbol declarationSymbol,
            SeparatedSyntaxList<ArgumentSyntax> arguments,
            SignatureChange updatedSignature,
            bool isReducedExtensionMethod = false)
        {
            var newArguments = PermuteArguments(declarationSymbol, arguments.Select(a => UnifiedArgumentSyntax.Create(a)).ToList(),
                updatedSignature,
                 callSiteValue => UnifiedArgumentSyntax.Create(SyntaxFactory.Argument(SyntaxFactory.ParseExpression(callSiteValue))),
                 isReducedExtensionMethod);

            // copy whitespace trivia from original position
            var newArgumentsWithTrivia = TransferLeadingWhitespaceTrivia(
                newArguments.Select(a => (ArgumentSyntax)(UnifiedArgumentSyntax)a), arguments);

            var numSeparatorsToSkip = arguments.Count - newArguments.Count;
            return SyntaxFactory.SeparatedList(newArgumentsWithTrivia, GetSeparators(arguments, numSeparatorsToSkip));
        }

        private List<T> TransferLeadingWhitespaceTrivia<T, U>(IEnumerable<T> newArguments, SeparatedSyntaxList<U> oldArguments)
            where T : SyntaxNode
            where U : SyntaxNode
        {
            var result = new List<T>();
            var index = 0;
            foreach (var newArgument in newArguments)
            {
                if (index < oldArguments.Count)
                {
                    result.Add(TransferLeadingWhitespaceTrivia(newArgument, oldArguments[index]));
                }
                else
                {
                    result.Add(newArgument);
                }

                index++;
            }

            return result;
        }

        private List<SyntaxTrivia> UpdateParamTagsInLeadingTrivia(Document document, CSharpSyntaxNode node, ISymbol declarationSymbol, SignatureChange updatedSignature)
        {
            if (!node.HasLeadingTrivia)
            {
                return null;
            }

            var paramNodes = node
                .DescendantNodes(descendIntoTrivia: true)
                .OfType<XmlElementSyntax>()
                .Where(e => e.StartTag.Name.ToString() == DocumentationCommentXmlNames.ParameterElementName);

            var permutedParamNodes = VerifyAndPermuteParamNodes(paramNodes, declarationSymbol, updatedSignature);
            if (permutedParamNodes == null)
            {
                return null;
            }

            return GetPermutedTrivia(document, node, permutedParamNodes);
        }

        private List<XmlElementSyntax> VerifyAndPermuteParamNodes(IEnumerable<XmlElementSyntax> paramNodes, ISymbol declarationSymbol, SignatureChange updatedSignature)
        {
            // Only reorder if count and order match originally.
            var originalParameters = updatedSignature.OriginalConfiguration.ToListOfParameters();
            var reorderedParameters = updatedSignature.UpdatedConfiguration.ToListOfParameters();

            var declaredParameters = declarationSymbol.GetParameters();
            if (paramNodes.Count() != declaredParameters.Length)
            {
                return null;
            }

            if (declaredParameters.Length == 0)
            {
                return null;
            }

            var dictionary = new Dictionary<string, XmlElementSyntax>();
            var i = 0;
            foreach (var paramNode in paramNodes)
            {
                var nameAttribute = paramNode.StartTag.Attributes.FirstOrDefault(a => a.Name.ToString().Equals("name", StringComparison.OrdinalIgnoreCase));
                if (nameAttribute == null)
                {
                    return null;
                }

                var identifier = nameAttribute.DescendantNodes(descendIntoTrivia: true).OfType<IdentifierNameSyntax>().FirstOrDefault();
                if (identifier == null || identifier.ToString() != declaredParameters.ElementAt(i).Name)
                {
                    return null;
                }

                dictionary.Add(originalParameters[i].Name.ToString(), paramNode);
                i++;
            }

            // Everything lines up, so permute them.
            var permutedParams = new List<XmlElementSyntax>();
            foreach (var parameter in reorderedParameters)
            {
                if (dictionary.TryGetValue(parameter.Name, out var permutedParam))
                {
                    permutedParams.Add(permutedParam);
                }
                else
                {
                    permutedParams.Add(SyntaxFactory.XmlElement(
                        SyntaxFactory.XmlElementStartTag(
                            SyntaxFactory.XmlName("param"),
                            SyntaxFactory.List<XmlAttributeSyntax>(new[] { SyntaxFactory.XmlNameAttribute(parameter.Name) })),
                        SyntaxFactory.XmlElementEndTag(SyntaxFactory.XmlName("param"))));
                }
            }

            return permutedParams;
        }

        private List<SyntaxTrivia> GetPermutedTrivia(Document document, CSharpSyntaxNode node, List<XmlElementSyntax> permutedParamNodes)
        {
            var updatedLeadingTrivia = new List<SyntaxTrivia>();
            var index = 0;
            SyntaxTrivia lastWhiteSpaceTrivia = default;

            var lastDocumentationCommentTriviaSyntax = node.GetLeadingTrivia()
                .LastOrDefault(t => t.HasStructure && t.GetStructure() is DocumentationCommentTriviaSyntax);
            DocumentationCommentTriviaSyntax documentationCommeStructuredTrivia = lastDocumentationCommentTriviaSyntax.GetStructure() as DocumentationCommentTriviaSyntax;

            foreach (var trivia in node.GetLeadingTrivia())
            {
                if (!trivia.HasStructure)
                {
                    if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                    {
                        lastWhiteSpaceTrivia = trivia;
                    }

                    updatedLeadingTrivia.Add(trivia);
                    continue;
                }

                if (!(trivia.GetStructure() is DocumentationCommentTriviaSyntax structuredTrivia))
                {
                    updatedLeadingTrivia.Add(trivia);
                    continue;
                }

                var updatedNodeList = new List<XmlNodeSyntax>();
                var structuredContent = structuredTrivia.Content.ToList();
                for (var i = 0; i < structuredContent.Count; i++)
                {
                    var content = structuredContent[i];
                    if (!content.IsKind(SyntaxKind.XmlElement))
                    {
                        updatedNodeList.Add(content);
                        continue;
                    }

                    var xmlElement = content as XmlElementSyntax;
                    if (xmlElement.StartTag.Name.ToString() != DocumentationCommentXmlNames.ParameterElementName)
                    {
                        updatedNodeList.Add(content);
                        continue;
                    }

                    // Found a param tag, so insert the next one from the reordered list
                    if (index < permutedParamNodes.Count)
                    {
                        updatedNodeList.Add(permutedParamNodes[index].WithLeadingTrivia(content.GetLeadingTrivia()).WithTrailingTrivia(content.GetTrailingTrivia()));
                        index++;
                    }
                    else
                    {
                        // Inspecting a param element that we are deleting but not replacing.
                    }
                }

                var newDocComments = SyntaxFactory.DocumentationCommentTrivia(
                    structuredTrivia.Kind(),
                    SyntaxFactory.List(updatedNodeList.AsEnumerable()),
                    structuredTrivia.EndOfComment);
                newDocComments = newDocComments.WithLeadingTrivia(structuredTrivia.GetLeadingTrivia()).WithTrailingTrivia(structuredTrivia.GetTrailingTrivia());
                var newTrivia = SyntaxFactory.Trivia(newDocComments);

                updatedLeadingTrivia.Add(newTrivia);
            }

            var extraNodeList = new List<XmlNodeSyntax>();
            while (index < permutedParamNodes.Count)
            {
                extraNodeList.Add(permutedParamNodes[index]);
                index++;
            }

            if (extraNodeList.Any())
            {
                var extraDocComments = SyntaxFactory.DocumentationCommentTrivia(
                    SyntaxKind.MultiLineDocumentationCommentTrivia,
                    SyntaxFactory.List(extraNodeList.AsEnumerable()),
                    SyntaxFactory.Token(SyntaxKind.EndOfDocumentationCommentToken));
                extraDocComments = extraDocComments
                    .WithLeadingTrivia(SyntaxFactory.DocumentationCommentExterior("/// "))
                    .WithTrailingTrivia(node.GetTrailingTrivia())
                    .WithTrailingTrivia(
                    SyntaxFactory.EndOfLine(document.Project.Solution.Workspace.Options.GetOption(FormattingOptions.NewLine, LanguageNames.CSharp)),
                    lastWhiteSpaceTrivia);

                var newTrivia = SyntaxFactory.Trivia(extraDocComments);

                updatedLeadingTrivia.Add(newTrivia);
            }

            return updatedLeadingTrivia;
        }

        public override async Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsFromDelegateInvoke(
            SymbolAndProjectId<IMethodSymbol> symbolAndProjectId,
            Document document,
            CancellationToken cancellationToken)
        {
            var symbol = symbolAndProjectId.Symbol;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var nodes = root.DescendantNodes().ToImmutableArray();
            var convertedMethodGroups = nodes
                .WhereAsArray(
                    n =>
                    {
                        if (!n.IsKind(SyntaxKind.IdentifierName) ||
                            !semanticModel.GetMemberGroup(n, cancellationToken).Any())
                        {
                            return false;
                        }

                        ISymbol convertedType = semanticModel.GetTypeInfo(n, cancellationToken).ConvertedType;

                        if (convertedType != null)
                        {
                            convertedType = convertedType.OriginalDefinition;
                        }

                        if (convertedType != null)
                        {
                            convertedType = SymbolFinder.FindSourceDefinitionAsync(convertedType, document.Project.Solution, cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken) ?? convertedType;
                        }

                        return Equals(convertedType, symbol.ContainingType);
                    })
                .SelectAsArray(n => semanticModel.GetSymbolInfo(n, cancellationToken).Symbol);

            return convertedMethodGroups.SelectAsArray(symbolAndProjectId.WithSymbol);
        }

        protected override IEnumerable<AbstractFormattingRule> GetFormattingRules(Document document)
            => SpecializedCollections.SingletonEnumerable<AbstractFormattingRule>(new ChangeSignatureFormattingRule()).Concat(Formatter.GetDefaultFormattingRules(document));

        protected override SyntaxToken CreateSeparatorSyntaxToken()
            => SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.ElasticSpace);
    }
}
