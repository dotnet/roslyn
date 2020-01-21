// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Microsoft.DiaSymReader
{
    internal unsafe class MetadataAdapterBase : IMetadataImport, IMetadataEmit
    {
        public virtual int GetTokenFromSig(byte* voidPointerSig, int byteCountSig)
            => throw new NotImplementedException();

        public virtual int GetSigFromToken(
            int standaloneSignature,
            [Out]byte** signature,
            [Out]int* signatureLength)
            => throw new NotImplementedException();

        public virtual int GetTypeDefProps(
            int typeDef,
            [Out]char* qualifiedName,
            int qualifiedNameBufferLength,
            [Out]int* qualifiedNameLength,
            [Out]TypeAttributes* attributes,
            [Out]int* baseType)
            => throw new NotImplementedException();

        public virtual int GetTypeRefProps(
            int typeRef,
            [Out]int* resolutionScope, // ModuleRef or AssemblyRef
            [Out]char* qualifiedName,
            int qualifiedNameBufferLength,
            [Out]int* qualifiedNameLength)
            => throw new NotImplementedException();

        public virtual int GetNestedClassProps(int nestedClass, out int enclosingClass)
            => throw new NotImplementedException();

        public virtual int GetMethodProps(
            int methodDef,
            [Out] int* declaringTypeDef,
            [Out] char* name,
            int nameBufferLength,
            [Out] int* nameLength,
            [Out] MethodAttributes* attributes,
            [Out] byte** signature,
            [Out] int* signatureLength,
            [Out] int* relativeVirtualAddress,
            [Out] MethodImplAttributes* implAttributes)
            => throw new NotImplementedException();

        void IMetadataImport.CloseEnum(void* enumHandle) => throw new NotImplementedException();
        int IMetadataImport.CountEnum(void* enumHandle, out int count) => throw new NotImplementedException();
        int IMetadataImport.ResetEnum(void* enumHandle, int position) => throw new NotImplementedException();
        int IMetadataImport.EnumTypeDefs(ref void* enumHandle, int* typeDefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumInterfaceImpls(ref void* enumHandle, int typeDef, int* interfaceImpls, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumTypeRefs(ref void* enumHandle, int* typeRefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.FindTypeDefByName(string name, int enclosingClass, out int typeDef) => throw new NotImplementedException();
        int IMetadataImport.GetScopeProps(char* name, int bufferLength, int* nameLength, Guid* mvid) => throw new NotImplementedException();
        int IMetadataImport.GetModuleFromScope(out int moduleDef) => throw new NotImplementedException();
        int IMetadataImport.GetInterfaceImplProps(int interfaceImpl, int* typeDef, int* interfaceDefRefSpec) => throw new NotImplementedException();
        int IMetadataImport.ResolveTypeRef(int typeRef, ref Guid scopeInterfaceId, out object scope, out int typeDef) => throw new NotImplementedException();
        int IMetadataImport.EnumMembers(ref void* enumHandle, int typeDef, int* memberDefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumMembersWithName(ref void* enumHandle, int typeDef, string name, int* memberDefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumMethods(ref void* enumHandle, int typeDef, int* methodDefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumMethodsWithName(ref void* enumHandle, int typeDef, string name, int* methodDefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumFields(ref void* enumHandle, int typeDef, int* fieldDefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumFieldsWithName(ref void* enumHandle, int typeDef, string name, int* fieldDefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumParams(ref void* enumHandle, int methodDef, int* paramDefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumMemberRefs(ref void* enumHandle, int parentToken, int* memberRefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumMethodImpls(ref void* enumHandle, int typeDef, int* implementationTokens, int* declarationTokens, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumPermissionSets(ref void* enumHandle, int token, uint action, int* declSecurityTokens, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.FindMember(int typeDef, string name, byte* signature, int signatureLength, out int memberDef) => throw new NotImplementedException();
        int IMetadataImport.FindMethod(int typeDef, string name, byte* signature, int signatureLength, out int methodDef) => throw new NotImplementedException();
        int IMetadataImport.FindField(int typeDef, string name, byte* signature, int signatureLength, out int fieldDef) => throw new NotImplementedException();
        int IMetadataImport.FindMemberRef(int typeDef, string name, byte* signature, int signatureLength, out int memberRef) => throw new NotImplementedException();
        int IMetadataImport.GetMemberRefProps(int memberRef, int* declaringType, char* name, int nameBufferLength, int* nameLength, byte** signature, int* signatureLength) => throw new NotImplementedException();
        int IMetadataImport.EnumProperties(ref void* enumHandle, int typeDef, int* properties, int bufferLength, int* count) => throw new NotImplementedException();
        uint IMetadataImport.EnumEvents(ref void* enumHandle, int typeDef, int* events, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.GetEventProps(int @event, int* declaringTypeDef, char* name, int nameBufferLength, int* nameLength, int* attributes, int* eventType, int* adderMethodDef, int* removerMethodDef, int* raiserMethodDef, int* otherMethodDefs, int otherMethodDefBufferLength, int* methodMethodDefsLength) => throw new NotImplementedException();
        int IMetadataImport.EnumMethodSemantics(ref void* enumHandle, int methodDef, int* eventsAndProperties, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.GetMethodSemantics(int methodDef, int eventOrProperty, int* semantics) => throw new NotImplementedException();
        int IMetadataImport.GetClassLayout(int typeDef, int* packSize, MetadataImportFieldOffset* fieldOffsets, int bufferLength, int* count, int* typeSize) => throw new NotImplementedException();
        int IMetadataImport.GetFieldMarshal(int fieldDef, byte** nativeTypeSignature, int* nativeTypeSignatureLengvth) => throw new NotImplementedException();
        int IMetadataImport.GetRVA(int methodDef, int* relativeVirtualAddress, int* implAttributes) => throw new NotImplementedException();
        int IMetadataImport.GetPermissionSetProps(int declSecurity, uint* action, byte** permissionBlob, int* permissionBlobLength) => throw new NotImplementedException();
        int IMetadataImport.GetModuleRefProps(int moduleRef, char* name, int nameBufferLength, int* nameLength) => throw new NotImplementedException();
        int IMetadataImport.EnumModuleRefs(ref void* enumHandle, int* moduleRefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.GetTypeSpecFromToken(int typeSpec, byte** signature, int* signatureLength) => throw new NotImplementedException();
        int IMetadataImport.GetNameFromToken(int token, byte* nameUTF8) => throw new NotImplementedException();
        int IMetadataImport.EnumUnresolvedMethods(ref void* enumHandle, int* methodDefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.GetUserString(int userStringToken, char* buffer, int bufferLength, int* length) => throw new NotImplementedException();
        int IMetadataImport.GetPinvokeMap(int memberDef, int* attributes, char* importName, int importNameBufferLength, int* importNameLength, int* moduleRef) => throw new NotImplementedException();
        int IMetadataImport.EnumSignatures(ref void* enumHandle, int* signatureTokens, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumTypeSpecs(ref void* enumHandle, int* typeSpecs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumUserStrings(ref void* enumHandle, int* userStrings, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.GetParamForMethodIndex(int methodDef, int sequenceNumber, out int parameterToken) => throw new NotImplementedException();
        int IMetadataImport.EnumCustomAttributes(ref void* enumHandle, int parent, int attributeType, int* customAttributes, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.GetCustomAttributeProps(int customAttribute, int* parent, int* constructor, byte** value, int* valueLength) => throw new NotImplementedException();
        int IMetadataImport.FindTypeRef(int resolutionScope, string name, out int typeRef) => throw new NotImplementedException();
        int IMetadataImport.GetMemberProps(int member, int* declaringTypeDef, char* name, int nameBufferLength, int* nameLength, int* attributes, byte** signature, int* signatureLength, int* relativeVirtualAddress, int* implAttributes, int* constantType, byte** constantValue, int* constantValueLength) => throw new NotImplementedException();
        int IMetadataImport.GetFieldProps(int fieldDef, int* declaringTypeDef, char* name, int nameBufferLength, int* nameLength, int* attributes, byte** signature, int* signatureLength, int* constantType, byte** constantValue, int* constantValueLength) => throw new NotImplementedException();
        int IMetadataImport.GetPropertyProps(int propertyDef, int* declaringTypeDef, char* name, int nameBufferLength, int* nameLength, int* attributes, byte** signature, int* signatureLength, int* constantType, byte** constantValue, int* constantValueLength, int* setterMethodDef, int* getterMethodDef, int* outerMethodDefs, int outerMethodDefsBufferLength, int* otherMethodDefCount) => throw new NotImplementedException();
        int IMetadataImport.GetParamProps(int parameter, int* declaringMethodDef, int* sequenceNumber, char* name, int nameBufferLength, int* nameLength, int* attributes, int* constantType, byte** constantValue, int* constantValueLength) => throw new NotImplementedException();
        int IMetadataImport.GetCustomAttributeByName(int parent, string name, byte** value, int* valueLength) => throw new NotImplementedException();
        bool IMetadataImport.IsValidToken(int token) => throw new NotImplementedException();
        int IMetadataImport.GetNativeCallConvFromSig(byte* signature, int signatureLength, int* callingConvention) => throw new NotImplementedException();
        int IMetadataImport.IsGlobal(int token, bool value) => throw new NotImplementedException();

        void IMetadataEmit.__SetModuleProps() => throw new NotImplementedException();
        void IMetadataEmit.__Save() => throw new NotImplementedException();
        void IMetadataEmit.__SaveToStream() => throw new NotImplementedException();
        void IMetadataEmit.__GetSaveSize() => throw new NotImplementedException();
        void IMetadataEmit.__DefineTypeDef() => throw new NotImplementedException();
        void IMetadataEmit.__DefineNestedType() => throw new NotImplementedException();
        void IMetadataEmit.__SetHandler() => throw new NotImplementedException();
        void IMetadataEmit.__DefineMethod() => throw new NotImplementedException();
        void IMetadataEmit.__DefineMethodImpl() => throw new NotImplementedException();
        void IMetadataEmit.__DefineTypeRefByName() => throw new NotImplementedException();
        void IMetadataEmit.__DefineImportType() => throw new NotImplementedException();
        void IMetadataEmit.__DefineMemberRef() => throw new NotImplementedException();
        void IMetadataEmit.__DefineImportMember() => throw new NotImplementedException();
        void IMetadataEmit.__DefineEvent() => throw new NotImplementedException();
        void IMetadataEmit.__SetClassLayout() => throw new NotImplementedException();
        void IMetadataEmit.__DeleteClassLayout() => throw new NotImplementedException();
        void IMetadataEmit.__SetFieldMarshal() => throw new NotImplementedException();
        void IMetadataEmit.__DeleteFieldMarshal() => throw new NotImplementedException();
        void IMetadataEmit.__DefinePermissionSet() => throw new NotImplementedException();
        void IMetadataEmit.__SetRVA() => throw new NotImplementedException();
        void IMetadataEmit.__DefineModuleRef() => throw new NotImplementedException();
        void IMetadataEmit.__SetParent() => throw new NotImplementedException();
        void IMetadataEmit.__GetTokenFromTypeSpec() => throw new NotImplementedException();
        void IMetadataEmit.__SaveToMemory() => throw new NotImplementedException();
        void IMetadataEmit.__DefineUserString() => throw new NotImplementedException();
        void IMetadataEmit.__DeleteToken() => throw new NotImplementedException();
        void IMetadataEmit.__SetMethodProps() => throw new NotImplementedException();
        void IMetadataEmit.__SetTypeDefProps() => throw new NotImplementedException();
        void IMetadataEmit.__SetEventProps() => throw new NotImplementedException();
        void IMetadataEmit.__SetPermissionSetProps() => throw new NotImplementedException();
        void IMetadataEmit.__DefinePinvokeMap() => throw new NotImplementedException();
        void IMetadataEmit.__SetPinvokeMap() => throw new NotImplementedException();
        void IMetadataEmit.__DeletePinvokeMap() => throw new NotImplementedException();
        void IMetadataEmit.__DefineCustomAttribute() => throw new NotImplementedException();
        void IMetadataEmit.__SetCustomAttributeValue() => throw new NotImplementedException();
        void IMetadataEmit.__DefineField() => throw new NotImplementedException();
        void IMetadataEmit.__DefineProperty() => throw new NotImplementedException();
        void IMetadataEmit.__DefineParam() => throw new NotImplementedException();
        void IMetadataEmit.__SetFieldProps() => throw new NotImplementedException();
        void IMetadataEmit.__SetPropertyProps() => throw new NotImplementedException();
        void IMetadataEmit.__SetParamProps() => throw new NotImplementedException();
        void IMetadataEmit.__DefineSecurityAttributeSet() => throw new NotImplementedException();
        void IMetadataEmit.__ApplyEditAndContinue() => throw new NotImplementedException();
        void IMetadataEmit.__TranslateSigWithScope() => throw new NotImplementedException();
        void IMetadataEmit.__SetMethodImplFlags() => throw new NotImplementedException();
        void IMetadataEmit.__SetFieldRVA() => throw new NotImplementedException();
        void IMetadataEmit.__Merge() => throw new NotImplementedException();
        void IMetadataEmit.__MergeEnd() => throw new NotImplementedException();
    }
}
