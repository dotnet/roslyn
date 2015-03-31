// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            _disabledProviders.Add(provider);
        }

        protected void EnableProvider(object provider)
        {
            _disabledProviders.Remove(provider);
        }

        protected void IgnoreProvider(object provider)
        {
            _ignoredProviders.Add(provider);
        }

        public bool IsIgnored(object provider)
        {
            return _ignoredProviders.Contains(provider);
        }

        public bool IsDisabled(object provider)
        {
            return _disabledProviders.Contains(provider);
        }

        public virtual bool CanHandleException(object provider, Exception exception)
        {
            return true;
        }

        public virtual void HandleException(object provider, Exception exception)
        {
            DisableProvider(provider);
        }
    }
}
