// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
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
    public async Task ServerThrowsOnStreamCorruption()
    {
        var server = await CreateLanguageServerAsync();

        // Write a valid JSON-RPC header with a corrupt (non-JSON) body to cause a deserialization error.
        var garbageBody = Encoding.UTF8.GetBytes("this is not valid json!!");
        var header = Encoding.ASCII.GetBytes($"Content-Length: {garbageBody.Length}\r\n\r\n");
        await server.ClientToServerPipe.Writer.WriteAsync(header);
        await server.ClientToServerPipe.Writer.WriteAsync(garbageBody);
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
}
