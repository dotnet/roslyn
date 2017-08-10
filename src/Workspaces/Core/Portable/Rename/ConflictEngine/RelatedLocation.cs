// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    /// <summary>
    /// Gives information about an identifier span that was affected by Rename (Reference or Non reference)
    /// </summary>
    internal sealed class RelatedLocation
    {
        // The Span of the original identifier if it was in source, otherwise the span to check for implicit references
        public TextSpan ConflictCheckSpan { get; }
        public RelatedLocationType Type { get; set; }
        public bool IsReference { get; }
        public DocumentId DocumentId { get; }

        // If there was a conflict at ConflictCheckSpan during rename, then the next phase in rename uses ComplexifiedTargetSpan span to be expanded to resolve the conflict
        public TextSpan ComplexifiedTargetSpan { get; }

        public RelatedLocation(TextSpan location, DocumentId documentId, RelatedLocationType type, bool isReference = false, TextSpan complexifiedTargetSpan = default)
        {
            this.ConflictCheckSpan = location;
            this.Type = type;
            this.IsReference = isReference;
            this.DocumentId = documentId;
            this.ComplexifiedTargetSpan = complexifiedTargetSpan;
        }
    }
}
