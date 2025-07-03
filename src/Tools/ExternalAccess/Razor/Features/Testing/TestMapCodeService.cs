// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MapCode;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Testing;

[Shared]
[PartNotDiscoverable]
[ExportLanguageService(typeof(IMapCodeService), language: LanguageNames.CSharp, layer: ServiceLayer.Test)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class TestMapCodeService() : IMapCodeService
{
    public async Task<ImmutableArray<TextChange>?> MapCodeAsync(
        Document document, ImmutableArray<string> contents, ImmutableArray<(Document, TextSpan)> focusLocations, CancellationToken cancellationToken)
    {
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(true);
        var newText = text.Replace(focusLocations.Single().Item2, contents.Single());
        return newText.GetTextChanges(text).ToImmutableArray();
    }
}
