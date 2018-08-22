// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.Win32.SafeHandles;
using Xunit;
using IMessageFilter = Microsoft.VisualStudio.OLE.Interop.IMessageFilter;
using INTERFACEINFO = Microsoft.VisualStudio.OLE.Interop.INTERFACEINFO;
using PENDINGMSG = Microsoft.VisualStudio.OLE.Interop.PENDINGMSG;
using SERVERCALL = Microsoft.VisualStudio.OLE.Interop.SERVERCALL;

namespace Roslyn.VisualStudio.IntegrationTests
{
    [CaptureTestName]
    public abstract class AbstractIntegrationTest : IAsyncLifetime, IDisposable
    {
        protected readonly string ProjectName = "TestProj";
        protected readonly string SolutionName = "TestSolution";

        private readonly MessageFilter _messageFilter;
        private readonly VisualStudioInstanceFactory _instanceFactory;
        private VisualStudioInstanceContext _visualStudioContext;

        protected AbstractIntegrationTest(VisualStudioInstanceFactory instanceFactory)
        {
            Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());

            // Install a COM message filter to handle retry operations when the first attempt fails
            _messageFilter = RegisterMessageFilter();
            _instanceFactory = instanceFactory;

            try
            {
                Helper.Automation.TransactionTimeout = 20000;
            }
            catch
            {
                _messageFilter.Dispose();
                _messageFilter = null;
                throw;
            }
        }

        public VisualStudioInstance VisualStudio => _visualStudioContext?.Instance;

        public virtual async Task InitializeAsync()
        {
            try
            {
                _visualStudioContext = await _instanceFactory.GetNewOrUsedInstanceAsync(SharedIntegrationHostFixture.RequiredPackageIds).ConfigureAwait(false);
            }
            catch
            {
                _messageFilter.Dispose();
                throw;
            }
        }

        /// <summary>
        /// This method implements <see cref="IAsyncLifetime.DisposeAsync"/>, and is used for releasing resources
        /// created by <see cref="IAsyncLifetime.InitializeAsync"/>. This method is only called if
        /// <see cref="InitializeAsync"/> completes successfully.
        /// </summary>
        public virtual Task DisposeAsync()
        {
            _visualStudioContext.Dispose();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual MessageFilter RegisterMessageFilter()
            => new MessageFilter();

        protected void Wait(double seconds)
        {
            var timeout = TimeSpan.FromMilliseconds(seconds * 1000);
            Thread.Sleep(timeout);
        }

        /// <summary>
        /// This method provides the implementation for <see cref="IDisposable.Dispose"/>. This method via the
        /// <see cref="IDisposable"/> interface (i.e. <paramref name="disposing"/> is <see langword="true"/>) if the
        /// constructor completes successfully. The <see cref="InitializeAsync"/> may or may not have completed
        /// successfully.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _messageFilter.Dispose();
            }
        }

        protected KeyPress Ctrl(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Ctrl);

        protected KeyPress Shift(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Shift);

        protected KeyPress Alt(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Alt);

        protected class MessageFilter : IMessageFilter, IDisposable
        {
            protected const uint CancelCall = ~0U;

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

            public virtual uint HandleInComingCall(uint dwCallType, IntPtr htaskCaller, uint dwTickCount, INTERFACEINFO[] lpInterfaceInfo)
            {
                return (uint)SERVERCALL.SERVERCALL_ISHANDLED;
            }

            public virtual uint RetryRejectedCall(IntPtr htaskCallee, uint dwTickCount, uint dwRejectType)
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

            public virtual uint MessagePending(IntPtr htaskCallee, uint dwTickCount, uint dwPendingType)
            {
                return (uint)PENDINGMSG.PENDINGMSG_WAITDEFPROCESS;
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _messageFilterRegistration.Dispose();
                }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        private sealed class MessageFilterSafeHandle : SafeHandleMinusOneIsInvalid
        {
            private readonly IntPtr _oldFilter;

            private MessageFilterSafeHandle(IntPtr handle)
                : base(true)
            {
                SetHandle(handle);

                try
                {
                    if (CoRegisterMessageFilter(handle, out _oldFilter) != VSConstants.S_OK)
                    {
                        throw new InvalidOperationException("Failed to register a new message filter");
                    }
                }
                catch
                {
                    SetHandleAsInvalid();
                    throw;
                }
            }

            [DllImport("ole32", SetLastError = true)]
            private static extern int CoRegisterMessageFilter(IntPtr messageFilter, out IntPtr oldMessageFilter);

            public static MessageFilterSafeHandle Register<T>(T messageFilter)
                where T : IMessageFilter
            {
                var handle = Marshal.GetComInterfaceForObject<T, IMessageFilter>(messageFilter);
                return new MessageFilterSafeHandle(handle);
            }

            protected override bool ReleaseHandle()
            {
                if (CoRegisterMessageFilter(_oldFilter, out _) == VSConstants.S_OK)
                {
                    Marshal.Release(handle);
                }

                return true;
            }
        }
    }
}
