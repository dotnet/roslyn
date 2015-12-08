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

            var keepAliveTimeout = GetKeepAliveTimeout();

            // Pipename should be passed as the first and only argument to the server process
            // and it must have the form "-pipename:name". Otherwise, exit with a non-zero
            // exit code
            const string pipeArgPrefix = "-pipename:";
            if (args.Length != 1 ||
                args[0].Length <= pipeArgPrefix.Length ||
                !args[0].StartsWith(pipeArgPrefix))
            {
                return CommonCompiler.Failed;
            }

            var pipeName = args[0].Substring(pipeArgPrefix.Length);
            var serverMutexName = BuildProtocolConstants.GetServerMutexName(pipeName);

            // VBCSCompiler is installed in the same directory as csc.exe and vbc.exe which is also the 
            // location of the response files.
            var clientDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var sdkDirectory = RuntimeEnvironment.GetRuntimeDirectory();
            var compilerServerHost = new DesktopCompilerServerHost(clientDirectory, sdkDirectory);
            var clientConnectionHost = new NamedPipeClientConnectionHost(compilerServerHost, pipeName);
            var listener = new EmptyDiagnosticListener();
            return Run(serverMutexName, clientConnectionHost, listener, keepAliveTimeout);
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
            CancellationToken cancellationToken = default(CancellationToken))
        {
            CompilerServerLogger.Log("Keep alive timeout is: {0} milliseconds.", keepAliveTimeout?.TotalMilliseconds ?? 0);
            FatalError.Handler = FailFast.OnFatalException;

            var dispatcher = new ServerDispatcher(connectionHost, listener);
            dispatcher.ListenAndDispatchConnections(keepAliveTimeout, cancellationToken);
            return CommonCompiler.Succeeded;
        }
    }
}
