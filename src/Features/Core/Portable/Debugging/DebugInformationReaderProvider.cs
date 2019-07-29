// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.DiaSymReader;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Debugging
{
    /// <summary>
    /// An abstraction of a symbol reader that provides a reader of Edit and Continue debug information.
    /// Owns the underlying PDB reader.
    /// </summary>
    internal abstract class DebugInformationReaderProvider : IDisposable
    {
        private sealed class DummySymReaderMetadataProvider : ISymReaderMetadataProvider
        {
            public static readonly DummySymReaderMetadataProvider Instance = new DummySymReaderMetadataProvider();

            public unsafe bool TryGetStandaloneSignature(int standaloneSignatureToken, out byte* signature, out int length)
                => throw ExceptionUtilities.Unreachable;

            public bool TryGetTypeDefinitionInfo(int typeDefinitionToken, out string namespaceName, out string typeName, out TypeAttributes attributes)
                => throw ExceptionUtilities.Unreachable;

            public bool TryGetTypeReferenceInfo(int typeReferenceToken, out string namespaceName, out string typeName)
                => throw ExceptionUtilities.Unreachable;
        }

        private sealed class Portable : DebugInformationReaderProvider
        {
            private readonly MetadataReaderProvider _pdbReaderProvider;

            public Portable(MetadataReaderProvider pdbReaderProvider)
                => _pdbReaderProvider = pdbReaderProvider;

            public override EditAndContinueMethodDebugInfoReader CreateEditAndContinueMethodDebugInfoReader()
                => EditAndContinueMethodDebugInfoReader.Create(_pdbReaderProvider.GetMetadataReader());

            public override void Dispose()
                => _pdbReaderProvider.Dispose();
        }

        private sealed class Native : DebugInformationReaderProvider
        {
            private readonly Stream _stream;
            private readonly int _version;
            private ISymUnmanagedReader5 _symReader;

            public Native(Stream stream, ISymUnmanagedReader5 symReader, int version)
            {
                _stream = stream;
                _symReader = symReader;
                _version = version;
            }

            public override EditAndContinueMethodDebugInfoReader CreateEditAndContinueMethodDebugInfoReader()
                => EditAndContinueMethodDebugInfoReader.Create(_symReader, _version);

            public override void Dispose()
            {
                _stream.Dispose();

                var symReader = Interlocked.Exchange(ref _symReader, null);
                if (symReader != null && Marshal.IsComObject(symReader))
                {
                    Marshal.ReleaseComObject(symReader);
                }
            }
        }

        public abstract void Dispose();

        /// <summary>
        /// Creates EnC debug information reader.
        /// </summary>
        public abstract EditAndContinueMethodDebugInfoReader CreateEditAndContinueMethodDebugInfoReader();

        /// <summary>
        /// Creates <see cref="DebugInformationReaderProvider"/> from a stream of Portable or Windows PDB.
        /// </summary>
        /// <returns>
        /// Provider instance, which keeps the <paramref name="stream"/> open until disposed.
        /// </returns>
        /// <remarks>
        /// Requires Microsoft.DiaSymReader.Native.{platform}.dll to be available for reading Windows PDB.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="stream"/> does not support read and seek operations.</exception>
        /// <exception cref="Exception">Error reading debug information from <paramref name="stream"/>.</exception>
        public static DebugInformationReaderProvider CreateFromStream(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead || !stream.CanSeek)
            {
                throw new ArgumentException(FeaturesResources.StreamMustSupportReadAndSeek, nameof(stream));
            }

            var isPortable = stream.ReadByte() == 'B' && stream.ReadByte() == 'S' && stream.ReadByte() == 'J' && stream.ReadByte() == 'B';
            stream.Position = 0;

            if (isPortable)
            {
                return new Portable(MetadataReaderProvider.FromPortablePdbStream(stream));
            }

            // We can use DummySymReaderMetadataProvider since we do not need to decode signatures, 
            // which is the only operation SymReader needs the provider for.
            return new Native(stream, SymUnmanagedReaderFactory.CreateReader<ISymUnmanagedReader5>(
                stream, DummySymReaderMetadataProvider.Instance, SymUnmanagedReaderCreationOptions.UseAlternativeLoadPath), version: 1);
        }

        /// <summary>
        /// Creates <see cref="DebugInformationReaderProvider"/> from a Portable PDB metadata reader provider.
        /// </summary>
        /// <returns>
        /// Provider instance, which takes ownership of the <paramref name="metadataProvider"/> until disposed.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="metadataProvider"/> is null.</exception>
        public static DebugInformationReaderProvider CreateFromMetadataReader(MetadataReaderProvider metadataProvider)
            => new Portable(metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider)));
    }
}
