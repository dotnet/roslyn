// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Kind of placeholder for an <see cref="IInterpolatedStringHandlerArgumentPlaceholderOperation"/>.
    /// </summary>
    public enum InterpolatedStringArgumentPlaceholderKind
    {
        /// <summary>
        /// This is a placeholder for an argument from the containing method call, indexer access, or object creation.
        /// The corresponding argument index is accessed in <see cref="IInterpolatedStringHandlerArgumentPlaceholderOperation.ArgumentIndex"/>.
        /// </summary>
        CallsiteArgument,
        /// <summary>
        /// This is a placeholder for the receiver of the containing method call, indexer access, or object creation.
        /// </summary>
        CallsiteReceiver,
        /// <summary>
        /// This is a placeholder for the trailing bool out parameter of the interpolated string handler type. This bool
        /// controls whether the conditional evaluation for the rest of the interpolated string should be run after the
        /// constructor returns.
        /// </summary>
        TrailingValidityArgument,
    }
}
