// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// A factory for creating <typeparamref name="TRequestContext"/> objects from <see cref="IQueueItem{RequestContextType}"/>'s.
/// </summary>
/// <typeparam name="TRequestContext">The type of the RequestContext to be used by the handler.</typeparam>
public interface IRequestContextFactory<TRequestContext>
{
    /// <summary>
    /// Create a <typeparamref name="TRequestContext"/> object from the given <see cref="IQueueItem{RequestContextType}"/>.
    /// </summary>
    /// <param name="queueItem">The <see cref="IQueueItem{RequestContextType}"/> from which to create a request.</param>
    /// <param name="requestParam">The request parameters.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<TRequestContext> CreateRequestContextAsync<TRequestParam>(IQueueItem<TRequestContext> queueItem, TRequestParam requestParam, CancellationToken cancellationToken);
}
