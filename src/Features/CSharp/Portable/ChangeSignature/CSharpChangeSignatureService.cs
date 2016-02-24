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
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ChangeSignature
{
    [ExportLanguageService(typeof(AbstractChangeSignatureService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpChangeSignatureService : AbstractChangeSignatureService
    {
        public override ISymbol GetInvocationSymbol(Document document, int position, bool restrictToDeclarations, CancellationToken cancellationToken)
        {
            var tree = document.GetSyntaxTreeAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var token = tree.GetRoot(cancellationToken).FindToken(position != tree.Length ? position : Math.Max(0, position - 1));

            var ancestorDeclarationKinds = restrictToDeclarations ? _invokableAncestorKinds.Add(SyntaxKind.Block) : _invokableAncestorKinds;
            SyntaxNode matchingNode = token.Parent.AncestorsAndSelf().FirstOrDefault(n => ancestorDeclarationKinds.Contains(n.Kind()));
            if (matchingNode == null || matchingNode.IsKind(SyntaxKind.Block))
            {
                return null;
            }

            ISymbol symbol;
            var semanticModel = document.GetSemanticModelAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            symbol = semanticModel.GetDeclaredSymbol(matchingNode, cancellationToken);
            if (symbol != null)
            {
                return symbol;
            }

            if (matchingNode.IsKind(SyntaxKind.ObjectCreationExpression))
            {
                var objectCreation = matchingNode as ObjectCreationExpressionSyntax;

                if (token.Parent.AncestorsAndSelf().Any(a => a == objectCreation.Type))
                {
                    var typeSymbol = semanticModel.GetSymbolInfo(objectCreation.Type, cancellationToken).Symbol;
                    if (typeSymbol != null && typeSymbol.IsKind(SymbolKind.NamedType) && (typeSymbol as ITypeSymbol).TypeKind == TypeKind.Delegate)
                    {
                        return typeSymbol;
                    }
                }
            }

            var symbolInfo = semanticModel.GetSymbolInfo(matchingNode, cancellationToken);
            return symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
        }

        private ImmutableArray<SyntaxKind> _invokableAncestorKinds = new[]
            {
                SyntaxKind.MethodDeclaration,
                SyntaxKind.ConstructorDeclaration,
                SyntaxKind.IndexerDeclaration,
                SyntaxKind.InvocationExpression,
                SyntaxKind.ElementAccessExpression,
                SyntaxKind.ThisConstructorInitializer,
                SyntaxKind.BaseConstructorInitializer,
                SyntaxKind.ObjectCreationExpression,
                SyntaxKind.Attribute,
                SyntaxKind.NameMemberCref,
                SyntaxKind.SimpleLambdaExpression,
                SyntaxKind.ParenthesizedLambdaExpression,
                SyntaxKind.DelegateDeclaration
            }.ToImmutableArray();

        private ImmutableArray<SyntaxKind> _updatableAncestorKinds = new[]
            {
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
                SyntaxKind.NameMemberCref
            }.ToImmutableArray();

        private ImmutableArray<SyntaxKind> _updatableNodeKinds = new[]
            {
                SyntaxKind.MethodDeclaration,
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
                SyntaxKind.SimpleLambdaExpression
            }.ToImmutableArray();

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
                var method = updatedNode as MethodDeclarationSyntax;
                var updatedParameters = PermuteDeclaration(method.ParameterList.Parameters, signaturePermutation);
                return method.WithParameterList(method.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.ConstructorDeclaration))
            {
                var constructor = updatedNode as ConstructorDeclarationSyntax;
                var updatedParameters = PermuteDeclaration(constructor.ParameterList.Parameters, signaturePermutation);
                return constructor.WithParameterList(constructor.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.IndexerDeclaration))
            {
                var indexer = updatedNode as IndexerDeclarationSyntax;
                var updatedParameters = PermuteDeclaration(indexer.ParameterList.Parameters, signaturePermutation);
                return indexer.WithParameterList(indexer.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.DelegateDeclaration))
            {
                var delegateDeclaration = updatedNode as DelegateDeclarationSyntax;
                var updatedParameters = PermuteDeclaration(delegateDeclaration.ParameterList.Parameters, signaturePermutation);
                return delegateDeclaration.WithParameterList(delegateDeclaration.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.AnonymousMethodExpression))
            {
                var anonymousMethod = updatedNode as AnonymousMethodExpressionSyntax;

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
                var lambda = updatedNode as SimpleLambdaExpressionSyntax;

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

                    return SyntaxFactory.ParenthesizedLambdaExpression(lambda.AsyncKeyword, emptyParameterList, lambda.ArrowToken, lambda.RefKeyword, lambda.Body);
                }
            }

            if (updatedNode.IsKind(SyntaxKind.ParenthesizedLambdaExpression))
            {
                var lambda = updatedNode as ParenthesizedLambdaExpressionSyntax;
                var updatedParameters = PermuteDeclaration(lambda.ParameterList.Parameters, signaturePermutation);
                return lambda.WithParameterList(lambda.ParameterList.WithParameters(updatedParameters));
            }

            // Update reference site argument lists

            if (updatedNode.IsKind(SyntaxKind.InvocationExpression))
            {
                var invocation = updatedNode as InvocationExpressionSyntax;
                var semanticModel = document.GetSemanticModelAsync(cancellationToken).WaitAndGetResult(cancellationToken);

                var symbolInfo = semanticModel.GetSymbolInfo(originalNode as InvocationExpressionSyntax, cancellationToken);
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
                var objCreation = updatedNode as ObjectCreationExpressionSyntax;
                var newArguments = PermuteArgumentList(document, declarationSymbol, objCreation.ArgumentList.Arguments, signaturePermutation);
                return objCreation.WithArgumentList(objCreation.ArgumentList.WithArguments(newArguments).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.ThisConstructorInitializer) ||
                updatedNode.IsKind(SyntaxKind.BaseConstructorInitializer))
            {
                var objCreation = updatedNode as ConstructorInitializerSyntax;
                var newArguments = PermuteArgumentList(document, declarationSymbol, objCreation.ArgumentList.Arguments, signaturePermutation);
                return objCreation.WithArgumentList(objCreation.ArgumentList.WithArguments(newArguments).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.ElementAccessExpression))
            {
                var elementAccess = updatedNode as ElementAccessExpressionSyntax;
                var newArguments = PermuteArgumentList(document, declarationSymbol, elementAccess.ArgumentList.Arguments, signaturePermutation);
                return elementAccess.WithArgumentList(elementAccess.ArgumentList.WithArguments(newArguments).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            if (updatedNode.IsKind(SyntaxKind.Attribute))
            {
                var attribute = updatedNode as AttributeSyntax;
                var newArguments = PermuteAttributeArgumentList(document, declarationSymbol, attribute.ArgumentList.Arguments, signaturePermutation);
                return attribute.WithArgumentList(attribute.ArgumentList.WithArguments(newArguments).WithAdditionalAnnotations(changeSignatureFormattingAnnotation));
            }

            // Handle references in crefs

            if (updatedNode.IsKind(SyntaxKind.NameMemberCref))
            {
                var nameMemberCref = updatedNode as NameMemberCrefSyntax;

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
            foreach (var newParam in reorderedParameters)
            {
                var pos = originalParameters.IndexOf(newParam);
                var param = list[pos];
                newParameters.Add(param);
            }

            var numSeparatorsToSkip = originalParameters.Count - reorderedParameters.Count;
            return SyntaxFactory.SeparatedList(newParameters, GetSeparators(list, numSeparatorsToSkip));
        }

        private static SeparatedSyntaxList<AttributeArgumentSyntax> PermuteAttributeArgumentList(
            Document document,
            ISymbol declarationSymbol,
            SeparatedSyntaxList<AttributeArgumentSyntax> arguments,
            SignatureChange updatedSignature)
        {
            var newArguments = PermuteArguments(document, declarationSymbol, arguments.Select(a => UnifiedArgumentSyntax.Create(a)).ToList(), updatedSignature);
            var numSeparatorsToSkip = arguments.Count - newArguments.Count;
            return SyntaxFactory.SeparatedList(newArguments.Select(a => (AttributeArgumentSyntax)(UnifiedArgumentSyntax)a), GetSeparators(arguments, numSeparatorsToSkip));
        }

        private static SeparatedSyntaxList<ArgumentSyntax> PermuteArgumentList(
            Document document,
            ISymbol declarationSymbol,
            SeparatedSyntaxList<ArgumentSyntax> arguments,
            SignatureChange updatedSignature,
            bool isReducedExtensionMethod = false)
        {
            var newArguments = PermuteArguments(document, declarationSymbol, arguments.Select(a => UnifiedArgumentSyntax.Create(a)).ToList(), updatedSignature, isReducedExtensionMethod);
            var numSeparatorsToSkip = arguments.Count - newArguments.Count;
            return SyntaxFactory.SeparatedList(newArguments.Select(a => (ArgumentSyntax)(UnifiedArgumentSyntax)a), GetSeparators(arguments, numSeparatorsToSkip));
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
            int i = 0;
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
                for (int i = 0; i < structuredContent.Count; i++)
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
            for (int i = 0; i < arguments.SeparatorCount - numSeparatorsToSkip; i++)
            {
                separators.Add(arguments.GetSeparator(i));
            }

            return separators;
        }

        public override async Task<IEnumerable<ISymbol>> DetermineCascadedSymbolsFromDelegateInvoke(IMethodSymbol symbol, Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var nodes = root.DescendantNodes();
            var convertedMethodGroups = nodes
                .Where(
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
                                convertedType = SymbolFinder.FindSourceDefinitionAsync(convertedType, document.Project.Solution, cancellationToken).WaitAndGetResult(cancellationToken) ?? convertedType;
                            }

                            return convertedType == symbol.ContainingType;
                        })
                .Select(n => semanticModel.GetSymbolInfo(n, cancellationToken).Symbol);

            return convertedMethodGroups;
        }

        protected override IEnumerable<IFormattingRule> GetFormattingRules(Document document)
        {
            return new[] { new ChangeSignatureFormattingRule() }.Concat(Formatter.GetDefaultFormattingRules(document));
        }
    }
}
