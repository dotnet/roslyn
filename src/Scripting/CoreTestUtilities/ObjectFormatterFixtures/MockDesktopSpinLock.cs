// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;

namespace ObjectFormatterFixtures
{
    /// <summary>
    /// Follows the shape of the Desktop version of <see cref="SpinLock"/> relevant for debugger display.
    /// </summary>
    [DebuggerTypeProxy(typeof(SpinLockDebugView))]
    [DebuggerDisplay("IsHeld = {IsHeld}")]
    internal struct MockDesktopSpinLock
    {
#pragma warning disable IDE0044 // Add readonly modifier - See https://github.com/dotnet/roslyn/issues/47225
        private volatile int m_owner;
#pragma warning restore IDE0044 // Add readonly modifier

        public MockDesktopSpinLock(bool enableThreadOwnerTracking)
        {
            m_owner = enableThreadOwnerTracking ? 0 : int.MinValue;
        }

        public bool IsHeld
            => false;

        public bool IsHeldByCurrentThread
            => IsThreadOwnerTrackingEnabled ? true : throw new InvalidOperationException("Error");

        public bool IsThreadOwnerTrackingEnabled
            => (m_owner & int.MinValue) == 0;

        internal class SpinLockDebugView
        {
            private MockDesktopSpinLock m_spinLock;

            public bool? IsHeldByCurrentThread
                => m_spinLock.IsHeldByCurrentThread;

            public int? OwnerThreadID
                => m_spinLock.IsThreadOwnerTrackingEnabled ? m_spinLock.m_owner : (int?)null;

            public bool IsHeld => m_spinLock.IsHeld;

            public SpinLockDebugView(MockDesktopSpinLock spinLock)
            {
                m_spinLock = spinLock;
            }
        }
    }
}
