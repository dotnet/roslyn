// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Reflection;
using System.Runtime.InteropServices;
#if NET
using System.Runtime.InteropServices.Marshalling;
#endif

namespace Microsoft.DiaSymReader
{
    [ComVisible(false)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("7DAC8207-D3AE-4c75-9B67-92801A497D44")]
#if NET
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
#else
    [ComImport]
#endif
    internal unsafe partial interface IMetadataImport
    {
        [PreserveSig]
        void CloseEnum(void* enumHandle);

        [PreserveSig]
        int CountEnum(void* enumHandle, out int count);

        [PreserveSig]
        int ResetEnum(void* enumHandle, int position);

        [PreserveSig]
        int EnumTypeDefs(
            ref void* enumHandle,
            int* typeDefs,
            int bufferLength,
            int* count);

        [PreserveSig]
        int EnumInterfaceImpls(
            ref void* enumHandle,
            int typeDef,
            int* interfaceImpls,
            int bufferLength,
            int* count);

        [PreserveSig]
        int EnumTypeRefs(
            ref void* enumHandle,
            int* typeRefs,
            int bufferLength,
            int* count);

        [PreserveSig]
        int FindTypeDefByName(
            string name,
            int enclosingClass,
            out int typeDef);

        [PreserveSig]
        int GetScopeProps(
            char* name,
            int bufferLength,
            int* nameLength,
            Guid* mvid);

        [PreserveSig]
        int GetModuleFromScope(
            out int moduleDef);

        [PreserveSig]
        int GetTypeDefProps(
            int typeDef,
            char* qualifiedName,
            int qualifiedNameBufferLength,
            int* qualifiedNameLength,
            TypeAttributes* attributes,
            int* baseType);

        [PreserveSig]
        int GetInterfaceImplProps(
            int interfaceImpl,
            int* typeDef,
            int* interfaceDefRefSpec);

        [PreserveSig]
        int GetTypeRefProps(
            int typeRef,
            int* resolutionScope, // ModuleRef or AssemblyRef
            char* qualifiedName,
            int qualifiedNameBufferLength,
            int* qualifiedNameLength);

        [PreserveSig]
        int ResolveTypeRef(
            int typeRef,
            ref Guid scopeInterfaceId,
            [MarshalAs(UnmanagedType.Interface)] out object scope,
            out int typeDef);

        [PreserveSig]
        int EnumMembers(
            ref void* enumHandle,
            int typeDef,
            int* memberDefs,
            int bufferLength,
            int* count);

        [PreserveSig]
        int EnumMembersWithName(
            ref void* enumHandle,
            int typeDef,
            string name,
            int* memberDefs,
            int bufferLength,
            int* count);

        [PreserveSig]
        int EnumMethods(
            ref void* enumHandle,
            int typeDef,
            int* methodDefs,
            int bufferLength,
            int* count);

        [PreserveSig]
        int EnumMethodsWithName(
            ref void* enumHandle,
            int typeDef,
            string name,
            int* methodDefs,
            int bufferLength,
            int* count);

        [PreserveSig]
        int EnumFields(
            ref void* enumHandle,
            int typeDef,
            int* fieldDefs,
            int bufferLength,
            int* count);

        [PreserveSig]
        int EnumFieldsWithName(
            ref void* enumHandle,
            int typeDef,
            string name,
            int* fieldDefs,
            int bufferLength,
            int* count);

        [PreserveSig]
        int EnumParams(
            ref void* enumHandle,
            int methodDef,
            int* paramDefs,
            int bufferLength,
            int* count);

        [PreserveSig]
        int EnumMemberRefs(
            ref void* enumHandle,
            int parentToken,
            int* memberRefs,
            int bufferLength,
            int* count);

        [PreserveSig]
        int EnumMethodImpls(
            ref void* enumHandle,
            int typeDef,
            int* implementationTokens,
            int* declarationTokens,
            int bufferLength,
            int* count);

        [PreserveSig]
        int EnumPermissionSets(
            ref void* enumHandle,
            int token,
            uint action,
            int* declSecurityTokens,
            int bufferLength,
            int* count);

        [PreserveSig]
        int FindMember(
            int typeDef,
            string name,
            byte* signature,
            int signatureLength,
            out int memberDef);

        [PreserveSig]
        int FindMethod(
            int typeDef,
            string name,
            byte* signature,
            int signatureLength,
            out int methodDef);

        [PreserveSig]
        int FindField(
            int typeDef,
            string name,
            byte* signature,
            int signatureLength,
            out int fieldDef);

        [PreserveSig]
        int FindMemberRef(
            int typeDef,
            string name,
            byte* signature,
            int signatureLength,
            out int memberRef);

        [PreserveSig]
        int GetMethodProps(
           int methodDef,
           int* declaringTypeDef,
           char* name,
           int nameBufferLength,
           int* nameLength,
           MethodAttributes* attributes,
           byte** signature,
           int* signatureLength,
           int* relativeVirtualAddress,
           MethodImplAttributes* implAttributes);

        [PreserveSig]
        int GetMemberRefProps(
            int memberRef,
            int* declaringType,
            char* name,
            int nameBufferLength,
            int* nameLength,
            byte** signature,
            int* signatureLength);

        [PreserveSig]
        int EnumProperties(
           ref void* enumHandle,
           int typeDef,
           int* properties,
           int bufferLength,
           int* count);

        [PreserveSig]
        uint EnumEvents(
           ref void* enumHandle,
           int typeDef,
           int* events,
           int bufferLength,
           int* count);

