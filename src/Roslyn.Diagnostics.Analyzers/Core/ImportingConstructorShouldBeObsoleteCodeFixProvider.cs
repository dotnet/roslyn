// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Diagnostics.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = nameof(ImportingConstructorShouldBeObsoleteCodeFixProvider))]
    [Shared]
    public class ImportingConstructorShouldBeObsoleteCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ImportingConstructorShouldBeObsolete.Rule.Id);

        public override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                if (!diagnostic.Properties.TryGetValue(nameof(ImportingConstructorShouldBeObsolete.Scenario), out var scenario))
                {
                    continue;
                }

                string title;
                Func<CancellationToken, Task<Document>> createChangedDocument;
                switch (scenario)
                {
                    case ImportingConstructorShouldBeObsolete.Scenario.MissingAttribute:
                        title = RoslynDiagnosticsAnalyzersResources.ImportingConstructorShouldBeObsoleteCodeFix_MissingAttribute;
                        createChangedDocument = cancellationToken => AddObsoleteAttributeAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken);
                        break;

                    case ImportingConstructorShouldBeObsolete.Scenario.MissingDescription:
                        title = RoslynDiagnosticsAnalyzersResources.ImportingConstructorShouldBeObsoleteCodeFix_MissingDescription;
                        createChangedDocument = cancellationToken => AddDescriptionAndErrorAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken);
                        break;

                    case ImportingConstructorShouldBeObsolete.Scenario.IncorrectDescription:
                        title = RoslynDiagnosticsAnalyzersResources.ImportingConstructorShouldBeObsoleteCodeFix_IncorrectDescription;
                        createChangedDocument = cancellationToken => UpdateDescriptionAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken);
                        break;

                    case ImportingConstructorShouldBeObsolete.Scenario.MissingError:
                        title = RoslynDiagnosticsAnalyzersResources.ImportingConstructorShouldBeObsoleteCodeFix_MissingError;
                        createChangedDocument = cancellationToken => AddErrorAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken);
                        break;

                    case ImportingConstructorShouldBeObsolete.Scenario.ErrorSetToFalse:
                        title = RoslynDiagnosticsAnalyzersResources.ImportingConstructorShouldBeObsoleteCodeFix_ErrorSetToFalse;
                        createChangedDocument = cancellationToken => SetErrorToTrueAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken);
                        break;

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

            return Task.CompletedTask;
        }

        private async Task<Document> AddObsoleteAttributeAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var obsoleteAttributeSymbol = semanticModel.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemObsoleteAttribute);
            if (obsoleteAttributeSymbol is null)
            {
                return document;
            }

            var constructor = root.FindNode(sourceSpan, getInnermostNodeForTie: true);

            var generator = SyntaxGenerator.GetGenerator(document);

            var declaration = constructor;
            var declarationKind = generator.GetDeclarationKind(declaration);
            while (declarationKind != DeclarationKind.Constructor)
            {
                declaration = generator.GetDeclaration(declaration.Parent);
                if (declaration is null)
                {
                    return document;
                }

                declarationKind = generator.GetDeclarationKind(declaration);
            }

            var obsoleteAttribute = generator.Attribute(
                generator.TypeExpression(obsoleteAttributeSymbol),
                new[]
                {
                    GenerateDescriptionArgument(generator, semanticModel),
                    GenerateErrorArgument(generator, allowNamedArgument: document.Project.Language == LanguageNames.CSharp),
                });

            var newDeclaration = generator.AddAttributes(declaration, obsoleteAttribute);
            return document.WithSyntaxRoot(root.ReplaceNode(declaration, newDeclaration));
        }

        private async Task<Document> AddDescriptionAndErrorAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var obsoleteAttributeApplication = root.FindNode(sourceSpan, getInnermostNodeForTie: true);

            var generator = SyntaxGenerator.GetGenerator(document);

            var declaration = obsoleteAttributeApplication;
            var declarationKind = generator.GetDeclarationKind(declaration);
            while (declarationKind != DeclarationKind.Attribute)
            {
                declaration = generator.GetDeclaration(declaration.Parent);
                if (declaration is null)
                {
                    return document;
                }

                declarationKind = generator.GetDeclarationKind(declaration);
            }

            var descriptionArgument = GenerateDescriptionArgument(generator, semanticModel);
            var errorArgument = GenerateErrorArgument(generator, allowNamedArgument: document.Project.Language == LanguageNames.CSharp);
            var newDeclaration = generator.AddAttributeArguments(declaration, new[] { descriptionArgument, errorArgument });
            return document.WithSyntaxRoot(root.ReplaceNode(declaration, newDeclaration));
        }

        private async Task<Document> UpdateDescriptionAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var obsoleteAttributeApplication = root.FindNode(sourceSpan, getInnermostNodeForTie: true);

            var generator = SyntaxGenerator.GetGenerator(document);

            var declaration = obsoleteAttributeApplication;
            var declarationKind = generator.GetDeclarationKind(declaration);
            while (declarationKind != DeclarationKind.Attribute)
            {
                declaration = generator.GetDeclaration(declaration.Parent);
                if (declaration is null)
                {
                    return document;
                }

                declarationKind = generator.GetDeclarationKind(declaration);
            }

            var argumentToReplace = generator.GetAttributeArguments(declaration).ElementAtOrDefault(0);
            if (argumentToReplace is null)
            {
                return document;
            }

            var descriptionArgument = GenerateDescriptionArgument(generator, semanticModel);
            return document.WithSyntaxRoot(root.ReplaceNode(argumentToReplace, descriptionArgument));
        }

        private async Task<Document> AddErrorAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var obsoleteAttributeApplication = root.FindNode(sourceSpan, getInnermostNodeForTie: true);

            var generator = SyntaxGenerator.GetGenerator(document);

            var declaration = obsoleteAttributeApplication;
            var declarationKind = generator.GetDeclarationKind(declaration);
            while (declarationKind != DeclarationKind.Attribute)
            {
                declaration = generator.GetDeclaration(declaration.Parent);
                if (declaration is null)
                {
                    return document;
                }

                declarationKind = generator.GetDeclarationKind(declaration);
            }

            var errorArgument = GenerateErrorArgument(generator, allowNamedArgument: document.Project.Language == LanguageNames.CSharp);
            var newDeclaration = generator.AddAttributeArguments(declaration, new[] { errorArgument });
            return document.WithSyntaxRoot(root.ReplaceNode(declaration, newDeclaration));
        }

        private async Task<Document> SetErrorToTrueAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var obsoleteAttributeApplication = root.FindNode(sourceSpan, getInnermostNodeForTie: true);

            var generator = SyntaxGenerator.GetGenerator(document);

            var declaration = obsoleteAttributeApplication;
            var declarationKind = generator.GetDeclarationKind(declaration);
            while (declarationKind != DeclarationKind.Attribute)
            {
                declaration = generator.GetDeclaration(declaration.Parent);
                if (declaration is null)
                {
                    return document;
                }

                declarationKind = generator.GetDeclarationKind(declaration);
            }

            var argumentToReplace = generator.GetAttributeArguments(declaration).ElementAtOrDefault(1);
            if (argumentToReplace is null)
            {
                return document;
            }

            var errorArgument = GenerateErrorArgument(generator, allowNamedArgument: document.Project.Language == LanguageNames.CSharp);
            return document.WithSyntaxRoot(root.ReplaceNode(argumentToReplace, errorArgument));
        }

        private static SyntaxNode GenerateDescriptionArgument(SyntaxGenerator generator, SemanticModel semanticModel)
        {
            var mefConstructionType = semanticModel.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisHostMefMefConstruction);
            var knownConstant = mefConstructionType?.GetMembers("ImportingConstructorMessage").OfType<IFieldSymbol>().Any();

            SyntaxNode attributeArgument;
            if (knownConstant is object)
            {
                attributeArgument = generator.MemberAccessExpression(
                    generator.TypeExpressionForStaticMemberAccess(mefConstructionType),
                    generator.IdentifierName("ImportingConstructorMessage"));
            }
            else
            {
                attributeArgument = generator.LiteralExpression("This exported object must be obtained through the MEF export provider.");
            }

            return generator.AttributeArgument(attributeArgument);
        }

        private static SyntaxNode GenerateErrorArgument(SyntaxGenerator generator, bool allowNamedArgument)
        {
            if (allowNamedArgument)
            {
                var argument = generator.Argument("error", RefKind.None, generator.TrueLiteralExpression());
                var attribute = generator.Attribute("ignored", argument);
                return generator.GetAttributeArguments(attribute)[0];
            }
            else
            {
                return generator.AttributeArgument(generator.TrueLiteralExpression());
            }
        }
    }
}
