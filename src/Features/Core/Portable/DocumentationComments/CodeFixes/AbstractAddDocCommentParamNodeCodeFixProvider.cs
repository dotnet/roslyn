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

            var parentMethod = parameter.FirstAncestorOrSelf<TMemberDeclarationSyntax>();
            if (parentMethod != null)
            {
                context.RegisterCodeFix(
                    new MyCodeAction(
                        c => AddParamTagAsync(context.Document, context.Span, c)),
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
            var parameter = root.FindNode(span) as TParameterSyntax;

            var parentMethod = parameter.FirstAncestorOrSelf<TMemberDeclarationSyntax>();
            var docCommentNode = GetDocCommentNode(parentMethod.GetLeadingTrivia());
            var docCommentChildNodes = docCommentNode.ChildNodes().ToList();

            var paramNodes = docCommentChildNodes.OfType<TXmlElementSyntax>()
                                                 .Where(w => GetXmlElementLocalName(w) == "param");

            // Prefer to place the doc comment in the outer level, as in auto-created comments
            if (!paramNodes.Any())
            {
                paramNodes = docCommentNode.DescendantNodes(descendIntoChildren: _ => true)
                                           .OfType<TXmlElementSyntax>()
                                           .Where(w => GetXmlElementLocalName(w) == "param");
            }

            var methodParamNames = GetParameterNames(parentMethod);
            var paramsBeforeDiagnosticParam = methodParamNames.TakeWhile(t => t != GetParameterName(parameter)).ToList();
            var paramsAfterDiagnosticParam = methodParamNames.Except(paramsBeforeDiagnosticParam).ToList();
            paramsAfterDiagnosticParam.Remove(GetParameterName(parameter));

            SyntaxNode nodeBeforeNewParamNode = null;
            SyntaxNode nodeAfterNewParamNode = null;
            if (paramsBeforeDiagnosticParam.Any())
            {
                nodeBeforeNewParamNode = GetLastParamNodeCorrespondingToParamInList(paramNodes, paramsBeforeDiagnosticParam);
            }
            if (paramsAfterDiagnosticParam.Any())
            {
                nodeAfterNewParamNode = GetLastParamNodeCorrespondingToParamInList(paramNodes.Reverse(), paramsAfterDiagnosticParam);
                var indexOfNode = docCommentChildNodes.IndexOf(nodeAfterNewParamNode);

                // set insert node to be the doc comment signifier of the closest param before the new node
                for (var i = indexOfNode - 1; i >= 0; i--)
                {
                    if (docCommentChildNodes[i] is TXmlTextSyntax)
                    {
                        nodeAfterNewParamNode = docCommentChildNodes[i];
                        break;
                    }
                }
            }

            // No `param` nodes either before or after the current node
            // This will currently never be hit as CS1573 only fires if there is a `param` node in the documentation
            // and there is no VB equivalent yet
            if (paramsBeforeDiagnosticParam == null &&
                paramsAfterDiagnosticParam == null)    
            {
                // First try to insert the node after the `summary` node,
                // then just insert it at the top of the comment
                var summaryNode = docCommentChildNodes.OfType<TXmlElementSyntax>().FirstOrDefault(f => GetXmlElementLocalName(f) == "summary");
                if (summaryNode != null)
                {
                    nodeBeforeNewParamNode = summaryNode;
                }
                else
                {
                    nodeAfterNewParamNode = docCommentChildNodes.First();
                }
            }

            // this must always have a value because doc comments always have the doc comment token
            var newNodeList = new SyntaxNode[]
            {
                GetNewNode(GetParameterName(parameter),
                    isFirstNodeInComment: docCommentChildNodes.First() == nodeAfterNewParamNode)
            };

            SyntaxNode newDocComment = null;
            if (nodeBeforeNewParamNode != null)
            {
                newDocComment = docCommentNode.InsertNodesAfter(nodeBeforeNewParamNode, newNodeList);
            }
            else
            {
                // there were no `param` nodes above the new one
                newDocComment = docCommentNode.InsertNodesBefore(nodeAfterNewParamNode, newNodeList);
            }

            var newRoot = root.ReplaceNode(docCommentNode, newDocComment.WithAdditionalAnnotations(Formatter.Annotation));
            return document.WithSyntaxRoot(newRoot);
        }

        protected TXmlElementSyntax GetLastParamNodeCorrespondingToParamInList(IEnumerable<TXmlElementSyntax> paramNodeList, List<string> methodParamSubset)
        {
            TXmlElementSyntax nodeAfterNewParamNode = null;

            foreach (var paramNode in paramNodeList)
            {
                var paramNameForNode = GetNameAttributes(paramNode);

                // param node is missing `name` attribute or there are multiple `name` attributes
                // there is currently no test for this scenario because CS1573 does not fire if there are malformed nodes
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
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Add_attribute, createChangedDocument)
            {
            }
        }
    }
}