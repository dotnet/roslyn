// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Roslyn.Reflection.Metadata.Ecma335.Blobs;
using System.Reflection.PortableExecutable;
using Xunit;
using Roslyn.Reflection;
using Roslyn.Reflection.Metadata.Ecma335;
using Roslyn.Reflection.PortableExecutable;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class PEBuilderTests
    {
        [Fact]
        public void Basic()
        {
            using (var peStream = new MemoryStream())
            {
                WritePEImage(peStream);

                peStream.Position = 0;
                var r = new PEReader(peStream);
                var h = r.PEHeaders;
                var mdReader = r.GetMetadataReader();
            }
        }

        public static void WritePEImage(Stream peStream)
        {
            var peBuilder = new PEBuilder(
                machine: 0, 
                sectionAlignment: 0x2000,
                fileAlignment: 0x200, 
                imageBase: 0x00400000,
                majorLinkerVersion: 0x30, // (what is ref.emit using?)
                minorLinkerVersion: 0,
                majorOperatingSystemVersion: 4,
                minorOperatingSystemVersion: 0,
                majorImageVersion: 0,
                minorImageVersion: 0,
                majorSubsystemVersion: 4,
                minorSubsystemVersion: 0,
                subsystem: Subsystem.WindowsCui,
                dllCharacteristics: DllCharacteristics.DynamicBase | DllCharacteristics.NxCompatible | DllCharacteristics.NoSeh | DllCharacteristics.TerminalServerAware,
                imageCharacteristics: Characteristics.ExecutableImage,
                sizeOfStackReserve: 0x00100000,
                sizeOfStackCommit: 0x1000,
                sizeOfHeapReserve: 0x00100000,
                sizeOfHeapCommit: 0x1000);

            var ilBuilder = new BlobBuilder();
            var metadataBlobBuilder = new BlobBuilder();
            var mappedFieldDataBuilder = new BlobBuilder();
            var managedResourceDataBuilder = new BlobBuilder();

            var metadata = new MetadataBuilder();
            int mainMethodDefRowId;
            EmitMetadataAndIL(metadata, ilBuilder, out mainMethodDefRowId);

            var peDirectoriesBuilder = new PEDirectoriesBuilder();

            peBuilder.AddManagedSections(
                peDirectoriesBuilder,
                new TypeSystemMetadataSerializer(metadata, "v4.0.30319", isMinimalDelta: false),
                ilBuilder,
                mappedFieldDataBuilder,
                managedResourceDataBuilder,
                nativeResourceSectionSerializer: null,
                strongNameSignatureSize: 0,
                entryPointToken: 0x06000000 | mainMethodDefRowId,
                pdbPathOpt: null,
                nativePdbContentId: default(ContentId),
                portablePdbContentId: default(ContentId),
                corFlags: CorFlags.ILOnly);

            var peBlob = new BlobBuilder();
            ContentId peContentId;
            peBuilder.Serialize(peBlob, peDirectoriesBuilder, out peContentId);

            peBlob.WriteContentTo(peStream);
        }

        private static void EmitMetadataAndIL(MetadataBuilder metadata, BlobBuilder ilBuilder, out int mainMethodDefRowId)
        {
            metadata.AddModule(0, metadata.GetStringIndex("ConsoleApplication.exe"), metadata.GetGuidIndex(Guid.NewGuid()), default(GuidIdx), default(GuidIdx));

            metadata.AddAssembly(
                metadata.GetStringIndex("ConsoleApplication"),
                version: new Version(0, 0, 0, 0),
                culture: default(StringIdx),
                publicKey: default(BlobIdx),
                flags: 0,
                hashAlgorithm: AssemblyHashAlgorithm.Sha1);

            int mscorlibAssemblyRefRowId = metadata.AddAssemblyReference(
                name: metadata.GetStringIndex("mscorlib"),
                version: new Version(4, 0, 0, 0),
                culture: default(StringIdx),
                publicKeyOrToken: metadata.GetBlobIndex(ImmutableArray.Create<byte>(0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89)),
                flags: 0,
                hashValue: default(BlobIdx));

            int systemObjectTypeRefRowId = metadata.AddTypeReference(
                mscorlibAssemblyRefRowId.ToCodedIndex(CodedIndex.ResolutionScope.AssemblyRef),
                metadata.GetStringIndex("System"),
                metadata.GetStringIndex("Object"));

            int systemConsoleTypeRefRowId = metadata.AddTypeReference(
                mscorlibAssemblyRefRowId.ToCodedIndex(CodedIndex.ResolutionScope.AssemblyRef),
                metadata.GetStringIndex("System"),
                metadata.GetStringIndex("Console"));

            var consoleWriteLineSignature = new BlobEncoder(new BlobBuilder()).
                MethodSignature(SignatureCallingConvention.Default, genericParameterCount: 0, isInstanceMethod: false).
                Parameters(1).
                    EndModifiers().Void().
                    AddParameter().ModifiedType().EndModifiers().Type(isByRef: false).String().
                EndParameters();

            int consoleWriteLineMemberRefRowId = metadata.AddMemberReference(
                systemConsoleTypeRefRowId.ToCodedIndex(CodedIndex.MemberRefParent.TypeRef),
                metadata.GetStringIndex("WriteLine"),
                metadata.GetBlobIndex(consoleWriteLineSignature.Builder));

            var parameterlessCtorSignature = new BlobEncoder(new BlobBuilder()).
                MethodSignature(SignatureCallingConvention.Default, genericParameterCount: 0, isInstanceMethod: true).
                Parameters(0).EndModifiers().Void().EndParameters();

            var parameterlessCtorBlobIndex = metadata.GetBlobIndex(parameterlessCtorSignature.Builder);

            int objectCtorMemberRefRowId = metadata.AddMemberReference(
                systemObjectTypeRefRowId.ToCodedIndex(CodedIndex.MemberRefParent.TypeRef),
                metadata.GetStringIndex(".ctor"),
                parameterlessCtorBlobIndex);

            var mainSignature = new BlobEncoder(new BlobBuilder()).
                MethodSignature(SignatureCallingConvention.Default, genericParameterCount: 0, isInstanceMethod: false).
                Parameters(0).EndModifiers().Void().EndParameters();

            //
            // Program::Main
            //
            int mainBodyOffset = ilBuilder.Count;
            ilBuilder.WriteByte(((5 + 5 + 1) << 2) | 2); // IL length

            // ldstr "Hello world"
            ilBuilder.WriteByte(0x72);
            ilBuilder.WriteInt32(metadata.GetUserStringToken("Hello world!"));

            // call void [mscorlib]System.Console::WriteLine(string)
            ilBuilder.WriteByte(0x28);
            ilBuilder.WriteInt32(0x0A000000 | consoleWriteLineMemberRefRowId);

            // ret
            ilBuilder.WriteByte(0x2A);

            //
            // Program::.ctor
            //
            int ctorBodyOffset = ilBuilder.Count;
            ilBuilder.WriteByte(((1 + 5 + 1) << 2) | 2); // IL length

            // ldarg.0
            ilBuilder.WriteByte(0x02);

            // call instance void [mscorlib]System.Object::.ctor()
            ilBuilder.WriteByte(0x28);
            ilBuilder.WriteInt32(0x0A000000 | objectCtorMemberRefRowId);

            // ret
            ilBuilder.WriteByte(0x2A);

            mainMethodDefRowId = metadata.AddMethodDefinition(
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                metadata.GetStringIndex("Main"),
                metadata.GetBlobIndex(mainSignature.Builder),
                mainBodyOffset,
                paramList: 0);

            int ctorDefRowId = metadata.AddMethodDefinition(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                metadata.GetStringIndex(".ctor"),
                parameterlessCtorBlobIndex,
                ctorBodyOffset,
                paramList: 0);

            metadata.AddTypeDefinition(
                0,
                default(StringIdx),
                metadata.GetStringIndex("<Module>"),
                baseTypeCodedIndex: 0,
                fieldList: 1,
                methodList: 1);

            metadata.AddTypeDefinition(
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit,
                metadata.GetStringIndex("ConsoleApplication"),
                metadata.GetStringIndex("Program"),
                systemObjectTypeRefRowId.ToCodedIndex(CodedIndex.TypeDefOrRef.TypeRef),
                fieldList: 1,
                methodList: mainMethodDefRowId);
        }
    }
}
