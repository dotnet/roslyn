// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.AddDebuggerDisplay
{
    internal abstract class AbstractAddDebuggerDisplayCodeRefactoringProvider<TTypeDeclarationSyntax, TMethodDeclarationSyntax> : CodeRefactoringProvider
        where TTypeDeclarationSyntax : SyntaxNode
        where TMethodDeclarationSyntax : SyntaxNode
    {
        private static readonly SyntaxAnnotation s_trackingAnnotation = new SyntaxAnnotation();

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var type = await context.TryGetRelevantNodeAsync<TTypeDeclarationSyntax>().ConfigureAwait(false);

            var generator = SyntaxGenerator.GetGenerator(context.Document);

            if (type != null)
            {
                if (!DeclaresToStringOverride(generator, type)) return;
            }
            else
            {
                var method = await context.TryGetRelevantNodeAsync<TMethodDeclarationSyntax>().ConfigureAwait(false);
                if (!IsToStringOverride(method)) return;

                type = (TTypeDeclarationSyntax)method.Parent;
            }

            if (generator.GetAttributes(type).Any(IsDebuggerDisplayAttribute))
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

            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Insert attribute
            syntaxRoot = syntaxRoot.ReplaceNode(type, generator.AddAttributes(type, newAttribute).WithAdditionalAnnotations(s_trackingAnnotation));

            // Append namespace import
            var importsService = document.Project.LanguageServices.GetRequiredService<IAddImportsService>();
            var newImport = generator.NamespaceImportDeclaration("System.Diagnostics");
            var contextLocation = syntaxRoot.GetAnnotatedNodes(s_trackingAnnotation).Single();

            if (!importsService.HasExistingImport(compilation, syntaxRoot, contextLocation, newImport))
            {
                var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var placeSystemNamespaceFirst = optionSet.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, document.Project.Language);

                return document.WithSyntaxRoot(importsService.AddImport(
                    compilation,
                    syntaxRoot,
                    contextLocation,
                    newImport,
                    placeSystemNamespaceFirst,
                    cancellationToken));
            }

            return document.WithSyntaxRoot(syntaxRoot);
        }

        protected abstract bool IsDebuggerDisplayAttribute(SyntaxNode attribute);

        protected abstract bool IsToStringOverride(TMethodDeclarationSyntax methodDeclaration);

        protected virtual bool DeclaresToStringOverride(SyntaxGenerator generator, TTypeDeclarationSyntax typeDeclaration)
        {
            return generator.GetMembers(typeDeclaration).OfType<TMethodDeclarationSyntax>().Any(IsToStringOverride);
        }

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
