// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Watson
{
    /// <summary>
    /// Helper for filing non-fatal Watson reports.
    /// </summary>
    internal class WatsonErrorReport : IDisposable
    {
        /// <summary>
        /// The minimum interval that must pass between individual error submissions for the same failed component.
        /// </summary>
        /// <remarks>
        /// This is important so we don't slam the WER servers from a single dev box that keeps crashing.
        /// Particularly when the failing code happens to be in a loop or on multiple threads, we don't want to get
        /// the same crash over and over.
        /// </remarks>
        private static readonly TimeSpan s_minimumSubmissionInterval = TimeSpan.FromHours(1);

        /// <summary>
        /// A record of when a given component last submitted an error report in this app domain's lifetime.  
        /// </summary>
        /// <remarks>
        /// Used for throttling report submissions.
        /// </remarks>
        private static readonly Dictionary<string, DateTime> s_lastReportSubmissionByComponent = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Info describing source of this non-fatal error
        /// </summary>
        private ExceptionInfo _exceptionInfo;

#if !SILVERLIGHT
        /// <summary>
        /// <see cref="IntPtr"/> pointer to ExceptionPointers structure that is created when the exception is thrown (required for submission to watson)
        /// </summary>
        /// <remarks>
        /// We do not need to clean this memory up because it will be cleaned up at the conclusion of the exception handling
        /// </remarks>
        private IntPtr _exceptionPointersPointer = IntPtr.Zero;

        /// <summary>
        /// WatsonBucket EventType for all non-fatal devenv errors we use "Dev11NonFatalError"
        /// </summary>
        private const string WatsonEventType = "Dev11NonFatalError";

        /// <summary>
        /// Id generated when opening the event handle used to identify this request
        /// </summary>
        private string _snapshotId;

        /// <summary>
        /// Open, Inheritable handle to the event that will be used to signal snapshotting complete
        /// </summary>
        private IntPtr _eventHandle = IntPtr.Zero;

        /// <summary>
        /// Open, Inheritable handle to this process (this is the process that will be snapshotted)
        /// </summary>
        private IntPtr _processHandleDupe = IntPtr.Zero;

        /// <summary>
        /// Open, Inheritable handle to this thread (used by watson to identify the thread where the error happened
        /// </summary>
        private IntPtr _threadHandleDupe = IntPtr.Zero;
#endif

        /// <summary>
        /// Bool indicating if <see cref="Dispose()"/> has been called on this instance
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="WatsonErrorReport"/> class.
        /// </summary>
        private WatsonErrorReport()
        {
        }

        /// <summary>
        /// Releases native resources.
        /// </summary>
        ~WatsonErrorReport()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "NativeWin32Stubs.CloseHandle(System.IntPtr)", Justification = "It doesn't matter what the HRESULT is, we should always invalidate thread handle")]
        protected virtual void Dispose(bool disposing)
        {
            _disposed = true;

#if !SILVERLIGHT
            // This pointer is no longer valid outside of the exception filter
            _exceptionPointersPointer = IntPtr.Zero;
            if (_eventHandle != IntPtr.Zero)
            {
                NativeWin32Stubs.CloseHandle(_eventHandle);
                _eventHandle = IntPtr.Zero;
            }
            if (_processHandleDupe != IntPtr.Zero)
            {
                NativeWin32Stubs.CloseHandle(_processHandleDupe);
                _processHandleDupe = IntPtr.Zero;
            }
            if (_threadHandleDupe != IntPtr.Zero)
            {
                NativeWin32Stubs.CloseHandle(_threadHandleDupe);
                _threadHandleDupe = IntPtr.Zero;
            }
#endif
        }

        /// <summary>
        /// This Initializes a new instance of <see cref="WatsonErrorReport"/> that must be disposed.
        /// </summary>
        /// <param name="exceptionInfo">Exception info describing this error</param>
        /// <returns>New error report instance</returns>
        internal static WatsonErrorReport CreateNonFatalReport(ExceptionInfo exceptionInfo)
        {
            var report = new WatsonErrorReport();

            report._exceptionInfo = exceptionInfo;

            return report;
        }

        /// <summary>
        /// If not throttled, this fires off a report request and waits for the snapshot to be taken. 
        /// If throttled or a snapshot is not taken due to a failure this will return false.
        /// </summary>
        /// <remarks>
        /// This must be called from a exception filter to get the pointer to the ExceptionPointers structure.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this object has already been disposed
        /// </exception>
        internal bool ReportIfNecessary()
        {
            if (_disposed)
            {
                throw new InvalidOperationException("Cannot submit report from disposed instance");
            }

            bool reportSubmitted = false;

#if !SILVERLIGHT
            // check that this is windows blue or later if so the snapshot API will exist and we can continue
            if ((Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 3) || Environment.OSVersion.Version.Major > 6)
            {
                // Check if the current component has been throttled
                if (CheckThrottledSubmission(_exceptionInfo.ImplementationName))
                {
                    if (InitializeHandlesForSnapshot())
                    {
                        reportSubmitted = ReportException();
                    }
                }
            }
#endif

            return reportSubmitted;
        }

