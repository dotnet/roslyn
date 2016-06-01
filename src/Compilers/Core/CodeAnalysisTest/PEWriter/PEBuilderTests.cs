// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Xunit;
using Microsoft.CodeAnalysis.CodeGen;
using ILOpCode = Microsoft.CodeAnalysis.CodeGen.ILOpCode;

namespace Microsoft.CodeAnalysis.UnitTests
{
    using Roslyn.Reflection;
    using Roslyn.Reflection.Metadata;
    using Roslyn.Reflection.Metadata.Ecma335;
    using Roslyn.Reflection.PortableExecutable;
    using Roslyn.Reflection.Metadata.Ecma335.Blobs;

    internal static class BlobEncoderExtensions
    {
        public static void Parameters(this MethodSignatureEncoder encoder, int parameterCount, Action<ReturnTypeEncoder> returnType, Action<ParametersEncoder> parameters)
        {
            ReturnTypeEncoder returnTypeEncoder;
            ParametersEncoder parametersEncoder;
            encoder.Parameters(parameterCount, out returnTypeEncoder, out parametersEncoder);
            returnType(returnTypeEncoder);
            parameters(parametersEncoder);
        }
    }

    public class PEBuilderTests
    {
        private static void WritePEImage(Stream peStream, MetadataBuilder metadataBuilder, BlobBuilder ilBuilder, MethodDefinitionHandle entryPointHandle)
        {
            var mappedFieldDataBuilder = new BlobBuilder();
            var managedResourceDataBuilder = new BlobBuilder();

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
                imageCharacteristics: entryPointHandle.IsNil ? Characteristics.Dll : Characteristics.ExecutableImage,
                sizeOfStackReserve: 0x00100000,
                sizeOfStackCommit: 0x1000,
                sizeOfHeapReserve: 0x00100000,
                sizeOfHeapCommit: 0x1000);

            var peDirectoriesBuilder = new PEDirectoriesBuilder();

            peBuilder.AddManagedSections(
                peDirectoriesBuilder,
                new TypeSystemMetadataSerializer(metadataBuilder, "v4.0.30319", isMinimalDelta: false),
                ilBuilder,
                mappedFieldDataBuilder,
                managedResourceDataBuilder,
                nativeResourceSectionSerializer: null,
                strongNameSignatureSize: 0,
                entryPoint: entryPointHandle,
                pdbPathOpt: null,
                nativePdbContentId: default(ContentId),
                portablePdbContentId: default(ContentId),
                corFlags: CorFlags.ILOnly);

            var peBlob = new BlobBuilder();
            ContentId peContentId;
            peBuilder.Serialize(peBlob, peDirectoriesBuilder, out peContentId);

            peBlob.WriteContentTo(peStream);
        }

        [Fact]
        public void BasicValidation()
        {
            using (var peStream = new MemoryStream())
            {
                var ilBuilder = new BlobBuilder();
                var metadataBuilder = new MetadataBuilder();
                var entryPoint = BasicValidationEmit(metadataBuilder, ilBuilder);
                WritePEImage(peStream, metadataBuilder, ilBuilder, entryPoint);

                peStream.Position = 0;
                var r = new PEReader(peStream);
                var h = r.PEHeaders;
                var mdReader = r.GetMetadataReader();
            }
        }

