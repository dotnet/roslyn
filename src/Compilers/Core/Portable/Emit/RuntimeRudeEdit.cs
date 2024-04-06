// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Emit;

/// <summary>
/// Describes rude edit to be reported at runtime.
/// </summary>
/// <param name="message">Error message.</param>
public readonly struct RuntimeRudeEdit(string message)
{
    public string Message { get; } = message;

    internal bool IsDefault
        => Message is null;
}
