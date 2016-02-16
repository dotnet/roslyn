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
    internal class DesktopServerClient : ServerClient
    {
        protected override IClientConnectionHost CreateClientConnectionHost(string pipeName)
        {
            // VBCSCompiler is installed in the same directory as csc.exe and vbc.exe which is also the 
            // location of the response files.
            var clientDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var sdkDirectory = RuntimeEnvironment.GetRuntimeDirectory();
            var compilerServerHost = new DesktopCompilerServerHost(clientDirectory, sdkDirectory);
            return new NamedPipeClientConnectionHost(compilerServerHost, pipeName);
        }

        protected override TimeSpan? GetKeepAliveTimeout()
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

        protected override Task<Stream> ConnectForShutdownAsync(string pipeName, int timeout)
        {
            var client = new NamedPipeClientStream(pipeName);
            client.Connect(timeout);
            return Task.FromResult<Stream>(client);
        }

        protected override string GetShutdownDefaultPipeName()
        {
            var clientDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return DesktopBuildClient.GetPipeNameForPath(clientDirectory);
        }
    }
}
