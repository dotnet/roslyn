// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// An Id that can be used to identify a metadata instance.  If two metadata instances 
    /// have the same id then they are guaranteed to have the same content.  If two metadata
    /// instances have different ids, then the contents may or may not be the same.  As such,
    /// the id is useful as a key in a cache when a client wants to share data for a metadata
    /// reference as long as it has not changed.
    /// </summary>
    public sealed class MetadataId
    {
        private MetadataId()
        {
        }

        internal static MetadataId CreateNewId() => new MetadataId();
    }

    /// <summary>
    /// Represents immutable assembly or module CLI metadata.
    /// </summary>
    public abstract class Metadata : IDisposable
    {
        internal readonly bool IsImageOwner;

        /// <summary>
        /// The id for this metadata instance.  If two metadata instances have the same id, then 
        /// they have the same content.  If they have different ids they may or may not have the
        /// same content.
        /// </summary>
        public MetadataId Id { get; }

        internal Metadata(bool isImageOwner, MetadataId id)
        {
            this.IsImageOwner = isImageOwner;
            this.Id = id;
        }

        /// <summary>
        /// Retrieves the <see cref="MetadataImageKind"/> for this instance.
        /// </summary>
        public abstract MetadataImageKind Kind { get; }

        /// <summary>
        /// Releases any resources associated with this instance.
        /// </summary>
        public abstract void Dispose();

        protected abstract Metadata CommonCopy();

        /// <summary>
        /// Creates a copy of this object.
        /// </summary>
        public Metadata Copy()
        {
            return CommonCopy();
        }
    }
}
