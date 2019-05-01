// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.Debugging;

namespace Microsoft.CodeAnalysis.Emit
{
    /// <summary>
    /// Reads compilation outputs such as output assembly and PDB.
    /// </summary>
    internal abstract class CompilationOutputs
    {
        /// <summary>
        /// String describing the assembly to be used in user facing error messages (e.g. file path).
        /// </summary>
        public abstract string AssemblyDisplayPath { get; }

        /// <summary>
        /// String describing the PDB to be used in user facing error messages (e.g. file path).
        /// </summary>
        public abstract string PdbDisplayPath { get; }

        /// <summary>
        /// Opens metadata section of the assembly file produced by the compiler.
        /// </summary>
        /// <param name="prefetch">
        /// True to prefetch all metadata from the assembly and close the underlying stream on return,
        /// otherwise keeps the underlying stream open until the returned <see cref="MetadataReaderProvider"/> is disposed.
        /// </param>
        /// <returns>
        /// Instance of <see cref="MetadataReaderProvider"/>, which owns the opened metadata and must be disposed once the caller is done reading the data, 
        /// or null if the assembly is not available.
        /// </returns>
        /// <exception cref="BadImageFormatException">Invalid format of the assembly data.</exception>
        /// <exception cref="InvalidOperationException">The stream returned by <see cref="OpenAssemblyStreamChecked"/> does not support read and seek operations.</exception>
        /// <exception cref="Exception">Error while reading assembly data.</exception>
        public virtual MetadataReaderProvider OpenAssemblyMetadata(bool prefetch)
        {
            var peStream = OpenAssemblyStreamChecked();
            if (peStream != null)
            {
                PEHeaders peHeaders;
                using (var peReader = new PEReader(peStream, PEStreamOptions.LeaveOpen))
                {
                    peHeaders = peReader.PEHeaders;
                }

                peStream.Position = peHeaders.MetadataStartOffset;
                return MetadataReaderProvider.FromMetadataStream(
                    peStream,
                    prefetch ? MetadataStreamOptions.PrefetchMetadata : MetadataStreamOptions.Default,
                    size: peHeaders.MetadataSize);
            }

            return null;
        }

        /// <summary>
        /// Opens PDB produced by the compiler.
        /// The caller must dispose the returned <see cref="DebugInformationReaderProvider"/>.
        /// </summary>
        /// <returns>
        /// Instance of <see cref="DebugInformationReaderProvider"/>, which owns the opened PDB and must be disposed once the caller is done reading the data,
        /// or null if PDB is not available.
        /// </returns>
        /// <exception cref="BadImageFormatException">Invalid format of the PDB or assembly data.</exception>
        /// <exception cref="InvalidOperationException">The stream returned by <see cref="OpenPdbStreamChecked"/> or <see cref="OpenAssemblyStreamChecked"/> does not support read and seek operations.</exception>
        /// <exception cref="Exception">Error while reading assembly data.</exception>
        /// <remarks>
        /// If a separate PDB stream is not available (<see cref="OpenPdbStreamChecked"/> returns null) opens the PDB embedded in the assembly, if present.
        /// </remarks>
        public virtual DebugInformationReaderProvider OpenPdb()
        {
            var stream = OpenPdbStreamChecked();
            if (stream != null)
            {
                return DebugInformationReaderProvider.CreateFromStream(stream);
            }

            // check for embedded PDB
            using (var peReader = new PEReader(OpenAssemblyStreamChecked()))
            {
                var embeddedPdbEntry = peReader.ReadDebugDirectory().FirstOrDefault(e => e.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
                if (embeddedPdbEntry.DataSize != 0)
                {
                    return DebugInformationReaderProvider.CreateFromMetadataReader(peReader.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedPdbEntry));
                }
            }

            return null;
        }

        private Stream ValidateStream(Stream stream, string methodName)
        {
            if (stream != null && (!stream.CanRead || !stream.CanSeek))
            {
                throw new InvalidOperationException(string.Format(FeaturesResources.MethodMustReturnStreamThatSupportsReadAndSeek, methodName));
            }

            return stream;
        }

        private Stream OpenPdbStreamChecked()
            => ValidateStream(OpenPdbStream(), nameof(OpenPdbStream));

        private Stream OpenAssemblyStreamChecked()
            => ValidateStream(OpenAssemblyStream(), nameof(OpenAssemblyStream));

        /// <summary>
        /// Opens an assembly file produced by the compiler.
        /// </summary>
        /// <remarks>
        /// The stream must be readable and seekable.
        /// </remarks>
        /// <returns>New <see cref="Stream"/> instance or null if the assembly is not available.</returns>
        protected abstract Stream OpenAssemblyStream();

        /// <summary>
        /// Opens a PDB file produced by the compiler.
        /// </summary>
        /// <remarks>
        /// The stream must be readable and seekable.
        /// </remarks>
        /// <returns>New <see cref="Stream"/> instance or null if the compiler generated no PDB (the symbols might be embedded in the assembly).</returns>
        protected abstract Stream OpenPdbStream();
    }
}
