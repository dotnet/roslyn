﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    /// <summary>
    /// Information about a reference to a symbol.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    public readonly struct ReferenceLocation : IComparable<ReferenceLocation>, IEquatable<ReferenceLocation>
    {
        /// <summary>
        /// The document that the reference was found in.
        /// </summary>
        public Document Document { get; }

        /// <summary>
        /// If the symbol was bound through an alias, then this is the alias that was used.
        /// </summary>
        public IAliasSymbol Alias { get; }

        /// <summary>
        /// The actual source location for a given symbol.
        /// </summary>
        public Location Location { get; }

        /// <summary>
        /// Indicates if this is an implicit reference to the definition.  i.e. the definition wasn't
        /// explicitly stated in the source code at this position, but it was still referenced. For
        /// example, this can happen with special methods like GetEnumerator that are used
        /// implicitly by a 'for each' statement.
        /// </summary>
        public bool IsImplicit { get; }

        /// <summary>
        /// Indicates if this is a location where the reference is written to.
        /// </summary>
        internal bool IsWrittenTo => SymbolUsageInfo.IsWrittenTo();

        /// <summary>
        /// Symbol usage info for this reference.
        /// </summary>
        internal SymbolUsageInfo SymbolUsageInfo { get; }

        /// <summary>
        /// Additional properties for this reference
        /// </summary>
        internal ImmutableDictionary<string, string> AdditionalProperties { get; }

        public CandidateReason CandidateReason { get; }

        internal ReferenceLocation(Document document, IAliasSymbol alias, Location location, bool isImplicit, SymbolUsageInfo symbolUsageInfo, ImmutableDictionary<string, string> additionalProperties, CandidateReason candidateReason)
            : this()
        {
            this.Document = document;
            this.Alias = alias;
            this.Location = location;
            this.IsImplicit = isImplicit;
            this.SymbolUsageInfo = symbolUsageInfo;
            this.AdditionalProperties = additionalProperties ?? ImmutableDictionary<string, string>.Empty;
            this.CandidateReason = candidateReason;
        }

        /// <summary>
        /// Indicates if this was not an exact reference to a location, but was instead a possible
        /// location that was found through error tolerance.  For example, a call to a method like
        /// "Goo()" could show up as an error tolerance location to a method "Goo(int i)" if no
        /// actual "Goo()" method existed.
        /// </summary>
        public bool IsCandidateLocation => this.CandidateReason != CandidateReason.None;

        public static bool operator ==(ReferenceLocation left, ReferenceLocation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ReferenceLocation left, ReferenceLocation right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is ReferenceLocation &&
                Equals((ReferenceLocation)obj);
        }

        public bool Equals(ReferenceLocation other)
        {
            return
                EqualityComparer<IAliasSymbol>.Default.Equals(this.Alias, other.Alias) &&
                EqualityComparer<Location>.Default.Equals(this.Location, other.Location) &&
                EqualityComparer<DocumentId>.Default.Equals(this.Document.Id, other.Document.Id) &&
                this.CandidateReason == other.CandidateReason &&
                this.IsImplicit == other.IsImplicit;
        }

        public override int GetHashCode()
        {
            return
                Hash.Combine(this.IsImplicit,
                Hash.Combine((int)this.CandidateReason,
                Hash.Combine(this.Alias,
                Hash.Combine(this.Location, this.Document.Id.GetHashCode()))));
        }

        public int CompareTo(ReferenceLocation other)
        {
            int compare;

            var thisPath = this.Location.SourceTree.FilePath;
            var otherPath = other.Location.SourceTree.FilePath;

            if ((compare = StringComparer.OrdinalIgnoreCase.Compare(thisPath, otherPath)) != 0 ||
                (compare = this.Location.SourceSpan.CompareTo(other.Location.SourceSpan)) != 0)
            {
                return compare;
            }

            return 0;
        }

        private string GetDebuggerDisplay()
        {
            return string.Format("{0}: {1}", this.Document.Name, this.Location);
        }
    }
}
