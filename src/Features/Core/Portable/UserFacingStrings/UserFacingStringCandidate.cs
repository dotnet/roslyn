// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UserFacingStrings;

/// <summary>
/// Represents a string literal found in code that could potentially be user-facing.
/// </summary>
internal sealed record UserFacingStringCandidate
{
    public TextSpan Location { get; }
    public string Value { get; }
    public string Context { get; }

    public UserFacingStringCandidate(TextSpan location, string value, string context)
    {
        Location = location;
        Value = value;
        Context = context;
    }
}
