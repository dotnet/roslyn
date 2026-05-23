// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Text;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.VisualStudio.Razor.LanguageClient;

internal sealed class TestLSPRequestInvoker(params IEnumerable<(string method, object? response)> responses) : LSPRequestInvoker
{
    private readonly Dictionary<string, object?> _responses = responses.ToDictionary(kvp => kvp.method, kvp => kvp.response);

    public Action<object>? RequestAction { get; set; }

    [Obsolete]
    public override Task<IEnumerable<ReinvokeResponse<TOut>>> ReinvokeRequestOnMultipleServersAsync<TIn, TOut>(
        string method,
        string contentType,
        TIn parameters,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    [Obsolete]
    public override Task<IEnumerable<ReinvokeResponse<TOut>>> ReinvokeRequestOnMultipleServersAsync<TIn, TOut>(
        string method,
        string contentType,
        Func<JToken, bool> capabilitiesFilter,
        TIn parameters,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public override IAsyncEnumerable<ReinvocationResponse<TOut>> ReinvokeRequestOnMultipleServersAsync<TIn, TOut>(
        ITextBuffer textBuffer,
        string method,
        TIn parameters,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    [Obsolete]
    public override IAsyncEnumerable<ReinvocationResponse<TOut>> ReinvokeRequestOnMultipleServersAsync<TIn, TOut>(
        ITextBuffer textBuffer,
        string method,
        Func<JToken, bool> capabilitiesFilter,
        TIn parameters,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public override Task<ReinvokeResponse<TOut>> ReinvokeRequestOnServerAsync<TIn, TOut>(
        string method,
        string languageServerName,
        TIn parameters,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    [Obsolete]
    public override Task<ReinvokeResponse<TOut>> ReinvokeRequestOnServerAsync<TIn, TOut>(
        string method,
        string languageServerName,
        Func<JToken, bool> capabilitiesFilter,
        TIn parameters,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public override Task<ReinvocationResponse<TOut>?> ReinvokeRequestOnServerAsync<TIn, TOut>(
        ITextBuffer textBuffer,
        string method,
        string languageServerName,
        TIn parameters,
        CancellationToken cancellationToken)
    {
        RequestAction?.Invoke(parameters);

        Assert.True(_responses.TryGetValue(method, out var response), $"'{method}' was not defined with a response.");

        return Task.FromResult(new ReinvocationResponse<TOut>(languageClientName: "html", (TOut?)response)).AsNullable();
    }

    [Obsolete]
    public override Task<ReinvocationResponse<TOut>?> ReinvokeRequestOnServerAsync<TIn, TOut>(
        ITextBuffer textBuffer,
        string method,
        string languageServerName,
        Func<JToken, bool> capabilitiesFilter,
        TIn parameters,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
