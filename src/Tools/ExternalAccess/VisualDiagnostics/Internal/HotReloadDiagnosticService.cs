// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Internal;

[Export(typeof(IHotReloadDiagnosticService)), Shared]
internal sealed class HotReloadDiagnosticService : IHotReloadDiagnosticService
{
    private readonly IHotReloadDiagnosticService? _implementation;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public HotReloadDiagnosticService([ImportMany] IEnumerable<IDiagnosticSourceProvider> sourceProviders)
    {
        _implementation = sourceProviders.OfType<IHotReloadDiagnosticService>().FirstOrDefault();
    }

    void IHotReloadDiagnosticService.UpdateDiagnostics(IEnumerable<Diagnostic> diagnostics, string groupName)
        => _implementation?.UpdateDiagnostics(diagnostics, groupName);
}