        private static MethodDefinitionHandle BasicValidationEmit(MetadataBuilder metadata, BlobBuilder ilBuilder)
        {
            metadata.AddModule(
                0, 
                metadata.GetOrAddString("ConsoleApplication.exe"), 
                metadata.GetOrAddGuid(Guid.NewGuid()),
                default(GuidHandle), 
                default(GuidHandle));

            metadata.AddAssembly(
                metadata.GetOrAddString("ConsoleApplication"),
                version: new Version(0, 0, 0, 0),
                culture: default(StringHandle),
                publicKey: default(BlobHandle),
                flags: default(AssemblyFlags),
                hashAlgorithm: AssemblyHashAlgorithm.Sha1);

            var mscorlibAssemblyRef = metadata.AddAssemblyReference(
                name: metadata.GetOrAddString("mscorlib"),
                version: new Version(4, 0, 0, 0),
                culture: default(StringHandle),
                publicKeyOrToken: metadata.GetOrAddBlob(ImmutableArray.Create<byte>(0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89)),
                flags: default(AssemblyFlags),
                hashValue: default(BlobHandle));

            var systemObjectTypeRef = metadata.AddTypeReference(
                mscorlibAssemblyRef,
                metadata.GetOrAddString("System"),
                metadata.GetOrAddString("Object"));

            var systemConsoleTypeRefHandle = metadata.AddTypeReference(
                mscorlibAssemblyRef,
                metadata.GetOrAddString("System"),
                metadata.GetOrAddString("Console"));

            var consoleWriteLineSignature = new BlobBuilder();

            new BlobEncoder(consoleWriteLineSignature).
                MethodSignature().
                Parameters(1,
                    returnType => returnType.Void(),
                    parameters =>
                    {
                        parameters.AddParameter().Type().String();
                        parameters.EndParameters();
                    });

            var consoleWriteLineMemberRef = metadata.AddMemberReference(
                systemConsoleTypeRefHandle,
                metadata.GetOrAddString("WriteLine"),
                metadata.GetOrAddBlob(consoleWriteLineSignature));

            var parameterlessCtorSignature = new BlobBuilder();

            new BlobEncoder(parameterlessCtorSignature).
                MethodSignature(isInstanceMethod: true).
                Parameters(0, returnType => returnType.Void(), parameters => parameters.EndParameters());

            var parameterlessCtorBlobIndex = metadata.GetOrAddBlob(parameterlessCtorSignature);

            var objectCtorMemberRef = metadata.AddMemberReference(
                systemObjectTypeRef,
                metadata.GetOrAddString(".ctor"),
                parameterlessCtorBlobIndex);

            var mainSignature = new BlobBuilder();

            new BlobEncoder(mainSignature).
                MethodSignature().
                Parameters(0, returnType => returnType.Void(), parameters => parameters.EndParameters());

            var methodBodies = new MethodBodiesEncoder(ilBuilder);

            var codeBuilder = new BlobBuilder();
            var branchBuilder = new BranchBuilder();
            InstructionEncoder il;

            //
            // Program::.ctor
            //
            int ctorBodyOffset;
            il = new InstructionEncoder(codeBuilder);

            // ldarg.0
            il.LoadArgument(0);

            // call instance void [mscorlib]System.Object::.ctor()
            il.Call(objectCtorMemberRef);

            // ret
            il.OpCode(ILOpCode.Ret);

            methodBodies.AddMethodBody().WriteInstructions(codeBuilder, out ctorBodyOffset);
            codeBuilder.Clear();

            //
            // Program::Main
            //
            int mainBodyOffset;
            il = new InstructionEncoder(codeBuilder, branchBuilder);
            var endLabel = il.DefineLabel();

            // .try
            int tryOffset = il.Offset;

            //   ldstr "hello"
            il.LoadString(metadata.GetOrAddUserString("hello"));

            //   call void [mscorlib]System.Console::WriteLine(string)
            il.Call(consoleWriteLineMemberRef);

            //   leave.s END
            il.Branch(ILOpCode.Leave, endLabel);
            
            // .finally
            int handlerOffset = il.Offset;

            //   ldstr "world"
            il.LoadString(metadata.GetOrAddUserString("world"));

            //   call void [mscorlib]System.Console::WriteLine(string)
            il.Call(consoleWriteLineMemberRef);

            // .endfinally
            il.OpCode(ILOpCode.Endfinally);
            int handlerEnd = il.Offset;

            // END: 
            il.MarkLabel(endLabel);

            // ret
            il.OpCode(ILOpCode.Ret);

            var body = methodBodies.AddMethodBody(exceptionRegionCount: 1);
            var eh = body.WriteInstructions(codeBuilder, branchBuilder, out mainBodyOffset);
            eh.StartRegions();
            eh.AddFinally(tryOffset, handlerOffset - tryOffset, handlerOffset, handlerEnd - handlerOffset);
            eh.EndRegions();

            codeBuilder.Clear();
            branchBuilder.Clear();

            var mainMethodDef = metadata.AddMethodDefinition(
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                metadata.GetOrAddString("Main"),
                metadata.GetOrAddBlob(mainSignature),
                mainBodyOffset,
                paramList: default(ParameterHandle));

            var ctorDef = metadata.AddMethodDefinition(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                metadata.GetOrAddString(".ctor"),
                parameterlessCtorBlobIndex,
                ctorBodyOffset,
                paramList: default(ParameterHandle));

            metadata.AddTypeDefinition(
                default(TypeAttributes),
                default(StringHandle),
                metadata.GetOrAddString("<Module>"),
                baseType: default(EntityHandle),
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: mainMethodDef);

            metadata.AddTypeDefinition(
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit,
                metadata.GetOrAddString("ConsoleApplication"),
                metadata.GetOrAddString("Program"),
                systemObjectTypeRef,
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: mainMethodDef);
           
            return mainMethodDef;
        }

