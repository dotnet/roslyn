// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Extensions
{
    internal abstract class AbstractExtensionManager : IExtensionManager
    {
        private readonly ConcurrentSet<object> _disabledProviders = new ConcurrentSet<object>(ReferenceEqualityComparer.Instance);
        private readonly ConcurrentSet<object> _ignoredProviders = new ConcurrentSet<object>(ReferenceEqualityComparer.Instance);

        protected AbstractExtensionManager()
        {
        }

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

        public virtual bool CanHandleException(object provider, Exception exception)
            => true;

        public virtual void HandleException(object provider, Exception exception)
            => DisableProvider(provider);
    }
}
