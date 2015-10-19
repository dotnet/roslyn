// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.UseAutoProperty;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UseAutoProperty
{
    [Shared]
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseAutoPropertyCodeFixProvider))]
    internal class UseAutoPropertyCodeFixProvider : AbstractUseAutoPropertyCodeFixProvider<PropertyDeclarationSyntax, FieldDeclarationSyntax, VariableDeclaratorSyntax, ConstructorDeclarationSyntax, ExpressionSyntax>
    {
        protected override SyntaxNode GetNodeToRemove(VariableDeclaratorSyntax declarator)
        {
            var fieldDeclaration = (FieldDeclarationSyntax)declarator.Parent.Parent;
            var nodeToRemove = fieldDeclaration.Declaration.Variables.Count > 1 ? declarator : (SyntaxNode)fieldDeclaration;
            return nodeToRemove;
        }

        protected override async Task<SyntaxNode> UpdatePropertyAsync(
            Project project, Compilation compilation, IFieldSymbol fieldSymbol, IPropertySymbol propertySymbol, 
            PropertyDeclarationSyntax propertyDeclaration, bool isWrittenOutsideOfConstructor, CancellationToken cancellationToken)
        {
            var updatedProperty = propertyDeclaration.WithAccessorList(UpdateAccessorList(propertyDeclaration.AccessorList));

            // We may need to add a setter if the field is written to outside of the constructor
            // of it's class.
            if (NeedsSetter(compilation, propertyDeclaration, isWrittenOutsideOfConstructor))
            {
                var accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                               .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                var generator = SyntaxGenerator.GetGenerator(project);

                if (fieldSymbol.DeclaredAccessibility != propertySymbol.DeclaredAccessibility)
                {
                    accessor = (AccessorDeclarationSyntax)generator.WithAccessibility(accessor, fieldSymbol.DeclaredAccessibility);
                }

                updatedProperty = updatedProperty.AddAccessorListAccessors(accessor);
            }

            var fieldInitializer = await GetFieldInitializerAsync(fieldSymbol, cancellationToken).ConfigureAwait(false);
            if (fieldInitializer != null)
            {
                updatedProperty = updatedProperty.WithInitializer(SyntaxFactory.EqualsValueClause(fieldInitializer))
                                                 .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            }

            return updatedProperty;
        }

        private async Task<ExpressionSyntax> GetFieldInitializerAsync(IFieldSymbol fieldSymbol, CancellationToken cancellationToken)
        {
            var variableDeclarator = (VariableDeclaratorSyntax)await fieldSymbol.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
            return variableDeclarator.Initializer?.Value;
        }

        private bool NeedsSetter(Compilation compilation, PropertyDeclarationSyntax propertyDeclaration, bool isWrittenOutsideOfConstructor)
        {
            if (propertyDeclaration.AccessorList.Accessors.Any(SyntaxKind.SetAccessorDeclaration))
            {
                // Already has a setter.
                return false;
            }

            if (!SupportsReadOnlyProperties(compilation))
            {
                // If the language doesn't have readonly properties, then we'll need a 
                // setter here.
                return true;
            }

            // If we're written outside a constructor we need a setter.
            return isWrittenOutsideOfConstructor;
        }

        private bool SupportsReadOnlyProperties(Compilation compilation)
        {
            return ((CSharpCompilation)compilation).LanguageVersion >= LanguageVersion.CSharp6;
        }

        private AccessorListSyntax UpdateAccessorList(AccessorListSyntax accessorList)
        {
            return accessorList.WithAccessors(SyntaxFactory.List(GetAccessors(accessorList.Accessors)));
        }

        private IEnumerable<AccessorDeclarationSyntax> GetAccessors(SyntaxList<AccessorDeclarationSyntax> accessors)
        {
            foreach (var accessor in accessors)
            {
                yield return accessor.WithBody(null).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            }
        }
    }
}