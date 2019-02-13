﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal abstract partial class AbstractMetadataAsSourceService : IMetadataAsSourceService
    {
        private readonly ICodeGenerationService _codeGenerationService;

        protected AbstractMetadataAsSourceService(ICodeGenerationService codeGenerationService)
        {
            _codeGenerationService = codeGenerationService;
        }

        public async Task<Document> AddSourceToAsync(Document document, ISymbol symbol, CancellationToken cancellationToken)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var newSemanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var rootNamespace = newSemanticModel.GetEnclosingNamespace(0, cancellationToken);

            // Add the interface of the symbol to the top of the root namespace
            document = await CodeGenerator.AddNamespaceOrTypeDeclarationAsync(
                document.Project.Solution,
                rootNamespace,
                CreateCodeGenerationSymbol(document, symbol),
                CreateCodeGenerationOptions(newSemanticModel.SyntaxTree.GetLocation(new TextSpan()), symbol),
                cancellationToken).ConfigureAwait(false);

            var docCommentFormattingService = document.GetLanguageService<IDocumentationCommentFormattingService>();
            var docWithDocComments = await ConvertDocCommentsToRegularComments(document, docCommentFormattingService, cancellationToken).ConfigureAwait(false);

            var docWithAssemblyInfo = await AddAssemblyInfoRegionAsync(docWithDocComments, symbol.GetOriginalUnreducedDefinition(), cancellationToken).ConfigureAwait(false);
            var node = await docWithAssemblyInfo.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var formattedDoc = await Formatter.FormatAsync(
                docWithAssemblyInfo, SpecializedCollections.SingletonEnumerable(node.FullSpan), options: null, rules: GetFormattingRules(docWithAssemblyInfo), cancellationToken: cancellationToken).ConfigureAwait(false);

            var reducers = this.GetReducers();
            return await Simplifier.ReduceAsync(formattedDoc, reducers, null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// provide formatting rules to be used when formatting MAS file
        /// </summary>
        protected abstract IEnumerable<AbstractFormattingRule> GetFormattingRules(Document document);

        /// <summary>
        /// Prepends a region directive at the top of the document with a name containing
        /// information about the assembly and a comment inside containing the path to the
        /// referenced assembly.  The containing assembly may not have a path on disk, in which case
        /// a string similar to "location unknown" will be placed in the comment inside the region
        /// instead of the path.
        /// </summary>
        protected abstract Task<Document> AddAssemblyInfoRegionAsync(Document document, ISymbol symbol, CancellationToken cancellationToken);

        protected abstract Task<Document> ConvertDocCommentsToRegularComments(Document document, IDocumentationCommentFormattingService docCommentFormattingService, CancellationToken cancellationToken);

        protected abstract ImmutableArray<AbstractReducer> GetReducers();

        private static INamespaceOrTypeSymbol CreateCodeGenerationSymbol(Document document, ISymbol symbol)
        {
            symbol = symbol.GetOriginalUnreducedDefinition();
            var topLevelNamespaceSymbol = symbol.ContainingNamespace;
            var topLevelNamedType = MetadataAsSourceHelpers.GetTopLevelContainingNamedType(symbol);

            var canImplementImplicitly = document.GetLanguageService<ISemanticFactsService>().SupportsImplicitInterfaceImplementation;
            var docCommentFormattingService = document.GetLanguageService<IDocumentationCommentFormattingService>();

            INamespaceOrTypeSymbol wrappedType = new WrappedNamedTypeSymbol(topLevelNamedType, canImplementImplicitly, docCommentFormattingService);

            return topLevelNamespaceSymbol.IsGlobalNamespace
                ? wrappedType
                : CodeGenerationSymbolFactory.CreateNamespaceSymbol(
                    topLevelNamespaceSymbol.ToDisplayString(SymbolDisplayFormats.NameFormat),
                    null,
                    new[] { wrappedType });
        }

        private static CodeGenerationOptions CreateCodeGenerationOptions(Location contextLocation, ISymbol symbol)
        {
            return new CodeGenerationOptions(
                contextLocation: contextLocation,
                generateMethodBodies: false,
                generateDocumentationComments: true,
                mergeAttributes: false,
                autoInsertionLocation: false);
        }
    }
}
