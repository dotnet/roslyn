// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Roslyn.Utilities
{
    internal static class DebuggerUtilities
    {
        /// <summary>
        /// The issue here is acquiring a lock in the course of evaluating a property
        /// value in the debugger (e.g. in the Locals window).  If anything causes that
        /// evaluation to bail, it will do so without releasing the lock, making future
        /// evaluations impossible (leads to a timeout, among other things).  One thing
        /// that might cause the evaluation to bail is a call to 
        /// Debugger.NotifyOfCrossThreadDependency, which causes the debugger to prompt
        /// the user for confirmation (little swirling red and blue icon) before evaluating
        /// an expression that will involve multiple threads.  To prevent this from happening
        /// we make the call ourselved *before* acquiring the lock.  Then, when the user
        /// opts to proceed, the evaluation runs without interruption and succeeds.
        /// </summary>
        /// <remarks>
        /// TODO: This probably isn't necessary in Dev11 (see Dev11 548767 and/or Dev11 84313).
        /// </remarks>
        internal static void CallBeforeAcquiringLock()
        {
            Debugger.NotifyOfCrossThreadDependency();
        }
    }
}