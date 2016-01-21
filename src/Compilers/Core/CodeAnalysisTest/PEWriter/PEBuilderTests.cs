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
using Roslyn.Reflection.Metadata;
using Roslyn.Reflection.Metadata.Ecma335;
using Roslyn.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.CodeGen;

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

                peStream.Position = 0;
                // File.WriteAllBytes(@"c:\temp\test.exe", peStream.ToArray());
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
            metadata.AddModule(0, metadata.GetString("ConsoleApplication.exe"), metadata.GetGuid(Guid.NewGuid()), default(GuidHandle), default(GuidHandle));

            metadata.AddAssembly(
                metadata.GetString("ConsoleApplication"),
                version: new Version(0, 0, 0, 0),
                culture: default(StringHandle),
                publicKey: default(BlobHandle),
                flags: default(AssemblyFlags),
                hashAlgorithm: AssemblyHashAlgorithm.Sha1);

            var mscorlibAssemblyRef = metadata.AddAssemblyReference(
                name: metadata.GetString("mscorlib"),
                version: new Version(4, 0, 0, 0),
                culture: default(StringHandle),
                publicKeyOrToken: metadata.GetBlob(ImmutableArray.Create<byte>(0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89)),
                flags: default(AssemblyFlags),
                hashValue: default(BlobHandle));

            var systemObjectTypeRef = metadata.AddTypeReference(
                mscorlibAssemblyRef,
                metadata.GetString("System"),
                metadata.GetString("Object"));

            var systemConsoleTypeRefHandle = metadata.AddTypeReference(
                mscorlibAssemblyRef,
                metadata.GetString("System"),
                metadata.GetString("Console"));

            var consoleWriteLineSignature = new BlobEncoder(new BlobBuilder()).
                MethodSignature(SignatureCallingConvention.Default, genericParameterCount: 0, isInstanceMethod: false).
                Parameters(1).
                    EndModifiers().Void().
                    AddParameter().ModifiedType().EndModifiers().Type(isByRef: false).String().
                EndParameters();

            var consoleWriteLineMemberRef = metadata.AddMemberReference(
                systemConsoleTypeRefHandle,
                metadata.GetString("WriteLine"),
                metadata.GetBlob(consoleWriteLineSignature.Builder));

            var parameterlessCtorSignature = new BlobEncoder(new BlobBuilder()).
                MethodSignature(SignatureCallingConvention.Default, genericParameterCount: 0, isInstanceMethod: true).
                Parameters(0).EndModifiers().Void().EndParameters();

            var parameterlessCtorBlobIndex = metadata.GetBlob(parameterlessCtorSignature.Builder);

            var objectCtorMemberRef = metadata.AddMemberReference(
                systemObjectTypeRef,
                metadata.GetString(".ctor"),
                parameterlessCtorBlobIndex);

            var mainSignature = new BlobEncoder(new BlobBuilder()).
                MethodSignature(SignatureCallingConvention.Default, genericParameterCount: 0, isInstanceMethod: false).
                Parameters(0).EndModifiers().Void().EndParameters();

            var methodBodies = new MethodBodiesEncoder(ilBuilder);

            var buffer = new BlobBuilder();
            InstructionEncoder il;

            //
            // Program::.ctor
            //
            int ctorBodyOffset;
            il = new InstructionEncoder(buffer);

            // ldarg.0
            il.LoadArgument(0);

            // call instance void [mscorlib]System.Object::.ctor()
            il.Call(objectCtorMemberRef);

            // ret
            il.OpCode(ILOpCode.Ret);

            methodBodies.AddMethodBody().WriteInstructions(buffer, out ctorBodyOffset);
            buffer.Clear();

            //
            // Program::Main
            //
            int mainBodyOffset;
            il = new InstructionEncoder(buffer);

            // .try
            int tryOffset = il.Offset;

            //   ldstr "hello"
            il.LoadString(metadata.GetUserStringToken("hello"));

            //   call void [mscorlib]System.Console::WriteLine(string)
            il.Call(consoleWriteLineMemberRef);

            //   leave.s END
            il.OpCode(ILOpCode.Leave_s);
            Blob end = il.Builder.ReserveBytes(1);
            int leaveOffset = il.Offset;

            // .finally
            int handlerOffset = il.Offset;

            //   ldstr "world"
            il.LoadString(metadata.GetUserStringToken("world"));

            //   call void [mscorlib]System.Console::WriteLine(string)
            il.Call(consoleWriteLineMemberRef);

            // .endfinally
            il.OpCode(ILOpCode.Endfinally);

            // END: 
            int handlerEnd = il.Offset;
            new BlobWriter(end).WriteByte((byte)(handlerEnd - leaveOffset));

            // ret
            il.OpCode(ILOpCode.Ret);

            var body = methodBodies.AddMethodBody(exceptionRegionCount: 1);
            var eh = body.WriteInstructions(buffer, out mainBodyOffset);
            eh.StartRegions();
            eh.AddFinally(tryOffset, handlerOffset - tryOffset, handlerOffset, handlerEnd - handlerOffset);
            eh.EndRegions();

            buffer.Clear();
            
            mainMethodDef = metadata.AddMethodDefinition(
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                metadata.GetString("Main"),
                metadata.GetBlob(mainSignature.Builder),
                mainBodyOffset,
                paramList: default(ParameterHandle));

            var ctorDef = metadata.AddMethodDefinition(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                metadata.GetString(".ctor"),
                parameterlessCtorBlobIndex,
                ctorBodyOffset,
                paramList: default(ParameterHandle));

            metadata.AddTypeDefinition(
                default(TypeAttributes),
                default(StringHandle),
                metadata.GetString("<Module>"),
                baseType: default(EntityHandle),
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: MetadataTokens.MethodDefinitionHandle(1));

            metadata.AddTypeDefinition(
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit,
                metadata.GetString("ConsoleApplication"),
                metadata.GetString("Program"),
                systemObjectTypeRef,
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: mainMethodDef);
        }
    }
}
