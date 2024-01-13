// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// The context presented to a <see cref="QuickInfoProvider"/> when providing quick info.
    /// </summary>
    /// <remarks>
    /// Creates a <see cref="QuickInfoContext"/> instance.
    /// </remarks>
    internal sealed class QuickInfoContext(
        Document document,
        int position,
        SymbolDescriptionOptions options,
        CancellationToken cancellationToken)
    {
        /// <summary>
        /// The document that quick info was requested within.
        /// </summary>
        public Document Document { get; } = document ?? throw new ArgumentNullException(nameof(document));

        /// <summary>
        /// The caret position where quick info was requested from.
        /// </summary>
        public int Position { get; } = position;

        public SymbolDescriptionOptions Options { get; } = options;

        /// <summary>
        /// The cancellation token to use for this operation.
        /// </summary>
        public CancellationToken CancellationToken { get; } = cancellationToken;
    }
}
