// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Roslyn.Utilities
{
    internal static class ReaderWriterLockSlimExtensions
    {
        internal static ReadLockExiter DisposableRead(this ReaderWriterLockSlim @lock)
        {
            return new ReadLockExiter(@lock);
        }

        internal readonly struct ReadLockExiter : IDisposable
        {
            private readonly ReaderWriterLockSlim _lock;

            internal ReadLockExiter(ReaderWriterLockSlim @lock)
            {
                _lock = @lock;
                @lock.EnterReadLock();
            }

            public void Dispose()
            {
                _lock.ExitReadLock();
            }
        }

        internal static UpgradeableReadLockExiter DisposableUpgradeableRead(this ReaderWriterLockSlim @lock)
        {
            return new UpgradeableReadLockExiter(@lock);
        }

        internal readonly struct UpgradeableReadLockExiter : IDisposable
        {
            private readonly ReaderWriterLockSlim _lock;

            internal UpgradeableReadLockExiter(ReaderWriterLockSlim @lock)
            {
                _lock = @lock;
                @lock.EnterUpgradeableReadLock();
            }

            public void Dispose()
            {
                if (_lock.IsWriteLockHeld)
                {
                    _lock.ExitWriteLock();
                }

                _lock.ExitUpgradeableReadLock();
            }

            public void EnterWrite()
            {
                _lock.EnterWriteLock();
            }
        }

        internal static WriteLockExiter DisposableWrite(this ReaderWriterLockSlim @lock)
        {
            return new WriteLockExiter(@lock);
        }

        internal readonly struct WriteLockExiter : IDisposable
        {
            private readonly ReaderWriterLockSlim _lock;

            internal WriteLockExiter(ReaderWriterLockSlim @lock)
            {
                _lock = @lock;
                @lock.EnterWriteLock();
            }

            public void Dispose()
            {
                _lock.ExitWriteLock();
            }
        }

        internal static void AssertCanRead(this ReaderWriterLockSlim @lock)
        {
            if (!@lock.IsReadLockHeld && !@lock.IsUpgradeableReadLockHeld && !@lock.IsWriteLockHeld)
            {
                throw new InvalidOperationException();
            }
        }

        internal static void AssertCanWrite(this ReaderWriterLockSlim @lock)
        {
            if (!@lock.IsWriteLockHeld)
            {
                throw new InvalidOperationException();
            }
        }
    }
}
