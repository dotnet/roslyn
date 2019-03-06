// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal class DesktopBuildServerController : BuildServerController
    {
        internal const string KeepAliveSettingName = "keepalive";

        private readonly NameValueCollection _appSettings;

        internal DesktopBuildServerController(NameValueCollection appSettings)
        {
            _appSettings = appSettings;
        }

        protected override IClientConnectionHost CreateClientConnectionHost(string pipeName)
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
            catch (Exception e)
            {
                CompilerServerLogger.LogException(e, "Could not read AppSettings");
                return ServerDispatcher.DefaultServerKeepAlive;
            }
        }

        protected override async Task<Stream> ConnectForShutdownAsync(string pipeName, int timeout)
        {
            return await BuildServerConnection.TryConnectToServerAsync(pipeName, timeout, cancellationToken: default).ConfigureAwait(false);
        }

        protected override string GetDefaultPipeName()
        {
            var clientDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return BuildServerConnection.GetPipeNameForPathOpt(clientDirectory);
        }

        protected override bool? WasServerRunning(string pipeName)
        {
            string mutexName = BuildServerConnection.GetServerMutexName(pipeName);
            return BuildServerConnection.WasServerMutexOpen(mutexName);
        }

        protected override int RunServerCore(string pipeName, IClientConnectionHost connectionHost, IDiagnosticListener listener, TimeSpan? keepAlive, CancellationToken cancellationToken)
        {
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

                return base.RunServerCore(pipeName, connectionHost, listener, keepAlive, cancellationToken);
            }
        }

        internal static new int RunServer(
            string pipeName,
            string tempPath,
            IClientConnectionHost clientConnectionHost = null,
            IDiagnosticListener listener = null,
            TimeSpan? keepAlive = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            BuildServerController controller = new DesktopBuildServerController(new NameValueCollection());
            return controller.RunServer(pipeName, tempPath, clientConnectionHost, listener, keepAlive, cancellationToken);
        }
    }
}
