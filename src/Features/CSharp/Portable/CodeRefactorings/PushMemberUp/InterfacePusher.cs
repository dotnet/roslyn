// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeActions;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PushMemberUp
{
    internal class InterfacePusher : AbstractMemberPusher
    {
        internal InterfacePusher(INamedTypeSymbol targetInterfaceSymbol, SemanticModel semanticModel, SyntaxNode userSelectedNode, Document contextDocument):
            base(targetInterfaceSymbol, semanticModel, userSelectedNode, contextDocument)
        {
        }

        protected override bool AreModifiersValid(SyntaxNode userSelectNode)
        {
            var userSelectNodeSymbol = SemanticModel.GetDeclaredSymbol(userSelectNode);

            if (userSelectNodeSymbol != null)
            {
                return !userSelectNodeSymbol.IsStatic && !userSelectNodeSymbol.IsAbstract &&
                        (userSelectNodeSymbol.DeclaredAccessibility == Accessibility.Public);
            }
            else
            {
                return false;
            }
        }

        protected override CodeAction CreateDocumentChangeAction(MemberDeclarationSyntax memberToPushUp, Document contextDocument)
        {
            return new DocumentChangeAction(
               Title,
               async _ =>
               {
                   var documentEditor = await DocumentEditor.CreateAsync(contextDocument).ConfigureAwait(false);
                   documentEditor.AddMember(TargetSyntaxNode, memberToPushUp);
                   return documentEditor.GetChangedDocument();
               });
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
                async _ =>
                {
                    var solution = contextDocument.Project.Solution;
                    var solutionEditor = new SolutionEditor(solution);
                    var targetDocumentEditor = await solutionEditor.GetDocumentEditorAsync(solution.GetDocument(TargetSyntaxNode.SyntaxTree).Id).ConfigureAwait(false);

                    targetDocumentEditor.AddMember(TargetSyntaxNode, memberToPushUp);
                    return solutionEditor.GetChangedSolution();
                });
        }

        protected override MethodDeclarationSyntax CreateMethodPushUpSyntax(MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return SyntaxFactory.MethodDeclaration(
                        methodDeclarationSyntax.AttributeLists,
                        default,
                        methodDeclarationSyntax.ReturnType,
                        default,
                        methodDeclarationSyntax.Identifier,
                        methodDeclarationSyntax.TypeParameterList,
                        methodDeclarationSyntax.ParameterList,
                        methodDeclarationSyntax.ConstraintClauses,
                        default,
                        default,
                        SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        protected override IndexerDeclarationSyntax CreateIndexerPushUpSyntax(IndexerDeclarationSyntax indexerDeclarationSyntax)
        {
            var propertySymbol = SemanticModel.GetDeclaredSymbol(indexerDeclarationSyntax);
            var accessorList = SyntaxFactory.AccessorList();
            var syntaxList = new SyntaxList<AccessorDeclarationSyntax>();
            
            if (propertySymbol.GetMethod != null &&
                propertySymbol.GetMethod.DeclaredAccessibility == Accessibility.Public)
            {
                syntaxList = syntaxList.Add(CreateGetterAccessor());
            }
            if (propertySymbol.SetMethod != null &&
                propertySymbol.SetMethod.DeclaredAccessibility == Accessibility.Public)
            {
                syntaxList = syntaxList.Add(CreateSetterAccessor());
            }

            return SyntaxFactory.IndexerDeclaration(
                indexerDeclarationSyntax.AttributeLists,
                default,
                indexerDeclarationSyntax.Type,
                default,
                indexerDeclarationSyntax.ParameterList,
                accessorList.WithAccessors(syntaxList));
        }

        protected override PropertyDeclarationSyntax CreatePropertyPushUpSyntax(PropertyDeclarationSyntax propertyFieldDeclarationSyntax)
        {
            var propertySymbol = SemanticModel.GetDeclaredSymbol(propertyFieldDeclarationSyntax);
            var accessorList = SyntaxFactory.AccessorList();
            var syntaxList = new SyntaxList<AccessorDeclarationSyntax>();
            if (propertySymbol.GetMethod != null &&
                propertySymbol.GetMethod.DeclaredAccessibility == Accessibility.Public)
            {
               syntaxList = syntaxList.Add(CreateGetterAccessor());
            }
            if (propertySymbol.SetMethod != null &&
                propertySymbol.SetMethod.DeclaredAccessibility == Accessibility.Public)
            {
               syntaxList = syntaxList.Add(CreateSetterAccessor());
            }

            return SyntaxFactory.PropertyDeclaration(
                propertyFieldDeclarationSyntax.AttributeLists,
                default,
                propertyFieldDeclarationSyntax.Type,
                default,
                propertyFieldDeclarationSyntax.Identifier,
                accessorList.WithAccessors(syntaxList));
        }

        private AccessorDeclarationSyntax CreateGetterAccessor()
        {
            return SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).
                WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        private AccessorDeclarationSyntax CreateSetterAccessor()
        {
            return SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).
                WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
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
