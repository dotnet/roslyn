// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.GoToDefinition;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Navigation;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.GoToDefinition
{
    [ExportLanguageService(typeof(IFindDefinitionService), LanguageNames.FSharp), Shared]
    internal class FSharpFindDefinitionService : IFindDefinitionService
    {
        private readonly IFSharpFindDefinitionService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpFindDefinitionService(IFSharpFindDefinitionService service)
        {
            _service = service;
        }

        public async Task<ImmutableArray<INavigableItem>> FindDefinitionsAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var items = await _service.FindDefinitionsAsync(document, position, cancellationToken).ConfigureAwait(false);
            return items.SelectAsArray(x => (INavigableItem)new InternalFSharpNavigableItem(x));
        }
    }
}
