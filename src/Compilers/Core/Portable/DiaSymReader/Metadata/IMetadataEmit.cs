// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 436 // SuppressUnmanagedCodeSecurityAttribute defined in source and mscorlib 

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.DiaSymReader
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("BA3FEE4C-ECB9-4e41-83B7-183FA41CD859")]
    [SuppressUnmanagedCodeSecurity]
    internal unsafe interface IMetadataEmit
    {
        // SymWriter doesn't use any methods from this interface except for GetTokenFromSig, which is only called when 
        // DefineLocalVariable(2) and DefineConstant(2) don't specify signature token, or the token is nil.

        void __SetModuleProps();
        void __Save();
        void __SaveToStream();
        void __GetSaveSize();
        void __DefineTypeDef();
        void __DefineNestedType();
        void __SetHandler();
        void __DefineMethod();
        void __DefineMethodImpl();
        void __DefineTypeRefByName();

        void __DefineImportType();
        void __DefineMemberRef();
        void __DefineImportMember();
        void __DefineEvent();
        void __SetClassLayout();

        void __DeleteClassLayout();
        void __SetFieldMarshal();
        void __DeleteFieldMarshal();
        void __DefinePermissionSet();
        void __SetRVA();

        int GetTokenFromSig(byte* voidPointerSig, int byteCountSig);

        void __DefineModuleRef();
        void __SetParent();
        void __GetTokenFromTypeSpec();
        void __SaveToMemory();
        void __DefineUserString();
        void __DeleteToken();
        void __SetMethodProps();
        void __SetTypeDefProps();
        void __SetEventProps();
        void __SetPermissionSetProps();
        void __DefinePinvokeMap();
        void __SetPinvokeMap();
        void __DeletePinvokeMap();
        void __DefineCustomAttribute();
        void __SetCustomAttributeValue();
        void __DefineField();
        void __DefineProperty();
        void __DefineParam();
        void __SetFieldProps();
        void __SetPropertyProps();
        void __SetParamProps();
        void __DefineSecurityAttributeSet();
        void __ApplyEditAndContinue();
        void __TranslateSigWithScope();
        void __SetMethodImplFlags();
        void __SetFieldRVA();
        void __Merge();
        void __MergeEnd();
    }
}
