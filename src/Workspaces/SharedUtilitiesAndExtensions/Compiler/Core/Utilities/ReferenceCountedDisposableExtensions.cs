// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Roslyn.Utilities;

internal static class ReferenceCountedDisposableExtensions
{
    public static IReferenceCountedDisposable<T> AddReference<T>(this IReferenceCountedDisposable<T> disposable)
        where T : class, IDisposable
    {
        return disposable.TryAddReference() ?? throw new ObjectDisposedException(typeof(T).FullName);
    }

    public static ReferenceCountedDisposable<T> AddReference<T>(this ReferenceCountedDisposable<T> disposable)
        where T : class, IDisposable
    {
        return disposable.TryAddReference() ?? throw new ObjectDisposedException(typeof(T).FullName);
    }
}
