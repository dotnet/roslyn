// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PushMemberUp
{
    internal class ClassPusherWithQuickAction : AbstractMemberPusherWithQuickAction
    {
        internal ClassPusherWithQuickAction(INamedTypeSymbol targetClassSymbol, SemanticModel semanticModel, SyntaxNode userSelectedNode, Document contextDocument):
            base(targetClassSymbol, semanticModel, userSelectedNode, contextDocument)
        {
            SyntaxGenerator = new ClassPushUpMemberSyntaxGenerator();
        }

        internal override bool AreModifiersValid(INamedTypeSymbol targetSymbol, ISymbol selectedMembers)
        {
            var validator = new ClassModifiersValidator();
            return validator.IsAbstractModifiersMatch(targetSymbol, selectedMembers) &&
                   validator.IsStaticModifiersMatch(targetSymbol, selectedMembers);
        }

        protected override CodeAction CreateDocumentChangeAction(MemberDeclarationSyntax memberToPush, Document contextDocument)
        {
            return new DocumentChangeAction(
                Title,
                async _ =>
                {
                    var documentEditor = await DocumentEditor.CreateAsync(contextDocument).ConfigureAwait(false);
                    RemoveNode(documentEditor, UserSelectedNode);
                    documentEditor.AddMember(TargetSyntaxNode, memberToPush);
                    return documentEditor.GetChangedDocument();
                });
        }

        protected override CodeAction CreateSolutionChangeAction(MemberDeclarationSyntax memberToPush, Document contextDocument)
        {
            return new SolutionChangeAction(
               Title,
               async _ =>
               {
                   var solution = contextDocument.Project.Solution;
                   var solutionEditor = new SolutionEditor(solution);
                   var targetDocument = solution.GetDocument(TargetSyntaxNode.SyntaxTree);

                   var contextDocumentEditor = await solutionEditor.GetDocumentEditorAsync(contextDocument.Id);
                   var targetDocumentEditor = await solutionEditor.GetDocumentEditorAsync(targetDocument.Id);

                   RemoveNode(contextDocumentEditor, UserSelectedNode);
                   targetDocumentEditor.AddMember(TargetSyntaxNode, memberToPush);

                   return solutionEditor.GetChangedSolution();
               });
        }
        
        private void RemoveNode(DocumentEditor editor, SyntaxNode selectedMemberNode)
        {
            if (selectedMemberNode is BaseFieldDeclarationSyntax eventOrFieldNode)
            {
                if (eventOrFieldNode.Declaration.Variables.Count() == 1)
                {
                    // If there is only one variable, e.g.
                    // public int i = 0;
                    // Just remove all
                    editor.RemoveNode(selectedMemberNode);
                }
                else
                {
                    // public int i, j = 0;
                    // Remove only one variable
                    editor.RemoveNode(UserSelectedNode);
                }
            }
            else
            {
                editor.RemoveNode(selectedMemberNode);
            }
        }
    }
}
