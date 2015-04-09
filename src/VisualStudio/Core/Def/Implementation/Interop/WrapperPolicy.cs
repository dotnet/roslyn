// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Interop
{
    internal static class WrapperPolicy
    {
        /// <summary>
        /// Factory object for creating IComWrapper instances. Uses the Visual Studio implementation,
        /// if available, or falls back to using our own implementation based on <see cref="BlindAggregatorFactory"/>
        /// </summary>
        private static readonly IComWrapperFactory s_ComWrapperFactory =
            PackageUtilities.CreateInstance(typeof(IComWrapperFactory).GUID) as IComWrapperFactory
            ?? new ComWrapperFactory();

        internal static object CreateAggregatedObject(object managedObject) => s_ComWrapperFactory.CreateAggregatedObject(managedObject);

        private class ComWrapperFactory : IComWrapperFactory
        {
            private int _aggregatedObjectsCreatedSinceLastCleanup = 0;

            private void OnAggregatedObjectCreated()
            {
                // The VS main thread disables eager COM object cleanup, which means that
                // COM objects are only cleaned up when Marshal.CleanupUnusedObjectsInCurrentContext
                // is called.  The shell normally does this on idle, but in long lived 
                // code model operations, we end up creating enough code model objects that
                // we can run out of memory before an idle+gc actually happens.
                //
                // To work around this, we track when we create aggregated objects (currently only)
                // really used for the codemodel RCW's, and call 
                // Marshal.CleanupUnusedObjectsInCurrentContext periodically.  This is safe because
                // we're only cleaning things up for a given context, which means that there is no
                // need to pump to get to the right context, so it shouldn't cause unexpected 
                // re-entrancy issues.
                //
                // The "1000" is completely arbitrary, except that it seems to work well when looking at
                // http://vstfdevdiv:8080/WorkItemTracking/WorkItem.aspx?artifactMoniker=711863

                if (_aggregatedObjectsCreatedSinceLastCleanup++ == 1000)
                {
                    Marshal.CleanupUnusedObjectsInCurrentContext();
                    _aggregatedObjectsCreatedSinceLastCleanup = 0;
                }
            }

            public object CreateAggregatedObject(object managedObject)
            {
                Contract.ThrowIfNull(managedObject, "managedObject");

                // 1. Create our native COM object that will aggregate "managedObject"
                var wrapperUnknown = BlindAggregatorFactory.CreateWrapper(); // AddRef'ed

                try
                {
                    // 2. Ask the CLR to create an object supporting aggregation for "managedObject"
                    var innerUnknown = Marshal.CreateAggregatedObject(wrapperUnknown, managedObject); // AddRef'ed
                    try
                    {
                        // 3. Create a GC Handle to the managed object for later retrieval
                        var handle = GCHandle.Alloc(managedObject, GCHandleType.Normal);
                        var freeHandle = true;
                        try
                        {
                            // 4. Now, link our native (aggregator) with the IUnknown of the CLR inner object, the
                            //    GC Handle managed and the GC handle of the managed object.
                            //    GC handle will be free by the native wrapper.
                            BlindAggregatorFactory.SetInnerObject(wrapperUnknown, innerUnknown, GCHandle.ToIntPtr(handle));
                            freeHandle = false;
                        }
                        finally
                        {
                            if (freeHandle)
                            {
                                handle.Free();
                            }
                        }

                        OnAggregatedObjectCreated();

                        // 5. All done: Ask the CLR to create an RCW for the native aggregator
                        object wrapperRCW = Marshal.GetObjectForIUnknown(wrapperUnknown);
                        return (IComWrapper)wrapperRCW;
                    }
                    finally
                    {
                        Marshal.Release(innerUnknown);
                    }
                }
                finally
                {
                    Marshal.Release(wrapperUnknown);
                }
            }
        }

        /// <summary>
        /// Return the RCW for the native IComWrapper instance aggregating "managedObject"
        /// if there is one. Return "null" if "managedObject" is not aggregated.
        /// </summary>
        internal static IComWrapper TryGetWrapper(object managedObject)
        {
            // Note: this method should be "return managedObject" once we can get rid of this while IComWrapper
            // business.

            // This force the CLR to retrieve the "outer" object of "managedObject"
            // if "managedObject" has been aggregated
            var ptr = Marshal.GetIUnknownForObject(managedObject);
            try
            {
                // This asks the CLR to return the RCW correspoding to the
                // aggregator object.
                object wrapper = Marshal.GetObjectForIUnknown(ptr);

                // The aggregator (if there is one) implement IComWrapper!
                return wrapper as IComWrapper;
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }
    }
}
