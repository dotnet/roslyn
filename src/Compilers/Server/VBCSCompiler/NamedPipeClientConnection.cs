// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
using System.Security.AccessControl;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class NamedPipeClientConnectionHost : IClientConnectionHost
    {
        // Size of the buffers to use: 64K
        private const int PipeBufferSize = 0x10000;

        private readonly ICompilerServerHost _compilerServerHost;
        private readonly string _pipeName;
        private int _loggingIdentifier;

        internal NamedPipeClientConnectionHost(ICompilerServerHost compilerServerHost, string pipeName)
        {
            _compilerServerHost = compilerServerHost;
            _pipeName = pipeName;
        }

        public async Task<IClientConnection> CreateListenTask(CancellationToken cancellationToken)
        {
            var pipeStream = await CreateListenTaskCore(cancellationToken).ConfigureAwait(false);
            return new NamedPipeClientConnection(_compilerServerHost, _loggingIdentifier++.ToString(), pipeStream);
        }

        /// <summary>
        /// Creates a Task that waits for a client connection to occur and returns the connected 
        /// <see cref="NamedPipeServerStream"/> object.  Throws on any connection error.
        /// </summary>
        /// <param name="cancellationToken">Used to cancel the connection sequence.</param>
        private async Task<NamedPipeServerStream> CreateListenTaskCore(CancellationToken cancellationToken)
        {
            // Create the pipe and begin waiting for a connection. This 
            // doesn't block, but could fail in certain circumstances, such
            // as Windows refusing to create the pipe for some reason 
            // (out of handles?), or the pipe was disconnected before we 
            // starting listening.
            NamedPipeServerStream pipeStream = ConstructPipe(_pipeName);

            // Unfortunately the version of .Net we are using doesn't support the WaitForConnectionAsync
            // method.  When it is available it should absolutely be used here.  In the meantime we
            // have to deal with the idea that this WaitForConnection call will block a thread
            // for a significant period of time.  It is unadvisable to do this to a thread pool thread 
            // hence we will use an explicit thread here.
            var listenSource = new TaskCompletionSource<NamedPipeServerStream>();
            var listenTask = listenSource.Task;
            var listenThread = new Thread(() =>
            {
                try
                {
                    CompilerServerLogger.Log("Waiting for new connection");
                    pipeStream.WaitForConnection();
                    CompilerServerLogger.Log("Pipe connection detected.");

                    if (Environment.Is64BitProcess || MemoryHelper.IsMemoryAvailable())
                    {
                        CompilerServerLogger.Log("Memory available - accepting connection");
                        listenSource.SetResult(pipeStream);
                        return;
                    }

                    listenSource.SetException(new Exception("Insufficient resources to process new connection."));
                }
                catch (Exception ex)
                {
                    listenSource.SetException(ex);
                }

                // If the task didn't complete for whatever reason ensure that we did close out the 
                // named pipe so the client can continue processing locally.
                if (listenSource.Task.Status != TaskStatus.RanToCompletion)
                {
                    if (pipeStream.IsConnected)
                    {
                        try
                        {
                            pipeStream.Close();
                        }
                        catch
                        {
                            // Okay for Close failure here
                        }
                    }
                }
            });
            listenThread.Start();

            // Create a tasks that waits indefinitely (-1) and completes only when cancelled.
            var waitCancellationTokenSource = new CancellationTokenSource();
            var waitTask = Task.Delay(
                Timeout.Infinite,
                CancellationTokenSource.CreateLinkedTokenSource(waitCancellationTokenSource.Token, cancellationToken).Token);
            await Task.WhenAny(listenTask, waitTask).ConfigureAwait(false);
            if (listenTask.IsCompleted)
            {
                waitCancellationTokenSource.Cancel();
                return await listenTask.ConfigureAwait(false);
            }

            // The listen operation was cancelled.  Close the pipe stream throw a cancellation exception to
            // simulate the cancel operation.
            waitCancellationTokenSource.Cancel();
            try
            {
                pipeStream.Close();
            }
            catch
            {
                // Okay for Close failure here.
            }

            throw new OperationCanceledException();
        }

        /// <summary>
        /// Create an instance of the pipe. This might be the first instance, or a subsequent instance.
        /// There always needs to be an instance of the pipe created to listen for a new client connection.
        /// </summary>
        /// <returns>The pipe instance or throws an exception.</returns>
        private NamedPipeServerStream ConstructPipe(string pipeName)
        {
            CompilerServerLogger.Log("Constructing pipe '{0}'.", pipeName);

            SecurityIdentifier identifier = WindowsIdentity.GetCurrent().Owner;
            PipeSecurity security = new PipeSecurity();

            // Restrict access to just this account.  
            PipeAccessRule rule = new PipeAccessRule(identifier, PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow);
            security.AddAccessRule(rule);
            security.SetOwner(identifier);

            NamedPipeServerStream pipeStream = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances, // Maximum connections.
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.WriteThrough,
                PipeBufferSize, // Default input buffer
                PipeBufferSize, // Default output buffer
                security,
                HandleInheritability.None);

            CompilerServerLogger.Log("Successfully constructed pipe '{0}'.", pipeName);

            return pipeStream;
        }
    }

    internal sealed class NamedPipeClientConnection : ClientConnection
    {
        private readonly NamedPipeServerStream _pipeStream;

        internal NamedPipeClientConnection(ICompilerServerHost compilerServerHost, string loggingIdentifier, NamedPipeServerStream pipeStream)
            : base(compilerServerHost, loggingIdentifier, pipeStream)
        {
            _pipeStream = pipeStream;
        }

        /// <summary>
        /// The IsConnected property on named pipes does not detect when the client has disconnected
        /// if we don't attempt any new I/O after the client disconnects. We start an async I/O here
        /// which serves to check the pipe for disconnection. 
        ///
        /// This will return true if the pipe was disconnected.
        /// </summary>
        protected override async Task CreateMonitorDisconnectTask(CancellationToken cancellationToken)
        {
            var buffer = SpecializedCollections.EmptyBytes;

            while (!cancellationToken.IsCancellationRequested && _pipeStream.IsConnected)
            {
                // Wait a second before trying again
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

                try
                {
                    CompilerServerLogger.Log($"Pipe {LoggingIdentifier}: Before poking pipe.");
                    await _pipeStream.ReadAsync(buffer, 0, 0, cancellationToken).ConfigureAwait(false);
                    CompilerServerLogger.Log($"Pipe {LoggingIdentifier}: After poking pipe.");
                }
                catch (Exception e)
                {
                    // It is okay for this call to fail.  Errors will be reflected in the 
                    // IsConnected property which will be read on the next iteration of the 
                    // loop
                    var msg = string.Format($"Pipe {LoggingIdentifier}: Error poking pipe.");
                    CompilerServerLogger.LogException(e, msg);
                }
            }
        }

        protected override void ValidateBuildRequest(BuildRequest request)
        {
            // Now that we've read data from the stream we can validate the identity.
            if (!ClientAndOurIdentitiesMatch(_pipeStream))
            {
                throw new Exception("Client identity does not match server identity.");
            }
        }

        /// <summary>
        /// Does the client of "pipeStream" have the same identity and elevation as we do?
        /// </summary>
        private static bool ClientAndOurIdentitiesMatch(NamedPipeServerStream pipeStream)
        {
            var serverIdentity = GetIdentity(impersonating: false);

            Tuple<string, bool> clientIdentity = null;
            pipeStream.RunAsClient(() => { clientIdentity = GetIdentity(impersonating: true); });

            CompilerServerLogger.Log($"Server identity = '{serverIdentity.Item1}', server elevation='{serverIdentity.Item2}'.");
            CompilerServerLogger.Log($"Client identity = '{clientIdentity.Item1}', client elevation='{serverIdentity.Item2}'.");

            return
                StringComparer.OrdinalIgnoreCase.Equals(serverIdentity.Item1, clientIdentity.Item1) &&
                serverIdentity.Item2 == clientIdentity.Item2;
        }

        /// <summary>
        /// Return the current user name and whether the current user is in the administrator role.
        /// </summary>
        private static Tuple<string, bool> GetIdentity(bool impersonating)
        {
            WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent(impersonating);
            WindowsPrincipal currentPrincipal = new WindowsPrincipal(currentIdentity);
            var elevatedToAdmin = currentPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
            return Tuple.Create(currentIdentity.Name, elevatedToAdmin);
        }

        public override void Close()
        {
            CompilerServerLogger.Log($"Pipe {LoggingIdentifier}: Closing.");
            try
            {
                _pipeStream.Close();
            }
            catch (Exception e)
            {
                // The client connection failing to close isn't fatal to the server process.  It is simply a client
                // for which we can no longer communicate and that's okay because the Close method indicates we are
                // done with the client already.
                var msg = string.Format($"Pipe {LoggingIdentifier}: Error closing pipe.");
                CompilerServerLogger.LogException(e, msg);
            }
        }
    }
}
