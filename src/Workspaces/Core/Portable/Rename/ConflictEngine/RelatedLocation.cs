// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    /// <summary>
    /// Gives information about an identifier span that was affected by Rename (Reference or Non reference)
    /// </summary>
    internal readonly struct RelatedLocation : IEquatable<RelatedLocation>
    {
        /// <summary>
        /// The Span of the original identifier if it was in source, otherwise the span to check for implicit
        /// references.
        /// </summary>
        public readonly TextSpan ConflictCheckSpan;
        public readonly RelatedLocationType Type;
        public readonly bool IsReference;
        public readonly DocumentId DocumentId;

        /// <summary>
        /// If there was a conflict at ConflictCheckSpan during rename, then the next phase in rename uses
        /// ComplexifiedTargetSpan span to be expanded to resolve the conflict.
        /// </summary>
        public readonly TextSpan ComplexifiedTargetSpan;

        public RelatedLocation(TextSpan conflictCheckSpan, DocumentId documentId, RelatedLocationType type, bool isReference = false, TextSpan complexifiedTargetSpan = default)
        {
            this.ConflictCheckSpan = conflictCheckSpan;
            this.Type = type;
            this.IsReference = isReference;
            this.DocumentId = documentId;
            this.ComplexifiedTargetSpan = complexifiedTargetSpan;
        }

        public RelatedLocation WithType(RelatedLocationType type)
            => new RelatedLocation(ConflictCheckSpan, DocumentId, type, IsReference, ComplexifiedTargetSpan);

        public override bool Equals(object obj)
            => obj is RelatedLocation location && Equals(location);

        public bool Equals(RelatedLocation other)
        {
            return ConflictCheckSpan.Equals(other.ConflictCheckSpan) &&
                   Type == other.Type &&
                   IsReference == other.IsReference &&
                   EqualityComparer<DocumentId>.Default.Equals(DocumentId, other.DocumentId) &&
                   ComplexifiedTargetSpan.Equals(other.ComplexifiedTargetSpan);
        }

        public override int GetHashCode()
        {
            var hashCode = 928418920;
            hashCode = hashCode * -1521134295 + ConflictCheckSpan.GetHashCode();
            hashCode = hashCode * -1521134295 + Type.GetHashCode();
            hashCode = hashCode * -1521134295 + IsReference.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<DocumentId>.Default.GetHashCode(DocumentId);
            hashCode = hashCode * -1521134295 + ComplexifiedTargetSpan.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(RelatedLocation left, RelatedLocation right)
            => left.Equals(right);

        public static bool operator !=(RelatedLocation left, RelatedLocation right)
            => !(left == right);
    }
}
