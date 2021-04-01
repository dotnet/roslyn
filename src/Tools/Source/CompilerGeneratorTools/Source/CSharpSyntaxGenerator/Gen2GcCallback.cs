// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.4/src/libraries/System.Private.CoreLib/src/System/Gen2GcCallback.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace System
{
    /// <summary>
    /// Schedules a callback roughly every gen 2 GC (you may see a Gen 0 an Gen 1 but only once)
    /// (We can fix this by capturing the Gen 2 count at startup and testing, but I mostly don't care)
    /// </summary>
    internal sealed class Gen2GcCallback : CriticalFinalizerObject
    {
        private readonly Func<bool>? _callback0;
        private readonly Func<object, bool>? _callback1;
        private GCHandle _weakTargetObj;

        private Gen2GcCallback(Func<bool> callback)
        {
            _callback0 = callback;
        }

        private Gen2GcCallback(Func<object, bool> callback, object targetObj)
        {
            _callback1 = callback;
            _weakTargetObj = GCHandle.Alloc(targetObj, GCHandleType.Weak);
        }

        /// <summary>
        /// Schedule 'callback' to be called in the next GC.  If the callback returns true it is
        /// rescheduled for the next Gen 2 GC.  Otherwise the callbacks stop.
        /// </summary>
        public static void Register(Func<bool> callback)
        {
            // Create a unreachable object that remembers the callback function and target object.
            _ = new Gen2GcCallback(callback);
        }

        /// <summary>
        /// Schedule 'callback' to be called in the next GC.  If the callback returns true it is
        /// rescheduled for the next Gen 2 GC.  Otherwise the callbacks stop.
        ///
        /// NOTE: This callback will be kept alive until either the callback function returns false,
        /// or the target object dies.
        /// </summary>
        public static void Register(Func<object, bool> callback, object targetObj)
        {
            // Create a unreachable object that remembers the callback function and target object.
            _ = new Gen2GcCallback(callback, targetObj);
        }

        ~Gen2GcCallback()
        {
            if (_weakTargetObj.IsAllocated)
            {
                // Check to see if the target object is still alive.
                object? targetObj = _weakTargetObj.Target;
                if (targetObj == null)
                {
                    // The target object is dead, so this callback object is no longer needed.
                    _weakTargetObj.Free();
                    return;
                }

                // Execute the callback method.
                try
                {
                    Debug.Assert(_callback1 != null);
                    if (!_callback1(targetObj))
                    {
                        // If the callback returns false, this callback object is no longer needed.
                        return;
                    }
                }
                catch
                {
                    // Ensure that we still get a chance to resurrect this object, even if the callback throws an exception.
#if DEBUG
                    // Except in DEBUG, as we really shouldn't be hitting any exceptions here.
                    throw;
#endif
                }
            }
            else
            {
                // Execute the callback method.
                try
                {
                    Debug.Assert(_callback0 != null);
                    if (!_callback0())
                    {
                        // If the callback returns false, this callback object is no longer needed.
                        return;
                    }
                }
                catch
                {
                    // Ensure that we still get a chance to resurrect this object, even if the callback throws an exception.
#if DEBUG
                    // Except in DEBUG, as we really shouldn't be hitting any exceptions here.
                    throw;
#endif
                }
            }

            // Resurrect ourselves by re-registering for finalization.
            GC.ReRegisterForFinalize(this);
        }
    }
}
