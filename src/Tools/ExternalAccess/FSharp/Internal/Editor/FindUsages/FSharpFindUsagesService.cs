// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.FindUsages;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor.FindUsages
{
    [Shared]
    [ExportLanguageService(typeof(IFindUsagesService), LanguageNames.FSharp)]
    internal sealed class FSharpFindUsagesService : IFindUsagesService
    {
        private readonly IFSharpFindUsagesService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpFindUsagesService(IFSharpFindUsagesService service)
            => _service = service;

        public Task FindImplementationsAsync(IFindUsagesContext context, Document document, int position, CancellationToken cancellationToken)
            => _service.FindImplementationsAsync(document, position, new FSharpFindUsagesContext(context, cancellationToken));

        public Task FindReferencesAsync(IFindUsagesContext context, Document document, int position, CancellationToken cancellationToken)
            => _service.FindReferencesAsync(document, position, new FSharpFindUsagesContext(context, cancellationToken));
    }
}
