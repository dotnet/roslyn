// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal sealed class EditAndContinueLogReporter : IEditAndContinueLogReporter
{
    private const string CategoryName = "Roslyn";

    private readonly AsyncBatchingWorkQueue<HotReloadLogMessage> _queue;

    public EditAndContinueLogReporter(
        IServiceBroker serviceBroker,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        var logger = new HotReloadLoggerProxy(serviceBroker);

        _queue = new AsyncBatchingWorkQueue<HotReloadLogMessage>(
            delay: TimeSpan.Zero,
            async (messages, cancellationToken) =>
            {
                try
                {
                    foreach (var message in messages)
                    {
                        await logger.LogAsync(message, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch
                {
                    // Ignore. When running OOP the connection to the debugger log service might have been terminated.
                }
            },
            listenerProvider.GetListener(FeatureAttribute.EditAndContinue),
            CancellationToken.None);
    }

    public void Report(string message, LogMessageSeverity severity)
    {
        var (verbosity, errorLevel) = severity switch
        {
            LogMessageSeverity.Info => (HotReloadVerbosity.Diagnostic, HotReloadDiagnosticErrorLevel.Info),
            LogMessageSeverity.Warning => (HotReloadVerbosity.Minimal, HotReloadDiagnosticErrorLevel.Warning),
            LogMessageSeverity.Error => (HotReloadVerbosity.Minimal, HotReloadDiagnosticErrorLevel.Error),
            _ => throw ExceptionUtilities.UnexpectedValue(severity),
        };

        _queue.AddWork(new HotReloadLogMessage(verbosity, message, errorLevel: errorLevel, category: CategoryName));
    }
}