        [Fact]
        public void Complex()
        {
            using (var peStream = new MemoryStream())
            {
                var ilBuilder = new BlobBuilder();
                var metadataBuilder = new MetadataBuilder();
                var entryPoint = ComplexEmit(metadataBuilder, ilBuilder);

                WritePEImage(peStream, metadataBuilder, ilBuilder, entryPoint);

                // peStream.Position = 0;
                // File.WriteAllBytes(@"c:\temp\test.dll", peStream.ToArray());
            }
        }

        private static BlobBuilder BuildSignature(Action<BlobEncoder> action)
        {
            var builder = new BlobBuilder();
            action(new BlobEncoder(builder));
            return builder;
        }

        private static MethodDefinitionHandle ComplexEmit(MetadataBuilder metadata, BlobBuilder ilBuilder)
        {
            metadata.AddModule(
                0,
                metadata.GetOrAddString("ConsoleApplication.exe"),
                metadata.GetOrAddGuid(Guid.NewGuid()),
                default(GuidHandle),
                default(GuidHandle));

            metadata.AddAssembly(
                metadata.GetOrAddString("ConsoleApplication"),
                version: new Version(0, 0, 0, 0),
                culture: default(StringHandle),
                publicKey: default(BlobHandle),
                flags: default(AssemblyFlags),
                hashAlgorithm: AssemblyHashAlgorithm.Sha1);

            var mscorlibAssemblyRef = metadata.AddAssemblyReference(
                name: metadata.GetOrAddString("mscorlib"),
                version: new Version(4, 0, 0, 0),
                culture: default(StringHandle),
                publicKeyOrToken: metadata.GetOrAddBlob(ImmutableArray.Create<byte>(0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89)),
                flags: default(AssemblyFlags),
                hashValue: default(BlobHandle));

            // TypeRefs:

            var systemObjectTypeRef = metadata.AddTypeReference(mscorlibAssemblyRef, metadata.GetOrAddString("System"), metadata.GetOrAddString("Object"));
            var dictionaryTypeRef = metadata.AddTypeReference(mscorlibAssemblyRef, metadata.GetOrAddString("System.Collections.Generic"), metadata.GetOrAddString("Dictionary`2"));
            var strignBuilderTypeRef = metadata.AddTypeReference(mscorlibAssemblyRef, metadata.GetOrAddString("System.Text"), metadata.GetOrAddString("StringBuilder"));
            var typeTypeRef = metadata.AddTypeReference(mscorlibAssemblyRef, metadata.GetOrAddString("System"), metadata.GetOrAddString("Type"));
            var int32TypeRef = metadata.AddTypeReference(mscorlibAssemblyRef, metadata.GetOrAddString("System"), metadata.GetOrAddString("Int32"));
            var runtimeTypeHandleRef = metadata.AddTypeReference(mscorlibAssemblyRef, metadata.GetOrAddString("System"), metadata.GetOrAddString("RuntimeTypeHandle"));
            var invalidOperationExceptionTypeRef = metadata.AddTypeReference(mscorlibAssemblyRef, metadata.GetOrAddString("System"), metadata.GetOrAddString("InvalidOperationException"));

            // TypeDefs:

            metadata.AddTypeDefinition(
               default(TypeAttributes),
               default(StringHandle),
               metadata.GetOrAddString("<Module>"),
               baseType: default(EntityHandle),
               fieldList: MetadataTokens.FieldDefinitionHandle(1),
               methodList: MetadataTokens.MethodDefinitionHandle(1));

            var baseClassTypeDef = metadata.AddTypeDefinition(
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit | TypeAttributes.Abstract,
                metadata.GetOrAddString("Lib"),
                metadata.GetOrAddString("BaseClass"),
                systemObjectTypeRef,
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: MetadataTokens.MethodDefinitionHandle(1));

            var derivedClassTypeDef = metadata.AddTypeDefinition(
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit,
                metadata.GetOrAddString("Lib"),
                metadata.GetOrAddString("DerivedClass"),
                baseClassTypeDef,
                fieldList: MetadataTokens.FieldDefinitionHandle(4),
                methodList: MetadataTokens.MethodDefinitionHandle(1));

            // FieldDefs:

            // Field1
            var baseClassNumberFieldDef = metadata.AddFieldDefinition(
                FieldAttributes.Private,
                metadata.GetOrAddString("_number"),
                metadata.GetOrAddBlob(BuildSignature(e => e.FieldSignature().Int32())));

            // Field2
            var baseClassNegativeFieldDef = metadata.AddFieldDefinition(
                FieldAttributes.Assembly,
                metadata.GetOrAddString("negative"),
                metadata.GetOrAddBlob(BuildSignature(e => e.FieldSignature().Boolean())));

            // Field3
            var derivedClassSumCacheFieldDef = metadata.AddFieldDefinition(
                FieldAttributes.Assembly,
                metadata.GetOrAddString("_sumCache"),
                metadata.GetOrAddBlob(BuildSignature(e => 
                {
                    var inst = e.FieldSignature().GenericInstantiation(isValueType: false, typeRefDefSpec: dictionaryTypeRef, genericArgumentCount: 2);
                    inst.AddArgument().Int32();
                    inst.AddArgument().Object();
                })));

            // Field4
            var derivedClassCountFieldDef = metadata.AddFieldDefinition(
                FieldAttributes.Assembly,
                metadata.GetOrAddString("_count"),
                metadata.GetOrAddBlob(BuildSignature(e => e.FieldSignature().SZArray().Int32())));

            // Field5
            var derivedClassBCFieldDef = metadata.AddFieldDefinition(
              FieldAttributes.Assembly,
              metadata.GetOrAddString("_bc"),
              metadata.GetOrAddBlob(BuildSignature(e => e.FieldSignature().TypeDefOrRefOrSpec(isValueType: false, typeRefDefSpec: baseClassTypeDef))));

            var methodBodies = new MethodBodiesEncoder(ilBuilder);

            var buffer = new BlobBuilder();
            InstructionEncoder il;

            //
            // Foo
            //
            int fooBodyOffset;
            il = new InstructionEncoder(buffer);

            il.LoadString(metadata.GetOrAddUserString("asdsad"));
            il.OpCode(ILOpCode.Newobj);
            il.Token(invalidOperationExceptionTypeRef);
            il.OpCode(ILOpCode.Throw);

            methodBodies.AddMethodBody().WriteInstructions(buffer, out fooBodyOffset);
            buffer.Clear();

            // Method1
            var derivedClassFooMethodDef = metadata.AddMethodDefinition(
                MethodAttributes.PrivateScope | MethodAttributes.Private | MethodAttributes.HideBySig,
                MethodImplAttributes.IL,
                metadata.GetOrAddString("Foo"),
                metadata.GetOrAddBlob(BuildSignature(e =>
                    e.MethodSignature(isInstanceMethod: true).Parameters(0, returnType => returnType.Void(), parameters => parameters.EndParameters()))),
                fooBodyOffset,
                default(ParameterHandle));

            return default(MethodDefinitionHandle);
        }
    }
}
