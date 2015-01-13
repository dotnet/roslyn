// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly string reference;
        private readonly MetadataReferenceProperties properties;

        internal CommandLineReference(string reference, MetadataReferenceProperties properties)
        {
            Debug.Assert(!string.IsNullOrEmpty(reference));
            this.reference = reference;
            this.properties = properties;
        }

        /// <summary>
        /// Metadata file path or an assembly display name.
        /// </summary>
        public string Reference
        {
            get { return reference; }
        }

        /// <summary>
        /// Metadata reference properties.
        /// </summary>
        public MetadataReferenceProperties Properties
        {
            get { return properties; }
        }

        public override bool Equals(object obj)
        {
            return obj is CommandLineReference && base.Equals((CommandLineReference)obj);
        }

        public bool Equals(CommandLineReference other)
        {
            return this.reference == other.reference
                && this.properties.Equals(other.properties);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(reference, properties.GetHashCode());
        }
    }
}
