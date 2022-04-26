// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.DocumentationComments;

[DataContract]
internal readonly record struct DocumentationCommentOptions(
    [property: DataMember(Order = 0)] LineFormattingOptions LineFormatting,
    [property: DataMember(Order = 1)] bool AutoXmlDocCommentGeneration)
{
    public bool UseTabs => LineFormatting.UseTabs;
    public int TabSize => LineFormatting.TabSize;
    public string NewLine => LineFormatting.NewLine;
}
