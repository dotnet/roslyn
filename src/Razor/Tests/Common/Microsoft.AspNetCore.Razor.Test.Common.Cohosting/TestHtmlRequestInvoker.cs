// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.Cohost;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

internal class TestHtmlRequestInvoker : IHtmlRequestInvoker
{
    private readonly Dictionary<string, Func<object, object?>> _getResponses;

    public TestHtmlRequestInvoker()
        : this(Array.Empty<(string, Func<object, object?>)>())
    {
    }

    public TestHtmlRequestInvoker(params (string method, object? response)[] htmlResponses)
        : this(htmlResponses.Select<(string method, object? response), (string, Func<object, object?>)>(kvp => (kvp.method, _ => kvp.response)).ToArray())
    {
    }

    public TestHtmlRequestInvoker(params (string method, Func<object, object?> getResponse)[] htmlResponses)
    {
        _getResponses = htmlResponses.ToDictionary(kvp => kvp.method, kvp => kvp.getResponse);
    }

    public Task<TResponse?> MakeHtmlLspRequestAsync<TRequest, TResponse>(TextDocument razorDocument, string method, TRequest request, TimeSpan threshold, Guid correlationId, CancellationToken cancellationToken) where TRequest : notnull
    {
        if (_getResponses is not null &&
            _getResponses.TryGetValue(method, out var getResponse))
        {
            return Task.FromResult((TResponse?)getResponse(request));
        }

        return Task.FromResult<TResponse?>(default);
    }
}
