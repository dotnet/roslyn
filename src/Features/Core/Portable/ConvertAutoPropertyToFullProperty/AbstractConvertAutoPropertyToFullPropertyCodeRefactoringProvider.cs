// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ConvertAutoPropertyToFullProperty
{
    internal abstract class AbstractConvertAutoPropertyToFullPropertyCodeRefactoringProvider
        : CodeRefactoringProvider
    {
        internal abstract SyntaxNode GetProperty(SyntaxToken token);
        internal abstract Task<string> GetFieldNameAsync(Document document, IPropertySymbol propertySymbol, CancellationToken cancellationToken);
        internal abstract SyntaxNode GetInitializerValue(SyntaxNode property);
        internal abstract SyntaxNode GetPropertyWithoutInitializer(SyntaxNode property);
        internal abstract (SyntaxNode newGetAccessor, SyntaxNode newSetAccessor) GetNewAccessors(
            DocumentOptionSet options, SyntaxNode property, string fieldName, SyntaxGenerator generator);
        internal abstract SyntaxNode GetTypeBlock(SyntaxNode syntaxNode);
        internal abstract SyntaxNode ConvertPropertyToExpressionBodyIfDesired(DocumentOptionSet options, SyntaxNode fullProperty);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(context.Span.Start);

            var property = GetProperty(token);
            if (property == null)
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var propertySymbol = semanticModel.GetDeclaredSymbol(property) as IPropertySymbol;
            if (propertySymbol == null)
            {
                return;
            }

            if (!(IsValidAutoProperty(property, propertySymbol)))
            {
                return;
            }

            context.RegisterRefactoring(
                new ConvertAutoPropertyToFullPropertyCodeAction(
                    FeaturesResources.Convert_to_full_property,
                    c => ExpandToFullPropertyAsync(document, property, propertySymbol, root, cancellationToken)));
        }

        internal bool IsValidAutoProperty(SyntaxNode property, IPropertySymbol propertySymbol)
        {
            var fields = propertySymbol.ContainingType.GetMembers().OfType<IFieldSymbol>();
            var field = fields.FirstOrDefault(f => propertySymbol.Equals(f.AssociatedSymbol));
            return field != null;
        }

        private async Task<Document> ExpandToFullPropertyAsync(
            Document document,
            SyntaxNode property,
            IPropertySymbol propertySymbol,
            SyntaxNode root,
            CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var newRoot = await ExpandPropertyAndAddFieldAsync(
                document,
                property,
                root,
                propertySymbol,
                generator,
                cancellationToken).ConfigureAwait(false);

            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<SyntaxNode> ExpandPropertyAndAddFieldAsync(
            Document document, SyntaxNode property, SyntaxNode root, 
            IPropertySymbol propertySymbol, SyntaxGenerator generator,
            CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            // Create full property. If the auto property had an initial value
            // we need to remove it and later add it to the backing field
            var fieldName = await GetFieldNameAsync(document, propertySymbol, cancellationToken).ConfigureAwait(false);
            var accessorTuple = GetNewAccessors(options, property,fieldName, generator);
            var fullProperty = generator
                .WithAccessorDeclarations(
                    GetPropertyWithoutInitializer(property),
                    accessorTuple.newSetAccessor == null
                        ? new SyntaxNode[] { accessorTuple.newGetAccessor }
                        : new SyntaxNode[] { accessorTuple.newGetAccessor, accessorTuple.newSetAccessor })
                .WithLeadingTrivia(property.GetLeadingTrivia());
            fullProperty = ConvertPropertyToExpressionBodyIfDesired(options,fullProperty);
            var editor = new SyntaxEditor(root, workspace);
            editor.ReplaceNode(property, fullProperty.WithAdditionalAnnotations(Formatter.Annotation));

            // add backing field, plus initializer if it exists 
            var newField = CodeGenerationSymbolFactory.CreateFieldSymbol(
                default, Accessibility.Private, 
                DeclarationModifiers.From(propertySymbol), 
                propertySymbol.Type, fieldName, 
                initializer: GetInitializerValue(property));
            var containingType = GetTypeBlock(
                propertySymbol.ContainingType.DeclaringSyntaxReferences.FirstOrDefault().GetSyntax(cancellationToken));
            editor.ReplaceNode(containingType, (currentTypeDecl, _) 
                => CodeGenerator.AddFieldDeclaration(currentTypeDecl, newField, workspace)
                .WithAdditionalAnnotations(Formatter.Annotation));

            return editor.GetChangedRoot();
        }

        private class ConvertAutoPropertyToFullPropertyCodeAction : CodeAction.DocumentChangeAction
        {
            public ConvertAutoPropertyToFullPropertyCodeAction(
                string title, 
                Func<CancellationToken, Task<Document>> createChangedDocument) : base(title, createChangedDocument)
            {
            }
        }
}
}
