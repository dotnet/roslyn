// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[Export(typeof(IDiagnosticSourceProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LanguageServerProjectSystemDiagnosticSourceProvider()
    : AbstractProjectLoadDiagnosticSourceProvider("ProjectLoad")
{
    protected override LanguageServerProjectLoader? GetProjectLoader(RequestContext context)
        => context.GetService<LanguageServerProjectSystem>();
}
