// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal abstract class AutomationRetryWrapper<T> : IRetryWrapper, ICustomQueryInterface
    {
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

        protected TResult Retry<TResult>(Func<T, TResult> action)
        {
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                try
                {
                    return WrapIfNecessary(action(AutomationObject));
                }
                catch (COMException e) when (e.ErrorCode == AutomationElementExtensions.UIA_E_ELEMENTNOTAVAILABLE)
                {
                    if (stopwatch.Elapsed < Helper.HangMitigatingTimeout)
                        continue;

                    throw;
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
