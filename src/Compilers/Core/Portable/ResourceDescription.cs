// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Representation of a resource whose contents are to be embedded in the output assembly.
    /// </summary>
    public sealed class ResourceDescription : Cci.IFileReference
    {
        internal readonly string ResourceName;
        internal readonly string FileName; // null if embedded
        internal readonly bool IsPublic;
        internal readonly Func<Stream> DataProvider;
        private readonly CryptographicHashProvider _hashes;

        /// <summary>
        /// Creates a representation of a resource whose contents are to be embedded in the output assembly.
        /// </summary>
        /// <param name="resourceName">Resource name.</param>
        /// <param name="dataProvider">The callers will dispose the result after use.
        /// This allows the resources to be opened and read one at a time.
        /// </param>
        /// <param name="isPublic">True if the resource is public.</param>
        /// <remarks>
        /// Returns a stream of the data to embed.
        /// </remarks> 
        public ResourceDescription(string resourceName, Func<Stream> dataProvider, bool isPublic)
            : this(resourceName, null, dataProvider, isPublic, isEmbedded: true, checkArgs: true)
        {
        }

        /// <summary>
        /// Creates a representation of a resource whose file name will be recorded in the assembly.
        /// </summary>
        /// <param name="resourceName">Resource name.</param>
        /// <param name="fileName">File name with an extension to be stored in metadata.</param>
        /// <param name="dataProvider">The callers will dispose the result after use.
        /// This allows the resources to be opened and read one at a time.
        /// </param>
        /// <param name="isPublic">True if the resource is public.</param>
        /// <remarks>
        /// Function returning a stream of the resource content (used to calculate hash).
        /// </remarks>
        public ResourceDescription(string resourceName, string fileName, Func<Stream> dataProvider, bool isPublic)
            : this(resourceName, fileName, dataProvider, isPublic, isEmbedded: false, checkArgs: true)
        {
        }

        internal ResourceDescription(string resourceName, string fileName, Func<Stream> dataProvider, bool isPublic, bool isEmbedded, bool checkArgs)
        {
            if (checkArgs)
            {
                if (dataProvider == null)
                {
                    throw new ArgumentNullException(nameof(dataProvider));
                }

                if (resourceName == null)
                {
                    throw new ArgumentNullException(nameof(resourceName));
                }

                if (!MetadataHelpers.IsValidMetadataIdentifier(resourceName))
                {
                    throw new ArgumentException(CodeAnalysisResources.EmptyOrInvalidResourceName, nameof(resourceName));
                }

                if (!isEmbedded)
                {
                    if (fileName == null)
                    {
                        throw new ArgumentNullException(nameof(fileName));
                    }

                    if (!MetadataHelpers.IsValidMetadataFileName(fileName))
                    {
                        throw new ArgumentException(CodeAnalysisResources.EmptyOrInvalidFileName, nameof(fileName));
                    }
                }
            }

            this.ResourceName = resourceName;
            this.DataProvider = dataProvider;
            this.FileName = isEmbedded ? null : fileName;
            this.IsPublic = isPublic;
            _hashes = new ResourceHashProvider(this);
        }

        private sealed class ResourceHashProvider : CryptographicHashProvider
        {
            private readonly ResourceDescription _resource;

            public ResourceHashProvider(ResourceDescription resource)
            {
                Debug.Assert(resource != null);
                _resource = resource;
            }

            internal override ImmutableArray<byte> ComputeHash(HashAlgorithm algorithm)
            {
                try
                {
                    using (var stream = _resource.DataProvider())
                    {
                        if (stream == null)
                        {
                            throw new InvalidOperationException(CodeAnalysisResources.ResourceDataProviderShouldReturnNonNullStream);
                        }

                        return ImmutableArray.CreateRange(algorithm.ComputeHash(stream));
                    }
                }
                catch (Exception ex)
                {
                    throw new ResourceException(_resource.FileName, ex);
                }
            }
        }

        internal bool IsEmbedded
        {
            get { return FileName == null; }
        }

        internal Cci.ManagedResource ToManagedResource(CommonPEModuleBuilder moduleBeingBuilt)
        {
            return new Cci.ManagedResource(ResourceName, IsPublic, IsEmbedded ? DataProvider : null, IsEmbedded ? null : this, offset: 0);
        }

        ImmutableArray<byte> Cci.IFileReference.GetHashValue(AssemblyHashAlgorithm algorithmId)
        {
            return _hashes.GetHash(algorithmId);
        }

        string Cci.IFileReference.FileName
        {
            get { return FileName; }
        }

        bool Cci.IFileReference.HasMetadata
        {
            get { return false; }
        }
    }
}
