// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.Editor.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal readonly struct VSTypeScriptWaitIndicatorWrapper
    {
        private readonly IWaitIndicator _underlyingObject;

        public VSTypeScriptWaitIndicatorWrapper(IWaitIndicator underlyingObject)
            => _underlyingObject = underlyingObject;

        public VSTypeScriptWaitIndicatorResult Wait(string title, string message, bool allowCancel, Action<VSTypeScriptWaitContextWrapper> action)
            => (VSTypeScriptWaitIndicatorResult)_underlyingObject.Wait(title, message, allowCancel, context => action(new VSTypeScriptWaitContextWrapper(context)));

    }
}
