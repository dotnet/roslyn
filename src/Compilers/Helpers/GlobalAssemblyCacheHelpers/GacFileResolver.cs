// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// Extends MetadataFileReferenceResolver to enable resolution of assembly
    /// simple names in the GAC.
    /// </summary>
    internal sealed class GacFileResolver : MetadataReferenceResolver
    {
        internal static readonly ImmutableArray<ProcessorArchitecture> DefaultArchitectures = GlobalAssemblyCache.CurrentArchitectures;

        private readonly ImmutableArray<ProcessorArchitecture> _architectures;
        private readonly CultureInfo _preferredCulture;
        private readonly MetadataFileReferenceProvider _provider;

        /// <summary>
        /// Constructs an instance of a <see cref="GacFileResolver"/>
        /// </summary>
        /// <param name="architectures">Supported architectures used to filter GAC assemblies.</param>
        /// <param name="preferredCulture">A culture to use when choosing the best assembly from 
        /// among the set filtered by <paramref name="architectures"/></param>
        /// <param name="provider">Metadata provider.</param>
        public GacFileResolver(
            ImmutableArray<ProcessorArchitecture> architectures,
            CultureInfo preferredCulture,
            MetadataFileReferenceProvider provider)
        {
            _architectures = architectures;
            _preferredCulture = preferredCulture;
            _provider = provider;
        }

        /// <summary>
        /// Architecture filter used when resolving assembly references.
        /// </summary>
        public ImmutableArray<ProcessorArchitecture> Architectures
        {
            get { return _architectures; }
        }

        /// <summary>
        /// CultureInfo used when resolving assembly references.
        /// </summary>
        public CultureInfo PreferredCulture
        {
            get { return _preferredCulture; }
        }

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
        {
            var path = ResolveReference(reference);
            var metadata = (path == null) ? null : _provider.GetReference(path, properties);
            return (metadata == null) ?
                ImmutableArray<PortableExecutableReference>.Empty :
                ImmutableArray.Create(metadata);
        }

        internal string ResolveReference(string reference)
        {
            if (PathUtilities.IsFilePath(reference))
            {
                return null;
            }

            string path;
            GlobalAssemblyCache.ResolvePartialName(reference, out path, _architectures, this.PreferredCulture);
            return (path != null && PortableShim.File.Exists(path)) ? path : null;
        }

        public override bool Equals(object obj)
        {
            var other = obj as GacFileResolver;
            return (other != null) &&
                _architectures.SequenceEqual(other._architectures) &&
                _preferredCulture == other._preferredCulture &&
                object.Equals(_provider, other._provider);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_preferredCulture, Hash.Combine(_provider, Hash.CombineValues(_architectures)));
        }
    }
}
