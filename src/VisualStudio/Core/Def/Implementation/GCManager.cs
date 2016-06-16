// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    /// <summary>
    /// This class manages setting the GC mode to SustainedLowLatency.
    /// 
    /// It is safe to call from any thread, but is intended to be called from
    /// the UI thread whenever user keyboard or mouse input is received.
    /// </summary>
    internal static class GCManager
    {
        // The default delay can be overridden by setting <VS Registry>\Performance : SustainedLowLatencyDuration
        private static int s_delayMilliseconds = 0;
        private const int DefaultDelayMilliseconds = 5000;

        private static ResettableDelay s_delay;

        static GCManager()
        {
            // Allow disabling SustainedLowLatency by setting the reg key value to 0
            System.Threading.Tasks.Task.Run(() =>
            {
                using (var root = VSRegistry.RegistryRoot(__VsLocalRegistryType.RegType_UserSettings))
                {
                    if (root != null)
                    {
                        using (var key = root.OpenSubKey("Performance"))
                        {
                            const string name = "SustainedLowLatencyDuration";
                            if (key != null && key.GetValue(name) != null && key.GetValueKind(name) == Microsoft.Win32.RegistryValueKind.DWord)
                            {
                                s_delayMilliseconds = (int)key.GetValue(name, s_delayMilliseconds);
                                return;
                            }
                        }
                    }
                }

                s_delayMilliseconds = DefaultDelayMilliseconds;
            });
        }

        /// <summary>
        /// Turn off low latency GC mode.
        /// 
        /// if there is a pending low latency mode request, Latency mode will go back to its original status as
        /// pending request timeout. once it goes back to its original status, it will not go back to low latency mode again.
        /// </summary>
        internal static void TurnOffLowLatencyMode()
        {
            if (s_delayMilliseconds <= 0)
            {
                // if it is already turned off, we don't do anything.
                return;
            }

            // first set delay to 0 to turn it off
            s_delayMilliseconds = 0;

            // explictly call Full GC to remove impact of SustainedLowLatency
            // this is based on finding on https://github.com/dotnet/roslyn/issues/6802
            // one of reason we do this here is so that GC do compact on fragmented memory.
            // which will in return let us have bigger continuous free memory chunk.
            //
            // we do this 5 times to make sure we release all pending memories. something
            // most of our tests do. previous collect can put objects in finalizer and running
            // finalizer can release even more memory so we repeat this 5 times.
            for (var i = 0; i < 5; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        /// <summary>
        /// Call this method to suppress expensive blocking Gen 2 garbage GCs in
        /// scenarios where high-latency is unacceptable (e.g. processing typing input).
        /// 
        /// Blocking GCs will be re-enabled automatically after a short duration unless 
        /// UseLowLatencyModeForProcessingUserInput is called again.
        /// </summary>
        internal static void UseLowLatencyModeForProcessingUserInput()
        {
            if (s_delayMilliseconds <= 0)
            {
                // The registry key to opt out of Roslyn's SustainedLowLatency is set, or
                // we haven't yet initialized the value.
                return;
            }

            var currentMode = GCSettings.LatencyMode;
            var currentDelay = s_delay;
            if (currentMode != GCLatencyMode.SustainedLowLatency)
            {
                GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

                // Restore the LatencyMode a short duration after the
                // last request to UseLowLatencyModeForProcessingUserInput.
                currentDelay = new ResettableDelay(s_delayMilliseconds);
                currentDelay.Task.SafeContinueWith(_ => RestoreGCLatencyMode(currentMode), TaskScheduler.Default);
                s_delay = currentDelay;
            }

            if (currentDelay != null)
            {
                currentDelay.Reset();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.GC.Collect", Justification = "We are using GCCollectionMode.Optimized and this is called on a threadpool thread.")]
        private static void RestoreGCLatencyMode(GCLatencyMode originalMode)
        {
            GCSettings.LatencyMode = originalMode;
            s_delay = null;

            // hint to the GC that if it postponed a gen 2 collection, now might be a good time to do it.
            GC.Collect(2, GCCollectionMode.Optimized);
        }
    }
}
