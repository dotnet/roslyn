// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.OrganizeImports;

[ExportLanguageService(typeof(IOrganizeImportsService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class CSharpOrganizeImportsService() : IOrganizeImportsService
{
    public async Task<Document> OrganizeImportsAsync(Document document, OrganizeImportsOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var rewriter = new Rewriter(options);
        var newRoot = rewriter.Visit(root);
        Contract.ThrowIfNull(newRoot);

        return document.WithSyntaxRoot(newRoot);
    }

    public string SortImportsDisplayStringWithAccelerator
        => CSharpWorkspaceResources.Sort_Usings_with_accelerator;

    public string SortImportsDisplayStringWithoutAccelerator
        => CSharpWorkspaceResources.Sort_Usings;

    public string SortAndRemoveUnusedImportsDisplayStringWithAccelerator
        => CSharpWorkspaceResources.Remove_and_Sort_Usings;
}
