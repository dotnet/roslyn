using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp
{
    internal class ChangedSolutionAndDocumentCreator
    {
        internal async Task<Solution> AddMembersToSolutionAsync(
            IEnumerable<SyntaxNode> nodesToPullUp,
            SyntaxNode targetSyntaxNode,
            Document contextDocument,
            CancellationToken cancellationToken)
        {
            var solution = contextDocument.Project.Solution;
            var solutionEditor = new SolutionEditor(solution);
            var targetDocumentEditor = await solutionEditor.GetDocumentEditorAsync(solution.GetDocument(targetSyntaxNode.SyntaxTree).Id, cancellationToken).ConfigureAwait(false);

            foreach (var node in nodesToPullUp)
            {
                targetDocumentEditor.AddMember(targetSyntaxNode, node);
            }
            return solutionEditor.GetChangedSolution();
        }       

        internal async Task<Document> AddMembersToDocumentAsync(
            IEnumerable<SyntaxNode> nodesToPullUp,
            SyntaxNode targetSyntaxNode,
            Document contextDocument,
            CancellationToken cancellation)
        {
            var documentEditor = await DocumentEditor.CreateAsync(contextDocument, cancellation).ConfigureAwait(false);
            foreach (var node in nodesToPullUp)
            {
               documentEditor.AddMember(targetSyntaxNode, node);
            }
            return documentEditor.GetChangedDocument();
        }

        internal async Task<Document> MoveMemberToDocumentAsync(
            SyntaxNode nodeToPullUp,
            SyntaxNode targetNode,
            SyntaxNode selectedNode,
            Document contextDocument,
            CancellationToken cancellationToken)
        {
            var documentEditor = await DocumentEditor.CreateAsync(contextDocument).ConfigureAwait(false);
            RemoveNode(documentEditor, selectedNode);
            documentEditor.ReplaceNode(targetNode, nodeToPullUp);
            return documentEditor.GetChangedDocument();
        }

        internal async Task<Solution> MoveMemberToSolutionAsync(
            SyntaxNode nodeToPullUp,
            SyntaxNode targetNode,
            SyntaxNode selectedNode,
            Document contextDocument,
            CancellationToken cancellationToken)
        {
            var solution = contextDocument.Project.Solution;
            var solutionEditor = new SolutionEditor(solution);
            var targetDocument = solution.GetDocument(targetNode.SyntaxTree);

            var contextDocumentEditor = await solutionEditor.GetDocumentEditorAsync(contextDocument.Id);
            var targetDocumentEditor = await solutionEditor.GetDocumentEditorAsync(targetDocument.Id);

            RemoveNode(contextDocumentEditor, selectedNode);
            targetDocumentEditor.ReplaceNode(targetNode, nodeToPullUp);

            return solutionEditor.GetChangedSolution();
        }

        internal SyntaxNode FindSyntaxNodeLocation(ISymbol symbol)
        {
            var targets = symbol.DeclaringSyntaxReferences;
            if (targets.Length == 1)
            {
                return targets.First().GetSyntax();
            }
            else if (targets.Length > 1)
            {
                return default;
            }
            else
            {
                return default;
            }
        }

        private BaseFieldDeclarationSyntax FindFieldAndEventDeclaration(SyntaxNode variableDeclaratorNode)
        {
            // TODO: Make sure it really return the declaration
            return variableDeclaratorNode.Ancestors().OfType<BaseFieldDeclarationSyntax>().First();
        }

        private void RemoveNode(DocumentEditor editor, SyntaxNode userSelectedMemberNode)
        {
            if (userSelectedMemberNode is VariableDeclaratorSyntax variableDeclarator)
            {
                var variableDeclaration = FindFieldAndEventDeclaration(variableDeclarator);
                if (variableDeclaration.Declaration.Variables.Count() == 1)
                {
                    // If there is only one variable, e.g.
                    // public int i = 0;
                    // Just remove all
                    editor.RemoveNode(variableDeclaration);
                }
                else
                {
                    // public int i, j = 0;
                    // Remove only one variable
                    editor.RemoveNode(variableDeclarator);
                }
            }
            else
            {
                editor.RemoveNode(userSelectedMemberNode);
            }
        }
    }
}
