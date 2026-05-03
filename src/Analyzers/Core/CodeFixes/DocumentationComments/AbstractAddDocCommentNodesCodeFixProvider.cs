// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DocumentationComments;

internal abstract class AbstractAddDocCommentNodesCodeFixProvider
    <TXmlElementSyntax, TXmlNameAttributeSyntax, TXmlTextSyntax, TMemberDeclarationSyntax> : CodeFixProvider
    where TXmlElementSyntax : SyntaxNode
    where TXmlNameAttributeSyntax : SyntaxNode
    where TXmlTextSyntax : SyntaxNode
    where TMemberDeclarationSyntax : SyntaxNode
{
    public override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var parentMethod = root.FindNode(context.Span).FirstAncestorOrSelf<TMemberDeclarationSyntax>();
        if (parentMethod is null)
            return;

        var docCommentNode = TryGetDocCommentNode(parentMethod.GetLeadingTrivia());
        if (docCommentNode is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                CodeFixesResources.Add_missing_param_nodes,
                cancellationToken => AddParamTagAsync(context.Document, parentMethod, docCommentNode, cancellationToken),
                nameof(CodeFixesResources.Add_missing_param_nodes)),
            context.Diagnostics);
    }

    protected abstract string NodeName { get; }

    protected abstract List<TXmlNameAttributeSyntax> GetNameAttributes(TXmlElementSyntax node);
    protected abstract string GetValueFromNameAttribute(TXmlNameAttributeSyntax attribute);
    protected abstract SyntaxNode? TryGetDocCommentNode(SyntaxTriviaList parameter);
    protected abstract string GetXmlElementLocalName(TXmlElementSyntax element);
    protected abstract ImmutableArray<string> GetParameterNames(TMemberDeclarationSyntax method);
    protected abstract TXmlElementSyntax GetNewNode(string parameterName, bool isFirstNodeInComment);

    protected async Task<Document> AddParamTagAsync(
        Document document, TMemberDeclarationSyntax parentMethod, SyntaxNode docCommentNode, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var newDocComment = docCommentNode;
        var parameterNames = GetParameterNames(parentMethod);

        for (var index = 0; index < parameterNames.Length; index++)
        {
            var parameterName = parameterNames[index];
            var paramNodes = GetElementNodes(newDocComment, NodeName);
            if (NodeExists(paramNodes, parameterName))
            {
                continue;
            }

            var paramsBeforeCurrentParam = parameterNames.TakeWhile(t => t != parameterName).ToList();
            var paramsAfterCurrentParam = parameterNames.Except(paramsBeforeCurrentParam).ToList();
            paramsAfterCurrentParam.Remove(parameterName);

            // If the index is not `0`, there is a node before the current one for sure
            // If the index is `0`, try to add the node after the `summary` node,
            // only if any existing nodes are at the top level--this route will not
            // be taken if the existing node is nested in another node
            var summaryNode = GetElementNodes(newDocComment, "summary").FirstOrDefault();
            if (index != 0 || (!paramNodes.Any() && summaryNode != null))
            {
                // First, try to get the node before the param node so we know where to insert the new node
                TXmlElementSyntax? nodeBeforeNewParamNode = null;
                if (index > 0)
                {
                    nodeBeforeNewParamNode = GetParamNodeForParamName(paramNodes, parameterNames[index - 1]);
                }

                // This will be hit in the index is `0`, in which case the previous node is the summary node
                nodeBeforeNewParamNode ??= summaryNode;

                newDocComment = newDocComment.InsertNodesAfter(nodeBeforeNewParamNode!,
                    [GetNewNode(parameterName, isFirstNodeInComment: false)]);

                continue;
            }

            // At this point, the node has to go at the beginning of the comment
            var nodeAfterNewParamNode = paramNodes.FirstOrDefault() ?? newDocComment.ChildNodes().First();

            // Adjust for doc comment marker before the node
            var paramNodeSiblings = nodeAfterNewParamNode.GetRequiredParent().ChildNodes().ToList();
            var indexOfNode = paramNodeSiblings.IndexOf(nodeAfterNewParamNode);

            // set insert node to be the doc comment signifier of the closest param before the new node
            if (indexOfNode > 0 && paramNodeSiblings[indexOfNode - 1] is TXmlTextSyntax previousSibling)
                nodeAfterNewParamNode = previousSibling;

            var newNodeList = new[]
            {
                // the last value will almost always be true, unless the node is embedded in another doc comment node
                GetNewNode(parameterName, nodeAfterNewParamNode == newDocComment.ChildNodes().First())
            };
            newDocComment = newDocComment.InsertNodesBefore(nodeAfterNewParamNode, newNodeList);
        }

        var newRoot = root.ReplaceNode(docCommentNode, newDocComment.WithAdditionalAnnotations(Formatter.Annotation));
        return document.WithSyntaxRoot(newRoot);
    }

    private List<TXmlElementSyntax> GetElementNodes(SyntaxNode docComment, string nodeName)
    {
        var nodes = docComment.ChildNodes().OfType<TXmlElementSyntax>()
                                           .Where(w => GetXmlElementLocalName(w) == nodeName)
                                           .ToList();

        // Prefer to return element nodes that are the top-level children of the DocComment.
        // If we don't find any, then fallback to the first element node at any depth with the requested name.
        if (!nodes.Any())
        {
            nodes = [.. docComment.DescendantNodes(descendIntoChildren: _ => true)
                              .OfType<TXmlElementSyntax>()
                              .Where(w => GetXmlElementLocalName(w) == nodeName)];
        }

        return nodes;
    }

    private bool NodeExists(IEnumerable<TXmlElementSyntax> paramNodes, string name)
    {
        return paramNodes.Select(GetNameAttributes)
                         .Where(nameAttributes => nameAttributes.Count == 1)
                         .Any(nameAttributes => nameAttributes.Select(GetValueFromNameAttribute).Contains(name));
    }

    protected TXmlElementSyntax? GetParamNodeForParamName(
        IEnumerable<TXmlElementSyntax> paramNodeList,
        string name)
    {
        foreach (var paramNode in paramNodeList)
        {
            var paramNameAttributesForNode = GetNameAttributes(paramNode);

            // param node is missing `name` attribute or there are multiple `name` attributes
            if (paramNameAttributesForNode.Count != 1)
            {
                continue;
            }

            if (GetValueFromNameAttribute(paramNameAttributesForNode.Single()) == name)
            {
                return paramNode;
            }
        }

        return null;
    }
}
