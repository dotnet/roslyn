// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

[Export(typeof(IVSTypeScriptStreamingFindUsagesPresenterAccessor)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VSTypeScriptStreamingFindUsagesPresenterAccessor(IStreamingFindUsagesPresenter underlyingObject) : IVSTypeScriptStreamingFindUsagesPresenterAccessor
{
    private readonly IStreamingFindUsagesPresenter _underlyingObject = underlyingObject;

    public (IVSTypeScriptFindUsagesContext context, CancellationToken cancellationToken) StartSearch(
        string title, bool supportsReferences)
    {
        var (context, cancellationToken) = _underlyingObject.StartSearch(title, new StreamingFindUsagesPresenterOptions() { SupportsReferences = supportsReferences });
        return (new VSTypeScriptFindUsagesContext(context), cancellationToken);
    }

    public void ClearAll()
        => _underlyingObject.ClearAll();
}
