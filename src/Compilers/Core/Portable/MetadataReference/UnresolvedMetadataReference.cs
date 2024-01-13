// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a metadata reference that can't be or is not yet resolved.
    /// </summary>
    /// <remarks>
    /// For error reporting only, can't be used to reference a metadata file.
    /// </remarks>
    public sealed class UnresolvedMetadataReference : MetadataReference
    {
        public string Reference { get; }

        internal UnresolvedMetadataReference(string reference, MetadataReferenceProperties properties)
            : base(properties)
        {
            this.Reference = reference;
        }

        public override string Display
        {
            get
            {
                return CodeAnalysisResources.Unresolved + Reference;
            }
        }

        internal override bool IsUnresolved
        {
            get { return true; }
        }

        internal override MetadataReference WithPropertiesImplReturningMetadataReference(MetadataReferenceProperties properties)
        {
            return new UnresolvedMetadataReference(this.Reference, properties);
        }
    }
}
