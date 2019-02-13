// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FindUsages
{
    using ReferenceInfoMap = ImmutableDictionary<string, ImmutableArray<string>>;

    /// <summary>
    /// Information about a symbol's reference that can be used for display and 
    /// navigation in an editor.
    /// </summary>
    internal sealed class SourceReferenceItem
    {
        // We can have only a handful of different values for ValueUsageInfo flags enum, so the maximum size of this dictionary is capped.
        // So, we store this as a static dictionary which will be held in memory for the lifetime of the process.
        private static readonly ConcurrentDictionary<ValueUsageInfo, ReferenceInfoMap> s_valueUsageInfoToReferenceInfoMap
            = new ConcurrentDictionary<ValueUsageInfo, ReferenceInfoMap>();

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
        /// Additional information about the reference.
        /// Each entry represents a key-values pair of data. For example, consider the below entry:
        ///     { "ValueUsageInfo" } = { "Read", "Write" }
        /// This entry indicates that the reference has additional value usage information which indicate
        /// it is a read/write reference, such as say 'a++'.
        /// </summary>
        public ReferenceInfoMap ReferenceInfo { get; }

        [Obsolete]
        public SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan, bool isWrittenTo)
        {
            Definition = definition;
            SourceSpan = sourceSpan;
            IsWrittenTo = isWrittenTo;
            ReferenceInfo = ReferenceInfoMap.Empty;
        }

        public SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan, ReferenceInfoMap referenceInfo)
        {
            Definition = definition;
            SourceSpan = sourceSpan;
            ReferenceInfo = referenceInfo ?? ReferenceInfoMap.Empty;
        }

        internal SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan, ValueUsageInfo valueUsageInfo)
            : this(definition, sourceSpan, GetOrCreateReferenceInfo(valueUsageInfo))
        {
            IsWrittenTo = valueUsageInfo.IsWrittenTo();
        }

        private static ReferenceInfoMap GetOrCreateReferenceInfo(ValueUsageInfo valueUsageInfo)
            => s_valueUsageInfoToReferenceInfoMap.GetOrAdd(valueUsageInfo, v => CreateReferenceInfo(v));

        private static ReferenceInfoMap CreateReferenceInfo(ValueUsageInfo valueUsageInfo)
        {
            var referenceInfoMap = ReferenceInfoMap.Empty;
            if (valueUsageInfo != ValueUsageInfo.None)
            {
                referenceInfoMap = referenceInfoMap.Add(nameof(ValueUsageInfo), valueUsageInfo.ToLocalizableValues());
            }

            return referenceInfoMap;
        }
    }
}
