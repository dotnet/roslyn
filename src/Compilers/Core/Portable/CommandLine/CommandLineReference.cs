// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Describes a command line metadata reference (assembly or netmodule) specification.
    /// </summary>
    public struct CommandLineReference : IEquatable<CommandLineReference>
    {
        internal CommandLineReference(string reference, MetadataReferenceProperties properties, bool isDefaultCoreLibReference = false)
        {
            Debug.Assert(!string.IsNullOrEmpty(reference));
            Reference = reference;
            Properties = properties;
            IsDefaultCoreLibReference = isDefaultCoreLibReference;
        }

        /// <summary>
        /// Metadata file path or an assembly display name.
        /// </summary>
        public string Reference { get; }

        /// <summary>
        /// Metadata reference properties.
        /// </summary>
        public MetadataReferenceProperties Properties { get; }

        /// <summary>
        /// Flag indicating if this is an implicitly added core libary reference.
        /// </summary>
        internal bool IsDefaultCoreLibReference { get; }

        public override bool Equals(object obj)
        {
            return obj is CommandLineReference && base.Equals((CommandLineReference)obj);
        }

        public bool Equals(CommandLineReference other)
        {
            return Reference == other.Reference
                && Properties.Equals(other.Properties);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Reference, Properties.GetHashCode());
        }
    }
}
