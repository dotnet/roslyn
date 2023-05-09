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
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UsePrimaryConstructor
{
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
                context.RegisterCodeFix(CodeAction.Create(
                    CSharpAnalyzersResources.Use_primary_constructor,
                    cancellationToken => UsePrimaryConstructorAsync(document, constructorDeclaration, properties, removeMembers: false, cancellationToken)),
                    nameof(CSharpAnalyzersResources.Use_primary_constructor));

                if (diagnostic.Properties.Count > 0)
                {
                    context.RegisterCodeFix(CodeAction.Create(
                        CSharpCodeFixesResources.Use_primary_constructor_and_remove_members,
                        cancellationToken => UsePrimaryConstructorAsync(document, constructorDeclaration, properties, removeMembers: true, cancellationToken)),
                        nameof(CSharpCodeFixesResources.Use_primary_constructor_and_remove_members));
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
            var typeDeclaration = (TypeDeclarationSyntax)constructorDeclaration.GetRequiredParent();

            var solutionEditor = new SolutionEditor(document.Project.Solution);
            var constructorDocumentEditor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);

            // First, remove the constructor itself.
            constructorDocumentEditor.RemoveNode(constructorDeclaration);

            // Then move its parameter list to the type declaration.
            constructorDocumentEditor.ReplaceNode(
                typeDeclaration,
                (current, generator) =>
                {
                    var currentTypeDeclaration = (TypeDeclarationSyntax)current;

                    var typeParameterList = currentTypeDeclaration.TypeParameterList;
                    var triviaAfterName = typeParameterList != null
                        ? typeParameterList.GetTrailingTrivia()
                        : currentTypeDeclaration.Identifier.GetAllTrailingTrivia();

                    return currentTypeDeclaration
                        .WithIdentifier(typeParameterList != null ? currentTypeDeclaration.Identifier : currentTypeDeclaration.Identifier.WithoutTrailingTrivia())
                        .WithTypeParameterList(typeParameterList?.WithoutTrailingTrivia())
                        .WithParameterList(constructorDeclaration.ParameterList.WithTrailingTrivia(triviaAfterName));
                });

            // TODO: reconcile doc comments.
            // 1. If we are not removing members and the constructor had parameter doc comments, we likely want to move
            //    those to the type declaration.
            // 2. if we are removing members and the members had doc comments:
            //      2a. if the constructor had parameter doc comments, choose which to win (probably parameter)
            //      2b. if the constructor did not have parameter doc comments, take the member doc comments and convert
            //          to parameter comments.
        }
    }
}
