// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
        private bool _isDisposed;

        private readonly PEModule _module;

        private ModuleMetadata(PEReader peReader)
            : base(isImageOwner: true, id: MetadataId.CreateNewId())
        {
            _module = new PEModule(this, peReader: peReader, metadataOpt: IntPtr.Zero, metadataSizeOpt: 0);
        }

        private ModuleMetadata(IntPtr metadata, int size, bool includeEmbeddedInteropTypes)
            : base(isImageOwner: true, id: MetadataId.CreateNewId())
        {
            _module = new PEModule(this, peReader: null, metadataOpt: metadata, metadataSizeOpt: size, includeEmbeddedInteropTypes: includeEmbeddedInteropTypes);
        }

        // creates a copy
        private ModuleMetadata(ModuleMetadata metadata)
            : base(isImageOwner: false, id: metadata.Id)
        {
            _module = metadata.Module;
        }

        /// <summary>
        /// Create metadata module from a raw memory pointer to metadata directory of a PE image or .cormeta section of an object file.
        /// Only manifest modules are currently supported.
        /// </summary>
        /// <param name="metadata">Pointer to the start of metadata block.</param>
        /// <param name="size">The size of the metadata block.</param>
        /// <exception cref="ArgumentNullException"><paramref name="metadata"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is not positive.</exception>
        public static ModuleMetadata CreateFromMetadata(IntPtr metadata, int size)
        {
            if (metadata == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(CodeAnalysisResources.SizeHasToBePositive, nameof(size));
            }

            return new ModuleMetadata(metadata, size, includeEmbeddedInteropTypes: false);
        }

        internal static ModuleMetadata CreateFromMetadata(IntPtr metadata, int size, bool includeEmbeddedInteropTypes)
        {
            Debug.Assert(metadata != IntPtr.Zero);
            Debug.Assert(size > 0);
            return new ModuleMetadata(metadata, size, includeEmbeddedInteropTypes);
        }

        /// <summary>
        /// Create metadata module from a raw memory pointer to a PE image or an object file.
        /// </summary>
        /// <param name="peImage">Pointer to the DOS header ("MZ") of a portable executable image.</param>
        /// <param name="size">The size of the image pointed to by <paramref name="peImage"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="peImage"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is not positive.</exception>
        public static unsafe ModuleMetadata CreateFromImage(IntPtr peImage, int size)
        {
            if (peImage == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(peImage));
            }

            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(CodeAnalysisResources.SizeHasToBePositive, nameof(size));
            }

            return new ModuleMetadata(new PEReader((byte*)peImage, size));
        }

        /// <summary>
        /// Create metadata module from a sequence of bytes.
        /// </summary>
        /// <param name="peImage">The portable executable image beginning with the DOS header ("MZ").</param>
        /// <exception cref="ArgumentNullException"><paramref name="peImage"/> is null.</exception>
        public static ModuleMetadata CreateFromImage(IEnumerable<byte> peImage)
        {
            if (peImage == null)
            {
                throw new ArgumentNullException(nameof(peImage));
            }

            return CreateFromImage(ImmutableArray.CreateRange(peImage));
        }

        /// <summary>
        /// Create metadata module from a byte array.
        /// </summary>
        /// <param name="peImage">Portable executable image beginning with the DOS header ("MZ").</param>
        /// <exception cref="ArgumentNullException"><paramref name="peImage"/> is null.</exception>
        public static ModuleMetadata CreateFromImage(ImmutableArray<byte> peImage)
        {
            if (peImage.IsDefault)
            {
                throw new ArgumentNullException(nameof(peImage));
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
        /// <exception cref="ArgumentNullException"><paramref name="peStream"/> is null.</exception>
        /// <exception cref="ArgumentException">The stream doesn't support seek operations.</exception>
        public static ModuleMetadata CreateFromStream(Stream peStream, bool leaveOpen = false)
        {
            return CreateFromStream(peStream, leaveOpen ? PEStreamOptions.LeaveOpen : PEStreamOptions.Default);
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
        /// <exception cref="ArgumentNullException"><paramref name="peStream"/> is null.</exception>
        /// <exception cref="ArgumentException">The stream doesn't support read and seek operations.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> has an invalid value.</exception>
        /// <exception cref="BadImageFormatException">
        /// <see cref="PEStreamOptions.PrefetchMetadata"/> or <see cref="PEStreamOptions.PrefetchEntireImage"/> is specified and the PE headers of the image are invalid.
        /// </exception>
        /// <exception cref="IOException">
        /// <see cref="PEStreamOptions.PrefetchMetadata"/> or <see cref="PEStreamOptions.PrefetchEntireImage"/> is specified and an error occurs while reading the stream.
        /// </exception>
        public static ModuleMetadata CreateFromStream(Stream peStream, PEStreamOptions options)
        {
            if (peStream == null)
            {
                throw new ArgumentNullException(nameof(peStream));
            }

            if (!peStream.CanRead || !peStream.CanSeek)
            {
                throw new ArgumentException(CodeAnalysisResources.StreamMustSupportReadAndSeek, nameof(peStream));
            }

            // Workaround of issue https://github.com/dotnet/corefx/issues/1815: 
            if (peStream.Length == 0 && (options & PEStreamOptions.PrefetchEntireImage) != 0 && (options & PEStreamOptions.PrefetchMetadata) != 0)
            {
                // throws BadImageFormatException:
                new PEHeaders(peStream);
            }

            // ownership of the stream is passed on PEReader:
            return new ModuleMetadata(new PEReader(peStream, options));
        }

        /// <summary>
        /// Creates metadata module from a file containing a portable executable image.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <remarks>
        /// The file might remain mapped (and read-locked) until this object is disposed.
        /// The memory map is only created for large files. Small files are read into memory.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is invalid.</exception>
        /// <exception cref="IOException">Error opening file <paramref name="path"/>. See <see cref="Exception.InnerException"/> for details.</exception>
        /// <exception cref="FileNotFoundException">File <paramref name="path"/> not found.</exception>
        /// <exception cref="NotSupportedException">Reading from a file path is not supported by the platform.</exception>
        public static ModuleMetadata CreateFromFile(string path)
        {
            return CreateFromStream(FileUtilities.OpenFileStream(path));
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
            _isDisposed = true;

            if (IsImageOwner)
            {
                _module.Dispose();
            }
        }

        internal bool IsDisposed
        {
            get { return _isDisposed || _module.IsDisposed; }
        }

        internal PEModule Module
        {
            get
            {
                if (IsDisposed)
                {
                    throw new ObjectDisposedException(nameof(ModuleMetadata));
                }

                return _module;
            }
        }

        /// <summary>
        /// Name of the module.
        /// </summary>
        /// <exception cref="BadImageFormatException">Invalid metadata.</exception>
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

        /// <summary>
        /// Returns the metadata reader.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Module has been disposed.</exception>
        /// <exception cref="BadImageFormatException">When an invalid module name is encountered.</exception>
        internal MetadataReader MetadataReader
        {
            get { return Module.MetadataReader; }
        }

        /// <summary>
        /// Creates a reference to the module metadata.
        /// </summary>
        /// <param name="documentation">Provider of XML documentation comments for the metadata symbols contained in the module.</param>
        /// <param name="filePath">Path describing the location of the metadata, or null if the metadata have no location.</param>
        /// <param name="display">Display string used in error messages to identity the reference.</param>
        /// <returns>A reference to the module metadata.</returns>
        public PortableExecutableReference GetReference(DocumentationProvider documentation = null, string filePath = null, string display = null)
        {
            return new MetadataImageReference(this, MetadataReferenceProperties.Module, documentation, filePath, display);
        }
    }
}
