// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Formatting.Rules;

/// <summary>
/// Options for <see cref="IndentBlockOperation"/>.
/// </summary>
[Flags]
internal enum IndentBlockOption
{
    /// <summary>
    /// This indentation will be a delta to the first token in the line in which the base token is present
    /// </summary>
    RelativeToFirstTokenOnBaseTokenLine = 0x2,

    /// <summary>
    /// <see cref="IndentBlockOperation.IndentationDeltaOrPosition"/> will be interpreted as delta of its enclosing indentation
    /// </summary>
    RelativePosition = 0x4,

    /// <summary>
    /// <see cref="IndentBlockOperation.IndentationDeltaOrPosition"/> will be interpreted as absolute position
    /// </summary>
    AbsolutePosition = 0x8,

    /// <summary>
    /// Mask for relative position options
    /// </summary>
    RelativePositionMask = RelativeToFirstTokenOnBaseTokenLine | RelativePosition,

    /// <summary>
    /// Mask for position options.
    /// </summary>
    /// <remarks>
    /// Each <see cref="IndentBlockOperation"/> specifies one of the position options to indicate the primary
    /// behavior for the operation.
    /// </remarks>
    PositionMask = RelativeToFirstTokenOnBaseTokenLine | RelativePosition | AbsolutePosition,

    /// <summary>
    /// Increase the <see cref="IndentBlockOperation.IndentationDeltaOrPosition"/> if the block is part of a
    /// condition of the anchor token. For example:
    /// 
    /// <code>
    /// if (value is
    ///     { // This open brace token is part of a condition of the 'if' token.
    ///         Length: 2
    ///     })
    /// </code>
    /// </summary>
    IndentIfConditionOfAnchorToken = 0x10,
}
