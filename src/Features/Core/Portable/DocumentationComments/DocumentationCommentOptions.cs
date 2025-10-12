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

    /// <summary>
    /// When true, generates the summary tag on a single line (e.g., /// &lt;summary&gt;&lt;/summary&gt;)
    /// instead of spanning multiple lines.
    /// </summary>
    [DataMember] public bool GenerateSummaryTagOnSingleLine { get; init; } = false;

    /// <summary>
    /// When true, only generates the &lt;summary&gt; tag and omits other tags like
    /// &lt;param&gt;, &lt;typeparam&gt;, &lt;returns&gt;, and &lt;exception&gt;.
    /// </summary>
    [DataMember] public bool GenerateOnlySummaryTag { get; init; } = false;

    public DocumentationCommentOptions()
    {
    }

    public bool UseTabs => LineFormatting.UseTabs;
    public int TabSize => LineFormatting.TabSize;
    public string NewLine => LineFormatting.NewLine;
}
