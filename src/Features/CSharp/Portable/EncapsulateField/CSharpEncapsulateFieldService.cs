// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.EncapsulateField;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EncapsulateField
{
    [ExportLanguageService(typeof(AbstractEncapsulateFieldService), LanguageNames.CSharp), Shared]
    internal class CSharpEncapsulateFieldService : AbstractEncapsulateFieldService
    {
        protected async override Task<SyntaxNode> RewriteFieldNameAndAccessibility(string originalFieldName, bool makePrivate, Document document, SyntaxAnnotation declarationAnnotation, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var declarator = root.GetAnnotatedNodes<VariableDeclaratorSyntax>(declarationAnnotation).FirstOrDefault();

            // There may be no field to rewrite if this document is part of a set of linked files
            // and the declaration is not conditionally compiled in this document's project.
            if (declarator == null)
            {
                return root;
            }

            var tempAnnotation = new SyntaxAnnotation();
            var newIdentifier = SyntaxFactory.Identifier(originalFieldName)
                                             .WithTrailingTrivia(declarator.Identifier.TrailingTrivia)
                                             .WithLeadingTrivia(declarator.Identifier.LeadingTrivia);

            var updatedDeclarator = declarator.WithIdentifier(newIdentifier).WithAdditionalAnnotations(tempAnnotation);

            root = root.ReplaceNode(declarator, updatedDeclarator);
            document = document.WithSyntaxRoot(root);

            var declaration = root.GetAnnotatedNodes<SyntaxNode>(tempAnnotation).First().Parent as VariableDeclarationSyntax;

            if (declaration.Variables.Count == 1)
            {
                var fieldSyntax = declaration.Parent as FieldDeclarationSyntax;

                var modifierKinds = new[] { SyntaxKind.PrivateKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.InternalKeyword, SyntaxKind.PublicKeyword };

                if (makePrivate)
                {
                    var modifiers = SpecializedCollections.SingletonEnumerable(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                                                            .Concat(fieldSyntax.Modifiers.Where(m => !modifierKinds.Contains(m.Kind())));

                    root = root.ReplaceNode(fieldSyntax, fieldSyntax.WithModifiers(
                        SyntaxFactory.TokenList(modifiers))
                            .WithAdditionalAnnotations(Formatter.Annotation)
                            .WithLeadingTrivia(fieldSyntax.GetLeadingTrivia())
                            .WithTrailingTrivia(fieldSyntax.GetTrailingTrivia()));
                }
            }
            else if (declaration.Variables.Count > 1 && makePrivate)
            {
                document = document.WithSyntaxRoot(root);
                var codeGenService = document.GetLanguageService<ICodeGenerationService>();
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                declarator = root.GetAnnotatedNodes<VariableDeclaratorSyntax>(tempAnnotation).First();
                declaration = declarator.Parent as VariableDeclarationSyntax;

                var field = semanticModel.GetDeclaredSymbol(declarator, cancellationToken) as IFieldSymbol;

                var fieldToAdd = declarationAnnotation.AddAnnotationToSymbol(CodeGenerationSymbolFactory.CreateFieldSymbol(
                    field.GetAttributes(),
                    Accessibility.Private,
                    new DeclarationModifiers(isStatic: field.IsStatic, isReadOnly: field.IsReadOnly, isConst: field.IsConst),
                    field.Type,
                    field.Name,
                    field.HasConstantValue,
                    field.ConstantValue,
                    declarator.Initializer));

                var withField = await codeGenService.AddFieldAsync(document.Project.Solution, field.ContainingType, fieldToAdd, new CodeGenerationOptions(), cancellationToken).ConfigureAwait(false);
                root = await withField.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                declarator = root.GetAnnotatedNodes<VariableDeclaratorSyntax>(tempAnnotation).First();
                declaration = declarator.Parent as VariableDeclarationSyntax;

                return root.RemoveNode(declarator, SyntaxRemoveOptions.KeepNoTrivia);
            }

            return root;
        }

        protected override async Task<IEnumerable<IFieldSymbol>> GetFieldsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var fields = root.DescendantNodes(d => d.Span.IntersectsWith(span))
                                    .OfType<FieldDeclarationSyntax>()
                                    .Where(n => n.Span.IntersectsWith(span));

            var declarations = fields.Where(CanEncapsulate).Select(f => f.Declaration);

            IEnumerable<VariableDeclaratorSyntax> declarators;
            if (span.IsEmpty)
            {
                // no selection, get all variables
                declarators = declarations.SelectMany(d => d.Variables);
            }
            else
            {
                // has selection, get only the ones that are included in the selection
                declarators = declarations.SelectMany(d => d.Variables.Where(v => v.Span.IntersectsWith(span)));
            }

            return declarators.Select(d => semanticModel.GetDeclaredSymbol(d, cancellationToken) as IFieldSymbol)
                                .WhereNotNull()
                                .Where(f => f.Name.Length != 0);
        }

        private bool CanEncapsulate(FieldDeclarationSyntax field)
        {
            return field.Parent is TypeDeclarationSyntax;
        }

        protected override Tuple<string, string> GeneratePropertyAndFieldNames(IFieldSymbol field)
        {
            // Special case: if the field is "new", we will preserve its original name and the new keyword.
            if (field.DeclaredAccessibility == Accessibility.Private || IsNew(field))
            {
                // Create some capitalized version of the field name for the property
                return Tuple.Create(field.Name, MakeUnique(GeneratePropertyName(field.Name), field.ContainingType));
            }
            else
            {
                // Generate the new property name using the rules from 695042
                var newPropertyName = GeneratePropertyName(field.Name);

                if (newPropertyName == field.Name)
                {
                    // If we wind up with the field's old name, give the field the unique version of its current name.
                    return Tuple.Create(MakeUnique(GenerateFieldName(field, field.Name), field.ContainingType), newPropertyName);
                }

                // Otherwise, ensure the property's name is unique.
                newPropertyName = MakeUnique(newPropertyName, field.ContainingType);
                var newFieldName = GenerateFieldName(field, newPropertyName);

                // If converting the new property's name into a field name results in the old field name, we're done.
                if (newFieldName == field.Name)
                {
                    return Tuple.Create(newFieldName, newPropertyName);
                }

                // Otherwise, ensure the new field name is unique.
                return Tuple.Create(MakeUnique(newFieldName, field.ContainingType), newPropertyName);
            }
        }

        private bool IsNew(IFieldSymbol field)
        {
            return field.DeclaringSyntaxReferences.Any(d => d.GetSyntax().GetAncestor<FieldDeclarationSyntax>().Modifiers.Any(SyntaxKind.NewKeyword));
        }

        private string GenerateFieldName(IFieldSymbol field, string correspondingPropertyName)
        {
            return char.ToLower(correspondingPropertyName[0]).ToString() + correspondingPropertyName.Substring(1);
        }

        protected string MakeUnique(string baseName, INamedTypeSymbol containingType, bool considerBaseMembers = true)
        {
            var containingTypeMemberNames = containingType.GetAccessibleMembersInThisAndBaseTypes<ISymbol>(containingType).Select(m => m.Name);
            return NameGenerator.GenerateUniqueName(baseName, containingTypeMemberNames.ToSet(), StringComparer.Ordinal);
        }

        internal override IEnumerable<SyntaxNode> GetConstructorNodes(INamedTypeSymbol containingType)
        {
            return containingType.Constructors.SelectMany(c => c.DeclaringSyntaxReferences.Select(d => d.GetSyntax()));
        }
    }
}
