// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// The context presented to a <see cref="QuickInfoProvider"/> when providing quick info.
    /// </summary>
    internal sealed class QuickInfoContext
    {
        /// <summary>
        /// The document that quick info was requested within.
        /// </summary>
        public Document Document { get; }

        /// <summary>
        /// The caret position where quick info was requested from.
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// The cancellation token to use for this operation.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// Creates a <see cref="QuickInfoContext"/> instance.
        /// </summary>
        public QuickInfoContext(
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Position = position;
            CancellationToken = cancellationToken;
        }
    }
}
