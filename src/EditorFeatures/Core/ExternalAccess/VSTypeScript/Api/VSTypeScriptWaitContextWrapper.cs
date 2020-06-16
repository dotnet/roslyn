// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal readonly struct VSTypeScriptWaitContextWrapper
    {
        private readonly IWaitContext _underlyingObject;

        public VSTypeScriptWaitContextWrapper(IWaitContext underlyingObject)
            => _underlyingObject = underlyingObject;

        public CancellationToken CancellationToken => _underlyingObject.CancellationToken;
    }
}