        [PreserveSig]
        int GetEventProps(
            int @event,
            int* declaringTypeDef,
            char* name,
            int nameBufferLength,
            int* nameLength,
            int* attributes,
            int* eventType,
            int* adderMethodDef,
            int* removerMethodDef,
            int* raiserMethodDef,
            int* otherMethodDefs,
            int otherMethodDefBufferLength,
            int* methodMethodDefsLength);

        [PreserveSig]
        int EnumMethodSemantics(
            ref void* enumHandle,
            int methodDef,
            int* eventsAndProperties,
            int bufferLength,
            int* count);

        [PreserveSig]
        int GetMethodSemantics(
            int methodDef,
            int eventOrProperty,
            int* semantics);

        [PreserveSig]
        int GetClassLayout(
            int typeDef,
            int* packSize,
            MetadataImportFieldOffset* fieldOffsets,
            int bufferLength,
            int* count,
            int* typeSize);

        [PreserveSig]
        int GetFieldMarshal(
            int fieldDef,
            byte** nativeTypeSignature,
            int* nativeTypeSignatureLength);

        [PreserveSig]
        int GetRVA(
            int methodDef,
            int* relativeVirtualAddress,
            int* implAttributes);

        [PreserveSig]
        int GetPermissionSetProps(
            int declSecurity,
            uint* action,
            byte** permissionBlob,
            int* permissionBlobLength);

        [PreserveSig]
        int GetSigFromToken(
            int standaloneSignature,
            byte** signature,
            int* signatureLength);

        [PreserveSig]
        int GetModuleRefProps(
            int moduleRef,
            char* name,
            int nameBufferLength,
            int* nameLength);

        [PreserveSig]
        int EnumModuleRefs(
            ref void* enumHandle,
            int* moduleRefs,
            int bufferLength,
            int* count);

        [PreserveSig]
        int GetTypeSpecFromToken(
            int typeSpec,
            byte** signature,
            int* signatureLength);

        [PreserveSig]
        int GetNameFromToken(
            int token,
            byte* nameUTF8);

        [PreserveSig]
        int EnumUnresolvedMethods(
            ref void* enumHandle,
            int* methodDefs,
            int bufferLength,
            int* count);

        [PreserveSig]
        int GetUserString(
            int userStringToken,
            char* buffer,
            int bufferLength,
            int* length);

        [PreserveSig]
        int GetPinvokeMap(
            int memberDef,
            int* attributes,
            char* importName,
            int importNameBufferLength,
            int* importNameLength,
            int* moduleRef);

        [PreserveSig]
        int EnumSignatures(
            ref void* enumHandle,
            int* signatureTokens,
            int bufferLength,
            int* count);

        [PreserveSig]
        int EnumTypeSpecs(
            ref void* enumHandle,
            int* typeSpecs,
            int bufferLength,
            int* count);

        [PreserveSig]
        int EnumUserStrings(
            ref void* enumHandle,
            int* userStrings,
            int bufferLength,
            int* count);

        [PreserveSig]
        int GetParamForMethodIndex(
            int methodDef,
            int sequenceNumber,
            out int parameterToken);

        [PreserveSig]
        int EnumCustomAttributes(
            ref void* enumHandle,
            int parent,
            int attributeType,
            int* customAttributes,
            int bufferLength,
            int* count);

        [PreserveSig]
        int GetCustomAttributeProps(
            int customAttribute,
            int* parent,
            int* constructor,
            byte** value,
            int* valueLength);

        [PreserveSig]
        int FindTypeRef(
            int resolutionScope,
            string name,
            out int typeRef);

        [PreserveSig]
        int GetMemberProps(
            int member,
            int* declaringTypeDef,
            char* name,
            int nameBufferLength,
            int* nameLength,
            int* attributes,
            byte** signature,
            int* signatureLength,
            int* relativeVirtualAddress,
            int* implAttributes,
            int* constantType,
            byte** constantValue,
            int* constantValueLength);

        [PreserveSig]
        int GetFieldProps(
            int fieldDef,
            int* declaringTypeDef,
            char* name,
            int nameBufferLength,
            int* nameLength,
            int* attributes,
            byte** signature,
            int* signatureLength,
            int* constantType,
            byte** constantValue,
            int* constantValueLength);

        [PreserveSig]
        int GetPropertyProps(
            int propertyDef,
            int* declaringTypeDef,
            char* name,
            int nameBufferLength,
            int* nameLength,
            int* attributes,
            byte** signature,
            int* signatureLength,
            int* constantType,
            byte** constantValue,
            int* constantValueLength,
            int* setterMethodDef,
            int* getterMethodDef,
            int* outerMethodDefs,
            int outerMethodDefsBufferLength,
            int* otherMethodDefCount);

        [PreserveSig]
        int GetParamProps(
            int parameter,
            int* declaringMethodDef,
            int* sequenceNumber,
            char* name,
            int nameBufferLength,
            int* nameLength,
            int* attributes,
            int* constantType,
            byte** constantValue,
            int* constantValueLength);

        [PreserveSig]
        int GetCustomAttributeByName(
            int parent,
            string name,
            byte** value,
            int* valueLength);

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Bool)]
        bool IsValidToken(int token);

        [PreserveSig]
        int GetNestedClassProps(
            int nestedClass,
            out int enclosingClass);

        [PreserveSig]
        int GetNativeCallConvFromSig(
            byte* signature,
            int signatureLength,
            int* callingConvention);

        [PreserveSig]
        int IsGlobal(
            int token,
            [MarshalAs(UnmanagedType.Bool)] bool value);
    }
}
