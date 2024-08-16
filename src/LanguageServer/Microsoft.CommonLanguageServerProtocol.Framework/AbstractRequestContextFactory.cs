// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// <para>
/// A factory for creating <typeparamref name="TRequestContext"/> objects from <see cref="IQueueItem{RequestContextType}"/>'s.
/// </para>
/// <para>
/// RequestContext's are useful for passing document context, since by default <see cref="CreateRequestContextAsync{TRequestParam}(IQueueItem{TRequestContext}, IMethodHandler, TRequestParam, CancellationToken)"/>
/// is run on the queue thread (and thus no mutating requests may be executing simultaneously, preventing possible race conditions).
/// It also allows somewhere to pass things like the <see cref="ILspServices" /> or <see cref="ILspLogger" /> which are useful on a wide variety of requests.
/// </para>
/// </summary>
/// <typeparam name="TRequestContext">The type of the RequestContext to be used by the handler.</typeparam>
internal abstract class AbstractRequestContextFactory<TRequestContext>
{
    /// <summary>
    /// Create a <typeparamref name="TRequestContext"/> object from the given <see cref="IQueueItem{RequestContextType}"/>.
    /// Note - throwing in the implementation of this method will cause the server to shutdown.
    /// </summary>
    /// <param name="queueItem">The <see cref="IQueueItem{RequestContextType}"/> from which to create the request context.</param>
    /// <param name="methodHandler">The <see cref="IMethodHandler"/> for which to create the request context</param>
    /// <param name="requestParam">The request parameters.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The <typeparamref name="TRequestContext"/> for this request.</returns>
    /// <remarks>This method is called on the queue thread to allow context to be retrieved serially, without the possibility of race conditions from Mutating requests.</remarks>
    public abstract Task<TRequestContext> CreateRequestContextAsync<TRequestParam>(IQueueItem<TRequestContext> queueItem, IMethodHandler methodHandler, TRequestParam requestParam, CancellationToken cancellationToken);
}
