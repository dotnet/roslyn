// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal abstract class AutomationRetryWrapper<T> : IRetryWrapper, ICustomQueryInterface
    {
        internal const int UIA_E_ELEMENTNOTAVAILABLE = unchecked((int)0x80040201);

        /// <summary>
        /// The number of times to retry a UI automation operation that failed with
        /// <see cref="UIA_E_ELEMENTNOTAVAILABLE"/>, not counting the initial call. A value of 9 means the operation
        /// will be attempted a total of ten times.
        /// </summary>
        private const int AutomationRetryCount = 9;

        /// <summary>
        /// The delay between retrying a UI automation operation that failed with
        /// <see cref="UIA_E_ELEMENTNOTAVAILABLE"/>.
        /// </summary>
        private static readonly TimeSpan AutomationRetryDelay = TimeSpan.FromMilliseconds(100);

        private static readonly Guid IID_IManagedObject = new Guid("C3FCC19E-A970-11D2-8B5A-00A0C9B7C9C4");

        protected AutomationRetryWrapper(T automationObject)
        {
            AutomationObject = automationObject;
            var comCallableWrapper = Marshal.GetIUnknownForObject(automationObject);
            try
            {
                var aggregatedObject = Marshal.CreateAggregatedObject(comCallableWrapper, this);
                try
                {
                    RuntimeCallableWrapper = (T)Marshal.GetObjectForIUnknown(aggregatedObject);
                }
                finally
                {
                    Marshal.Release(aggregatedObject);
                }
            }
            finally
            {
                Marshal.Release(comCallableWrapper);
            }
        }

        protected T AutomationObject
        {
            get;
        }

        internal T RuntimeCallableWrapper
        {
            get;
        }

        object IRetryWrapper.WrappedObject => AutomationObject;

        protected void Retry(Action<T> action)
        {
            Retry(obj =>
            {
                action(AutomationObject);
                return 0;
            });
        }

        protected TResult Retry<TResult>(Func<T, TResult> function)
        {
            // NOTE: The loop termination condition on failure is the exception not matching the exception filter
            for (var i = 0; true; i++)
            {
                try
                {
                    return WrapIfNecessary(function(AutomationObject));
                }
                catch (COMException e) when (e.HResult == UIA_E_ELEMENTNOTAVAILABLE && i < AutomationRetryCount)
                {
                    Thread.Sleep(AutomationRetryDelay);
                    continue;
                }
            }
        }

        private TResult WrapIfNecessary<TResult>(TResult obj)
        {
            if (obj is IRetryWrapper)
            {
                return obj;
            }
            else if (obj is object[] objArray)
            {
                for (var i = 0; i < objArray.Length; i++)
                {
                    objArray[i] = WrapIfNecessary(objArray[i]);
                }

                return obj;
            }
            else
            {
                return AutomationRetryWrapper.WrapIfNecessary(obj);
            }
        }

        CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
        {
            if (iid == IID_IManagedObject)
            {
                // force this object to be treated as an unmanaged COM object
                ppv = IntPtr.Zero;
                return CustomQueryInterfaceResult.Failed;
            }

            var unk = Marshal.GetIUnknownForObject(this);

            if (iid == typeof(IRetryWrapper).GUID || iid == typeof(T).GUID)
            {
                ppv = unk;
                return CustomQueryInterfaceResult.Handled;
            }

            try
            {
                if (ErrorHandler.Succeeded(Marshal.QueryInterface(unk, ref iid, out ppv)))
                {
                    ppv = AutomationRetryWrapper.WrapNativeIfNecessary(iid, ppv);
                    return CustomQueryInterfaceResult.Handled;
                }

                ppv = IntPtr.Zero;
                return CustomQueryInterfaceResult.Failed;
            }
            finally
            {
                Marshal.Release(unk);
            }
        }
    }
}
