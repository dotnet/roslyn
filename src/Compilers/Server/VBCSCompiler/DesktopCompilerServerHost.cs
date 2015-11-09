// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
using System.IO.Pipes;
using System.Threading;
using System.Security.Principal;
using System.Security.AccessControl;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class DesktopCompilerServerHost : CompilerServerHost
    {
        // Size of the buffers to use
        private const int PipeBufferSize = 0x10000;  // 64K

        private static readonly IAnalyzerAssemblyLoader s_analyzerLoader = new ShadowCopyAnalyzerAssemblyLoader(Path.Combine(Path.GetTempPath(), "VBCSCompiler", "AnalyzerAssemblyLoader"));

        // Caches are used by C# and VB compilers, and shared here.
        private static readonly Func<string, MetadataReferenceProperties, PortableExecutableReference> s_assemblyReferenceProvider = (path, properties) => new CachingMetadataReference(path, properties);

        private readonly string _pipeName;

        public override IAnalyzerAssemblyLoader AnalyzerAssemblyLoader => s_analyzerLoader;

        public override Func<string, MetadataReferenceProperties, PortableExecutableReference> AssemblyReferenceProvider => s_assemblyReferenceProvider;

        internal DesktopCompilerServerHost(string pipeName)
            : this(pipeName, AppDomain.CurrentDomain.BaseDirectory, RuntimeEnvironment.GetRuntimeDirectory())
        {

        }

        internal DesktopCompilerServerHost(string pipeName, string clientDirectory, string sdkDirectory)
            : base(clientDirectory, sdkDirectory)
        {
            _pipeName = pipeName;
        }

        public override bool CheckAnalyzers(string baseDirectory, ImmutableArray<CommandLineAnalyzerReference> analyzers)
        {
            return AnalyzerConsistencyChecker.Check(baseDirectory, analyzers, s_analyzerLoader);
        }

        public override void Log(string message)
        {
            CompilerServerLogger.Log(message);
        }

        public override async Task<IClientConnection> CreateListenTask(CancellationToken cancellationToken)
        {
            var pipeStream = await CreateListenTaskCore(cancellationToken).ConfigureAwait(false);
            return new NamedPipeClientConnection(pipeStream);
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

                    try
                    {
                        pipeStream.Close();
                    }
                    catch
                    {
                        // Okay for Close failure here.  
                    }

                    listenSource.SetException(new Exception("Insufficient resources to process new connection."));
                }
                catch (Exception ex)
                {
                    listenSource.SetException(ex);
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
        /// Creates a Task representing the processing of the new connection.  This will return a task that
        /// will never fail.  It will always produce a <see cref="ConnectionData"/> value.  Connection errors
        /// will end up being represented as <see cref="CompletionReason.ClientDisconnect"/>
        /// </summary>
        internal static async Task<ConnectionData> CreateHandleConnectionTask(Task<NamedPipeServerStream> pipeStreamTask, IRequestHandler handler, CancellationToken cancellationToken)
        {
            Connection connection;
            try
            {
                var pipeStream = await pipeStreamTask.ConfigureAwait(false);
                var clientConnection = new NamedPipeClientConnection(pipeStream);
                connection = new Connection(clientConnection, handler);
            }
            catch (Exception ex)
            {
                // Unable to establish a connection with the client.  The client is responsible for
                // handling this case.  Nothing else for us to do here.
                CompilerServerLogger.LogException(ex, "Error creating client named pipe");
                return new ConnectionData(CompletionReason.CompilationNotStarted);
            }

            return await connection.ServeConnection(cancellationToken).ConfigureAwait(false);
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
}
