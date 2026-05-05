// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

internal abstract class LSPRequestInvoker
{
    /// <summary>
    /// Reinvoke the request on the given server.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    /// <param name="method"></param>
    /// <param name="languageServerName"></param>
    /// <param name="parameters"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <remarks>When operating on a document the <see cref="ITextBuffer"/> overload should be used, since it guarantees ordering.</remarks>
    public abstract Task<ReinvokeResponse<TOut>> ReinvokeRequestOnServerAsync<TIn, TOut>(
        string method,
        string languageServerName,
        TIn parameters,
        CancellationToken cancellationToken)
        where TIn : notnull;

    /// <summary>
    /// Reinvoke the request on the given server.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    /// <param name="method"></param>
    /// <param name="languageServerName"></param>
    /// <param name="capabilitiesFilter"></param>
    /// <param name="parameters"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <remarks>When operating on a document the <see cref="ITextBuffer"/> overload should be used, since it guarantees ordering.</remarks>
    [Obsolete("Use the overload that doesn't take a capability filter")]
    public abstract Task<ReinvokeResponse<TOut>> ReinvokeRequestOnServerAsync<TIn, TOut>(
        string method,
        string languageServerName,
        Func<JToken, bool> capabilitiesFilter,
        TIn parameters,
        CancellationToken cancellationToken)
        where TIn : notnull;

    public abstract Task<ReinvocationResponse<TOut>?> ReinvokeRequestOnServerAsync<TIn, TOut>(
        ITextBuffer textBuffer,
        string method,
        string languageServerName,
        TIn parameters,
        CancellationToken cancellationToken)
        where TIn : notnull;

    [Obsolete("Use the overload that doesn't take a capability filter")]
    public abstract Task<ReinvocationResponse<TOut>?> ReinvokeRequestOnServerAsync<TIn, TOut>(
        ITextBuffer textBuffer,
        string method,
        string languageServerName,
        Func<JToken, bool> capabilitiesFilter,
        TIn parameters,
        CancellationToken cancellationToken)
        where TIn : notnull;

    /// <summary>
    /// Reinvoke the request on the given server.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    /// <param name="method"></param>
    /// <param name="contentType"></param>
    /// <param name="parameters"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <remarks>When operating on a document the <see cref="ITextBuffer"/> overload should be used, since it guarantees ordering.</remarks>
    [Obsolete("New callers should use a method that returns a ReinvocationResponse rather than an a ReinvokeResponse")]
    public abstract Task<IEnumerable<ReinvokeResponse<TOut>>> ReinvokeRequestOnMultipleServersAsync<TIn, TOut>(
        string method,
        string contentType,
        TIn parameters,
        CancellationToken cancellationToken)
        where TIn : notnull;

    /// <summary>
    /// Reinvoke the request on the given server.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    /// <param name="method"></param>
    /// <param name="contentType"></param>
    /// <param name="capabilitiesFilter"></param>
    /// <param name="parameters"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <remarks>When operating on a document the <see cref="ITextBuffer"/> overload should be used, since it guarantees ordering.</remarks>
    [Obsolete("New callers should use a method that returns a ReinvocationResponse rather than an a ReinvokeResponse")]
    public abstract Task<IEnumerable<ReinvokeResponse<TOut>>> ReinvokeRequestOnMultipleServersAsync<TIn, TOut>(
        string method,
        string contentType,
        Func<JToken, bool> capabilitiesFilter,
        TIn parameters,
        CancellationToken cancellationToken)
        where TIn : notnull;

    public abstract IAsyncEnumerable<ReinvocationResponse<TOut>> ReinvokeRequestOnMultipleServersAsync<TIn, TOut>(
        ITextBuffer textBuffer,
        string method,
        TIn parameters,
        CancellationToken cancellationToken)
        where TIn : notnull;

    [Obsolete("New callers should use a method that doesn't have a capabilities filter")]
    public abstract IAsyncEnumerable<ReinvocationResponse<TOut>> ReinvokeRequestOnMultipleServersAsync<TIn, TOut>(
        ITextBuffer textBuffer,
        string method,
        Func<JToken, bool> capabilitiesFilter,
        TIn parameters,
        CancellationToken cancellationToken)
        where TIn : notnull;
}
