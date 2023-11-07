// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Xunit;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests;

public partial class ExampleTests
{
    [Fact]
    public void InitializeServer_WithMethodRegisteredTwice_Fails()
    {
        // Arrange
        var logger = GetLogger();

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => TestExampleLanguageServer.CreateBadLanguageServer(logger));

        Assert.Equal("Method textDocument/didOpen was implemented more than once.", ex.Message);
    }

    [Fact]
    public async Task InitializeServer_WithMultipleHandlerRegistration_Succeeds()
    {
        // Arrange
        var logger = GetLogger();
        var server = TestExampleLanguageServer.CreateLanguageServer(logger);

        // Act
        // Verifying that this does not throw.
        var _ = await server.InitializeServerAsync();

        // Assert
        var handlerProvider = server.GetTestAccessor().GetQueueAccessor()!.Value.GetHandlerProvider();
        var methods = handlerProvider.GetRegisteredMethods();

        Assert.Equal(5, methods.Length);
        Assert.Contains(methods, m => m.MethodName == Methods.TextDocumentDidCloseName);
        Assert.Contains(methods, m => m.MethodName == Methods.InitializeName);
        Assert.Contains(methods, m => m.MethodName == Methods.TextDocumentDidOpenName);
        Assert.Contains(methods, m => m.MethodName == Methods.InitializedName);
        Assert.Contains(methods, m => m.MethodName == Methods.TextDocumentDidChangeName);
    }

    [Fact]
    public async Task InitializeServer_SerializesCorrectly()
    {
        var logger = GetLogger();
        var server = TestExampleLanguageServer.CreateLanguageServer(logger);

        var result = await server.InitializeServerAsync();
        Assert.True(result.Capabilities.SemanticTokensOptions!.Range!.Value.First);
    }

    [Fact]
    public async Task ShutdownServer_Succeeds()
    {
        var logger = GetLogger();
        var server = TestExampleLanguageServer.CreateLanguageServer(logger);

        _ = await server.InitializeServerAsync();

        await server.ShutdownServerAsync();

        var result = await server.WaitForShutdown();
        Assert.True(0 == result, "Server failed to shut down properly");
    }

    private static ILspLogger GetLogger()
    {
        return NoOpLspLogger.Instance;
    }
}
