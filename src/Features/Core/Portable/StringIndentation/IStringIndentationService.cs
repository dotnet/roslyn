// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.StringIndentation;

internal interface IStringIndentationService : ILanguageService
{
    Task<ImmutableArray<StringIndentationRegion>> GetStringIndentationRegionsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken);
}

internal readonly struct StringIndentationRegion(TextSpan indentSpan, ImmutableArray<TextSpan> holeSpans = default)
{
    /// <summary>
    /// The entire span of the indent region.  Given code like:
    /// 
    /// <code>
    /// var x = """
    ///         x
    ///         y
    ///         """;
    /// </code>
    /// 
    /// The span will be the region between the ^'s in:
    /// 
    /// <code>
    /// ^var x = """
    ///         x
    ///         y
    ///         ^""";
    /// </code>
    /// 
    /// The span must be on the start and end lines of the string literal as those are the only lines with content
    /// known to exist.  In other words, the lines with content on them may be entire empty (or still shorter than
    /// the indent column), so there's no actual position to associate the span with.
    /// 
    /// The start of the span should be the start of the line that the string literal starts on.  The end of the
    /// span should be at the start of the ending quotes of the literal.
    /// 
    /// The tagger can then use this span to draw a line like so:
    /// 
    /// <code>
    /// var x = """
    ///        |x
    ///        |y
    ///         """;
    /// </code>
    /// </summary>
    public readonly TextSpan IndentSpan = indentSpan;

    /// <summary>
    /// Regions of the literal that count as 'code holes' and which the lines of the tagger should not draw through.
    /// For example, given code like:
    /// 
    /// <code>
    /// var x = $"""
    ///         x
    ///         {
    ///             1 + 1
    ///         } xcont
    ///         y
    ///         {
    ///             2 + 2
    ///         } ycont
    ///         z
    ///         """;
    /// </code>
    /// 
    /// Then there will be two holes demarcated by the ^'s in the following:
    /// 
    /// <code>
    /// var x = $"""
    ///         x
    ///         ^{
    ///             1 + 1
    ///         }^ xcont
    ///         y
    ///         ^{
    ///             2 + 2
    ///         }^ ycont
    ///         z
    ///         """;
    /// </code>
    /// 
    /// If the line draw were to intersect one of these spans it will not be drawn, causing the following to be
    /// presented:
    /// 
    /// <code>
    /// var x = $"""
    ///        |x
    ///        |{
    ///             1 + 1
    ///         } xcont
    ///        |y
    ///        |{
    ///             2 + 2
    ///         } ycont
    ///        |z
    ///         """;
    /// </code>
    /// 
    /// </summary>
    public readonly ImmutableArray<TextSpan> OrderedHoleSpans = holeSpans.NullToEmpty().Sort();
}
