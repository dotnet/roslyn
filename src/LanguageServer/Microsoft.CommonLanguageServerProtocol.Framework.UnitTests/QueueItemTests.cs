// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Moq;
using StreamJsonRpc;
using Xunit;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests;

public sealed class QueueItemTests
{
    [Fact]
    public async Task QueueItem_ContentModifiedException_Propagated()
    {
        Mock<ILspLogger> mockLogger = new(MockBehavior.Strict);
        mockLogger
            .Setup(l => l.LogDebug("Starting request handler", Array.Empty<object>()))
            .Verifiable();
        mockLogger
            .Setup(l => l.LogDebug(
                It.Is<string>(message => message.Contains(ThrowLocalRpcExceptionMethodHandler.ErrorMessage)), Array.Empty<object>()))
            .Verifiable();

        var lspServices = TestLspServices.Create(
            [(typeof(IMethodHandler), ThrowLocalRpcExceptionMethodHandler.Instance)],
            supportsMethodHandlerProvider: false);
        var queueItem = new QueueItem<int>(
            ThrowLocalRpcExceptionMethodHandler.Name,
            null,
            lspServices,
            mockLogger.Object,
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<LocalRpcException>(async () =>
        {
            await queueItem.StartRequestAsync<object?, object?>(
                request: null,
                context: 1,
                ThrowLocalRpcExceptionMethodHandler.Instance,
                CancellationToken.None);
        });
        Assert.Equal(LspErrorCodes.ContentModified, exception.ErrorCode);

        mockLogger.VerifyAll();
    }

    [Fact]
    public async Task QueueItem_CancellationToken_Cancelled()
    {
        using var cancellationSource = new CancellationTokenSource();
        var lspServices = TestLspServices.Create([], supportsMethodHandlerProvider: false);
        var (_, resultTask) = QueueItem<int>.Create(
            "Test/Method",
            serializedRequest: null,
            lspServices,
            NoOpLspLogger.Instance,
            cancellationSource.Token);

        cancellationSource.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => resultTask);
        Assert.Equal(cancellationSource.Token, exception.CancellationToken);
    }

    private sealed class ThrowLocalRpcExceptionMethodHandler : IRequestHandler<object?, object?, int>
    {
        public const string Name = "Test/Method";
        public const string ErrorMessage = "Request resolve version does not match current version";

        public static readonly ThrowLocalRpcExceptionMethodHandler Instance = new();

        public bool MutatesSolutionState => false;

        public Task<object?> HandleRequestAsync(object? request, int context, CancellationToken cancellationToken)
        {
            throw new LocalRpcException(ErrorMessage) { ErrorCode = LspErrorCodes.ContentModified };
        }
    }
}
