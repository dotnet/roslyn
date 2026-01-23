// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ExternalAccess.FSharp.GoToDefinition;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Internal.Navigation;
#else
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.GoToDefinition;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Navigation;
#endif

[ExportLanguageService(typeof(INavigableItemsService), LanguageNames.FSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class FSharpNavigableItemsService(IFSharpFindDefinitionService service) : INavigableItemsService
{
    public async Task<ImmutableArray<INavigableItem>> GetNavigableItemsAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var items = await service.FindDefinitionsAsync(document, position, cancellationToken).ConfigureAwait(false);
        return items.SelectAsArray(x => (INavigableItem)new InternalFSharpNavigableItem(x));
    }

    public Task<ImmutableArray<INavigableItem>> GetNavigableItemsAsync(Document document, int position, bool forSymbolType, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
