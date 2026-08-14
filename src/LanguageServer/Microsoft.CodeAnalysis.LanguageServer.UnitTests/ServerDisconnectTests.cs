// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using StreamJsonRpc;
using StreamJsonRpc.Protocol;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class ServerDisconnectTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerHostTests(testOutputHelper)
{
    [Fact]
    public async Task ServerExitsCleanlyOnIOException()
    {
        var server = await CreateLanguageServerAsync();

        // Simulate the server getting an EndOfStreamException(IOException) when reading from the JSON-RPC stream.
        server.ClientToServerPipe.Writer.Complete(new EndOfStreamException());

        // Server should exit cleanly without throwing.
        await server.ServerExitTask;
    }

    [Fact]
    public async Task ServerExitsCleanlyWhenClientDisconnects()
    {
        var server = await CreateLanguageServerAsync();

        // Simulate the client disconnecting abruptly.
        server.ClientToServerPipe.Writer.Complete();
        server.ServerToClientPipe.Reader.Complete();

        // Server should exit cleanly without throwing.
        await server.ServerExitTask;
    }

    [Fact]
    public async Task ServerExitsOnExitNotificationWithoutClosingTransport()
    {
        // Verify that the server terminates after it receives the exit notification, even if the client
        // never closes its end of the transport.
        var server = await CreateLanguageServerAsync();

        await server.ExecuteRequestAsync<object, object>(Methods.ShutdownName, new object(), CancellationToken.None);
        await server.ExecuteNotification0Async(Methods.ExitName);

        // Server should exit even though we never complete the client->server pipe.
        await server.ServerExitTask;
    }

    [Fact]
    public async Task ServerThrowsOnStreamCorruption()
    {
        var server = await CreateLanguageServerAsync();

        // Write a valid JSON-RPC header with a corrupt (non-JSON) body to cause a deserialization error.
        // Both the header and body must be written atomically (without awaiting between them) to avoid a
        // race condition where the server disconnects between the two writes causing _clientRpc to
        // asynchronously complete the pipe writer, making the second write throw.
        var garbageBody = Encoding.UTF8.GetBytes("this is not valid json!!");
        var header = Encoding.ASCII.GetBytes($"Content-Length: {garbageBody.Length}\r\n\r\n");
        var message = new byte[header.Length + garbageBody.Length];
        header.CopyTo(message, 0);
        garbageBody.CopyTo(message, header.Length);
        await server.ClientToServerPipe.Writer.WriteAsync(message);
        server.ClientToServerPipe.Writer.Complete();

        // Corruption is not a clean disconnect - the server should propagate the error.
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => server.ServerExitTask);
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task ServerThrowsOnUnexpectedException()
    {
        var server = await CreateLanguageServerAsync();

        server.ClientToServerPipe.Writer.Complete(new InvalidOperationException("Something went wrong"));

        // Unexpected exceptions should propagate to WaitForExitAsync callers.
        await Assert.ThrowsAsync<InvalidOperationException>(() => server.ServerExitTask);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/84890")]
    public async Task ServerReturnsMethodNotFoundForUnknownRequest()
    {
        await using var server = await CreateLanguageServerAsync();

        // An unknown request (for example one that a different language server implements) must not take down the
        // server - it must be answered with a JSON-RPC MethodNotFound (-32601) error response.
        var exception = await Assert.ThrowsAsync<RemoteMethodNotFoundException>(
            () => server.ExecuteRequest0Async<object>("rust-analyzer/reloadWorkspace", CancellationToken.None));
        Assert.Equal(JsonRpcErrorCode.MethodNotFound, exception.ErrorCode);

        // The server must still be alive and able to serve subsequent requests.
        Assert.False(server.ServerExitTask.IsCompleted);
        var response = await server.ExecuteRequestAsync<DidOpenTextDocumentParams, object>(Methods.TextDocumentDidOpenName, new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri("C:\\test.cs"),
                Text = "class C { }",
            }
        }, CancellationToken.None);
        Assert.Null(response);
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/84890"), WorkItem("https://github.com/dotnet/roslyn/issues/84890")]
    public async Task ServerSurvivesUnknownRequestWithNullParams()
    {
        await using var server = await CreateLanguageServerAsync();

        // Some clients send an explicit 'null' params member instead of omitting it. Today this crashes the server:
        // StreamJsonRpc's SystemTextJsonFormatter throws InvalidOperationException("Unexpected value kind: Null")
        // while deserializing the message, which faults the JSON-RPC read loop and terminates the process with an
        // unhandled exception instead of responding with MethodNotFound (-32601).
        var body = Encoding.UTF8.GetBytes("""{"jsonrpc":"2.0","id":9999,"method":"rust-analyzer/reloadWorkspace","params":null}""");
        var header = Encoding.ASCII.GetBytes($"Content-Length: {body.Length}\r\n\r\n");
        var message = new byte[header.Length + body.Length];
        header.CopyTo(message, 0);
        body.CopyTo(message, header.Length);
        await server.ClientToServerPipe.Writer.WriteAsync(message);

        // The server must remain alive and keep handling requests. The error response for id 9999 is not observed
        // here because it was not sent by this test's JSON-RPC client, so it has no pending request to match it.
        var response = await server.ExecuteRequestAsync<DidOpenTextDocumentParams, object>(Methods.TextDocumentDidOpenName, new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri("C:\\test.cs"),
                Text = "class C { }",
            }
        }, CancellationToken.None);
        Assert.Null(response);
        Assert.False(server.ServerExitTask.IsCompleted);
    }
}
