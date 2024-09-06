// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

internal abstract partial class AbstractMetadataAsSourceService : IMetadataAsSourceService
{
    public async Task<Document> AddSourceToAsync(
        Document document,
        Compilation symbolCompilation,
        ISymbol symbol,
        CleanCodeGenerationOptions options,
        CancellationToken cancellationToken)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var newSemanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var rootNamespace = newSemanticModel.GetEnclosingNamespace(position: 0, cancellationToken);
        Contract.ThrowIfNull(rootNamespace);

        var context = new CodeGenerationSolutionContext(
            document.Project.Solution,
            new CodeGenerationContext(
                contextLocation: newSemanticModel.SyntaxTree.GetLocation(new TextSpan()),
                generateMethodBodies: false,
                generateDocumentationComments: true,
                mergeAttributes: false,
                autoInsertionLocation: false),
            new CodeAndImportGenerationOptions()
            {
                GenerationOptions = options.GenerationOptions,
                AddImportOptions = options.CleanupOptions.AddImportOptions
            }.CreateProvider());

        // Add the interface of the symbol to the top of the root namespace
        document = await CodeGenerator.AddNamespaceOrTypeDeclarationAsync(
            context,
            rootNamespace,
            CreateCodeGenerationSymbol(document, symbol),
            cancellationToken).ConfigureAwait(false);

        document = await AddNullableRegionsAsync(document, cancellationToken).ConfigureAwait(false);

        var docCommentFormattingService = document.GetRequiredLanguageService<IDocumentationCommentFormattingService>();
        var docWithDocComments = await ConvertDocCommentsToRegularCommentsAsync(document, docCommentFormattingService, cancellationToken).ConfigureAwait(false);

        var docWithAssemblyInfo = await AddAssemblyInfoRegionAsync(docWithDocComments, symbolCompilation, symbol.GetOriginalUnreducedDefinition(), cancellationToken).ConfigureAwait(false);
        var node = await docWithAssemblyInfo.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var formattedDoc = await Formatter.FormatAsync(
            docWithAssemblyInfo,
            [node.FullSpan],
            options.CleanupOptions.FormattingOptions,
            GetFormattingRules(docWithAssemblyInfo),
            cancellationToken).ConfigureAwait(false);

        var reducers = GetReducers();
        return await Simplifier.ReduceAsync(formattedDoc, reducers, options.CleanupOptions.SimplifierOptions, cancellationToken).ConfigureAwait(false);
    }

    protected abstract Task<Document> AddNullableRegionsAsync(Document document, CancellationToken cancellationToken);

    /// <summary>
    /// provide formatting rules to be used when formatting MAS file
    /// </summary>
    protected abstract ImmutableArray<AbstractFormattingRule> GetFormattingRules(Document document);

    /// <summary>
    /// Prepends a region directive at the top of the document with a name containing
    /// information about the assembly and a comment inside containing the path to the
    /// referenced assembly.  The containing assembly may not have a path on disk, in which case
    /// a string similar to "location unknown" will be placed in the comment inside the region
    /// instead of the path.
    /// </summary>
    /// <param name="document">The document to generate source into</param>
    /// <param name="symbolCompilation">The <see cref="Compilation"/> in which symbol is resolved.</param>
    /// <param name="symbol">The symbol to generate source for</param>
    /// <param name="cancellationToken">To cancel document operations</param>
    /// <returns>The updated document</returns>
    protected abstract Task<Document> AddAssemblyInfoRegionAsync(Document document, Compilation symbolCompilation, ISymbol symbol, CancellationToken cancellationToken);

    protected abstract Task<Document> ConvertDocCommentsToRegularCommentsAsync(Document document, IDocumentationCommentFormattingService docCommentFormattingService, CancellationToken cancellationToken);

    protected abstract ImmutableArray<AbstractReducer> GetReducers();

    private static INamespaceOrTypeSymbol CreateCodeGenerationSymbol(Document document, ISymbol symbol)
    {
        symbol = symbol.GetOriginalUnreducedDefinition();
        var topLevelNamespaceSymbol = symbol.ContainingNamespace;
        var topLevelNamedType = MetadataAsSourceHelpers.GetTopLevelContainingNamedType(symbol);

        var canImplementImplicitly = document.GetRequiredLanguageService<ISemanticFactsService>().SupportsImplicitInterfaceImplementation;
        var docCommentFormattingService = document.GetLanguageService<IDocumentationCommentFormattingService>();

        INamespaceOrTypeSymbol wrappedType = new WrappedNamedTypeSymbol(topLevelNamedType, canImplementImplicitly, docCommentFormattingService);

        return topLevelNamespaceSymbol.IsGlobalNamespace
            ? wrappedType
            : CodeGenerationSymbolFactory.CreateNamespaceSymbol(
                topLevelNamespaceSymbol.ToDisplayString(SymbolDisplayFormats.NameFormat),
                null,
                [wrappedType]);
    }
}
