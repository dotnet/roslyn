// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Roslyn.Utilities
{
    internal class CancellableLazy<T>
    {
        private NonReentrantLock gate;
        private Func<CancellationToken, T> valueFactory;
        private T value;

        public CancellableLazy(Func<CancellationToken, T> valueFactory)
        {
            this.gate = new NonReentrantLock();
            this.valueFactory = valueFactory;
        }

        public CancellableLazy(T value)
        {
            this.value = value;
        }

        public bool HasValue
        {
            get
            {
                T tmp;
                return this.TryGetValue(out tmp);
            }
        }

        public bool TryGetValue(out T value)
        {
            if (this.valueFactory == null)
            {
                value = this.value;
                return true;
            }
            else
            {
                value = default(T);
                return false;
            }
        }

        public T GetValue(CancellationToken cancellationToken = default(CancellationToken))
        {
            var gate = this.gate;
            if (gate != null)
            {
                using (gate.DisposableWait(cancellationToken))
                {
                    if (this.valueFactory != null)
                    {
                        this.value = this.valueFactory(cancellationToken);
                        Interlocked.Exchange(ref this.valueFactory, null);
                    }

                    Interlocked.Exchange(ref this.gate, null);
                }
            }

            return this.value;
        }
    }
}
