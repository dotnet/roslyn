// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Diagnostics.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = nameof(TestExportsShouldNotBeDiscoverableCodeFixProvider))]
    [Shared]
    public class TestExportsShouldNotBeDiscoverableCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(TestExportsShouldNotBeDiscoverable.Rule.Id);

        public override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        RoslynDiagnosticsAnalyzersResources.TestExportsShouldNotBeDiscoverableCodeFix,
                        cancellationToken => AddPartNotDiscoverableAttributeAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
                        equivalenceKey: nameof(TestExportsShouldNotBeDiscoverable)),
                    diagnostic);
            }

            return Task.CompletedTask;
        }

        private async Task<Document> AddPartNotDiscoverableAttributeAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var exportingAttribute = root.FindNode(sourceSpan, getInnermostNodeForTie: true);

            var generator = SyntaxGenerator.GetGenerator(document);

            var declaration = exportingAttribute;
            var declarationKind = generator.GetDeclarationKind(declaration);
            while (declarationKind != DeclarationKind.Class)
            {
                declaration = generator.GetDeclaration(declaration.Parent);
                if (declaration is null)
                {
                    return document;
                }

                declarationKind = generator.GetDeclarationKind(declaration);
            }

            var exportedType = semanticModel.GetDeclaredSymbol(declaration, cancellationToken);
            if (exportedType is null)
            {
                return document;
            }

            INamedTypeSymbol? partNotDiscoverableAttributeSymbol = null;
            foreach (var attributeData in exportedType.GetAttributes())
            {
                INamedTypeSymbol? exportAttributeSymbol = null;
                foreach (var attributeClass in attributeData.AttributeClass.GetBaseTypesAndThis())
                {
                    if (attributeClass.Name == nameof(ExportAttribute))
                    {
                        exportAttributeSymbol = attributeClass;
                        break;
                    }
                }

                if (exportAttributeSymbol is null)
                {
                    continue;
                }

                partNotDiscoverableAttributeSymbol = exportAttributeSymbol.ContainingNamespace.GetTypeMembers(nameof(PartNotDiscoverableAttribute)).FirstOrDefault();
                if (partNotDiscoverableAttributeSymbol is object)
                {
                    break;
                }
            }

            if (partNotDiscoverableAttributeSymbol is null)
            {
                // This can only get hit if ExportAttribute is available but PartNotDiscoverableAttribute is missing.
                return document;
            }

            var newDeclaration = generator.AddAttributes(declaration, generator.Attribute(generator.TypeExpression(partNotDiscoverableAttributeSymbol)));
            return document.WithSyntaxRoot(root.ReplaceNode(declaration, newDeclaration));
        }
    }
}
