// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.DocumentationComments;

[DataContract]
internal readonly record struct DocumentationCommentOptions
{
    public static readonly DocumentationCommentOptions Default = new();

    [DataMember] public LineFormattingOptions LineFormatting { get; init; } = LineFormattingOptions.Default;
    [DataMember] public bool AutoXmlDocCommentGeneration { get; init; } = true;

    public DocumentationCommentOptions()
    {
    }

    public bool UseTabs => LineFormatting.UseTabs;
    public int TabSize => LineFormatting.TabSize;
    public string NewLine => LineFormatting.NewLine;
}
