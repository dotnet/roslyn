// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ConvertNamespace;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

[ExportNewDocumentFormattingProvider(LanguageNames.CSharp), Shared]
internal sealed class CSharpNamespaceDeclarationNewDocumentFormattingProvider : INewDocumentFormattingProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpNamespaceDeclarationNewDocumentFormattingProvider()
    {
    }

    public async Task<Document> FormatNewDocumentAsync(Document document, Document? hintDocument, CodeCleanupOptions options, CancellationToken cancellationToken)
    {
        var root = (CompilationUnitSyntax)await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var formattingOptions = (CSharpSyntaxFormattingOptions)options.FormattingOptions;

        var namespaces = GetNamespacesToReplace(document, root, formattingOptions.NamespaceDeclarations).ToList();
        if (namespaces.Count != 1)
            return document;

        return await ConvertNamespaceTransform.ConvertAsync(document, namespaces[0], formattingOptions, cancellationToken).ConfigureAwait(false);
    }

    private static IEnumerable<BaseNamespaceDeclarationSyntax> GetNamespacesToReplace(Document document, CompilationUnitSyntax root, CodeStyleOption2<NamespaceDeclarationPreference> option)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var declarations = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>();

        foreach (var declaration in declarations)
        {
            // Passing in forAnalyzer: true means we'll only get a result if the declaration doesn't match the preferences
            if (ConvertNamespaceAnalysis.CanOfferUseBlockScoped(option, declaration, forAnalyzer: true) ||
                ConvertNamespaceAnalysis.CanOfferUseFileScoped(option, root, declaration, forAnalyzer: true))
            {
                yield return declaration;
            }
        }
    }
}
