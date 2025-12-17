// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable warnings

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Diagnostics.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = nameof(ExportedPartsShouldHaveImportingConstructorCodeFixProvider))]
    [Shared]
    [method: ImportingConstructor]
    [method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    public class ExportedPartsShouldHaveImportingConstructorCodeFixProvider() : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(ExportedPartsShouldHaveImportingConstructor.Rule.Id);

        public override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                if (!diagnostic.Properties.TryGetValue(nameof(ExportedPartsShouldHaveImportingConstructor.Scenario), out var scenario))
                {
                    continue;
                }

                string title;
                Func<CancellationToken, Task<Document>> createChangedDocument;
                switch (scenario)
                {
                    case ExportedPartsShouldHaveImportingConstructor.Scenario.ImplicitConstructor:
                        title = RoslynDiagnosticsAnalyzersResources.ExportedPartsShouldHaveImportingConstructorCodeFix_ImplicitConstructor;
                        createChangedDocument = cancellationToken => AddExplicitImportingConstructorAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken);
                        break;

                    case ExportedPartsShouldHaveImportingConstructor.Scenario.NonPublicConstructor:
                        title = RoslynDiagnosticsAnalyzersResources.ExportedPartsShouldHaveImportingConstructorCodeFix_NonPublicConstructor;
                        createChangedDocument = cancellationToken => MakeConstructorPublicAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken);
                        break;

                    case ExportedPartsShouldHaveImportingConstructor.Scenario.MissingAttribute:
                        title = RoslynDiagnosticsAnalyzersResources.ExportedPartsShouldHaveImportingConstructorCodeFix_MissingAttribute;
                        createChangedDocument = cancellationToken => AddImportingConstructorAttributeAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken);
                        break;

                    case ExportedPartsShouldHaveImportingConstructor.Scenario.MultipleConstructors:
                    default:
                        continue;
                }

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title,
                        createChangedDocument,
                        equivalenceKey: scenario),
                    diagnostic);
            }
        }

        private static async Task<Document> AddExplicitImportingConstructorAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var exportAttribute = root.FindNode(sourceSpan, getInnermostNodeForTie: true);
            var exportAttributeSymbol = semanticModel.GetSymbolInfo(exportAttribute, cancellationToken).Symbol?.ContainingType;
            INamedTypeSymbol? importingConstructorAttributeSymbol = null;
            while (exportAttributeSymbol is object)
            {
                importingConstructorAttributeSymbol = exportAttributeSymbol.ContainingNamespace?.GetTypeMembers(nameof(ImportingConstructorAttribute)).FirstOrDefault();
                if (importingConstructorAttributeSymbol is object)
                {
                    break;
                }

                exportAttributeSymbol = exportAttributeSymbol.BaseType;
            }

            if (importingConstructorAttributeSymbol is null)
            {
                return document;
            }

            var generator = SyntaxGenerator.GetGenerator(document);

            var declaration = generator.TryGetContainingDeclaration(exportAttribute, DeclarationKind.Class);
            if (declaration is null)
            {
                return document;
            }

            var importingConstructor = generator.ConstructorDeclaration(
                containingTypeName: generator.GetName(declaration),
                parameters: Enumerable.Empty<SyntaxNode>(),
                Accessibility.Public,
                DeclarationModifiers.None,
                baseConstructorArguments: null,
                statements: Enumerable.Empty<SyntaxNode>());
            importingConstructor = generator.AddAttributes(importingConstructor, generator.Attribute(generator.TypeExpression(importingConstructorAttributeSymbol).WithAddImportsAnnotation()));

            var index = 0;
            var existingMembers = generator.GetMembers(declaration);
            while (index < existingMembers.Count)
            {
                switch (generator.GetDeclarationKind(existingMembers[index]))
                {
                    case DeclarationKind.Field:
                        index++;
                        continue;

                    default:
                        break;
                }

                break;
            }

            var newDeclaration = generator.InsertMembers(declaration, index, importingConstructor);
            return document.WithSyntaxRoot(root.ReplaceNode(declaration, newDeclaration));
        }

        private static async Task<Document> MakeConstructorPublicAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var importingConstructorAttribute = root.FindNode(sourceSpan, getInnermostNodeForTie: true);

            var generator = SyntaxGenerator.GetGenerator(document);

            var declaration = generator.TryGetContainingDeclaration(importingConstructorAttribute, DeclarationKind.Constructor);
            if (declaration is null)
            {
                return document;
            }

            var newDeclaration = generator.WithAccessibility(declaration, Accessibility.Public);
            return document.WithSyntaxRoot(root.ReplaceNode(declaration, newDeclaration));
        }

        private static async Task<Document> AddImportingConstructorAttributeAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var constructor = root.FindNode(sourceSpan, getInnermostNodeForTie: true);

            var generator = SyntaxGenerator.GetGenerator(document);

            var declaration = generator.TryGetContainingDeclaration(constructor, DeclarationKind.Constructor);
            if (declaration is null)
            {
                return document;
            }

            var exportedType = semanticModel.GetDeclaredSymbol(declaration, cancellationToken)?.ContainingType;
            if (exportedType is null)
            {
                return document;
            }

            INamedTypeSymbol? importingConstructorAttributeSymbol = null;
            foreach (var attributeData in exportedType.GetAttributes())
            {
                INamedTypeSymbol? exportAttributeSymbol = null;
                foreach (var attributeClass in attributeData.AttributeClass.GetBaseTypesAndThis())
                {
                    if (attributeClass.Name == nameof(ExportAttribute))
                    {
                        exportAttributeSymbol = (INamedTypeSymbol)attributeClass;
                        break;
                    }
                }

                if (exportAttributeSymbol is null)
                {
                    continue;
                }

                importingConstructorAttributeSymbol = exportAttributeSymbol.ContainingNamespace.GetTypeMembers(nameof(ImportingConstructorAttribute)).FirstOrDefault();
                if (importingConstructorAttributeSymbol is object)
                {
                    break;
                }
            }

            var newDeclaration = generator.AddAttributes(declaration, generator.Attribute(generator.TypeExpression(importingConstructorAttributeSymbol).WithAddImportsAnnotation()));
            return document.WithSyntaxRoot(root.ReplaceNode(declaration, newDeclaration));
        }
    }
}
