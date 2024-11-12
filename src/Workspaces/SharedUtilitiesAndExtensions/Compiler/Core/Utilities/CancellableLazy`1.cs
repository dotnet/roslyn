// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Roslyn.Utilities;

internal sealed class CancellableLazy<T>
{
    private NonReentrantLock? _gate;
    private Func<CancellationToken, T>? _valueFactory;
    private T? _value;

    public CancellableLazy(Func<CancellationToken, T> valueFactory)
    {
        _gate = new NonReentrantLock();
        _valueFactory = valueFactory;
    }

    public CancellableLazy(T value)
        => _value = value;

    public bool HasValue
    {
        get
        {
            return this.TryGetValue(out _);
        }
    }

    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        if (_valueFactory == null)
        {
            value = _value!;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    public T GetValue(CancellationToken cancellationToken = default)
    {
        var gate = _gate;
        if (gate != null)
        {
            using (gate.DisposableWait(cancellationToken))
            {
                if (_valueFactory != null)
                {
                    _value = _valueFactory(cancellationToken);
                    Interlocked.Exchange<Func<CancellationToken, T>?>(ref _valueFactory, null);
                }

                Interlocked.Exchange(ref _gate, null);
            }
        }

        return _value!;
    }
}
