// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal ref struct DisposableAction<T>
{
    private readonly Action<T> _action;
    private readonly T _arg;
    private bool _invoked;

    public DisposableAction(Action<T> action, T arg)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _arg = arg;
    }

    public void Dispose()
    {
        if (!_invoked)
        {
            _action(_arg);
            _invoked = true;
        }
    }
}
