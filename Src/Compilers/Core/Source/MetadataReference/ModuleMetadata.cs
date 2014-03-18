// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents an immutable snapshot of module CLI metadata.
    /// </summary>
    /// <remarks>This object may allocate significant resources or lock files depending upon how it is constructed.</remarks>
    public sealed partial class ModuleMetadata : Metadata
    {
        private bool isDisposed;

        private readonly bool isImageOwner;
        private readonly PEModule module;

        private ModuleMetadata(PEReader peReader)
        {
            this.module = new PEModule(peReader: peReader, metadataReader: null);
            this.isImageOwner = true;
        }

        private ModuleMetadata(MetadataReader metadataReader)
        {
            this.module = new PEModule(peReader: null, metadataReader: metadataReader);
            this.isImageOwner = true;
        }

        private ModuleMetadata(ModuleMetadata metadata)
        {
            this.module = metadata.Module;

            // This instance will not be the owner of the
            // resources backing the metadata. 

            this.isImageOwner = false;
        }

        /// <summary>
        /// Create metadata module from a raw memory pointer to metadata directory of a PE image or .cormeta section of an object file.
        /// Only manifest modules are currently supported.
        /// </summary>
        /// <param name="metadata">Pointer to the start of metadata block.</param>
        /// <param name="size">The size of the metadata block.</param>
        /// <exception cref="ArgumentNullException"><paramref name="metadata"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is not positive.</exception>
        /// <exception cref="BadImageFormatException"><paramref name="metadata"/> doesn't contain valid metadata.</exception>
        /// <exception cref="NotSupportedException"><paramref name="metadata"/> doesn't represent an assembly (manifest module).</exception>
        public static ModuleMetadata CreateFromMetadata(IntPtr metadata, int size)
        {
            if (metadata == IntPtr.Zero)
            {
                throw new ArgumentNullException("metadata");
            }

            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(CodeAnalysisResources.SizeHasToBePositive, "size");
            }

            var reader = new MetadataReader(metadata, size, MetadataReaderOptions.ApplyWindowsRuntimeProjections, stringInterner: StringTable.AddShared);
            return new ModuleMetadata(reader);
        }

        /// <summary>
        /// Create metadata module from a raw memory pointer to a PE image or an object file.
        /// </summary>
        /// <param name="peImage">Pointer to the DOS header ("MZ") of a portable executable image.</param>
        /// <param name="size">The size of the image pointed to by <paramref name="peImage"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="peImage"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is not positive.</exception>
        /// <exception cref="BadImageFormatException"><paramref name="peImage"/> is not valid portable executable image containing CLI metadata</exception>
        public static ModuleMetadata CreateFromImage(IntPtr peImage, int size)
        {
            if (peImage == IntPtr.Zero)
            {
                throw new ArgumentNullException("peImage");
            }

            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(CodeAnalysisResources.SizeHasToBePositive, "size");
            }

            return new ModuleMetadata(new PEReader(peImage, size));
        }

        /// <summary>
        /// Create metadata module from a sequence of bytes.
        /// </summary>
        /// <param name="peImage">The portable executable image beginning with the DOS header ("MZ").</param>
        /// <exception cref="ArgumentException"><paramref name="peImage"/> is null.</exception>
        /// <exception cref="BadImageFormatException"><paramref name="peImage"/> is not valid portable executable image containing CLI metadata</exception>
        public static ModuleMetadata CreateFromImage(IEnumerable<byte> peImage)
        {
            return CreateFromImage(peImage.AsImmutableOrNull());
        }

        /// <summary>
        /// Create metadata module from a byte array.
        /// </summary>
        /// <param name="peImage">Portable executable image beginning with the DOS header ("MZ").</param>
        /// <exception cref="ArgumentException"><paramref name="peImage"/> is null.</exception>
        /// <exception cref="BadImageFormatException"><paramref name="peImage"/> is not valid portable executable image containing CLI metadata</exception>
        public static ModuleMetadata CreateFromImage(ImmutableArray<byte> peImage)
        {
            if (peImage.IsDefault)
            {
                throw new ArgumentException("peImage");
            }

            return new ModuleMetadata(new PEReader(peImage));
        }

        /// <summary>
        /// Create metadata module from a stream.
        /// </summary>
        /// <param name="peStream">Stream containing portable executable image. Position zero should contain the first byte of the DOS header ("MZ").</param>
        /// <param name="leaveOpen">
        /// False to close the stream upon disposal of the metadata (the responsibility for disposal of the stream is transferred upon entry of the constructor
        /// unless the arguments given are invalid).
        /// </param>
        /// <exception cref="BadImageFormatException"><paramref name="peStream"/> is not valid portable executable image containing CLI metadata</exception>
        /// <exception cref="ArgumentException">The stream doesn't support seek operations.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="peStream"/> is null.</exception>
        public static ModuleMetadata CreateFromImageStream(Stream peStream, bool leaveOpen = false)
        {
            return CreateFromImageStream(peStream, leaveOpen ? PEStreamOptions.LeaveOpen : PEStreamOptions.Default);
        }

        /// <summary>
        /// Create metadata module from a stream.
        /// </summary>
        /// <param name="peStream">Stream containing portable executable image. Position zero should contain the first byte of the DOS header ("MZ").</param>
        /// <param name="options">
        /// Options specifying how sections of the PE image are read from the stream.
        /// Unless <see cref="PEStreamOptions.LeaveOpen"/> is specified, the responsibility for disposal of the stream is transferred upon entry of the constructor
        /// unless the arguments given are invalid.
        /// </param>
        /// <exception cref="BadImageFormatException"><paramref name="peStream"/> is not valid portable executable image containing CLI metadata</exception>
        /// <exception cref="ArgumentException">The stream doesn't support read and seek operations.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="peStream"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> has an invalid value.</exception>
        public static ModuleMetadata CreateFromImageStream(Stream peStream, PEStreamOptions options)
        {
            if (peStream == null)
            {
                throw new ArgumentNullException("peStream");
            }

            if (!peStream.CanRead || !peStream.CanSeek)
            {
                throw new ArgumentException(CodeAnalysisResources.StreamMustSupportReadAndSeek, "peImage");
            }

            // ownership of the stream is passed on PEReader:
            return new ModuleMetadata(new PEReader(peStream, options));
        }

        /// <summary>
        /// Creates a shallow copy of this object.
        /// </summary>
        /// <remarks>
        /// The resulting copy shares the metadata image and metadata information read from it with the original.
        /// It doesn't own the underlying metadata image and is not responsible for its disposal.
        /// 
        /// This is used, for example, when a metadata cache needs to return the cached metadata to its users
        /// while keeping the ownership of the cached metadata object.
        /// </remarks>
        internal new ModuleMetadata Copy()
        {
            return new ModuleMetadata(this);
        }

        protected override Metadata CommonCopy()
        {
            return Copy();
        }

        /// <summary>
        /// Frees memory and handles allocated for the module.
        /// </summary>
        public override void Dispose()
        {
            isDisposed = true;

            if (IsImageOwner)
            {
                module.Dispose();
            }
        }

        internal bool IsDisposed
        {
            get { return isDisposed || module.IsDisposed; }
        }

        internal PEModule Module
        {
            get
            {
                if (IsDisposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                return module;
            }
        }

        /// <summary>
        /// Name of the module.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Module has been disposed.</exception>
        public string Name
        {
            get { return Module.Name; }
        }

        /// <summary>
        /// Version of the module content.
        /// </summary>
        /// <exception cref="BadImageFormatException">Invalid metadata.</exception>
        /// <exception cref="ObjectDisposedException">Module has been disposed.</exception>
        public Guid GetModuleVersionId()
        {
            return Module.GetModuleVersionIdOrThrow();
        }

        /// <summary>
        /// Returns the <see cref="MetadataImageKind"/> for this instance.
        /// </summary>
        public override MetadataImageKind Kind
        {
            get { return MetadataImageKind.Module; }
        }

        /// <summary>
        /// Returns the file names of linked managed modules.
        /// </summary>
        /// <exception cref="BadImageFormatException">When an invalid module name is encountered.</exception>
        /// <exception cref="ObjectDisposedException">Module has been disposed.</exception>
        public ImmutableArray<string> GetModuleNames()
        {
            return Module.GetMetadataModuleNamesOrThrow();
        }

        internal MetadataReader MetadataReader
        {
            get { return Module.MetadataReader; }
        }

        internal override bool IsImageOwner
        {
            get { return isImageOwner; }
        }
    }
}
