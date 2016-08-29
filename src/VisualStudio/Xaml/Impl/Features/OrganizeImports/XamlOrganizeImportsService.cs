// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Xaml.Features.OrganizeImports;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.VisualStudio.LanguageServices.Xaml;

namespace Microsoft.CodeAnalysis.Editor.Xaml.OrganizeImports
{
    [ExportLanguageService(typeof(IOrganizeImportsService), StringConstants.XamlLanguageName), Shared]
    internal partial class XamlOrganizeImportsService : IOrganizeImportsService
    {
        private readonly IXamlOrganizeNamespacesService _organizeService;

        [ImportingConstructor]
        public XamlOrganizeImportsService(IXamlOrganizeNamespacesService organizeService)
        {
            _organizeService = organizeService;
        }

        public Task<Document> OrganizeImportsAsync(Document document, bool placeSystemNamespaceFirst, CancellationToken cancellationToken)
        {
            return _organizeService.OrganizeNamespacesAsync(document, placeSystemNamespaceFirst, cancellationToken) ?? Task.FromResult(document);
        }

        public string SortAndRemoveUnusedImportsDisplayStringWithAccelerator
        {
            get
            {
                return Resources.RemoveAndSortNamespacesWithAccelerator;
            }
        }
    }
}
