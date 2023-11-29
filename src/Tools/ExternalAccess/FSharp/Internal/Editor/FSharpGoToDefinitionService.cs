// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor;

[ExportLanguageService(typeof(IDefinitionLocationService), LanguageNames.FSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class FSharpDefinitionLocationService(
    IFSharpGoToDefinitionService service) : IDefinitionLocationService
{
    public async Task<DefinitionLocation?> GetDefinitionLocationAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var items = await service.FindDefinitionsAsync(document, position, cancellationToken).ConfigureAwait(false);
        if (items is null)
            return null;

        var firstItem = items.FirstOrDefault();
        if (firstItem is null)
            return null;

        var navigableItem = await new DocumentSpan(firstItem.Document, firstItem.SourceSpan).GetNavigableLocationAsync(cancellationToken).ConfigureAwait(false);
        if (navigableItem is null)
            return null;

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        return new DefinitionLocation(navigableItem, GetDocumentSpan());

        DocumentSpan GetDocumentSpan()
        {
            // To determine the span to underline, just use a simple heuristic of expanding out to the surrounding
            // letters and numbers.  F# can reimplement this in the future to be more accurate to their language.

            var startPosition = position;
            var endPosition = position + 1;

            if (char.IsLetterOrDigit(text[position]))
            {
                while (char.IsLetterOrDigit(GetChar(startPosition - 1)))
                    startPosition--;

                while (char.IsLetterOrDigit(GetChar(endPosition + 1)))
                    endPosition++;
            }

            return new DocumentSpan(document, TextSpan.FromBounds(startPosition, endPosition));
        }

        char GetChar(int position)
            => position >= 0 && position < text.Length ? text[position] : (char)0;
    }
}
