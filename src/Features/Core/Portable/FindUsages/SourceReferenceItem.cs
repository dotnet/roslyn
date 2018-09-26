// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        /// Additional information about the reference.
        /// Each entry represents a key-values pair of data. For example, consider the below entry:
        ///     { "ValueUsageInfo" } = { "Read", "Write" }
        /// This entry indicates that the reference has additional value usage information which indicate
        /// it is a read/write reference, such as say 'a++'.
        /// </summary>
        public ImmutableDictionary<string, ImmutableArray<string>> ReferenceInfo { get; }

        [Obsolete]
        public SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan, bool isWrittenTo)
        {
            Definition = definition;
            SourceSpan = sourceSpan;
            IsWrittenTo = isWrittenTo;
            ReferenceInfo = ImmutableDictionary<string, ImmutableArray<string>>.Empty;
        }

        public SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan, ImmutableDictionary<string, ImmutableArray<string>> referenceInfo)
        {
            Definition = definition;
            SourceSpan = sourceSpan;
            ReferenceInfo = referenceInfo ?? throw new ArgumentNullException(nameof(referenceInfo));
        }

        internal SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan, ValueUsageInfo valueUsageInfo)
            : this(definition, sourceSpan, CreateReferenceInfo(valueUsageInfo))
        {
            IsWrittenTo = valueUsageInfo.ContainsWriteOrWritableReference();
        }

        private static ImmutableDictionary<string, ImmutableArray<string>> CreateReferenceInfo(ValueUsageInfo valueUsageInfo)
        {
            var referenceInfo = ImmutableDictionary<string, ImmutableArray<string>>.Empty;

            var values = valueUsageInfo.ToValues();
            if (!values.IsEmpty)
            {
                referenceInfo = referenceInfo.Add(nameof(ValueUsageInfo), values);
            }

            return referenceInfo;
        }
    }
}
