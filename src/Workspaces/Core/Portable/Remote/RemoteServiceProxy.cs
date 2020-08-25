// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;

namespace Microsoft.CodeAnalysis.Remote
{
    internal readonly struct RemoteServiceProxy<T> : IDisposable
        where T : class
    {
        public readonly T Service { get; }

        public RemoteServiceProxy(T service)
        {
            Service = service;
        }

        public void Dispose()
            => (Service as IDisposable)?.Dispose();
    }
}
