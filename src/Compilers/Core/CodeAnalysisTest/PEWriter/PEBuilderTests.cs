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
            MethodDefinitionHandle mainMethodDef;
            EmitMetadataAndIL(metadata, ilBuilder, out mainMethodDef);

            var peDirectoriesBuilder = new PEDirectoriesBuilder();

            peBuilder.AddManagedSections(
                peDirectoriesBuilder,
                new TypeSystemMetadataSerializer(metadata, "v4.0.30319", isMinimalDelta: false),
                ilBuilder,
                mappedFieldDataBuilder,
                managedResourceDataBuilder,
                nativeResourceSectionSerializer: null,
                strongNameSignatureSize: 0,
                entryPointToken: MetadataTokens.GetToken(mainMethodDef),
                pdbPathOpt: null,
                nativePdbContentId: default(ContentId),
                portablePdbContentId: default(ContentId),
                corFlags: CorFlags.ILOnly);

            var peBlob = new BlobBuilder();
            ContentId peContentId;
            peBuilder.Serialize(peBlob, peDirectoriesBuilder, out peContentId);

            peBlob.WriteContentTo(peStream);
        }

        private static void EmitMetadataAndIL(MetadataBuilder metadata, BlobBuilder ilBuilder, out MethodDefinitionHandle mainMethodDef)
        {
            metadata.AddModule(0, metadata.GetStringIndex("ConsoleApplication.exe"), metadata.GetGuidIndex(Guid.NewGuid()), default(GuidIdx), default(GuidIdx));

            metadata.AddAssembly(
                metadata.GetStringIndex("ConsoleApplication"),
                version: new Version(0, 0, 0, 0),
                culture: default(StringIdx),
                publicKey: default(BlobIdx),
                flags: default(AssemblyFlags),
                hashAlgorithm: AssemblyHashAlgorithm.Sha1);

            var mscorlibAssemblyRef = metadata.AddAssemblyReference(
                name: metadata.GetStringIndex("mscorlib"),
                version: new Version(4, 0, 0, 0),
                culture: default(StringIdx),
                publicKeyOrToken: metadata.GetBlobIndex(ImmutableArray.Create<byte>(0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89)),
                flags: default(AssemblyFlags),
                hashValue: default(BlobIdx));

            var systemObjectTypeRef = metadata.AddTypeReference(
                mscorlibAssemblyRef,
                metadata.GetStringIndex("System"),
                metadata.GetStringIndex("Object"));

            var systemConsoleTypeRefHandle = metadata.AddTypeReference(
                mscorlibAssemblyRef,
                metadata.GetStringIndex("System"),
                metadata.GetStringIndex("Console"));

            var consoleWriteLineSignature = new BlobEncoder(new BlobBuilder()).
                MethodSignature(SignatureCallingConvention.Default, genericParameterCount: 0, isInstanceMethod: false).
                Parameters(1).
                    EndModifiers().Void().
                    AddParameter().ModifiedType().EndModifiers().Type(isByRef: false).String().
                EndParameters();

            var consoleWriteLineMemberRef = metadata.AddMemberReference(
                systemConsoleTypeRefHandle,
                metadata.GetStringIndex("WriteLine"),
                metadata.GetBlobIndex(consoleWriteLineSignature.Builder));

            var parameterlessCtorSignature = new BlobEncoder(new BlobBuilder()).
                MethodSignature(SignatureCallingConvention.Default, genericParameterCount: 0, isInstanceMethod: true).
                Parameters(0).EndModifiers().Void().EndParameters();

            var parameterlessCtorBlobIndex = metadata.GetBlobIndex(parameterlessCtorSignature.Builder);

            var objectCtorMemberRef = metadata.AddMemberReference(
                systemObjectTypeRef,
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
            ilBuilder.WriteInt32(MetadataTokens.GetToken(consoleWriteLineMemberRef));

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
            ilBuilder.WriteInt32(MetadataTokens.GetToken(objectCtorMemberRef));

            // ret
            ilBuilder.WriteByte(0x2A);

            mainMethodDef = metadata.AddMethodDefinition(
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                metadata.GetStringIndex("Main"),
                metadata.GetBlobIndex(mainSignature.Builder),
                mainBodyOffset,
                paramList: default(ParameterHandle));

            var ctorDef = metadata.AddMethodDefinition(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                metadata.GetStringIndex(".ctor"),
                parameterlessCtorBlobIndex,
                ctorBodyOffset,
                paramList: default(ParameterHandle));

            metadata.AddTypeDefinition(
                default(TypeAttributes),
                default(StringIdx),
                metadata.GetStringIndex("<Module>"),
                baseType: default(EntityHandle),
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: MetadataTokens.MethodDefinitionHandle(1));

            metadata.AddTypeDefinition(
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit,
                metadata.GetStringIndex("ConsoleApplication"),
                metadata.GetStringIndex("Program"),
                systemObjectTypeRef,
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: mainMethodDef);
        }
    }
}
