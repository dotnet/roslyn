// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.UseAutoProperty;

namespace Microsoft.CodeAnalysis.CSharp.UseAutoProperty
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpUseAutoPropertyCodeFixProvider)), Shared]
    internal class CSharpUseAutoPropertyCodeFixProvider
        : AbstractUseAutoPropertyCodeFixProvider<TypeDeclarationSyntax, PropertyDeclarationSyntax, VariableDeclaratorSyntax, ConstructorDeclarationSyntax, ExpressionSyntax>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpUseAutoPropertyCodeFixProvider()
        {
        }

        protected override PropertyDeclarationSyntax GetPropertyDeclaration(SyntaxNode node)
            => (PropertyDeclarationSyntax)node;

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
            private static bool ForceSingleSpace(SyntaxToken previousToken, SyntaxToken currentToken)
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

            public override AdjustNewLinesOperation GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
            {
                if (ForceSingleSpace(previousToken, currentToken))
                {
                    return null;
                }

                return base.GetAdjustNewLinesOperation(in previousToken, in currentToken, in nextOperation);
            }

            public override AdjustSpacesOperation GetAdjustSpacesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustSpacesOperation nextOperation)
            {
                if (ForceSingleSpace(previousToken, currentToken))
                {
                    return new AdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }

                return base.GetAdjustSpacesOperation(in previousToken, in currentToken, in nextOperation);
            }
        }

        private static async Task<ExpressionSyntax> GetFieldInitializerAsync(IFieldSymbol fieldSymbol, CancellationToken cancellationToken)
        {
            var variableDeclarator = (VariableDeclaratorSyntax)await fieldSymbol.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
            return variableDeclarator.Initializer?.Value;
        }

        private static bool NeedsSetter(Compilation compilation, PropertyDeclarationSyntax propertyDeclaration, bool isWrittenOutsideOfConstructor)
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

        private static bool SupportsReadOnlyProperties(Compilation compilation)
            => ((CSharpCompilation)compilation).LanguageVersion >= LanguageVersion.CSharp6;

        private static AccessorListSyntax UpdateAccessorList(AccessorListSyntax accessorList)
        {
            if (accessorList == null)
            {
                var getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                          .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                return SyntaxFactory.AccessorList(SyntaxFactory.List(Enumerable.Repeat(getter, 1)));
            }

            return accessorList.WithAccessors(SyntaxFactory.List(GetAccessors(accessorList.Accessors)));
        }

        private static IEnumerable<AccessorDeclarationSyntax> GetAccessors(SyntaxList<AccessorDeclarationSyntax> accessors)
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
