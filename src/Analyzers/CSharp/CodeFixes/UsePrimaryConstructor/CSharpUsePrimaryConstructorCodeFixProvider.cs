// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UsePrimaryConstructor
{
    using static SyntaxFactory;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UsePrimaryConstructor), Shared]
    internal class CSharpUsePrimaryConstructorCodeFixProvider : CodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpUsePrimaryConstructorCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UsePrimaryConstructorDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;

            foreach (var diagnostic in context.Diagnostics)
            {
                if (diagnostic.Location.FindNode(cancellationToken) is not ConstructorDeclarationSyntax constructorDeclaration)
                    continue;

                var properties = diagnostic.Properties;
                var additionalNodes = diagnostic.AdditionalLocations;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        CSharpAnalyzersResources.Use_primary_constructor,
                        cancellationToken => UsePrimaryConstructorAsync(document, constructorDeclaration, properties, removeMembers: false, cancellationToken),
                        nameof(CSharpAnalyzersResources.Use_primary_constructor)),
                    diagnostic);

                if (diagnostic.Properties.Count > 0)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            CSharpCodeFixesResources.Use_primary_constructor_and_remove_members,
                            cancellationToken => UsePrimaryConstructorAsync(document, constructorDeclaration, properties, removeMembers: true, cancellationToken),
                            nameof(CSharpCodeFixesResources.Use_primary_constructor_and_remove_members)),
                        diagnostic);
                }
            }

            return Task.CompletedTask;
        }

        private async Task<Solution> UsePrimaryConstructorAsync(
            Document document,
            ConstructorDeclarationSyntax constructorDeclaration,
            ImmutableDictionary<string, string?> properties,
            bool removeMembers,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var typeDeclaration = (TypeDeclarationSyntax)constructorDeclaration.GetRequiredParent();
            var namedType = semanticModel.GetRequiredDeclaredSymbol(typeDeclaration, cancellationToken);

            var solution = document.Project.Solution;
            var solutionEditor = new SolutionEditor(solution);

            using var _ = PooledHashSet<ISymbol>.GetInstance(out var removedMembers);

            // If we're removing members, first go through and update all references to that member to use the parameter name
            if (removeMembers)
            {
                Contract.ThrowIfTrue(properties.IsEmpty);
                foreach (var (memberName, parameterName) in properties)
                {
                    Contract.ThrowIfNull(parameterName);

                    // Validated by analyzer.
                    var member = namedType.GetMembers(memberName).Where(m => m is IFieldSymbol or IPropertySymbol).First();

                }
            }

            // Now, remove the constructor itself.
            var constructorDocumentEditor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);
            constructorDocumentEditor.RemoveNode(constructorDeclaration);

            // Then move its parameter list to the type declaration.
            constructorDocumentEditor.ReplaceNode(
                typeDeclaration,
                (current, generator) =>
                {
                    var currentTypeDeclaration = (TypeDeclarationSyntax)current;

                    // Move the whitespace that is current after the name (or type args) to after the parameter list.

                    var typeParameterList = currentTypeDeclaration.TypeParameterList;
                    var triviaAfterName = typeParameterList != null
                        ? typeParameterList.GetTrailingTrivia()
                        : currentTypeDeclaration.Identifier.GetAllTrailingTrivia();

                    return currentTypeDeclaration
                        .WithIdentifier(typeParameterList != null ? currentTypeDeclaration.Identifier : currentTypeDeclaration.Identifier.WithoutTrailingTrivia())
                        .WithTypeParameterList(typeParameterList?.WithoutTrailingTrivia())
                        .WithParameterList(constructorDeclaration.ParameterList
                            .WithoutLeadingTrivia()
                            .WithTrailingTrivia(triviaAfterName)
                            .WithAdditionalAnnotations(Formatter.Annotation));
                });

            // Now, take all the assignments in the constructor, and place them directly on the field/property initializers.
            if (constructorDeclaration.ExpressionBody is not null)
            {
                // Validated by analyzer.
                await ProcessAssignmentAsync((AssignmentExpressionSyntax)constructorDeclaration.ExpressionBody.Expression).ConfigureAwait(false);
            }
            else
            {
                // Validated by analyzer.
                Contract.ThrowIfNull(constructorDeclaration.Body);
                foreach (var statement in constructorDeclaration.Body.Statements)
                    await ProcessAssignmentAsync((AssignmentExpressionSyntax)((ExpressionStatementSyntax)statement).Expression).ConfigureAwait(false);
            }

            // TODO: reconcile doc comments.
            // 1. If we are not removing members and the constructor had parameter doc comments, we likely want to move
            //    those to the type declaration.
            // 2. if we are removing members and the members had doc comments:
            //      2a. if the constructor had parameter doc comments, choose which to win (probably parameter)
            //      2b. if the constructor did not have parameter doc comments, take the member doc comments and convert
            //          to parameter comments.

            return solutionEditor.GetChangedSolution();

            async ValueTask ProcessAssignmentAsync(AssignmentExpressionSyntax assignmentExpression)
            {
                var member = semanticModel.GetSymbolInfo(assignmentExpression.Left, cancellationToken).GetAnySymbol()?.OriginalDefinition;

                // Validated by analyzer.
                Contract.ThrowIfFalse(member is IFieldSymbol or IPropertySymbol);

                // no point updating the member if it's going to be removed.
                if (removedMembers.Contains(member))
                    return;

                var declaration = member.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
                var declarationDocument = solution.GetRequiredDocument(declaration.SyntaxTree);
                var declarationDocumentEditor = await solutionEditor.GetDocumentEditorAsync(declarationDocument.Id, cancellationToken).ConfigureAwait(false);

                declarationDocumentEditor.ReplaceNode(
                    declaration,
                    UpdateDeclaration(declaration, assignmentExpression).WithAdditionalAnnotations(Formatter.Annotation));
            }

            SyntaxNode UpdateDeclaration(SyntaxNode declaration, AssignmentExpressionSyntax assignmentExpression)
            {
                var newLeadingTrivia = assignmentExpression.Left.GetTrailingTrivia();
                var initializer = EqualsValueClause(assignmentExpression.OperatorToken, assignmentExpression.Right);
                if (declaration is VariableDeclaratorSyntax declarator)
                {
                    return declarator
                        .WithIdentifier(declarator.Identifier.WithTrailingTrivia(newLeadingTrivia))
                        .WithInitializer(initializer);
                }
                else if (declaration is PropertyDeclarationSyntax propertyDeclaration)
                {
                    return propertyDeclaration
                        .WithInitializer(initializer.WithLeadingTrivia(newLeadingTrivia));
                }
                else
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }
        }
    }
}
