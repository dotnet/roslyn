// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.AddDebuggerDisplay
{
    internal abstract class AbstractAddDebuggerDisplayCodeRefactoringProvider<TTypeDeclarationSyntax> : CodeRefactoringProvider
        where TTypeDeclarationSyntax : SyntaxNode
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var type = await context.TryGetRelevantNodeAsync<TTypeDeclarationSyntax>().ConfigureAwait(false);
            if (type is null)
            {
                return;
            }

            var syntaxGenerator = SyntaxGenerator.GetGenerator(context.Document);

            if (syntaxGenerator.GetAttributes(type).Any(IsDebuggerDisplayAttribute) || !DeclaresToStringOverride(type))
            {
                return;
            }

            context.RegisterRefactoring(new MyCodeAction(
                FeaturesResources.Add_DebuggerDisplay,
                cancellationToken => ApplyAsync(context.Document, type, cancellationToken)));
        }

        private async Task<Document> ApplyAsync(Document document, TTypeDeclarationSyntax type, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);

            var newAttribute = generator
                .Attribute("System.Diagnostics.DebuggerDisplayAttribute", generator.LiteralExpression("{ToString(),nq}"))
                .WithAdditionalAnnotations(Simplifier.Annotation);

            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Insert attribute
            syntaxRoot = syntaxRoot.ReplaceNode(type, generator.AddAttributes(type, newAttribute));

            // Append namespace import
            syntaxRoot = generator.AddNamespaceImports(syntaxRoot, generator.NamespaceImportDeclaration("System.Diagnostics"));

            // Organize imports
            var organizer = document.Project.LanguageServices.GetService<IOrganizeImportsService>();

            return await organizer.OrganizeImportsAsync(document.WithSyntaxRoot(syntaxRoot), cancellationToken).ConfigureAwait(false);
        }

        protected abstract bool IsDebuggerDisplayAttribute(SyntaxNode attribute);

        protected abstract bool DeclaresToStringOverride(TTypeDeclarationSyntax typeDeclaration);

        protected bool IsDebuggerDisplayAttributeIdentifier(SyntaxToken name)
        {
            return name.ValueText == "DebuggerDisplay" || name.ValueText == "DebuggerDisplayAttribute";
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
