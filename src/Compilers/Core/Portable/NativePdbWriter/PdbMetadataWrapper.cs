// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable 436 // SuppressUnmanagedCodeSecurityAttribute defined in source and mscorlib 

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Microsoft.Cci
{
    internal struct COR_FIELD_OFFSET
    {
        public uint RidOfField;
        public uint UlOffset;

        // Only here to shut up the warning about fields never being assigned to.
        internal COR_FIELD_OFFSET(object dummy)
        {
            this.RidOfField = 0;
            this.UlOffset = 0;
        }
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("BA3FEE4C-ECB9-4e41-83B7-183FA41CD859"), SuppressUnmanagedCodeSecurity]
    internal unsafe interface IMetaDataEmit
    {
        void SetModuleProps(string stringName);
        void Save(string stringFile, uint dwordSaveFlags);
        void SaveToStream(void* pointerIStream, uint dwordSaveFlags);
        uint GetSaveSize(uint save);
        uint DefineTypeDef(char* stringTypeDef, uint dwordTypeDefFlags, uint tokenExtends, uint* rtkImplements);
        uint DefineNestedType(char* stringTypeDef, uint dwordTypeDefFlags, uint tokenExtends, uint* rtkImplements, uint typeDefEncloser);
        void SetHandler([MarshalAs(UnmanagedType.IUnknown), In] object pointerUnk);
        uint DefineMethod(uint td, char* name, uint dwordMethodFlags, byte* voidPointerSigBlob, uint byteCountSigBlob, uint ulongCodeRVA, uint dwordImplFlags);
        void DefineMethodImpl(uint td, uint tokenBody, uint tokenDecl);
        uint DefineTypeRefByName(uint tokenResolutionScope, char* stringName);
        uint DefineImportType(IntPtr pointerAssemImport, void* bytePointerHashValue, uint byteCountHashValue, IMetaDataImport pointerImport,
          uint typeDefImport, IntPtr pointerAssemEmit);
        uint DefineMemberRef(uint tokenImport, string stringName, byte* voidPointerSigBlob, uint byteCountSigBlob);
        uint DefineImportMember(IntPtr pointerAssemImport, void* bytePointerHashValue, uint byteCountHashValue,
          IMetaDataImport pointerImport, uint member, IntPtr pointerAssemEmit, uint tokenParent);
        uint DefineEvent(uint td, string stringEvent, uint dwordEventFlags, uint tokenEventType, uint memberDefAddOn, uint memberDefRemoveOn, uint memberDefFire, uint* rmdOtherMethods);
        void SetClassLayout(uint td, uint dwordPackSize, COR_FIELD_OFFSET* arrayFieldOffsets, uint ulongClassSize);
        void DeleteClassLayout(uint td);
        void SetFieldMarshal(uint tk, byte* voidPointerNativeType, uint byteCountNativeType);
        void DeleteFieldMarshal(uint tk);
        uint DefinePermissionSet(uint tk, uint dwordAction, void* voidPointerPermission, uint byteCountPermission);
        void SetRVA(uint md, uint ulongRVA);
        uint GetTokenFromSig(byte* voidPointerSig, uint byteCountSig);
        uint DefineModuleRef(string stringName);
        void SetParent(uint mr, uint tk);
        uint GetTokenFromTypeSpec(byte* voidPointerSig, uint byteCountSig);
        void SaveToMemory(void* bytePointerData, uint byteCountData);
        uint DefineUserString(string stringString, uint cchString);
        void DeleteToken(uint tokenObj);
        void SetMethodProps(uint md, uint dwordMethodFlags, uint ulongCodeRVA, uint dwordImplFlags);
        void SetTypeDefProps(uint td, uint dwordTypeDefFlags, uint tokenExtends, uint* rtkImplements);
        void SetEventProps(uint ev, uint dwordEventFlags, uint tokenEventType, uint memberDefAddOn, uint memberDefRemoveOn, uint memberDefFire, uint* rmdOtherMethods);
        uint SetPermissionSetProps(uint tk, uint dwordAction, void* voidPointerPermission, uint byteCountPermission);
        void DefinePinvokeMap(uint tk, uint dwordMappingFlags, string stringImportName, uint importDLL);
        void SetPinvokeMap(uint tk, uint dwordMappingFlags, string stringImportName, uint importDLL);
        void DeletePinvokeMap(uint tk);
        uint DefineCustomAttribute(uint tokenObj, uint tokenType, void* pointerCustomAttribute, uint byteCountCustomAttribute);
        void SetCustomAttributeValue(uint pcv, void* pointerCustomAttribute, uint byteCountCustomAttribute);
        uint DefineField(uint td, string stringName, uint dwordFieldFlags, byte* voidPointerSigBlob, uint byteCountSigBlob, uint dwordCPlusTypeFlag, void* pointerValue, uint cchValue);
        uint DefineProperty(uint td, string stringProperty, uint dwordPropFlags, byte* voidPointerSig, uint byteCountSig, uint dwordCPlusTypeFlag,
          void* pointerValue, uint cchValue, uint memberDefSetter, uint memberDefGetter, uint* rmdOtherMethods);
        uint DefineParam(uint md, uint ulongParamSeq, string stringName, uint dwordParamFlags, uint dwordCPlusTypeFlag, void* pointerValue, uint cchValue);
        void SetFieldProps(uint fd, uint dwordFieldFlags, uint dwordCPlusTypeFlag, void* pointerValue, uint cchValue);
        void SetPropertyProps(uint pr, uint dwordPropFlags, uint dwordCPlusTypeFlag, void* pointerValue, uint cchValue, uint memberDefSetter, uint memberDefGetter, uint* rmdOtherMethods);
        void SetParamProps(uint pd, string stringName, uint dwordParamFlags, uint dwordCPlusTypeFlag, void* pointerValue, uint cchValue);
        uint DefineSecurityAttributeSet(uint tokenObj, IntPtr arraySecAttrs, uint countSecAttrs);
        void ApplyEditAndContinue([MarshalAs(UnmanagedType.IUnknown)] object pointerImport);
        uint TranslateSigWithScope(IntPtr pointerAssemImport, void* bytePointerHashValue, uint byteCountHashValue,
          IMetaDataImport import, byte* bytePointerSigBlob, uint byteCountSigBlob, IntPtr pointerAssemEmit, IMetaDataEmit emit, byte* voidPointerTranslatedSig, uint byteCountTranslatedSigMax);
        void SetMethodImplFlags(uint md, uint dwordImplFlags);
        void SetFieldRVA(uint fd, uint ulongRVA);
        void Merge(IMetaDataImport pointerImport, IntPtr pointerHostMapToken, [MarshalAs(UnmanagedType.IUnknown)] object pointerHandler);
        void MergeEnd();
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("7DAC8207-D3AE-4c75-9B67-92801A497D44"), SuppressUnmanagedCodeSecurity]
    internal unsafe interface IMetaDataImport
    {
        [PreserveSig]
        void CloseEnum(uint handleEnum);
        uint CountEnum(uint handleEnum);
        void ResetEnum(uint handleEnum, uint ulongPos);
        uint EnumTypeDefs(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arrayTypeDefs, uint countMax);
        uint EnumInterfaceImpls(ref uint handlePointerEnum, uint td, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] arrayImpls, uint countMax);
        uint EnumTypeRefs(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arrayTypeRefs, uint countMax);
        uint FindTypeDefByName(string stringTypeDef, uint tokenEnclosingClass);
        Guid GetScopeProps(StringBuilder stringName, uint cchName, out uint pchName);
        uint GetModuleFromScope();
        uint GetTypeDefProps(uint td, IntPtr stringTypeDef, uint cchTypeDef, out uint pchTypeDef, IntPtr pdwTypeDefFlags);
        uint GetInterfaceImplProps(uint impl, out uint pointerClass);
        uint GetTypeRefProps(uint tr, out uint ptkResolutionScope, StringBuilder stringName, uint cchName);
        uint ResolveTypeRef(uint tr, [In] ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppIScope);
        uint EnumMembers(ref uint handlePointerEnum, uint cl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] arrayMembers, uint countMax);
        uint EnumMembersWithName(ref uint handlePointerEnum, uint cl, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayMembers, uint countMax);
        uint EnumMethods(ref uint handlePointerEnum, uint cl, uint* arrayMethods, uint countMax);
        uint EnumMethodsWithName(ref uint handlePointerEnum, uint cl, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayMethods, uint countMax);
        uint EnumFields(ref uint handlePointerEnum, uint cl, uint* arrayFields, uint countMax);
        uint EnumFieldsWithName(ref uint handlePointerEnum, uint cl, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayFields, uint countMax);
        uint EnumParams(ref uint handlePointerEnum, uint mb, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] arrayParams, uint countMax);
        uint EnumMemberRefs(ref uint handlePointerEnum, uint tokenParent, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] arrayMemberRefs, uint countMax);
        uint EnumMethodImpls(ref uint handlePointerEnum, uint td, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayMethodBody,
          [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayMethodDecl, uint countMax);
        uint EnumPermissionSets(ref uint handlePointerEnum, uint tk, uint dwordActions, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayPermission,
          uint countMax);
        uint FindMember(uint td, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] voidPointerSigBlob, uint byteCountSigBlob);
        uint FindMethod(uint td, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] voidPointerSigBlob, uint byteCountSigBlob);
        uint FindField(uint td, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] voidPointerSigBlob, uint byteCountSigBlob);
        uint FindMemberRef(uint td, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] voidPointerSigBlob, uint byteCountSigBlob);
        uint GetMethodProps(uint mb, out uint pointerClass, IntPtr stringMethod, uint cchMethod, out uint pchMethod, IntPtr pdwAttr,
          IntPtr ppvSigBlob, IntPtr pcbSigBlob, IntPtr pulCodeRVA);
        uint GetMemberRefProps(uint mr, ref uint ptk, StringBuilder stringMember, uint cchMember, out uint pchMember, out byte* ppvSigBlob);
        uint EnumProperties(ref uint handlePointerEnum, uint td, uint* arrayProperties, uint countMax);
        uint EnumEvents(ref uint handlePointerEnum, uint td, uint* arrayEvents, uint countMax);
        uint GetEventProps(uint ev, out uint pointerClass, StringBuilder stringEvent, uint cchEvent, out uint pchEvent, out uint pdwEventFlags,
          out uint ptkEventType, out uint pmdAddOn, out uint pmdRemoveOn, out uint pmdFire,
          [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 11)] uint[] rmdOtherMethod, uint countMax);
        uint EnumMethodSemantics(ref uint handlePointerEnum, uint mb, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] arrayEventProp, uint countMax);
        uint GetMethodSemantics(uint mb, uint tokenEventProp);
        uint GetClassLayout(uint td, out uint pdwPackSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] COR_FIELD_OFFSET[] arrayFieldOffset, uint countMax, out uint countPointerFieldOffset);
        uint GetFieldMarshal(uint tk, out byte* ppvNativeType);
        uint GetRVA(uint tk, out uint pulCodeRVA);
        uint GetPermissionSetProps(uint pm, out uint pdwAction, out void* ppvPermission);
        uint GetSigFromToken(uint memberDefSig, out IntPtr ppvSig, out uint pcbSig);
        uint GetModuleRefProps(uint mur, StringBuilder stringName, uint cchName);
        uint EnumModuleRefs(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arrayModuleRefs, uint cmax);
        uint GetTypeSpecFromToken(uint typespec, out byte* ppvSig);
        uint GetNameFromToken(uint tk);
        uint EnumUnresolvedMethods(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arrayMethods, uint countMax);
        uint GetUserString(uint stk, StringBuilder stringString, uint cchString);
        uint GetPinvokeMap(uint tk, out uint pdwMappingFlags, StringBuilder stringImportName, uint cchImportName, out uint pchImportName);
        uint EnumSignatures(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arraySignatures, uint cmax);
        uint EnumTypeSpecs(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arrayTypeSpecs, uint cmax);
        uint EnumUserStrings(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arrayStrings, uint cmax);
        [PreserveSig]
        int GetParamForMethodIndex(uint md, uint ulongParamSeq, out uint pointerParam);
        uint EnumCustomAttributes(ref uint handlePointerEnum, uint tk, uint tokenType, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayCustomAttributes, uint countMax);
        uint GetCustomAttributeProps(uint cv, out uint ptkObj, out uint ptkType, out void* ppBlob);
        uint FindTypeRef(uint tokenResolutionScope, string stringName);
        uint GetMemberProps(uint mb, out uint pointerClass, StringBuilder stringMember, uint cchMember, out uint pchMember, out uint pdwAttr,
          out byte* ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags, out uint pdwCPlusTypeFlag, out void* ppValue);
        uint GetFieldProps(uint mb, out uint pointerClass, StringBuilder stringField, uint cchField, out uint pchField, out uint pdwAttr,
          out byte* ppvSigBlob, out uint pcbSigBlob, out uint pdwCPlusTypeFlag, out void* ppValue);
        uint GetPropertyProps(uint prop, out uint pointerClass, StringBuilder stringProperty, uint cchProperty, out uint pchProperty, out uint pdwPropFlags,
          out byte* ppvSig, out uint bytePointerSig, out uint pdwCPlusTypeFlag, out void* ppDefaultValue, out uint pcchDefaultValue, out uint pmdSetter,
          out uint pmdGetter, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 14)] uint[] rmdOtherMethod, uint countMax);
        uint GetParamProps(uint tk, out uint pmd, out uint pulSequence, StringBuilder stringName, uint cchName, out uint pchName,
          out uint pdwAttr, out uint pdwCPlusTypeFlag, out void* ppValue);
        uint GetCustomAttributeByName(uint tokenObj, string stringName, out void* ppData);
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Bool)]
        bool IsValidToken(uint tk);
        uint GetNestedClassProps(uint typeDefNestedClass);
        uint GetNativeCallConvFromSig(void* voidPointerSig, uint byteCountSig);
        int IsGlobal(uint pd);
    }

    // The emit interface is only needed because the unmanaged pdb writer does a QueryInterface for it and fails if the wrapper does not implement it.
    // None of its methods are called.
    [SuppressUnmanagedCodeSecurity]
    internal class PdbMetadataWrapper : IMetaDataEmit, IMetaDataImport
    {
        private readonly MetadataWriter _writer;

        private uint _lastTypeDef;
        private string _lastTypeDefName;

        internal PdbMetadataWrapper(MetadataWriter writer)
        {
            _writer = writer;
        }

        // The only purpose of this method is to get type name and "is nested" flag, everything else is ignored by the SymWriter.
        // "td" is token returned by GetMethodProps or GetNestedClassProps
        unsafe uint IMetaDataImport.GetTypeDefProps(uint td, IntPtr stringTypeDef, uint cchTypeDef, out uint pchTypeDef, IntPtr pdwTypeDefFlags)
        {
            pchTypeDef = 0;
            if (td == 0)
            {
                return 0;
            }

            // The typeDef name should be fully qualified 
            ITypeDefinition t = _writer.GetTypeDefinition(td);
            string typeDefName;
            if (_lastTypeDef == td)
            {
                typeDefName = _lastTypeDefName;
            }
            else
            {
                typeDefName = MetadataWriter.GetMangledName((INamedTypeReference)t);

                INamespaceTypeDefinition namespaceTypeDef;

                if ((namespaceTypeDef = t.AsNamespaceTypeDefinition(_writer.Context)) != null)
                {
                    typeDefName = CodeAnalysis.MetadataHelpers.BuildQualifiedName(namespaceTypeDef.NamespaceName, typeDefName);
                }

                _lastTypeDef = td;
                _lastTypeDefName = typeDefName;
            }

            pchTypeDef = (uint)typeDefName.Length;
            if (pchTypeDef >= cchTypeDef)
            {
                pchTypeDef = cchTypeDef - 1;
            }

            char* pointerTypeDef = (char*)stringTypeDef.ToPointer();
            for (int i = 0; i < pchTypeDef; i++)
            {
                *(pointerTypeDef + i) = typeDefName[i];
            }

            *(pointerTypeDef + pchTypeDef) = (char)0;
            uint* pointerFlags = (uint*)pdwTypeDefFlags.ToPointer();
            *pointerFlags = _writer.GetTypeDefFlags(t.GetResolvedType(_writer.Context));
            return 0;
        }

        // The only purpose of this method is to get type name of the method and declaring type token (opaque for SymWriter), everything else is ignored by the SymWriter.
        // "mb" is the token passed to OpenMethod. The token is remembered until the corresponding CloseMethod, which passes it to GetMethodProps.
        // It's opaque for SymWriter.
        unsafe uint IMetaDataImport.GetMethodProps(uint mb, out uint pointerClass, IntPtr stringMethod, uint cchMethod, out uint pchMethod, IntPtr pdwAttr,
          IntPtr ppvSigBlob, IntPtr pcbSigBlob, IntPtr pulCodeRVA)
        {
            IMethodDefinition m = _writer.GetMethodDefinition(mb);
            pointerClass = (uint)_writer.GetTypeToken(m.GetContainingType(_writer.Context));
            string methName = m.Name;

            // if the buffer is too small to fit the name, truncate the name
            uint nameLengthIncludingNull = Math.Min((uint)methName.Length + 1, cchMethod);

            // we shall return the length of the name not including NUL
            pchMethod = nameLengthIncludingNull - 1;

            char* pointerMethName = (char*)stringMethod.ToPointer();
            for (int i = 0; i < pchMethod; i++)
            {
                *(pointerMethName + i) = methName[i];
            }

            *(pointerMethName + pchMethod) = (char)0;
            return 0;
        }

        uint IMetaDataImport.GetNestedClassProps(uint typeDefNestedClass)
        {
            INestedTypeReference nt = _writer.GetNestedTypeReference(typeDefNestedClass);
            if (nt == null)
            {
                return 0;
            }

            return (uint)_writer.GetTypeToken(nt.GetContainingType(_writer.Context));
        }

        #region Not Implemented 

        void IMetaDataEmit.SetModuleProps(string stringName)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.Save(string stringFile, uint dwordSaveFlags)
        {
            throw new NotImplementedException();
        }

        unsafe void IMetaDataEmit.SaveToStream(void* pointerIStream, uint dwordSaveFlags)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataEmit.GetSaveSize(uint save)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineTypeDef(char* stringTypeDef, uint dwordTypeDefFlags, uint tokenExtends, uint* rtkImplements)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineNestedType(char* stringTypeDef, uint dwordTypeDefFlags, uint tokenExtends, uint* rtkImplements, uint typeDefEncloser)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.SetHandler([MarshalAs(UnmanagedType.IUnknown), In] object pointerUnk)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineMethod(uint td, char* name, uint dwordMethodFlags, byte* voidPointerSigBlob, uint byteCountSigBlob, uint ulongCodeRVA, uint dwordImplFlags)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.DefineMethodImpl(uint td, uint tokenBody, uint tokenDecl)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineTypeRefByName(uint tokenResolutionScope, char* stringName)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineImportType(IntPtr pointerAssemImport, void* bytePointerHashValue, uint byteCountHashValue, IMetaDataImport pointerImport,
          uint typeDefImport, IntPtr pointerAssemEmit)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineMemberRef(uint tokenImport, string stringName, byte* voidPointerSigBlob, uint byteCountSigBlob)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineImportMember(IntPtr pointerAssemImport, void* bytePointerHashValue, uint byteCountHashValue,
          IMetaDataImport pointerImport, uint member, IntPtr pointerAssemEmit, uint tokenParent)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineEvent(uint td, string stringEvent, uint dwordEventFlags, uint tokenEventType, uint memberDefAddOn, uint memberDefRemoveOn, uint memberDefFire, uint* rmdOtherMethods)
        {
            throw new NotImplementedException();
        }

        unsafe void IMetaDataEmit.SetClassLayout(uint td, uint dwordPackSize, COR_FIELD_OFFSET* arrayFieldOffsets, uint ulongClassSize)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.DeleteClassLayout(uint td)
        {
            throw new NotImplementedException();
        }

        unsafe void IMetaDataEmit.SetFieldMarshal(uint tk, byte* voidPointerNativeType, uint byteCountNativeType)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.DeleteFieldMarshal(uint tk)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefinePermissionSet(uint tk, uint dwordAction, void* voidPointerPermission, uint byteCountPermission)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.SetRVA(uint md, uint ulongRVA)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.GetTokenFromSig(byte* voidPointerSig, uint byteCountSig)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataEmit.DefineModuleRef(string stringName)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.SetParent(uint mr, uint tk)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.GetTokenFromTypeSpec(byte* voidPointerSig, uint byteCountSig)
        {
            throw new NotImplementedException();
        }

        unsafe void IMetaDataEmit.SaveToMemory(void* bytePointerData, uint byteCountData)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataEmit.DefineUserString(string stringString, uint cchString)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.DeleteToken(uint tokenObj)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.SetMethodProps(uint md, uint dwordMethodFlags, uint ulongCodeRVA, uint dwordImplFlags)
        {
            throw new NotImplementedException();
        }

        unsafe void IMetaDataEmit.SetTypeDefProps(uint td, uint dwordTypeDefFlags, uint tokenExtends, uint* rtkImplements)
        {
            throw new NotImplementedException();
        }

        unsafe void IMetaDataEmit.SetEventProps(uint ev, uint dwordEventFlags, uint tokenEventType, uint memberDefAddOn, uint memberDefRemoveOn, uint memberDefFire, uint* rmdOtherMethods)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.SetPermissionSetProps(uint tk, uint dwordAction, void* voidPointerPermission, uint byteCountPermission)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.DefinePinvokeMap(uint tk, uint dwordMappingFlags, string stringImportName, uint importDLL)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.SetPinvokeMap(uint tk, uint dwordMappingFlags, string stringImportName, uint importDLL)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.DeletePinvokeMap(uint tk)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineCustomAttribute(uint tokenObj, uint tokenType, void* pointerCustomAttribute, uint byteCountCustomAttribute)
        {
            throw new NotImplementedException();
        }

        unsafe void IMetaDataEmit.SetCustomAttributeValue(uint pcv, void* pointerCustomAttribute, uint byteCountCustomAttribute)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineField(uint td, string stringName, uint dwordFieldFlags, byte* voidPointerSigBlob, uint byteCountSigBlob, uint dwordCPlusTypeFlag,
          void* pointerValue, uint cchValue)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineProperty(uint td, string stringProperty, uint dwordPropFlags, byte* voidPointerSig, uint byteCountSig, uint dwordCPlusTypeFlag,
          void* pointerValue, uint cchValue, uint memberDefSetter, uint memberDefGetter, uint* rmdOtherMethods)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineParam(uint md, uint ulongParamSeq, string stringName, uint dwordParamFlags, uint dwordCPlusTypeFlag, void* pointerValue, uint cchValue)
        {
            throw new NotImplementedException();
        }

        unsafe void IMetaDataEmit.SetFieldProps(uint fd, uint dwordFieldFlags, uint dwordCPlusTypeFlag, void* pointerValue, uint cchValue)
        {
            throw new NotImplementedException();
        }

        unsafe void IMetaDataEmit.SetPropertyProps(uint pr, uint dwordPropFlags, uint dwordCPlusTypeFlag, void* pointerValue, uint cchValue, uint memberDefSetter, uint memberDefGetter, uint* rmdOtherMethods)
        {
            throw new NotImplementedException();
        }

        unsafe void IMetaDataEmit.SetParamProps(uint pd, string stringName, uint dwordParamFlags, uint dwordCPlusTypeFlag, void* pointerValue, uint cchValue)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataEmit.DefineSecurityAttributeSet(uint tokenObj, IntPtr arraySecAttrs, uint countSecAttrs)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.ApplyEditAndContinue([MarshalAs(UnmanagedType.IUnknown)] object pointerImport)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.TranslateSigWithScope(IntPtr pointerAssemImport, void* bytePointerHashValue, uint byteCountHashValue,
          IMetaDataImport import, byte* bytePointerSigBlob, uint byteCountSigBlob, IntPtr pointerAssemEmit, IMetaDataEmit emit, byte* voidPointerTranslatedSig, uint byteCountTranslatedSigMax)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.SetMethodImplFlags(uint md, uint dwordImplFlags)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.SetFieldRVA(uint fd, uint ulongRVA)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.Merge(IMetaDataImport pointerImport, IntPtr pointerHostMapToken, [MarshalAs(UnmanagedType.IUnknown)] object pointerHandler)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.MergeEnd()
        {
            throw new NotImplementedException();
        }

        [PreserveSig]
        void IMetaDataImport.CloseEnum(uint handleEnum)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.CountEnum(uint handleEnum)
        {
            throw new NotImplementedException();
        }

        void IMetaDataImport.ResetEnum(uint handleEnum, uint ulongPos)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumTypeDefs(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arrayTypeDefs, uint countMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumInterfaceImpls(ref uint handlePointerEnum, uint td, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] arrayImpls, uint countMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumTypeRefs(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arrayTypeRefs, uint countMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.FindTypeDefByName(string stringTypeDef, uint tokenEnclosingClass)
        {
            throw new NotImplementedException();
        }

        Guid IMetaDataImport.GetScopeProps(StringBuilder stringName, uint cchName, out uint pchName)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetModuleFromScope()
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetInterfaceImplProps(uint impl, out uint pointerClass)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetTypeRefProps(uint tr, out uint ptkResolutionScope, StringBuilder stringName, uint cchName)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.ResolveTypeRef(uint tr, [In] ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppIScope)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumMembers(ref uint handlePointerEnum, uint cl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] arrayMembers, uint countMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumMembersWithName(ref uint handlePointerEnum, uint cl, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayMembers, uint countMax)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.EnumMethods(ref uint handlePointerEnum, uint cl, uint* arrayMethods, uint countMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumMethodsWithName(ref uint handlePointerEnum, uint cl, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayMethods, uint countMax)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.EnumFields(ref uint handlePointerEnum, uint cl, uint* arrayFields, uint countMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumFieldsWithName(ref uint handlePointerEnum, uint cl, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayFields, uint countMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumParams(ref uint handlePointerEnum, uint mb, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] arrayParams, uint countMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumMemberRefs(ref uint handlePointerEnum, uint tokenParent, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] arrayMemberRefs, uint countMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumMethodImpls(ref uint handlePointerEnum, uint td, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayMethodBody,
          [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayMethodDecl, uint countMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumPermissionSets(ref uint handlePointerEnum, uint tk, uint dwordActions, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayPermission,
          uint countMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.FindMember(uint td, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] voidPointerSigBlob, uint byteCountSigBlob)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.FindMethod(uint td, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] voidPointerSigBlob, uint byteCountSigBlob)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.FindField(uint td, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] voidPointerSigBlob, uint byteCountSigBlob)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.FindMemberRef(uint td, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] voidPointerSigBlob, uint byteCountSigBlob)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetMemberRefProps(uint mr, ref uint ptk, StringBuilder stringMember, uint cchMember, out uint pchMember, out byte* ppvSigBlob)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.EnumProperties(ref uint handlePointerEnum, uint td, uint* arrayProperties, uint countMax)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.EnumEvents(ref uint handlePointerEnum, uint td, uint* arrayEvents, uint countMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetEventProps(uint ev, out uint pointerClass, StringBuilder stringEvent, uint cchEvent, out uint pchEvent, out uint pdwEventFlags,
          out uint ptkEventType, out uint pmdAddOn, out uint pmdRemoveOn, out uint pmdFire,
          [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 11)] uint[] rmdOtherMethod, uint countMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumMethodSemantics(ref uint handlePointerEnum, uint mb, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] arrayEventProp, uint countMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetMethodSemantics(uint mb, uint tokenEventProp)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetClassLayout(uint td, out uint pdwPackSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] COR_FIELD_OFFSET[] arrayFieldOffset, uint countMax, out uint countPointerFieldOffset)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetFieldMarshal(uint tk, out byte* ppvNativeType)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetRVA(uint tk, out uint pulCodeRVA)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetPermissionSetProps(uint pm, out uint pdwAction, out void* ppvPermission)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetSigFromToken(uint memberDefSig, out IntPtr ppvSig, out uint pcbSig)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetModuleRefProps(uint mur, StringBuilder stringName, uint cchName)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumModuleRefs(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arrayModuleRefs, uint cmax)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetTypeSpecFromToken(uint typespec, out byte* ppvSig)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetNameFromToken(uint tk)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumUnresolvedMethods(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arrayMethods, uint countMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetUserString(uint stk, StringBuilder stringString, uint cchString)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetPinvokeMap(uint tk, out uint pdwMappingFlags, StringBuilder stringImportName, uint cchImportName, out uint pchImportName)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumSignatures(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arraySignatures, uint cmax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumTypeSpecs(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arrayTypeSpecs, uint cmax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumUserStrings(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arrayStrings, uint cmax)
        {
            throw new NotImplementedException();
        }

        [PreserveSig]
        int IMetaDataImport.GetParamForMethodIndex(uint md, uint ulongParamSeq, out uint pointerParam)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumCustomAttributes(ref uint handlePointerEnum, uint tk, uint tokenType, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayCustomAttributes, uint countMax)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetCustomAttributeProps(uint cv, out uint ptkObj, out uint ptkType, out void* ppBlob)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.FindTypeRef(uint tokenResolutionScope, string stringName)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetMemberProps(uint mb, out uint pointerClass, StringBuilder stringMember, uint cchMember, out uint pchMember, out uint pdwAttr,
          out byte* ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags, out uint pdwCPlusTypeFlag, out void* ppValue)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetFieldProps(uint mb, out uint pointerClass, StringBuilder stringField, uint cchField, out uint pchField, out uint pdwAttr,
          out byte* ppvSigBlob, out uint pcbSigBlob, out uint pdwCPlusTypeFlag, out void* ppValue)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetPropertyProps(uint prop, out uint pointerClass, StringBuilder stringProperty, uint cchProperty, out uint pchProperty, out uint pdwPropFlags,
          out byte* ppvSig, out uint bytePointerSig, out uint pdwCPlusTypeFlag, out void* ppDefaultValue, out uint pcchDefaultValue, out uint pmdSetter,
          out uint pmdGetter, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 14)] uint[] rmdOtherMethod, uint countMax)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetParamProps(uint tk, out uint pmd, out uint pulSequence, StringBuilder stringName, uint cchName, out uint pchName,
          out uint pdwAttr, out uint pdwCPlusTypeFlag, out void* ppValue)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetCustomAttributeByName(uint tokenObj, string stringName, out void* ppData)
        {
            throw new NotImplementedException();
        }

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Bool)]
        bool IMetaDataImport.IsValidToken(uint tk)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetNativeCallConvFromSig(void* voidPointerSig, uint byteCountSig)
        {
            throw new NotImplementedException();
        }

        int IMetaDataImport.IsGlobal(uint pd)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
