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

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal static class VBCSCmopiler
    {
        public static int Main(string[] args)
        {
            CompilerServerLogger.Initialize("SRV");
            CompilerServerLogger.Log("Process started");

            TimeSpan? keepAliveTimeout = null;

            // VBCSCompiler is installed in the same directory as csc.exe and vbc.exe which is also the 
            // location of the response files.
            var compilerExeDirectory = AppDomain.CurrentDomain.BaseDirectory;

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

            // Grab the server mutex to prevent multiple servers from starting with the same
            // pipename and consuming excess resources. If someone else holds the mutex
            // exit immediately with a non-zero exit code
            var serverMutexName = $"{pipeName}.server";
            bool holdsMutex;
            using (var serverMutex = new Mutex(initiallyOwned: true,
                                               name: serverMutexName,
                                               createdNew: out holdsMutex))
            {
                if (!holdsMutex)
                {
                    return CommonCompiler.Failed;
                }

                try
                {
                    return Run(keepAliveTimeout, compilerExeDirectory, pipeName);
                }
                finally
                {
                    serverMutex.ReleaseMutex();
                }
            }
        }

        private static int Run(TimeSpan? keepAliveTimeout, string compilerExeDirectory, string pipeName)
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
                        keepAliveTimeout = null;
                    }
                    else
                    {
                        keepAliveTimeout = TimeSpan.FromSeconds(keepAliveValue);
                    }
                }
                else
                {
                    keepAliveTimeout = ServerDispatcher.DefaultServerKeepAlive;
                }
            }
            catch (ConfigurationErrorsException e)
            {
                keepAliveTimeout = ServerDispatcher.DefaultServerKeepAlive;
                CompilerServerLogger.LogException(e, "Could not read AppSettings");
            }

            CompilerServerLogger.Log("Keep alive timeout is: {0} milliseconds.", keepAliveTimeout?.TotalMilliseconds ?? 0);
            FatalError.Handler = FailFast.OnFatalException;

            var compilerServerHost = new DesktopCompilerServerHost(pipeName);
            var dispatcher = new ServerDispatcher(
                compilerServerHost,
                new CompilerRequestHandler(compilerServerHost, compilerExeDirectory), 
                new EmptyDiagnosticListener());

            dispatcher.ListenAndDispatchConnections(keepAliveTimeout);
            return CommonCompiler.Succeeded;
        }

    }
}
