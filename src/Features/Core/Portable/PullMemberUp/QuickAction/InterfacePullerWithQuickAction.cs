// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.PullMemberUp.QuickAction
{
    internal class InterfacePullerWithQuickAction : AbstractMemberPullerWithQuickAction
    {
        private IMethodSymbol FilterGetterOrSetter(IMethodSymbol getterOrSetter)
        {
            if (getterOrSetter == null)
            {
                return getterOrSetter;
            }
            else
            {
                return getterOrSetter.DeclaredAccessibility == Accessibility.Public ? getterOrSetter : null;
            }
        }

        internal override async Task<CodeAction> CreateAction(ISymbol memberSymbol)
        {
            var memberToPullUp = memberSymbol;
            if (memberSymbol is IPropertySymbol propertyOrIndexerSymbol)
            {
                memberToPullUp = CodeGenerationSymbolFactory.CreatePropertySymbol(
                    propertyOrIndexerSymbol,
                    propertyOrIndexerSymbol.GetAttributes(),
                    propertyOrIndexerSymbol.DeclaredAccessibility,
                    DeclarationModifiers.From(propertyOrIndexerSymbol),
                    propertyOrIndexerSymbol.ExplicitInterfaceImplementations,
                    propertyOrIndexerSymbol.Name,
                    propertyOrIndexerSymbol.IsIndexer,
                    FilterGetterOrSetter(propertyOrIndexerSymbol.GetMethod), 
                    FilterGetterOrSetter(propertyOrIndexerSymbol.SetMethod));

            }
            var options = new CodeGenerationOptions(generateMembers: false, generateMethodBodies: false);
            var changedDocument = await CodeGenerationService.AddMembersAsync(
                ContextDocument.Project.Solution,
                TargetTypeSymbol,
                new ISymbol[] { memberToPullUp },
                options: options,
                cancellationToken: _cancellationToken);

            return new DocumentChangeAction(Title, _ => Task.FromResult(changedDocument));
        }

        protected override bool IsDeclarationAlreadyInTarget(INamedTypeSymbol targetSymbol, ISymbol userSelectedNodeSymbol)
        {
            var allMembers = targetSymbol.GetMembers();

            foreach (var member in allMembers)
            {
                var implementationOfMember = userSelectedNodeSymbol.ContainingType.FindImplementationForInterfaceMember(member);
                if (userSelectedNodeSymbol.OriginalDefinition.Equals(implementationOfMember?.OriginalDefinition))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
