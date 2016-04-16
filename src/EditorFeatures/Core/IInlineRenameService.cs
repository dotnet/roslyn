// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Text;

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
        InlineRenameSessionInfo StartInlineSession(Document document, TextSpan triggerSpan, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns the currently active inline session, or null if none is active.
        /// </summary>
        IInlineRenameSession ActiveSession { get; }
    }
}
