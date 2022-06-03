// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Rename;

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// Provides services for starting an interactive rename session.
    /// </summary>
    internal interface IInlineRenameService
    {
        /// <summary>
        /// Starts an interactive rename session. If an existing inline session was active, it will
        /// commit the previous session, possibly causing changes to the text buffer.
        /// </summary>
        /// <param name="document">The Document containing the triggerSpan.</param>
        /// <param name="triggerSpan">The triggerSpan itself.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The rename session.</returns>
        InlineRenameSessionInfo StartInlineSession(Document document, TextSpan triggerSpan, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the currently active inline session, or null if none is active.
        /// </summary>
        IInlineRenameSession? ActiveSession { get; }
    }
}
