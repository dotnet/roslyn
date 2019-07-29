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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ConvertAutoPropertyToFullProperty
{
    internal abstract class AbstractConvertAutoPropertyToFullPropertyCodeRefactoringProvider<TPropertyDeclarationNode, TTypeDeclarationNode>
        : CodeRefactoringProvider
        where TPropertyDeclarationNode : SyntaxNode
        where TTypeDeclarationNode : SyntaxNode
    {
        internal abstract Task<string> GetFieldNameAsync(Document document, IPropertySymbol propertySymbol, CancellationToken cancellationToken);
        internal abstract (SyntaxNode newGetAccessor, SyntaxNode newSetAccessor) GetNewAccessors(
            DocumentOptionSet options, SyntaxNode property, string fieldName, SyntaxGenerator generator);
        internal abstract SyntaxNode GetPropertyWithoutInitializer(SyntaxNode property);
        internal abstract SyntaxNode GetInitializerValue(SyntaxNode property);
        internal abstract SyntaxNode ConvertPropertyToExpressionBodyIfDesired(DocumentOptionSet options, SyntaxNode fullProperty);
        internal abstract SyntaxNode GetTypeBlock(SyntaxNode syntaxNode);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var property = await GetPropertyAsync(context).ConfigureAwait(false);
            if (property == null)
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            if (!(semanticModel.GetDeclaredSymbol(property) is IPropertySymbol propertySymbol))
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
                    c => ExpandToFullPropertyAsync(document, property, propertySymbol, root, c)));
        }

        internal bool IsValidAutoProperty(SyntaxNode property, IPropertySymbol propertySymbol)
        {
            var fields = propertySymbol.ContainingType.GetMembers().OfType<IFieldSymbol>();
            var field = fields.FirstOrDefault(f => propertySymbol.Equals(f.AssociatedSymbol));
            return field != null;
        }

        private async Task<SyntaxNode> GetPropertyAsync(CodeRefactoringContext context)
        {
            var containingProperty = await context.TryGetRelevantNodeAsync<TPropertyDeclarationNode>().ConfigureAwait(false);
            if (!(containingProperty?.Parent is TTypeDeclarationNode))
            {
                return null;
            }

            return containingProperty;
        }

        private async Task<Document> ExpandToFullPropertyAsync(
            Document document,
            SyntaxNode property,
            IPropertySymbol propertySymbol,
            SyntaxNode root,
            CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var workspace = document.Project.Solution.Workspace;
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            // Create full property. If the auto property had an initial value
            // we need to remove it and later add it to the backing field
            var fieldName = await GetFieldNameAsync(document, propertySymbol, cancellationToken).ConfigureAwait(false);
            var (newGetAccessor, newSetAccessor) = GetNewAccessors(options, property, fieldName, generator);
            var fullProperty = generator
                .WithAccessorDeclarations(
                    GetPropertyWithoutInitializer(property),
                    newSetAccessor == null
                        ? new SyntaxNode[] { newGetAccessor }
                        : new SyntaxNode[] { newGetAccessor, newSetAccessor })
                .WithLeadingTrivia(property.GetLeadingTrivia());
            fullProperty = ConvertPropertyToExpressionBodyIfDesired(options, fullProperty);
            var editor = new SyntaxEditor(root, workspace);
            editor.ReplaceNode(property, fullProperty.WithAdditionalAnnotations(Formatter.Annotation));

            // add backing field, plus initializer if it exists 
            var newField = CodeGenerationSymbolFactory.CreateFieldSymbol(
                default, Accessibility.Private,
                DeclarationModifiers.From(propertySymbol),
                propertySymbol.Type, fieldName,
                initializer: GetInitializerValue(property));

            var typeDeclaration = propertySymbol.ContainingType.DeclaringSyntaxReferences;
            foreach (var td in typeDeclaration)
            {
                var block = GetTypeBlock(await td.GetSyntaxAsync(cancellationToken).ConfigureAwait(false));
                if (property.Ancestors().Contains(block))
                {
                    editor.ReplaceNode(block, (currentTypeDecl, _)
                        => CodeGenerator.AddFieldDeclaration(currentTypeDecl, newField, workspace)
                        .WithAdditionalAnnotations(Formatter.Annotation));
                }
            }

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
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
