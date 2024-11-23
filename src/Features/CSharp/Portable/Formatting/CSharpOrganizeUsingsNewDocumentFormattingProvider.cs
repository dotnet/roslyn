// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CSharp.MisplacedUsingDirectives;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

[ExportNewDocumentFormattingProvider(LanguageNames.CSharp), Shared]
internal class CSharpOrganizeUsingsNewDocumentFormattingProvider : INewDocumentFormattingProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpOrganizeUsingsNewDocumentFormattingProvider()
    {
    }

    public async Task<Document> FormatNewDocumentAsync(Document document, Document? hintDocument, CodeCleanupOptions options, CancellationToken cancellationToken)
    {
        var organizeImportsService = document.GetRequiredLanguageService<IOrganizeImportsService>();
        var organizedDocument = await organizeImportsService.OrganizeImportsAsync(document, options.GetOrganizeImportsOptions(), cancellationToken).ConfigureAwait(false);

        return await MisplacedUsingDirectivesCodeFixProvider.TransformDocumentIfRequiredAsync(
            organizedDocument, options.SimplifierOptions, options.AddImportOptions.UsingDirectivePlacement, cancellationToken).ConfigureAwait(false);
    }
}
