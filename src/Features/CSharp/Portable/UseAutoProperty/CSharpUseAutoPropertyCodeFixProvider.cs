// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.UseAutoProperty;

namespace Microsoft.CodeAnalysis.CSharp.UseAutoProperty
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpUseAutoPropertyCodeFixProvider)), Shared]
    internal class CSharpUseAutoPropertyCodeFixProvider
        : AbstractUseAutoPropertyCodeFixProvider<TypeDeclarationSyntax, PropertyDeclarationSyntax, VariableDeclaratorSyntax, ConstructorDeclarationSyntax, ExpressionSyntax>
    {
        [ImportingConstructor]
        public CSharpUseAutoPropertyCodeFixProvider()
        {
        }

        protected override SyntaxNode GetNodeToRemove(VariableDeclaratorSyntax declarator)
        {
            var fieldDeclaration = (FieldDeclarationSyntax)declarator.Parent.Parent;
            var nodeToRemove = fieldDeclaration.Declaration.Variables.Count > 1 ? declarator : (SyntaxNode)fieldDeclaration;
            return nodeToRemove;
        }

        protected override async Task<SyntaxNode> UpdatePropertyAsync(
            Document propertyDocument, Compilation compilation, IFieldSymbol fieldSymbol, IPropertySymbol propertySymbol,
            PropertyDeclarationSyntax propertyDeclaration, bool isWrittenOutsideOfConstructor, CancellationToken cancellationToken)
        {
            var project = propertyDocument.Project;
            var sourceText = await propertyDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var trailingTrivia = propertyDeclaration.GetTrailingTrivia();

            var updatedProperty = propertyDeclaration.WithAccessorList(UpdateAccessorList(propertyDeclaration.AccessorList))
                                                     .WithExpressionBody(null)
                                                     .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None));

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

            return updatedProperty.WithTrailingTrivia(trailingTrivia).WithAdditionalAnnotations(SpecializedFormattingAnnotation);
        }

        protected override IEnumerable<AbstractFormattingRule> GetFormattingRules(Document document)
        {
            var rules = new List<AbstractFormattingRule> { new SingleLinePropertyFormattingRule() };
            rules.AddRange(Formatter.GetDefaultFormattingRules(document));

            return rules;
        }

        private class SingleLinePropertyFormattingRule : AbstractFormattingRule
        {
            private bool ForceSingleSpace(SyntaxToken previousToken, SyntaxToken currentToken)
            {
                if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentToken.Parent.IsKind(SyntaxKind.AccessorList))
                {
                    return true;
                }

                if (previousToken.IsKind(SyntaxKind.OpenBraceToken) && previousToken.Parent.IsKind(SyntaxKind.AccessorList))
                {
                    return true;
                }

                if (currentToken.IsKind(SyntaxKind.CloseBraceToken) && currentToken.Parent.IsKind(SyntaxKind.AccessorList))
                {
                    return true;
                }

                return false;
            }

            public override AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextGetAdjustNewLinesOperation nextOperation)
            {
                if (ForceSingleSpace(previousToken, currentToken))
                {
                    return null;
                }

                return base.GetAdjustNewLinesOperation(previousToken, currentToken, optionSet, in nextOperation);
            }

            public override AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextGetAdjustSpacesOperation nextOperation)
            {
                if (ForceSingleSpace(previousToken, currentToken))
                {
                    return new AdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }

                return base.GetAdjustSpacesOperation(previousToken, currentToken, optionSet, in nextOperation);
            }
        }

        private async Task<ExpressionSyntax> GetFieldInitializerAsync(IFieldSymbol fieldSymbol, CancellationToken cancellationToken)
        {
            var variableDeclarator = (VariableDeclaratorSyntax)await fieldSymbol.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
            return variableDeclarator.Initializer?.Value;
        }

        private bool NeedsSetter(Compilation compilation, PropertyDeclarationSyntax propertyDeclaration, bool isWrittenOutsideOfConstructor)
        {
            if (propertyDeclaration.AccessorList?.Accessors.Any(SyntaxKind.SetAccessorDeclaration) == true)
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
            if (accessorList == null)
            {
                var getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                          .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                return SyntaxFactory.AccessorList(SyntaxFactory.List(Enumerable.Repeat(getter, 1)));
            }

            return accessorList.WithAccessors(SyntaxFactory.List(GetAccessors(accessorList.Accessors)));
        }

        private IEnumerable<AccessorDeclarationSyntax> GetAccessors(SyntaxList<AccessorDeclarationSyntax> accessors)
        {
            foreach (var accessor in accessors)
            {
                yield return accessor.WithBody(null)
                                     .WithExpressionBody(null)
                                     .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            }
        }
    }
}
