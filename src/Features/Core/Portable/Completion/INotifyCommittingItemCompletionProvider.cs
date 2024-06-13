// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// Interface to implement if the provider want to sign up for notification when one of the items it provided
    /// is being committed by the host, since calling <see cref="CompletionProvider.GetChangeAsync"/> doesn't necessarily
    /// lead to commission.
    /// </summary>
    internal interface INotifyCommittingItemCompletionProvider
    {
        /// <summary>
        /// This is invoked when one of the items provided by this provider is being committed.
        /// </summary>
        /// <remarks>
        /// Host provides no guarantee when will this be called (i.e. pre or post document change), nor the text 
        /// change will actually happen at all (e.g. the commit operation might be cancelled due to cancellation/exception/etc.)
        /// </remarks>
        Task NotifyCommittingItemAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken);
    }
}
