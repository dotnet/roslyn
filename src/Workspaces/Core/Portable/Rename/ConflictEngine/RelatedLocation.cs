// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    /// <summary>
    /// Gives information about an identifier span that was affected by Rename (Reference or Non reference)
    /// </summary>
    [DataContract]
    internal readonly struct RelatedLocation(TextSpan conflictCheckSpan, DocumentId documentId, RelatedLocationType type, bool isReference = false, TextSpan complexifiedTargetSpan = default) : IEquatable<RelatedLocation>
    {
        /// <summary>
        /// The Span of the original identifier if it was in source, otherwise the span to check for implicit
        /// references.
        /// </summary>
        [DataMember(Order = 0)]
        public readonly TextSpan ConflictCheckSpan = conflictCheckSpan;

        [DataMember(Order = 1)]
        public readonly DocumentId DocumentId = documentId;

        [DataMember(Order = 2)]
        public readonly RelatedLocationType Type = type;

        [DataMember(Order = 3)]
        public readonly bool IsReference = isReference;

        /// <summary>
        /// If there was a conflict at ConflictCheckSpan during rename, then the next phase in rename uses
        /// ComplexifiedTargetSpan span to be expanded to resolve the conflict.
        /// </summary>
        [DataMember(Order = 4)]
        public readonly TextSpan ComplexifiedTargetSpan = complexifiedTargetSpan;

        public RelatedLocation WithType(RelatedLocationType type)
            => new(ConflictCheckSpan, DocumentId, type, IsReference, ComplexifiedTargetSpan);

        public override bool Equals(object? obj)
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
            hashCode = hashCode * -1521134295 + ((int)Type).GetHashCode();
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
