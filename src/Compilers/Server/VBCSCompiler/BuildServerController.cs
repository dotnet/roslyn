// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.CodeAnalysis.CommandLine;
using System.Runtime.InteropServices;
using System.Collections.Specialized;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    /// <summary>
    /// Base type for the build server code.  Contains the basic logic for running the actual server, startup 
    /// and shutdown.
    /// </summary>
    internal sealed class BuildServerController
    {
        internal const string KeepAliveSettingName = "keepalive";

        private readonly NameValueCollection _appSettings;

        internal BuildServerController(NameValueCollection appSettings)
        {
            _appSettings = appSettings;
        }

        internal int Run(string[] args)
        {
            string pipeName;
            bool shutdown;
            if (!ParseCommandLine(args, out pipeName, out shutdown))
            {
                return CommonCompiler.Failed;
            }

            pipeName = pipeName ?? GetDefaultPipeName();
            var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) => { cancellationTokenSource.Cancel(); };

            var tempPath = Path.GetTempPath();

            return shutdown
                ? RunShutdown(pipeName, cancellationToken: cancellationTokenSource.Token)
                : RunServer(pipeName, tempPath, cancellationToken: cancellationTokenSource.Token);
        }

        internal TimeSpan? GetKeepAliveTimeout()
        {
            try
            {
                int keepAliveValue;
                string keepAliveStr = _appSettings[KeepAliveSettingName];
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
            catch (Exception e)
            {
                CompilerServerLogger.LogException(e, "Could not read AppSettings");
                return ServerDispatcher.DefaultServerKeepAlive;
            }
        }

        /// <summary>
        /// Was a server running with the specified session key during the execution of this call?
        /// </summary>
        private bool? WasServerRunning(string pipeName)
        {
            string mutexName = BuildServerConnection.GetServerMutexName(pipeName);
            return BuildServerConnection.WasServerMutexOpen(mutexName);
        }

        private IClientConnectionHost CreateClientConnectionHost(string pipeName)
        {
            var compilerServerHost = CreateCompilerServerHost();
            return CreateClientConnectionHostForServerHost(compilerServerHost, pipeName);
        }

        internal static ICompilerServerHost CreateCompilerServerHost()
        {
            // VBCSCompiler is installed in the same directory as csc.exe and vbc.exe which is also the 
            // location of the response files.
            var clientDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var sdkDirectory = BuildClient.GetSystemSdkDirectory();

            return new DesktopCompilerServerHost(clientDirectory, sdkDirectory);
        }

        internal static IClientConnectionHost CreateClientConnectionHostForServerHost(
            ICompilerServerHost compilerServerHost,
            string pipeName)
        {
            return new NamedPipeClientConnectionHost(compilerServerHost, pipeName);
        }

        private async Task<Stream> ConnectForShutdownAsync(string pipeName, int timeout)
        {
            return await BuildServerConnection.TryConnectToServerAsync(pipeName, timeout, cancellationToken: default).ConfigureAwait(false);
        }

        private string GetDefaultPipeName()
        {
            var clientDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return BuildServerConnection.GetPipeNameForPathOpt(clientDirectory);
        }

        internal int RunServer(
            string pipeName,
            string tempPath,
            IClientConnectionHost clientConnectionHost = null,
            IDiagnosticListener listener = null,
            TimeSpan? keepAlive = null,
            CancellationToken cancellationToken = default)
        {
            keepAlive = keepAlive ?? GetKeepAliveTimeout();
            listener = listener ?? new EmptyDiagnosticListener();
            clientConnectionHost = clientConnectionHost ?? CreateClientConnectionHost(pipeName);

            // Grab the server mutex to prevent multiple servers from starting with the same
            // pipename and consuming excess resources. If someone else holds the mutex
            // exit immediately with a non-zero exit code
            var mutexName = BuildServerConnection.GetServerMutexName(pipeName);
            bool createdNew;
            using (var serverMutex = BuildServerConnection.OpenOrCreateMutex(name: mutexName,
                                                                             createdNew: out createdNew))
            {
                if (!createdNew)
                {
                    return CommonCompiler.Failed;
                }

                CompilerServerLogger.Log("Keep alive timeout is: {0} milliseconds.", keepAlive?.TotalMilliseconds ?? 0);
                FatalError.Handler = FailFast.OnFatalException;

                var dispatcher = new ServerDispatcher(clientConnectionHost, listener);
                dispatcher.ListenAndDispatchConnections(keepAlive, cancellationToken);
                return CommonCompiler.Succeeded;
            }
        }

        internal static int CreateAndRunServer(
            string pipeName,
            string tempPath,
            IClientConnectionHost clientConnectionHost = null,
            IDiagnosticListener listener = null,
            TimeSpan? keepAlive = null,
            NameValueCollection appSettings = null,
            CancellationToken cancellationToken = default)
        {
            appSettings ??= new NameValueCollection();
            var controller = new BuildServerController(appSettings);
            return controller.RunServer(pipeName, tempPath, clientConnectionHost, listener, keepAlive, cancellationToken);
        }

        internal int RunShutdown(string pipeName, bool waitForProcess = true, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
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
        internal async Task<int> RunShutdownAsync(string pipeName, bool waitForProcess = true, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            if (WasServerRunning(pipeName) == false)
            {
                // The server holds the mutex whenever it is running, if it's not open then the 
                // server simply isn't running.
                return CommonCompiler.Succeeded;
            }

            try
            {
                var realTimeout = timeout != null
                    ? (int)timeout.Value.TotalMilliseconds
                    : Timeout.Infinite;
                using (var client = await ConnectForShutdownAsync(pipeName, realTimeout).ConfigureAwait(false))
                {
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
                if (WasServerRunning(pipeName) == false)
                {
                    // If the server was in the process of shutting down when we connected then it's reasonable
                    // for an exception to happen.  If the mutex has shutdown at this point then the server 
                    // is shut down.
                    return CommonCompiler.Succeeded;
                }

                return CommonCompiler.Failed;
            }
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
