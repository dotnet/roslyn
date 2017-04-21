// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
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

            // output pumping threads (stream output from stdout/stderr of the host process to the output/errorOutput writers)
            private Thread _readOutputThread;           // nulled on dispose
            private Thread _readErrorOutputThread;      // nulled on dispose
            private InteractiveHost _host;              // nulled on dispose
            private bool _disposing;                    // set to true on dispose

            internal RemoteService(InteractiveHost host, Process process, int processId, Service service)
            {
                Debug.Assert(host != null);
                Debug.Assert(process != null);
                Debug.Assert(service != null);

                _host = host;
                _disposing = false;
                this.Process = process;
                _processId = processId;
                this.Service = service;

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
                const int ProcessExitHooked = 1;
                const int ProcessExitHandled = 2;

                int processExitHandling = 0;

                EventHandler localHandler = null;
                localHandler = async (_, __) =>
                {
                    try
                    {
                        if (Interlocked.Exchange(ref processExitHandling, ProcessExitHandled) == ProcessExitHooked)
                        {
                            Process.Exited -= localHandler;

                            if (!_disposing)
                            {
                                await _host.OnProcessExited(Process).ConfigureAwait(false);
                            }
                        }
                    }
                    catch (Exception e) when (FatalError.Report(e))
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                };

                // hook the event only once per process:
                if (Interlocked.Exchange(ref processExitHandling, ProcessExitHooked) == 0)
                {
                    Process.Exited += localHandler;
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

            internal void Dispose(bool joinThreads)
            {
                // set _disposing so that we don't attempt restart the host anymore:
                _disposing = true;

                InitiateTermination(Process, _processId);

                // only tests require joining the threads, so we can wait synchronously
                if (joinThreads)
                {
                    if (_readOutputThread != null)
                    {
                        try
                        {
                            _readOutputThread.Join();
                        }
                        catch (ThreadStateException)
                        {
                            // thread hasn't started
                        }
                    }

                    if (_readErrorOutputThread != null)
                    {
                        try
                        {
                            _readErrorOutputThread.Join();
                        }
                        catch (ThreadStateException)
                        {
                            // thread hasn't started
                        }
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
        }
    }
}
