// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents metadata image reference.
    /// </summary>
    /// <remarks>
    /// Represents a logical location of the image, not the content of the image. 
    /// The content might change in time. A snapshot is taken when the compiler queries the reference for its metadata.
    /// </remarks>
    public abstract class MetadataReference
    {
        public MetadataReferenceProperties Properties { get; private set; }

        internal MetadataReference(MetadataReferenceProperties properties)
        {
            this.Properties = properties;
        }

        /// <summary>
        /// Path or name used in error messages to identity the reference.
        /// </summary>
        public virtual string Display { get { return null; } }

        /// <summary>
        /// Returns true if this reference is an unresolved reference.
        /// </summary>
        internal virtual bool IsUnresolved
        {
            get { return false; }
        }
    }
}