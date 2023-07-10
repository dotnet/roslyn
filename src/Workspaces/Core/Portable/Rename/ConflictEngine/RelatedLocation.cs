// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    /// <summary>
    /// Gives information about an identifier span that was affected by Rename (Reference or Non reference)
    /// </summary>
    /// <param name="ConflictCheckSpan">
    /// The Span of the original identifier if it was in source, otherwise the span to check for implicit
    /// references.
    /// </param>
    /// <param name="ComplexifiedTargetSpan">
    /// If there was a conflict at ConflictCheckSpan during rename, then the next phase in rename uses
    /// ComplexifiedTargetSpan span to be expanded to resolve the conflict.
    /// </param>
    [DataContract]
    internal readonly record struct RelatedLocation(
        [field: DataMember(Order = 0)] TextSpan ConflictCheckSpan,
        [field: DataMember(Order = 1)] DocumentId DocumentId,
        [field: DataMember(Order = 2)] RelatedLocationType Type,
        [field: DataMember(Order = 3)] bool IsReference = false,
        [field: DataMember(Order = 4)] TextSpan ComplexifiedTargetSpan = default)
    {
        public RelatedLocation WithType(RelatedLocationType type)
            => new(ConflictCheckSpan, DocumentId, type, IsReference, ComplexifiedTargetSpan);

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
    }
}
