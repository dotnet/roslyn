// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Describes a command line metadata reference (assembly or netmodule) specification.
    /// </summary>
    [DebuggerDisplay("{Reference,nq}")]
    public readonly struct CommandLineReference : IEquatable<CommandLineReference>
    {
        private readonly string _reference;
        private readonly MetadataReferenceProperties _properties;

        public CommandLineReference(string reference, MetadataReferenceProperties properties)
        {
            Debug.Assert(!string.IsNullOrEmpty(reference));
            _reference = reference;
            _properties = properties;
        }

        /// <summary>
        /// Metadata file path or an assembly display name.
        /// </summary>
        public string Reference
        {
            get { return _reference; }
        }

        /// <summary>
        /// Metadata reference properties.
        /// </summary>
        public MetadataReferenceProperties Properties
        {
            get { return _properties; }
        }

        public override bool Equals(object? obj)
        {
            return obj is CommandLineReference && base.Equals((CommandLineReference)obj);
        }

        public bool Equals(CommandLineReference other)
        {
            return _reference == other._reference
                && _properties.Equals(other._properties);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_reference, _properties.GetHashCode());
        }
    }
}
