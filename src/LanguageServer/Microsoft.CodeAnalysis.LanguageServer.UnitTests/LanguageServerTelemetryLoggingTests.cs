// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer.Logging;
using Roslyn.LanguageServer.Protocol;
using Xunit.Abstractions;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using TelemetryLogLevel = Microsoft.CodeAnalysis.Internal.Log.LogLevel;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class LanguageServerTelemetryLoggingTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerHostTests(testOutputHelper)
{
    [Fact]
    public async Task TelemetryEventIsLoggedOnce()
    {
        const string eventMessage = "Telemetry event with delimiters: a=b|c,d";
        const string faultMessage = "Fault logging remains enabled without Trace.";
        var messages = new ConcurrentQueue<LogMessageParams>();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var faultCompletion = new TaskCompletionSource<LogMessageParams>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var scope = RoslynTelemetry.SetCurrent(new RoslynTelemetry());

        await using (var server = await CreateLanguageServerAsync(
            serverConfiguration: ServerConfigurationWithoutDevKit with { InitialLogLevel = LogLevel.Information }))
        {
            server.LogMessageReceived += message =>
            {
                if (message.Message.Contains(eventMessage, StringComparison.Ordinal))
                {
                    messages.Enqueue(message);
                    completion.TrySetResult();
                }

                if (message.Message.Contains(faultMessage, StringComparison.Ordinal))
                    faultCompletion.TrySetResult(message);
            };

            var telemetry = server.GetRequiredLspService<RoslynTelemetry>();
            var logConfiguration = server.GetRequiredLspService<LspLoggerFactory>().LogConfiguration;

            Assert.False(telemetry.IsEnabled(FunctionId.TestEvent_NotUsed));
            telemetry.Log(FunctionId.TestEvent_NotUsed, eventMessage, TelemetryLogLevel.Warning);
            telemetry.ReportFault(new InvalidOperationException(faultMessage), ErrorSeverity.General, forceDump: false);
            var fault = await faultCompletion.Task.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(MessageType.Error, fault.MessageType);

            logConfiguration.UpdateLogLevel(LogLevel.Trace);
            Assert.True(telemetry.IsEnabled(FunctionId.TestEvent_NotUsed));
            telemetry.Log(
                FunctionId.TestEvent_NotUsed,
                KeyValueLogMessage.Create(properties => properties["Message"] = eventMessage, TelemetryLogLevel.Information));
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(30));

            logConfiguration.UpdateLogLevel(LogLevel.Information);
            Assert.False(telemetry.IsEnabled(FunctionId.TestEvent_NotUsed));
            telemetry.Log(FunctionId.TestEvent_NotUsed, eventMessage, TelemetryLogLevel.Error);
        }

        var loggedMessage = Assert.Single(messages);
        Assert.Equal($"[RoslynTelemetry] TestEvent_NotUsed: Message={eventMessage}", loggedMessage.Message);
        Assert.Equal(MessageType.Debug, loggedMessage.MessageType);
    }
}
