// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Interop
{
    /// <summary>
    /// Holds onto a managed object as well as the CCW for that object if there is one.
    /// </summary>
    /// <typeparam name="THandle">The COM interface type to keep a reference to</typeparam>
    /// <typeparam name="TObject">The managed object type to keep a reference to</typeparam>
    internal struct ComHandle<THandle, TObject>
        where THandle : class
        where TObject : class, THandle
    {
        private readonly THandle _handle;
        private readonly TObject _managedObject;

        /// <summary>
        /// Create an instance from a "ComObject" or from a managed object.
        /// </summary>
        public ComHandle(THandle handleOrManagedObject)
        {
            if (handleOrManagedObject == null)
            {
                _handle = null;
                _managedObject = null;
            }
            else if (Marshal.IsComObject(handleOrManagedObject))
            {
                _handle = handleOrManagedObject;
                _managedObject = ComAggregate.GetManagedObject<TObject>(handleOrManagedObject);
            }
            else
            {
                _handle = (THandle)ComAggregate.TryGetWrapper(handleOrManagedObject);
                _managedObject = (TObject)handleOrManagedObject;
            }
        }

        public ComHandle(THandle handle, TObject managedObject)
        {
            if (handle == null && managedObject == null)
            {
                _handle = null;
                _managedObject = null;
            }
            else
            {
                // NOTE: This might get triggered if you do testing with the "NoWrap"
                // ComAggregatePolicy, since both handle will not be a ComObject in that
                // case.
                if (handle != null && !Marshal.IsComObject(handle))
                {
                    throw new ArgumentException("must be null or a Com object", nameof(handle));
                }

                _handle = handle;
                _managedObject = managedObject;
            }
        }

        /// <summary>
        /// Return the IComWrapper object (as T) or the managed object (as T) if the managed object is not wrapped.
        /// </summary>
        public THandle Handle
        {
            get
            {
                Debug.Assert(_handle == null || Marshal.IsComObject(_handle), "Invariant broken!");

                if (_handle == null)
                {
                    return _managedObject;
                }
                else
                {
                    return _handle;
                }
            }
        }

        /// <summary>
        /// Return the managed object
        /// </summary>
        public TObject Object
        {
            get
            {
                return _managedObject;
            }
        }

        public ComHandle<TNewHandle, TNewObject> Cast<TNewHandle, TNewObject>()
            where TNewHandle : class
            where TNewObject : class, TNewHandle
        {
            if (!(Handle is TNewHandle newHandle))
            {
                throw new InvalidOperationException("Invalid cast.");
            }

            if (!(Object is TNewObject newObject))
            {
                throw new InvalidOperationException("Invalid cast.");
            }

            return new ComHandle<TNewHandle, TNewObject>(newHandle, newObject);
        }
    }
}
