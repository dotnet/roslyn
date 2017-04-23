// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal class SerializableSourceReferenceItem
    {
        public int DefinitionId;
        public SerializableDocumentSpan SourceSpan;
        public bool IsWrittenTo;

        internal static SerializableSourceReferenceItem Dehydrate(
            SourceReferenceItem referenceItem, int definitionId)
        {
            return new SerializableSourceReferenceItem
            {
                DefinitionId = definitionId,
                SourceSpan = SerializableDocumentSpan.Dehydrate(referenceItem.SourceSpan),
                IsWrittenTo = referenceItem.IsWrittenTo
            };
        }

        internal SourceReferenceItem Rehydrate(Solution solution, DefinitionItem definition)
            => new SourceReferenceItem(definition, SourceSpan.Rehydrate(solution), IsWrittenTo);
    }
}