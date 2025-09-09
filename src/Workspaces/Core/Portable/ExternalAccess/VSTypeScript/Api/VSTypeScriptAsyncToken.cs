// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

internal sealed class VSTypeScriptAsyncToken(IAsyncToken underlyingObject) : IDisposable
{
    internal IAsyncToken UnderlyingObject
        => underlyingObject;

    public void Dispose()
        => UnderlyingObject.Dispose();
}
