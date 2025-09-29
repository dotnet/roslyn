// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.CodeAnalysis.Test.Utilities;

/// <summary>
/// This factory creates COM "blind aggregator" instances in managed code.
/// </summary>
public static class BlindAggregatorFactory
{
    public static unsafe IntPtr CreateWrapper()
        => (IntPtr)BlindAggregator.CreateInstance();

    public static unsafe void SetInnerObject(IntPtr wrapperUnknown, IntPtr innerUnknown, IntPtr managedObjectGCHandlePtr)
    {
        var pWrapper = (BlindAggregator*)wrapperUnknown;
        pWrapper->SetInnerObject(innerUnknown, managedObjectGCHandlePtr);
    }

    /// <summary>
    /// A blind aggregator instance. It is allocated in native memory.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct BlindAggregator
    {
        private IntPtr _vfPtr;           // Pointer to the virtual function table
        private int _refCount;           // COM reference count
        private IntPtr _innerUnknown;    // CCW for the managed object supporting aggregation
        private IntPtr _gcHandle;        // The GC Handle to the managed object (the non aggregated object)

        public static unsafe BlindAggregator* CreateInstance()
        {
            var pResult = (BlindAggregator*)Marshal.AllocCoTaskMem(sizeof(BlindAggregator));
            if (pResult != null)
            {
                pResult->Construct();
            }

            return pResult;
        }

        private void Construct()
        {
            _vfPtr = VTable.AddressOfVTable;
            _refCount = 1;
            _innerUnknown = IntPtr.Zero;
            _gcHandle = IntPtr.Zero;
        }

        public void SetInnerObject(IntPtr innerUnknown, IntPtr gcHandle)
        {
            _innerUnknown = innerUnknown;
            Marshal.AddRef(_innerUnknown);
            _gcHandle = gcHandle;
        }

        private void FinalRelease()
        {
            Marshal.Release(_innerUnknown);

            if (_gcHandle != IntPtr.Zero)
            {
                GCHandle.FromIntPtr(_gcHandle).Free();
                _gcHandle = IntPtr.Zero;
            }
        }

        private unsafe delegate int QueryInterfaceDelegateType(BlindAggregator* pThis, [In] ref Guid riid, out IntPtr pvObject);
        private unsafe delegate uint AddRefDelegateType(BlindAggregator* pThis);
        private unsafe delegate uint ReleaseDelegateType(BlindAggregator* pThis);
        private unsafe delegate int GetGCHandlePtrDelegateType(BlindAggregator* pThis, out IntPtr pResult);

        [StructLayout(LayoutKind.Sequential)]
        private struct VTable
        {
            // Need these to keep the delegates alive
            private static readonly unsafe QueryInterfaceDelegateType s_queryInterface = BlindAggregator.QueryInterface;
            private static readonly unsafe AddRefDelegateType s_addRef = BlindAggregator.AddRef;
            private static readonly unsafe ReleaseDelegateType s_release = BlindAggregator.Release;
            private static readonly unsafe GetGCHandlePtrDelegateType s_get_GCHandlePtr = BlindAggregator.GetGCHandlePtr;

            private IntPtr _queryInterfacePtr;
            private IntPtr _addRefPtr;
            private IntPtr _releasePtr;
            private IntPtr _getGCHandlePtr;

            private void Construct()
            {
                _queryInterfacePtr = Marshal.GetFunctionPointerForDelegate(VTable.s_queryInterface);
                _addRefPtr = Marshal.GetFunctionPointerForDelegate(VTable.s_addRef);
                _releasePtr = Marshal.GetFunctionPointerForDelegate(VTable.s_release);
                _getGCHandlePtr = Marshal.GetFunctionPointerForDelegate(VTable.s_get_GCHandlePtr);
            }

            /// <summary>
            /// A 'holder' for a native memory allocation. The allocation is freed in the finalizer.
            /// </summary>
            private sealed class CoTaskMemPtr
            {
                public readonly IntPtr VTablePtr;

                public unsafe CoTaskMemPtr()
                {
                    var ptr = Marshal.AllocCoTaskMem(sizeof(VTable));
                    this.VTablePtr = ptr;
                    ((VTable*)ptr)->Construct();
                }

                ~CoTaskMemPtr()
                    => Marshal.FreeCoTaskMem(this.VTablePtr);
            }

            // Singleton instance of the VTable allocated in native memory. Since it's static, the
            // underlying native memory will be freed when finalizers run at shutdown.
            private static readonly CoTaskMemPtr s_instance = new();

            public static IntPtr AddressOfVTable { get { return s_instance.VTablePtr; } }
        }

        private const int S_OK = 0;
        private const int E_NOINTERFACE = unchecked((int)0x80004002);

        // 00000000-0000-0000-C000-000000000046
        private static readonly Guid s_IUnknownInterfaceGuid = new(0x00000000, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);

        // 00000003-0000-0000-C000-000000000046
        private static readonly Guid s_IMarshalInterfaceGuid = new(0x00000003, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);

        // CBD71F2C-6BC5-4932-B851-B93EB3151386
        private static readonly Guid s_IComWrapperGuid = new("CBD71F2C-6BC5-4932-B851-B93EB3151386");

        private static unsafe int QueryInterface(BlindAggregator* pThis, [In] ref Guid riid, out IntPtr pvObject)
        {
            if (riid == s_IUnknownInterfaceGuid || riid == s_IComWrapperGuid)
            {
                AddRef(pThis);
                pvObject = (IntPtr)pThis;
                return S_OK;
            }
            else if (riid == s_IMarshalInterfaceGuid)
            {
                pvObject = IntPtr.Zero;
                return E_NOINTERFACE;
            }
            else
            {
                // We don't know what the interface is, so aggregate blindly from here
                return Marshal.QueryInterface(pThis->_innerUnknown, ref riid, out pvObject);
            }
        }

        private static unsafe uint AddRef(BlindAggregator* pThis)
            => unchecked((uint)Interlocked.Increment(ref pThis->_refCount));

        private static unsafe uint Release(BlindAggregator* pThis)
        {
            var result = unchecked((uint)Interlocked.Decrement(ref pThis->_refCount));
            if (result == 0u)
            {
                pThis->FinalRelease();
                Marshal.FreeCoTaskMem((IntPtr)pThis);
            }

            return result;
        }

        private static unsafe int GetGCHandlePtr(BlindAggregator* pThis, out IntPtr pResult)
        {
            pResult = pThis->_gcHandle;
            return S_OK;
        }
    }
}

