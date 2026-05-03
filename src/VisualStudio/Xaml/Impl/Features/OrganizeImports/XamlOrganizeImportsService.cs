// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Xaml.Features.OrganizeImports;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.VisualStudio.LanguageServices.Xaml;

namespace Microsoft.CodeAnalysis.Editor.Xaml.OrganizeImports;

[ExportLanguageService(typeof(IOrganizeImportsService), StringConstants.XamlLanguageName), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class XamlOrganizeImportsService(IXamlOrganizeNamespacesService organizeService) : IOrganizeImportsService
{
    private readonly IXamlOrganizeNamespacesService _organizeService = organizeService;

    public async Task<Document> OrganizeImportsAsync(Document document, OrganizeImportsOptions options, CancellationToken cancellationToken)
    {
        return await _organizeService.OrganizeNamespacesAsync(document, options.PlaceSystemNamespaceFirst, cancellationToken).ConfigureAwait(false) ?? document;
    }

    public string SortImportsDisplayStringWithAccelerator => Resources.Sort_Namespaces_with_accelerator;
    public string SortImportsDisplayStringWithoutAccelerator => Resources.Sort_Namespaces;

    public string SortAndRemoveUnusedImportsDisplayStringWithAccelerator => Resources.RemoveAndSortNamespacesWithAccelerator;
}
