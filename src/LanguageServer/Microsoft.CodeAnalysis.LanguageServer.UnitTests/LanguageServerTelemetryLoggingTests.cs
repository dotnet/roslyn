// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.LanguageServer.Protocol;
using Xunit.Abstractions;
using TelemetryLogLevel = Microsoft.CodeAnalysis.Internal.Log.LogLevel;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class LanguageServerTelemetryLoggingTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerHostTests(testOutputHelper)
{
    [Fact]
    public async Task TelemetryEventIsLoggedOnce()
    {
        const string eventMessage = "Telemetry event with delimiters: a=b|c,d";
        var messages = new ConcurrentQueue<LogMessageParams>();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var scope = RoslynTelemetry.SetCurrent(new RoslynTelemetry());

        await using (var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit))
        {
            server.LogMessageReceived += message =>
            {
                if (message.Message.Contains(eventMessage, StringComparison.Ordinal))
                {
                    messages.Enqueue(message);
                    completion.TrySetResult();
                }
            };

            server.GetRequiredLspService<RoslynTelemetry>().Log(
                FunctionId.TestEvent_NotUsed,
                KeyValueLogMessage.Create(properties => properties["Message"] = eventMessage, TelemetryLogLevel.Information));
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }

        var loggedMessage = Assert.Single(messages);
        Assert.Equal($"[RoslynTelemetry] TestEvent_NotUsed: Message={eventMessage}", loggedMessage.Message);
        Assert.Equal(MessageType.Debug, loggedMessage.MessageType);
    }
}
