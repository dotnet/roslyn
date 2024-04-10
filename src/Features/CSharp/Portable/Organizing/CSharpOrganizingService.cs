// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Organizing;
using Microsoft.CodeAnalysis.Organizing.Organizers;

namespace Microsoft.CodeAnalysis.CSharp.Organizing;

[ExportLanguageService(typeof(IOrganizingService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal partial class CSharpOrganizingService(
    [ImportMany] IEnumerable<Lazy<ISyntaxOrganizer, LanguageMetadata>> organizers) : AbstractOrganizingService(organizers.Where(o => o.Metadata.Language == LanguageNames.CSharp).Select(o => o.Value))
{
    protected override async Task<Document> ProcessAsync(Document document, IEnumerable<ISyntaxOrganizer> organizers, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var rewriter = new Rewriter(this, organizers, await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
        return document.WithSyntaxRoot(rewriter.Visit(root));
    }
}
