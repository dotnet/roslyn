// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests;

internal sealed record class MockRequest(int Param);
internal sealed record class MockResponse(string Response);

[LanguageServerEndpoint(Name, LanguageServerConstants.DefaultLanguageName)]
internal sealed class TestMethodHandler : IRequestHandler<MockRequest, MockResponse, TestRequestContext>
{
    public const string Name = "Method";
    public static readonly IMethodHandler Instance = new TestMethodHandler();

    public bool MutatesSolutionState => true;
    public static TypeRef RequestType = TypeRef.Of<MockRequest>();
    public static TypeRef ResponseType = TypeRef.Of<MockResponse>();
    public static RequestHandlerMetadata Metadata = new(Name, RequestType, ResponseType, LanguageServerConstants.DefaultLanguageName);

    public Task<MockResponse> HandleRequestAsync(MockRequest request, TestRequestContext context, CancellationToken cancellationToken)
        => Task.FromResult<MockResponse>(new("stuff"));
}

[LanguageServerEndpoint(Name, LanguageServerConstants.DefaultLanguageName)]
internal sealed class TestParameterlessMethodHandler : IRequestHandler<MockResponse, TestRequestContext>
{
    public const string Name = "ParameterlessMethod";
    public static readonly IMethodHandler Instance = new TestParameterlessMethodHandler();

    public bool MutatesSolutionState => true;

    public static TypeRef ResponseTypeRef = TypeRef.Of<MockResponse>();
    public static RequestHandlerMetadata Metadata = new(Name, RequestTypeRef: null, ResponseTypeRef, LanguageServerConstants.DefaultLanguageName);

    public Task<MockResponse> HandleRequestAsync(TestRequestContext context, CancellationToken cancellationToken)
        => Task.FromResult(new MockResponse("true"));
}

[LanguageServerEndpoint(Name, LanguageServerConstants.DefaultLanguageName)]
internal sealed class TestNotificationHandler : INotificationHandler<MockRequest, TestRequestContext>
{
    public const string Name = "Notification";
    public static readonly IMethodHandler Instance = new TestNotificationHandler();

    public bool MutatesSolutionState => true;
    public static TypeRef RequestTypeRef = TypeRef.Of<MockRequest>();
    public static readonly RequestHandlerMetadata Metadata = new(Name, RequestTypeRef, ResponseTypeRef: null, LanguageServerConstants.DefaultLanguageName);

    public Task HandleNotificationAsync(MockRequest request, TestRequestContext context, CancellationToken cancellationToken)
        => Task.FromResult(true);
}

[LanguageServerEndpoint(Name, LanguageServerConstants.DefaultLanguageName)]
internal sealed class TestParameterlessNotificationHandler : INotificationHandler<TestRequestContext>
{
    public const string Name = "ParameterlessNotification";
    public static readonly IMethodHandler Instance = new TestParameterlessNotificationHandler();

    public bool MutatesSolutionState => true;
    public static readonly RequestHandlerMetadata Metadata = new(Name, RequestTypeRef: null, ResponseTypeRef: null, LanguageServerConstants.DefaultLanguageName);

    public Task HandleNotificationAsync(TestRequestContext context, CancellationToken cancellationToken)
        => Task.FromResult(true);
}

internal sealed class TestMethodHandlerWithoutAttribute : INotificationHandler<TestRequestContext>
{
    public bool MutatesSolutionState => true;

    public Task HandleNotificationAsync(TestRequestContext requestContext, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

[LanguageServerEndpoint(Name, LanguageServerConstants.DefaultLanguageName)]
internal sealed class MutatingHandler : IRequestHandler<MockRequest, MockResponse, TestRequestContext>
{
    public const string Name = "MutatingMethod";
    public static readonly IMethodHandler Instance = new MutatingHandler();

    public static readonly RequestHandlerMetadata Metadata = new(Name, RequestTypeRef: TypeRef.Of<MockRequest>(), ResponseTypeRef: TypeRef.Of<MockResponse>(), LanguageServerConstants.DefaultLanguageName);

    public MutatingHandler()
    {
    }

    public bool MutatesSolutionState => true;

    public Task<MockResponse> HandleRequestAsync(MockRequest request, TestRequestContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new MockResponse(string.Empty));
    }
}

[LanguageServerEndpoint(Name, LanguageServerConstants.DefaultLanguageName)]
internal sealed class CompletingHandler : IRequestHandler<MockRequest, MockResponse, TestRequestContext>
{
    public const string Name = "CompletingMethod";
    public static readonly IMethodHandler Instance = new CompletingHandler();
    public static readonly RequestHandlerMetadata Metadata = new(Name, RequestTypeRef: TypeRef.Of<MockRequest>(), ResponseTypeRef: TypeRef.Of<MockResponse>(), LanguageServerConstants.DefaultLanguageName);

