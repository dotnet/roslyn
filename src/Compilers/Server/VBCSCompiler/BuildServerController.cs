// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.ErrorReporting;

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
        private readonly ICompilerServerLogger _logger;

        internal BuildServerController(NameValueCollection appSettings, ICompilerServerLogger logger)
        {
            _appSettings = appSettings;
            _logger = logger;
        }

        internal int Run(string[] args)
        {
            string? pipeName;
            bool shutdown;
            if (!ParseCommandLine(args, out pipeName, out shutdown))
            {
                return CommonCompiler.Failed;
            }

            pipeName = pipeName ?? GetDefaultPipeName();
            if (pipeName is null)
            {
                throw new Exception("Cannot calculate pipe name");
            }

            var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) => { cancellationTokenSource.Cancel(); };

            return shutdown
                ? RunShutdown(pipeName, cancellationToken: cancellationTokenSource.Token)
                : RunServer(pipeName, cancellationToken: cancellationTokenSource.Token);
        }

        internal TimeSpan? GetKeepAliveTimeout()
        {
            try
            {
                if (int.TryParse(_appSettings[KeepAliveSettingName], NumberStyles.Integer, CultureInfo.InvariantCulture, out int keepAliveValue) &&
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
                _logger.LogException(e, "Could not read AppSettings");
                return ServerDispatcher.DefaultServerKeepAlive;
            }
        }

        internal static IClientConnectionHost CreateClientConnectionHost(string pipeName, ICompilerServerLogger logger) => new NamedPipeClientConnectionHost(pipeName, logger);

        internal static ICompilerServerHost CreateCompilerServerHost(ICompilerServerLogger logger)
        {
            var clientDirectory = BuildClient.GetClientDirectory();
            var sdkDirectory = BuildClient.GetSystemSdkDirectory();
            return new CompilerServerHost(clientDirectory, sdkDirectory, logger);
        }

        private static string? GetDefaultPipeName()
        {
            return BuildServerConnection.GetPipeName(BuildClient.GetClientDirectory());
        }

        internal int RunServer(
            string pipeName,
            ICompilerServerHost? compilerServerHost = null,
            IClientConnectionHost? clientConnectionHost = null,
            IDiagnosticListener? listener = null,
            TimeSpan? keepAlive = null,
            CancellationToken cancellationToken = default)
        {
            keepAlive ??= GetKeepAliveTimeout();
            listener ??= new EmptyDiagnosticListener();
            compilerServerHost ??= CreateCompilerServerHost(_logger);
            clientConnectionHost ??= CreateClientConnectionHost(pipeName, _logger);

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

                compilerServerHost.Logger.Log("Keep alive timeout is: {0} milliseconds.", keepAlive?.TotalMilliseconds ?? 0);
                FatalError.Handler = FailFast.Handler;

                var dispatcher = new ServerDispatcher(compilerServerHost, clientConnectionHost, listener);
                dispatcher.ListenAndDispatchConnections(keepAlive, cancellationToken);
                return CommonCompiler.Succeeded;
            }
        }

        internal static int CreateAndRunServer(
            string pipeName,
            ICompilerServerHost? compilerServerHost = null,
            IClientConnectionHost? clientConnectionHost = null,
            IDiagnosticListener? listener = null,
            TimeSpan? keepAlive = null,
            NameValueCollection? appSettings = null,
            ICompilerServerLogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            appSettings ??= new NameValueCollection();
            logger ??= EmptyCompilerServerLogger.Instance;
            var controller = new BuildServerController(appSettings, logger);
            return controller.RunServer(pipeName, compilerServerHost, clientConnectionHost, listener, keepAlive, cancellationToken);
        }

        internal int RunShutdown(string pipeName, int? timeoutOverride = null, CancellationToken cancellationToken = default) =>
            RunShutdownAsync(pipeName, waitForProcess: true, timeoutOverride, cancellationToken).GetAwaiter().GetResult();

        internal async Task<int> RunShutdownAsync(string pipeName, bool waitForProcess, int? timeoutOverride, CancellationToken cancellationToken = default)
        {
            var success = await BuildServerConnection.RunServerShutdownRequestAsync(
                pipeName,
                timeoutOverride,
                waitForProcess: waitForProcess,
                _logger,
                cancellationToken).ConfigureAwait(false);
            return success ? CommonCompiler.Succeeded : CommonCompiler.Failed;
        }

        internal static bool ParseCommandLine(string[] args, out string? pipeName, out bool shutdown)
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
