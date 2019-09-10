// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FindSymbols.FindReferences;

namespace Microsoft.CodeAnalysis.FindUsages
{
    using AdditionalPropertiesWithMultipleValuesMap = ImmutableDictionary<string, ImmutableArray<string>>;

    /// <summary>
    /// Information about a symbol's reference that can be used for display and 
    /// navigation in an editor.
    /// </summary>
    internal sealed class SourceReferenceItem
    {
        // We can have only a handful of different values for enums within SymbolUsageInfo, so the maximum size of this dictionary is capped.
        // So, we store this as a static dictionary which will be held in memory for the lifetime of the process.
        private static readonly ConcurrentDictionary<SymbolUsageInfo, AdditionalPropertiesWithMultipleValuesMap> s_symbolUsageInfoToReferenceInfoMap
            = new ConcurrentDictionary<SymbolUsageInfo, AdditionalPropertiesWithMultipleValuesMap>();

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
        /// Additional properties for the reference that can have multiple values.
        /// Each entry represents a key-values pair of data. For example, consider the below entry:
        ///     { "ValueUsageInfo" } = { "Read", "Write" }
        /// This entry indicates that the reference has additional value usage information which indicate
        /// it is a read/write reference, such as say 'a++'.
        /// </summary>
        public AdditionalPropertiesWithMultipleValuesMap AdditionalPropertiesWithMultipleValues { get; }

        /// <summary>
        /// Additional properties for the reference that can have only one value.
        /// For example, { "ContainingTypeInfo" } = { "MyClass" }
        /// </summary>
        public ImmutableArray<FindUsageProperty> FindUsagesProperties { get; }

        [Obsolete]
        public SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan, bool isWrittenTo)
        {
            Definition = definition;
            SourceSpan = sourceSpan;
            IsWrittenTo = isWrittenTo;
            AdditionalPropertiesWithMultipleValues = AdditionalPropertiesWithMultipleValuesMap.Empty;
            FindUsagesProperties = ImmutableArray<FindUsageProperty>.Empty;
        }

        public SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan, AdditionalPropertiesWithMultipleValuesMap referenceInfo)
        {
            Definition = definition;
            SourceSpan = sourceSpan;
            AdditionalPropertiesWithMultipleValues = referenceInfo ?? AdditionalPropertiesWithMultipleValuesMap.Empty;
            FindUsagesProperties = ImmutableArray<FindUsageProperty>.Empty;
        }

        // Being used by TypeScript
        internal SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan, SymbolUsageInfo symbolUsageInfo)
            : this(definition, sourceSpan, GetOrCreateAdditionalPropertiesWithMultipleValuesMap(symbolUsageInfo))
        {
            IsWrittenTo = symbolUsageInfo.IsWrittenTo();
            FindUsagesProperties = ImmutableArray<FindUsageProperty>.Empty;
        }

        internal SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan, SymbolUsageInfo symbolUsageInfo, ImmutableArray<FindUsageProperty> findUsagesProperty)
            : this(definition, sourceSpan, GetOrCreateAdditionalPropertiesWithMultipleValuesMap(symbolUsageInfo))
        {
            IsWrittenTo = symbolUsageInfo.IsWrittenTo();
            FindUsagesProperties = findUsagesProperty.NullToEmpty();
        }

        private static AdditionalPropertiesWithMultipleValuesMap GetOrCreateAdditionalPropertiesWithMultipleValuesMap(SymbolUsageInfo symbolUsageInfo)
            => s_symbolUsageInfoToReferenceInfoMap.GetOrAdd(symbolUsageInfo, v => CreateReferenceUsageInfo(v));

        private static AdditionalPropertiesWithMultipleValuesMap CreateReferenceUsageInfo(SymbolUsageInfo symbolUsageInfo)
        {
            var additionalPropertiesWithMultipleValuesMap = AdditionalPropertiesWithMultipleValuesMap.Empty;
            if (!symbolUsageInfo.Equals(SymbolUsageInfo.None))
            {
                additionalPropertiesWithMultipleValuesMap = additionalPropertiesWithMultipleValuesMap.Add(nameof(SymbolUsageInfo), symbolUsageInfo.ToLocalizableValues());
            }

            return additionalPropertiesWithMultipleValuesMap;
        }
    }
}
