// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management;

namespace ProcessWatchdog
{
    /// <summary>
    /// Keeps track of a process and all its descendants.
    /// </summary>
    internal class ProcessTracker : IDisposable
    {
        private readonly Process _parentProcess;
        private List<TrackedProcess> _trackedProcesses;
        private readonly ProcDump _procDump;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessTracker"/> class from the
        /// specified process id.
        /// </summary>
        /// <param name="parentProcess">
        /// The process whose descendants are to be tracked.
        /// </param>
        /// <param name="procDump">
        /// Object responsible for producing memory dumps of any tracked processes that
        /// fail or are terminated.
        /// </param>
        internal ProcessTracker(Process parentProcess, ProcDump procDump)
        {
            _parentProcess = parentProcess;
            _trackedProcesses = new List<TrackedProcess>();
            _procDump = procDump;

            TrackProcess(parentProcess);
        }

        internal bool AllFinished => !_trackedProcesses.Any();

        internal void Update()
        {
            // Clear out any processes which have ended.
            _trackedProcesses = _trackedProcesses.Where(tp => !tp.HasExited).ToList();

            // CAUTION: This code is subject to a race condition where between one
            // call to update and the next, all the processes in the list ended, but new
            // processes (which we are not yet tracking) were created. In that case,
            // _trackedProcesses would now be empty, and the ProcessWatchdog would exit,
            // even though there are still processes we care about.
            //
            // This should not happen for the scenarios we care about, since the parent
            // process should outlive all its descendants.
            if (_trackedProcesses.Any())
            {
                // Add any new descendants of the remaining processes (that is, any
                // descendants that we're not already tracking).
                int[] existingProcessIds = _trackedProcesses.Select(tp => tp.Process.Id).ToArray();

                foreach (Process descendant in GetDescendants(_parentProcess.Id))
                {
                    // BUG: This code is subject to a race condition where, between the time we
                    // captured the existing process ids and the time we enumerated the descendants,
                    // one of those existing processes had terminated and a new descendant process
                    // was created with the same id. This can happen, since process ids can be
                    // recycled.
                    if (!existingProcessIds.Contains(descendant.Id))
                    {
                        TrackProcess(descendant);
                    }
                }
            }
        }

        internal void TerminateAll()
        {
            foreach (TrackedProcess trackedProcess in _trackedProcesses)
            {
                // Launch another procdump process, distinct from the one that has been
                // monitoring the tracked process. This procdump process will take an
                // immediate dump of the tracked process.
                Process immediateDumpProcess = _procDump.DumpProcessNow(
                    trackedProcess.Process.Id,
                    trackedProcess.Description);

                while (!immediateDumpProcess.HasExited)
                    ;

                // Terminate the procdump process that has been monitoring the target process
                // Since this procdump is acting as a debugger, terminating it will
                // terminate the target process as well.
                SafeKillProcess(trackedProcess.ProcDumpProcess);
            }
        }

        private void TrackProcess(Process process)
        {
            string description = MakeProcessDescription(process);
            Process procDumpProcess = _procDump.MonitorProcess(process.Id, description);

            _trackedProcesses.Add(new TrackedProcess(process, procDumpProcess, description));
        }

        private static string MakeProcessDescription(Process process)
        {
            return $"{process.ProcessName}-{process.Id}";
        }

        private IList<Process> GetDescendants(int processId)
        {
            var descendants = new List<Process>();

            string query = string.Format(
                CultureInfo.InvariantCulture,
                "SELECT * FROM Win32_Process WHERE ParentProcessId={0}",
                processId);
            var searcher = new ManagementObjectSearcher(query);

            foreach (ManagementObject process in searcher.Get())
            {
                object descendantIdProperty = process["ProcessId"];
                int descendantId = Convert.ToInt32(descendantIdProperty);

                try
                {
                    Process descendant = Process.GetProcessById(descendantId);
                    descendants.Add(descendant);

                    // Recurse to find descendants of descendants.
                    descendants.AddRange(GetDescendants(descendantId));
                }
                catch (ArgumentException)
                {
                    // Don't worry if the process stopped running between the time we got
                    // its id and the time we tried to get a Process object from the id.
                    // Just don't add it to the list.
                }
            }

            return descendants;
        }

        /// <summary>
        /// Terminate a process safely, avoiding the potential race condition
        /// that is unavoidable when using Process.HasExited.
        /// </summary>
        /// <param name="process">
        /// The process to be terminated.
        /// </param>
        private void SafeKillProcess(Process process)
        {
            if (!process.HasExited)
            {
                try
                {
                    process.Kill();
                }
                catch (InvalidOperationException)
                {
                    // This will happen if the process ended between the call to
                    // Process.HasExited and the call to Process.Kill. It doesn't
                    // indicate an error, so ignore it.
                }
            }
        }

        #region IDisposable Support

        private bool _isDisposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    foreach (TrackedProcess trackedProcess in _trackedProcesses)
                    {
                        // Killing the procdump process will also kill the tracked
                        // process to which it had attached itself as a debugger.
                        SafeKillProcess(trackedProcess.ProcDumpProcess);

                        trackedProcess.Process.Dispose();
                        trackedProcess.ProcDumpProcess.Dispose();
                    }

                    _trackedProcesses = null;
                }

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        /// <summary>
        /// Information about a single process being tracked by a <see cref="ProcessTracker"/>.
        /// </summary>
        private class TrackedProcess
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TrackedProcess"/> class from
            /// the specified process information.
            /// </summary>
            /// <param name="process">
            /// The process being tracked.
            /// </param>
            /// <param name="procDumpProcess">
            /// The procdump process attached to <paramref name="process"/>, and responsible
            /// for producing a memory dump if that process should fail.
            /// </param>
            /// <param name="description">
            /// A string that describes the process, of the form "processName-processId".
            /// </param>
            internal TrackedProcess(Process process, Process procDumpProcess, string description)
            {
                Process = process;
                ProcDumpProcess = procDumpProcess;
                Description = description;
            }

            /// <summary>
            /// Gets a value indicating whether the tracked process has exited.
            /// </summary>
            internal bool HasExited => Process.HasExited;

            /// <summary>
            /// Gets the process being tracked.
            /// </summary>
            internal Process Process { get; }

            /// <summary>
            /// Gets the procdump process attached to <see cref="Process"/>, and responsible
            /// for producing a memory dump if that process should fail..
            /// </summary>
            internal Process ProcDumpProcess { get; }

            /// <summary>
            /// Gets a string that describes the process, of the form "processName-processId".
            /// </summary>
            internal string Description { get; }
        }
    }
}