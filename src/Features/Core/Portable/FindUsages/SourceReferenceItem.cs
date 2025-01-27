// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Classification;

namespace Microsoft.CodeAnalysis.FindUsages;

/// <summary>
/// Information about a symbol's reference that can be used for display and 
/// navigation in an editor.
/// </summary>
internal sealed class SourceReferenceItem
{
    /// <summary>
    /// The definition this reference corresponds to.
    /// </summary>
    public DefinitionItem Definition { get; }

    /// <summary>
    /// The location of the source item.
    /// </summary>
    public DocumentSpan SourceSpan { get; }

    /// <summary>
    /// Precomputed classified spans for the corresponding <see cref="SourceSpan"/>.
    /// </summary>
    public ClassifiedSpansAndHighlightSpan? ClassifiedSpans { get; }

    /// <summary>
    /// If this reference is a location where the definition is written to.
    /// </summary>
    public bool IsWrittenTo { get; }

    /// <summary>
    /// Symbol usage info associated with the reference.
    /// This entry indicates that the reference has additional usage information, such as
    /// it is a read/write reference for 'a++'.
    /// </summary>
    public SymbolUsageInfo SymbolUsageInfo { get; }

    /// <summary>
    /// Additional properties for the reference.
    /// For example, { "ContainingTypeInfo" } = { "MyClass" }
    /// </summary>
    public ImmutableArray<(string key, string value)> AdditionalProperties { get; }

    private SourceReferenceItem(
        DefinitionItem definition,
        DocumentSpan sourceSpan,
        ClassifiedSpansAndHighlightSpan? classifiedSpans,
        SymbolUsageInfo symbolUsageInfo,
        ImmutableArray<(string key, string value)> additionalProperties,
        bool isWrittenTo)
    {
        Definition = definition;
        SourceSpan = sourceSpan;
        ClassifiedSpans = classifiedSpans;
        SymbolUsageInfo = symbolUsageInfo;
        IsWrittenTo = isWrittenTo;
        AdditionalProperties = additionalProperties.NullToEmpty();
    }

    // Used by F#
    internal SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan, ClassifiedSpansAndHighlightSpan? classifiedSpans)
        : this(definition, sourceSpan, classifiedSpans, SymbolUsageInfo.None)
    {
    }

    // Used by TypeScript
    internal SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan, ClassifiedSpansAndHighlightSpan? classifiedSpans, SymbolUsageInfo symbolUsageInfo)
        : this(definition, sourceSpan, classifiedSpans, symbolUsageInfo, additionalProperties: [])
    {
    }

    internal SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan, ClassifiedSpansAndHighlightSpan? classifiedSpans, SymbolUsageInfo symbolUsageInfo, ImmutableArray<(string key, string value)> additionalProperties)
        : this(definition, sourceSpan, classifiedSpans, symbolUsageInfo, additionalProperties, isWrittenTo: symbolUsageInfo.IsWrittenTo())
    {
    }
}
