// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.CodeAnalysis.CommandLine;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal static class VBCSCompiler
    {
        public static int Main(string[] args)
        {
            CompilerServerLogger.Initialize("SRV");
            CompilerServerLogger.Log("Process started");

            string pipeName;
            bool shutdown;
            if (!ParseCommandLine(args, out pipeName, out shutdown))
            {
                return CommonCompiler.Failed;
            }

            var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) => { cancellationTokenSource.Cancel(); };

            return shutdown
                ? RunShutdown(pipeName, cancellationToken: cancellationTokenSource.Token)
                : RunServer(pipeName, cancellationToken: cancellationTokenSource.Token);
        }

        internal static int RunServer(string pipeName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(pipeName))
            {
                return CommonCompiler.Failed;
            }

            var keepAliveTimeout = GetKeepAliveTimeout();
            var serverMutexName = BuildProtocolConstants.GetServerMutexName(pipeName);

            // VBCSCompiler is installed in the same directory as csc.exe and vbc.exe which is also the 
            // location of the response files.
            var clientDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var sdkDirectory = RuntimeEnvironment.GetRuntimeDirectory();
            var compilerServerHost = new DesktopCompilerServerHost(clientDirectory, sdkDirectory);
            var clientConnectionHost = new NamedPipeClientConnectionHost(compilerServerHost, pipeName);
            return Run(serverMutexName, clientConnectionHost, keepAliveTimeout, cancellationToken);
        }

        internal static int RunShutdown(string pipeName, bool waitForProcess = true, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return RunShutdownAsync(pipeName, waitForProcess, timeout, cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Shutting down the server is an inherently racy operation.  The server can be started or stopped by
        /// external parties at any time.
        /// 
        /// This function will return success if at any time in the function the server is determined to no longer
        /// be running.
        /// </summary>
        internal static async Task<int> RunShutdownAsync(string pipeName, bool waitForProcess = true, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(pipeName))
            {
                var clientDirectory = AppDomain.CurrentDomain.BaseDirectory;
                pipeName = DesktopBuildClient.GetPipeNameFromFileInfo(clientDirectory);
            }

            var mutexName = BuildProtocolConstants.GetServerMutexName(pipeName);
            if (!DesktopBuildClient.WasServerMutexOpen(mutexName))
            {
                // The server holds the mutex whenever it is running, if it's not open then the 
                // server simply isn't running.
                return CommonCompiler.Succeeded;
            }

            try
            {
                using (var client = new NamedPipeClientStream(pipeName))
                {
                    var realTimeout = timeout != null
                        ? (int)timeout.Value.TotalMilliseconds
                        : Timeout.Infinite;
                    client.Connect(realTimeout);

                    var request = BuildRequest.CreateShutdown();
                    await request.WriteAsync(client, cancellationToken).ConfigureAwait(false);
                    var response = await BuildResponse.ReadAsync(client, cancellationToken).ConfigureAwait(false);
                    var shutdownResponse = (ShutdownBuildResponse)response;

                    if (waitForProcess)
                    {
                        try
                        {
                            var process = Process.GetProcessById(shutdownResponse.ServerProcessId);
                            process.WaitForExit();
                        }
                        catch (Exception)
                        {
                            // There is an inherent race here with the server process.  If it has already shutdown
                            // by the time we try to access it then the operation has succeed.
                        }
                    }
                }

                return CommonCompiler.Succeeded;
            }
            catch (Exception)
            {
                if (!DesktopBuildClient.WasServerMutexOpen(mutexName))
                {
                    // If the server was in the process of shutting down when we connected then it's reasonable
                    // for an exception to happen.  If the mutex has shutdown at this point then the server 
                    // is shut down.
                    return CommonCompiler.Succeeded;
                }

                return CommonCompiler.Failed;
            }
        }

        private static TimeSpan? GetKeepAliveTimeout()
        {
            try
            {
                int keepAliveValue;
                string keepAliveStr = ConfigurationManager.AppSettings["keepalive"];
                if (int.TryParse(keepAliveStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out keepAliveValue) &&
                    keepAliveValue >= 0)
                {
                    if (keepAliveValue == 0)
                    {
                        // This is a one time server entry.
                        return null;
                    }
                    else
                    {
                        return TimeSpan.FromSeconds(keepAliveValue);
                    }
                }
                else
                {
                    return ServerDispatcher.DefaultServerKeepAlive;
                }
            }
            catch (ConfigurationErrorsException e)
            {
                CompilerServerLogger.LogException(e, "Could not read AppSettings");
                return ServerDispatcher.DefaultServerKeepAlive;
            }
        }

        internal static int Run(string mutexName, IClientConnectionHost connectionHost, TimeSpan? keepAlive, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Run(mutexName, connectionHost, new EmptyDiagnosticListener(), keepAlive, cancellationToken);
        }

        internal static int Run(string mutexName, IClientConnectionHost connectionHost, IDiagnosticListener listener, TimeSpan? keepAlive, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Grab the server mutex to prevent multiple servers from starting with the same
            // pipename and consuming excess resources. If someone else holds the mutex
            // exit immediately with a non-zero exit code
            bool holdsMutex;
            using (var serverMutex = new Mutex(initiallyOwned: true,
                                               name: mutexName,
                                               createdNew: out holdsMutex))
            {
                if (!holdsMutex)
                {
                    return CommonCompiler.Failed;
                }

                try
                {
                    return RunCore(connectionHost, listener, keepAlive, cancellationToken);
                }
                finally
                {
                    serverMutex.ReleaseMutex();
                }
            }
        }

        private static int RunCore(
            IClientConnectionHost connectionHost,
            IDiagnosticListener listener,
            TimeSpan? keepAliveTimeout,
            CancellationToken cancellationToken)
        {
            CompilerServerLogger.Log("Keep alive timeout is: {0} milliseconds.", keepAliveTimeout?.TotalMilliseconds ?? 0);
            FatalError.Handler = FailFast.OnFatalException;

            var dispatcher = new ServerDispatcher(connectionHost, listener);
            dispatcher.ListenAndDispatchConnections(keepAliveTimeout, cancellationToken);
            return CommonCompiler.Succeeded;
        }

        internal static bool ParseCommandLine(string[] args, out string pipeName, out bool shutdown)
        {
            pipeName = null;
            shutdown = false;

            foreach (var arg in args)
            {
                const string pipeArgPrefix = "-pipename:";
                if (arg.StartsWith(pipeArgPrefix, StringComparison.Ordinal))
                {
                    pipeName = arg.Substring(pipeArgPrefix.Length);
                }
                else if (arg == "-shutdown")
                {
                    shutdown = true;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}
