﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.DiaSymReader;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Reader of debug information needed for EnC.
    /// This object does not own the underlying memory (SymReader/MetadataReader).
    /// </summary>
    internal abstract class EditAndContinueMethodDebugInfoReader
    {
        public abstract bool IsPortable { get; }
        public abstract EditAndContinueMethodDebugInformation GetDebugInfo(MethodDefinitionHandle methodHandle);
        public abstract StandaloneSignatureHandle GetLocalSignature(MethodDefinitionHandle methodHandle);

        private sealed class Native : EditAndContinueMethodDebugInfoReader
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
                int methodToken = MetadataTokens.GetToken(methodHandle);

                byte[] debugInfo;
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
                catch (Exception e) when (FatalError.ReportWithoutCrash(e)) // likely a bug in the compiler/debugger
                {
                    throw new InvalidDataException(e.Message, e);
                }

                try
                {
                    ImmutableArray<byte> localSlots, lambdaMap;
                    if (debugInfo != null)
                    {
                        localSlots = CustomDebugInfoReader.TryGetCustomDebugInfoRecord(debugInfo, CustomDebugInfoKind.EditAndContinueLocalSlotMap);
                        lambdaMap = CustomDebugInfoReader.TryGetCustomDebugInfoRecord(debugInfo, CustomDebugInfoKind.EditAndContinueLambdaMap);
                    }
                    else
                    {
                        localSlots = lambdaMap = default;
                    }

                    return EditAndContinueMethodDebugInformation.Create(localSlots, lambdaMap);
                }
                catch (InvalidOperationException e) when (FatalError.ReportWithoutCrash(e)) // likely a bug in the compiler/debugger
                {
                    // TODO: CustomDebugInfoReader should throw InvalidDataException
                    throw new InvalidDataException(e.Message, e);
                }
            }
        }

        private sealed class Portable : EditAndContinueMethodDebugInfoReader
        {
            private readonly MetadataReader _pdbReader;

            public Portable(MetadataReader pdbReader)
            {
                _pdbReader = pdbReader;
            }

            public override bool IsPortable => true;

            public override StandaloneSignatureHandle GetLocalSignature(MethodDefinitionHandle methodHandle)
                => _pdbReader.GetMethodDebugInformation(methodHandle.ToDebugInformationHandle()).LocalSignature;

            public override EditAndContinueMethodDebugInformation GetDebugInfo(MethodDefinitionHandle methodHandle)
                => EditAndContinueMethodDebugInformation.Create(
                    compressedSlotMap: GetCdiBytes(methodHandle, PortableCustomDebugInfoKinds.EncLocalSlotMap),
                    compressedLambdaMap: GetCdiBytes(methodHandle, PortableCustomDebugInfoKinds.EncLambdaAndClosureMap));

            private ImmutableArray<byte> GetCdiBytes(MethodDefinitionHandle methodHandle, Guid kind)
                => TryGetCustomDebugInformation(_pdbReader, methodHandle, kind, out var cdi) ?
                    _pdbReader.GetBlobContent(cdi.Value) : default;

            /// <exception cref="BadImageFormatException">Invalid data format.</exception>
            private static bool TryGetCustomDebugInformation(MetadataReader reader, EntityHandle handle, Guid kind, out CustomDebugInformation customDebugInfo)
            {
                bool foundAny = false;
                customDebugInfo = default;
                foreach (var infoHandle in reader.GetCustomDebugInformation(handle))
                {
                    var info = reader.GetCustomDebugInformation(infoHandle);
                    var id = reader.GetGuid(info.Kind);
                    if (id == kind)
                    {
                        if (foundAny)
                        {
                            throw new BadImageFormatException();
                        }

                        customDebugInfo = info;
                        foundAny = true;
                    }
                }

                return foundAny;
            }
        }

        public unsafe static EditAndContinueMethodDebugInfoReader Create(ISymUnmanagedReader5 symReader, int version)
        {
            int hr = symReader.GetPortableDebugMetadataByVersion(version, metadata: out byte* metadata, size: out int size);
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
    }
}
