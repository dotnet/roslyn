// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Represents a span of an active statement tracked by the client editor.
    /// </summary>
    [DataContract]
    internal readonly struct ActiveStatementSpan : IEquatable<ActiveStatementSpan>
    {
        /// <summary>
        /// The corresponding <see cref="ActiveStatement.Ordinal"/>.
        /// </summary>
        [DataMember(Order = 0)]
        public readonly int Ordinal;

        /// <summary>
        /// Line span in the mapped document.
        /// </summary>
        [DataMember(Order = 1)]
        public readonly LinePositionSpan LineSpan;

        /// <summary>
        /// Flags.
        /// </summary>
        [DataMember(Order = 2)]
        public readonly ActiveStatementFlags Flags;

        /// <summary>
        /// The id of the unmapped document where the source of the active statement is and from where the statement might be mapped to <see cref="LineSpan"/> via <c>#line</c> directive.
        /// Null if unknown (not determined yet).
        /// </summary>
        [DataMember(Order = 3)]
        public readonly DocumentId? UnmappedDocumentId;

        public ActiveStatementSpan(int ordinal, LinePositionSpan lineSpan, ActiveStatementFlags flags, DocumentId? unmappedDocumentId)
        {
            Debug.Assert(ordinal >= 0);

            Ordinal = ordinal;
            LineSpan = lineSpan;
            Flags = flags;
            UnmappedDocumentId = unmappedDocumentId;
        }

        public override bool Equals(object? obj)
            => obj is ActiveStatementSpan other && Equals(other);

        public bool Equals(ActiveStatementSpan other)
            => Ordinal.Equals(other.Ordinal) &&
               LineSpan.Equals(other.LineSpan) &&
               Flags == other.Flags &&
               UnmappedDocumentId == other.UnmappedDocumentId;

        public override int GetHashCode()
            => Hash.Combine(Ordinal, Hash.Combine(LineSpan.GetHashCode(), Hash.Combine(UnmappedDocumentId, (int)Flags)));
    }
}
