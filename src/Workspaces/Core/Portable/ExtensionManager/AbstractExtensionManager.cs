// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Extensions;

internal abstract class AbstractExtensionManager : IExtensionManager
{
    private readonly ConcurrentSet<object> _disabledProviders = new(ReferenceEqualityComparer.Instance);
    private readonly ConcurrentSet<object> _ignoredProviders = new(ReferenceEqualityComparer.Instance);

    protected abstract void HandleNonCancellationException(object provider, Exception exception);

    protected void DisableProvider(object provider)
        => _disabledProviders.Add(provider);

    protected void EnableProvider(object provider)
        => _disabledProviders.Remove(provider);

    protected void IgnoreProvider(object provider)
        => _ignoredProviders.Add(provider);

    public bool IsIgnored(object provider)
        => _ignoredProviders.Contains(provider);

    public bool IsDisabled(object provider)
        => _disabledProviders.Contains(provider);

    public bool HandleException(object provider, Exception exception)
    {
        if (exception is OperationCanceledException)
            return false;

        HandleNonCancellationException(provider, exception);
        return true;
    }
}
