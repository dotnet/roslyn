// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal partial class InteractiveHost
    {
        internal sealed class RemoteService
        {
            private static readonly char[] s_unicodeAsciiTranscodingMarkerDesktop = EncodeMarker("\nProcess is terminated due to StackOverflowException.\n");
            private static readonly char[] s_unicodeAsciiTranscodingMarkerCore = EncodeMarker("Stack overflow.\n");

            public readonly Process Process;
            public readonly JsonRpc JsonRpc;
            public readonly InteractiveHostPlatformInfo PlatformInfo;
            public readonly InteractiveHostOptions Options;

            private readonly int _processId;
            private readonly SemaphoreSlim _disposeSemaphore = new SemaphoreSlim(initialCount: 1);
            private readonly bool _joinOutputWritingThreadsOnDisposal;

            // output pumping threads (stream output from stdout/stderr of the host process to the output/errorOutput writers)
            private InteractiveHost? _host;              // nulled on dispose
            private Thread? _readOutputThread;           // nulled on dispose	
            private Thread? _readErrorOutputThread;      // nulled on dispose
            private volatile ProcessExitHandlerStatus _processExitHandlerStatus;  // set to Handled on dispose

            internal RemoteService(InteractiveHost host, Process process, int processId, JsonRpc jsonRpc, InteractiveHostPlatformInfo platformInfo, InteractiveHostOptions options)
            {
                Process = process;
                JsonRpc = jsonRpc;
                PlatformInfo = platformInfo;
                Options = options;

                _host = host;
                _joinOutputWritingThreadsOnDisposal = _host._joinOutputWritingThreadsOnDisposal;
                _processId = processId;
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
                        await host.OnProcessExitedAsync(Process).ConfigureAwait(false);
                    }
                }
                catch (Exception e) when (FatalError.ReportAndPropagate(e))
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }

            private static char[] EncodeMarker(string ascii)
            {
                Debug.Assert(ascii.Length % 2 == 0);
                return Encoding.Unicode.GetChars(Encoding.ASCII.GetBytes(ascii));
            }

            private void ReadOutput(bool error)
            {
                const int BaseLength = 4096;

                // Workaround for https://github.com/dotnet/runtime/issues/45503.
                // When the process terminates due to stack overflow the CLR prints out a message that is not correctly encoded to Unicode.
                // Hence it will come out as ASCII bytes interpreted as UTF-16 characters.
                // We detect known message text in the output stream and transcode it and the output that follows to Unicode.

                var transcodingMarker = Options.Platform == InteractiveHostPlatform.Core ?
                    s_unicodeAsciiTranscodingMarkerCore : s_unicodeAsciiTranscodingMarkerDesktop;

                var buffer = new char[BaseLength + transcodingMarker.Length];
                StreamReader reader = error ? Process.StandardError : Process.StandardOutput;
                bool transcoding = false;
                try
                {
                    // loop until the output pipe is closed and has no more data (process is killed):
                    while (!reader.EndOfStream)
                    {
                        int charsRead = reader.Read(buffer, 0, BaseLength);
                        if (charsRead == 0)
                        {
                            break;
                        }

                        if (transcoding)
                        {
                            charsRead = Transcode(ref buffer, charsRead, 0);
                        }
                        else if (error)
                        {
                            int transcodingMarkerStart = Array.IndexOf(buffer, transcodingMarker[0], startIndex: 0, count: charsRead);
                            if (transcodingMarkerStart >= 0)
                            {
                                int additionalCharsToRead = transcodingMarkerStart + transcodingMarker.Length - charsRead;
                                if (additionalCharsToRead > 0)
                                {
                                    charsRead += ReadAll(reader, buffer, index: charsRead, length: additionalCharsToRead);
                                }

                                if (ArraysEqual(buffer, transcodingMarkerStart, transcodingMarker, 0, transcodingMarker.Length))
                                {
                                    // once we hit the marker we assume everything that follows is encoded:
                                    charsRead = Transcode(ref buffer, charsRead, transcodingMarkerStart);
                                    transcoding = true;
                                }
                            }
                        }

                        var localHost = _host;
                        if (localHost == null)
                        {
                            break;
                        }

                        localHost.OnOutputReceived(error, buffer, charsRead);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("InteractiveHostProcess: exception while reading output from process {0}: {1}", _processId, e.Message);
                }
            }

            private static bool ArraysEqual(char[] left, int leftStart, char[] right, int rightStart, int length)
            {
                if (leftStart + length > left.Length || rightStart + length > right.Length)
                {
                    return false;
                }

                for (int i = 0; i < length; i++)
                {
                    if (left[leftStart + i] != right[rightStart + i])
                    {
                        return false;
                    }
                }

                return true;
            }

            private static int Transcode(ref char[] buffer, int bufferLength, int start)
            {
                var newBufferLength = start + (bufferLength - start) * 2;
                if (buffer.Length < newBufferLength)
                {
                    Array.Resize(ref buffer, newBufferLength);
                }

                for (int i = bufferLength - 1, j = newBufferLength - 2; i >= start; i--, j -= 2)
                {
                    var c = buffer[i];

                    // Unicode is little endian:
                    buffer[j] = (char)(c & 0x00ff);
                    buffer[j + 1] = (char)(c >> 8);
                }

                return newBufferLength;
            }

            private static int ReadAll(StreamReader reader, char[] buffer, int index, int length)
            {
                var totalRead = 0;
                while (!reader.EndOfStream && length > 0)
                {
                    var charsRead = reader.Read(buffer, index, length);
                    if (charsRead == 0)
                    {
                        break;
                    }

                    totalRead += charsRead;
                    index += charsRead;
                    length -= charsRead;
                }

                return totalRead;
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

                if (_joinOutputWritingThreadsOnDisposal)
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
