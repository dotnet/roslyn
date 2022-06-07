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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageClient
{
    [Export(typeof(ILspLoggerFactory))]
    internal class VisualStudioLogHubLoggerFactory : ILspLoggerFactory
    {
        /// <summary>
        /// A unique, always increasing, ID we use to identify this server in our loghub logs.  Needed so that if our
        /// server is restarted that we can have a new logstream for the new server.
        /// </summary>
        private static int s_logHubSessionId;

        private readonly IAsyncServiceProvider _asyncServiceProvider;
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioLogHubLoggerFactory(
            [Import(typeof(SAsyncServiceProvider))] IAsyncServiceProvider asyncServiceProvider,
            IThreadingContext threadingContext)
        {
            _asyncServiceProvider = asyncServiceProvider;
            _threadingContext = threadingContext;
        }

        public async Task<ILspLogger> CreateLoggerAsync(string serverTypeName, JsonRpc jsonRpc, CancellationToken cancellationToken)
        {
            var logName = $"Roslyn.{serverTypeName}.{Interlocked.Increment(ref s_logHubSessionId)}";
            var logId = new LogId(logName, new ServiceMoniker(typeof(LanguageServerTarget).FullName));

            var serviceContainer = await _asyncServiceProvider.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>(_threadingContext.JoinableTaskFactory).ConfigureAwait(false);
            var service = serviceContainer.GetFullAccessServiceBroker();

            var configuration = await TraceConfiguration.CreateTraceConfigurationInstanceAsync(service, ownsServiceBroker: true, cancellationToken).ConfigureAwait(false);

            // Register the default log level as information.
            // Loghub will take care of cleaning up older logs from past sessions / current session
            // if it decides the log file sizes are too large.
            var loggingLevel = SourceLevels.ActivityTracing | SourceLevels.Information;

            var logOptions = new RpcContracts.Logging.LoggerOptions(new LoggingLevelSettings(loggingLevel));
            var traceSource = await configuration.RegisterLogSourceAsync(logId, logOptions, cancellationToken).ConfigureAwait(false);

            // Associate this trace source with the jsonrpc conduit.  This ensures that we can associate logs we report
            // with our callers and the operations they are performing.
            jsonRpc.ActivityTracingStrategy = new CorrelationManagerTracingStrategy { TraceSource = traceSource };

            return new LogHubLspLogger(configuration, traceSource);
        }
    }
}
