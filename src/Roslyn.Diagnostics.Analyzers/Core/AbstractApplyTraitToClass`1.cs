// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Diagnostics.Analyzers
{
    public abstract class AbstractApplyTraitToClass<TAttributeSyntax> : CodeRefactoringProvider
        where TAttributeSyntax : SyntaxNode
    {
        protected AbstractApplyTraitToClass()
        {
        }

        private protected abstract IRefactoringHelpers RefactoringHelpers { get; }

        protected abstract SyntaxNode? GetTypeDeclarationForNode(SyntaxNode reportedNode);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var attribute = await context.TryGetRelevantNodeAsync<TAttributeSyntax>(RefactoringHelpers).ConfigureAwait(false);
            if (attribute is null)
            {
                // No attribute in context
                return;
            }

            var syntaxGenerator = SyntaxGenerator.GetGenerator(context.Document);
            if (syntaxGenerator.TryGetContainingDeclaration(attribute, DeclarationKind.Method) is null)
            {
                // The attribute is not applied to a method
                return;
            }

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var attributeType = semanticModel!.GetTypeInfo(attribute, context.CancellationToken);
            if (attributeType.Type is not { Name: "TraitAttribute", ContainingNamespace: { Name: "Xunit", ContainingNamespace: { IsGlobalNamespace: true } } })
                return;

            var location = attribute.GetLocation();
            context.RegisterRefactoring(
                CodeAction.Create(
                    RoslynDiagnosticsAnalyzersResources.ApplyTraitToContainingType,
                    cancellationToken => ApplyTraitToClassAsync(context.Document, location.SourceSpan, cancellationToken),
                    nameof(AbstractApplyTraitToClass<TAttributeSyntax>)));
        }

        private async Task<Document> ApplyTraitToClassAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var semanticModel = (await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false))!;

            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxRoot = await syntaxTree!.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var reportedNode = syntaxRoot.FindNode(sourceSpan, getInnermostNodeForTie: true);
            var typeDeclaration = GetTypeDeclarationForNode(reportedNode);
            if (typeDeclaration is null)
                return document;

            var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
            if (syntaxGenerator.TryGetContainingDeclaration(reportedNode, DeclarationKind.Attribute) is not { } attribute)
            {
                return document;
            }

            if (syntaxGenerator.TryGetContainingDeclaration(attribute, DeclarationKind.Method) is not { } method)
            {
                // The attribute is not applied to a method
                return document;
            }

            var expectedAttributeData = semanticModel.GetDeclaredSymbol(method, cancellationToken).GetAttributes()
                .Single(attributeData => attributeData.ApplicationSyntaxReference is not null && attribute.Span.Contains(attributeData.ApplicationSyntaxReference.Span));

            var newTypeDeclaration = typeDeclaration.ReplaceNodes(
                syntaxGenerator.GetMembers(typeDeclaration),
                (originalNode, replacementNode) =>
                {
                    foreach (var attribute in syntaxGenerator.GetAttributes(originalNode))
                    {
                        var attributeType = semanticModel.GetTypeInfo(attribute, cancellationToken);
                        if (attributeType.Type is null)
                            attributeType = semanticModel.GetTypeInfo(attribute.ChildNodes().First(), cancellationToken);

                        if (attributeType.Type is not { Name: "TraitAttribute", ContainingNamespace: { Name: "Xunit", ContainingNamespace: { IsGlobalNamespace: true } } })
                            continue;

                        var actualAttributeData = semanticModel.GetDeclaredSymbol(originalNode, cancellationToken).GetAttributes()
                            .Single(attributeData => attributeData.ApplicationSyntaxReference is not null && attribute.Span.Contains(attributeData.ApplicationSyntaxReference.Span));

                        if (!expectedAttributeData.ConstructorArguments.SequenceEqual(actualAttributeData.ConstructorArguments))
                            continue;

                        return syntaxGenerator.RemoveNode(originalNode, attribute);
                    }

                    return originalNode;
                });

            newTypeDeclaration = syntaxGenerator.AddAttributes(newTypeDeclaration, attribute);

            return document.WithSyntaxRoot(syntaxRoot.ReplaceNode(typeDeclaration, newTypeDeclaration));
        }
    }
}
