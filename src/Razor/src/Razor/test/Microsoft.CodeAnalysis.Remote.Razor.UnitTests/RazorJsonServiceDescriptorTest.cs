// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol.Completion;
using Microsoft.CodeAnalysis.Remote.Razor;
using Microsoft.ServiceHub.Framework;
using Moq;
using Nerdbank.Streams;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using StreamJsonRpc;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Remote;

public class RazorJsonServiceDescriptorTest(ITestOutputHelper testOutputHelper) : TestBase
{
    public static IEnumerable<object[]> JsonServices
        => RazorServices.TestAccessor.JsonServices.Select(service => new object[] { service.Item1 });

    [Theory]
    [MemberData(nameof(JsonServices))]
    public void DescriptorFormattersUseGeneratedAndAuditedMetadata(Type serviceType)
    {
        Assert.Equal("1", Environment.GetEnvironmentVariable(ReflectionFallbackJsonTypeInfoResolver.RequireGeneratedMetadataEnvironmentVariable));
        var descriptor = RazorServices.JsonDescriptors.GetDescriptorForServiceFactory(serviceType);
        var remoteDescriptor = RemoteRazorJsonSerializerContext.JsonServiceDescriptors.GetDescriptorForServiceFactory(serviceType);
        var traceSource = new TraceSource(nameof(RazorJsonServiceDescriptorTest));
        try
        {
            foreach (var serviceDescriptor in new[]
            {
                descriptor,
                (ServiceJsonRpcDescriptor)descriptor.WithTraceSource(traceSource),
                remoteDescriptor,
                (ServiceJsonRpcDescriptor)remoteDescriptor.WithTraceSource(traceSource),
            })
            {
                var formatter = new TestDescriptor(serviceDescriptor).CreateTestFormatter();
                var options = formatter.JsonSerializerOptions;

                Assert.Contains(ReflectionFallbackJsonTypeInfoResolver.Instance, options.TypeInfoResolverChain);
                Assert.DoesNotContain(options.TypeInfoResolverChain, resolver => resolver is DefaultJsonTypeInfoResolver);
                Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(new Version(1, 2), options));
            }
        }
        finally
        {
            traceSource.Close();
        }
    }

    private sealed class TestDescriptor(
        ServiceJsonRpcDescriptor descriptor,
        Action<SystemTextJsonFormatter>? configureFormatter = null) : DelegatingServiceJsonRpcDescriptor(descriptor)
    {
        public SystemTextJsonFormatter CreateTestFormatter()
            => Assert.IsType<SystemTextJsonFormatter>(CreateFormatter());

        protected override IJsonRpcMessageFormatter CreateFormatter()
        {
            var formatter = Assert.IsType<SystemTextJsonFormatter>(base.CreateFormatter());
            configureFormatter?.Invoke(formatter);
            return formatter;
        }
    }

    [Theory]
    [MemberData(nameof(JsonServices))]
    public void BrokeredServiceContractsHaveGeneratedMetadata(Type serviceType)
    {
        foreach (var descriptor in new[]
        {
            RazorServices.JsonDescriptors.GetDescriptorForServiceFactory(serviceType),
            RemoteRazorJsonSerializerContext.JsonServiceDescriptors.GetDescriptorForServiceFactory(serviceType),
        })
        {
            var options = new TestDescriptor(descriptor).CreateTestFormatter().JsonSerializerOptions;
            options.MakeReadOnly();
            var pending = new Queue<Type>();
            foreach (var method in serviceType.GetMethods())
            {
                foreach (var parameter in method.GetParameters())
                {
                    if (parameter.ParameterType != typeof(CancellationToken))
                        pending.Enqueue(parameter.ParameterType);
                }

                if (method.ReturnType.IsGenericType)
                    pending.Enqueue(method.ReturnType.GenericTypeArguments.Single());
            }

            var visited = new HashSet<Type>();
            var missing = new SortedSet<string>();
            while (pending.Count > 0)
            {
                var type = pending.Dequeue();
                if (!visited.Add(type))
                    continue;

                if (type.HasElementType)
                    pending.Enqueue(type.GetElementType()!);
                foreach (var argument in type.GenericTypeArguments)
                    pending.Enqueue(argument);

                try
                {
                    foreach (var property in options.GetTypeInfo(type).Properties)
                        pending.Enqueue(property.PropertyType);
                }
                catch (NotSupportedException)
                {
                    missing.Add(type.ToString());
                }
            }

            Assert.True(missing.Count == 0, "Missing generated brokered-service metadata:\n" + string.Join("\n", missing));
        }
    }

    [Fact]
    public async Task BrokeredInitializationRoundTrips()
    {
        var descriptor = RazorServices.JsonDescriptors.GetDescriptorForServiceFactory(typeof(IRemoteClientInitializationService));
        var remoteDescriptor = RemoteRazorJsonSerializerContext.JsonServiceDescriptors.GetDescriptorForServiceFactory(typeof(IRemoteClientInitializationService));
        var (clientStream, serverStream) = FullDuplexStream.CreatePair();
        using var clientConnection = descriptor.ConstructRpcConnection(clientStream.UsePipe());
        using var serverConnection = remoteDescriptor.ConstructRpcConnection(serverStream.UsePipe());
        var service = new TestInitializationService();
        serverConnection.AddLocalRpcTarget(service);
        var client = clientConnection.ConstructRpcClient<IRemoteClientInitializationService>();
        serverConnection.StartListening();
        clientConnection.StartListening();
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        await client.InitializeLspAsync(new RemoteClientLSPInitializationOptions
        {
            TokenTypes = ["class"],
            TokenModifiers = ["static"],
            ClientCapabilities = new VSInternalClientCapabilities(),
        }, cancellationSource.Token);

        Assert.Equal(["class"], service.Options.TokenTypes);
        Assert.Equal(["static"], service.Options.TokenModifiers);
        Assert.NotNull(service.Options.ClientCapabilities);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BrokeredCompletionPreservesRequestsResultsAndErrors(bool throwOnServer)
    {
        var descriptor = RazorServices.JsonDescriptors.GetDescriptorForServiceFactory(typeof(IRemoteCompletionService));
        var remoteDescriptor = RemoteRazorJsonSerializerContext.JsonServiceDescriptors.GetDescriptorForServiceFactory(typeof(IRemoteCompletionService));
        var (clientStream, serverStream) = FullDuplexStream.CreatePair();
        using var clientConnection = descriptor.ConstructRpcConnection(clientStream.UsePipe());
        using var serverConnection = remoteDescriptor.ConstructRpcConnection(serverStream.UsePipe());
        var service = new TestCompletionService();
        serverConnection.AddLocalRpcTarget(service);
        var client = clientConnection.ConstructRpcClient<IRemoteCompletionService>();
        serverConnection.StartListening();
        clientConnection.StartListening();
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        var solution = new JsonSerializableRazorSolutionWrapper(123, 456, Solution: null);
        var document = new JsonSerializableDocumentId(Guid.NewGuid(), Guid.NewGuid());
        var item = new VSInternalCompletionItem { Label = throwOnServer ? "throw" : "test" };

        if (throwOnServer)
        {
            var exception = await Assert.ThrowsAsync<RemoteInvocationException>(
                () => client.ResolveCompletionItemAsync(solution, document, item, new RazorFormattingOptions(), cancellationSource.Token).AsTask());
            Assert.Contains("Expected completion failure", exception.Message);
        }
        else
        {
            var result = await client.ResolveCompletionItemAsync(solution, document, item, new RazorFormattingOptions(), cancellationSource.Token);
            Assert.Equal("test resolved", result.Label);
            Assert.True(Assert.IsType<JsonElement>(result.Data).GetProperty("resolved").GetBoolean());
        }

        Assert.Equal(solution, service.SolutionInfo);
        Assert.Equal(document, service.DocumentId);
    }

    [Fact]
    public async Task BrokeredCodeActionRoundTripsRemoteOwnedPayload()
    {
        var descriptor = RazorServices.JsonDescriptors.GetDescriptorForServiceFactory(typeof(IRemoteCodeActionsService));
        var traceSource = new TraceSource(nameof(BrokeredCodeActionRoundTripsRemoteOwnedPayload), SourceLevels.All);
        using var traceListener = new XunitTraceListener(testOutputHelper);
        traceSource.Listeners.Add(traceListener);
        var remoteDescriptor = (ServiceJsonRpcDescriptor)RemoteRazorJsonSerializerContext.JsonServiceDescriptors.GetDescriptorForServiceFactory(typeof(IRemoteCodeActionsService)).WithTraceSource(traceSource);
        var (clientStream, serverStream) = FullDuplexStream.CreatePair();
        using var clientConnection = descriptor.ConstructRpcConnection(clientStream.UsePipe());
        using var serverConnection = remoteDescriptor.ConstructRpcConnection(serverStream.UsePipe());
        var service = new Mock<IRemoteCodeActionsService>(MockBehavior.Strict);
        service.Setup(s => s.ResolveCodeActionAsync(
            It.IsAny<JsonSerializableRazorSolutionWrapper>(), It.IsAny<JsonSerializableDocumentId>(),
            It.Is<CodeAction>(action => action.Title == "Add using"), null,
            It.IsAny<RazorFormattingOptions>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<CodeAction>(new CodeAction
            {
                Title = "Add using",
                Data = new AddUsingsCodeActionParams { Namespace = "System.Text.Json" },
            }));
        serverConnection.AddLocalRpcTarget(service.Object);
        var client = clientConnection.ConstructRpcClient<IRemoteCodeActionsService>();
        serverConnection.StartListening();
        clientConnection.StartListening();
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        try
        {
            var result = await client.ResolveCodeActionAsync(
                new JsonSerializableRazorSolutionWrapper(123, 456, Solution: null),
                new JsonSerializableDocumentId(Guid.NewGuid(), Guid.NewGuid()),
                new CodeAction { Title = "Add using" }, delegatedCodeAction: null,
                new RazorFormattingOptions(), cancellationSource.Token);

            Assert.Equal("Add using", result.Title);
            Assert.Equal("System.Text.Json", Assert.IsType<JsonElement>(result.Data).GetProperty("namespace").GetString());
            service.VerifyAll();
        }
        finally
        {
            traceSource.Close();
        }
    }

    [Fact]
    public async Task BrokeredProductionFallbackReportsUnknownPayloadAndPreservesCompatibility()
    {
        var reportedTypes = new List<Type>();
        var fallback = new ReflectionFallbackJsonTypeInfoResolver(requireGeneratedMetadata: false, reportedTypes.Add);
        var descriptor = new TestDescriptor(
            RazorServices.JsonDescriptors.GetDescriptorForServiceFactory(typeof(IRemoteClientInitializationService)),
            formatter =>
            {
                var chain = formatter.JsonSerializerOptions.TypeInfoResolverChain;
                var index = chain.IndexOf(ReflectionFallbackJsonTypeInfoResolver.Instance);
                Assert.True(index >= 0);
                chain[index] = fallback;
            });
        var remoteDescriptor = RemoteRazorJsonSerializerContext.JsonServiceDescriptors.GetDescriptorForServiceFactory(typeof(IRemoteClientInitializationService));
        var (clientStream, serverStream) = FullDuplexStream.CreatePair();
        using var clientConnection = descriptor.ConstructRpcConnection(clientStream.UsePipe());
        using var serverConnection = remoteDescriptor.ConstructRpcConnection(serverStream.UsePipe());
        var service = new TestInitializationService();
        serverConnection.AddLocalRpcTarget(service);
        var client = clientConnection.ConstructRpcClient<IRemoteClientInitializationService>();
        serverConnection.StartListening();
        clientConnection.StartListening();
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        var options = new RemoteClientLSPInitializationOptions
        {
            TokenTypes = [],
            TokenModifiers = [],
            ClientCapabilities = new VSInternalClientCapabilities { Experimental = new Version(1, 2) },
        };

        await client.InitializeLspAsync(options, cancellationSource.Token);
        await client.InitializeLspAsync(options, cancellationSource.Token);

        Assert.Equal("1.2", service.Options.ClientCapabilities.Experimental);
        Assert.Equal([typeof(Version)], reportedTypes);
    }

    private sealed class TestInitializationService : IRemoteClientInitializationService
    {
        public RemoteClientLSPInitializationOptions Options { get; private set; }

        public ValueTask InitializeLspAsync(RemoteClientLSPInitializationOptions lspInitializationOptions, CancellationToken cancellationToken)
        {
            Options = lspInitializationOptions;
            return default;
        }
    }

    private sealed class TestCompletionService : IRemoteCompletionService
    {
        public JsonSerializableRazorSolutionWrapper SolutionInfo { get; private set; }
        public JsonSerializableDocumentId DocumentId { get; private set; }

        public ValueTask<CompletionPositionInfo?> GetPositionInfoAsync(
            JsonSerializableRazorSolutionWrapper solutionInfo, JsonSerializableDocumentId documentId,
            VSInternalCompletionContext completionContext, Position position, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask<RemoteResponse<CompletionResult>> GetCompletionAsync(
            JsonSerializableRazorSolutionWrapper solutionInfo, JsonSerializableDocumentId documentId,
            CompletionPositionInfo positionInfo, VSInternalCompletionContext completionContext,
            RazorCompletionOptions razorCompletionOptions, Guid correlationId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask<VSInternalCompletionItem> ResolveCompletionItemAsync(
            JsonSerializableRazorSolutionWrapper solutionInfo, JsonSerializableDocumentId id,
            VSInternalCompletionItem request, RazorFormattingOptions formattingOptions, CancellationToken cancellationToken)
        {
            SolutionInfo = solutionInfo;
            DocumentId = id;
            if (request.Label == "throw")
                throw new InvalidOperationException("Expected completion failure");

            return new(new VSInternalCompletionItem
            {
                Label = request.Label + " resolved",
                Data = new JsonObject { ["resolved"] = true },
            });
        }
    }
}
