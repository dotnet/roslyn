// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.LogHub;
using Microsoft.VisualStudio.RpcContracts.Logging;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Threading;
using StreamJsonRpc;
using VSShell = Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageClient
{
    [Export(typeof(ILspLoggerFactory))]
    internal class VisualStudioLogHubLoggerFactory : ILspLoggerFactory
    {
        /// <summary>
        /// Command line flag name for the /log parameter when launching devenv.
        /// </summary>
        private const string LogCommandLineFlag = "log";

        /// <summary>
        /// A unique, always increasing, ID we use to identify this server in our loghub logs.  Needed so that if our
        /// server is restarted that we can have a new logstream for the new server.
        /// </summary>
        private static int s_logHubSessionId;

        private readonly VSShell.IAsyncServiceProvider _asyncServiceProvider;
        private readonly IThreadingContext _threadingContext;

        private readonly AsyncLazy<bool> _wasVSStartedWithLogParameterLazy;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioLogHubLoggerFactory(
            [Import(typeof(SAsyncServiceProvider))] VSShell.IAsyncServiceProvider asyncServiceProvider,
            IThreadingContext threadingContext)
        {
            _asyncServiceProvider = asyncServiceProvider;
            _threadingContext = threadingContext;

            _wasVSStartedWithLogParameterLazy = new AsyncLazy<bool>(WasVSStartedWithLogParameterAsync, _threadingContext.JoinableTaskFactory);
        }

        public async Task<ILspLogger> CreateLoggerAsync(string serverTypeName, string? clientName, JsonRpc jsonRpc, CancellationToken cancellationToken)
        {
            var logName = $"Roslyn.{serverTypeName}.{clientName ?? "Default"}.{Interlocked.Increment(ref s_logHubSessionId)}";
            var logId = new LogId(logName, new ServiceMoniker(typeof(LanguageServerTarget).FullName));

            var serviceContainer = await VSShell.ServiceExtensions.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>(_asyncServiceProvider).ConfigureAwait(false);
            var service = serviceContainer.GetFullAccessServiceBroker();

            var configuration = await TraceConfiguration.CreateTraceConfigurationInstanceAsync(service, ownsServiceBroker: true, cancellationToken).ConfigureAwait(false);

            // Register the default log level as warning to avoid creating log files in the hundreds of GB.
            // This level can be overriden by setting the environment variable 'LogLevel' to the desired source level.
            // See https://dev.azure.com/devdiv/DevDiv/_git/VS?path=%2Fsrc%2FPlatform%2FUtilities%2FImpl%2FLogHub%2FLocalTraceHub.cs&version=GBmain&line=142&lineEnd=143&lineStartColumn=1&lineEndColumn=1&lineStyle=plain&_a=contents
            // This should be switched back to SourceLevels.Information once Loghub adds support for recyclying logs while VS is open.
            // See https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1359778/
            var loggingLevel = SourceLevels.ActivityTracing | SourceLevels.Warning;

            // If VS was explicitly started with /log, then record all information logs as well.
            // This is extremely useful for development so that F5 deployment automatically logs everything.
            var wasVSStartedWithLogParameter = await _wasVSStartedWithLogParameterLazy.GetValueAsync(cancellationToken).ConfigureAwait(false);
            if (wasVSStartedWithLogParameter)
            {
                loggingLevel |= SourceLevels.Information;
            }

            var logOptions = new RpcContracts.Logging.LoggerOptions(new LoggingLevelSettings(loggingLevel));
            var traceSource = await configuration.RegisterLogSourceAsync(logId, logOptions, cancellationToken).ConfigureAwait(false);

            // Associate this trace source with the jsonrpc conduit.  This ensures that we can associate logs we report
            // with our callers and the operations they are performing.
            jsonRpc.ActivityTracingStrategy = new CorrelationManagerTracingStrategy { TraceSource = traceSource };

            return new LogHubLspLogger(configuration, traceSource);
        }

        private async Task<bool> WasVSStartedWithLogParameterAsync()
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
            var appCommandLiveService = await _asyncServiceProvider.GetServiceAsync(typeof(SVsAppCommandLine)).ConfigureAwait(true);
            if (appCommandLiveService is IVsAppCommandLine commandLine)
            {
                return ErrorHandler.Succeeded(commandLine.GetOption(LogCommandLineFlag, out var present, out _)) && present == 1;
            }

            return false;
        }
    }
}
