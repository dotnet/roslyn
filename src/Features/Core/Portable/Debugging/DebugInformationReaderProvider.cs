// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.DiaSymReader;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Debugging;

/// <summary>
/// An abstraction of a symbol reader that provides a reader of Edit and Continue debug information.
/// Owns the underlying PDB reader.
/// </summary>
internal abstract class DebugInformationReaderProvider : IDisposable
{
    private sealed class DummySymReaderMetadataProvider : ISymReaderMetadataProvider
    {
        public static readonly DummySymReaderMetadataProvider Instance = new();

        public unsafe bool TryGetStandaloneSignature(int standaloneSignatureToken, out byte* signature, out int length)
            => throw ExceptionUtilities.Unreachable();

        public bool TryGetTypeDefinitionInfo(int typeDefinitionToken, out string namespaceName, out string typeName, out TypeAttributes attributes)
            => throw ExceptionUtilities.Unreachable();

        public bool TryGetTypeReferenceInfo(int typeReferenceToken, out string namespaceName, out string typeName)
            => throw ExceptionUtilities.Unreachable();
    }

    private sealed class Portable(MetadataReaderProvider pdbReaderProvider) : DebugInformationReaderProvider
    {
        private readonly MetadataReaderProvider _pdbReaderProvider = pdbReaderProvider;

        public override EditAndContinueMethodDebugInfoReader CreateEditAndContinueMethodDebugInfoReader()
            => EditAndContinueMethodDebugInfoReader.Create(_pdbReaderProvider.GetMetadataReader());

        public override ValueTask CopyContentToAsync(Stream stream, CancellationToken cancellationToken)
        {
            var reader = _pdbReaderProvider.GetMetadataReader();
            unsafe
            {
                using var metadataStream = new UnmanagedMemoryStream(reader.MetadataPointer, reader.MetadataLength);
                metadataStream.CopyTo(stream);
            }

            return ValueTaskFactory.CompletedTask;
        }

        public override void Dispose()
            => _pdbReaderProvider.Dispose();
    }

    private sealed class Native(Stream stream, ISymUnmanagedReader5 symReader, int version) : DebugInformationReaderProvider
    {
        private readonly Stream _stream = stream;
        private readonly int _version = version;
        private ISymUnmanagedReader5 _symReader = symReader;

        public override EditAndContinueMethodDebugInfoReader CreateEditAndContinueMethodDebugInfoReader()
            => EditAndContinueMethodDebugInfoReader.Create(_symReader, _version);

        public override async ValueTask CopyContentToAsync(Stream stream, CancellationToken cancellationToken)
        {
            var position = _stream.Position;
            try
            {
                _stream.Position = 0;
                await _stream.CopyToAsync(stream, bufferSize: 4 * 1024, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _stream.Position = position;
            }
        }

        public override void Dispose()
        {
            _stream.Dispose();

            var symReader = Interlocked.Exchange(ref _symReader, null);
            if (symReader != null && Marshal.IsComObject(symReader))
            {
#if NETCOREAPP
                Debug.Assert(OperatingSystem.IsWindows());
#endif
                Marshal.ReleaseComObject(symReader);
            }
        }
    }

    public abstract void Dispose();

    /// <summary>
    /// Creates EnC debug information reader.
    /// </summary>
    public abstract EditAndContinueMethodDebugInfoReader CreateEditAndContinueMethodDebugInfoReader();

    public abstract ValueTask CopyContentToAsync(Stream stream, CancellationToken cancellationToken);

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

        return CreateNative(stream);
    }

    // Do not inline to avoid loading Microsoft.DiaSymReader until it's actually needed.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static DebugInformationReaderProvider CreateNative(Stream stream)
    {
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
