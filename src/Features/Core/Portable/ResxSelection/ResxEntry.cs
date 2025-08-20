// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ResxSelection;

/// <summary>
/// Represents a single entry in a .resx file.
/// </summary>
internal sealed record ResxEntry
{
    public string Key { get; }
    public string Value { get; }
    public string? Comment { get; }
    
    public ResxEntry(string key, string value, string? comment = null)
    {
        Key = key;
        Value = value;
        Comment = comment;
    }
}
