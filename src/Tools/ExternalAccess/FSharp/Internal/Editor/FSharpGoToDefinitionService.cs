// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Composition;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Navigation;
using Microsoft.CodeAnalysis.GoToDefinition;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor
{
    [Shared]
    [ExportLanguageService(typeof(IGoToDefinitionService), LanguageNames.FSharp)]
    internal class FSharpGoToDefinitionService : IGoToDefinitionService
    {
        private readonly IFSharpGoToDefinitionService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpGoToDefinitionService(IFSharpGoToDefinitionService service)
        {
            _service = service;
        }

        public async Task<IEnumerable<INavigableItem>> FindDefinitionsAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var items = await _service.FindDefinitionsAsync(document, position, cancellationToken).ConfigureAwait(false);
            return items?.Select(x => new InternalFSharpNavigableItem(x));
        }

        public bool TryGoToDefinition(Document document, int position, CancellationToken cancellationToken)
        {
            return _service.TryGoToDefinition(document, position, cancellationToken);
        }
    }
}
