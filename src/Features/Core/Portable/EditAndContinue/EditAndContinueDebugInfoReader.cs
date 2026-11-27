// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.DiaSymReader;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Reader of debug information needed for EnC.
/// This object does not own the underlying memory (SymReader/MetadataReader).
/// </summary>
internal abstract class EditAndContinueDebugInfoReader
{
    public abstract bool IsPortable { get; }
    public abstract EditAndContinueMethodDebugInformation GetDebugInfo(MethodDefinitionHandle methodHandle);
    public abstract StandaloneSignatureHandle GetLocalSignature(MethodDefinitionHandle methodHandle);
    public abstract ImmutableDictionary<string, string> GetCompilationOptions();

    /// <summary>
    /// Returns default source file encoding specified in the compilation options.
    /// </summary>
    /// <exception cref="NotSupportedException">The PDB does not support compilation options, the options do not include encoding, or the encoding is not supproted by the platform.</exception>
    public abstract Encoding? GetDefaultSourceFileEncoding();

    /// <summary>
    /// Reads document checksum.
    /// </summary>
    /// <returns>True if a document with given path is listed in the PDB.</returns>
    /// <exception cref="Exception">Error reading debug information from the PDB.</exception>
    public abstract bool TryGetDocumentChecksum(string documentPath, out ImmutableArray<byte> checksum, out Guid algorithmId);

    private sealed class Native : EditAndContinueDebugInfoReader
    {
        private readonly ISymUnmanagedReader5 _symReader;
        private readonly int _version;

        public Native(ISymUnmanagedReader5 symReader, int version)
        {
            Debug.Assert(symReader != null);
            Debug.Assert(version >= 1);

            _symReader = symReader;
            _version = version;
        }

        public override bool IsPortable => false;

        public override StandaloneSignatureHandle GetLocalSignature(MethodDefinitionHandle methodHandle)
        {
            var symMethod = (ISymUnmanagedMethod2)_symReader.GetMethodByVersion(MetadataTokens.GetToken(methodHandle), _version);

            // Compiler generated methods (e.g. async kick-off methods) might not have debug information.
            return symMethod == null ? default : MetadataTokens.StandaloneSignatureHandle(symMethod.GetLocalSignatureToken());
        }

        public override EditAndContinueMethodDebugInformation GetDebugInfo(MethodDefinitionHandle methodHandle)
        {
            var methodToken = MetadataTokens.GetToken(methodHandle);

            byte[]? debugInfo;
            try
            {
                debugInfo = _symReader.GetCustomDebugInfo(methodToken, _version);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Sometimes the debugger returns the HRESULT for ArgumentOutOfRangeException, rather than E_FAIL,
                // for methods without custom debug info (https://github.com/dotnet/roslyn/issues/4138).
                debugInfo = null;
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e)) // likely a bug in the compiler/debugger
            {
                throw new InvalidDataException(e.Message, e);
            }

            try
            {
                ImmutableArray<byte> localSlots, lambdaMap, stateMachineSuspensionPoints;
                if (debugInfo != null)
                {
                    localSlots = CustomDebugInfoReader.TryGetCustomDebugInfoRecord(debugInfo, CustomDebugInfoKind.EditAndContinueLocalSlotMap);
                    lambdaMap = CustomDebugInfoReader.TryGetCustomDebugInfoRecord(debugInfo, CustomDebugInfoKind.EditAndContinueLambdaMap);
                    stateMachineSuspensionPoints = CustomDebugInfoReader.TryGetCustomDebugInfoRecord(debugInfo, CustomDebugInfoKind.EditAndContinueStateMachineStateMap);
                }
                else
                {
                    localSlots = lambdaMap = stateMachineSuspensionPoints = default;
                }

                return EditAndContinueMethodDebugInformation.Create(localSlots, lambdaMap, stateMachineSuspensionPoints);
            }
            catch (InvalidOperationException e) when (FatalError.ReportAndCatch(e)) // likely a bug in the compiler/debugger
            {
                // TODO: CustomDebugInfoReader should throw InvalidDataException
                throw new InvalidDataException(e.Message, e);
            }
        }

        public override bool TryGetDocumentChecksum(string documentPath, out ImmutableArray<byte> checksum, out Guid algorithmId)
            => TryGetDocumentChecksum(_symReader, documentPath, out checksum, out algorithmId);

        public override ImmutableDictionary<string, string> GetCompilationOptions()
            // Windows PDBs do not store compilation options.
            => ImmutableDictionary<string, string>.Empty;

