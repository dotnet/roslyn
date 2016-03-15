// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ProcessWatchdog
{
    /// <summary>
    /// Keeps track of a process and all its descendants.
    /// </summary>
    internal class ProcessTracker : IDisposable
    {
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
            _trackedProcesses = new List<TrackedProcess>();
            _procDump = procDump;

            TrackProcess(parentProcess);
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

        internal bool AllFinished => !_trackedProcesses.Any();

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
                        trackedProcess.Process.Dispose();
                        trackedProcess.ProcDumpProcess.Dispose();
                    }

                    _trackedProcesses = null;
                }

                _isDisposed = true;
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
                if (!trackedProcess.ProcDumpProcess.HasExited)
                {
                    trackedProcess.ProcDumpProcess.Kill();
                }
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
