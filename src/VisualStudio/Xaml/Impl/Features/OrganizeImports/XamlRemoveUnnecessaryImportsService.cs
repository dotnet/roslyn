// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Xaml.Features.OrganizeImports;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;

namespace Microsoft.CodeAnalysis.Editor.Xaml.OrganizeImports;

[ExportLanguageService(typeof(IRemoveUnnecessaryImportsService), StringConstants.XamlLanguageName), Shared]
internal sealed class XamlRemoveUnnecessaryImportsService : IRemoveUnnecessaryImportsService
{
    private readonly IXamlRemoveUnnecessaryNamespacesService _removeService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public XamlRemoveUnnecessaryImportsService(IXamlRemoveUnnecessaryNamespacesService removeService)
    {
        _removeService = removeService;
    }

    public Task<Document> RemoveUnnecessaryImportsAsync(Document document, CancellationToken cancellationToken)
        => RemoveUnnecessaryImportsAsync(document, predicate: null, cancellationToken: cancellationToken);

    public Task<Document> RemoveUnnecessaryImportsAsync(
        Document document, Func<SyntaxNode, bool>? predicate, CancellationToken cancellationToken)
    {
        return _removeService.RemoveUnnecessaryNamespacesAsync(document, cancellationToken) ?? Task.FromResult(document);
    }
}
