// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// A value source that caches its value weakly once obtained from its source.
    /// The source must allow repeatable accesses.
    /// </summary>
    internal sealed class WeaklyCachedValueSource<T> : ValueSource<T>
        where T : class
    {
        private SemaphoreSlim? _lazyGate; // Lazily created. Access via the Gate property
        private readonly ValueSource<T> _source;
        private WeakReference<T>? _weakReference;

        public WeaklyCachedValueSource(ValueSource<T> source)
        {
            _source = source;
            _weakReference = null;
        }

        private SemaphoreSlim Gate => LazyInitialization.EnsureInitialized(ref _lazyGate, SemaphoreSlimFactory.Instance);

        public override bool TryGetValue([MaybeNullWhen(false)]out T value)
        {
            // Suppressing nullable warning due to https://github.com/dotnet/roslyn/issues/40266
            var weakReference = _weakReference;
            return weakReference != null && weakReference.TryGetTarget(out value) ||
                _source.TryGetValue(out value!);
        }

        public override T GetValue(CancellationToken cancellationToken = default)
        {
            var weakReference = _weakReference;
            if (weakReference == null || !weakReference.TryGetTarget(out var value))
            {
                using (Gate.DisposableWait(cancellationToken))
                {
                    if (_weakReference == null || !_weakReference.TryGetTarget(out value))
                    {
                        value = _source.GetValue(cancellationToken);
                        _weakReference = new WeakReference<T>(value);
                    }
                }
            }

            return value;
        }

        public override async Task<T> GetValueAsync(CancellationToken cancellationToken = default)
        {
            var weakReference = _weakReference;
            if (weakReference == null || !weakReference.TryGetTarget(out var value))
            {
                using (await Gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (_weakReference == null || !_weakReference.TryGetTarget(out value))
                    {
                        value = await _source.GetValueAsync(cancellationToken).ConfigureAwait(false);
                        _weakReference = new WeakReference<T>(value);
                    }
                }
            }

            return value;
        }
    }
}
