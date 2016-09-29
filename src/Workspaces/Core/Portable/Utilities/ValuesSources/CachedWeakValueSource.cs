// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// A value source that cache's its value weakly once obtained from its source.
    /// The source must allow repeatable accesses.
    /// </summary>
    internal class CachedWeakValueSource<T> : ValueSource<T>
        where T : class
    {
        private SemaphoreSlim _gateDoNotAccessDirectly; // Lazily created. Access via the Gate property
        private readonly ValueSource<T> _source;
        private WeakReference<T> _reference;

        private static readonly WeakReference<T> s_noReference = new WeakReference<T>(null);

        public CachedWeakValueSource(ValueSource<T> source)
        {
            _source = source;
            _reference = s_noReference;
        }

        private SemaphoreSlim Gate => LazyInitialization.EnsureInitialized(ref _gateDoNotAccessDirectly, SemaphoreSlimFactory.Instance);

        public override bool TryGetValue(out T value)
        {
            return _reference.TryGetTarget(out value)
                || _source.TryGetValue(out value);
        }

        public override T GetValue(CancellationToken cancellationToken = default(CancellationToken))
        {
            T value;
            if (!_reference.TryGetTarget(out value))
            {
                using (Gate.DisposableWait(cancellationToken))
                {
                    if (!_reference.TryGetTarget(out value))
                    {
                        value = _source.GetValue(cancellationToken);
                        _reference = new WeakReference<T>(value);
                    }
                }
            }

            return value;
        }

        public override async Task<T> GetValueAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            T value;
            if (!_reference.TryGetTarget(out value))
            {
                using (await Gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (!_reference.TryGetTarget(out value))
                    {
                        value = await _source.GetValueAsync(cancellationToken).ConfigureAwait(false);
                        _reference = new WeakReference<T>(value);
                    }
                }
            }

            return value;
        }
    }
}