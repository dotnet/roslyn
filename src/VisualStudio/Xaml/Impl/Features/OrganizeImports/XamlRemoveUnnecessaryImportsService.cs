// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Xaml.Features.OrganizeImports;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;

namespace Microsoft.CodeAnalysis.Editor.Xaml.OrganizeImports
{
    [ExportLanguageService(typeof(IRemoveUnnecessaryImportsService), StringConstants.XamlLanguageName), Shared]
    internal class XamlRemoveUnnecessaryImportsService : IRemoveUnnecessaryImportsService
    {
        private readonly IXamlRemoveUnnecessaryNamespacesService _removeService;

        [ImportingConstructor]
        public XamlRemoveUnnecessaryImportsService(IXamlRemoveUnnecessaryNamespacesService removeService)
        {
            _removeService = removeService;
        }

        public Task<Document> RemoveUnnecessaryImportsAsync(Document document, CancellationToken cancellationToken)
        {
            return _removeService.RemoveUnnecessaryNamespacesAsync(document, cancellationToken) ?? Task.FromResult(document);
        }
    }
}
