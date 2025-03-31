// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CustomMessageHandler;

/// <summary>
/// Represents the context of a custom message handler.
/// </summary>
public sealed class CustomMessageContext
{
    internal CustomMessageContext(Solution solution)
    {
        Solution = solution;
    }

    /// <summary>
    /// Gets the current solution state.
    /// </summary>
    public Solution Solution { get; }
}
