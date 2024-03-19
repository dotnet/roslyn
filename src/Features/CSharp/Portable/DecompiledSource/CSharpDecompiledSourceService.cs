// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.DecompiledSource;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.DecompiledSource;

[ExportLanguageService(typeof(IDecompiledSourceService), LanguageNames.CSharp), Shared]
internal class CSharpDecompiledSourceService : IDecompiledSourceService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpDecompiledSourceService()
    {
    }

    public async Task<Document?> AddSourceToAsync(Document document, Compilation symbolCompilation, ISymbol symbol, MetadataReference? metadataReference, string? assemblyLocation, SyntaxFormattingOptions formattingOptions, CancellationToken cancellationToken)
    {
        // Get the name of the type the symbol is in
        var containingOrThis = symbol.GetContainingTypeOrThis();
        var fullName = GetFullReflectionName(containingOrThis);

        // Decompile
        var decompilationService = document.GetRequiredLanguageService<IDecompilationService>();
        var decompiledDocument = decompilationService.PerformDecompilation(document, fullName, symbolCompilation, metadataReference, assemblyLocation);

        if (decompiledDocument is null)
            return null;

        document = decompiledDocument;

        document = await AddAssemblyInfoRegionAsync(document, symbol, decompilationService, cancellationToken).ConfigureAwait(false);

        // Convert XML doc comments to regular comments, just like MAS
        var docCommentFormattingService = document.GetRequiredLanguageService<IDocumentationCommentFormattingService>();
        document = await ConvertDocCommentsToRegularCommentsAsync(document, docCommentFormattingService, cancellationToken).ConfigureAwait(false);

        return await FormatDocumentAsync(document, formattingOptions, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<Document> FormatDocumentAsync(Document document, SyntaxFormattingOptions options, CancellationToken cancellationToken)
    {
        var node = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        // Apply formatting rules
        var formattedDoc = await Formatter.FormatAsync(
             document,
             SpecializedCollections.SingletonEnumerable(node.FullSpan),
             options,
             CSharpDecompiledSourceFormattingRule.Instance.Concat(Formatter.GetDefaultFormattingRules(document)),
             cancellationToken).ConfigureAwait(false);

        return formattedDoc;
    }

    private static async Task<Document> AddAssemblyInfoRegionAsync(Document document, ISymbol symbol, IDecompilationService decompilationService, CancellationToken cancellationToken)
    {
        var assemblyInfo = MetadataAsSourceHelpers.GetAssemblyInfo(symbol.ContainingAssembly);
        var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
        var assemblyPath = MetadataAsSourceHelpers.GetAssemblyDisplay(compilation, symbol.ContainingAssembly);

        var regionTrivia = SyntaxFactory.RegionDirectiveTrivia(true)
            .WithTrailingTrivia(new[] { SyntaxFactory.Space, SyntaxFactory.PreprocessingMessage(assemblyInfo) });

        var decompilerVersion = decompilationService.GetDecompilerVersion();

        var oldRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = oldRoot.WithLeadingTrivia(new[]
            {
                SyntaxFactory.Trivia(regionTrivia),
                SyntaxFactory.CarriageReturnLineFeed,
                SyntaxFactory.Comment("// " + assemblyPath),
                SyntaxFactory.CarriageReturnLineFeed,
                SyntaxFactory.Comment($"// Decompiled with ICSharpCode.Decompiler {decompilerVersion.FileVersion}"),
                SyntaxFactory.CarriageReturnLineFeed,
                SyntaxFactory.Trivia(SyntaxFactory.EndRegionDirectiveTrivia(true)),
                SyntaxFactory.CarriageReturnLineFeed,
                SyntaxFactory.CarriageReturnLineFeed
            });

        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<Document> ConvertDocCommentsToRegularCommentsAsync(Document document, IDocumentationCommentFormattingService docCommentFormattingService, CancellationToken cancellationToken)
    {
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var newSyntaxRoot = DocCommentConverter.ConvertToRegularComments(syntaxRoot, docCommentFormattingService, cancellationToken);

        return document.WithSyntaxRoot(newSyntaxRoot);
    }

    private static string GetFullReflectionName(INamedTypeSymbol? containingType)
    {
        var containingTypeStack = new Stack<string>();
        var containingNamespaceStack = new Stack<string>();

        for (INamespaceOrTypeSymbol? symbol = containingType;
            symbol is not null and not INamespaceSymbol { IsGlobalNamespace: true };
            symbol = (INamespaceOrTypeSymbol?)symbol.ContainingType ?? symbol.ContainingNamespace)
        {
            if (symbol.ContainingType is not null)
                containingTypeStack.Push(symbol.MetadataName);
            else
                containingNamespaceStack.Push(symbol.MetadataName);
        }

        var result = string.Join(".", containingNamespaceStack);
        if (containingTypeStack.Any())
        {
            result += "+" + string.Join("+", containingTypeStack);
        }

        return result;
    }
}
