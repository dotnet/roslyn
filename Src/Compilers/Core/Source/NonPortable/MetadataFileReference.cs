// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents metadata stored in a file.
    /// </summary>
    /// <remarks>
    /// Metadata image is read from the file, owned by the reference, and doesn't change 
    /// since the reference is accessed by the compiler until the reference object is garbage collected.
    /// During this time the file is open and its content is read-only.
    /// 
    /// If you need to manage the lifetime of the metadata (and the file stream) explicitly use <see cref="MetadataImageReference"/> or 
    /// implement a custom subclass of <see cref="PortableExecutableReference"/>.
    /// </remarks>
    public sealed class MetadataFileReference : PortableExecutableReference
    {
        private Metadata lazyMetadata;

        public MetadataFileReference(string fullPath, MetadataImageKind kind = MetadataImageKind.Assembly, ImmutableArray<string> aliases = default(ImmutableArray<string>), bool embedInteropTypes = false, DocumentationProvider documentation = null)
            : this(fullPath, new MetadataReferenceProperties(kind, aliases, embedInteropTypes), documentation)
        {
        }

        public MetadataFileReference(string fullPath, MetadataReferenceProperties properties, DocumentationProvider documentation = null)
            : base(properties, fullPath, initialDocumentation: documentation ?? DocumentationProvider.Default)
        {
            if (fullPath == null)
            {
                throw new ArgumentNullException("fullPath");
            }
        }

        protected override DocumentationProvider CreateDocumentationProvider()
        {
            // TODO (tomat): Implement a reasonable provider for xml files.
            // 
            // Use the MetadataCache to cache the provider in the same way we cache metadata. 
            // We have to make sure that we give the same provider instance for the same file since we are 
            // reusing a AssemblyMetadata object that stores cached symbols that depend on the documentation provider.
            //
            // private class XmlFileDocumentationProvider : DocumentationProvider, IDisposable
            // {
            //     public XmlFileDocumentationProvider(string fullPath)
            //     {
            //         // open file for reading 
            //     }
            // 
            //     protected internal override DocumentationComment GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default(CancellationToken))
            //     {
            //         // seek and read the requested element
            //         return DocumentationComment.Empty;
            //     }
            // 
            //     public void Dispose()
            //     {
            //         // close file
            //     }
            // }
            throw ExceptionUtilities.Unreachable;
        }

        public override string Display
        {
            get
            {
                return FullPath;
            }
        }

        /// <exception cref="IOException"/>
        protected override Metadata GetMetadataImpl()
        {
            if (lazyMetadata == null)
            {
                Interlocked.CompareExchange(ref lazyMetadata, MetadataCache.GetOrCreateFromFile(FullPath, this.Properties.Kind), null);
            }

            return lazyMetadata;
        }
    }
}
