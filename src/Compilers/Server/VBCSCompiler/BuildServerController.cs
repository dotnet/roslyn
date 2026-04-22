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

        private readonly ICompilerServerLogger _logger;

        internal BuildServerController(ICompilerServerLogger logger)
        {
            _logger = logger;
        }

        internal int Run(string? pipeName, bool shutdown, TimeSpan? keepAlive)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) => { cancellationTokenSource.Cancel(); };

            return shutdown
                ? RunShutdown(pipeName, cancellationToken: cancellationTokenSource.Token)
                : RunServer(pipeName, keepAlive: keepAlive, cancellationToken: cancellationTokenSource.Token);
        }

        internal static TimeSpan GetDefaultKeepAlive(ICompilerServerLogger logger, NameValueCollection? appSettings = null)
        {
            try
            {
#if NET472
                appSettings ??= System.Configuration.ConfigurationManager.AppSettings;
#endif
                if (appSettings is null)
                {
                    return ServerDispatcher.DefaultServerKeepAlive;
                }

                if (int.TryParse(appSettings[KeepAliveSettingName], NumberStyles.Integer, CultureInfo.InvariantCulture, out int keepAliveValue) &&
                    keepAliveValue >= 0)
                {
                    if (keepAliveValue == 0)
                    {
                        // This is a one time server entry.
                        return Timeout.InfiniteTimeSpan;
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
                logger.LogException(e, "Could not read AppSettings");
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
            string? pipeName = null,
            ICompilerServerHost? compilerServerHost = null,
            IClientConnectionHost? clientConnectionHost = null,
            IDiagnosticListener? listener = null,
            TimeSpan? keepAlive = null,
            CancellationToken cancellationToken = default)
        {
            pipeName ??= GetDefaultPipeName();
            if (pipeName is null)
            {
                throw new Exception("Cannot calculate pipe name");
            }

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

                keepAlive ??= GetDefaultKeepAlive(_logger);
                compilerServerHost.Logger.Log("Keep alive timeout is: {0} milliseconds.", keepAlive.Value.TotalMilliseconds);
                FatalError.SetHandlers(FailFast.Handler, nonFatalHandler: null);

                var dispatcher = new ServerDispatcher(compilerServerHost, clientConnectionHost, listener);
                dispatcher.ListenAndDispatchConnections(keepAlive.Value, cancellationToken);
                return CommonCompiler.Succeeded;
            }
        }

        internal static int CreateAndRunServer(
            string pipeName,
            ICompilerServerHost? compilerServerHost = null,
            IClientConnectionHost? clientConnectionHost = null,
            IDiagnosticListener? listener = null,
            TimeSpan? keepAlive = null,
            ICompilerServerLogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            logger ??= EmptyCompilerServerLogger.Instance;
            var controller = new BuildServerController(logger);
            return controller.RunServer(pipeName, compilerServerHost, clientConnectionHost, listener, keepAlive, cancellationToken: cancellationToken);
        }

        internal int RunShutdown(string? pipeName, int? timeoutOverride = null, CancellationToken cancellationToken = default) =>
            RunShutdownAsync(pipeName, waitForProcess: true, timeoutOverride, cancellationToken).GetAwaiter().GetResult();

        internal async Task<int> RunShutdownAsync(string? pipeName, bool waitForProcess, int? timeoutOverride, CancellationToken cancellationToken = default)
        {
            pipeName ??= GetDefaultPipeName();
            if (pipeName is null)
            {
                throw new Exception("Cannot calculate pipe name");
            }

            var success = await BuildServerConnection.RunServerShutdownRequestAsync(
                pipeName,
                timeoutOverride,
                waitForProcess: waitForProcess,
                _logger,
                cancellationToken).ConfigureAwait(false);
            return success ? CommonCompiler.Succeeded : CommonCompiler.Failed;
        }

        /// <summary>
        /// Parses the command-line arguments for the build server.
        /// </summary>
        /// <remarks>
        /// Recognized options:
        /// <list type="bullet">
        ///   <item><description><c>-pipename:&lt;name&gt;</c> — the named pipe to listen on.</description></item>
        ///   <item><description><c>-timeout:&lt;seconds&gt;</c> — keep-alive in seconds; <c>0</c> means infinite (no timeout).</description></item>
        ///   <item><description><c>-log:&lt;path&gt;</c> — path to the log file.</description></item>
        ///   <item><description><c>-shutdown</c> — request the server to shut down.</description></item>
        /// </list>
        /// </remarks>
        internal static bool ParseCommandLine(string[] args, out string? pipeName, out bool shutdown, out TimeSpan? timeout, out string? logFilePath)
        {
            pipeName = null;
            shutdown = false;
            timeout = null;
            logFilePath = null;

            foreach (var arg in args)
            {
                const string pipeArgPrefix = "-pipename:";
                const string timeoutArgPrefix = "-timeout:";
                const string logArgPrefix = "-log:";
                var argSpan = arg.AsSpan();
                if (argSpan.StartsWith(pipeArgPrefix.AsSpan(), StringComparison.Ordinal))
                {
                    pipeName = argSpan[pipeArgPrefix.Length..].ToString();
                }
                else if (argSpan.StartsWith(timeoutArgPrefix.AsSpan(), StringComparison.Ordinal))
                {
                    var timeoutValue = argSpan[timeoutArgPrefix.Length..];
                    if (!int.TryParse(timeoutValue.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedTimeout) ||
                        parsedTimeout < 0)
                    {
                        return false;
                    }

                    timeout = parsedTimeout == 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(parsedTimeout);
                }
                else if (argSpan.StartsWith(logArgPrefix.AsSpan(), StringComparison.Ordinal))
                {
                    var parsedLogFilePath = argSpan[logArgPrefix.Length..];
                    if (parsedLogFilePath.Length == 0)
                    {
                        return false;
                    }

                    logFilePath = parsedLogFilePath.ToString();
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
