// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Interop
{
    internal struct WeakComHandle<THandle, TObject>
        where THandle : class
        where TObject : class, THandle
    {
        // NOTE: The logic here is a little bit tricky.  We can't just keep a WeakReference to
        // something like a ComHandle, since it's not something that our clients keep alive.
        // instead, we keep a weak reference to the inner managed object, which we know will
        // always be alive if the outer aggregate is alive.  We can't just keep a WeakReference
        // to the RCW for the outer object either, since in cases where we have a DCOM or native
        // client, the RCW will be cleaned up, even though there is still a native reference
        // to the underlying native outer object.
        //
        // Instead we make use of an implementation detail of the way the CLR's COM aggregation 
        // works.  Namely, if all references to the aggregated object are released, the CLR 
        // responds to QI's for IUnknown with a different object.  So, we store the original
        // value, when we know that we have a client, and then we use that to compare to see
        // if we still have a client alive.
        //
        // NOTE: This is _NOT_ AddRef'd.  We use it just to store the integer value of the
        // IUnknown for comparison purposes.
        private readonly WeakReference _managedObjectWeakReference;
        private readonly IntPtr _pUnkOfInnerUnknownWhenAlive;

        public WeakComHandle(THandle comAggregateObject)
        {
            if (comAggregateObject == null)
            {
                _managedObjectWeakReference = null;
                _pUnkOfInnerUnknownWhenAlive = IntPtr.Zero;
            }

            var pUnk = IntPtr.Zero;
            var managedObject = ComAggregate.GetManagedObject<TObject>(comAggregateObject);

            try
            {
                pUnk = Marshal.GetIUnknownForObject(managedObject);
                _pUnkOfInnerUnknownWhenAlive = pUnk;
            }
            finally
            {
                if (pUnk != IntPtr.Zero)
                {
                    Marshal.Release(pUnk);
                }
            }

            _managedObjectWeakReference = new WeakReference(managedObject);
        }

        public WeakComHandle(ComHandle<THandle, TObject> handle)
        {
            var pUnk = IntPtr.Zero;
            try
            {
                pUnk = Marshal.GetIUnknownForObject(handle.Object);
                _pUnkOfInnerUnknownWhenAlive = pUnk;
            }
            finally
            {
                if (pUnk != IntPtr.Zero)
                {
                    Marshal.Release(pUnk);
                }
            }

            _managedObjectWeakReference = new WeakReference(handle.Object);
        }

        public THandle ComAggregateObject
        {
            get
            {
                // This is pretty fragile code, watch carefully for race conditions!
                var pUnk = IntPtr.Zero;
                try
                {
                    if (_managedObjectWeakReference == null)
                    {
                        return null;
                    }

                    // Copy target locally to make sure other thread won't delete it before we use it
                    var target = _managedObjectWeakReference.Target;
                    if (target == null)
                    {
                        return null;
                    }

                    pUnk = Marshal.GetIUnknownForObject(target);
                    if (pUnk == _pUnkOfInnerUnknownWhenAlive)
                    {
                        // QueryInterface on COM aggregate might fail during shutdown, so we 
                        // defensively use "as" instead of casting (see Dev10 816848).
                        return Marshal.GetObjectForIUnknown(pUnk) as THandle;
                    }
                    else
                    {
                        return null;
                    }
                }
                finally
                {
                    if (pUnk != IntPtr.Zero)
                    {
                        Marshal.Release(pUnk);
                    }
                }
            }
        }

        internal bool TryGetManagedObjectWithoutCaringWhetherNativeObjectIsAlive(out TObject managedObject)
        {
            // NOTE: Only use this method if you do NOT care whether the native ComAggregate
            // object has already been released.
            if (_managedObjectWeakReference == null)
            {
                managedObject = null;
                return false;
            }

            managedObject = _managedObjectWeakReference.Target as TObject;
            return managedObject != null;
        }

        public ComHandle<THandle, TObject>? ComHandle
        {
            get
            {
                var rcw = this.ComAggregateObject;
                if (rcw == null)
                {
                    return null;
                }

                Debug.Assert(_managedObjectWeakReference != null);
                if (_managedObjectWeakReference.Target is TObject managedObject)
                {
                    // Construct a new ComHandle without going through the cycle of unwrapping
                    // the managed object from the rcw, that has shown to be a perf concern for 
                    // progression (see Dev10 Bug 628992).
                    return new ComHandle<THandle, TObject>(rcw, managedObject);
                }
                else
                {
                    // We fall back to trying to unwrap the managed object out of the rcw, but
                    // the Weakref to the managed object shouldn't go null if the rcw is still
                    // alive, should it?
                    Debug.Fail("Can this really happen?");
                    return new ComHandle<THandle, TObject>(rcw);
                }
            }
        }

        public bool IsAlive()
        {
            if (_managedObjectWeakReference == null)
            {
                return false;
            }

            return _managedObjectWeakReference.IsAlive;
        }
    }
}
