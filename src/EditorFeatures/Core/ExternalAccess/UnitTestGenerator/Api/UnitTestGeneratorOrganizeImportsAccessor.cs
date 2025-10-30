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

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTestGenerator.Api;

[Export]
[Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class UnitTestGeneratorOrganizeImportsAccessor()
{
#pragma warning disable CA1822 // Mark members as static
    public async Task<Document> OrganizeImportsAsync(Document document, CancellationToken cancellationToken)
#pragma warning restore CA1822 // Mark members as static
    {
        var organizeImportsService = document.GetRequiredLanguageService<IOrganizeImportsService>();
        var options = await document.GetOrganizeImportsOptionsAsync(cancellationToken).ConfigureAwait(false);
        return await organizeImportsService.OrganizeImportsAsync(document, options, cancellationToken).ConfigureAwait(false);
    }
}
