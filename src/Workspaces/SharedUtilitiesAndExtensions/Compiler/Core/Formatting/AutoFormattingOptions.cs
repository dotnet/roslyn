// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Formatting;

/// <summary>
/// Automatic (on-type) formatting options.
/// </summary>
[DataContract]
internal readonly record struct AutoFormattingOptions
{
    [DataMember] public bool FormatOnReturn { get; init; } = true;
    [DataMember] public bool FormatOnTyping { get; init; } = true;
    [DataMember] public bool FormatOnSemicolon { get; init; } = true;
    [DataMember] public bool FormatOnCloseBrace { get; init; } = true;

    public AutoFormattingOptions()
    {
    }

    public static readonly AutoFormattingOptions Default = new();
}
