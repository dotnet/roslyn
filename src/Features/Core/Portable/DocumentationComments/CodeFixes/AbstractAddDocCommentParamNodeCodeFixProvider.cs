// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DiagnosticComments.CodeFixes
{
    internal abstract class AbstractAddDocCommentParamNodeCodeFixProvider
        <TXmlElementSyntax, TXmlNameAttributeSyntax, TXmlTextSyntax, TMemberDeclarationSyntax, TParameterSyntax> : CodeFixProvider
        where TXmlElementSyntax : SyntaxNode
        where TXmlNameAttributeSyntax : SyntaxNode
        where TXmlTextSyntax : SyntaxNode
        where TMemberDeclarationSyntax : SyntaxNode
        where TParameterSyntax : SyntaxNode
    {
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public async sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync().ConfigureAwait(false);
            var parameter = root.FindNode(context.Span) as TParameterSyntax;

            var parentMethod = parameter?.FirstAncestorOrSelf<TMemberDeclarationSyntax>();
            if (parentMethod != null)
            {
                context.RegisterCodeFix(
                    new MyCodeAction(
                        c => AddParamTagAsync(context.Document, context.Span, c), GetParameterName(parameter)),
                    context.Diagnostics);
            }
        }
        
        protected abstract List<TXmlNameAttributeSyntax> GetNameAttributes(TXmlElementSyntax node);
        protected abstract string GetValueFromNameAttribute(TXmlNameAttributeSyntax attribute);
        protected abstract SyntaxNode GetDocCommentNode(SyntaxTriviaList parameter);
        protected abstract string GetXmlElementLocalName(TXmlElementSyntax element);
        protected abstract List<string> GetParameterNames(TMemberDeclarationSyntax method);
        protected abstract string GetParameterName(TParameterSyntax parameter);
        protected abstract TXmlElementSyntax GetNewNode(string parameterName, bool isFirstNodeInComment);

        protected async Task<Document> AddParamTagAsync(
            Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var parentMethod = root.FindNode(span).FirstAncestorOrSelf<TMemberDeclarationSyntax>();
            var docCommentNode = GetDocCommentNode(parentMethod.GetLeadingTrivia());

            var newDocComment = docCommentNode;
            var methodParamNames = GetParameterNames(parentMethod);

            foreach (var name in methodParamNames)
            {
                var paramNodes = GetParamNodes(newDocComment);

                if (NodeExists(paramNodes, name)) { continue; }

                var paramsBeforeCurrentParam = methodParamNames.TakeWhile(t => t != name).ToList();
                var paramsAfterCurrentParam = methodParamNames.Except(paramsBeforeCurrentParam).ToList();
                paramsAfterCurrentParam.Remove(name);

                SyntaxNode nodeBeforeNewParamNode = null;
                SyntaxNode nodeAfterNewParamNode = null;
                if (paramsBeforeCurrentParam.Any())
                {
                    nodeBeforeNewParamNode = GetLastParamNodeCorrespondingToParamInList(paramNodes, paramsBeforeCurrentParam);
                }
                if (paramsAfterCurrentParam.Any())
                {
                    nodeAfterNewParamNode = GetLastParamNodeCorrespondingToParamInList(paramNodes.Reverse(), paramsAfterCurrentParam);

                    // Adjust for doc comment marker before `param` node
                    if (nodeAfterNewParamNode != null)
                    {
                        var paramNodeSiblings = nodeAfterNewParamNode.Parent.ChildNodes().ToList();
                        var indexOfNode = paramNodeSiblings.IndexOf(nodeAfterNewParamNode);

                        // set insert node to be the doc comment signifier of the closest param before the new node
                        if (indexOfNode > 0 && paramNodeSiblings[indexOfNode - 1] is TXmlTextSyntax)
                        {
                            nodeAfterNewParamNode = paramNodeSiblings[indexOfNode - 1];
                        }
                    }
                }
                
                var newNodeList = new SyntaxNode[]
                {
                    GetNewNode(name, isFirstNodeInComment: newDocComment.ChildNodes().First() == nodeAfterNewParamNode)
                };

                newDocComment = nodeBeforeNewParamNode != null
                    ? newDocComment.InsertNodesAfter(nodeBeforeNewParamNode, newNodeList)
                    : newDocComment.InsertNodesBefore(nodeAfterNewParamNode, newNodeList);
            }

            var newRoot = root.ReplaceNode(docCommentNode, newDocComment.WithAdditionalAnnotations(Formatter.Annotation));
            return document.WithSyntaxRoot(newRoot);
        }

        private IEnumerable<TXmlElementSyntax> GetParamNodes(SyntaxNode docComment)
        {
            var paramNodes = docComment.ChildNodes().OfType<TXmlElementSyntax>()
                                                    .Where(w => GetXmlElementLocalName(w) == "param");

            // Prefer to place the doc comment in the outer level, as in auto-created comments
            if (!paramNodes.Any())
            {
                paramNodes = docComment.DescendantNodes(descendIntoChildren: _ => true)
                    .OfType<TXmlElementSyntax>()
                    .Where(w => GetXmlElementLocalName(w) == "param");
            }

            return paramNodes;
        }

        private bool NodeExists(IEnumerable<TXmlElementSyntax> paramNodes, string name)
        {
            return paramNodes.Select(GetNameAttributes)
                             .Where(nameAttributes => nameAttributes.Count == 1)
                             .Any(nameAttributes => nameAttributes.Select(GetValueFromNameAttribute).Contains(name));
        }

        protected TXmlElementSyntax GetLastParamNodeCorrespondingToParamInList(IEnumerable<TXmlElementSyntax> paramNodeList, List<string> methodParamSubset)
        {
            TXmlElementSyntax nodeAfterNewParamNode = null;

            foreach (var paramNode in paramNodeList)
            {
                var paramNameForNode = GetNameAttributes(paramNode);

                // param node is missing `name` attribute or there are multiple `name` attributes
                if (paramNameForNode.Count != 1)
                {
                    continue;
                }
                
                if (methodParamSubset.Contains(GetValueFromNameAttribute(paramNameForNode.Single())))
                {
                    nodeAfterNewParamNode = paramNode;
                }
            }

            return nodeAfterNewParamNode;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string parameterName)
                : base(string.Format(FeaturesResources.Add_missing_param_nodes, parameterName), createChangedDocument)
            {
            }
        }
    }
}