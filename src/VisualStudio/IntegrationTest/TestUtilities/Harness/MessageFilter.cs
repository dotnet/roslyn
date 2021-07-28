// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using IMessageFilter = Microsoft.VisualStudio.OLE.Interop.IMessageFilter;
using INTERFACEINFO = Microsoft.VisualStudio.OLE.Interop.INTERFACEINFO;
using PENDINGMSG = Microsoft.VisualStudio.OLE.Interop.PENDINGMSG;
using SERVERCALL = Microsoft.VisualStudio.OLE.Interop.SERVERCALL;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Harness
{
    public sealed class MessageFilter : IMessageFilter, IDisposable
    {
        private const uint CancelCall = ~0U;

        private readonly MessageFilterSafeHandle _messageFilterRegistration;
        private readonly TimeSpan _timeout;
        private readonly TimeSpan _retryDelay;

        public MessageFilter()
            : this(timeout: TimeSpan.FromSeconds(60), retryDelay: TimeSpan.FromMilliseconds(150))
        {
        }

        public MessageFilter(TimeSpan timeout, TimeSpan retryDelay)
        {
            _timeout = timeout;
            _retryDelay = retryDelay;
            _messageFilterRegistration = MessageFilterSafeHandle.Register(this);
        }

        public uint HandleInComingCall(uint dwCallType, IntPtr htaskCaller, uint dwTickCount, INTERFACEINFO[] lpInterfaceInfo)
        {
            return (uint)SERVERCALL.SERVERCALL_ISHANDLED;
        }

        public uint RetryRejectedCall(IntPtr htaskCallee, uint dwTickCount, uint dwRejectType)
        {
            if ((SERVERCALL)dwRejectType != SERVERCALL.SERVERCALL_RETRYLATER
                && (SERVERCALL)dwRejectType != SERVERCALL.SERVERCALL_REJECTED)
            {
                return CancelCall;
            }

            if (dwTickCount >= _timeout.TotalMilliseconds)
            {
                return CancelCall;
            }

            return (uint)_retryDelay.TotalMilliseconds;
        }

        public uint MessagePending(IntPtr htaskCallee, uint dwTickCount, uint dwPendingType)
        {
            return (uint)PENDINGMSG.PENDINGMSG_WAITDEFPROCESS;
        }

        public void Dispose()
        {
            _messageFilterRegistration.Dispose();
        }
    }
}
