// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal partial class InteractiveHost
    {
        internal sealed class RemoteService
        {
            public readonly Process Process;
            public readonly Service Service;
            private readonly int _processId;
            private readonly SemaphoreSlim _disposeSemaphore = new SemaphoreSlim(initialCount: 1);

            // output pumping threads (stream output from stdout/stderr of the host process to the output/errorOutput writers)
            private InteractiveHost _host;              // nulled on dispose
            private Thread _readOutputThread;           // nulled on dispose	
            private Thread _readErrorOutputThread;      // nulled on dispose
            private volatile ProcessExitHandlerStatus _processExitHandlerStatus;  // set to Handled on dispose

            internal RemoteService(InteractiveHost host, Process process, int processId, Service service)
            {
                Debug.Assert(host != null);
                Debug.Assert(process != null);
                Debug.Assert(service != null);

                _host = host;
                this.Process = process;
                _processId = processId;
                this.Service = service;
                _processExitHandlerStatus = ProcessExitHandlerStatus.Uninitialized;

                // TODO (tomat): consider using single-thread async readers
                _readOutputThread = new Thread(() => ReadOutput(error: false));
                _readOutputThread.Name = "InteractiveHost-OutputReader-" + processId;
                _readOutputThread.IsBackground = true;
                _readOutputThread.Start();

                _readErrorOutputThread = new Thread(() => ReadOutput(error: true));
                _readErrorOutputThread.Name = "InteractiveHost-ErrorOutputReader-" + processId;
                _readErrorOutputThread.IsBackground = true;
                _readErrorOutputThread.Start();
            }

            internal void HookAutoRestartEvent()
            {
                using (_disposeSemaphore.DisposableWait())
                {
                    // hook the event only once per process:
                    if (_processExitHandlerStatus == ProcessExitHandlerStatus.Uninitialized)
                    {
                        Process.Exited += ProcessExitedHandler;
                        _processExitHandlerStatus = ProcessExitHandlerStatus.Hooked;
                    }
                }
            }

            private void ProcessExitedHandler(object sender, EventArgs e)
            {
                _ = ProcessExitedHandlerAsync();
            }

            private async Task ProcessExitedHandlerAsync()
            {
                try
                {
                    using (await _disposeSemaphore.DisposableWaitAsync().ConfigureAwait(false))
                    {
                        if (_processExitHandlerStatus == ProcessExitHandlerStatus.Hooked)
                        {
                            Process.Exited -= ProcessExitedHandler;
                            _processExitHandlerStatus = ProcessExitHandlerStatus.Handled;
                            // Should set _processExitHandlerStatus before calling OnProcessExited to avoid deadlocks.
                            // Calling the host should be within the lock to prevent its disposing during the execution.
                        }
                    }

                    var host = _host;
                    if (host != null)
                    {
                        await host.OnProcessExited(Process).ConfigureAwait(false);
                    }
                }
                catch (Exception e) when (FatalError.Report(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private void ReadOutput(bool error)
            {
                var buffer = new char[4096];
                StreamReader reader = error ? Process.StandardError : Process.StandardOutput;
                try
                {
                    // loop until the output pipe is closed and has no more data (process is killed):
                    while (!reader.EndOfStream)
                    {
                        int count = reader.Read(buffer, 0, buffer.Length);
                        if (count == 0)
                        {
                            break;
                        }

                        var localHost = _host;
                        if (localHost == null)
                        {
                            break;
                        }

                        localHost.OnOutputReceived(error, buffer, count);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("InteractiveHostProcess: exception while reading output from process {0}: {1}", _processId, e.Message);
                }
            }

            // Dispose may called anytime, on any thread.
            internal void Dispose()
            {
                // There can be a call from host initiated from OnProcessExit. 
                // We should not proceed with disposing if _disposeSemaphore is locked.
                using (_disposeSemaphore.DisposableWait())
                {
                    if (_processExitHandlerStatus == ProcessExitHandlerStatus.Hooked)
                    {
                        Process.Exited -= ProcessExitedHandler;
                        _processExitHandlerStatus = ProcessExitHandlerStatus.Handled;
                    }
                }

                InitiateTermination(Process, _processId);

                if (_host._joinOutputWritingThreadsOnDisposal)
                {
                    try
                    {
                        _readOutputThread?.Join();
                    }
                    catch (ThreadStateException)
                    {
                        // thread hasn't started	
                    }

                    try
                    {
                        _readErrorOutputThread?.Join();
                    }
                    catch (ThreadStateException)
                    {
                        // thread hasn't started	
                    }
                }

                // null the host so that we don't attempt to write to the buffer anymore:
                _host = null;

                _readOutputThread = _readErrorOutputThread = null;
            }

            internal static void InitiateTermination(Process process, int processId)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("InteractiveHostProcess: can't terminate process {0}: {1}", processId, e.Message);
                }
            }

            private enum ProcessExitHandlerStatus
            {
                Uninitialized = 0,
                Hooked = 1,
                Handled = 2
            }
        }
    }
}
