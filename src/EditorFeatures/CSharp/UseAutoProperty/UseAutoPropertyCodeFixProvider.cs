using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
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

        protected override SyntaxNode UpdateProperty(
            Project project, IFieldSymbol fieldSymbol, IPropertySymbol propertySymbol, PropertyDeclarationSyntax propertyDeclaration,
            bool isWrittenOutsideOfConstructor, CancellationToken cancellationToken)
        {
            var updatedProperty = propertyDeclaration.WithAccessorList(UpdateAccessorList(propertyDeclaration.AccessorList));

            // We may need to add a setter if the field is written to outside of the constructor
            // of it's class.
            if (NeedsSetter(propertyDeclaration, isWrittenOutsideOfConstructor))
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

            return updatedProperty;
        }

        private bool NeedsSetter(PropertyDeclarationSyntax propertyDeclaration, bool isWrittenOutsideOfConstructor)
        {
            return isWrittenOutsideOfConstructor && !propertyDeclaration.AccessorList.Accessors.Any(SyntaxKind.SetAccessorDeclaration);
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