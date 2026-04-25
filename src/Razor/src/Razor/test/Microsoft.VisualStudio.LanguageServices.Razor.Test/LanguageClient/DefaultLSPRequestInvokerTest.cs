// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient;

public class DefaultLSPRequestInvokerTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public async Task ReinvokeRequestOnServerAsync_InvokesRazorLanguageClient()
    {
        // Arrange
        var called = false;
        var expectedMethod = "razor/test";
        var broker = CreateLanguageServiceBroker((method) =>
        {
            called = true;
            Assert.Equal(expectedMethod, method);
        });
        var requestInvoker = new DefaultLSPRequestInvoker(broker);

        // Act
        await requestInvoker.ReinvokeRequestOnServerAsync<object, object>(
            expectedMethod, RazorLSPConstants.RazorLanguageServerName, new object(), DisposalToken);

        // Assert
        Assert.True(called);
    }

    [Fact]
    public async Task ReinvokeRequestOnServerAsync_InvokesHtmlLanguageClient()
    {
        // Arrange
        var called = false;
        var expectedMethod = "textDocument/test";
        var broker = CreateLanguageServiceBroker((method) =>
        {
            called = true;
            Assert.Equal(expectedMethod, method);
        });
        var requestInvoker = new DefaultLSPRequestInvoker(broker);

        // Act
        await requestInvoker.ReinvokeRequestOnServerAsync<object, object>(
            expectedMethod, RazorLSPConstants.HtmlLanguageServerName, new object(), DisposalToken);

        // Assert
        Assert.True(called);
    }

    [Fact]
    public async Task ReinvokeRequestOnServerAsync_InvokesCSharpLanguageClient()
    {
        // Arrange
        var called = false;
        var expectedMethod = "textDocument/test";
        var broker = CreateLanguageServiceBroker((method) =>
        {
            called = true;
            Assert.Equal(expectedMethod, method);
        });
        var requestInvoker = new DefaultLSPRequestInvoker(broker);

        // Act
        await requestInvoker.ReinvokeRequestOnServerAsync<object, object>(
            expectedMethod, RazorLSPConstants.RazorCSharpLanguageServerName, new object(), DisposalToken);

        // Assert
        Assert.True(called);
    }

    [Fact]
    public async Task CustomRequestServerAsync_InvokesRazorLanguageClient()
    {
        // Arrange
        var called = false;
        var expectedMethod = "razor/test";
        var broker = CreateLanguageServiceBroker((method) =>
        {
            called = true;
            Assert.Equal(expectedMethod, method);
        });
        var requestInvoker = new DefaultLSPRequestInvoker(broker);

        // Act
        await requestInvoker.ReinvokeRequestOnServerAsync<object, object>(
            expectedMethod, RazorLSPConstants.RazorLanguageServerName, new object(), DisposalToken);

        // Assert
        Assert.True(called);
    }

    [Fact]
    public async Task CustomRequestServerAsync_InvokesHtmlLanguageClient()
    {
        // Arrange
        var called = false;
        var expectedMethod = "textDocument/test";
        var broker = CreateLanguageServiceBroker((method) =>
        {
            called = true;
            Assert.Equal(expectedMethod, method);
        });
        var requestInvoker = new DefaultLSPRequestInvoker(broker);

        // Act
        await requestInvoker.ReinvokeRequestOnServerAsync<object, object>(
            expectedMethod, RazorLSPConstants.HtmlLanguageServerName, new object(), DisposalToken);

        // Assert
        Assert.True(called);
    }

    [Fact]
    public async Task CustomRequestServerAsync_InvokesCSharpLanguageClient()
    {
        // Arrange
        var called = false;
        var expectedMethod = "textDocument/test";
        var broker = CreateLanguageServiceBroker((method) =>
        {
            called = true;
            Assert.Equal(expectedMethod, method);
        });
        var requestInvoker = new DefaultLSPRequestInvoker(broker);

        // Act
        await requestInvoker.ReinvokeRequestOnServerAsync<object, object>(
            expectedMethod, RazorLSPConstants.RazorCSharpLanguageServerName, new object(), DisposalToken);

        // Assert
        Assert.True(called);
    }

    private static ILanguageServiceBroker2 CreateLanguageServiceBroker(Action<string> callback)
    {
        var broker = new StrictMock<ILanguageServiceBroker2>();
#pragma warning disable CS0618 // Type or member is obsolete
        broker.Setup(b => b.RequestAsync(It.IsAny<Request<object, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null))
            .Callback((Request<object, object> request, CancellationToken _) => callback(request.Method));
#pragma warning restore CS0618 // Type or member is obsolete

        return broker.Object;
    }
}
