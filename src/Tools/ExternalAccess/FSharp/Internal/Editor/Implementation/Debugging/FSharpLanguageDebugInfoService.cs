// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor.Implementation.Debugging
{
    [Shared]
    [ExportLanguageService(typeof(ILanguageDebugInfoService), LanguageNames.FSharp)]
    internal class FSharpLanguageDebugInfoService : ILanguageDebugInfoService
    {
        private readonly IFSharpLanguageDebugInfoService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpLanguageDebugInfoService(IFSharpLanguageDebugInfoService service)
        {
            _service = service;
        }

        public async Task<DebugDataTipInfo> GetDataTipInfoAsync(Document document, int position, CancellationToken cancellationToken)
            => (await _service.GetDataTipInfoAsync(document, position, cancellationToken).ConfigureAwait(false)).UnderlyingObject;

        public async Task<DebugLocationInfo> GetLocationInfoAsync(Document document, int position, CancellationToken cancellationToken)
            => (await _service.GetLocationInfoAsync(document, position, cancellationToken).ConfigureAwait(false)).UnderlyingObject;
    }
}
