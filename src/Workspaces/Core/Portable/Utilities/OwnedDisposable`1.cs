// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Roslyn.Utilities
{
    internal struct OwnedDisposable<T> : IDisposable, IEquatable<OwnedDisposable<T>>
        where T : class, IDisposable
    {
        private T _resource;

        public OwnedDisposable(T resource)
        {
            _resource = resource;
        }

        public OwnedDisposable(Func<T> resource)
        {
            _resource = resource();
        }

        public T Target => _resource;

        public T Claim()
        {
            return Interlocked.Exchange(ref _resource, null);
        }

        public OwnedDisposable<T> Move()
        {
            return new OwnedDisposable<T>(Claim());
        }

        public Boxed Box()
        {
            return new Boxed(ref this);
        }

        public void Dispose()
        {
            Claim()?.Dispose();
        }

        public override bool Equals(object obj)
        {
            return obj is OwnedDisposable<T> other
                && Equals(other);
        }

        public bool Equals(OwnedDisposable<T> other)
        {
            return EqualityComparer<T>.Default.Equals(_resource, other._resource);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<T>.Default.GetHashCode(_resource);
        }

        public static bool operator ==(OwnedDisposable<T> disposable1, OwnedDisposable<T> disposable2)
        {
            return disposable1.Equals(disposable2);
        }

        public static bool operator !=(OwnedDisposable<T> disposable1, OwnedDisposable<T> disposable2)
        {
            return !(disposable1 == disposable2);
        }

        public static bool operator ==(OwnedDisposable<T> disposable1, T y)
        {
            return EqualityComparer<T>.Default.Equals(disposable1._resource, y);
        }

        public static bool operator !=(OwnedDisposable<T> disposable1, T y)
        {
            return !(disposable1 == y);
        }

        internal sealed class Boxed : IDisposable
        {
            private OwnedDisposable<T> _resource;

            internal Boxed(ref OwnedDisposable<T> resource)
            {
                _resource = resource.Move();
            }

            public ref OwnedDisposable<T> Resource => ref _resource;

            public T Target => _resource.Target;

            public void Dispose()
            {
                Resource.Dispose();
            }
        }
    }
}
