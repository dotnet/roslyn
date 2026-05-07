// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: This code is copied from dotnet/roslyn:
// https://github.com/dotnet/roslyn/blob/3f00cca15312e35225295d6f6ec68f8498538f8c/src/Compilers/Core/Portable/InternalUtilities/ReaderWriterLockSlimExtensions.cs

using System;
using System.Threading;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Threading;

internal static class ReaderWriterLockSlimExtensions
{
    internal static ReadLockExiter DisposableRead(this ReaderWriterLockSlim @lock)
    {
        return new ReadLockExiter(@lock);
    }

    [NonCopyable]
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

    [NonCopyable]
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

    [NonCopyable]
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
