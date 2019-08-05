// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
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

        public override SyntaxNode ChangeSignature(
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
                var updatedLeadingTrivia = UpdateParamTagsInLeadingTrivia(updatedNode, declarationSymbol, signaturePermutation);
                if (updatedLeadingTrivia != null)
                {
                    updatedNode = updatedNode.WithLeadingTrivia(updatedLeadingTrivia);
                }
            }

            // Update declarations parameter lists

            if (updatedNode.IsKind(SyntaxKind.MethodDeclaration))
            {
                var method = (MethodDeclarationSyntax)updatedNode;
                var updatedParameters = PermuteDeclaration(method.ParameterList.Parameters, signaturePermutation);
                return method.WithParameterList(method.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.LocalFunctionStatement))
            {
                var localFunction = (LocalFunctionStatementSyntax)updatedNode;
                var updatedParameters = PermuteDeclaration(localFunction.ParameterList.Parameters, signaturePermutation);
                return localFunction.WithParameterList(localFunction.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.ConstructorDeclaration))
            {
                var constructor = (ConstructorDeclarationSyntax)updatedNode;
                var updatedParameters = PermuteDeclaration(constructor.ParameterList.Parameters, signaturePermutation);
                return constructor.WithParameterList(constructor.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.IndexerDeclaration))
            {
                var indexer = (IndexerDeclarationSyntax)updatedNode;
                var updatedParameters = PermuteDeclaration(indexer.ParameterList.Parameters, signaturePermutation);
                return indexer.WithParameterList(indexer.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.DelegateDeclaration))
            {
                var delegateDeclaration = (DelegateDeclarationSyntax)updatedNode;
                var updatedParameters = PermuteDeclaration(delegateDeclaration.ParameterList.Parameters, signaturePermutation);
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

                var updatedParameters = PermuteDeclaration(anonymousMethod.ParameterList.Parameters, signaturePermutation);
                return anonymousMethod.WithParameterList(anonymousMethod.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.SimpleLambdaExpression))
            {
                var lambda = (SimpleLambdaExpressionSyntax)updatedNode;

                if (signaturePermutation.UpdatedConfiguration.ToListOfParameters().Any())
                {
                    Debug.Assert(false, "Updating a simple lambda expression without removing its parameter");
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
                var updatedParameters = PermuteDeclaration(lambda.ParameterList.Parameters, signaturePermutation);
                return lambda.WithParameterList(lambda.ParameterList.WithParameters(updatedParameters));
            }

            // Update reference site argument lists

            if (updatedNode.IsKind(SyntaxKind.InvocationExpression))
            {
                var invocation = (InvocationExpressionSyntax)updatedNode;
                var semanticModel = document.GetSemanticModelAsync(cancellationToken).WaitAndGetResult(cancellationToken);

                var symbolInfo = semanticModel.GetSymbolInfo((InvocationExpressionSyntax)originalNode, cancellationToken);
                var methodSymbol = symbolInfo.Symbol as IMethodSymbol;
                var isReducedExtensionMethod = false;

                if (methodSymbol != null && methodSymbol.MethodKind == MethodKind.ReducedExtension)
                {
                    isReducedExtensionMethod = true;
                }

                var newArguments = PermuteArgumentList(document, declarationSymbol, invocation.ArgumentList.Arguments, signaturePermutation, isReducedExtensionMethod);
                return invocation.WithArgumentList(invocation.ArgumentList.WithArguments(newArguments).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.ObjectCreationExpression))
            {
                var objCreation = (ObjectCreationExpressionSyntax)updatedNode;
                var newArguments = PermuteArgumentList(document, declarationSymbol, objCreation.ArgumentList.Arguments, signaturePermutation);
                return objCreation.WithArgumentList(objCreation.ArgumentList.WithArguments(newArguments).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.ThisConstructorInitializer) ||
                updatedNode.IsKind(SyntaxKind.BaseConstructorInitializer))
            {
                var objCreation = (ConstructorInitializerSyntax)updatedNode;
                var newArguments = PermuteArgumentList(document, declarationSymbol, objCreation.ArgumentList.Arguments, signaturePermutation);
                return objCreation.WithArgumentList(objCreation.ArgumentList.WithArguments(newArguments).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.ElementAccessExpression))
            {
                var elementAccess = (ElementAccessExpressionSyntax)updatedNode;
                var newArguments = PermuteArgumentList(document, declarationSymbol, elementAccess.ArgumentList.Arguments, signaturePermutation);
                return elementAccess.WithArgumentList(elementAccess.ArgumentList.WithArguments(newArguments).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.Attribute))
            {
                var attribute = (AttributeSyntax)updatedNode;
                var newArguments = PermuteAttributeArgumentList(document, declarationSymbol, attribute.ArgumentList.Arguments, signaturePermutation);
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

                var newParameters = PermuteDeclaration(nameMemberCref.Parameters.Parameters, signaturePermutation);

                var newCrefParameterList = nameMemberCref.Parameters.WithParameters(newParameters);
                return nameMemberCref.WithParameters(newCrefParameterList);
            }

            Debug.Assert(false, "Unknown reference location");
            return null;
        }

        private SeparatedSyntaxList<T> PermuteDeclaration<T>(SeparatedSyntaxList<T> list, SignatureChange updatedSignature) where T : SyntaxNode
        {
            var originalParameters = updatedSignature.OriginalConfiguration.ToListOfParameters();
            var reorderedParameters = updatedSignature.UpdatedConfiguration.ToListOfParameters();

            var newParameters = new List<T>();
            for (var index = 0; index < reorderedParameters.Count; index++)
            {
                var newParam = reorderedParameters[index];
                var pos = originalParameters.IndexOf(newParam);
                var param = list[pos];

                // copy whitespace trivia from original position
                param = TransferLeadingWhitespaceTrivia(param, list[index]);

                newParameters.Add(param);
            }

            var numSeparatorsToSkip = originalParameters.Count - reorderedParameters.Count;
            return SyntaxFactory.SeparatedList(newParameters, GetSeparators(list, numSeparatorsToSkip));
        }

        private static T TransferLeadingWhitespaceTrivia<T>(T newArgument, SyntaxNode oldArgument) where T : SyntaxNode
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

        private static SeparatedSyntaxList<AttributeArgumentSyntax> PermuteAttributeArgumentList(
            Document document,
            ISymbol declarationSymbol,
            SeparatedSyntaxList<AttributeArgumentSyntax> arguments,
            SignatureChange updatedSignature)
        {
            var newArguments = PermuteArguments(document, declarationSymbol, arguments.Select(a => UnifiedArgumentSyntax.Create(a)).ToList(), updatedSignature);
            var numSeparatorsToSkip = arguments.Count - newArguments.Count;

            // copy whitespace trivia from original position
            var newArgumentsWithTrivia = TransferLeadingWhitespaceTrivia(
                newArguments.Select(a => (AttributeArgumentSyntax)(UnifiedArgumentSyntax)a), arguments);

            return SyntaxFactory.SeparatedList(newArgumentsWithTrivia, GetSeparators(arguments, numSeparatorsToSkip));
        }

        private static SeparatedSyntaxList<ArgumentSyntax> PermuteArgumentList(
            Document document,
            ISymbol declarationSymbol,
            SeparatedSyntaxList<ArgumentSyntax> arguments,
            SignatureChange updatedSignature,
            bool isReducedExtensionMethod = false)
        {
            var newArguments = PermuteArguments(document, declarationSymbol, arguments.Select(a => UnifiedArgumentSyntax.Create(a)).ToList(), updatedSignature, isReducedExtensionMethod);

            // copy whitespace trivia from original position
            var newArgumentsWithTrivia = TransferLeadingWhitespaceTrivia(
                newArguments.Select(a => (ArgumentSyntax)(UnifiedArgumentSyntax)a), arguments);

            var numSeparatorsToSkip = arguments.Count - newArguments.Count;
            return SyntaxFactory.SeparatedList(newArgumentsWithTrivia, GetSeparators(arguments, numSeparatorsToSkip));
        }

        private static List<T> TransferLeadingWhitespaceTrivia<T, U>(IEnumerable<T> newArguments, SeparatedSyntaxList<U> oldArguments)
            where T : SyntaxNode
            where U : SyntaxNode
        {
            var result = new List<T>();
            var index = 0;
            foreach (var newArgument in newArguments)
            {
                result.Add(TransferLeadingWhitespaceTrivia(newArgument, oldArguments[index]));
                index++;
            }

            return result;
        }

        private List<SyntaxTrivia> UpdateParamTagsInLeadingTrivia(CSharpSyntaxNode node, ISymbol declarationSymbol, SignatureChange updatedSignature)
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

            return GetPermutedTrivia(node, permutedParamNodes);
        }

        private List<XmlElementSyntax> VerifyAndPermuteParamNodes(IEnumerable<XmlElementSyntax> paramNodes, ISymbol declarationSymbol, SignatureChange updatedSignature)
        {
            // Only reorder if count and order match originally.

            var originalParameters = updatedSignature.OriginalConfiguration.ToListOfParameters();
            var reorderedParameters = updatedSignature.UpdatedConfiguration.ToListOfParameters();

            var declaredParameters = declarationSymbol.GetParameters();
            if (paramNodes.Count() != declaredParameters.Count())
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
                permutedParams.Add(dictionary[parameter.Name]);
            }

            return permutedParams;
        }

        private List<SyntaxTrivia> GetPermutedTrivia(CSharpSyntaxNode node, List<XmlElementSyntax> permutedParamNodes)
        {
            var updatedLeadingTrivia = new List<SyntaxTrivia>();
            var index = 0;

            foreach (var trivia in node.GetLeadingTrivia())
            {
                if (!trivia.HasStructure)
                {
                    updatedLeadingTrivia.Add(trivia);
                    continue;
                }

                var structuredTrivia = trivia.GetStructure() as DocumentationCommentTriviaSyntax;
                if (structuredTrivia == null)
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

                var newDocComments = SyntaxFactory.DocumentationCommentTrivia(structuredTrivia.Kind(), SyntaxFactory.List(updatedNodeList.AsEnumerable()));
                newDocComments = newDocComments.WithEndOfComment(structuredTrivia.EndOfComment);
                newDocComments = newDocComments.WithLeadingTrivia(structuredTrivia.GetLeadingTrivia()).WithTrailingTrivia(structuredTrivia.GetTrailingTrivia());
                var newTrivia = SyntaxFactory.Trivia(newDocComments);

                updatedLeadingTrivia.Add(newTrivia);
            }

            return updatedLeadingTrivia;
        }

        private static List<SyntaxToken> GetSeparators<T>(SeparatedSyntaxList<T> arguments, int numSeparatorsToSkip = 0) where T : SyntaxNode
        {
            var separators = new List<SyntaxToken>();
            for (var i = 0; i < arguments.SeparatorCount - numSeparatorsToSkip; i++)
            {
                separators.Add(arguments.GetSeparator(i));
            }

            return separators;
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
        {
            return new[] { new ChangeSignatureFormattingRule() }.Concat(Formatter.GetDefaultFormattingRules(document));
        }
    }
}
