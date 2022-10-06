﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// <para>
/// A factory for creating <typeparamref name="TRequestContext"/> objects from <see cref="IQueueItem{RequestContextType}"/>'s.
/// </para>
/// <para>
/// RequestContext's are useful for passing document context, since by default <see cref="CreateRequestContextAsync{TRequestParam}(IQueueItem{TRequestContext}, TRequestParam, CancellationToken)"/>
/// is run on the queue thread (and thus no mutating requests may be executing simultaniously, preventing possible race conditions).
/// It also allows somewhere to pass things like the <see cref="ILspServices" /> or <see cref="ILspLogger" /> which are useful on a wide variety of requests.
/// </para>
/// </summary>
/// <typeparam name="TRequestContext">The type of the RequestContext to be used by the handler.</typeparam>
public interface IRequestContextFactory<TRequestContext>
{
    /// <summary>
    /// Create a <typeparamref name="TRequestContext"/> object from the given <see cref="IQueueItem{RequestContextType}"/>.
    /// Note - throwing in the implementation of this method will cause the server to shutdown.
    /// </summary>
    /// <param name="queueItem">The <see cref="IQueueItem{RequestContextType}"/> from which to create a request.</param>
    /// <param name="requestParam">The request parameters.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The <typeparamref name="TRequestContext"/> for this request.</returns>
    /// <remarks>This method is called on the queue thread to allow context to be retrieved serially, without the posibility of race conditions from Mutating requests.</remarks>
    Task<TRequestContext> CreateRequestContextAsync<TRequestParam>(IQueueItem<TRequestContext> queueItem, TRequestParam requestParam, CancellationToken cancellationToken);
}
