// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FindUsages
{
    using UsageColumnInfoMap = ImmutableDictionary<string, ImmutableArray<string>>;

    /// <summary>
    /// Information about a symbol's reference that can be used for display and 
    /// navigation in an editor.
    /// </summary>
    internal sealed class SourceReferenceItem
    {
        // We can have only a handful of different values for enums within SymbolUsageInfo, so the maximum size of this dictionary is capped.
        // So, we store this as a static dictionary which will be held in memory for the lifetime of the process.
        private static readonly ConcurrentDictionary<SymbolUsageInfo, UsageColumnInfoMap> s_symbolUsageInfoToReferenceInfoMap
            = new ConcurrentDictionary<SymbolUsageInfo, UsageColumnInfoMap>();

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


        public string ContainingMemberInfo { get; }
        public string ContainingTypeInfo { get; }


        /// <summary>
        /// Additional information about the reference.
        /// Each entry represents a key-values pair of data. For example, consider the below entry:
        ///     { "ValueUsageInfo" } = { "Read", "Write" }
        /// This entry indicates that the reference has additional value usage information which indicate
        /// it is a read/write reference, such as say 'a++'.
        /// </summary>
        public UsageColumnInfoMap ReferenceUsageInfo { get; }

        [Obsolete]
        public SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan, bool isWrittenTo)
        {
            Definition = definition;
            SourceSpan = sourceSpan;
            IsWrittenTo = isWrittenTo;
            ReferenceUsageInfo = UsageColumnInfoMap.Empty;
        }

        public SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan, UsageColumnInfoMap referenceInfo)
        {
            Definition = definition;
            SourceSpan = sourceSpan;
            ReferenceUsageInfo = referenceInfo ?? UsageColumnInfoMap.Empty;
        }

        internal SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan, SymbolUsageInfo symbolUsageInfo, ContainingTypeInfo containingTypeInfo, ContainingMemberInfo containingMemberInfo)
            : this(definition, sourceSpan, GetOrCreateReferenceUsageInfo(symbolUsageInfo))
        {
            IsWrittenTo = symbolUsageInfo.IsWrittenTo();
            ContainingTypeInfo = containingTypeInfo.typeInfo;
            ContainingMemberInfo = containingMemberInfo.memberInfo;
        }

        private static UsageColumnInfoMap GetOrCreateReferenceUsageInfo(SymbolUsageInfo symbolUsageInfo)
        {
            var result = s_symbolUsageInfoToReferenceInfoMap.GetOrAdd(symbolUsageInfo, v => CreateReferenceUsageInfo(v));
            return result;
        }

        private static UsageColumnInfoMap CreateReferenceUsageInfo(SymbolUsageInfo symbolUsageInfo)
        {
            var referenceUsageInfoMap = UsageColumnInfoMap.Empty;
            if (!symbolUsageInfo.Equals(SymbolUsageInfo.None))
            {
                referenceUsageInfoMap = referenceUsageInfoMap.Add(nameof(SymbolUsageInfo), symbolUsageInfo.ToLocalizableValues());
            }

            return referenceUsageInfoMap;
        }
    }
}
