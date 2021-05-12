// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    [Obsolete("This is just a wrapper around the public Visual Studio API IUIThreadOperationContext, please use it directly.")]
    internal readonly struct VSTypeScriptWaitIndicatorWrapper
    {
        private readonly IUIThreadOperationExecutor _underlyingObject;

        public VSTypeScriptWaitIndicatorWrapper(IUIThreadOperationExecutor underlyingObject)
            => _underlyingObject = underlyingObject;

        public VSTypeScriptWaitIndicatorResult Wait(string title, string message, bool allowCancel, Action<VSTypeScriptWaitContextWrapper> action)
            => (VSTypeScriptWaitIndicatorResult)_underlyingObject.Execute(title, message, allowCancel, showProgress: false, context => action(new VSTypeScriptWaitContextWrapper(context)));

    }
}