        public override Encoding? GetDefaultSourceFileEncoding()
            => throw new NotSupportedException("Windows PDB does not support compilation options.");
    }

    private sealed class Portable : EditAndContinueDebugInfoReader
    {
        private readonly MetadataReader _pdbReader;
        private readonly Lazy<Encoding?> _lazyDefaultSourceFileEncoding;
        private ImmutableDictionary<string, string>? _lazyCompilationOptions;

        public Portable(MetadataReader pdbReader)
        {
            _pdbReader = pdbReader;
            _lazyDefaultSourceFileEncoding = new(ReadDefaultSourceFileEncoding);
        }

        public override bool IsPortable => true;

        public override StandaloneSignatureHandle GetLocalSignature(MethodDefinitionHandle methodHandle)
            => _pdbReader.GetMethodDebugInformation(methodHandle.ToDebugInformationHandle()).LocalSignature;

        public override EditAndContinueMethodDebugInformation GetDebugInfo(MethodDefinitionHandle methodHandle)
            => EditAndContinueMethodDebugInformation.Create(
                compressedSlotMap: GetCdiBytes(methodHandle, PortableCustomDebugInfoKinds.EncLocalSlotMap),
                compressedLambdaMap: GetCdiBytes(methodHandle, PortableCustomDebugInfoKinds.EncLambdaAndClosureMap),
                compressedStateMachineStateMap: GetCdiBytes(methodHandle, PortableCustomDebugInfoKinds.EncStateMachineStateMap));

        private ImmutableArray<byte> GetCdiBytes(MethodDefinitionHandle methodHandle, Guid kind)
            => _pdbReader.TryGetCustomDebugInformation(methodHandle, kind, out var cdi)
                ? _pdbReader.GetBlobContent(cdi.Value)
                : default;

        public override bool TryGetDocumentChecksum(string documentPath, out ImmutableArray<byte> checksum, out Guid algorithmId)
        {
            foreach (var documentHandle in _pdbReader.Documents)
            {
                var document = _pdbReader.GetDocument(documentHandle);

                if (_pdbReader.StringComparer.Equals(document.Name, documentPath, ignoreCase: false))
                {
                    checksum = _pdbReader.GetBlobContent(document.Hash);
                    algorithmId = _pdbReader.GetGuid(document.HashAlgorithm);
                    return true;
                }
            }

            checksum = default;
            algorithmId = default;
            return false;
        }

        public override ImmutableDictionary<string, string> GetCompilationOptions()
            => _lazyCompilationOptions ??= _pdbReader.GetCompilationOptions();

        public override Encoding? GetDefaultSourceFileEncoding()
            => _lazyDefaultSourceFileEncoding.Value;

        private Encoding? ReadDefaultSourceFileEncoding()
        {
            var options = GetCompilationOptions();

            if (!options.TryGetValue(CompilationOptionNames.DefaultEncoding, out var encodingName) &&
                !options.TryGetValue(CompilationOptionNames.FallbackEncoding, out encodingName))
            {
                return null;
            }

            try
            {
                return Encoding.GetEncoding(encodingName);
            }
            catch (Exception e)
            {
                throw new NotSupportedException($"Encoding '{encodingName}' not supported: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Creates <see cref="EditAndContinueDebugInfoReader"/> backed by a given <see cref="ISymUnmanagedReader5"/>.
    /// </summary>
    /// <param name="symReader">SymReader open on a Portable or Windows PDB.</param>
    /// <param name="version">The version of the PDB to read.</param>
    /// <exception cref="ArgumentNullException"><paramref name="symReader"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is less than 1.</exception>
    /// <exception cref="COMException">Error reading debug information.</exception>
    /// <returns>
    /// The resulting reader does not take ownership of the <paramref name="symReader"/> or the memory it reads.
    /// </returns>
    /// <remarks>
    /// Automatically detects the underlying PDB format and returns the appropriate reader.
    /// </remarks>
    public static unsafe EditAndContinueDebugInfoReader Create(ISymUnmanagedReader5 symReader, int version = 1)
    {
        if (symReader == null)
        {
            throw new ArgumentNullException(nameof(symReader));
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        var hr = symReader.GetPortableDebugMetadataByVersion(version, metadata: out var metadata, size: out var size);
        Marshal.ThrowExceptionForHR(hr);

        if (hr == 0)
        {
            return new Portable(new MetadataReader(metadata, size));
        }
        else
        {
            return new Native(symReader, version);
        }
    }

    /// <summary>
    /// Creates <see cref="EditAndContinueDebugInfoReader"/> back by a given <see cref="MetadataReader"/>.
    /// </summary>
    /// <param name="pdbReader"><see cref="MetadataReader"/> open on a Portable PDB.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pdbReader"/> is null.</exception>
    /// <returns>
    /// The resulting reader does not take ownership of the <paramref name="pdbReader"/> or the memory it reads.
    /// </returns>
    public static unsafe EditAndContinueDebugInfoReader Create(MetadataReader pdbReader)
       => new Portable(pdbReader ?? throw new ArgumentNullException(nameof(pdbReader)));

    internal static bool TryGetDocumentChecksum(ISymUnmanagedReader5 symReader, string documentPath, out ImmutableArray<byte> checksum, out Guid algorithmId)
    {
        var symDocument = symReader.GetDocument(documentPath);

        // Make sure the full path matches.
        // Native SymReader allows partial match on the document file name.
        if (symDocument == null || !StringComparer.Ordinal.Equals(symDocument.GetName(), documentPath))
        {
            checksum = default;
            algorithmId = default;
            return false;
        }

        algorithmId = symDocument.GetHashAlgorithm();
        checksum = [.. symDocument.GetChecksum()];
        return true;
    }
}
