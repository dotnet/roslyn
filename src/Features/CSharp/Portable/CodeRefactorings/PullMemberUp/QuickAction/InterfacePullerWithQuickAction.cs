// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PullMemberUp.QuickAction;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
{
    internal class InterfacePullerWithQuickAction : AbstractMemberPullerWithQuickAction
    {
        internal override bool AreModifiersValid(INamedTypeSymbol targetSymbol, ISymbol selectedMembers)
        {
            var validator = new InterfaceModifiersValidator();
            return validator.AreModifiersValid(targetSymbol, new ISymbol[] { selectedMembers });
        }

        internal override async Task<CodeAction> CreateAction(IMethodSymbol methodSymbol, Document contextDocument)
        {
            var changedDocument = await CodeGenerationService.AddMethodAsync(contextDocument.Project.Solution, TargetTypeSymbol, methodSymbol);
            return new DocumentChangeAction(Title, _ => Task.FromResult(changedDocument));
        }

        internal override async Task<CodeAction> CreateAction(IPropertySymbol propertyOrIndexerSymbol, Document contextDocument)
        {
            var pullUpSymbol = CodeGenerationSymbolFactory.CreatePropertySymbol(
                propertyOrIndexerSymbol,
                propertyOrIndexerSymbol.GetAttributes(),
                propertyOrIndexerSymbol.DeclaredAccessibility,
                DeclarationModifiers.From(propertyOrIndexerSymbol),
                propertyOrIndexerSymbol.ExplicitInterfaceImplementations,
                propertyOrIndexerSymbol.Name,
                propertyOrIndexerSymbol.IsIndexer,
                propertyOrIndexerSymbol.GetMethod.DeclaredAccessibility == Accessibility.Public ? propertyOrIndexerSymbol.GetMethod : null,
                propertyOrIndexerSymbol.SetMethod.DeclaredAccessibility == Accessibility.Public ? propertyOrIndexerSymbol.SetMethod : null);

            var changedDocument = await CodeGenerationService.AddPropertyAsync(contextDocument.Project.Solution, TargetTypeSymbol, pullUpSymbol);
            return new DocumentChangeAction(Title, _ => Task.FromResult(changedDocument));
        }

        internal override async Task<CodeAction> CreateAction(IEventSymbol eventSymbol, Document contextDocument)
        {
            var option = new CodeGenerationOptions(generateMembers: false, generateMethodBodies: false);
            var changedDocument = await CodeGenerationService.AddEventAsync(contextDocument.Project.Solution, TargetTypeSymbol ,eventSymbol, option);
            return new DocumentChangeAction(Title, _ => Task.FromResult(changedDocument));
        }

        internal override Task<CodeAction> CreateAction(IFieldSymbol fieldSymbol, Document contextDocument)
        {
            throw new NotImplementedException("Can't pull a field up to interface");
        }

        protected override bool IsDeclarationAlreadyInTarget()
        {
            var allMembers = TargetTypeSymbol.GetMembers();

            foreach (var member in allMembers)
            {
                var implementationOfMember = UserSelectedNodeSymbol.ContainingType.FindImplementationForInterfaceMember(member);
                if (UserSelectedNodeSymbol.OriginalDefinition.Equals(implementationOfMember?.OriginalDefinition))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
