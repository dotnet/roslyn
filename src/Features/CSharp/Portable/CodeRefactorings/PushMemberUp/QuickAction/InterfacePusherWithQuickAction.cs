// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeActions;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PushMemberUp
{
    internal class InterfacePusherWithQuickAction : AbstractMemberPusherWithQuickAction
    {
        internal InterfacePusherWithQuickAction(
            INamedTypeSymbol targetInterfaceSymbol,
            SemanticModel semanticModel,
            SyntaxNode userSelectedNode,
            Document contextDocument):
            base(targetInterfaceSymbol, semanticModel, userSelectedNode, contextDocument)
        {
            SyntaxGenerator = new InterfacePushUpSyntaxGenerator(semanticModel);
        }

        internal override bool AreModifiersValid(INamedTypeSymbol targetSymbol, ISymbol selectedMembers)
        {
            var validator = new InterfaceModifiersValidator();
            return validator.AreModifiersValid(targetSymbol, selectedMembers);
        }

        protected override CodeAction CreateDocumentChangeAction(MemberDeclarationSyntax memberToPushUp, Document contextDocument)
        {
            return new DocumentChangeAction(
               Title,
               ct => SyntaxGenerator.CreateChangedDocument(memberToPushUp, TargetSyntaxNode, contextDocument, ct));
        }

        internal override CodeAction ComputeRefactoring()
        {
            if (IsDeclarationAlreadyInTarget())
            {
                return default;
            }
            return base.ComputeRefactoring();
        }

        protected override CodeAction CreateSolutionChangeAction(MemberDeclarationSyntax memberToPushUp, Document contextDocument)
        {
            return new SolutionChangeAction(
                Title,
                ct => SyntaxGenerator.CreateChangedSolution(
                    new List<MemberDeclarationSyntax>() { memberToPushUp },
                    TargetSyntaxNode, contextDocument, ct));
        }

        private bool IsDeclarationAlreadyInTarget()
        {
            var userSelectNodeSymbol = SemanticModel.GetDeclaredSymbol(UserSelectedNode);

            var allMembers = TargetTypeSymbol.GetMembers();

            foreach (var member in allMembers)
            {
                var implementationOfMember = userSelectNodeSymbol.ContainingType.FindImplementationForInterfaceMember(member);
                if (userSelectNodeSymbol.OriginalDefinition.Equals(implementationOfMember?.OriginalDefinition))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
