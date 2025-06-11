// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace Roslyn.Diagnostics.Analyzers
{
    public abstract class AbstractApplyTraitToClass<TAttributeSyntax> : CodeRefactoringProvider
        where TAttributeSyntax : SyntaxNode
    {
        private protected abstract IRefactoringHelpers RefactoringHelpers { get; }

        protected abstract SyntaxNode? GetTypeDeclarationForNode(SyntaxNode reportedNode);

        private record State(
            Document Document,
            SemanticModel SemanticModel,
            INamedTypeSymbol TraitAttribute,
            TAttributeSyntax AttributeSyntax);

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

            var semanticModel = (await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false))!;
            if (semanticModel.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.XunitTraitAttribute) is not { } traitAttribute)
            {
                // Xunit.TraitAttribute not found in compilation
                return;
            }

            var attributeType = semanticModel.GetTypeInfo(attribute, context.CancellationToken);
            if (!SymbolEqualityComparer.Default.Equals(attributeType.Type, traitAttribute))
                return;

            var state = new State(context.Document, semanticModel, traitAttribute, attribute);
            context.RegisterRefactoring(
                CodeAction.Create(
                    RoslynDiagnosticsAnalyzersResources.ApplyTraitToContainingType,
                    cancellationToken => ApplyTraitToClassAsync(state, cancellationToken),
                    nameof(AbstractApplyTraitToClass<>)));
        }

        private async Task<Document> ApplyTraitToClassAsync(State state, CancellationToken cancellationToken)
        {
            var syntaxRoot = await state.SemanticModel.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var typeDeclaration = GetTypeDeclarationForNode(state.AttributeSyntax);
            if (typeDeclaration is null)
                return state.Document;

            var syntaxGenerator = SyntaxGenerator.GetGenerator(state.Document);
            if (syntaxGenerator.TryGetContainingDeclaration(state.AttributeSyntax, DeclarationKind.Attribute) is not { } attribute)
            {
                throw new InvalidOperationException("Failed to obtain the attribute declaration.");
            }

            if (syntaxGenerator.TryGetContainingDeclaration(attribute, DeclarationKind.Method) is not { } method)
            {
                throw new InvalidOperationException("Failed to obtain the method syntax to which the attribute is applied.");
            }

            var expectedAttributeData = state.SemanticModel.GetDeclaredSymbol(method, cancellationToken)!.GetAttributes()
                .Single(attributeData => attributeData.ApplicationSyntaxReference is not null && attribute.Span.Contains(attributeData.ApplicationSyntaxReference.Span));

            var newTypeDeclaration = typeDeclaration.ReplaceNodes(
                syntaxGenerator.GetMembers(typeDeclaration),
                (originalNode, replacementNode) =>
                {
                    foreach (var attribute in syntaxGenerator.GetAttributes(originalNode))
                    {
                        var attributeType = state.SemanticModel.GetTypeInfo(attribute, cancellationToken);
                        if (attributeType.Type is null)
                        {
                            // In this case, 'attribute' is an attribute list syntax containing a single attribute
                            // syntax. SyntaxGenerator treats this case differently from SemanticModel.
                            attributeType = state.SemanticModel.GetTypeInfo(attribute.ChildNodes().First(), cancellationToken);
                        }

                        if (!SymbolEqualityComparer.Default.Equals(attributeType.Type, state.TraitAttribute))
                            continue;

                        var actualAttributeData = state.SemanticModel.GetDeclaredSymbol(originalNode, cancellationToken)!.GetAttributes()
                            .Single(attributeData => attributeData.ApplicationSyntaxReference is not null && attribute.Span.Contains(attributeData.ApplicationSyntaxReference.Span));

                        if (!expectedAttributeData.ConstructorArguments.SequenceEqual(actualAttributeData.ConstructorArguments))
                            continue;

                        return syntaxGenerator.RemoveNode(originalNode, attribute);
                    }

                    return originalNode;
                });

            newTypeDeclaration = syntaxGenerator.AddAttributes(newTypeDeclaration, attribute.WithAdditionalAnnotations(Formatter.Annotation));

            return state.Document.WithSyntaxRoot(syntaxRoot.ReplaceNode(typeDeclaration, newTypeDeclaration));
        }
    }
}