    public bool MutatesSolutionState => false;

    public async Task<MockResponse> HandleRequestAsync(MockRequest request, TestRequestContext context, CancellationToken cancellationToken)
    {
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new("I completed!");
            }
            await Task.Delay(100, cancellationToken).NoThrowAwaitable();
        }
    }
}

[LanguageServerEndpoint(Name, LanguageServerConstants.DefaultLanguageName)]
internal sealed class CancellingHandler : IRequestHandler<MockRequest, MockResponse, TestRequestContext>
{
    public const string Name = "CancellingMethod";
    public static readonly IMethodHandler Instance = new CancellingHandler();
    public static readonly RequestHandlerMetadata Metadata = new(Name, RequestTypeRef: TypeRef.Of<MockRequest>(), ResponseTypeRef: TypeRef.Of<MockResponse>(), LanguageServerConstants.DefaultLanguageName);

    public bool MutatesSolutionState => false;

    public async Task<MockResponse> HandleRequestAsync(MockRequest request, TestRequestContext context, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(100, cancellationToken).NoThrowAwaitable();
        }
    }
}

[LanguageServerEndpoint(Name, LanguageServerConstants.DefaultLanguageName)]
internal sealed class ThrowingHandler : IRequestHandler<MockRequest, MockResponse, TestRequestContext>
{
    public const string Name = "ThrowingMethod";
    public static readonly IMethodHandler Instance = new ThrowingHandler();
    public static readonly RequestHandlerMetadata Metadata = new(Name, RequestTypeRef: TypeRef.Of<MockRequest>(), ResponseTypeRef: TypeRef.Of<MockResponse>(), LanguageServerConstants.DefaultLanguageName);

    public bool MutatesSolutionState => false;

    public Task<MockResponse> HandleRequestAsync(MockRequest request, TestRequestContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

[LanguageServerEndpoint(Name, LanguageServerConstants.DefaultLanguageName)]
internal sealed class TestDefaultLanguageHandler : IRequestHandler<MockRequest, MockResponse, TestRequestContext>
{
    public const string Name = "Language";
    public static readonly IMethodHandler Instance = new TestDefaultLanguageHandler();

    public bool MutatesSolutionState => true;
    public static TypeRef RequestType = TypeRef.Of<MockRequest>();
    public static TypeRef ResponseType = TypeRef.Of<MockResponse>();
    public static RequestHandlerMetadata Metadata = new(Name, RequestType, ResponseType, LanguageServerConstants.DefaultLanguageName);

    public Task<MockResponse> HandleRequestAsync(MockRequest request, TestRequestContext context, CancellationToken cancellationToken)
        => Task.FromResult(new MockResponse(string.Empty));
}

[LanguageServerEndpoint(Name, Language)]
internal sealed class TestXamlLanguageHandler : IRequestHandler<MockRequest, MockResponse, TestRequestContext>
{
    public const string Name = TestDefaultLanguageHandler.Name;
    public const string Language = "xaml";
    public static readonly IMethodHandler Instance = new TestXamlLanguageHandler();

    public bool MutatesSolutionState => true;
    public static TypeRef RequestType = TypeRef.Of<MockRequest>();
    public static TypeRef ResponseType = TypeRef.Of<MockResponse>();
    public static RequestHandlerMetadata Metadata = new(Name, RequestType, ResponseType, Language);

    public Task<MockResponse> HandleRequestAsync(MockRequest request, TestRequestContext context, CancellationToken cancellationToken)
        => Task.FromResult(new MockResponse("xaml"));
}

