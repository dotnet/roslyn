// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.LogHub;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageClient
{
    internal partial class InProcLanguageServer : ILspLogger
    {
        public async Task<TraceSource?> CreateTraceSourceAsync(string logName, CancellationToken cancellationToken)
        {
            if (_asyncServiceProvider == null)
                return null;

            var cleaned = string.Concat(logName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            var logId = new LogId(cleaned, new ServiceMoniker("Microsoft.VisualStudio.LanguageServices.Implementation.LanguageClient"));

            var serviceContainer = await _asyncServiceProvider.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>().ConfigureAwait(false);
            var service = serviceContainer.GetFullAccessServiceBroker();

            using var configuration = await TraceConfiguration.CreateTraceConfigurationInstanceAsync(service, cancellationToken).ConfigureAwait(false);

            var traceSource = await configuration.RegisterLogSourceAsync(logId, new LogHub.LoggerOptions(), cancellationToken).ConfigureAwait(false);
            Trace.AutoFlush = true;
            return traceSource;
        }
    }
}
