// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal readonly struct VSTypeScriptWaitContextWrapper
    {
        private readonly IWaitContext _underlyingObject;

        public VSTypeScriptWaitContextWrapper(IWaitContext underlyingObject)
        {
            _underlyingObject = underlyingObject;
        }

        public CancellationToken CancellationToken => _underlyingObject.CancellationToken;
    }
}
