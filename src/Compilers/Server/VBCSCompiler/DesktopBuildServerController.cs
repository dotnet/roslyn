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
using System.Collections.Specialized;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal class DesktopBuildServerController : BuildServerController
    {
        internal const string KeepAliveSettingName = "keepalive";

        private readonly NameValueCollection _appSettings;

        internal DesktopBuildServerController(NameValueCollection appSettings = null)
        {
            _appSettings = appSettings ?? ConfigurationManager.AppSettings;
        }

        protected override IClientConnectionHost CreateClientConnectionHost(string pipeName)
        {
            // VBCSCompiler is installed in the same directory as csc.exe and vbc.exe which is also the 
            // location of the response files.
            var clientDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var sdkDirectory = RuntimeEnvironment.GetRuntimeDirectory();
            var compilerServerHost = new DesktopCompilerServerHost(clientDirectory, sdkDirectory);
            return new NamedPipeClientConnectionHost(compilerServerHost, pipeName);
        }

        protected internal override TimeSpan? GetKeepAliveTimeout()
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
            catch (ConfigurationErrorsException e)
            {
                CompilerServerLogger.LogException(e, "Could not read AppSettings");
                return ServerDispatcher.DefaultServerKeepAlive;
            }
        }

        protected override Task<Stream> ConnectForShutdownAsync(string pipeName, int timeout)
        {
            var client = new NamedPipeClientStream(pipeName);
            client.Connect(timeout);
            return Task.FromResult<Stream>(client);
        }

        protected override string GetDefaultPipeName()
        {
            var clientDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return DesktopBuildClient.GetPipeNameForPath(clientDirectory);
        }

        protected override bool? WasServerRunning(string pipeName)
        {
            string mutexName = DesktopBuildClient.GetServerMutexName(pipeName);
            return DesktopBuildClient.WasServerMutexOpen(mutexName);
        }

        protected override int RunServerCore(string pipeName, IClientConnectionHost connectionHost, IDiagnosticListener listener, TimeSpan? keepAlive, CancellationToken cancellationToken)
        {
            // Grab the server mutex to prevent multiple servers from starting with the same
            // pipename and consuming excess resources. If someone else holds the mutex
            // exit immediately with a non-zero exit code
            var mutexName = DesktopBuildClient.GetServerMutexName(pipeName);
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
                    return base.RunServerCore(pipeName, connectionHost, listener, keepAlive, cancellationToken);
                }
                finally
                {
                    serverMutex.ReleaseMutex();
                }
            }
        }

        internal static new int RunServer(string pipeName, IClientConnectionHost clientConnectionHost = null, IDiagnosticListener listener = null, TimeSpan? keepAlive = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            BuildServerController controller = new DesktopBuildServerController();
            return controller.RunServer(pipeName, clientConnectionHost, listener, keepAlive, cancellationToken);
        }
    }
}
