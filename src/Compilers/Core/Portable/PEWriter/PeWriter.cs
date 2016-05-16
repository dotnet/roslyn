// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using EmitContext = Microsoft.CodeAnalysis.Emit.EmitContext;
using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.Cci
{
    using Roslyn.Reflection;
    using Roslyn.Reflection.PortableExecutable;

    internal sealed class PeWritingException : Exception
    {
        public PeWritingException(Exception inner)
            : base(inner.Message, inner)
        { }
    }

    internal static class PeWriter
    {
        public static bool WritePeToStream(
            EmitContext context,
            CommonMessageProvider messageProvider,
            Func<Stream> getPeStream,
            Func<Stream> getPortablePdbStreamOpt,
            PdbWriter nativePdbWriterOpt,
            string pdbPathOpt,
            bool allowMissingMethodBodies,
            bool isDeterministic,
            CancellationToken cancellationToken)
        {
            // If PDB writer is given, we have to have PDB path.
            Debug.Assert(nativePdbWriterOpt == null || pdbPathOpt != null);

            var mdWriter = FullMetadataWriter.Create(context, messageProvider, allowMissingMethodBodies, isDeterministic, getPortablePdbStreamOpt != null, cancellationToken);

            var properties = context.Module.Properties;

            nativePdbWriterOpt?.SetMetadataEmitter(mdWriter);

            // Since we are producing a full assembly, we should not have a module version ID
            // imposed ahead-of time. Instead we will compute a deterministic module version ID
            // based on the contents of the generated stream.
            Debug.Assert(properties.PersistentIdentifier == default(Guid));

            var ilBuilder = new BlobBuilder(32 * 1024);
            var mappedFieldDataBuilder = new BlobBuilder();
            var managedResourceBuilder = new BlobBuilder(1024);

            Blob mvidFixup;
            mdWriter.BuildMetadataAndIL(
                nativePdbWriterOpt,
                ilBuilder,
                mappedFieldDataBuilder,
                managedResourceBuilder,
                out mvidFixup);

            MethodDefinitionHandle entryPointHandle;
            MethodDefinitionHandle debugEntryPointHandle;
            mdWriter.GetEntryPoints(out entryPointHandle, out debugEntryPointHandle);
            
            if (!debugEntryPointHandle.IsNil)
            {
                nativePdbWriterOpt?.SetEntryPoint((uint)MetadataTokens.GetToken(debugEntryPointHandle));
            }

            if (nativePdbWriterOpt != null)
            {
                var assembly = mdWriter.Module.AsAssembly;
                if (assembly != null && assembly.Kind == OutputKind.WindowsRuntimeMetadata)
                {
                    // Dev12: If compiling to winmdobj, we need to add to PDB source spans of
                    //        all types and members for better error reporting by WinMDExp.
                    nativePdbWriterOpt.WriteDefinitionLocations(mdWriter.Module.GetSymbolToLocationMap());
                }
                else
                {
#if DEBUG
                    // validate that all definitions are writable
                    // if same scenario would happen in an winmdobj project
                    nativePdbWriterOpt.AssertAllDefinitionsHaveTokens(mdWriter.Module.GetSymbolToLocationMap());
#endif
                }
            }

            Stream peStream = getPeStream();
            if (peStream == null)
            {
                return false;
            }

            ContentId nativePdbContentId = nativePdbWriterOpt?.GetContentId() ?? default(ContentId);

            // the writer shall not be used after this point for writing:
            nativePdbWriterOpt = null;

            var metadataSerializer = mdWriter.GetTypeSystemMetadataSerializer();

            var peBuilder = new PEBuilder(
                machine: properties.Machine,
                sectionAlignment: properties.SectionAlignment,
                fileAlignment: properties.FileAlignment,
                imageBase: properties.BaseAddress,
                majorLinkerVersion: properties.LinkerMajorVersion,
                minorLinkerVersion: properties.LinkerMinorVersion,
                majorOperatingSystemVersion: 4,
                minorOperatingSystemVersion: 0,
                majorImageVersion: 0,
                minorImageVersion: 0,
                majorSubsystemVersion: properties.MajorSubsystemVersion,
                minorSubsystemVersion: properties.MinorSubsystemVersion,
                subsystem: properties.Subsystem,
                dllCharacteristics: properties.DllCharacteristics,
                imageCharacteristics: properties.ImageCharacteristics,
                sizeOfStackReserve: properties.SizeOfStackReserve,
                sizeOfStackCommit: properties.SizeOfStackCommit,
                sizeOfHeapReserve: properties.SizeOfHeapReserve,
                sizeOfHeapCommit: properties.SizeOfHeapCommit,
                deterministicIdProvider: isDeterministic ? new Func<BlobBuilder, ContentId>(content => ContentId.FromHash(CryptographicHashProvider.ComputeSha1(content))) : null);

            ContentId portablePdbContentId;
            if (mdWriter.EmitStandaloneDebugMetadata)
            {
                Debug.Assert(getPortablePdbStreamOpt != null);

                var debugMetadataBuilder = new BlobBuilder();
                var debugMetadataSerializer = mdWriter.GetStandaloneDebugMetadataSerializer(metadataSerializer.MetadataSizes, debugEntryPointHandle);
                debugMetadataSerializer.SerializeMetadata(debugMetadataBuilder, peBuilder.IdProvider, out portablePdbContentId);

                // write to Portable PDB stream:
                Stream portablePdbStream = getPortablePdbStreamOpt();
                if (portablePdbStream != null)
                {
                    debugMetadataBuilder.WriteContentTo(portablePdbStream);
                }
            }
            else
            {
                portablePdbContentId = default(ContentId);
            }

            var peDirectoriesBuilder = new PEDirectoriesBuilder();

            peBuilder.AddManagedSections(
                peDirectoriesBuilder,
                metadataSerializer,
                ilBuilder,
                mappedFieldDataBuilder,
                managedResourceBuilder,
                CreateNativeResourceSectionSerializer(context.Module),
                CalculateStrongNameSignatureSize(context.Module),
                entryPointHandle,
                pdbPathOpt,
                nativePdbContentId,
                portablePdbContentId,
                properties.CorFlags);

            var peBlob = new BlobBuilder();
            ContentId peContentId;
            peBuilder.Serialize(peBlob, peDirectoriesBuilder, out peContentId);

            // Patch MVID
            if (!mvidFixup.IsDefault)
            {
                var mvidWriter = new BlobWriter(mvidFixup);
                mvidWriter.WriteBytes(peContentId.Guid);
                Debug.Assert(mvidWriter.RemainingBytes == 0);
            }

            try
            {
                peBlob.WriteContentTo(peStream);
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                throw new PeWritingException(e);
            }

            return true;
        }

        private static Action<BlobBuilder, PESectionLocation> CreateNativeResourceSectionSerializer(IModule module)
        {
            // Win32 resources are supplied to the compiler in one of two forms, .RES (the output of the resource compiler),
            // or .OBJ (the output of running cvtres.exe on a .RES file). A .RES file is parsed and processed into
            // a set of objects implementing IWin32Resources. These are then ordered and the final image form is constructed
            // and written to the resource section. Resources in .OBJ form are already very close to their final output
            // form. Rather than reading them and parsing them into a set of objects similar to those produced by 
            // processing a .RES file, we process them like the native linker would, copy the relevant sections from 
            // the .OBJ into our output and apply some fixups.

            var nativeResourcesOpt = module.Win32Resources;
            var nativeResourceSectionOpt = module.Win32ResourceSection;
            if (!IteratorHelper.EnumerableIsEmpty(nativeResourcesOpt) || nativeResourceSectionOpt != null)
            {
                return (sectionBuilder, location) =>
                {
                    if (nativeResourceSectionOpt != null)
                    {
                        NativeResourceWriter.SerializeWin32Resources(sectionBuilder, nativeResourceSectionOpt, location.RelativeVirtualAddress);
                    }
                    else
                    {
                        NativeResourceWriter.SerializeWin32Resources(sectionBuilder, nativeResourcesOpt, location.RelativeVirtualAddress);
                    }
                };
            }

            return null;
        }

        private static int CalculateStrongNameSignatureSize(IModule module)
        {
            IAssembly assembly = module.AsAssembly;
            if (assembly == null)
            {
                return 0;
            }

            // EDMAURER the count of characters divided by two because the each pair of characters will turn in to one byte.
            int keySize = (assembly.SignatureKey == null) ? 0 : assembly.SignatureKey.Length / 2;

            if (keySize == 0)
            {
                keySize = assembly.PublicKey.Length;
            }

            if (keySize == 0)
            {
                return 0;
            }

            return (keySize < 128 + 32) ? 128 : keySize - 32;
        }
    }
}
