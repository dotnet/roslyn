// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Transforms;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.DecompiledSource;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.DecompiledSource
{
    [ExportLanguageService(typeof(IDecompiledSourceService), LanguageNames.CSharp), Shared]
    internal class CSharpDecompiledSourceService : IDecompiledSourceService
    {
        private static readonly FileVersionInfo s_decompilerVersion = FileVersionInfo.GetVersionInfo(typeof(CSharpDecompiler).Assembly.Location);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpDecompiledSourceService()
        {
        }

        public async Task<Document> AddSourceToAsync(Document document, Compilation symbolCompilation, ISymbol symbol, MetadataReference metadataReference, string assemblyLocation, CancellationToken cancellationToken)
        {
            // Get the name of the type the symbol is in
            var containingOrThis = symbol.GetContainingTypeOrThis();
            var fullName = GetFullReflectionName(containingOrThis);

            // Decompile
            document = PerformDecompilation(document, fullName, symbolCompilation, metadataReference, assemblyLocation);

            document = await AddAssemblyInfoRegionAsync(document, symbol, cancellationToken).ConfigureAwait(false);

            // Convert XML doc comments to regular comments, just like MAS
            var docCommentFormattingService = document.GetRequiredLanguageService<IDocumentationCommentFormattingService>();
            document = await ConvertDocCommentsToRegularCommentsAsync(document, docCommentFormattingService, cancellationToken).ConfigureAwait(false);

            return await FormatDocumentAsync(document, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<Document> FormatDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var node = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var options = await SyntaxFormattingOptions.FromDocumentAsync(document, cancellationToken).ConfigureAwait(false);

            // Apply formatting rules
            var formattedDoc = await Formatter.FormatAsync(
                 document,
                 SpecializedCollections.SingletonEnumerable(node.FullSpan),
                 options,
                 CSharpDecompiledSourceFormattingRule.Instance.Concat(Formatter.GetDefaultFormattingRules(document)),
                 cancellationToken).ConfigureAwait(false);

            return formattedDoc;
        }

        private static Document PerformDecompilation(Document document, string fullName, Compilation compilation, MetadataReference? metadataReference, string assemblyLocation)
        {
            var logger = new StringBuilder();
            var resolver = new AssemblyResolver(compilation, logger);

            // Load the assembly.
            PEFile? file = null;
            if (metadataReference is not null)
                file = resolver.TryResolve(metadataReference, PEStreamOptions.PrefetchEntireImage);

            if (file is null && assemblyLocation is null)
            {
                throw new NotSupportedException(FeaturesResources.Cannot_navigate_to_the_symbol_under_the_caret);
            }

            file ??= new PEFile(assemblyLocation, PEStreamOptions.PrefetchEntireImage);

            // Initialize a decompiler with default settings.
            var decompiler = new CSharpDecompiler(file, resolver, new DecompilerSettings());
            // Escape invalid identifiers to prevent Roslyn from failing to parse the generated code.
            // (This happens for example, when there is compiler-generated code that is not yet recognized/transformed by the decompiler.)
            decompiler.AstTransforms.Add(new EscapeInvalidIdentifiers());

            var fullTypeName = new FullTypeName(fullName);

            // Try to decompile; if an exception is thrown the caller will handle it
            var text = decompiler.DecompileTypeAsString(fullTypeName);

            text += "#if false // " + CSharpEditorResources.Decompilation_log + Environment.NewLine;
            text += logger.ToString();
            text += "#endif" + Environment.NewLine;

            return document.WithText(SourceText.From(text));
        }

        private static async Task<Document> AddAssemblyInfoRegionAsync(Document document, ISymbol symbol, CancellationToken cancellationToken)
        {
            var assemblyInfo = MetadataAsSourceHelpers.GetAssemblyInfo(symbol.ContainingAssembly);
            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var assemblyPath = MetadataAsSourceHelpers.GetAssemblyDisplay(compilation, symbol.ContainingAssembly);

            var regionTrivia = SyntaxFactory.RegionDirectiveTrivia(true)
                .WithTrailingTrivia(new[] { SyntaxFactory.Space, SyntaxFactory.PreprocessingMessage(assemblyInfo) });

            var oldRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot.WithLeadingTrivia(new[]
                {
                    SyntaxFactory.Trivia(regionTrivia),
                    SyntaxFactory.CarriageReturnLineFeed,
                    SyntaxFactory.Comment("// " + assemblyPath),
                    SyntaxFactory.CarriageReturnLineFeed,
                    SyntaxFactory.Comment($"// Decompiled with ICSharpCode.Decompiler {s_decompilerVersion.FileVersion}"),
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
}
