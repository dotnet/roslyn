// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Formatting;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Host;
#endif

namespace Microsoft.CodeAnalysis.Indentation;

[DataContract]
internal readonly record struct IndentationOptions(
    [property: DataMember(Order = 0)] SyntaxFormattingOptions FormattingOptions)
{
    [DataMember(Order = 1)] public AutoFormattingOptions AutoFormattingOptions { get; init; } = AutoFormattingOptions.Default;
    [DataMember(Order = 2)] public FormattingOptions2.IndentStyle IndentStyle { get; init; } = DefaultIndentStyle;

    public const FormattingOptions2.IndentStyle DefaultIndentStyle = FormattingOptions2.IndentStyle.Smart;

#if !CODE_STYLE
    public static IndentationOptions GetDefault(LanguageServices languageServices)
        => new(SyntaxFormattingOptions.GetDefault(languageServices));
#endif
}
