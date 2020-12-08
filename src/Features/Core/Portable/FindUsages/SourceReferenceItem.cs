// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FindUsages
{
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
        public ImmutableDictionary<string, string> AdditionalProperties { get; }

        private SourceReferenceItem(
            DefinitionItem definition,
            DocumentSpan sourceSpan,
            SymbolUsageInfo symbolUsageInfo,
            ImmutableDictionary<string, string> additionalProperties,
            bool isWrittenTo)
        {
            Definition = definition;
            SourceSpan = sourceSpan;
            SymbolUsageInfo = symbolUsageInfo;
            IsWrittenTo = isWrittenTo;
            AdditionalProperties = additionalProperties ?? ImmutableDictionary<string, string>.Empty;
        }

        // Used by F#
        internal SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan)
            : this(definition, sourceSpan, SymbolUsageInfo.None)
        {
        }

        // Used by TypeScript
        internal SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan, SymbolUsageInfo symbolUsageInfo)
            : this(definition, sourceSpan, symbolUsageInfo, additionalProperties: ImmutableDictionary<string, string>.Empty)
        {
        }

        internal SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan, SymbolUsageInfo symbolUsageInfo, ImmutableDictionary<string, string> additionalProperties)
            : this(definition, sourceSpan, symbolUsageInfo, additionalProperties, isWrittenTo: symbolUsageInfo.IsWrittenTo())
        {
        }
    }
}
