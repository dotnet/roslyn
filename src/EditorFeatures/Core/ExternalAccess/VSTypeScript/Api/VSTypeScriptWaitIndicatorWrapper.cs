// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.Editor.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal readonly struct VSTypeScriptWaitIndicatorWrapper
    {
        private readonly IWaitIndicator _underlyingObject;

        public VSTypeScriptWaitIndicatorWrapper(IWaitIndicator underlyingObject)
        {
            _underlyingObject = underlyingObject;
        }

        public VSTypeScriptWaitIndicatorResult Wait(string title, string message, bool allowCancel, Action<VSTypeScriptWaitContextWrapper> action)
            => (VSTypeScriptWaitIndicatorResult)_underlyingObject.Wait(title, message, allowCancel, context => action(new VSTypeScriptWaitContextWrapper(context)));

    }
}
