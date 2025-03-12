// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Copilot;

/// <summary>
/// Holds details about a replacement node, providing either a message explaining the absence of a replacement or the
/// replacement syntax node itself. One of <see cref="Message"/> or <see cref="ReplacementNode"/> must always be set.
/// </summary>
internal sealed class ImplementationDetails
{
    /// <summary>
    /// Gets the message explaining why a replacement node is not provided. Either this property or <see cref="ReplacementNode"/> must be set.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets the replacement syntax node. Either this property or <see cref="Message"/> must be set.
    /// </summary>
    public SyntaxNode? ReplacementNode { get; init; }
}
