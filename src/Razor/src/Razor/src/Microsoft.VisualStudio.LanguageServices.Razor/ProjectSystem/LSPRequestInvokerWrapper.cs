// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

namespace Microsoft.VisualStudio.Razor.ProjectSystem;

/// <summary>
/// Wraps LSPRequestInvoker so that MEF doesn't need to load the ContainedLanguage.dll to satisfy imports
/// </summary>
/// <param name="requestInvoker"></param>
[Export(typeof(LSPRequestInvokerWrapper))]
[method: ImportingConstructor]
internal sealed class LSPRequestInvokerWrapper(Lazy<LSPRequestInvoker> requestInvoker)
{
    private readonly Lazy<LSPRequestInvoker> _requestInvoker = requestInvoker;

    public Task<ReinvokeResponse<TOut>> ReinvokeRequestOnServerAsync<TIn, TOut>(
        string method,
        string languageServerName,
        TIn parameters,
        CancellationToken cancellationToken)
        where TIn : notnull
    {
        return _requestInvoker.Value.ReinvokeRequestOnServerAsync<TIn, TOut>(method, languageServerName, parameters, cancellationToken);
    }
}
