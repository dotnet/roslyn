// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

[Shared]
[Export(typeof(VSTypeScriptAsynchronousOperationListenerProvider))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VSTypeScriptAsynchronousOperationListenerProvider(
    IAsynchronousOperationListenerProvider provider)
{
    public VSTypeScriptAsynchronousOperationListener GetListener(string featureName)
        => new(provider.GetListener(featureName));
}
