// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests;

[LanguageServerEndpoint(Name)]
internal class TestMethodHandler : IRequestHandler<int, string, TestRequestContext>
{
    public const string Name = "Method";
    public static readonly IMethodHandler Instance = new TestMethodHandler();

    public bool MutatesSolutionState => true;
    public static Type RequestType = typeof(int);
    public static Type ResponseType = typeof(string);
    public static RequestHandlerMetadata Metadata = new(Name, RequestType, ResponseType);

    public Task<string> HandleRequestAsync(int request, TestRequestContext context, CancellationToken cancellationToken)
        => Task.FromResult("stuff");
}

[LanguageServerEndpoint(Name)]
internal class TestParameterlessMethodHandler : IRequestHandler<bool, TestRequestContext>
{
    public const string Name = "ParameterlessMethod";
    public static readonly IMethodHandler Instance = new TestParameterlessMethodHandler();

    public bool MutatesSolutionState => true;

    public static Type ResponseType = typeof(bool);
    public static RequestHandlerMetadata Metadata = new(Name, RequestType: null, ResponseType);

    public Task<bool> HandleRequestAsync(TestRequestContext context, CancellationToken cancellationToken)
        => Task.FromResult(true);
}

[LanguageServerEndpoint(Name)]
internal class TestNotificationHandler : INotificationHandler<bool, TestRequestContext>
{
    public const string Name = "Notification";
    public static readonly IMethodHandler Instance = new TestNotificationHandler();

    public bool MutatesSolutionState => true;
    public static Type RequestType = typeof(bool);
    public static readonly RequestHandlerMetadata Metadata = new(Name, RequestType, ResponseType: null);

    public Task HandleNotificationAsync(bool request, TestRequestContext context, CancellationToken cancellationToken)
        => Task.FromResult(true);
}

[LanguageServerEndpoint(Name)]
internal class TestParameterlessNotificationHandler : INotificationHandler<TestRequestContext>
{
    public const string Name = "ParameterlessNotification";
    public static readonly IMethodHandler Instance = new TestParameterlessNotificationHandler();

    public bool MutatesSolutionState => true;
    public static readonly RequestHandlerMetadata Metadata = new(Name, RequestType: null, ResponseType: null);

    public Task HandleNotificationAsync(TestRequestContext context, CancellationToken cancellationToken)
        => Task.FromResult(true);
}

internal class TestMethodHandlerWithoutAttribute : INotificationHandler<TestRequestContext>
{
    public bool MutatesSolutionState => true;

    public Task HandleNotificationAsync(TestRequestContext requestContext, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

[LanguageServerEndpoint(Name)]
public class MutatingHandler : IRequestHandler<int, string, TestRequestContext>
{
    public const string Name = "MutatingMethod";
    public static readonly IMethodHandler Instance = new MutatingHandler();
    public static readonly RequestHandlerMetadata Metadata = new(Name, RequestType: typeof(int), ResponseType: typeof(string));

    public MutatingHandler()
    {
    }

    public bool MutatesSolutionState => true;

    public Task<string> HandleRequestAsync(int request, TestRequestContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(string.Empty);
    }
}

[LanguageServerEndpoint(Name)]
public class CompletingHandler : IRequestHandler<int, string, TestRequestContext>
{
    public const string Name = "CompletingMethod";
    public static readonly IMethodHandler Instance = new CompletingHandler();
    public static readonly RequestHandlerMetadata Metadata = new(Name, RequestType: typeof(int), ResponseType: typeof(string));

    public bool MutatesSolutionState => false;

    public async Task<string> HandleRequestAsync(int request, TestRequestContext context, CancellationToken cancellationToken)
    {
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return "I completed!";
            }
            await Task.Delay(100);
        }
    }
}

[LanguageServerEndpoint(Name)]
public class CancellingHandler : IRequestHandler<int, string, TestRequestContext>
{
    public const string Name = "CancellingMethod";
    public static readonly IMethodHandler Instance = new CancellingHandler();
    public static readonly RequestHandlerMetadata Metadata = new(Name, RequestType: typeof(int), ResponseType: typeof(string));

    public bool MutatesSolutionState => false;

    public async Task<string> HandleRequestAsync(int request, TestRequestContext context, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(100);
        }
    }
}

[LanguageServerEndpoint(Name)]
public class ThrowingHandler : IRequestHandler<int, string, TestRequestContext>
{
    public const string Name = "ThrowingMethod";
    public static readonly IMethodHandler Instance = new ThrowingHandler();
    public static readonly RequestHandlerMetadata Metadata = new(Name, RequestType: typeof(int), ResponseType: typeof(string));

    public bool MutatesSolutionState => false;

    public Task<string> HandleRequestAsync(int request, TestRequestContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