#if !SILVERLIGHT
        /// <summary>
        /// Sets up for taking a snapshot by initializing the necessary handles for the helper process
        /// </summary>
        /// <remarks>
        /// This must be called from an Exception Filter inorder to gather the current exception information.
        /// </remarks>
        /// <returns>true on success and false on failure</returns>
        private unsafe bool InitializeHandlesForSnapshot()
        {
            // Grab the pointer to the exception
            _exceptionPointersPointer = Marshal.GetExceptionPointers();

            NativeWin32Stubs.SECURITY_ATTRIBUTES secAttrib = new NativeWin32Stubs.SECURITY_ATTRIBUTES();
            secAttrib.bInheritHandle = true;

            // Prepare id (ThreadId_DateTime) is sufficient to uniquely identify this snapshot request (used by helper process to name the dump file)
            _snapshotId = string.Format("{0}_{1}", Thread.CurrentThread.ManagedThreadId, unchecked((ulong)DateTime.Now.ToBinary()));

            // pointer to the security attributes structure
            IntPtr pSecAttrib = IntPtr.Zero;
            // handle to this thread
            IntPtr hThread = IntPtr.Zero;
            try
            {
                // allocate some native to accommodate the SECURITY_ATTRIBUTES structure needed to create the event object and get its handle
                pSecAttrib = Marshal.AllocHGlobal(sizeof(NativeWin32Stubs.SECURITY_ATTRIBUTES));
                if (pSecAttrib == IntPtr.Zero)
                {
                    return false;
                }

                // copy the managed structure into native
                Marshal.StructureToPtr(secAttrib, pSecAttrib, true);

                // Create event object in the OS (named eventHandleName) and get the handle to it
                _eventHandle = NativeWin32Stubs.CreateEvent(pSecAttrib, false, false, null);
                if (_eventHandle == IntPtr.Zero)
                {
                    return false;
                }

                // Get current thread and process' handle and duplicate them (when you open the handle on a System.Process you must dispose it)
                using (Process thisProc = Process.GetCurrentProcess())
                {
                    // Grab the handle to this thread
                    hThread = NativeWin32Stubs.GetCurrentThread();
                    if (hThread == IntPtr.Zero)
                    {
                        return false;
                    }

                    IntPtr hThisProc = thisProc.Handle;

                    // duplicate the thread handle
                    if (!NativeWin32Stubs.DuplicateHandle(hThisProc, hThread, hThisProc, out _threadHandleDupe, 0, true, (uint)NativeWin32Stubs.DESIRED_ACCESS.DUPLICATE_SAME_ACCESS) || _threadHandleDupe == IntPtr.Zero)
                    {
                        return false;
                    }

                    // duplicate the process handle
                    if (!NativeWin32Stubs.DuplicateHandle(hThisProc, hThisProc, hThisProc, out _processHandleDupe, 0, true, (uint)NativeWin32Stubs.DESIRED_ACCESS.DUPLICATE_SAME_ACCESS) || _processHandleDupe == IntPtr.Zero)
                    {
                        return false;
                    }
                }
            }
            finally // cleanup the temp handles and native memory we allocated
            {
                if (pSecAttrib != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pSecAttrib);
                }
                if (hThread != IntPtr.Zero)
                {
                    NativeWin32Stubs.CloseHandle(hThread);
                }
            }

            return true;
        }

        /// <summary>
        /// Reports the current non-fatal exception using a process snapshot (if InitializeHandlesForSnapshot has been called)
        /// </summary>
        /// <returns>true if the report was successfully taken, false otherwise</returns>
        private bool ReportException()
        {
            bool reportTaken = false;

            string dbgHelperPath;
            // If we found the helper exe lets call it
            if (IsHelperExeFound(out dbgHelperPath))
            {
                using (Process p = new Process())
                {
                    // assemble the arguments to the helper, if this returns false we can not proceed
                    string helperArguments;
                    if (PrepareHelperArguments(out helperArguments))
                    {
                        p.StartInfo = new ProcessStartInfo(dbgHelperPath, helperArguments)
                        {
                            // Prevent the helper process from spawning a new window
                            CreateNoWindow = true,

                            // When UseShellExecute is off, the .NET framework will always call CreateProcess with the bInheritHandles true, which is required to trigger the event
                            UseShellExecute = false,
                        };

                        p.Start();

                        if (_eventHandle != IntPtr.Zero)
                        {
                            // wait for the event trigger from the helper or the helper to exit (a maximum of 10 second) 
                            // we should keep this timeout low because the IDE will be hung while waiting on this
                            IntPtr[] handles = new IntPtr[] { _eventHandle, p.Handle };

                            // This should be the landing point for all non-fatal watson dumps, 
                            // navigate up the callstack to identify the source of the exception
                            UInt32 result = NativeWin32Stubs.WaitForMultipleObjects((UInt32)handles.Length, handles, false, 10000);

                            // If we get an error result back the wait timed out and the report was not taken in time
                            // or if the wait was triggered by the process exiting
                            reportTaken = result == NativeWin32Stubs.WAIT_OBJECT_0;
                        }
                        else // we don't have an eventHandle to wait on so we should just continue
                        {
                            reportTaken = true;
                        }
                    }
                }
            }

            return reportTaken;
        }

        /// <summary>
        /// Locates the helper required to take the snapshot
        /// </summary>
        /// <param name="filePath">[Optional, Out] if this method returns true this will be the path to the helper exe</param>
        /// <returns>true if the exe was found, false otherwise</returns>
        private static bool IsHelperExeFound(out string filePath)
        {
            string dbgHelperExe = "VsDebugWERHelper.exe";
            filePath = null;

            // Try and get the path to helper exe, which should be next to Microsoft.VisualStudio.Debugger.Engine.dll (the code where this is compiled to)
            var thisModule = Process.GetCurrentProcess()?.MainModule;
            if (thisModule != null)
            {
                string exeDir = Path.GetDirectoryName(thisModule.FileName);
                filePath = Path.Combine(exeDir, dbgHelperExe);
                return File.Exists(filePath);
            }
            return false;
        }

        /// <summary>
        /// Assembles the Watson bucket parameters.
        /// </summary>
        /// <param name="bucketParameters">Receives the bucket parameters.</param>
        /// <returns>A value indicating whether error details were successfully collected.</returns>
        /// <remarks>
        /// NOTE, this method should be called from the filter of an exception block.  Otherwise the runtime
        /// will not fill in the bucket parameters because there won't be a "current" exception.
        /// </remarks>
        private static bool TryGetBucketParameters(out Watson.BucketParameters bucketParameters)
        {
            bucketParameters = new Watson.BucketParameters();

            var runtimeHostType = Type.GetTypeFromCLSID(Watson.ClrRuntimeHostClassId);
            var runtime = Activator.CreateInstance(runtimeHostType) as Watson.IClrRuntimeHost;
            if (runtime == null)
            {
                return false;
            }

            Watson.IClrControl clrControl = runtime.GetCLRControl();
            if (clrControl == null)
            {
                return false;
            }

            var errorManager = clrControl.GetCLRManager(ref Watson.ClrErrorReportingManagerInterfaceId) as Watson.IClrErrorReportingManager;
            if (errorManager == null)
            {
                return false;
            }

            int errorCode = errorManager.GetBucketParametersForCurrentException(out bucketParameters);
            if (errorCode != 0)
            {
                Debug.Fail("GetBucketParametersForCurrentException failed");
                return false;
            }

            // Make sure the event type is a traditional managed exception (clr20r3)
            if (!string.Equals(bucketParameters.EventType, "Clr20R3", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // TODO:  Do we really need to muck with the version parameters?  Why aren't the clr20r3 settings good enough?
            return true;
        }

        /// <summary>
        /// Determines whether a given component should be allowed to submit a report, considering throttling requirements.
        /// </summary>
        /// <param name="componentName">The name of the failed component.</param>
        /// <returns><c>true</c> if the report submission is allowed; <c>false</c> otherwise.</returns>
        private static bool CheckThrottledSubmission(string componentName)
        {
            // Done this way to allow testing with set next statement
#if DEBUG
            string warningMessage = "Non-fatal exception being thrown.";

            // Don't file reports when the debugger is attached.
            if (Debugger.IsAttached)
            {
                Debug.WriteLine(warningMessage);
                Debugger.Break();
                return false;
            }
            else
            {
                Debug.Fail(warningMessage);
            }

            bool debugBuild = true;
#else
            bool debugBuild = false;
#endif
            if (debugBuild)
            {
                // Do not report to watson for check bits
                return false;
            }
            else
            {
                DateTime denyIfMoreRecentlyThan = DateTime.UtcNow - s_minimumSubmissionInterval;
                DateTime lastSubmission;

                lock (s_lastReportSubmissionByComponent)
                {
                    // If an error submission for this component has already occurred recently, we won't submit again.
                    if (s_lastReportSubmissionByComponent.TryGetValue(componentName, out lastSubmission) && lastSubmission > denyIfMoreRecentlyThan)
                    {
                        return false;
                    }
                    s_lastReportSubmissionByComponent[componentName] = DateTime.UtcNow;
                }
                return true;
            }
        }

        /// <summary>
        /// Gets a string to pass to DebuggerReportingHelper as a commandline argument representing the Watson Arguments
        /// </summary>
        /// <param name="arguments">[Required, Out] resulting process start arguments</param>
        /// <returns>Bool indicating if gathering the required arguments was successful. If false report process should be aborted.</returns>
        private bool PrepareHelperArguments(out string arguments)
        {
            Watson.BucketParameters bp;
            if (!TryGetBucketParameters(out bp))
            {
                arguments = null;
                return false;
            }
            bp.Component = _exceptionInfo.ComponentName;
            // Add some additional bucket information based on the XapiException
            bp.AsmAndModName = string.Format("{0}+{1}", bp.AsmAndModName, _exceptionInfo.ImplementationName);

            StringBuilder argumentBuilder = new StringBuilder();

            string snapshotIdArg = _snapshotId;
            // If InitializeHandlesForSnapshot has not been called, the handles can be zero, which means we shouldn't request a snapshot
            if (_eventHandle == IntPtr.Zero || _processHandleDupe == IntPtr.Zero || _threadHandleDupe == IntPtr.Zero || _exceptionPointersPointer == IntPtr.Zero)
            {
                snapshotIdArg = "~";
            }
            argumentBuilder.AppendFormat("\"{0}\" ", snapshotIdArg);
            argumentBuilder.AppendFormat("\"{0}\" ", _eventHandle);
            argumentBuilder.AppendFormat("\"{0}\" ", _processHandleDupe);
            argumentBuilder.AppendFormat("\"{0}\" ", _threadHandleDupe);
            argumentBuilder.AppendFormat("\"{0}\" ", WatsonEventType);
            argumentBuilder.AppendFormat("\"{0}\" ", _exceptionPointersPointer);

            foreach (KeyValuePair<string, string> pair in bp.Parameters)
            {
                argumentBuilder.AppendFormat("\"{0}:{1}\" ", pair.Key, pair.Value);
            }

            arguments = argumentBuilder.ToString();
            return true;
        }
#endif
    }
}
