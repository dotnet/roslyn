// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindUsages
{
    /// <summary>
    /// Information about a symbol's reference that can be used for display and 
    /// navigation in an editor.
    /// </summary>
    internal sealed class SourceReferenceItem
    {
        // We can have only a handful of different values for ValueUsageInfo flags enum, so the maximum size of this dictionary is capped.
        // So, we store this as a static dictionary which will be held in memory for the lifetime of the process.
        private static readonly ConcurrentDictionary<ValueUsageInfo, MultiDictionary<string, string>> s_valueUsageInfoToReferenceInfoMap
            = new ConcurrentDictionary<ValueUsageInfo, MultiDictionary<string, string>>();

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
        public MultiDictionary<string, string> ReferenceInfo { get; }

        [Obsolete]
        public SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan, bool isWrittenTo)
        {
            Definition = definition;
            SourceSpan = sourceSpan;
            IsWrittenTo = isWrittenTo;
            ReferenceInfo = GetOrCreateReferenceInfo(ValueUsageInfo.None);
        }

        public SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan, MultiDictionary<string, string> referenceInfo)
        {
            Definition = definition;
            SourceSpan = sourceSpan;
            ReferenceInfo = referenceInfo ?? throw new ArgumentNullException(nameof(referenceInfo));
        }

        internal SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan, ValueUsageInfo valueUsageInfo)
            : this(definition, sourceSpan, GetOrCreateReferenceInfo(valueUsageInfo))
        {
            IsWrittenTo = valueUsageInfo.IsWrittenTo();
        }

        private static MultiDictionary<string, string> GetOrCreateReferenceInfo(ValueUsageInfo valueUsageInfo)
            => s_valueUsageInfoToReferenceInfoMap.GetOrAdd(valueUsageInfo, CreateReferenceInfo);

        private static MultiDictionary<string, string> CreateReferenceInfo(ValueUsageInfo valueUsageInfo)
        {
            var referenceInfo = new MultiDictionary<string, string>();
            foreach (var value in valueUsageInfo.ToLocalizableValues())
            {
                referenceInfo.Add(nameof(ValueUsageInfo), value);
            }

            return referenceInfo;
        }
    }
}
