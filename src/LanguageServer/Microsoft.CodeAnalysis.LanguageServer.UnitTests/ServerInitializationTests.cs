// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.LanguageServer.Handler.Logging;
using Microsoft.CodeAnalysis.LanguageServer.Logging;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class ServerInitializationTests : AbstractLanguageServerHostTests
{
    public ServerInitializationTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task TestServerHandlesTextSyncRequestsAsync()
    {
        await using var server = await CreateLanguageServerAsync();
        var document = new VersionedTextDocumentIdentifier { DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri("C:\\\ue25b\ud86d\udeac.cs") };
        var response = await server.ExecuteRequestAsync<DidOpenTextDocumentParams, object>(Methods.TextDocumentDidOpenName, new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                DocumentUri = document.DocumentUri,
                Text = "Write"
            }
        }, CancellationToken.None);

        // These are notifications so we should get a null response (but no exceptions).
        Assert.Null(response);

        response = await server.ExecuteRequestAsync<DidChangeTextDocumentParams, object>(Methods.TextDocumentDidChangeName, new DidChangeTextDocumentParams
        {
            TextDocument = document,
            ContentChanges =
            [
               new TextDocumentContentChangeEvent
               {
                   Range = new Roslyn.LanguageServer.Protocol.Range { Start = new Position(0, 0), End = new Position(0, 0) },
                   Text = "Console."
               }
            ]
        }, CancellationToken.None);

        // These are notifications so we should get a null response (but no exceptions).
        Assert.Null(response);

        response = await server.ExecuteRequestAsync<DidCloseTextDocumentParams, object>(Methods.TextDocumentDidCloseName, new DidCloseTextDocumentParams
        {
            TextDocument = document
        }, CancellationToken.None);

        // These are notifications so we should get a null response (but no exceptions).
        Assert.Null(response);
    }

    [Fact]
    public async Task TestOnAutoInsertCapabilitiesSerializedCorrectly()
    {
        await using var server = await CreateLanguageServerAsync();

        var capabilities = server.ServerCapabilities as VSInternalServerCapabilities;
        Assert.NotNull(capabilities);
        Assert.NotNull(capabilities.OnAutoInsertProvider);
        Assert.NotEmpty(capabilities.OnAutoInsertProvider.TriggerCharacters);
    }

    [Fact]
    public async Task TestUpdateLogLevelAppliesToNextWindowLogMessagesAsync()
    {
        var debugOne = "DebugOne";
        var infoOne = "InfoOne";
        var debugTwo = "DebugTwo";
        var infoTwo = "InfoTwo";
        var logEnd = "LogEnd";
        var logMessages = new List<string>();
        await using (var server = await CreateLanguageServerAsync())
        {
            var logCompletionSource = new TaskCompletionSource<LogMessageParams>();

            server.LogMessageReceived += logMessage =>
            {
                logMessages.Add(logMessage.Message);
                if (logMessage.Message.Contains(logEnd, StringComparison.Ordinal))
                {
                    logCompletionSource.TrySetResult(logMessage);
                }
            };

            var lspLogger = server.GetRequiredLspService<ILspLogger>();

            // At the initial trace level, expect both the debug and info messages to be logged.
            lspLogger.LogDebug(debugOne);
            lspLogger.LogInformation(infoOne);

            // Update log level to Information. Expect only info messages to be logged after this.
            await server.ExecuteNotificationAsync(UpdateLogLevelHandler.MethodName, new { logLevel = nameof(LogLevel.Information) });
            // Update log level is a notification, so we need to wait until the server processes it.
            await WaitForLogLevelUpdate(server, LogLevel.Information);

            lspLogger.LogDebug(debugTwo);
            lspLogger.LogInformation(infoTwo);
            lspLogger.LogInformation(logEnd);
            await logCompletionSource.Task;
        }

        Assert.Contains(debugOne, logMessages);
        Assert.Contains(infoOne, logMessages);
        Assert.DoesNotContain(debugTwo, logMessages);
        Assert.Contains(infoTwo, logMessages);

        async Task WaitForLogLevelUpdate(TestLspServer server, LogLevel newLevel)
        {
            while (!server.GetRequiredLspService<LspLoggerFactory>().LogConfiguration.GetLogLevel().Equals(newLevel))
            {
                await Task.Delay(50);
            }
        }
    }
}
