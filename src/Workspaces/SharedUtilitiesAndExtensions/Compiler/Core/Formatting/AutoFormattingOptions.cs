// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Formatting;

/// <summary>
/// Automatic (on-type) formatting options.
/// </summary>
[DataContract]
internal readonly record struct AutoFormattingOptions(
    [property: DataMember(Order = 0)] bool FormatOnReturn = true,
    [property: DataMember(Order = 1)] bool FormatOnTyping = true,
    [property: DataMember(Order = 2)] bool FormatOnSemicolon = true,
    [property: DataMember(Order = 3)] bool FormatOnCloseBrace = true)
{
    public AutoFormattingOptions()
        : this(FormatOnReturn: true)
    {
    }

    public static readonly AutoFormattingOptions Default = new();
}
