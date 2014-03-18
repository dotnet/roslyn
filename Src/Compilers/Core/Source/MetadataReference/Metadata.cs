// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents immutable assembly or module CLI metadata.
    /// </summary>
    public abstract class Metadata : IDisposable
    {
        internal Metadata()
        {
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

        // does this instance own the PE image?
        internal abstract bool IsImageOwner { get; }
    }
}
