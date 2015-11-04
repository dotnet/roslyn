// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class DesktopCompilerServerHost : ICompilerServerHost
    {
        // Size of the buffers to use
        private const int PipeBufferSize = 0x10000;  // 64K

        private static readonly IAnalyzerAssemblyLoader s_analyzerLoader = new ShadowCopyAnalyzerAssemblyLoader(Path.Combine(Path.GetTempPath(), "VBCSCompiler", "AnalyzerAssemblyLoader"));
        private readonly string _pipeName;

        internal DesktopCompilerServerHost(string pipeName)
        {
            _pipeName = pipeName;
        }

        public IAnalyzerAssemblyLoader AnalyzerAssemblyLoader => s_analyzerLoader;

        public string GetSdkDirectory() => RuntimeEnvironment.GetRuntimeDirectory();

        public bool CheckAnalyzers(string baseDirectory, ImmutableArray<CommandLineAnalyzerReference> analyzers, out BuildResponse response)
        {
            if (!AnalyzerConsistencyChecker.Check(baseDirectory, analyzers, s_analyzerLoader))
            {
                response = new AnalyzerInconsistencyBuildResponse();
                return false;
            }

            response = null;
            return true;
        }

        public async Task<IClientConnection> CreateListenTask(CancellationToken cancellationToken)
        {
            var namedPipeServerStream = await CreateListenTaskCore(_pipeName, cancellationToken).ConfigureAwait(true);
            return new NamedPipeClientConnection(namedPipeServerStream);
        }

        /// <summary>
        /// Creates a Task that waits for a client connection to occur and returns the connected 
        /// <see cref="NamedPipeServerStream"/> object.  Throws on any connection error.
        /// </summary>
        /// <param name="pipeName">Name of the pipe on which the instance will listen for requests.</param>
        /// <param name="cancellationToken">Used to cancel the connection sequence.</param>
        private async Task<NamedPipeServerStream> CreateListenTaskCore(string pipeName, CancellationToken cancellationToken)
        {
            // Create the pipe and begin waiting for a connection. This 
            // doesn't block, but could fail in certain circumstances, such
            // as Windows refusing to create the pipe for some reason 
            // (out of handles?), or the pipe was disconnected before we 
            // starting listening.
            NamedPipeServerStream pipeStream = ConstructPipe(pipeName);

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

        /*

        BTODO: Need to rationalize this
        private static void LogAbnormalExit(string msg)
        {
            string roslynTempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "RoslynCompilerServerCrash");
            if (!PortableShim.Directory.Exists(roslynTempDir))
            {
                Directory.CreateDirectory(roslynTempDir);
            }
            string path = Path.Combine(roslynTempDir, DateTime.Now.ToString());

            using (var writer = File.AppendText(path))
            {
                writer.WriteLine(msg);
            }
        }
        */
    }
}
