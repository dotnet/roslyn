// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Host.Mef;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ExternalAccess.FSharp.Editor.Implementation.Debugging;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Internal.Editor.Implementation.Debugging;
#else
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.Implementation.Debugging;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor.Implementation.Debugging;
#endif

[ExportLanguageService(typeof(ILanguageDebugInfoService), LanguageNames.FSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class FSharpLanguageDebugInfoService(IFSharpLanguageDebugInfoService service) : ILanguageDebugInfoService
{
    private readonly IFSharpLanguageDebugInfoService _service = service;

    public async Task<DebugDataTipInfo> GetDataTipInfoAsync(Document document, int position, bool includeKind, CancellationToken cancellationToken)
        => (await _service.GetDataTipInfoAsync(document, position, cancellationToken).ConfigureAwait(false)).UnderlyingObject;

    public async Task<DebugLocationInfo> GetLocationInfoAsync(Document document, int position, CancellationToken cancellationToken)
        => (await _service.GetLocationInfoAsync(document, position, cancellationToken).ConfigureAwait(false)).UnderlyingObject;
}
