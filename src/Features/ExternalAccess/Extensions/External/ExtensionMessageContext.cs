// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Extensions;

/// <summary>
/// Represents the context of an extension message handler.
/// </summary>
public sealed class ExtensionMessageContext
{
    internal ExtensionMessageContext(Solution solution)
    {
        Solution = solution;
    }

    /// <summary>
    /// Gets the current solution state.
    /// </summary>
    public Solution Solution { get; }
}
