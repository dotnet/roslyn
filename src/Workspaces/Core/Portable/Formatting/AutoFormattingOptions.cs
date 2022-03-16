// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Formatting;

/// <summary>
/// Automatic formatting options.
/// </summary>
[DataContract]
internal readonly record struct AutoFormattingOptions(
    [property: DataMember(Order = 0)] FormattingOptions.IndentStyle IndentStyle = FormattingOptions.IndentStyle.Smart,
    [property: DataMember(Order = 1)] bool FormatOnReturn = true,
    [property: DataMember(Order = 2)] bool FormatOnTyping = true,
    [property: DataMember(Order = 3)] bool FormatOnSemicolon = true,
    [property: DataMember(Order = 4)] bool FormatOnCloseBrace = true)
{
    public AutoFormattingOptions()
        : this(IndentStyle: FormattingOptions.IndentStyle.Smart)
    {
    }

    public static readonly AutoFormattingOptions Default = new();
}
