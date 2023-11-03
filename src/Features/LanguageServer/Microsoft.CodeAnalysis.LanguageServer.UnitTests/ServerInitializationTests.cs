// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.LanguageServer.Protocol;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public class ServerInitializationTests : AbstractLanguageServerHostTests
{
    public ServerInitializationTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task TestServerHandlesTextSyncRequestsAsync()
    {
        await using var server = await CreateLanguageServerAsync();
        var document = new VersionedTextDocumentIdentifier { Uri = ProtocolConversions.CreateAbsoluteUri("C:\\\ue25b\ud86d\udeac.cs") };
        var response = await server.ExecuteRequestAsync<DidOpenTextDocumentParams, object>(Methods.TextDocumentDidOpenName, new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = document.Uri,
                Text = "Write"
            }
        }, CancellationToken.None);

        // These are notifications so we should get a null response (but no exceptions).
        Assert.Null(response);

        response = await server.ExecuteRequestAsync<DidChangeTextDocumentParams, object>(Methods.TextDocumentDidChangeName, new DidChangeTextDocumentParams
        {
            TextDocument = document,
            ContentChanges = new[]
            {
               new TextDocumentContentChangeEvent
               {
                   Range = new VisualStudio.LanguageServer.Protocol.Range { Start = new Position(0, 0), End = new Position(0, 0) },
                   Text = "Console."
               }
            }
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
}
