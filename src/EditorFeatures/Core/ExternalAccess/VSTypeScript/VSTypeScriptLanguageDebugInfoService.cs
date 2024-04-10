// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

[Shared]
[ExportLanguageService(typeof(ILanguageDebugInfoService), InternalLanguageNames.TypeScript)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VSTypeScriptLanguageDebugInfoService(IVSTypeScriptLanguageDebugInfoServiceImplementation implementation) : ILanguageDebugInfoService
{
    private readonly IVSTypeScriptLanguageDebugInfoServiceImplementation _implementation = implementation;

    public async Task<DebugDataTipInfo> GetDataTipInfoAsync(Document document, int position, CancellationToken cancellationToken)
        => (await _implementation.GetDataTipInfoAsync(document, position, cancellationToken).ConfigureAwait(false)).UnderlyingObject;

    public async Task<DebugLocationInfo> GetLocationInfoAsync(Document document, int position, CancellationToken cancellationToken)
        => (await _implementation.GetLocationInfoAsync(document, position, cancellationToken).ConfigureAwait(false)).UnderlyingObject;
}
