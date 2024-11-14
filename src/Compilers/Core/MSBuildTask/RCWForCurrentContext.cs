// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable CA1416 // Validate platform compatibility (Windows only APIs)

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// Create an RCW for the current context/apartment. 
    /// This improves performance of cross apartment calls as the CLR will only
    /// cache marshalled pointers for an RCW created in the current context.
    /// </summary>
    /// <typeparam name="T">Type of the RCW object</typeparam>
    internal class RCWForCurrentContext<T> : IDisposable where T : class
    {
        /// <summary>
        /// The last RCW that was created for the current context.
        /// </summary>
        private T? _rcwForCurrentCtx;

        /// <summary>
        /// Indicates if we created the RCW and therefore need to release it's com reference.
        /// </summary>
        private readonly bool _shouldReleaseRCW;

        /// <summary>
        /// Constructor creates the new RCW in the current context.
        /// </summary>
        /// <param name="rcw">The RCW created in the original context.</param>
        public RCWForCurrentContext(T rcw)
        {
            // To improve performance we create a new RCW for the current context so we get 
            // the caching behavior of the marshaled pointer. 
            // See RCW::GetComIPForMethodTableFromCache in ndp\clr\src\VM\RuntimeCallableWrapper.cpp
            IntPtr iunknownPtr = Marshal.GetIUnknownForObject(rcw);
            Object? objInCurrentCtx = null;

            try
            {
                objInCurrentCtx = Marshal.GetObjectForIUnknown(iunknownPtr);
            }
            finally
            {
                Marshal.Release(iunknownPtr);
            }

            Debug.Assert(objInCurrentCtx != null, "Unable to marshal COM Object to the current context (apartment). This will hurt performance.");

            // If we failed to create the new RCW we default to returning the original RCW.
            if (objInCurrentCtx == null)
            {
                _shouldReleaseRCW = false;
                _rcwForCurrentCtx = rcw;
            }
            else
            {
                _shouldReleaseRCW = true;
                _rcwForCurrentCtx = objInCurrentCtx as T;
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~RCWForCurrentContext()
        {
            Debug.Fail("This object requires explicit call to Dispose");
            CleanupComObject();
        }

        /// <summary>
        /// Call this helper if your managed object is really an RCW to a COM object
        /// and that COM object was created in a different apartment from where it is being accessed
        /// </summary>
        /// <returns>A new RCW created in the current apartment context</returns>
        public T RCW
        {
            get
            {
                if (null == _rcwForCurrentCtx)
                {
                    throw new ObjectDisposedException("RCWForCurrentCtx");
                }

                return _rcwForCurrentCtx;
            }
        }

        /// <summary>
        /// Override for IDisposable::Dispose
        /// </summary>
        /// <remarks>
        /// We created an RCW for the current apartment. When this object goes out of scope
        /// we need to release the COM object before the apartment is released (via COUninitialize)
        /// </remarks>
        public void Dispose()
        {
            CleanupComObject();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Cleanup our RCW com object references if required.
        /// </summary>
        private void CleanupComObject()
        {
            try
            {
                if (null != _rcwForCurrentCtx &&
                    _shouldReleaseRCW &&
                    Marshal.IsComObject(_rcwForCurrentCtx))
                {
#if NET
                    Debug.Assert(OperatingSystem.IsWindows());
#endif
                    Marshal.ReleaseComObject(_rcwForCurrentCtx);
                }
            }
            finally
            {
                _rcwForCurrentCtx = null;
            }
        }
    }
}
