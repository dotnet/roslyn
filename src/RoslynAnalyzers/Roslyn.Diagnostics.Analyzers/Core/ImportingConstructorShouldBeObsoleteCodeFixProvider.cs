// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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

namespace Roslyn.Diagnostics.Analyzers;

public abstract class AbstractImportingConstructorShouldBeObsoleteCodeFixProvider() : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = [ImportingConstructorShouldBeObsolete.Rule.Id];

    protected abstract bool IsOnPrimaryConstructorTypeDeclaration(SyntaxNode attributeName, [NotNullWhen(true)] out SyntaxNode? typeDeclaration);
    protected abstract SyntaxNode AddMethodTarget(SyntaxNode attributeList);

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
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var obsoleteAttributeSymbol = semanticModel.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemObsoleteAttribute);
        if (obsoleteAttributeSymbol is null)
            return document;

        var attributeName = root.FindNode(sourceSpan, getInnermostNodeForTie: true);

        var generator = SyntaxGenerator.GetGenerator(document);

        var isPrimaryConstructorTypeDeclaration = this.IsOnPrimaryConstructorTypeDeclaration(attributeName, out var typeDeclaration);
        var declaration = isPrimaryConstructorTypeDeclaration
            ? typeDeclaration
            : generator.TryGetContainingDeclaration(attributeName, DeclarationKind.Constructor);
        if (declaration is null)
            return document;

        var obsoleteAttributeList = generator.Attribute(
            generator.TypeExpression(obsoleteAttributeSymbol).WithAddImportsAnnotation(),
            [
                GenerateDescriptionArgument(generator, semanticModel),
                GenerateErrorArgument(generator, allowNamedArgument: document.Project.Language == LanguageNames.CSharp),
            ]);

        obsoleteAttributeList = isPrimaryConstructorTypeDeclaration ? this.AddMethodTarget(obsoleteAttributeList) : obsoleteAttributeList;

        var newDeclaration = generator.AddAttributes(declaration, obsoleteAttributeList);
        return document.WithSyntaxRoot(root.ReplaceNode(declaration, newDeclaration));
    }

    private static async Task<Document> AddDescriptionAndErrorAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var obsoleteAttributeApplication = root.FindNode(sourceSpan, getInnermostNodeForTie: true);

        var generator = SyntaxGenerator.GetGenerator(document);

        var declaration = generator.TryGetContainingDeclaration(obsoleteAttributeApplication, DeclarationKind.Attribute);
        if (declaration is null)
        {
            return document;
        }

        var descriptionArgument = GenerateDescriptionArgument(generator, semanticModel);
        var errorArgument = GenerateErrorArgument(generator, allowNamedArgument: document.Project.Language == LanguageNames.CSharp);
        var newDeclaration = generator.AddAttributeArguments(declaration, [descriptionArgument, errorArgument]);
        return document.WithSyntaxRoot(root.ReplaceNode(declaration, newDeclaration));
    }

    private static async Task<Document> UpdateDescriptionAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var obsoleteAttributeApplication = root.FindNode(sourceSpan, getInnermostNodeForTie: true);

        var generator = SyntaxGenerator.GetGenerator(document);

        var declaration = generator.TryGetContainingDeclaration(obsoleteAttributeApplication, DeclarationKind.Attribute);
        if (declaration is null)
        {
            return document;
        }

        var argumentToReplace = generator.GetAttributeArguments(declaration).ElementAtOrDefault(0);
        if (argumentToReplace is null)
        {
            return document;
        }

        var descriptionArgument = GenerateDescriptionArgument(generator, semanticModel);
        return document.WithSyntaxRoot(root.ReplaceNode(argumentToReplace, descriptionArgument));
    }

    private static async Task<Document> AddErrorAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var obsoleteAttributeApplication = root.FindNode(sourceSpan, getInnermostNodeForTie: true);

        var generator = SyntaxGenerator.GetGenerator(document);

        var declaration = generator.TryGetContainingDeclaration(obsoleteAttributeApplication, DeclarationKind.Attribute);
        if (declaration is null)
        {
            return document;
        }

        var errorArgument = GenerateErrorArgument(generator, allowNamedArgument: document.Project.Language == LanguageNames.CSharp);
        var newDeclaration = generator.AddAttributeArguments(declaration, [errorArgument]);
        return document.WithSyntaxRoot(root.ReplaceNode(declaration, newDeclaration));
    }

    private static async Task<Document> SetErrorToTrueAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var obsoleteAttributeApplication = root.FindNode(sourceSpan, getInnermostNodeForTie: true);

        var generator = SyntaxGenerator.GetGenerator(document);

        var declaration = generator.TryGetContainingDeclaration(obsoleteAttributeApplication, DeclarationKind.Attribute);
        if (declaration is null)
        {
            return document;
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
        SyntaxNode attributeArgument;
        if (semanticModel.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisHostMefMefConstruction, out var mefConstructionType) &&
            mefConstructionType.GetMembers("ImportingConstructorMessage").OfType<IFieldSymbol>().Any())
        {
            attributeArgument = generator.MemberAccessExpression(
                generator.TypeExpression(mefConstructionType).WithAddImportsAnnotation(),
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
